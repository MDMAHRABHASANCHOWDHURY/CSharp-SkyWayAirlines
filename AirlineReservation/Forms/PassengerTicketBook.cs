using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Flight search. Picking a flight opens the booking wizard.</summary>
    internal class PassengerTicketBook : PassengerShell
    {
        private readonly ComboBox fromCB = UiKit.MakeCombo();
        private readonly ComboBox toCB = UiKit.MakeCombo();
        private readonly DateTimePicker datePicker = UiKit.MakeDate("ddd, dd MMM yyyy");
        private readonly ComboBox cabinCB = UiKit.MakeCombo(Cabin.Economy, Cabin.Business);
        private readonly NumericUpDown paxUpDown =
        new NumericUpDown { Minimum = 1, Maximum = Services.BookingService.MaxTravellers, Value = 1,
            Font = Theme.Base };
        private readonly DataGridView resultsGrid = new DataGridView();
        private readonly Label resultsTitle =
        UiKit.MakeLabel("Search for a route and date to see available flights.", Theme.Subtitle, Theme.Text);
        private readonly Label emptyLabel = UiKit.MakeLabel(string.Empty, Theme.Base, Theme.Muted);
        private readonly Button selectButton;
        private readonly Button prevDay;
        private readonly Button nextDay;

        // The criteria used for the results currently on screen.
        private string searchCabin = Cabin.Economy;
        private int searchPassengers = 1;

        public PassengerTicketBook()
        : base("Book a Flight", "Search schedules and fares · times use server time", "book")
        {
            SuspendLayout();

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.Background };
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 118));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 18));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

            // ---------------- search card
            Panel searchCard = UiKit.MakeCard();
            searchCard.Dock = DockStyle.Fill;
            searchCard.Padding = new Padding(20, 16, 20, 10);

            TableLayoutPanel row =
            new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 7, RowCount = 1, BackColor = Color.White };
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 46));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 22));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 10));
            row.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 124));

            datePicker.MinDate = DateTime.Today;
            datePicker.Value = DateTime.Today.AddDays(7);
            Button swap = UiKit.MakeButton("\u21C4", ButtonKind.Ghost, 38, delegate { SwapAirports(); });
            swap.Font = new Font(Theme.Base.FontFamily, 13f, FontStyle.Bold);
            swap.Height = 32;
            swap.Margin = new Padding(4, 24, 4, 0);
            Button search = UiKit.MakeButton("Search", ButtonKind.Primary, 112, delegate { DoSearch(); });
            search.Margin = new Padding(8, 22, 0, 0);

            row.Controls.Add(Wrap("From", fromCB, new Padding(0, 0, 8, 0)), 0, 0);
            row.Controls.Add(swap, 1, 0);
            row.Controls.Add(Wrap("To", toCB, new Padding(8, 0, 8, 0)), 2, 0);
            row.Controls.Add(Wrap("Departure date", datePicker, new Padding(0, 0, 8, 0)), 3, 0);
            row.Controls.Add(Wrap("Cabin", cabinCB, new Padding(0, 0, 8, 0)), 4, 0);
            row.Controls.Add(Wrap("Travellers", paxUpDown, new Padding(0)), 5, 0);
            row.Controls.Add(search, 6, 0);
            searchCard.Controls.Add(row);

            // ---------------- results card
            Panel resultsCard = UiKit.MakeCard();
            resultsCard.Dock = DockStyle.Fill;
            resultsCard.Padding = new Padding(0);

            Panel resultsHeader = new Panel { Dock = DockStyle.Top, Height = 66, BackColor = Color.White,
                Padding = new Padding(20, 0, 20, 0) };
            resultsTitle.Location = new Point(20, 20);
            resultsTitle.AutoSize = false;
            resultsTitle.AutoEllipsis = true;
            resultsTitle.Height = 28;
            resultsHeader.Controls.Add(resultsTitle);

            nextDay = UiKit.MakeButton("Next day \u203A", ButtonKind.Secondary, 110, delegate { ShiftDay(1); });
            prevDay = UiKit.MakeButton("\u2039 Previous day", ButtonKind.Secondary, 130, delegate { ShiftDay(-1); });
            selectButton = UiKit.MakeButton("Select flight", ButtonKind.Primary, 140, delegate { SelectFlight(); });
            foreach (Button b in new[] { nextDay, prevDay, selectButton })
            {
                b.Height = 36;
                b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                b.Enabled = false;
                resultsHeader.Controls.Add(b);
            }
            resultsHeader.Resize += delegate
            {
                selectButton.Location = new Point(resultsHeader.Width - selectButton.Width - 20, 15);
                nextDay.Location = new Point(selectButton.Left - nextDay.Width - 10, 15);
                prevDay.Location = new Point(nextDay.Left - prevDay.Width - 10, 15);
                resultsTitle.Width = Math.Max(80, prevDay.Left - 30);
            };

            UiKit.StyleGrid(resultsGrid);
            resultsGrid.Dock = DockStyle.Fill;
            UiKit.AddColumn(resultsGrid, "FlightNo", "Flight", 70, null);
            UiKit.AddColumn(resultsGrid, "Route", "Route", 90, null);
            UiKit.AddColumn(resultsGrid, "DepartureTime", "Departs", 70, "HH:mm");
            UiKit.AddColumn(resultsGrid, "ArrivalTime", "Arrives", 70, "HH:mm");
            UiKit.AddColumn(resultsGrid, "Duration", "Duration", 70, null);
            UiKit.AddColumn(resultsGrid, "Fare", "Fare per traveller", 100, null);
            UiKit.AddColumn(resultsGrid, "SeatsLeft", "Seats left", 70, null);
            UiKit.AddColumn(resultsGrid, "FStatus", "Status", 80, null);
            UiKit.MoneyColumns(resultsGrid, "Fare");
            UiKit.ColorStatus(resultsGrid, "FStatus");
            resultsGrid.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0)
                SelectFlight();
            };
            resultsGrid.SelectionChanged += delegate
            {
                selectButton.Enabled = resultsGrid.CurrentRow != null && resultsGrid.Rows.Count > 0;
            };
            resultsGrid.CellFormatting += delegate(object s, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0 || resultsGrid.Columns[e.ColumnIndex].Name != "SeatsLeft" || e.Value == null)
                return;
                int left = Convert.ToInt32(e.Value);
                if (left <= 5)
                {
                    e.CellStyle.ForeColor = Theme.Orange;
                    e.CellStyle.Font = Theme.Bold;
                }
            };

            emptyLabel.AutoSize = false;
            emptyLabel.Dock = DockStyle.Fill;
            emptyLabel.TextAlign = ContentAlignment.MiddleCenter;
            emptyLabel.Text = "Choose where you want to go and press Search.";
            emptyLabel.BackColor = Color.White;

            resultsCard.Controls.Add(emptyLabel);
            resultsCard.Controls.Add(resultsGrid);
            resultsCard.Controls.Add(resultsHeader);
            resultsGrid.Visible = false;

            layout.Controls.Add(searchCard, 0, 0);
            layout.Controls.Add(resultsCard, 0, 2);
            Body.Controls.Add(layout);
            ResumeLayout(false);

            Shown += delegate
            {
                LoadAirports();
            };
        }

        private static Panel Wrap(string caption, Control input, Padding padding)
        {
            Panel p = UiKit.Field(caption, input);
            p.Dock = DockStyle.Fill;
            p.Padding = padding;
            return p;
        }

        private void LoadAirports()
        {
            UiKit.Guard(this, delegate {
                DataTable airports = FlightEntity.GetAirports();
                fromCB.DataSource = airports.Copy();
                fromCB.DisplayMember = "Display";
                fromCB.ValueMember = "Code";
                toCB.DataSource = airports.Copy();
                toCB.DisplayMember = "Display";
                toCB.ValueMember = "Code";
                SelectCode(fromCB, "DAC");
                SelectCode(toCB, "DXB");
            });
        }

        private static void SelectCode(ComboBox combo, string code)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                DataRowView r = combo.Items[i] as DataRowView;
                if (r != null && Convert.ToString(r["Code"]).Trim() == code)
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SwapAirports()
        {
            object from = fromCB.SelectedValue, to = toCB.SelectedValue;
            if (from == null || to == null)
            return;
            fromCB.SelectedValue = to;
            toCB.SelectedValue = from;
        }

        private void ShiftDay(int days)
        {
            DateTime target = datePicker.Value.Date.AddDays(days);
            if (target < datePicker.MinDate)
            return;
            datePicker.Value = target;
            DoSearch();
        }

        private void DoSearch()
        {
            if (fromCB.SelectedValue == null || toCB.SelectedValue == null)
            return;
            string src = Convert.ToString(fromCB.SelectedValue).Trim();
            string dest = Convert.ToString(toCB.SelectedValue).Trim();
            if (src == dest)
            {
                UiKit.Warn(this, "Departure and destination must be different airports.");
                return;
            }

            searchCabin = cabinCB.Text;
            searchPassengers = (int)paxUpDown.Value;
            DateTime date = datePicker.Value.Date;

            UiKit.Guard(this, delegate {
                DataTable results = FlightEntity.Search(src, dest, date, searchCabin, searchPassengers);
                results.Columns.Add("Route", typeof(string), "Fsrc + ' \u2192 ' + FDest");
                results.Columns.Add("Duration", typeof(string));
                foreach (DataRow r in results.Rows)
                {
                    r["Duration"] = FlightInfo.FormatDuration(Convert.ToDateTime(r["ArrivalTime"]) -
                    Convert.ToDateTime(r["DepartureTime"]));
                }

                resultsGrid.DataSource = results;
                bool any = results.Rows.Count > 0;
                resultsGrid.Visible = any;
                emptyLabel.Visible = !any;
                string route = fromCB.Text + "  \u2192  " + toCB.Text;
                resultsTitle.Text = any ? results.Rows.Count + (results.Rows.Count == 1 ? " flight" : " flights") +
                " \u00B7 " + route + " \u00B7 " + date.ToString("ddd, dd MMM yyyy")
                : route;
                emptyLabel.Text = "No " + searchCabin + " flights with " + searchPassengers + " free seat" +
                (searchPassengers == 1 ? "" : "s") + " on " + date.ToString("dd MMM yyyy") +
                ".\nTry another day or cabin.";
                prevDay.Enabled = date > DateTime.Today;
                nextDay.Enabled = true;
                selectButton.Enabled = any;
            });
        }

        private void SelectFlight()
        {
            DataRowView row =
            resultsGrid.CurrentRow == null ? null : resultsGrid.CurrentRow.DataBoundItem as DataRowView;
            if (row == null)
            return;

            FlightInfo flight = null;
            if (!UiKit.Guard(this, delegate { flight = FlightEntity.GetInfo(Convert.ToInt32(row["FCode"])); }))
            return;
            if (flight == null)
            {
                UiKit.Warn(this, "That flight is no longer available.");
                DoSearch();
                return;
            }

            using (BookingWizard wizard = new BookingWizard(flight, searchCabin, searchPassengers))
            {
                DialogResult result = wizard.ShowDialog(this);
                if (wizard.CreatedBookingId > 0)
                {
                    NavigateTo(new PassengerTicketCancel(wizard.CreatedBookingId));
                }
                else if (result == DialogResult.Retry)
                {
                    DoSearch();
                }
                else
                {
                    DoSearch(); // seat counts may have changed
                }
            }
        }
    }
}
