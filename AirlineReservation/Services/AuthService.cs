using System;
using System.Data;
using System.Data.SqlClient;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;

namespace AirlineReservationDemo.Services
{
    /// <summary>A problem the user can fix (wrong password, locked account, duplicate username...).</summary>
    internal sealed class AuthException : Exception
    {
        public AuthException(string message) : base(message)
        {
        }
    }

    internal sealed class RegistrationRequest
    {
        public string FullName { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Nationality { get; set; }
        public string Gender { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string PassportNo { get; set; }
        public string Address { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
    }

    /// <summary>Sign-in, registration and demo-account seeding.</summary>
    internal static class AuthService
    {
        private const int MaxFailedAttempts = 5;
        private const int LockoutMinutes = 10;

        public const string DemoAdminUser = "admin";
        public const string DemoAdminPassword = "Admin@123";
        public const string DemoPassengerUser = "passenger";
        public const string DemoPassengerPassword = "Pass@123";

        private sealed class LoginAttempt
        {
            public UserEntity User { get; set; }
            public string Error { get; set; }
        }

        public static UserEntity Login(string username, string password)
        {
            Session.End();
            if(!Validator.IsUsername((username??string.Empty).Trim()) || string.IsNullOrEmpty(password) || password.Length>128)
            throw new AuthException("Invalid username or password.");
            LoginAttempt attempt=Db.InTransaction(delegate(SqlConnection con,SqlTransaction tx)
            {
                DataTable table=Db.QueryTx(con,tx,@"
                    SELECT u.MustChangePassword,u.id,u.username,u.PasswordHash,u.IsActive,u.FailedAttempts,u.LockoutUntil,
                           r.roletype,u.PassId,p.PassName,p.PassEmail,SYSDATETIME() AS ServerNow
                    FROM dbo.users u WITH (UPDLOCK,ROWLOCK)
                    JOIN dbo.role r ON r.roleid=u.userrole
                    LEFT JOIN dbo.PassengerTbl p ON p.PassId=u.PassId
                    WHERE u.username=@u",Db.P("@u",username.Trim()));
                if(table.Rows.Count==0) return new LoginAttempt{Error="Invalid username or password."};
                DataRow row=table.Rows[0];
                int id=Convert.ToInt32(row["id"]);
                DateTime now=Convert.ToDateTime(row["ServerNow"]);
                if(!Convert.ToBoolean(row["IsActive"])) return new LoginAttempt{Error="This account has been disabled. Please contact support."};
                if(row["LockoutUntil"]!=DBNull.Value && Convert.ToDateTime(row["LockoutUntil"])>now)
                return new LoginAttempt{Error="Too many failed attempts. Try again after "+Convert.ToDateTime(row["LockoutUntil"]).ToString("HH:mm")+" (server time)."};
                if(!PasswordHasher.Verify(password,Convert.ToString(row["PasswordHash"])))
                {
                    int failed=Convert.ToInt32(row["FailedAttempts"])+1;
                    bool locked=failed>=MaxFailedAttempts;
                    Db.ExecuteTx(con,tx,@"UPDATE dbo.users SET FailedAttempts=@f,
                        LockoutUntil=CASE WHEN @lock=1 THEN DATEADD(MINUTE,@mins,SYSDATETIME()) ELSE NULL END WHERE id=@id",
                    Db.P("@f",locked?0:failed),Db.P("@lock",locked?1:0),Db.P("@mins",LockoutMinutes),Db.P("@id",id));
                    // Return the error rather than throwing: failure counters must COMMIT.
                    return new LoginAttempt{Error=locked?"Too many failed attempts. The account is locked for 10 minutes.":"Invalid username or password."};
                }
                string role=Convert.ToString(row["roletype"]);
                if(role!="admin" && role!="passenger") return new LoginAttempt{Error="This account has an unsupported role. Contact support."};
                Db.ExecuteTx(con,tx,"UPDATE dbo.users SET FailedAttempts=0,LockoutUntil=NULL,LastLoginAt=SYSDATETIME() WHERE id=@id",Db.P("@id",id));
                return new LoginAttempt{User=new UserEntity{
                        MustChangePassword=Convert.ToBoolean(row["MustChangePassword"]),Id=id,Username=Convert.ToString(row["username"]),Role=role,
                        PassId=row["PassId"]==DBNull.Value?(int?)null:Convert.ToInt32(row["PassId"]),
                        FullName=row["PassName"]==DBNull.Value?Convert.ToString(row["username"]):Convert.ToString(row["PassName"]),
                        Email=row["PassEmail"] as string}};
            });
            if(attempt.User==null) throw new AuthException(attempt.Error);
            Session.Start(attempt.User);
            return attempt.User;
        }

        /// <summary>Creates the passenger profile and the login in one transaction.</summary>
        public static void Register(RegistrationRequest r)
        {
            if (r == null || !Validator.IsPersonName(r.FullName) || !Validator.IsEmail(r.Email) ||
            !Validator.IsPhone(r.Phone) || !Validator.IsUsername(r.Username) ||
            !Validator.IsStrongPassword(r.Password))
            throw new AuthException("Check the registration details and password.");
            if((r.Address??string.Empty).Length>200 || (r.Nationality??string.Empty).Length>60)
            throw new AuthException("Address must be at most 200 characters and nationality at most 60.");
            int age = Validator.AgeOn(r.DateOfBirth, DateTime.Today);
            if (age < 12 || age > 120)
            throw new AuthException("Account holders must be between 12 and 120 years old.");
            if (r.Gender != "Male" && r.Gender != "Female" && r.Gender != "Other")
            throw new AuthException("Choose your gender.");
            if (!string.IsNullOrWhiteSpace(r.PassportNo) && !Validator.IsPassport(r.PassportNo))
            throw new AuthException("Check your passport number.");
            try
            {
                Db.InTransaction<int>(delegate(SqlConnection con, SqlTransaction tx) {
                    object passId = Db.ScalarTx(
                    con, tx, @"
                        INSERT INTO dbo.PassengerTbl
                            (PassName, PassAddress, PassNat, PassGender, PassPhone, PassEmail, PassportNo, DateOfBirth)
                        OUTPUT INSERTED.PassId
                        VALUES (@name, @address, @nat, @gender, @phone, @email, @passport, @dob)",
                    Db.P("@name", r.FullName.Trim()),
                    Db.P("@address", string.IsNullOrWhiteSpace(r.Address) ? null : r.Address.Trim()),
                    Db.P("@nat", r.Nationality), Db.P("@gender", r.Gender), Db.P("@phone", r.Phone.Trim()),
                    Db.P("@email", r.Email.Trim()),
                    Db.P("@passport",
                    string.IsNullOrWhiteSpace(r.PassportNo) ? null : r.PassportNo.Trim().ToUpperInvariant()),
                    Db.P("@dob", r.DateOfBirth.Date));

                    Db.ExecuteTx(con, tx, @"
                        INSERT INTO dbo.users (username, PasswordHash, userrole, PassId)
                        VALUES (@u, @h, (SELECT roleid FROM dbo.role WHERE roletype = N'passenger'), @p)",
                    Db.P("@u", r.Username.Trim()), Db.P("@h", PasswordHasher.Hash(r.Password)),
                    Db.P("@p", Convert.ToInt32(passId)));
                    return 0;
                });
            }
            catch (SqlException ex)
            {
                if (Db.IsDuplicateKey(ex))
                {
                    throw new AuthException("That username is already taken. Please choose another.");
                }
                throw;
            }
        }

        /// <summary>Creates demo accounts the first time the database is empty.</summary>
        public static void EnsureDefaultAccounts()
        {
            if(!string.Equals(System.Configuration.ConfigurationManager.AppSettings["SeedDemoAccounts"],"true",StringComparison.OrdinalIgnoreCase)) return;
            if (Db.Scalar("SELECT id FROM dbo.users WHERE username=@u", Db.P("@u", DemoAdminUser)) == null)
            Db.Execute(@"INSERT INTO dbo.users (username, PasswordHash, userrole)
                         VALUES (@u, @h, (SELECT roleid FROM dbo.role WHERE roletype = N'admin'))",
            Db.P("@u", DemoAdminUser), Db.P("@h", PasswordHasher.Hash(DemoAdminPassword)));

            if (Db.Scalar("SELECT id FROM dbo.users WHERE username=@u", Db.P("@u", DemoPassengerUser)) == null)
            Register(new RegistrationRequest { FullName = "Demo Passenger", Email = "passenger@example.com",
                Phone = "+8801700000000", Nationality = "Bangladeshi",
                Gender = "Male", DateOfBirth = new DateTime(1998, 5, 20),
                PassportNo = "A01234567", Address = "Dhaka, Bangladesh",
                Username = DemoPassengerUser, Password = DemoPassengerPassword });
        }
    }
}
