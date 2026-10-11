using System;
using System.Data;
using System.Data.SqlClient;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;

namespace AirlineReservationDemo.Services
{
    internal static class AccountService
    {
        private static int PassengerId()
        {
            if (!Session.IsSignedIn || Session.Current.IsAdmin || !Session.Current.PassId.HasValue)
                throw new UnauthorizedAccessException("Sign in as a passenger.");
            return Session.Current.PassId.Value;
        }
        public static DataRow Profile()
        {
            DataTable t = Db.Query("SELECT * FROM dbo.PassengerTbl WHERE PassId=@id", Db.P("@id", PassengerId()));
            if(t.Rows.Count!=1) throw new ArgumentException("Profile not found.");
            return t.Rows[0];
        }
        public static void SaveProfile(RegistrationRequest p, byte[] photo)
        {
            int id=PassengerId();
            if(p==null) throw new ArgumentNullException("p");
            if(!Validator.IsPersonName(p.FullName) || !Validator.IsEmail(p.Email) || !Validator.IsPhone(p.Phone))
                throw new ArgumentException("Enter a valid name, email and phone number.");
            int age=Validator.AgeOn(p.DateOfBirth,DateTime.Today);
            if(age<12 || age>120) throw new ArgumentException("Account holders must be between 12 and 120 years old.");
            if(p.Gender!="Male" && p.Gender!="Female" && p.Gender!="Other") throw new ArgumentException("Choose your gender.");
            if((p.Address??"").Length>200 || (p.Nationality??"").Length>60) throw new ArgumentException("Address or nationality is too long.");
            if(!string.IsNullOrWhiteSpace(p.PassportNo) && !Validator.IsPassport(p.PassportNo)) throw new ArgumentException("Enter a valid passport number.");
            if(photo!=null && photo.Length>2*1024*1024) throw new ArgumentException("Photo must be under 2 MB.");
            var imageParameter=new SqlParameter("@photo",SqlDbType.VarBinary,-1){Value=(object)photo??DBNull.Value};
            int changed=Db.Execute(@"UPDATE dbo.PassengerTbl SET PassName=@name,PassEmail=@email,PassPhone=@phone,
                PassAddress=@address,PassNat=@nat,PassGender=@gender,PassportNo=@passport,DateOfBirth=@dob,ProfilePhoto=@photo
                WHERE PassId=@id",Db.P("@name",p.FullName.Trim()),Db.P("@email",p.Email.Trim()),Db.P("@phone",p.Phone.Trim()),
                Db.P("@address",(p.Address??"").Trim()),Db.P("@nat",(p.Nationality??"").Trim()),Db.P("@gender",p.Gender),
                Db.P("@passport",(p.PassportNo??"").Trim().ToUpperInvariant()),Db.P("@dob",p.DateOfBirth.Date),imageParameter,Db.P("@id",id));
            if(changed!=1) throw new ArgumentException("Profile not found.");
            Session.Current.FullName=p.FullName.Trim();Session.Current.Email=p.Email.Trim();
        }
        public static DataTable MyRequests()
        {
            PassengerId();
            return Db.Query("SELECT RequestId,Status,RequestedAt,ReviewedAt,Reason FROM dbo.PasswordResetRequests WHERE UserId=@u ORDER BY RequestId DESC",Db.P("@u",Session.Current.Id));
        }
        public static void RequestReset(string reason)
        {
            PassengerId();
            if(string.IsNullOrWhiteSpace(reason) || reason.Trim().Length>500) throw new ArgumentException("Enter a reason of up to 500 characters. Do not include a password.");
            try { Db.Execute("INSERT dbo.PasswordResetRequests(UserId,Reason) VALUES(@u,@r)",Db.P("@u",Session.Current.Id),Db.P("@r",reason.Trim())); }
            catch(SqlException ex) {if(Db.IsDuplicateKey(ex)) throw new ArgumentException("You already have a pending request.");throw;}
        }
        public static DataTable Accounts()
        {
            Session.RequireAdmin();
            return Db.Query(@"SELECT u.id,u.username,p.PassName,u.IsActive,u.MustChangePassword,
                q.RequestId,q.Status,q.RequestedAt,q.Reason FROM dbo.users u JOIN dbo.role r ON r.roleid=u.userrole
                LEFT JOIN dbo.PassengerTbl p ON p.PassId=u.PassId
                OUTER APPLY(SELECT TOP(1) * FROM dbo.PasswordResetRequests x WHERE x.UserId=u.id ORDER BY x.RequestId DESC)q
                WHERE r.roletype=N'passenger' ORDER BY CASE WHEN q.Status=N'Pending' THEN 0 ELSE 1 END,u.username");
        }
        public static void ResetPassword(int userId,string temporaryPassword)
        {
            Session.RequireAdmin();
            if(!Validator.IsStrongPassword(temporaryPassword)) throw new ArgumentException("Use 8–128 characters containing letters and numbers.");
            string hash=PasswordHasher.Hash(temporaryPassword);
            int admin=Session.Current.Id;
            Db.InTransaction<int>((con,tx)=>{
                DataTable t=Db.QueryTx(con,tx,@"SELECT u.PasswordHash FROM dbo.users u WITH(UPDLOCK,ROWLOCK)
                    JOIN dbo.role r ON r.roleid=u.userrole WHERE u.id=@id AND r.roletype=N'passenger'",Db.P("@id",userId));
                if(t.Rows.Count!=1) throw new ArgumentException("Select a passenger account.");
                if(PasswordHasher.Verify(temporaryPassword,Convert.ToString(t.Rows[0][0]))) throw new ArgumentException("Choose a different temporary password.");
                Db.ExecuteTx(con,tx,"UPDATE dbo.users SET PasswordHash=@h,MustChangePassword=1,FailedAttempts=0,LockoutUntil=NULL WHERE id=@id",Db.P("@h",hash),Db.P("@id",userId));
                Db.ExecuteTx(con,tx,"UPDATE dbo.PasswordResetRequests SET Status=N'Approved',ReviewedAt=SYSDATETIME(),ReviewedBy=@a WHERE UserId=@u AND Status=N'Pending'",Db.P("@a",admin),Db.P("@u",userId));
                Db.ExecuteTx(con,tx,"INSERT dbo.PasswordResetAudit(UserId,AdminId) VALUES(@u,@a)",Db.P("@u",userId),Db.P("@a",admin));return 0;
            });
        }
        public static void RejectRequest(int requestId)
        {
            Session.RequireAdmin();
            if(Db.Execute("UPDATE dbo.PasswordResetRequests SET Status=N'Rejected',ReviewedAt=SYSDATETIME(),ReviewedBy=@a WHERE RequestId=@id AND Status=N'Pending'",Db.P("@a",Session.Current.Id),Db.P("@id",requestId))!=1)
                throw new ArgumentException("This request is no longer pending. Refresh the list.");
        }
        public static void ChangeOwnPassword(string currentPassword,string newPassword)
        {
            if(!Session.IsSignedIn) throw new UnauthorizedAccessException("Sign in first.");
            if(!Validator.IsStrongPassword(newPassword)) throw new ArgumentException("Use 8–128 characters containing letters and numbers.");
            int id=Session.Current.Id;
            Db.InTransaction<int>((con,tx)=>{
                object hash=Db.ScalarTx(con,tx,"SELECT PasswordHash FROM dbo.users WITH(UPDLOCK,ROWLOCK) WHERE id=@id AND IsActive=1",Db.P("@id",id));
                if(hash==null || !PasswordHasher.Verify(currentPassword,Convert.ToString(hash))) throw new ArgumentException("Current password is incorrect.");
                if(PasswordHasher.Verify(newPassword,Convert.ToString(hash))) throw new ArgumentException("Your new password must be different.");
                Db.ExecuteTx(con,tx,"UPDATE dbo.users SET PasswordHash=@h,MustChangePassword=0,FailedAttempts=0,LockoutUntil=NULL WHERE id=@id",Db.P("@h",PasswordHasher.Hash(newPassword)),Db.P("@id",id));return 0;
            });
            Session.Current.MustChangePassword=false;
        }
    }
}
