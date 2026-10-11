using System;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Passenger sign-up: creates the passenger profile and the login together.</summary>
    internal class Register : BaseForm
    {
        private static readonly string[] Nationalities = { "Bangladeshi", "Indian",      "Pakistani", "Nepali",
            "Sri Lankan",  "Emirati",     "Qatari",    "Saudi",
            "Malaysian",   "Singaporean", "Thai",      "Turkish",
            "British",     "American",    "Canadian",  "Australian",
            "Other" };

        private readonly TextBox nameTB = UiKit.MakeTextBox();
        private readonly TextBox emailTB = UiKit.MakeTextBox();
        private readonly TextBox phoneTB = UiKit.MakeTextBox();
        private readonly ComboBox nationalityCB = UiKit.MakeCombo(Nationalities);
        private readonly ComboBox genderCB = UiKit.MakeCombo("Male", "Female", "Other");
        private readonly DateTimePicker dobPicker = UiKit.MakeDate("dd MMM yyyy");
        private readonly TextBox passportTB = UiKit.MakeTextBox();
        private readonly TextBox addressTB = UiKit.MakeTextBox();
        private readonly TextBox usernameTB = UiKit.MakeTextBox();
        private readonly TextBox passwordTB = UiKit.MakeTextBox();
        private readonly TextBox confirmTB = UiKit.MakeTextBox();

        public Register()
        {
            SuspendLayout();

            BrandPanel brand = new BrandPanel(
            "Join SkyWay.", "One Account for Every Trip. Your Details are Saved so Booking Takes Seconds.",
            string.Empty);
            brand.Dock = DockStyle.Left;
            brand.Width = 380;

            Panel right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, AutoScroll = true,
                Padding = new Padding(48, 28, 48, 20) };

            Label title = UiKit.MakeLabel("Create your account", Theme.Title, Theme.Text);
            title.Dock = DockStyle.Top;
            title.AutoSize = false;
            title.Height = 44;
            Label sub =
            UiKit.MakeLabel("Use Your Name Exactly as It Appears on Your Passport.", Theme.Base, Theme.Muted);
            sub.Dock = DockStyle.Top;
            sub.AutoSize = false;
            sub.Height = 34;

            dobPicker.MaxDate = DateTime.Today;
            dobPicker.MinDate = DateTime.Today.AddYears(-120);
            dobPicker.Value = new DateTime(1995, 1, 1);
            passwordTB.UseSystemPasswordChar = true;
            confirmTB.UseSystemPasswordChar = true;
            nameTB.MaxLength = 100;
            emailTB.MaxLength = 120;
            phoneTB.MaxLength = 25;
            passportTB.MaxLength = 12;
            addressTB.MaxLength = 200;
            usernameTB.MaxLength = 30;
            passwordTB.MaxLength = 100;
            confirmTB.MaxLength = 100;

            TableLayoutPanel grid = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2,
                BackColor = Color.White };
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));

            AddRow(grid, "Full Name", nameTB, "Email", emailTB);
            AddRow(grid, "Mobile Phone", phoneTB, "Nationality", nationalityCB);
            AddRow(grid, "Gender", genderCB, "Date of Birth", dobPicker);
            AddRow(grid, "Passport Number (optional)", passportTB, "Address (Optional)", addressTB);
            AddRow(grid, "Username", usernameTB, null, null);
            AddRow(grid, "Password (8+ chars, letters and numbers)", passwordTB, "Confirm Password", confirmTB);

            FlowLayoutPanel buttons =
            new FlowLayoutPanel { Dock = DockStyle.Top, Height = 64, Padding = new Padding(0, 14, 0, 0),
                BackColor = Color.White };
            Button create = UiKit.MakeButton("Create Account", ButtonKind.Primary, 200, delegate { DoRegister(); });
            create.Height = 44;
            Button cancel =
            UiKit.MakeButton("Back to Sign In", ButtonKind.Secondary, 180, delegate { NavigateTo(new Login()); });
            cancel.Height = 44;
            buttons.Controls.Add(create);
            buttons.Controls.Add(cancel);

            right.Controls.Add(buttons);
            right.Controls.Add(grid);
            right.Controls.Add(sub);
            right.Controls.Add(title);

            Controls.Add(right);
            Controls.Add(brand);
            AcceptButton = create;
            ResumeLayout(false);
        }

        private static void AddRow(TableLayoutPanel grid, string leftCaption, Control left, string rightCaption,
        Control right)
        {
            int row = grid.RowCount++;
            grid.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            Panel l = UiKit.Field(leftCaption, left);
            l.Dock = DockStyle.Fill;
            l.Padding = new Padding(0, 0, 14, 8);
            grid.Controls.Add(l, 0, row);
            if (right != null)
            {
                Panel r = UiKit.Field(rightCaption, right);
                r.Dock = DockStyle.Fill;
                r.Padding = new Padding(0, 0, 0, 8);
                grid.Controls.Add(r, 1, row);
            }
        }

        private void DoRegister()
        {
            string problem = ValidateRegistration();
            if (problem != null)
            {
                UiKit.Warn(this, problem);
                return;
            }

            RegistrationRequest request = new RegistrationRequest { FullName = nameTB.Text.Trim(),
                Email = emailTB.Text.Trim(),
                Phone = phoneTB.Text.Trim(),
                Nationality = nationalityCB.Text,
                Gender = genderCB.Text,
                DateOfBirth = dobPicker.Value.Date,
                PassportNo = passportTB.Text.Trim(),
                Address = addressTB.Text.Trim(),
                Username = usernameTB.Text.Trim(),
                Password = passwordTB.Text };

            if (UiKit.Guard(this, delegate { AuthService.Register(request); }))
            {
                UiKit.Info(this, "Your Account has Been Created. You Can Sign In Now.");
                NavigateTo(new Login());
            }
        }

        private string ValidateRegistration()
        {
            if (!Validator.IsPersonName(nameTB.Text))
            return "Enter Your Full Name (Letters Only).";
            if (!Validator.IsEmail(emailTB.Text))
            return "Enter a Valid Email Address.";
            if (!Validator.IsPhone(phoneTB.Text))
            return "Enter a Valid Phone Number, For Example +8801712345678.";
            if (Validator.AgeOn(dobPicker.Value, DateTime.Today) < 12)
            return "You Must be at Least 12 Years Old to Create an Account.";
            if (!string.IsNullOrWhiteSpace(passportTB.Text) && !Validator.IsPassport(passportTB.Text))
            return "The Passport Number Must be 6-12 Letters or Digits.";
            if (!Validator.IsUsername(usernameTB.Text))
            return "Username must be 3-30 characters: letters, digits, dot or underscore.";
            if (!Validator.IsStrongPassword(passwordTB.Text))
            return "Password Must be at Least 8 Characters and Contain Letters and Numbers.";
            if (passwordTB.Text != confirmTB.Text)
            return "The Two Passwords Do Not Match.";
            return null;
        }
    }
}
