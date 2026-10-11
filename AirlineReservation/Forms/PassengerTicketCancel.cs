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
    /// <summary>My Bookings: view e-tickets and cancel a booking (with the refund shown first).</summary>
    internal class PassengerTicketCancel : PassengerShell
    {
        private readonly DataGridView bookingsGrid = new DataGridView();
        private readonly DataGridView ticketsGrid = new DataGridView();
        private readonly Label detailTitle =
        UiKit.MakeLabel("Select a booking to see its tickets", Theme.Subtitle, Theme.Text);
        private readonly Label refundHint = UiKit.MakeLabel(string.Empty, Theme.Small, Theme.Muted);
        private readonly Button viewButton;
        private readonly Button cancelButton;
        private readonly int highlightBookingId;

        public PassengerTicketCancel() : this(0)
        {
        }

        public PassengerTicketCancel(int highlightBookingId)
        : base("My Bookings", "Your trips, e-tickets and cancellations", "bookings")
        {
            this.highlightBookingId = highlightBookingId;
            SuspendLayout();

            TableLayoutPanel layout = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 3,
                BackColor = Theme.Background };
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 54));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 16));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 46));

            // ---- bookings list
            Panel listCard = UiKit.MakeCard();
            listCard.Dock = DockStyle.Fill;
            listCard.Padding = new Padding(0);
            UiKit.StyleGrid(bookingsGrid);
            bookingsGrid.Dock = DockStyle.Fill;
            UiKit.AddColumn(bookingsGrid, "PNR", "PNR", 60, null);
            UiKit.AddColumn(bookingsGrid, "FlightNo", "Flight", 60, null);
            UiKit.AddColumn(bookingsGrid, "Route", "Route", 80, null);
            UiKit.AddColumn(bookingsGrid, "DepartureTime", "Departure", 130, "ddd, dd MMM yyyy  HH:mm");
            UiKit.AddColumn(bookingsGrid, "CabinClass", "Cabin", 70, null);
            UiKit.AddColumn(bookingsGrid, "Pax", "Travellers", 60, null);
            UiKit.AddColumn(bookingsGrid, "TotalAmount", "Total", 80, null);
            UiKit.AddColumn(bookingsGrid, "BookingStatus", "Status", 80, null);
            UiKit.MoneyColumns(bookingsGrid, "TotalAmount");
            UiKit.ColorStatus(bookingsGrid, "BookingStatus");
            bookingsGrid.SelectionChanged += delegate
            {
                ShowDetails();
            };
            listCard.Controls.Add(bookingsGrid);

            // ---- details
            Panel detailCard = UiKit.MakeCard();
            detailCard.Dock = DockStyle.Fill;
            detailCard.Padding = new Padding(0);

            Panel detailHeader = new Panel { Dock = DockStyle.Top, Height = 68, BackColor = Color.White };
            detailTitle.Location = new Point(20, 12);
            refundHint.Location = new Point(22, 42);
            cancelButton = UiKit.MakeButton("Cancel booking", ButtonKind.Danger, 160, delegate { CancelSelected(); });
            viewButton = UiKit.MakeButton("View e-ticket", ButtonKind.Primary, 150, delegate { ViewTickets(); });
            foreach (Button b in new[] { cancelButton, viewButton })
            {
                b.Height = 38;
                b.Enabled = false;
                b.Anchor = AnchorStyles.Top | AnchorStyles.Right;
                detailHeader.Controls.Add(b);
            }
            detailHeader.Controls.Add(detailTitle);
            detailHeader.Controls.Add(refundHint);
            detailHeader.Resize += delegate
            {
                cancelButton.Location = new Point(detailHeader.Width - cancelButton.Width - 20, 14);
                viewButton.Location = new Point(cancelButton.Left - viewButton.Width - 10, 14);
                detailTitle.AutoSize=false;detailTitle.AutoEllipsis=true;detailTitle.Size=new Size(Math.Max(80,viewButton.Left-30),28);
                refundHint.AutoSize=false;refundHint.AutoEllipsis=true;refundHint.Size=new Size(Math.Max(80,detailHeader.Width-40),22);
            };

            UiKit.StyleGrid(ticketsGrid);
            ticketsGrid.Dock = DockStyle.Fill;
            UiKit.AddColumn(ticketsGrid, "TicketNo", "E-ticket no.", 90, null);
            UiKit.AddColumn(ticketsGrid, "Name", "Traveller", 110, null);
            UiKit.AddColumn(ticketsGrid, "Age", "Age", 35, null);
            UiKit.AddColumn(ticketsGrid, "SeatNo", "Seat", 40, null);
            UiKit.AddColumn(ticketsGrid, "CabinClass", "Cabin", 60, null);
            UiKit.AddColumn(ticketsGrid, "ExtraBaggageKg", "Extra bag (kg)", 60, null);
            UiKit.AddColumn(ticketsGrid, "Amount", "Amount", 60, null);
            UiKit.AddColumn(ticketsGrid, "TicketStatus", "Status", 60, null);
            UiKit.MoneyColumns(ticketsGrid, "Amount");
            UiKit.ColorStatus(ticketsGrid, "TicketStatus");

            detailCard.Controls.Add(ticketsGrid);
            detailCard.Controls.Add(detailHeader);

            layout.Controls.Add(listCard, 0, 0);
            layout.Controls.Add(detailCard, 0, 2);
            Body.Controls.Add(layout);
            ResumeLayout(false);

            Shown += delegate
            {
                LoadBookings();
            };
        }

        private void LoadBookings()
        {
            UiKit.Guard(this, delegate {
                DataTable t = BookingService.GetUserBookings(Session.Current.Id);
                t.Columns.Add("Route", typeof(string), "Fsrc + ' \u2192 ' + FDest");
                bookingsGrid.DataSource = t;

                bookingsGrid.ClearSelection();
                if (t.Rows.Count == 0)
                {
                    ShowDetails();
                    return;
                }

                int target = 0;
                for (int i = 0; i < t.Rows.Count; i++)
                {
                    if (Convert.ToInt32(t.Rows[i]["BookingId"]) == highlightBookingId)
                    {
                        target = i;
                        break;
                    }
                }
                bookingsGrid.CurrentCell = bookingsGrid.Rows[target].Cells[0];
                bookingsGrid.Rows[target].Selected = true;
                ShowDetails();
            });
        }

        private DataRowView SelectedBooking()
        {
            return bookingsGrid.CurrentRow == null ? null : bookingsGrid.CurrentRow.DataBoundItem as DataRowView;
        }

        private void ShowDetails()
        {
            DataRowView b = SelectedBooking();
            if (b == null)
            {
                detailTitle.Text = "You have no bookings yet";
                refundHint.Text = "Use \"Book a Flight\" to make your first booking.";
                ticketsGrid.DataSource = null;
                viewButton.Enabled = cancelButton.Enabled = false;
                return;
            }

            string status = Convert.ToString(b["BookingStatus"]);
            DateTime dep = Convert.ToDateTime(b["DepartureTime"]);
            decimal total = Convert.ToDecimal(b["TotalAmount"]);
            detailTitle.Text =
            "Booking " + Convert.ToString(b["PNR"]).Trim() + "  \u00B7  " + b["Fsrc"] + " \u2192 " + b["FDest"];

            DateTime serverNow=b.Row.Table.Columns.Contains("ServerNow")?Convert.ToDateTime(b["ServerNow"]):DateTime.Now;
            bool canCancel = status == "Confirmed" && dep > serverNow;
            if (status == "Cancelled")
            {
                object refund = b["RefundAmount"];
                refundHint.Text =
                "Cancelled. Refunded: " + Money.Format(refund == DBNull.Value ? 0m : Convert.ToDecimal(refund));
            }
            else if (!canCancel)
            {
                refundHint.Text = "This flight has departed - it can no longer be cancelled.";
            }
            else
            {
                RefundQuote q = RefundPolicy.Calculate(dep, serverNow, total, false);
                refundHint.Text =
                "If you cancel now: " + Money.Format(q.Amount) + " refund (" + q.Percent + "%). " + q.Rule;
            }

            viewButton.Enabled = true;
            cancelButton.Enabled = canCancel;

            int id = Convert.ToInt32(b["BookingId"]);
            UiKit.Guard(this, delegate { ticketsGrid.DataSource = BookingService.GetTicketsForBooking(id); });
        }

        private void ViewTickets()
        {
            DataRowView b = SelectedBooking();
            if (b == null)
            return;
            int id = Convert.ToInt32(b["BookingId"]);
            UiKit.Guard(this, delegate { using (TicketView view = new TicketView(id)) view.ShowDialog(this); });
        }

        private void CancelSelected()
        {
            DataRowView b = SelectedBooking();
            if (b == null)
            return;
            int id = Convert.ToInt32(b["BookingId"]);
            string pnr = Convert.ToString(b["PNR"]).Trim();

            RefundQuote quote = null;
            if (!UiKit.Guard(this, delegate { quote = BookingService.PreviewRefund(id, false); }))
            return;
            if (!quote.Allowed)
            {
                UiKit.Warn(this, quote.Rule);
                return;
            }

            string message =
            "Cancel booking " + pnr + "?\n\n" + quote.Rule + "\n" +
            (quote.Amount > 0 ? "You will be refunded " + Money.Format(quote.Amount) + " in this demonstration (no real money moves)."
            : "You will not receive a refund.") +
            "\n\nThis cannot be undone.";
            if (!UiKit.Confirm(this, message))
            return;

            CancelResult result = null;
            if (UiKit.Guard(this, delegate { result = BookingService.Cancel(id, Session.Current.Id, false,quote.Amount); }))
            {
                UiKit.Info(this, "Booking " + pnr + " has been cancelled." +
                (result.Refund > 0 ? "\nRefund of " + Money.Format(result.Refund) +
                " issued (ref " + result.Reference + ")."
                : string.Empty));
                LoadBookings();
            }
        }
    }
}
