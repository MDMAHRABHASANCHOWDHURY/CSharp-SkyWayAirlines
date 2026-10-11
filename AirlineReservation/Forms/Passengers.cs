using System;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.UI;
namespace AirlineReservationDemo
{
    internal class Passengers : AdminListForm
    {
        public Passengers() : base("Passengers", "Manage registered passenger profiles", "passengers")
        {
            UiKit.AddColumn(Grid, "PassId", "ID", 45, null);
            UiKit.AddColumn(Grid, "PassName", "Name", 120, null);
            UiKit.AddColumn(Grid, "PassEmail", "Email", 150, null);
            UiKit.AddColumn(Grid, "PassPhone", "Phone", 100, null);
            UiKit.AddColumn(Grid, "PassNat", "Nationality", 90, null);
            UiKit.AddColumn(Grid, "PassportNo", "Passport", 90, null);
            UiKit.AddColumn(Grid, "DateOfBirth", "Birth date", 80, "dd MMM yyyy");
            ActionButton("Refresh", ButtonKind.Ghost, delegate { Reload(); });
            ActionButton("Delete", ButtonKind.Danger, delegate { DeleteSelected(); });
            ActionButton("Edit", ButtonKind.Secondary, delegate { EditSelected(); });
            ActionButton("Add passenger", ButtonKind.Primary, delegate {
                using (var e = new PassengerEditor(new PassengerEntity())) if (e.ShowDialog(this) == DialogResult.OK)
                Reload();
            });
        }
        protected override void Reload()
        {
            UiKit.Guard(this, delegate { Grid.DataSource = PassengerEntity.List(SearchBox.Text); });
        }
        private void EditSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["PassId"]);
            UiKit.Guard(this, delegate {
                using (var e = new PassengerEditor(PassengerEntity.GetById(id))) if (e.ShowDialog(this) ==
                DialogResult.OK) Reload();
            });
        }
        private void DeleteSelected()
        {
            if (Selected == null)
            return;
            int id = Convert.ToInt32(Selected["PassId"]);
            if (!UiKit.Confirm(this, "Delete this passenger profile? Linked login profiles cannot be deleted."))
            return;
            if (UiKit.Guard(this, delegate { PassengerEntity.Delete(id); }))
            Reload();
        }
    }
    internal sealed class PassengerEditor : EditorForm
    {
        private readonly PassengerEntity passenger;
        private readonly TextBox name = UiKit.MakeTextBox(), email = UiKit.MakeTextBox(), phone = UiKit.MakeTextBox(),
        nationality = UiKit.MakeTextBox(), passport = UiKit.MakeTextBox(),
        address = UiKit.MakeTextBox();
        private readonly ComboBox gender = UiKit.MakeCombo("Male", "Female", "Other");
        private readonly DateTimePicker dob = UiKit.MakeDate("dd MMM yyyy");
        public PassengerEditor(PassengerEntity p) : base("Passenger profile")
        {
            Session.RequireAdmin();
            name.MaxLength=100;email.MaxLength=120;phone.MaxLength=25;nationality.MaxLength=60;passport.MaxLength=12;address.MaxLength=200;
            passenger = p ?? new PassengerEntity();
            dob.MaxDate = DateTime.Today;
            dob.ShowCheckBox = true;
            Field("Full name", name);
            Field("Email", email);
            Field("Phone", phone);
            Field("Nationality", nationality);
            Field("Gender", gender);
            Field("Date of birth (optional)", dob);
            Field("Passport (optional)", passport);
            Field("Address", address);
            name.Text = passenger.PassName;
            email.Text = passenger.PassEmail;
            phone.Text = passenger.PassPhone;
            nationality.Text = passenger.PassNat;
            gender.Text = passenger.PassGender;
            passport.Text = passenger.PassportNo;
            address.Text = passenger.PassAddress;
            dob.Value = passenger.DateOfBirth ?? DateTime.Today.AddYears(-18);
            dob.Checked = passenger.DateOfBirth.HasValue;
        }
        protected override void Save()
        {
            passenger.PassName = name.Text;
            passenger.PassEmail = email.Text;
            passenger.PassPhone = phone.Text;
            passenger.PassNat = nationality.Text;
            passenger.PassGender = gender.Text;
            passenger.PassportNo = passport.Text.Trim().ToUpperInvariant();
            passenger.PassAddress = address.Text;
            passenger.DateOfBirth = dob.Checked ? (DateTime?)dob.Value.Date : null;
            passenger.Save();
        }
    }
}
