using System;

namespace AirlineReservationDemo.Services
{
    internal sealed class RefundQuote
    {
        public int Percent { get; set; }
        public decimal Amount { get; set; }
        public string Rule { get; set; }
        public bool Allowed { get; set; }
    }

    /// <summary>
    /// Cancellation rules: 72 h or more before departure = 90 % back, 24-72 h = 50 %,
    /// under 24 h = no refund, after departure = cannot cancel.
    /// Cancellations made by the airline / admin are always refunded in full.
    /// </summary>
    internal static class RefundPolicy
    {
        public static RefundQuote Calculate(DateTime departure, DateTime now, decimal amountPaid,
        bool cancelledByAirline)
        {
            if (cancelledByAirline)
            return new RefundQuote { Allowed = true, Percent = 100, Amount = amountPaid,
                Rule = "Cancelled by the airline - full refund." };
            if (departure <= now)
            return new RefundQuote { Allowed = false, Percent = 0, Amount = 0m,
                Rule = "The flight has already departed." };

            double hours = (departure - now).TotalHours;
            int percent;
            string rule;
            if (hours >= 72)
            {
                percent = 90;
                rule = "72 hours or more before departure: 90% refund.";
            }
            else if (hours >= 24)
            {
                percent = 50;
                rule = "24 to 72 hours before departure: 50% refund.";
            }
            else
            {
                percent = 0;
                rule = "Less than 24 hours before departure: no refund.";
            }

            return new RefundQuote { Allowed = true, Percent = percent,
                Amount = Math.Round(amountPaid * percent / 100m, 2, MidpointRounding.AwayFromZero),
                Rule = rule };
        }
    }
}
