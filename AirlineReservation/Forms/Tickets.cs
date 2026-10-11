using System;
using System.Data;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.UI;
namespace AirlineReservationDemo
{
    internal class Tickets : AdminListForm
    {
        public Tickets() : base("Bookings & Tickets", "View reservations, e-tickets and simulated refunds", "tickets")
        {
            UiKit.AddColumn(Grid, "PNR", "PNR", 65, null);
            UiKit.AddColumn(Grid, "username", "Account", 80, null);
            UiKit.AddColumn(Grid, "FlightNo", "Flight", 60, null);
            UiKit.AddColumn(Grid, "Route", "Route", 80, null);
            UiKit.AddColumn(Grid, "DepartureTime", "Departure", 130, "dd MMM yyyy HH:mm");
            UiKit.AddColumn(Grid, "Pax", "Travellers", 60, null);
            UiKit.AddColumn(Grid, "TotalAmount", "Paid", 80, null);
            UiKit.AddColumn(Grid, "RefundAmount", "Refund", 80, null);
            UiKit.AddColumn(Grid, "BookingStatus", "Status", 80, null);
            UiKit.MoneyColumns(Grid, "TotalAmount", "RefundAmount");
            UiKit.ColorStatus(Grid, "BookingStatus");
            ActionButton("Refresh", ButtonKind.Ghost, delegate { Reload(); });
            ActionButton("Export CSV", ButtonKind.Secondary, delegate { Export(); });
            ActionButton("Cancel/refund", ButtonKind.Danger, delegate { CancelSelected(); });
            ActionButton("View e-tickets", ButtonKind.Primary, delegate { View(); });
        }
        protected override void Reload()
        {
            UiKit.Guard(this, delegate { Grid.DataSource = TicketEntity.ListBookings(SearchBox.Text); });
        }
        private void View()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["BookingId"]);
            UiKit.Guard(this, delegate { using (var f = new TicketView(id)) f.ShowDialog(this); });
        }
        private void CancelSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["BookingId"]);
            UiKit.Guard(this, delegate {
                var quote = BookingService.PreviewRefund(id, true);
                if (!UiKit.Confirm(this, "Cancel booking " + Selected["PNR"] +
                " and issue a full simulated refund of " + Money.Format(quote.Amount) +
                "?"))
                return;
                var result = BookingService.Cancel(id, Session.Current.Id, true);
                UiKit.Info(this, "Booking cancelled. Refund: " + Money.Format(result.Refund));
                Reload();
            });
        }
        private void Export()
        {
            using (var d = new SaveFileDialog { Filter = "CSV files|*.csv",
                FileName = "bookings.csv" }) if (d.ShowDialog(this) == DialogResult.OK)
            UiKit.Guard(this, delegate {
                CsvExporter.Save((DataTable)Grid.DataSource, d.FileName);
                UiKit.Info(this, "CSV saved.");
            });
        }
    }
}
