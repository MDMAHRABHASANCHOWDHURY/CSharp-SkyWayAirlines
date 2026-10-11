using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Security.Cryptography;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;

namespace AirlineReservationDemo.Services
{
    /// <summary>A booking problem the user can act on (seat taken, card declined, flight closed...).</summary>
    internal sealed class BookingException : Exception
    {
        public bool SeatConflict { get; private set; }
        public bool FlightChanged { get; private set; }

        public BookingException(string message) : base(message)
        {
        }

        public BookingException(string message, bool seatConflict, bool flightChanged=false) : base(message)
        {
            SeatConflict = seatConflict;
            FlightChanged = flightChanged;
        }
    }

    internal sealed class BookingRequest
    {
        public int UserId { get; set; }
        public FlightInfo Flight { get; set; }
        public string Cabin { get; set; }
        public IList<TravellerInput> Travellers { get; set; }
        public string ContactEmail { get; set; }
        public string ContactPhone { get; set; }
        public CardDetails Card { get; set; }
    }

    internal sealed class BookingResult
    {
        public int BookingId { get; set; }
        public string Pnr { get; set; }
        public decimal Total { get; set; }
        public string PaymentReference { get; set; }
    }

    internal sealed class CancelResult
    {
        public decimal Refund { get; set; }
        public int Percent { get; set; }
        public string Reference { get; set; }
    }

    /// <summary>Everything printed on one e-ticket.</summary>
    internal sealed class TicketData
    {
        public string Pnr { get; set; }
        public string TicketNo { get; set; }
        public string PassengerName { get; set; }
        public string Gender { get; set; }
        public string Airline { get; set; }
        public string FlightNo { get; set; }
        public string Src { get; set; }
        public string Dest { get; set; }
        public string SrcCity { get; set; }
        public string DestCity { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public string Seat { get; set; }
        public string Cabin { get; set; }
        public int BaggageKg { get; set; }
        public string Status { get; set; }
        public DateTime BookedAt { get; set; }
    }

    /// <summary>Creates, reads and cancels bookings.</summary>
    internal static class BookingService
    {
        public const int MaxTravellers = 9;
        private const string PnrAlphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"; // no 0/O/1/I

        // ------------------------------------------------------------ book

