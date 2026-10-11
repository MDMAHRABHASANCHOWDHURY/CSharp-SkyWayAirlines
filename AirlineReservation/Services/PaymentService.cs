using System;
using System.Linq;
using System.Security.Cryptography;

namespace AirlineReservationDemo.Services
{
    internal sealed class CardDetails
    {
        public string HolderName { get; set; }
        public string Number { get; set; } // digits only; never stored
        public int ExpiryMonth { get; set; }
        public int ExpiryYear { get; set; } // four digits
        public string Cvv { get; set; }     // never stored

        public string Brand
        {
            get {
                return CardValidator.Brand(Number);
            }
        }
        public string Last4
        {
            get {
                return string.IsNullOrEmpty(Number) || Number.Length<4 ? string.Empty : Number.Substring(Number.Length - 4);
            }
        }
    }

    internal sealed class PaymentResult
    {
        public bool Approved { get; set; }
        public string Reference { get; set; }
        public string Message { get; set; }
    }

    /// <summary>
    /// Payment provider abstraction. The project ships with a SIMULATED gateway so it can be
    /// demonstrated without a merchant account; to take real payments implement this interface
    /// with a hosted provider (Stripe, SSLCommerz, ...) and assign it to Payments.Gateway.
    /// </summary>
    internal interface IPaymentGateway
    {
        PaymentResult Charge(CardDetails card, decimal amount, string description);
        PaymentResult Refund(string originalReference, decimal amount);
    }

    internal static class Payments
    {
        public static IPaymentGateway Gateway { get; set; } = new SimulatedPaymentGateway();
    }

    /// <summary>
    /// Test gateway. 4242 4242 4242 4242 is approved, 4000 0000 0000 0002 is declined,
    /// 4000 0000 0000 9995 fails with insufficient funds; any other valid card is approved.
    /// </summary>
    internal sealed class SimulatedPaymentGateway : IPaymentGateway
    {
        public PaymentResult Charge(CardDetails card, decimal amount, string description)
        {
            if (amount <= 0 || card==null || !CardValidator.PassesLuhn(card.Number))
            return new PaymentResult { Approved = false, Message = "Invalid amount." };

            if (card.Number == "4000000000000002")
            return new PaymentResult { Approved = false, Message = "Your card was declined by the bank." };
            if (card.Number == "4000000000009995")
            return new PaymentResult { Approved = false, Message = "Your card has insufficient funds." };

            return new PaymentResult { Approved = true, Reference = NewReference("SIM-CH"), Message = "Approved" };
        }

        public PaymentResult Refund(string originalReference, decimal amount)
        {
            if(amount<=0 || string.IsNullOrWhiteSpace(originalReference))
            return new PaymentResult { Approved=false,Message="A valid charge reference and positive refund are required." };
            return new PaymentResult { Approved = true, Reference = NewReference("SIM-RF"), Message = "Refund issued" };
        }

        private static string NewReference(string prefix)
        {
            byte[] bytes = new byte[5];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(bytes);
            }
            return prefix + "-" + BitConverter.ToString(bytes).Replace("-", string.Empty);
        }
    }

    /// <summary>Card number / expiry / CVV checks done before anything is sent to the gateway.</summary>
    internal static class CardValidator
    {
        public static string Normalize(string number)
        {
            return new string((number ?? string.Empty).Where(c=>c>='0' && c<='9').ToArray());
        }

        /// <summary>Luhn checksum.</summary>
        public static bool PassesLuhn(string digits)
        {
            if (string.IsNullOrEmpty(digits) || digits.Length < 13 || digits.Length > 19)
            return false;
            int sum = 0;
            bool anyNonZero=false;
            bool alternate = false;
            for (int i = digits.Length - 1; i >= 0; i--)
            {
                if (digits[i]<'0' || digits[i]>'9')
                return false;
                int n = digits[i] - '0';
                if(n!=0) anyNonZero=true;
                if (alternate)
                {
                    n *= 2;
                    if (n > 9)
                    n -= 9;
                }
                sum += n;
                alternate = !alternate;
            }
            return anyNonZero && sum % 10 == 0;
        }

        public static string Brand(string digits)
        {
            if (string.IsNullOrEmpty(digits))
            return "Card";
            if (digits[0] == '4')
            return "Visa";
            if (digits.StartsWith("34") || digits.StartsWith("37"))
            return "Amex";
            int two;
            if (digits.Length >= 2 && int.TryParse(digits.Substring(0, 2), out two) && two >= 51 && two <= 55)
            return "Mastercard";
            int four;
            if(digits.Length>=4 && int.TryParse(digits.Substring(0,4),out four) && four>=2221 && four<=2720) return "Mastercard";
            return "Card";
        }

        public static bool IsExpiryValid(int month, int year, DateTime now)
        {
            if(month<1 || month>12 || year<1 || year>9999 || year<now.Year || year>Math.Min(9999,now.Year+20)) return false;
            return year>now.Year || month>=now.Month;
        }

        public static bool IsCvvValid(string cvv, string brand)
        {
            if (string.IsNullOrEmpty(cvv) || !cvv.All(c=>c>='0' && c<='9'))
            return false;
            return brand == "Amex" ? cvv.Length == 4 : cvv.Length == 3;
        }
    }
}
