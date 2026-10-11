using System;
using System.Data;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;
namespace AirlineReservationDemo
{
    internal class Flights : AdminListForm
    {
        public Flights() : base("Flights", "Manage schedules, fares and seat capacity", "flights")
        {
            UiKit.AddColumn(Grid, "FlightNo", "Flight", 65, null);
            UiKit.AddColumn(Grid, "Route", "Route", 80, null);
            UiKit.AddColumn(Grid, "DepartureTime", "Departure", 140, "dd MMM yyyy HH:mm");
            UiKit.AddColumn(Grid, "ArrivalTime", "Arrival", 140, "dd MMM yyyy HH:mm");
            UiKit.AddColumn(Grid, "FCap", "Seats", 50, null);
            UiKit.AddColumn(Grid, "EconomyFare", "Economy fare", 80, null);
            UiKit.AddColumn(Grid, "BusinessFare", "Business fare", 80, null);
            UiKit.AddColumn(Grid, "FStatus", "Status", 70, null);
            UiKit.MoneyColumns(Grid, "EconomyFare", "BusinessFare");
            UiKit.ColorStatus(Grid, "FStatus");
            ActionButton("Refresh", ButtonKind.Ghost, delegate { Reload(); });
            ActionButton("Delete", ButtonKind.Danger, delegate { DeleteSelected(); });
            ActionButton("Cancel flight", ButtonKind.Secondary, delegate { CancelSelected(); });
            ActionButton("Edit", ButtonKind.Secondary, delegate { EditSelected(); });
            ActionButton("Add flight", ButtonKind.Primary, delegate {
                UiKit.Guard(this, delegate {
                    using (var f = new FlightEditor(null)) if (f.ShowDialog(this) == DialogResult.OK) Reload();
                });
            });
            Grid.CellDoubleClick += delegate(object s, DataGridViewCellEventArgs e)
            {
                if (e.RowIndex >= 0)
                EditSelected();
            };
        }
        protected override void Reload()
        {
            UiKit.Guard(this, delegate { Grid.DataSource = FlightEntity.List(SearchBox.Text); });
        }
        private void EditSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["FCode"]);
            UiKit.Guard(this, delegate {
                using (var f = new FlightEditor(FlightEntity.GetInfo(id))) if (f.ShowDialog(this) == DialogResult.OK)
                Reload();
            });
        }
        private void DeleteSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["FCode"]);
            if (!UiKit.Confirm(this, "Delete this flight? Flights with booking history cannot be deleted."))
            return;
            if (UiKit.Guard(this, delegate { FlightEntity.Delete(id); }))
            Reload();
        }
        private void CancelSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["FCode"]);
            if (!UiKit.Confirm(this,
            "Cancel this flight and issue a full simulated refund for every confirmed booking?"))
            return;
            int count = 0;
            if (UiKit.Guard(this, delegate { count = BookingService.CancelFlight(id, Session.Current.Id); }))
            {
                UiKit.Info(this, count + " booking(s) refunded. Flight cancelled.");
                Reload();
            }
        }
    }
    internal sealed class FlightEditor : EditorForm
    {
        private readonly int id;
        private readonly TextBox number = UiKit.MakeTextBox(), airline = UiKit.MakeTextBox();
        private readonly ComboBox source = UiKit.MakeCombo(), destination = UiKit.MakeCombo(),
        status = UiKit.MakeCombo("Scheduled", "Delayed");
        private readonly DateTimePicker departure = UiKit.MakeDate("dd MMM yyyy HH:mm"),
        arrival = UiKit.MakeDate("dd MMM yyyy HH:mm");
        private readonly NumericUpDown business = Seats(), economy = Seats(), ecoFare = Fare(), busFare = Fare();
        private static NumericUpDown Seats()
        {
            return new NumericUpDown { Maximum = 600 };
        }
        private static NumericUpDown Fare()
        {
            return new NumericUpDown { Maximum = 1000000, DecimalPlaces = 2, ThousandsSeparator = true };
        }
        public FlightEditor(FlightInfo f) : base(f == null ? "Add flight" : "Edit flight")
        {
            Session.RequireAdmin();
            number.MaxLength=8;airline.MaxLength=60;
            id = f == null ? 0 : f.FCode;
            DataTable airports = FlightEntity.GetAirports();
            source.DataSource = airports.Copy();
            destination.DataSource = airports.Copy();
            foreach (var c in new[] { source, destination })
            {
                c.DisplayMember = "Display";
                c.ValueMember = "Code";
            }
            Field("Flight number", number);
            Field("Airline", airline);
            Field("From", source);
            Field("To", destination);
            Field("Departure (server time)", departure);
            Field("Arrival (server time)", arrival);
            Field("Business seats", business);
            Field("Economy seats", economy);
            Field("Economy fare", ecoFare);
            Field("Business fare", busFare);
            Field("Status", status);
            if (f == null)
            {
                airline.Text = "SkyWay Airlines";
                departure.Value = DateTime.Now.AddDays(7);
                arrival.Value = departure.Value.AddHours(1);
                business.Value = 8;
                economy.Value = 66;
                ecoFare.Value = 42;
                busFare.Value = 105;
                UiKit.SelectByCode(source, "DAC");
                UiKit.SelectByCode(destination, "CGP");
            }
            else
            {
                number.Text = f.FlightNo;
                airline.Text = f.Airline;
                UiKit.SelectByCode(source, f.Src);
                UiKit.SelectByCode(destination, f.Dest);
                departure.Value = f.Departure;
                arrival.Value = f.Arrival;
                business.Value = f.BusinessSeats;
                economy.Value = f.EconomySeats;
                ecoFare.Value = f.EconomyFare;
                busFare.Value = f.BusinessFare;
                status.Text = f.Status;
            }
        }
        protected override void Save()
        {
            FlightEntity.Save(new FlightInfo { FCode = id, FlightNo = number.Text, Airline = airline.Text,
                Src = Convert.ToString(source.SelectedValue).Trim(),
                Dest = Convert.ToString(destination.SelectedValue).Trim(),
                Departure = departure.Value, Arrival = arrival.Value,
                BusinessSeats = (int)business.Value, EconomySeats = (int)economy.Value,
                EconomyFare = ecoFare.Value, BusinessFare = busFare.Value,
                Status = status.Text });
        }
    }
}