        public static BookingResult Book(BookingRequest r)
        {
            ValidateRequest(r);
            Session.RequireUser(r.UserId);
            PriceQuote quote = null;

            try
            {
                return Db.InTransaction(delegate(SqlConnection con, SqlTransaction tx) {
                    // Lock the flight row so status/time checks and the inserts are consistent.
                    DataTable ft =
                    Db.QueryTx(con, tx,
                    "SELECT FStatus, DepartureTime, SYSDATETIME() AS Now FROM dbo.FlightTbl " +
                    "WITH (UPDLOCK, ROWLOCK) WHERE FCode = @f",
                    Db.P("@f", r.Flight.FCode));
                    if (ft.Rows.Count == 0)
                    throw new BookingException("This flight no longer exists.",false,true);
                    if (Convert.ToString(ft.Rows[0]["FStatus"]) == "Cancelled")
                    throw new BookingException("This flight has been cancelled.",false,true);
                    if (Convert.ToDateTime(ft.Rows[0]["DepartureTime"]) <=
                    Convert.ToDateTime(ft.Rows[0]["Now"]).AddHours(1))
                    throw new BookingException("Online booking closes 1 hour before departure.",false,true);

                    FlightInfo fresh = FlightEntity.GetInfoTx(con, tx, r.Flight.FCode);
                    ValidateTravellers(r, fresh);
                    quote = PricingService.Quote(fresh, r.Cabin, r.Travellers);
                    PriceQuote displayed = PricingService.Quote(r.Flight, r.Cabin, r.Travellers);
                    if (quote.Total != displayed.Total || fresh.Departure != r.Flight.Departure ||
                    fresh.Src != r.Flight.Src || fresh.Dest != r.Flight.Dest || fresh.Arrival!=r.Flight.Arrival ||
                    fresh.BusinessSeats!=r.Flight.BusinessSeats || fresh.EconomySeats!=r.Flight.EconomySeats)
                    throw new BookingException(
                    "The flight schedule or fare changed. Please search again and review the new details.",false,true);
                    string pnr = NewPnr(con, tx);
                    int bookingId = Convert.ToInt32(Db.ScalarTx(
                    con, tx, @"
                        INSERT INTO dbo.BookingTbl
                            (PNR, UserId, FCode, CabinClass, ContactEmail, ContactPhone, BaseFare, AddOns, Taxes, TotalAmount)
                        OUTPUT INSERTED.BookingId
                        VALUES (@pnr, @user, @f, @cabin, @email, @phone, @base, @addons, @tax, @total)",
                    Db.P("@pnr", pnr), Db.P("@user", r.UserId), Db.P("@f", r.Flight.FCode), Db.P("@cabin", r.Cabin),
                    Db.P("@email", r.ContactEmail.Trim()), Db.P("@phone", r.ContactPhone.Trim()),
                    Db.P("@base", quote.BaseFare), Db.P("@addons", quote.AddOns), Db.P("@tax", quote.Taxes),
                    Db.P("@total", quote.Total)));

                    for (int i = 0; i < r.Travellers.Count; i++)
                    {
                        TravellerInput t = r.Travellers[i];
                        try
                        {
                            Db.ExecuteTx(con, tx, @"
                                INSERT INTO dbo.TicketTbl
                                    (BookingId, FCode, Name, Age, Gender, PassportNo, CabinClass, SeatNo, ExtraBaggageKg, Amount)
                                VALUES (@b, @f, @name, @age, @gender, @passport, @cabin, @seat, @bag, @amount)",
                            Db.P("@b", bookingId), Db.P("@f", r.Flight.FCode),
                            Db.P("@name", t.Name.Trim()), Db.P("@age", t.AgeOn(r.Flight.Departure)),
                            Db.P("@gender", t.Gender),
                            Db.P("@passport", string.IsNullOrWhiteSpace(t.PassportNo)
                            ? null
                            : t.PassportNo.Trim().ToUpperInvariant()),
                            Db.P("@cabin", r.Cabin), Db.P("@seat", t.SeatNo.ToUpperInvariant()),
                            Db.P("@bag", t.BaggageKg), Db.P("@amount", quote.Lines[i].Total));
                        }
                        catch (SqlException ex)
                        {
                            if (Db.IsDuplicateKey(ex))
                            {
                                throw new BookingException(
                                "Seat " + t.SeatNo +
                                " was just taken by another passenger. Please pick a different seat.",
                                true);
                            }
                            throw;
                        }
                    }

                    // Charge the card last: if it is declined the whole transaction rolls back and no seat is lost.
                    PaymentResult pay = Payments.Gateway.Charge(r.Card, quote.Total, "Booking " + pnr);
                    if (!pay.Approved)
                    throw new BookingException(pay.Message ?? "Payment was not approved.");

                    Db.ExecuteTx(con, tx, @"
                        INSERT INTO dbo.PaymentTbl (BookingId, PaymentType, CardBrand, CardLast4, Amount, GatewayRef)
                        VALUES (@b, N'Charge', @brand, @last4, @amount, @ref)",
                    Db.P("@b", bookingId), Db.P("@brand", r.Card.Brand), Db.P("@last4", r.Card.Last4),
                    Db.P("@amount", quote.Total), Db.P("@ref", pay.Reference));

                    return new BookingResult { BookingId = bookingId, Pnr = pnr, Total = quote.Total,
                        PaymentReference = pay.Reference };
                });
            }
            catch (SqlException ex)
            {
                if (Db.IsDuplicateKey(ex))
                throw new BookingException(
                "Your booking could not be saved because of a conflict. Please try again.");
                throw;
            }
        }

        private static void ValidateRequest(BookingRequest r)
        {
            if (r == null || r.Flight == null)
            throw new BookingException("No flight selected.");
            if (r.Cabin != Cabin.Economy && r.Cabin != Cabin.Business)
            throw new BookingException("Choose a cabin class.");
            if (r.Travellers == null || r.Travellers.Count < 1 || r.Travellers.Count > MaxTravellers)
            throw new BookingException("A booking needs between 1 and " + MaxTravellers + " travellers.");
            if (!Validator.IsEmail(r.ContactEmail))
            throw new BookingException("Enter a valid contact e-mail address.");
            if (!Validator.IsPhone(r.ContactPhone))
            throw new BookingException("Enter a valid contact phone number.");
            if (r.Card == null)
            throw new BookingException("Payment details are missing.");
            if (!Validator.IsPersonName(r.Card.HolderName) || !CardValidator.PassesLuhn(r.Card.Number) ||
            !CardValidator.IsExpiryValid(r.Card.ExpiryMonth, r.Card.ExpiryYear, DateTime.Today) ||
            !CardValidator.IsCvvValid(r.Card.Cvv, r.Card.Brand))
            throw new BookingException("Check the card holder, number, expiry and CVV.");
            ValidateTravellers(r, r.Flight);
        }

