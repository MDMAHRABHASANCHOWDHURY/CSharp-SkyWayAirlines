using System;
using System.Collections.Generic;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;

namespace AirlineReservationDemo.Services
{
    /// <summary>One traveller entered in the booking wizard.</summary>
    internal sealed class TravellerInput
    {
        public string Name { get; set; }
        public DateTime DateOfBirth { get; set; }
        public string Gender { get; set; }
        public string PassportNo { get; set; }
        public int BaggageKg { get; set; }
        public string SeatNo { get; set; }

        public int AgeOn(DateTime date)
        {
            return Validator.AgeOn(DateOfBirth, date);
        }
    }

    internal sealed class FareLine
    {
        public string Name { get; set; }
        public string FareType { get; set; }
        public decimal BaseFare { get; set; }
        public decimal Baggage { get; set; }
        public decimal Tax { get; set; }
        public decimal Total
        {
            get {
                return BaseFare + Baggage + Tax;
            }
        }
    }

    internal sealed class PriceQuote
    {
        public List<FareLine> Lines { get; private set; }
        public decimal BaseFare { get; set; }
        public decimal AddOns { get; set; }
        public decimal Taxes { get; set; }
        public decimal Total
        {
            get {
                return BaseFare + AddOns + Taxes;
            }
        }

        public PriceQuote()
        {
            Lines = new List<FareLine>();
        }
    }

    /// <summary>
    /// Fare rules. Adults pay the full fare, children (2-11) pay 75 %, infants under 2
    /// are not sold online. Tax = 7.5 % of the fare plus a per-passenger airport fee.
    /// </summary>
    internal static class PricingService
    {
        public const decimal TaxRate = 0.075m;
        public const decimal DomesticAirportFee = 8m;
        public const decimal InternationalAirportFee = 22m;
        public const decimal ChildFareFactor = 0.75m;
        public const int MinimumAge = 2;

        public static readonly int[] BaggageOptions = { 0, 10, 20, 30 };

        public static decimal BaggagePrice(int kg)
        {
            switch (kg)
            {
                case 10:
                return 18m;
                case 20:
                return 32m;
                case 30:
                return 45m;
                case 0:
                return 0m;
                default:
                throw new ArgumentException("Select 0, 10, 20 or 30 kg of extra baggage.");
            }
        }

        public static PriceQuote Quote(FlightInfo flight, string cabin, IList<TravellerInput> travellers)
        {
            if (flight == null)
            throw new ArgumentNullException("flight");
            if (travellers == null || travellers.Count == 0)
            throw new ArgumentException("At least one traveller is required.");

            decimal fare = flight.FareFor(cabin);
            if(fare<0 || fare>1000000m) throw new ArgumentException("The fare is outside the supported range.");
            if(travellers.Count>BookingService.MaxTravellers) throw new ArgumentException("A booking supports at most 9 travellers.");
            decimal airportFee = flight.IsInternational ? InternationalAirportFee : DomesticAirportFee;
            PriceQuote quote = new PriceQuote();

            foreach (TravellerInput t in travellers)
            {
                if(t==null) throw new ArgumentException("Traveller details are missing.");
                int age = t.AgeOn(flight.Departure);
                if(age>120) throw new ArgumentException("Check the traveller's date of birth.");
                if (age < MinimumAge)
                {
                    throw new ArgumentException("Infants under 2 years cannot be booked online (" + t.Name + ").");
                }

                bool child = age < 12;
                decimal baseFare = Round(child ? fare * ChildFareFactor : fare);
                decimal baggage = BaggagePrice(t.BaggageKg);
                decimal tax = Round(baseFare * TaxRate + airportFee);

                quote.Lines.Add(new FareLine { Name = t.Name, FareType = child ? "Child" : "Adult", BaseFare = baseFare,
                    Baggage = baggage, Tax = tax });
                quote.BaseFare += baseFare;
                quote.AddOns += baggage;
                quote.Taxes += tax;
            }
            return quote;
        }

        private static decimal Round(decimal value)
        {
            return Math.Round(value, 2, MidpointRounding.AwayFromZero);
        }
    }
}
