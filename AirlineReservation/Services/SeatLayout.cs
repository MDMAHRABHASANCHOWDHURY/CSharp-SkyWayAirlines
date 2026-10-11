using System;
using System.Collections.Generic;
using AirlineReservationDemo.Entities;

namespace AirlineReservationDemo.Services
{
    internal sealed class Seat
    {
        public string Number { get; set; }
        public int Row { get; set; }
        public char Letter { get; set; }
        public string CabinClass { get; set; }
    }

    /// <summary>
    /// Builds the cabin plan from the seat counts stored on the flight.
    /// Business: 2-2 layout (A C | D F). Economy: 3-3 layout (A B C | D E F).
    /// Business rows come first; the last row of a cabin may be partly filled.
    /// </summary>
    internal static class SeatLayout
    {
        public static readonly char[] BusinessLetters = { 'A', 'C', 'D', 'F' };
        public static readonly char[] EconomyLetters = { 'A', 'B', 'C', 'D', 'E', 'F' };

        public static List<Seat> Build(int businessSeats, int economySeats)
        {
            if(businessSeats<0 || economySeats<0 || businessSeats>600 || economySeats>600 || businessSeats+economySeats>600)
            throw new ArgumentException("Seat counts must be non-negative and total at most 600.");
            List<Seat> seats = new List<Seat>();
            int row = 1;
            row = AddCabin(seats, Cabin.Business, BusinessLetters, businessSeats, row);
            AddCabin(seats, Cabin.Economy, EconomyLetters, economySeats, row);
            return seats;
        }

        public static List<Seat> Build(FlightInfo flight)
        {
            if(flight==null) throw new ArgumentNullException("flight");
            return Build(flight.BusinessSeats, flight.EconomySeats);
        }

        public static bool IsValid(FlightInfo flight, string cabin, string seatNumber)
        {
            if (string.IsNullOrEmpty(seatNumber))
            return false;
            foreach (Seat s in Build(flight))
            {
                if (s.CabinClass == cabin && string.Equals(s.Number, seatNumber, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            return false;
        }

        private static int AddCabin(List<Seat> seats, string cabin, char[] letters, int count, int startRow)
        {
            int row = startRow;
            int placed = 0;
            while (placed < count)
            {
                foreach (char letter in letters)
                {
                    if (placed >= count)
                    break;
                    seats.Add(
                    new Seat { Number = row + letter.ToString(), Row = row, Letter = letter, CabinClass = cabin });
                    placed++;
                }
                row++;
            }
            return row;
        }
    }
}
