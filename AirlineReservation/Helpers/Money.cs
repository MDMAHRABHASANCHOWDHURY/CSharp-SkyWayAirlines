using System;
using System.Configuration;
using System.Globalization;

namespace AirlineReservationDemo.Helpers
{
    internal static class Money
    {
        private static string currency =
            string.Equals(
                ConfigurationManager.AppSettings["DisplayCurrency"],
                "BDT",
                StringComparison.OrdinalIgnoreCase)
            ? "BDT"
            : "USD";

        public static string Currency
        {
            get { return currency; }
        }

        public static decimal UsdToBdtRate
        {
            get
            {
                decimal rate;

                if (!decimal.TryParse(
                    ConfigurationManager.AppSettings["UsdToBdtRate"],
                    NumberStyles.Number,
                    CultureInfo.InvariantCulture,
                    out rate) || rate <= 0)
                {
                    throw new ConfigurationErrorsException(
                        "UsdToBdtRate must be a positive number in App.config.");
                }

                return rate;
            }
        }

        public static void ToggleCurrency()
        {
            currency = currency == "USD" ? "BDT" : "USD";
        }

        public static decimal ToBdt(decimal usdAmount)
        {
            return Math.Round(
                usdAmount * UsdToBdtRate,
                2,
                MidpointRounding.AwayFromZero);
        }

        public static string FormatUsd(decimal amount)
        {
            return "USD " + amount.ToString(
                "#,##0.00", CultureInfo.InvariantCulture);
        }

        public static string FormatBdt(decimal usdAmount)
        {
            return "BDT " + ToBdt(usdAmount).ToString(
                "#,##0.00", CultureInfo.InvariantCulture);
        }

        public static string Format(decimal usdAmount)
        {
            return currency == "BDT"
                ? FormatBdt(usdAmount)
                : FormatUsd(usdAmount);
        }
    }
}