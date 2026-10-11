using System;
using System.Configuration;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Sign-in screen. Admins go to the admin dashboard, passengers to theirs.</summary>
    internal class Login : BaseForm
    {
        private readonly TextBox usernameTB = UiKit.MakeTextBox();
        private readonly TextBox passwordTB = UiKit.MakeTextBox();
        private readonly Label errorLabel;
        private readonly Button signInButton;

        public Login()
        {
            SuspendLayout();

            BrandPanel brand = new BrandPanel("Welcome back.",
            "Sign in to book flights, manage your trips and download your e-tickets.",
            "Protected sign-in with salted password hashing");
            brand.Dock = DockStyle.Left;
            brand.Width = 440;

            Panel right = new Panel { Dock = DockStyle.Fill, BackColor = Color.White };
            Panel form = new Panel { Size = new Size(380, 520), Anchor = AnchorStyles.None, BackColor = Color.White };

            Label title = UiKit.MakeLabel("Sign in", Theme.Title, Theme.Text);
            title.Location = new Point(0, 0);
            Label sub = UiKit.MakeLabel("Enter your account details to continue.", Theme.Base, Theme.Muted);
            sub.Location = new Point(2, 40);

            passwordTB.UseSystemPasswordChar = true;
            usernameTB.MaxLength = 50;
            passwordTB.MaxLength = 100;

            Panel userField = UiKit.Field("Username", usernameTB);
            Panel passField = UiKit.Field("Password", passwordTB);
            userField.Dock = DockStyle.None;
            userField.Size = new Size(380, 62);
            userField.Location = new Point(0, 88);
            passField.Dock = DockStyle.None;
            passField.Size = new Size(380, 62);
            passField.Location = new Point(0, 156);

            CheckBox show = new CheckBox { Text = "Show password", Font = Theme.Small, ForeColor = Theme.Muted,
                AutoSize = true, Location = new Point(2, 222) };
            show.CheckedChanged += delegate
            {
                passwordTB.UseSystemPasswordChar = !show.Checked;
            };

            errorLabel = UiKit.MakeLabel(string.Empty, Theme.Small, Theme.Danger);
            errorLabel.AutoSize = false;
            errorLabel.Size = new Size(380, 36);
            errorLabel.Location = new Point(0, 250);

            signInButton = UiKit.MakeButton("Sign in", ButtonKind.Primary, 380, delegate { DoLogin(); });
            signInButton.Height = 46;
            signInButton.Location = new Point(0, 292);

            Label noAccount = UiKit.MakeLabel("New to SkyWay?", Theme.Base, Theme.Muted);
            noAccount.Location = new Point(2, 356);
            LinkLabel register = new LinkLabel { Text = "Create a passenger account",
                Font = Theme.Bold,
                AutoSize = true,
                LinkColor = Theme.Accent,
                ActiveLinkColor = Theme.AccentDark,
                LinkBehavior = LinkBehavior.HoverUnderline,
                Location = new Point(120, 356) };
            register.Click += delegate
            {
                NavigateTo(new Register());
            };

            form.Controls.AddRange(new Control[] { title, sub, userField, passField, show, errorLabel, signInButton,
                noAccount, register });

            if (string.Equals(ConfigurationManager.AppSettings["ShowDemoHint"], "true",
            StringComparison.OrdinalIgnoreCase))
            {
                Panel hint =
                new Panel { Size = new Size(380, 84), Location = new Point(0, 404), BackColor = Theme.AccentLight };
                Label h1 = UiKit.MakeLabel("Demo accounts", Theme.Bold, Theme.AccentDark);
                h1.Location = new Point(14, 10);
                Label h2 = UiKit.MakeLabel(
                "Admin:  " + AuthService.DemoAdminUser + " / " + AuthService.DemoAdminPassword +
                "\nPassenger:  " + AuthService.DemoPassengerUser + " / " + AuthService.DemoPassengerPassword,
                Theme.Small, Theme.Text);
                h2.Location = new Point(14, 34);
                hint.Controls.Add(h1);
                hint.Controls.Add(h2);
                form.Controls.Add(hint);
            }

            Button back = UiKit.MakeButton("Back", ButtonKind.Ghost, 80, delegate { NavigateTo(new Splash()); });
            back.Location = new Point(10, 14);

            right.Controls.Add(form);
            right.Controls.Add(back);
            right.Resize += delegate
            {
                form.Location =
                new Point((right.Width - form.Width) / 2, Math.Max(20, (right.Height - form.Height) / 2));
            };

            Controls.Add(right);
            Controls.Add(brand);
            AcceptButton = signInButton;
            ResumeLayout(false);

            Shown += delegate
            {
                usernameTB.Focus();
            };
        }

        private void DoLogin()
        {
            errorLabel.Text = string.Empty;
            if (string.IsNullOrWhiteSpace(usernameTB.Text) || passwordTB.Text.Length == 0)
            {
                errorLabel.Text = "Please enter your username and password.";
                return;
            }

            signInButton.Enabled = false;
            UserEntity user = null;
            try
            {
                Cursor = Cursors.WaitCursor;
                user = AuthService.Login(usernameTB.Text, passwordTB.Text);
            }
            catch (AuthException ex)
            {
                errorLabel.Text = ex.Message;
                passwordTB.Clear();
                passwordTB.Focus();
            }
            catch (Exception ex)
            {
                errorLabel.Text = "Cannot reach the database: " + ex.Message.Split('\n')[0];
            }
            finally
            {
                Cursor = Cursors.Default;
                signInButton.Enabled = true;
            }

            if (user == null)
            return;
            if (user.MustChangePassword)
            {
                using (var change = new ChangePasswordForm())
                {
                    if (change.ShowDialog(this) != DialogResult.OK)
                    {
                        Session.End();
                        passwordTB.Clear();
                        return;
                    }
                }
            }
            passwordTB.Clear();
            if (user.IsAdmin)
            NavigateTo(new AdminDashboard());
            else
            NavigateTo(new UserDashboard());
        }
    }
}
