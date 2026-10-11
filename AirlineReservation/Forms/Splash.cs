using System;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;
using AirlineReservationDemo.Data;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Welcome screen. Also prepares the database on first start.</summary>
    internal class Splash : BaseForm
    {
        private readonly Button startButton;
        private readonly Button retryButton;
        private readonly Label statusLabel;

        public Splash()
        {
            SuspendLayout();

            BrandPanel brand =
            new BrandPanel("Fly Smarter.\nBook in Minutes.",
            "Find Your Flight, Choose Your Seat and Manage Your Journey.",
            "\u00A9 SkyWay Airlines Reservation System");
            brand.Dock = DockStyle.Left;
            brand.Width = 500;

            Panel right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel content =
            new Panel { Size = new Size(420, 470), Anchor = AnchorStyles.None, BackColor = Color.White };

            Label hello = UiKit.MakeLabel("Welcome Aboard", Theme.Title, Theme.Text);
            hello.Location = new Point(0, 0);
            Label sub = UiKit.MakeLabel("The Complete Airline Booking and Management System.", Theme.Base, Theme.Muted);
            sub.Location = new Point(2, 40);

            string[] features = { "Search Flights by Route, Date and Cabin",
                "Choose Your Seat on an Interactive Cabin Map",
                "Receive Your Booking Reference and e-Ticket",
                "Manage, Print or Cancel Bookings with Refunds",
                "Admin Tools for Flights, Passengers and Sales" };
            int y = 92;
            foreach (string f in features)
            {
                Label dot = new Label { Text = "\u2022", ForeColor = Theme.Accent, Font = Theme.Subtitle,
                    AutoSize = true, Location = new Point(2, y - 4) };
                Label l = UiKit.MakeLabel(f, Theme.Base, Theme.Text);
                l.Location = new Point(26, y);
                content.Controls.Add(dot);
                content.Controls.Add(l);
                y += 34;
            }

            startButton =
            UiKit.MakeButton("Get Started", ButtonKind.Primary, 420, delegate { NavigateTo(new Login()); });
            startButton.Location = new Point(0, 300);
            startButton.Height = 48;
            System.Configuration.ConfigurationManager.RefreshSection("connectionStrings");
            System.Configuration.ConfigurationManager.RefreshSection("appSettings");
            startButton.Enabled = false;

            statusLabel = UiKit.MakeLabel("Connecting to the Database...", Theme.Small, Theme.Muted);
            statusLabel.AutoSize = false;
            statusLabel.Size = new Size(420, 70);
            statusLabel.Location = new Point(0, 362);

            retryButton =
            UiKit.MakeButton("Retry Connection", ButtonKind.Secondary, 180, delegate { StartInitialisation(); });
            retryButton.Location = new Point(0, 436 - 6);
            retryButton.Visible = false;

            Button exit = UiKit.MakeButton("Exit", ButtonKind.Ghost, 80, delegate { Close(); });
            exit.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            exit.Location = new Point(right.Width - 100, 16);

            content.Controls.Add(hello);
            content.Controls.Add(sub);
            content.Controls.Add(startButton);
            content.Controls.Add(statusLabel);
            content.Controls.Add(retryButton);

            right.Controls.Add(content);
            right.Controls.Add(exit);
            right.Resize += delegate
            {
                content.Location =
                new Point((right.Width - content.Width) / 2, Math.Max(20, (right.Height - content.Height) / 2));
                exit.Location = new Point(right.Width - exit.Width - 20, 16);
            };

            Controls.Add(right);
            Controls.Add(brand);
            ResumeLayout(false);

            Shown += delegate
            {
                StartInitialisation();
            };
        }

        private void StartInitialisation()
        {
            startButton.Enabled = false;
            retryButton.Visible = false;
            statusLabel.ForeColor = Theme.Muted;
            statusLabel.Text = "Preparing The Database (The First Start Can Take a Few Seconds)...";

            Task.Run(() =>
            {
                try
                {
                    DbInitializer.EnsureCreated();
                    return null;
                }
                catch (Exception ex)
                {
                    return ex;
                }
            })
            .ContinueWith(
            t =>
            {
                if (IsDisposed)
                return;
                Exception error = t.Result;
                if (error == null)
                {
                    statusLabel.ForeColor = Theme.Success;
                    statusLabel.Text = "Database Ready.";
                    startButton.Enabled = true;
                    AcceptButton = startButton;
                }
                else
                {
                    string first = error.Message.Split('\n')[0];
                    statusLabel.ForeColor = Theme.Danger;
                    statusLabel.Text = "Could not Open the Database: " + first +
                    "\nEdit AirlineReservationDemo.exe.config beside the EXE, then restart. For source builds, edit App.config and rebuild.";
                    retryButton.Visible = true;
                }
            },
            TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // Splash
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.ClientSize = new System.Drawing.Size(1180, 680);
            this.Name = "Splash";
            this.Load += new System.EventHandler(this.Splash_Load);
            this.ResumeLayout(false);

        }

        private void Splash_Load(object sender, EventArgs e)
        {

        }
    }
}
