using System;
using System.Collections.Generic;
using System.Linq;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;

static class T
{
    static int pass, fail;
    static void Check(string name, bool ok)
    {
        if (ok)
        pass++;
        else
        {
            fail++;
            Console.WriteLine("FAIL: " + name);
        }
    }

    static FlightInfo Flight(string sc, string dc, DateTime dep)
    {
        return new FlightInfo { FCode = 1,          FlightNo = "SW101",        SrcCountry = sc,   DestCountry = dc,
            Departure = dep,    Arrival = dep.AddHours(1), BusinessSeats = 8, EconomySeats = 66,
            EconomyFare = 100m, BusinessFare = 250m,       Src = "DAC",       Dest = "CGP" };
    }

    static void Main()
    {
        Session.End();
        Action[] securedActions = {
            () => { AccountService.Profile(); },
            () => { AccountService.Accounts(); },
            () => { AccountService.MyRequests(); },
            () => AccountService.RequestReset("Please reset my password"),
            () => AccountService.ResetPassword(1,"Temporary123"),
            () => AccountService.RejectRequest(1),
            () => AccountService.ChangeOwnPassword("old","NewPassword123")
        };
        foreach (Action action in securedActions)
        {
            bool accessDenied=false;
            try { action(); } catch(UnauthorizedAccessException) { accessDenied=true; }
            Check("account operations require authentication",accessDenied);
        }
        // password hashing
        string h = PasswordHasher.Hash("Secret123");
        Check("hash verifies", PasswordHasher.Verify("Secret123", h));
        Check("wrong pw rejected", !PasswordHasher.Verify("secret123", h));
        Check("hash is salted", PasswordHasher.Hash("Secret123") != h);
        Check("garbage hash safe", !PasswordHasher.Verify("x", "notahash"));

        // Luhn / cards
        Check("visa test ok", CardValidator.PassesLuhn("4242424242424242"));
        Check("bad luhn", !CardValidator.PassesLuhn("4242424242424241"));
        Check("amex ok", CardValidator.PassesLuhn("378282246310005"));
        Check("brand visa", CardValidator.Brand("4242424242424242") == "Visa");
        Check("brand mc", CardValidator.Brand("5555555555554444") == "Mastercard");
        Check("brand amex", CardValidator.Brand("378282246310005") == "Amex");
        Check("normalize", CardValidator.Normalize("4242 4242-4242 4242") == "4242424242424242");
        Check("expiry future", CardValidator.IsExpiryValid(12, 2030, new DateTime(2026, 10, 11)));
        Check("expiry this month ok", CardValidator.IsExpiryValid(10, 2026, new DateTime(2026, 10, 11)));
        Check("expiry past", !CardValidator.IsExpiryValid(9, 2026, new DateTime(2026, 10, 11)));
        Check("cvv", CardValidator.IsCvvValid("123", "Visa") && !CardValidator.IsCvvValid("1234", "Visa") &&
        CardValidator.IsCvvValid("1234", "Amex"));
        var gw = new SimulatedPaymentGateway();
        Check("gw approve", gw.Charge(new CardDetails { Number = "4242424242424242" }, 10m, "x").Approved);
        Check("gw decline", !gw.Charge(new CardDetails { Number = "4000000000000002" }, 10m, "x").Approved);

        Check("expiry zero year", !CardValidator.IsExpiryValid(12, 0, new DateTime(2026, 10, 11)));
        Check("expiry excessive year", !CardValidator.IsExpiryValid(12, 10000, new DateTime(2026, 10, 11)));
        Check("expiry invalid month", !CardValidator.IsExpiryValid(13, 2027, new DateTime(2026, 10, 11)));
        Check("zero card rejected", !CardValidator.PassesLuhn("0000000000000000"));
        Check("null card rejected", !gw.Charge(null, 10m, "x").Approved);
        Check("zero charge rejected", !gw.Charge(new CardDetails { Number = "4242424242424242" }, 0m, "x").Approved);

        // pricing
        DateTime dep = new DateTime(2026, 12, 1, 10, 0, 0);
        var trav = new List<TravellerInput> {
            new TravellerInput { Name = "Adult One", DateOfBirth = new DateTime(1990, 1, 1), BaggageKg = 20 },
            new TravellerInput { Name = "Kid Two", DateOfBirth = new DateTime(2018, 6, 1), BaggageKg = 0 }
        };
        PriceQuote q = PricingService.Quote(Flight("BD", "BD", dep), Cabin.Economy, trav);
        // adult: 100 base, tax 7.5+8=15.5, bag 32 ; child: 75, tax 5.625->5.63+8=13.63
        Check("adult base", q.Lines[0].BaseFare == 100m);
        Check("child base", q.Lines[1].BaseFare == 75m);
        Check("adult tax", q.Lines[0].Tax == 15.5m);
        Check("child tax", q.Lines[1].Tax == 13.63m);
        Check("addons", q.AddOns == 32m);
        Check("total", q.Total == 100m + 75m + 32m + 15.5m + 13.63m);
        PriceQuote qi = PricingService.Quote(Flight("BD", "AE", dep), Cabin.Business, trav.Take(1).ToList());
        Check("intl fee", qi.Lines[0].Tax == Math.Round(250m * 0.075m + 22m, 2));
        bool threw = false;
        try
        {
            PricingService.Quote(Flight("BD", "BD", dep), Cabin.Economy,
            new List<TravellerInput> {
                new TravellerInput { Name = "Baby", DateOfBirth = new DateTime(2026, 1, 1) }
            });
        }
        catch (ArgumentException)
        {
            threw = true;
        }
        Check("infant rejected", threw);
        Check("age calc", Validator.AgeOn(new DateTime(2000, 12, 2), new DateTime(2026, 12, 1)) == 25);

        // refund
        DateTime now = new DateTime(2026, 10, 11, 12, 0, 0);
        Check("90%", RefundPolicy.Calculate(now.AddHours(100), now, 200m, false).Amount == 180m);
        Check("50%", RefundPolicy.Calculate(now.AddHours(30), now, 200m, false).Amount == 100m);
        Check("0%", RefundPolicy.Calculate(now.AddHours(5), now, 200m, false).Amount == 0m);
        Check("airline 100%", RefundPolicy.Calculate(now.AddHours(5), now, 200m, true).Amount == 200m);
        Check("departed", !RefundPolicy.Calculate(now.AddHours(-1), now, 200m, false).Allowed);

        // seats
        var seats = SeatLayout.Build(8, 66);
        Check("seat count", seats.Count == 74);
        Check("unique seats", seats.Select(s => s.Number).Distinct().Count() == 74);
        Check("first biz 1A", seats[0].Number == "1A" && seats[0].CabinClass == "Business");
        Check("biz rows 1-2", seats.Where(s => s.CabinClass == "Business").Max(s => s.Row) == 2);
        Check("eco starts row 3", seats.First(s => s.CabinClass == "Economy").Row == 3);
        Check("eco last row partial", seats.Last().Number.Length >= 2);
        Check("valid seat", SeatLayout.IsValid(Flight("BD", "BD", dep), Cabin.Economy, "3A"));
        Check("biz seat not eco", !SeatLayout.IsValid(Flight("BD", "BD", dep), Cabin.Economy, "1A"));
        Check("no B in biz", !SeatLayout.IsValid(Flight("BD", "BD", dep), Cabin.Business, "1B"));
        Check("max seat id len", SeatLayout.Build(24, 180).All(s => s.Number.Length <= 4));

        // barcode patterns
        foreach (var kv in Barcode39.AllPatterns)
        Check("code39 pattern " + kv.Key, kv.Value.Length == 9 && kv.Value.Count(c => c == '1') == 3);
        Check("43+1 chars", Barcode39.AllPatterns.Count == 44);
        Check("patterns unique", Barcode39.AllPatterns.Values.Distinct().Count() == 44);

        // validators
        Check("email", Validator.IsEmail("a@b.co") && !Validator.IsEmail("a@b") && !Validator.IsEmail("a b@c.com"));
        Check("phone", Validator.IsPhone("+8801712345678") && !Validator.IsPhone("abc"));
        Check("name", Validator.IsPersonName("Md. Rafi O'Neil") && !Validator.IsPersonName("R2D2") &&
        !Validator.IsPersonName("A"));
        Check("passport", Validator.IsPassport("A01234567") && !Validator.IsPassport("12"));
        Check("password", Validator.IsStrongPassword("abcdef12") && !Validator.IsStrongPassword("abcdefgh") &&
        !Validator.IsStrongPassword("a1"));
        Check("flightno", Validator.IsFlightNumber("sw101") && !Validator.IsFlightNumber("SWX"));

        Check("malformed base64 rejected", !PasswordHasher.Verify("x", "PBKDF2$100000$invalid!$invalid!"));
        Check("airline late full refund", RefundPolicy.Calculate(now.AddHours(-1), now, 100m, true).Amount == 100m);
        Check("72h boundary", RefundPolicy.Calculate(now.AddHours(72), now, 100m, false).Percent == 90);
        Check("24h boundary", RefundPolicy.Calculate(now.AddHours(24), now, 100m, false).Percent == 50);
        Check("CSV quoting", CsvExporter.Escape("a,b\"c") == "\"a,b\"\"c\"");
        Check("CSV formula guarded", CsvExporter.Escape("=1+1").StartsWith("\"'="));

        // SQL batches + script sanity
        string sql = System.IO.File.ReadAllText(
        System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AirlineReservation.sql"));
        var batches = DbInitializer.SplitBatches(sql).ToList();
        Console.WriteLine("SQL batches: " + batches.Count);
        Check("batches > 10", batches.Count > 10);
        Check("no GO left", batches.All(b => !System.Text.RegularExpressions.Regex.IsMatch(
        b, @"^\s*GO\s*$", System.Text.RegularExpressions.RegexOptions.Multiline)));
        Check("like escape", Db.Like("50%_[x]") == "%50[%][_][[]x]%");

        Console.WriteLine("passed " + pass + ", failed " + fail);
        Environment.Exit(fail == 0 ? 0 : 1);
    }
}