        private static void ValidateTravellers(BookingRequest r, FlightInfo flight)
        {

            HashSet<string> seats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            HashSet<string> passports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (TravellerInput t in r.Travellers)
            {
                if (t == null)
                throw new BookingException("Traveller details are missing.");
                int age = t.AgeOn(flight.Departure);
                if (age < 2 || age > 120)
                throw new BookingException("Travellers must be between 2 and 120 on departure.");
                if (Array.IndexOf(PricingService.BaggageOptions, t.BaggageKg) < 0)
                throw new BookingException("Choose a valid baggage allowance.");
                if (t.Gender != "Male" && t.Gender != "Female" && t.Gender != "Other")
                throw new BookingException("Choose a traveller gender.");
                if (!Validator.IsPersonName(t.Name))
                throw new BookingException("Enter the traveller's name exactly as on the passport.");
                if (flight.IsInternational && !Validator.IsPassport(t.PassportNo))
                throw new BookingException("A passport number is required for international flights (" + t.Name +
                ").");
                if(!string.IsNullOrWhiteSpace(t.PassportNo)) {
                    if(!Validator.IsPassport(t.PassportNo)) throw new BookingException("Check the traveller's passport/ID.");
                    if(!passports.Add(t.PassportNo.Trim())) throw new BookingException("Travellers cannot share a passport number.");
                }
                if (!SeatLayout.IsValid(flight, r.Cabin, t.SeatNo))
                throw new BookingException("Seat '" + t.SeatNo + "' is not in the " + r.Cabin + " cabin.");
                if (!seats.Add(t.SeatNo))
                throw new BookingException("Two travellers cannot share seat " + t.SeatNo + ".");
            }
        }

        private static string NewPnr(SqlConnection con, SqlTransaction tx)
        {
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                for (int attempt = 0; attempt < 20; attempt++)
                {
                    byte[] bytes = new byte[6];
                    rng.GetBytes(bytes);
                    char[] chars = new char[6];
                    for (int i = 0; i < 6; i++)
                    chars[i] = PnrAlphabet[bytes[i] % PnrAlphabet.Length];
                    string pnr = new string(chars);

                    if (Db.ScalarTx(con, tx, "SELECT 1 FROM dbo.BookingTbl WHERE PNR = @p", Db.P("@p", pnr)) == null)
                    {
                        return pnr;
                    }
                }
            }
            throw new BookingException("Could not generate a booking reference. Please try again.");
        }

        // ----------------------------------------------------------- reads

        public static DataTable GetUserBookings(int userId)
        {
            Session.RequireUser(userId);
            return Db.Query(BookingListSql + " WHERE b.UserId = @u ORDER BY f.DepartureTime DESC", Db.P("@u", userId));
        }

        private const string BookingListSql = @"
            SELECT b.BookingId, b.PNR, f.FlightNo, f.Fsrc, f.FDest, f.DepartureTime,
                   b.CabinClass, (SELECT COUNT(*) FROM dbo.TicketTbl t WHERE t.BookingId = b.BookingId) AS Pax,
                   b.TotalAmount, b.BookingStatus, b.RefundAmount, b.BookedAt, b.FCode, SYSDATETIME() AS ServerNow
            FROM   dbo.BookingTbl b
            JOIN   dbo.FlightTbl  f ON f.FCode = b.FCode";

        public static DataTable GetTicketsForBooking(int bookingId)
        {
            RequireBookingAccess(bookingId);
            return Db.Query(@"
                SELECT TicketId, TicketNo, Name, Age, SeatNo, CabinClass, ExtraBaggageKg, Amount, TicketStatus
                FROM   dbo.TicketTbl WHERE BookingId = @b ORDER BY TicketId",
            Db.P("@b", bookingId));
        }

