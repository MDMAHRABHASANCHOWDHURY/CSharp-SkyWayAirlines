using System;
using System.Data;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Entities;

namespace AirlineReservationDemo.Services
{
    internal sealed class AdminStats
    {
        public int UpcomingFlights { get; set; }
        public int ConfirmedBookings { get; set; }
        public int TicketsSold { get; set; }
        public int Passengers { get; set; }
        public int CancelledBookings { get; set; }
        public decimal NetRevenue { get; set; }
    }

    internal sealed class UserSummary
    {
        public int UpcomingTrips { get; set; }
        public int TotalBookings { get; set; }
        public decimal NetSpent { get; set; }
    }

    /// <summary>Read-only figures for the dashboards.</summary>
    internal static class ReportService
    {
        public static AdminStats GetAdminStats()
        {
            Session.RequireAdmin();
            DataTable t = Db.Query(@"
                SELECT
                  (SELECT COUNT(*) FROM dbo.FlightTbl WHERE DepartureTime >= SYSDATETIME() AND FStatus <> N'Cancelled') AS UpcomingFlights,
                  (SELECT COUNT(*) FROM dbo.BookingTbl WHERE BookingStatus = N'Confirmed') AS ConfirmedBookings,
                  (SELECT COUNT(*) FROM dbo.TicketTbl  WHERE TicketStatus  = N'Confirmed') AS TicketsSold,
                  (SELECT COUNT(*) FROM dbo.PassengerTbl) AS Passengers,
                  (SELECT COUNT(*) FROM dbo.BookingTbl WHERE BookingStatus = N'Cancelled') AS CancelledBookings,
                  (SELECT ISNULL(SUM(CASE WHEN PaymentType = N'Charge' THEN Amount ELSE -Amount END), 0) FROM dbo.PaymentTbl) AS NetRevenue");

            DataRow r = t.Rows[0];
            return new AdminStats { UpcomingFlights = Convert.ToInt32(r["UpcomingFlights"]),
                ConfirmedBookings = Convert.ToInt32(r["ConfirmedBookings"]),
                TicketsSold = Convert.ToInt32(r["TicketsSold"]),
                Passengers = Convert.ToInt32(r["Passengers"]),
                CancelledBookings = Convert.ToInt32(r["CancelledBookings"]),
                NetRevenue = Convert.ToDecimal(r["NetRevenue"]) };
        }

        public static DataTable GetRecentBookings(int count)
        {
            Session.RequireAdmin();
            return Db.Query(@"
                SELECT TOP (@n) b.PNR, p.PassName AS Customer, f.FlightNo, f.Fsrc, f.FDest, f.DepartureTime,
                       b.CabinClass, b.TotalAmount, b.BookingStatus, b.BookedAt
                FROM   dbo.BookingTbl b
                JOIN   dbo.FlightTbl f ON f.FCode = b.FCode
                JOIN   dbo.users u ON u.id = b.UserId
                LEFT JOIN dbo.PassengerTbl p ON p.PassId = u.PassId
                ORDER BY b.BookedAt DESC, b.BookingId DESC",
            Db.P("@n", count));
        }

        /// <summary>Upcoming flights ordered by how full they are.</summary>
        public static DataTable GetFillingFlights(int count)
        {
            Session.RequireAdmin();
            return Db.Query(@"
                SELECT TOP (@n) f.FlightNo, f.Fsrc, f.FDest, f.DepartureTime,
                       v.BusinessSold + v.EconomySold AS Sold, f.FCap,
                       CAST(100.0 * (v.BusinessSold + v.EconomySold) / NULLIF(f.FCap, 0) AS DECIMAL(5,1)) AS LoadFactor
                FROM   dbo.FlightTbl f
                JOIN   dbo.vw_FlightAvailability v ON v.FCode = f.FCode
                WHERE  f.DepartureTime >= SYSDATETIME() AND f.FStatus <> N'Cancelled'
                ORDER BY (v.BusinessSold + v.EconomySold) DESC, f.DepartureTime",
            Db.P("@n", count));
        }

        public static UserSummary GetUserSummary(int userId)
        {
            Session.RequireUser(userId);
            DataTable t = Db.Query(@"
                SELECT
                  (SELECT COUNT(*) FROM dbo.BookingTbl b JOIN dbo.FlightTbl f ON f.FCode = b.FCode
                    WHERE b.UserId = @u AND b.BookingStatus = N'Confirmed' AND f.DepartureTime >= SYSDATETIME()) AS UpcomingTrips,
                  (SELECT COUNT(*) FROM dbo.BookingTbl WHERE UserId = @u) AS TotalBookings,
                  (SELECT ISNULL(SUM(CASE WHEN p.PaymentType = N'Charge' THEN p.Amount ELSE -p.Amount END), 0)
                     FROM dbo.PaymentTbl p JOIN dbo.BookingTbl b ON b.BookingId = p.BookingId WHERE b.UserId = @u) AS NetSpent",
            Db.P("@u", userId));

            DataRow r = t.Rows[0];
            return new UserSummary { UpcomingTrips = Convert.ToInt32(r["UpcomingTrips"]),
                TotalBookings = Convert.ToInt32(r["TotalBookings"]),
                NetSpent = Convert.ToDecimal(r["NetSpent"]) };
        }

        /// <summary>The traveller's next confirmed flight, or null.</summary>
        public static DataRow GetNextTrip(int userId)
        {
            Session.RequireUser(userId);
            DataTable t = Db.Query(@"
                SELECT TOP 1 b.BookingId, b.PNR, f.FlightNo, f.Fsrc, f.FDest, a1.City AS SrcCity, a2.City AS DestCity,
                       f.DepartureTime, f.ArrivalTime, b.CabinClass
                FROM   dbo.BookingTbl b
                JOIN   dbo.FlightTbl f ON f.FCode = b.FCode
                JOIN   dbo.AirportTbl a1 ON a1.Code = f.Fsrc
                JOIN   dbo.AirportTbl a2 ON a2.Code = f.FDest
                WHERE  b.UserId = @u AND b.BookingStatus = N'Confirmed' AND f.DepartureTime >= SYSDATETIME()
                ORDER BY f.DepartureTime",
            Db.P("@u", userId));
            return t.Rows.Count == 0 ? null : t.Rows[0];
        }
    }
}
