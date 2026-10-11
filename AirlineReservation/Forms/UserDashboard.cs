using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Passenger home: trip summary and the next upcoming flight.</summary>
    internal class UserDashboard : PassengerShell
    {
        private readonly StatCard upcomingCard = new StatCard("Upcoming trips", Theme.Accent);
        private readonly StatCard bookingsCard = new StatCard("Total bookings", Theme.Orange);
        private readonly StatCard spentCard = new StatCard("Net spent", Theme.Success);
        private readonly Panel nextTripHost = UiKit.MakeCard();

        public UserDashboard() : base("Dashboard", "Welcome back, " + FirstName(), "dashboard")
        {
            SuspendLayout();

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.Background };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 112));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            TableLayoutPanel stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Theme.Background };
            for (int i = 0; i < 3; i++)
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            foreach (StatCard c in new[] { upcomingCard, bookingsCard, spentCard })
            {
                c.Dock = DockStyle.Fill;
                stats.Controls.Add(c);
            }

            nextTripHost.Dock = DockStyle.Fill;

            layout.Controls.Add(stats, 0, 0);
            layout.Controls.Add(nextTripHost, 0, 2);
            Body.Controls.Add(layout);
            ResumeLayout(false);

            Shown += delegate
            {
                LoadData();
            };
        }

        private static string FirstName()
        {
            if (!Session.IsSignedIn)
            return string.Empty;
            string[] parts = Session.Current.FullName.Split(' ');
            return parts[0];
        }

        protected override void OnCurrencyChanged()
        {
            LoadData();
        }
        private void LoadData()
        {
            UiKit.Guard(this, delegate {
                UserSummary summary = ReportService.GetUserSummary(Session.Current.Id);
                upcomingCard.Value = summary.UpcomingTrips.ToString();
                bookingsCard.Value = summary.TotalBookings.ToString();
                spentCard.Value = Money.Format(summary.NetSpent);
                ShowNextTrip(ReportService.GetNextTrip(Session.Current.Id));
            });
        }

        private void ShowNextTrip(DataRow trip)
        {
            nextTripHost.Controls.Clear();
            nextTripHost.Padding = new Padding(28);

            if (trip == null)
            {
                Label none = UiKit.MakeLabel("No upcoming trips", Theme.Title, Theme.Text);
                none.Location = new Point(28, 40);
                Label hint = UiKit.MakeLabel("Search live flights, pick your seat and get your e-ticket in minutes.",
                Theme.Base, Theme.Muted);
                hint.Location = new Point(30, 90);
                Button book = UiKit.MakeButton("Search flights", ButtonKind.Primary, 180,
                delegate { NavigateTo(new PassengerTicketBook()); });
                book.Height = 46;
                book.Location = new Point(30, 140);
                nextTripHost.Controls.AddRange(new Control[] { none, hint, book });
                return;
            }

            DateTime dep = Convert.ToDateTime(trip["DepartureTime"]);
            DateTime arr = Convert.ToDateTime(trip["ArrivalTime"]);
            int bookingId = Convert.ToInt32(trip["BookingId"]);
            TimeSpan until = dep - DateTime.Now;
            string countdown = until.TotalDays >= 1 ? "Departs in " + (int)until.TotalDays + " day" +
            ((int)until.TotalDays == 1 ? "" : "s")
            : "Departs in " + Math.Max(1, (int)until.TotalHours) + " hours";

            Label caption = UiKit.MakeLabel("YOUR NEXT TRIP  \u00B7  " + countdown, Theme.Small, Theme.Orange);
            caption.Font = Theme.Bold;
            caption.Location = new Point(28, 24);

            Label from = UiKit.MakeLabel(Convert.ToString(trip["Fsrc"]).Trim(), Theme.Display, Theme.Navy);
            from.Location = new Point(24, 56);
            Label to = UiKit.MakeLabel(Convert.ToString(trip["FDest"]).Trim(), Theme.Display, Theme.Navy);
            to.Location = new Point(300, 56);
            Label arrow = UiKit.MakeLabel("\u2014\u2014\u2014\u2192", Theme.Subtitle, Theme.Accent);
            arrow.Location = new Point(170, 82);
            Label fromCity = UiKit.MakeLabel(Convert.ToString(trip["SrcCity"]), Theme.Base, Theme.Muted);
            fromCity.Location = new Point(30, 118);
            Label toCity = UiKit.MakeLabel(Convert.ToString(trip["DestCity"]), Theme.Base, Theme.Muted);
            toCity.Location = new Point(306, 118);

            Label facts = UiKit.MakeLabel(dep.ToString("dddd, dd MMMM yyyy") + "\nDeparts " + dep.ToString("HH:mm") +
            "   \u00B7   Arrives " + arr.ToString("HH:mm") + "\nFlight " +
            trip["FlightNo"] + "   \u00B7   " + trip["CabinClass"] +
            "   \u00B7   PNR " + Convert.ToString(trip["PNR"]).Trim(),
            Theme.Base, Theme.Text);
            facts.Location = new Point(30, 164);

            Button view = UiKit.MakeButton("View e-ticket", ButtonKind.Primary, 170, delegate {
                using (TicketView tv = new TicketView(bookingId)) tv.ShowDialog(this);
            });
            view.Location = new Point(30, 250);
            Button manage = UiKit.MakeButton("Manage bookings", ButtonKind.Secondary, 180,
            delegate { NavigateTo(new PassengerTicketCancel(bookingId)); });
            manage.Location = new Point(214, 250);

            nextTripHost.Controls.AddRange(
            new Control[] { caption, from, arrow, to, fromCity, toCity, facts, view, manage });
        }
    }
}