        public static List<TicketData> GetTicketData(int bookingId)
        {
            RequireBookingAccess(bookingId);
            DataTable t = Db.Query(@"
                SELECT b.PNR, t.TicketNo, t.Name, t.Gender, f.Airline, f.FlightNo, f.Fsrc, f.FDest,
                       a1.City AS SrcCity, a2.City AS DestCity, f.DepartureTime, f.ArrivalTime,
                       t.SeatNo, t.CabinClass, t.ExtraBaggageKg, t.TicketStatus, b.BookedAt
                FROM   dbo.TicketTbl t
                JOIN   dbo.BookingTbl b ON b.BookingId = t.BookingId
                JOIN   dbo.FlightTbl  f ON f.FCode = t.FCode
                JOIN   dbo.AirportTbl a1 ON a1.Code = f.Fsrc
                JOIN   dbo.AirportTbl a2 ON a2.Code = f.FDest
                WHERE  t.BookingId = @b ORDER BY t.TicketId",
            Db.P("@b", bookingId));

            List<TicketData> list = new List<TicketData>();
            foreach (DataRow r in t.Rows)
            {
                list.Add(new TicketData {
                    Pnr = Convert.ToString(r["PNR"]).Trim(), TicketNo = Convert.ToString(r["TicketNo"]),
                    PassengerName = Convert.ToString(r["Name"]), Gender = Convert.ToString(r["Gender"]),
                    Airline = Convert.ToString(r["Airline"]), FlightNo = Convert.ToString(r["FlightNo"]),
                    Src = Convert.ToString(r["Fsrc"]).Trim(), Dest = Convert.ToString(r["FDest"]).Trim(),
                    SrcCity = Convert.ToString(r["SrcCity"]), DestCity = Convert.ToString(r["DestCity"]),
                    Departure = Convert.ToDateTime(r["DepartureTime"]), Arrival = Convert.ToDateTime(r["ArrivalTime"]),
                    Seat = Convert.ToString(r["SeatNo"]), Cabin = Convert.ToString(r["CabinClass"]),
                    BaggageKg = Convert.ToInt32(r["ExtraBaggageKg"]), Status = Convert.ToString(r["TicketStatus"]),
                    BookedAt = Convert.ToDateTime(r["BookedAt"])
                });
            }
            return list;
        }

        // ---------------------------------------------------------- cancel

        /// <summary>What a cancellation would refund right now (nothing is changed).</summary>
        public static RefundQuote PreviewRefund(int bookingId, bool byAdmin)
        {
            RequireBookingAccess(bookingId);
            if (byAdmin)
            Session.RequireAdmin();
            DataTable t = Db.Query(@"
                SELECT f.DepartureTime, b.TotalAmount, b.BookingStatus, SYSDATETIME() AS Now
                FROM dbo.BookingTbl b JOIN dbo.FlightTbl f ON f.FCode = b.FCode
                WHERE b.BookingId = @b",
            Db.P("@b", bookingId));
            if (t.Rows.Count == 0)
            throw new BookingException("Booking not found.");
            if (Convert.ToString(t.Rows[0]["BookingStatus"]) == "Cancelled")
            throw new BookingException("This booking is already cancelled.");

            return RefundPolicy.Calculate(Convert.ToDateTime(t.Rows[0]["DepartureTime"]),
            Convert.ToDateTime(t.Rows[0]["Now"]),
            Convert.ToDecimal(t.Rows[0]["TotalAmount"]), byAdmin);
        }

        /// <summary>Cancels the whole booking, frees the seats and records the refund.</summary>
        public static CancelResult Cancel(int bookingId, int requestingUserId, bool byAdmin, decimal? expectedRefund=null)
        {
            Session.RequireUser(requestingUserId);
            if (byAdmin)
            Session.RequireAdmin();
            RequireBookingAccess(bookingId);
            return Db.InTransaction(delegate(SqlConnection con, SqlTransaction tx) {
                int flightId = Convert.ToInt32(
                Db.ScalarTx(con, tx, "SELECT FCode FROM dbo.BookingTbl WHERE BookingId=@b", Db.P("@b", bookingId)));
                FlightEntity.GetInfoTx(con, tx, flightId);
                return CancelTx(con, tx, bookingId, requestingUserId, byAdmin,expectedRefund);
            });
        }

