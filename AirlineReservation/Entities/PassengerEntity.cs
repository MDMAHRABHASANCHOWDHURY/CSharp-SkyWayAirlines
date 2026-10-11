using System;
using System.Data;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Helpers;
namespace AirlineReservationDemo.Entities
{
    internal sealed class PassengerEntity
    {
        public int PassId { get; set; }
        public string PassName { get; set; }
        public string PassAddress { get; set; }
        public string PassNat { get; set; }
        public string PassGender { get; set; }
        public string PassPhone { get; set; }
        public string PassEmail { get; set; }
        public string PassportNo { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public static PassengerEntity GetById(int id)
        {
            if (!Session.IsSignedIn || (!Session.Current.IsAdmin && Session.Current.PassId != id))
            throw new UnauthorizedAccessException("Profile access denied.");
            DataTable t = Db.Query("SELECT * FROM dbo.PassengerTbl WHERE PassId=@id", Db.P("@id", id));
            if (t.Rows.Count == 0)
            return null;
            DataRow x = t.Rows[0];
            return new PassengerEntity { PassId = id,
                PassName = Convert.ToString(x["PassName"]),
                PassAddress = Convert.ToString(x["PassAddress"]),
                PassNat = Convert.ToString(x["PassNat"]),
                PassGender = Convert.ToString(x["PassGender"]),
                PassPhone = Convert.ToString(x["PassPhone"]),
                PassEmail = Convert.ToString(x["PassEmail"]),
                PassportNo = Convert.ToString(x["PassportNo"]),
                DateOfBirth = x["DateOfBirth"] == DBNull.Value
                ? (DateTime?)null
                : Convert.ToDateTime(x["DateOfBirth"]) };
        }
        public static DataTable List(string query)
        {
            Session.RequireAdmin();
            return Db.Query(
            "SELECT * FROM dbo.PassengerTbl WHERE PassName LIKE @q OR PassEmail LIKE @q OR PassportNo " +
            "LIKE @q ORDER BY PassId DESC",
            Db.P("@q", Db.Like(query)));
        }
        public void Save()
        {
            Session.RequireAdmin();
            if (!Validator.IsPersonName(PassName) || !Validator.IsEmail(PassEmail) || !Validator.IsPhone(PassPhone))
            throw new ArgumentException("Enter a valid passenger name, email and phone.");
            if((PassAddress??string.Empty).Length>200 || (PassNat??string.Empty).Length>60)
            throw new ArgumentException("Address must be at most 200 characters and nationality at most 60.");
            if(PassGender!="Male" && PassGender!="Female" && PassGender!="Other") throw new ArgumentException("Choose a gender.");
            if (DateOfBirth.HasValue &&
            (DateOfBirth > DateTime.Today || Validator.AgeOn(DateOfBirth.Value, DateTime.Today) > 120))
            throw new ArgumentException("Check date of birth.");
            if (!string.IsNullOrWhiteSpace(PassportNo) && !Validator.IsPassport(PassportNo))
            throw new ArgumentException("Passport must contain 6–12 letters or digits.");
            string sql = PassId == 0
            ? "INSERT dbo.PassengerTbl " +
            "(PassName,PassAddress,PassNat,PassGender,PassPhone,PassEmail,PassportNo," +
            "DateOfBirth) VALUES (@n,@a,@nat,@g,@p,@e,@pp,@dob)"
            : "UPDATE dbo.PassengerTbl SET " +
            "PassName=@n,PassAddress=@a,PassNat=@nat,PassGender=@g,PassPhone=@p,PassEmail=@e," +
            "PassportNo=@pp,DateOfBirth=@dob WHERE PassId=@id";
            Db.Execute(sql, Db.P("@n", PassName.Trim()), Db.P("@a", PassAddress), Db.P("@nat", PassNat),
            Db.P("@g", PassGender), Db.P("@p", PassPhone.Trim()), Db.P("@e", PassEmail.Trim()),
            Db.P("@pp", PassportNo), Db.P("@dob", DateOfBirth), Db.P("@id", PassId));
        }
        public static void Delete(int id)
        {
            Session.RequireAdmin();
            Db.Execute("DELETE dbo.PassengerTbl WHERE PassId=@id", Db.P("@id", id));
        }
    }
}
