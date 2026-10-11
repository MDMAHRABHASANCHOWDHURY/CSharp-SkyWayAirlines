using System;
namespace AirlineReservationDemo.Entities
{
    internal static class Cabin
    {
        public const string Economy = "Economy", Business = "Business";
    }
    internal sealed class FlightInfo
    {
        public int FCode { get; set; }
        public string FlightNo { get; set; }
        public string Airline { get; set; }
        public string Src { get; set; }
        public string Dest { get; set; }
        public string SrcCity { get; set; }
        public string DestCity { get; set; }
        public string SrcCountry { get; set; }
        public string DestCountry { get; set; }
        public DateTime Departure { get; set; }
        public DateTime Arrival { get; set; }
        public int BusinessSeats { get; set; }
        public int EconomySeats { get; set; }
        public decimal EconomyFare { get; set; }
        public decimal BusinessFare { get; set; }
        public string Status { get; set; }
        public bool IsInternational
        {
            get {
                return !string.Equals(SrcCountry, DestCountry, StringComparison.OrdinalIgnoreCase);
            }
        }
        public TimeSpan Duration
        {
            get {
                return Arrival - Departure;
            }
        }
        public decimal FareFor(string cabin)
        {
            if (cabin == Cabin.Business)
            return BusinessFare;
            if (cabin == Cabin.Economy)
            return EconomyFare;
            throw new ArgumentException("Choose a valid cabin.");
        }
        public static string FormatDuration(TimeSpan t)
        {
            return ((int)t.TotalHours) + "h " + t.Minutes.ToString("00") + "m";
        }
    }
}
