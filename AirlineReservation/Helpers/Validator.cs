using System;
using System.Text.RegularExpressions;

namespace AirlineReservationDemo.Helpers
{
    /// <summary>Input validation rules shared by every form.</summary>
    internal static class Validator
    {
        private static readonly Regex EmailPattern = new Regex(@"^[^@\s]+@[^@\s]+\.[^@\s]{2,}$", RegexOptions.Compiled);

        private static readonly Regex PhonePattern =
        new Regex(@"^\+?[0-9][0-9\s\-]{6,18}[0-9]$", RegexOptions.Compiled);

        private static readonly Regex NamePattern = new Regex(@"^[\p{L}][\p{L}\p{Zs}'.\-]{1,98}$", RegexOptions.Compiled);

        private static readonly Regex PassportPattern = new Regex(@"^[A-Za-z0-9]{6,12}$", RegexOptions.Compiled);

        private static readonly Regex UsernamePattern = new Regex(@"^[A-Za-z0-9_.]{3,30}$", RegexOptions.Compiled);

        private static readonly Regex FlightNoPattern = new Regex(@"^[A-Z0-9]{2}[0-9]{1,4}$", RegexOptions.Compiled);

        public static bool IsEmail(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && value.Trim().Length <= 120 && EmailPattern.IsMatch(value.Trim());
        }

        public static bool IsPhone(string value)
        {
            if(string.IsNullOrWhiteSpace(value) || value.Trim().Length > 25 || !PhonePattern.IsMatch(value.Trim())) return false;
            int digits=0;
            foreach(char c in value) if(c>='0' && c<='9') digits++;
            return digits>=8 && digits<=15;
        }

        public static bool IsPersonName(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && NamePattern.IsMatch(value.Trim());
        }

        public static bool IsPassport(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && PassportPattern.IsMatch(value.Trim());
        }

        public static bool IsUsername(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && UsernamePattern.IsMatch(value);
        }

        public static bool IsFlightNumber(string value)
        {
            return !string.IsNullOrWhiteSpace(value) && FlightNoPattern.IsMatch(value.Trim().ToUpperInvariant());
        }

        /// <summary>At least 8 characters with a letter and a digit.</summary>
        public static bool IsStrongPassword(string value)
        {
            if (string.IsNullOrEmpty(value) || value.Length < 8 || value.Length > 128)
            return false;
            bool hasLetter = false, hasDigit = false;
            foreach (char c in value)
            {
                if (char.IsLetter(c))
                hasLetter = true;
                else if (char.IsDigit(c))
                hasDigit = true;
            }
            return hasLetter && hasDigit;
        }

        /// <summary>Whole years between a birth date and a reference date.</summary>
        public static int AgeOn(DateTime dateOfBirth, DateTime reference)
        {
            int age = reference.Year - dateOfBirth.Year;
            if (dateOfBirth.Date > reference.Date.AddYears(-age))
            age--;
            return age;
        }
    }
}
