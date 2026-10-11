using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Admin home: business figures, latest bookings and the fullest upcoming flights.</summary>
    internal class AdminDashboard : AdminShell
    {
        private readonly StatCard flightsCard = new StatCard("Upcoming flights", Theme.Accent);
        private readonly StatCard bookingsCard = new StatCard("Confirmed bookings", Theme.Orange);
        private readonly StatCard ticketsCard = new StatCard("Tickets sold", Theme.Navy2);
        private readonly StatCard passengersCard = new StatCard("Passengers", Theme.AccentDark);
        private readonly StatCard revenueCard = new StatCard("Net revenue", Theme.Success);
        private readonly DataGridView recentGrid = new DataGridView();
        private readonly DataGridView fillingGrid = new DataGridView();

        public AdminDashboard() : base("Dashboard", "Sales and operations overview", "dashboard")
        {
            SuspendLayout();

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.Background };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 20));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            TableLayoutPanel stats = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1,
                BackColor = Theme.Background };
            for (int i = 0; i < 5; i++)
            stats.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
            StatCard[] cards = { flightsCard, bookingsCard, ticketsCard, passengersCard, revenueCard };
            foreach (StatCard c in cards)
            {
                c.Dock = DockStyle.Fill;
                c.Margin = new Padding(0, 0, c == revenueCard ? 0 : 14, 0);
                stats.Controls.Add(c);
            }

            TableLayoutPanel grids = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                BackColor = Theme.Background };
            grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 58));
            grids.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 16));
            grids.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 42));

            UiKit.StyleGrid(recentGrid);
            recentGrid.Dock = DockStyle.Fill;
            UiKit.AddColumn(recentGrid, "PNR", "PNR", 55, null);
            UiKit.AddColumn(recentGrid, "Customer", "Customer", 100, null);
            UiKit.AddColumn(recentGrid, "Route", "Route", 70, null);
            UiKit.AddColumn(recentGrid, "CabinClass", "Cabin", 55, null);
            UiKit.AddColumn(recentGrid, "TotalAmount", "Total", 65, null);
            UiKit.AddColumn(recentGrid, "BookingStatus", "Status", 65, null);
            UiKit.MoneyColumns(recentGrid, "TotalAmount");
            UiKit.ColorStatus(recentGrid, "BookingStatus");

            UiKit.StyleGrid(fillingGrid);
            fillingGrid.Dock = DockStyle.Fill;
            UiKit.AddColumn(fillingGrid, "FlightNo", "Flight", 50, null);
            UiKit.AddColumn(fillingGrid, "Route", "Route", 65, null);
            UiKit.AddColumn(fillingGrid, "DepartureTime", "Departs", 80, "dd MMM HH:mm");
            UiKit.AddColumn(fillingGrid, "Seats", "Sold", 50, null);
            UiKit.AddColumn(fillingGrid, "LoadFactor", "Load %", 45, "0.0");

            grids.Controls.Add(TitledCard("Latest bookings", recentGrid), 0, 0);
            grids.Controls.Add(TitledCard("Fullest upcoming flights", fillingGrid), 2, 0);

            layout.Controls.Add(stats, 0, 0);
            layout.Controls.Add(grids, 0, 2);
            Body.Controls.Add(layout);
            ResumeLayout(false);

            Shown += delegate
            {
                LoadData();
            };
        }

        private static Panel TitledCard(string title, Control content)
        {
            Panel card = UiKit.MakeCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(0);
            Label l = UiKit.MakeLabel(title, Theme.Subtitle, Theme.Text);
            l.AutoSize = false;
            l.Dock = DockStyle.Top;
            l.Height = 46;
            l.Padding = new Padding(18, 12, 0, 0);
            card.Controls.Add(content);
            card.Controls.Add(l);
            return card;
        }

        protected override void OnCurrencyChanged()
        {
            LoadData();
        }

        private void LoadData()
        {
            UiKit.Guard(this, delegate
            {
                AdminStats stats = ReportService.GetAdminStats();

                flightsCard.Value =
                    stats.UpcomingFlights.ToString("N0");

                bookingsCard.Value =
                    stats.ConfirmedBookings.ToString("N0");

                ticketsCard.Value =
                    stats.TicketsSold.ToString("N0");

                passengersCard.Value =
                    stats.Passengers.ToString("N0");

                revenueCard.Value =
                    Money.Format(stats.NetRevenue);

                DataTable recent =
                    ReportService.GetRecentBookings(12);

                recent.Columns.Add(
                    "Route",
                    typeof(string),
                    "Fsrc + ' \u2192 ' + FDest");

                recentGrid.DataSource = recent;

                DataTable filling =
                    ReportService.GetFillingFlights(12);

                filling.Columns.Add(
                    "Route",
                    typeof(string),
                    "Fsrc + ' \u2192 ' + FDest");

                filling.Columns.Add(
                    "Seats",
                    typeof(string),
                    "Convert(Sold, 'System.String') + ' / ' + " +
                    "Convert(FCap, 'System.String')");

                fillingGrid.DataSource = filling;

                recentGrid.Refresh();
                fillingGrid.Refresh();
            });
        }
    }
}
