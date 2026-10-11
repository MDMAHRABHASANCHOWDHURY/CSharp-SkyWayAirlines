using System.Data;
using AirlineReservationDemo.Data;
namespace AirlineReservationDemo.Entities
{
    internal static class TicketEntity
    {
        public static DataTable ListBookings(string query)
        {
            Session.RequireAdmin();
            return Db.Query(@"SELECT b.BookingId,b.PNR,u.username,f.FlightNo,f.Fsrc+' → '+f.FDest AS Route,
                 f.DepartureTime,b.CabinClass,b.TotalAmount,b.BookingStatus,b.RefundAmount,b.ContactEmail,
                 (SELECT COUNT(*) FROM dbo.TicketTbl t WHERE t.BookingId=b.BookingId) AS Pax
                 FROM dbo.BookingTbl b JOIN dbo.users u ON u.id=b.UserId JOIN dbo.FlightTbl f ON f.FCode=b.FCode
                 WHERE b.PNR LIKE @q OR u.username LIKE @q OR b.ContactEmail LIKE @q OR f.FlightNo LIKE @q ORDER BY b.BookedAt DESC",
            Db.P("@q", Db.Like(query)));
        }
    }
}