        private static CancelResult CancelTx(SqlConnection con, SqlTransaction tx, int bookingId, int requestingUserId,
        bool byAdmin,decimal? expectedRefund=null)
        {
            DataTable t = Db.QueryTx(con, tx, @"
                    SELECT b.UserId, b.TotalAmount, b.BookingStatus, f.DepartureTime, SYSDATETIME() AS Now
                    FROM dbo.BookingTbl b WITH (UPDLOCK, ROWLOCK)
                    JOIN dbo.FlightTbl f ON f.FCode = b.FCode
                    WHERE b.BookingId = @b",
            Db.P("@b", bookingId));

            if (t.Rows.Count == 0)
            throw new BookingException("Booking not found.");
            DataRow row = t.Rows[0];
            if (!byAdmin && Convert.ToInt32(row["UserId"]) != requestingUserId)
            throw new BookingException("You can only cancel your own bookings.");
            if (Convert.ToString(row["BookingStatus"]) == "Cancelled")
            throw new BookingException("This booking is already cancelled.");

            RefundQuote quote =
            RefundPolicy.Calculate(Convert.ToDateTime(row["DepartureTime"]), Convert.ToDateTime(row["Now"]),
            Convert.ToDecimal(row["TotalAmount"]), byAdmin);
            if (!quote.Allowed)
            throw new BookingException(quote.Rule);

            if(expectedRefund.HasValue && expectedRefund.Value!=quote.Amount)
            throw new BookingException("The refund changed while you were reviewing it. Please review cancellation again.");
            string refundRef = null;
            if (quote.Amount > 0)
            {
                DataTable charge =
                Db.QueryTx(con, tx,
                "SELECT TOP 1 GatewayRef, CardBrand, CardLast4 FROM dbo.PaymentTbl WHERE " +
                "BookingId = @b AND PaymentType = N'Charge' ORDER BY PaymentId",
                Db.P("@b", bookingId));
                if(charge.Rows.Count!=1) throw new BookingException("No original charge was found. Contact the administrator.");
                string chargeRef =
                charge.Rows.Count > 0 ? Convert.ToString(charge.Rows[0]["GatewayRef"]) : string.Empty;

                PaymentResult refund = Payments.Gateway.Refund(chargeRef, quote.Amount);
                if (!refund.Approved)
                throw new BookingException("The refund could not be issued: " + refund.Message);
                refundRef = refund.Reference;

                Db.ExecuteTx(con, tx, @"
                        INSERT INTO dbo.PaymentTbl (BookingId, PaymentType, CardBrand, CardLast4, Amount, GatewayRef)
                        VALUES (@b, N'Refund', @brand, @last4, @amount, @ref)",
                Db.P("@b", bookingId),
                Db.P("@brand", charge.Rows.Count > 0 ? charge.Rows[0]["CardBrand"] : null),
                Db.P("@last4", charge.Rows.Count > 0 ? charge.Rows[0]["CardLast4"] : null),
                Db.P("@amount", quote.Amount), Db.P("@ref", refundRef));
            }

            Db.ExecuteTx(con, tx, "UPDATE dbo.TicketTbl SET TicketStatus = N'Cancelled' WHERE BookingId = @b",
            Db.P("@b", bookingId));
            Db.ExecuteTx(con, tx, @"UPDATE dbo.BookingTbl
                                        SET BookingStatus = N'Cancelled', CancelledAt = SYSDATETIME(), RefundAmount = @r
                                        WHERE BookingId = @b",
            Db.P("@r", quote.Amount), Db.P("@b", bookingId));

            return new CancelResult { Refund = quote.Amount, Percent = quote.Percent, Reference = refundRef };
        }

        private static void RequireBookingAccess(int id)
        {
            object owner = Db.Scalar("SELECT UserId FROM dbo.BookingTbl WHERE BookingId=@b", Db.P("@b", id));
            if (owner == null)
            throw new BookingException("Booking not found.");
            Session.RequireUser(Convert.ToInt32(owner));
        }

        public static int CancelFlight(int fcode, int adminUserId)
        {
            Session.RequireAdmin();
            Session.RequireUser(adminUserId);
            return Db.InTransaction(delegate(SqlConnection con, SqlTransaction tx) {
                FlightInfo f = FlightEntity.GetInfoTx(con, tx, fcode);
                if (f.Status == "Cancelled")
                throw new BookingException("This flight is already cancelled.");
                Db.ExecuteTx(con, tx, "UPDATE dbo.FlightTbl SET FStatus=N'Cancelled' WHERE FCode=@f",
                Db.P("@f", fcode));
                DataTable bookings = Db.QueryTx(con, tx,
                "SELECT BookingId FROM dbo.BookingTbl WHERE FCode=@f AND " +
                "BookingStatus=N'Confirmed' ORDER BY BookingId",
                Db.P("@f", fcode));
                foreach (DataRow x in bookings.Rows)
                CancelTx(con, tx, Convert.ToInt32(x[0]), adminUserId, true);
                return bookings.Rows.Count;
            });
        }
    }
}
