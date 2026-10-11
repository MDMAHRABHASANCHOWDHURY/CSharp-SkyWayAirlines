using System;
using AirlineReservationDemo.Entities;

namespace AirlineReservationDemo.UI
{
    /// <summary>Shell with the administrator menu.</summary>
    internal abstract class AdminShell : ShellForm
    {
        protected AdminShell(string title, string subtitle, string activePage) : base(title, subtitle)
        {
            Session.RequireAdmin();
            if(Session.Current.MustChangePassword) throw new UnauthorizedAccessException("Change your temporary password before continuing.");
            AddNav("Account security", delegate { if(activePage!="security") NavigateTo(new AccountSecurity()); }, activePage=="security");
            AddNav("Dashboard", delegate {
                if (activePage != "dashboard")
                NavigateTo(new AdminDashboard());
            }, activePage == "dashboard");
            AddNav("Flights", delegate {
                if (activePage != "flights")
                NavigateTo(new Flights());
            }, activePage == "flights");
            AddNav("Passengers", delegate {
                if (activePage != "passengers")
                NavigateTo(new Passengers());
            }, activePage == "passengers");
            AddNav("Bookings & Tickets", delegate {
                if (activePage != "tickets")
                NavigateTo(new Tickets());
            }, activePage == "tickets");
        }
    }

    /// <summary>Shell with the passenger menu.</summary>
    internal abstract class PassengerShell : ShellForm
    {
        protected PassengerShell(string title, string subtitle, string activePage) : base(title, subtitle)
        {
            if (!Session.IsSignedIn)
            throw new UnauthorizedAccessException("Please sign in.");
            if(Session.Current.MustChangePassword) throw new UnauthorizedAccessException("Change your temporary password before continuing.");
            AddNav("My profile", delegate { if(activePage!="profile") NavigateTo(new MyProfile()); }, activePage=="profile");
            AddNav("Dashboard", delegate {
                if (activePage != "dashboard")
                NavigateTo(new UserDashboard());
            }, activePage == "dashboard");
            AddNav("Book a Flight", delegate {
                if (activePage != "book")
                NavigateTo(new PassengerTicketBook());
            }, activePage == "book");
            AddNav("My Bookings", delegate {
                if (activePage != "bookings")
                NavigateTo(new PassengerTicketCancel());
            }, activePage == "bookings");
        }
    }
}
