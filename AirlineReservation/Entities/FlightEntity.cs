using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
namespace AirlineReservationDemo.Entities
{
    internal static class FlightEntity
    {
        private const string InfoSql =
        @"SELECT f.*, s.City AS SrcCity, d.City AS DestCity, s.Country AS SrcCountry, d.Country AS DestCountry
             FROM dbo.FlightTbl f JOIN dbo.AirportTbl s ON s.Code=f.Fsrc JOIN dbo.AirportTbl d ON d.Code=f.FDest";
        public static DataTable GetAirports()
        {
            return Db.Query(
            "SELECT Code, City, Country, City + ' (' + Code + ')' AS Display FROM dbo.AirportTbl ORDER BY City");
        }
        public static FlightInfo GetInfo(int id)
        {
            DataTable t = Db.Query(InfoSql + " WHERE f.FCode=@id", Db.P("@id", id));
            return t.Rows.Count == 0 ? null : FromRow(t.Rows[0]);
        }
        internal static FlightInfo FromRow(DataRow x)
        {
            return new FlightInfo { FCode = Convert.ToInt32(x["FCode"]),
                FlightNo = Convert.ToString(x["FlightNo"]),
                Airline = Convert.ToString(x["Airline"]),
                Src = Convert.ToString(x["Fsrc"]).Trim(),
                Dest = Convert.ToString(x["FDest"]).Trim(),
                SrcCity = Convert.ToString(x["SrcCity"]),
                DestCity = Convert.ToString(x["DestCity"]),
                SrcCountry = Convert.ToString(x["SrcCountry"]),
                DestCountry = Convert.ToString(x["DestCountry"]),
                Departure = Convert.ToDateTime(x["DepartureTime"]),
                Arrival = Convert.ToDateTime(x["ArrivalTime"]),
                BusinessSeats = Convert.ToInt32(x["BusinessSeats"]),
                EconomySeats = Convert.ToInt32(x["EconomySeats"]),
                EconomyFare = Convert.ToDecimal(x["EconomyFare"]),
                BusinessFare = Convert.ToDecimal(x["BusinessFare"]),
                Status = Convert.ToString(x["FStatus"]) };
        }
        internal static FlightInfo GetInfoTx(SqlConnection con, SqlTransaction tx, int id)
        {
            DataTable t = Db.QueryTx(con, tx,
            InfoSql.Replace("dbo.FlightTbl f", "dbo.FlightTbl f WITH (UPDLOCK, ROWLOCK)") +
            " WHERE f.FCode=@id",
            Db.P("@id", id));
            if (t.Rows.Count == 0)
            throw new BookingException("Flight not found.");
            return FromRow(t.Rows[0]);
        }
        public static HashSet<string> GetTakenSeats(int id)
        {
            var result = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (DataRow x in Db
            .Query("SELECT SeatNo FROM dbo.TicketTbl WHERE FCode=@f AND TicketStatus=N'Confirmed'",
            Db.P("@f", id))
            .Rows)
            result.Add(Convert.ToString(x[0]));
            return result;
        }
        public static DataTable Search(string src, string dest, DateTime date, string cabin, int passengers)
        {
            if(passengers<1 || passengers>BookingService.MaxTravellers) throw new ArgumentException("Choose 1–9 travellers.");
            if (cabin != Cabin.Business && cabin != Cabin.Economy)
            throw new ArgumentException("Invalid cabin.");
            return Db.Query(@"SELECT f.*, CASE WHEN @c=N'Business' THEN f.BusinessFare ELSE f.EconomyFare END AS Fare,
              CASE WHEN @c=N'Business' THEN a.BusinessSeats-a.BusinessSold ELSE a.EconomySeats-a.EconomySold END AS SeatsLeft
              FROM dbo.FlightTbl f JOIN dbo.vw_FlightAvailability a ON a.FCode=f.FCode
              WHERE f.Fsrc=@s AND f.FDest=@d AND f.DepartureTime>=@day AND f.DepartureTime<DATEADD(day,1,@day)
              AND f.DepartureTime>DATEADD(hour,1,SYSDATETIME()) AND f.FStatus<>N'Cancelled'
              AND CASE WHEN @c=N'Business' THEN a.BusinessSeats-a.BusinessSold ELSE a.EconomySeats-a.EconomySold END >=@p ORDER BY f.DepartureTime",
            Db.P("@s", src), Db.P("@d", dest), Db.P("@day", date.Date), Db.P("@c", cabin),
            Db.P("@p", passengers));
        }
        public static DataTable List(string search)
        {
            Session.RequireAdmin();
            return Db.Query(
            "SELECT *, Fsrc+' → '+FDest AS Route FROM dbo.FlightTbl WHERE FlightNo LIKE @q OR Fsrc LIKE " +
            "@q OR FDest LIKE @q ORDER BY DepartureTime DESC",
            Db.P("@q", Db.Like(search)));
        }
        public static void Save(FlightInfo f)
        {
            Session.RequireAdmin();
            if(f==null) throw new ArgumentNullException("f");
            if (!Validator.IsFlightNumber(f.FlightNo) || string.IsNullOrWhiteSpace(f.Airline) || f.Airline.Trim().Length>60)
            throw new ArgumentException("Enter an airline and a valid flight number, such as SW101.");
            if (f.Src == f.Dest || f.Arrival <= f.Departure || f.Departure <= DateTime.Now.AddHours(1))
            throw new ArgumentException("Choose different airports and future departure/arrival times.");
            if (f.BusinessSeats < 0 || f.EconomySeats < 0 || f.BusinessSeats + f.EconomySeats < 1 ||
            f.BusinessSeats + f.EconomySeats > 600)
            throw new ArgumentException("Total capacity must be between 1 and 600.");
            if (f.EconomyFare < 0 || f.BusinessFare < 0 || f.BusinessFare>1000000m || f.EconomyFare>1000000m || (f.Status != "Scheduled" && f.Status != "Delayed"))
            throw new ArgumentException("Check fares and status. Use Cancel flight to cancel and refund bookings.");
            Db.InTransaction<int>(delegate(SqlConnection con, SqlTransaction tx) {
                if (f.FCode > 0)
                {
                    FlightInfo existing = GetInfoTx(con, tx, f.FCode);
                    if (existing.Status == "Cancelled")
                    throw new ArgumentException("A cancelled flight cannot be changed.");
                    int booked = Convert.ToInt32(Db.ScalarTx(
                    con, tx, "SELECT COUNT(*) FROM dbo.BookingTbl WHERE FCode=@f", Db.P("@f", f.FCode)));
                    if (booked > 0 &&
                    (existing.Src != f.Src || existing.Dest != f.Dest ||
                    existing.BusinessSeats != f.BusinessSeats || existing.EconomySeats != f.EconomySeats ||
                    existing.Departure != f.Departure || existing.Arrival != f.Arrival ||
                    existing.FlightNo != f.FlightNo || existing.Airline != f.Airline))
                    throw new ArgumentException(
                    "This flight has booking history. Its route, identity, schedule and " +
                    "seat layout are fixed; fares and delay status can still be changed.");
                }
                string sql =
                f.FCode == 0
                ? @"INSERT dbo.FlightTbl " +
                @"(FlightNo,Airline,Fsrc,FDest,DepartureTime,ArrivalTime,BusinessSeats,EconomySeats," +
                @"EconomyFare,BusinessFare,FStatus) VALUES " +
                @"(@no,@air,@s,@d,@dep,@arr,@bus,@eco,@ef,@bf,@st)"
                : @"UPDATE dbo.FlightTbl SET " +
                @"FlightNo=@no,Airline=@air,Fsrc=@s,FDest=@d,DepartureTime=@dep,ArrivalTime=@arr," +
                @"BusinessSeats=" +
                @"@bus,EconomySeats=@eco,EconomyFare=@ef,BusinessFare=@bf,FStatus=@st WHERE FCode=@id";
                Db.ExecuteTx(con, tx, sql, Db.P("@no", f.FlightNo.Trim().ToUpperInvariant()),
                Db.P("@air", f.Airline.Trim()), Db.P("@s", f.Src), Db.P("@d", f.Dest),
                Db.P("@dep", f.Departure), Db.P("@arr", f.Arrival), Db.P("@bus", f.BusinessSeats),
                Db.P("@eco", f.EconomySeats), Db.P("@ef", f.EconomyFare), Db.P("@bf", f.BusinessFare),
                Db.P("@st", f.Status), Db.P("@id", f.FCode));
                return 0;
            });
        }
        public static void Delete(int id)
        {
            Session.RequireAdmin();
            Db.Execute("DELETE dbo.FlightTbl WHERE FCode=@f", Db.P("@f", id));
        }
    }
}
