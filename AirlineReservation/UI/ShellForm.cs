using System;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;

namespace AirlineReservationDemo.UI
{
    internal abstract class ShellForm : BaseForm
    {
        protected readonly Panel Sidebar = new Panel
        {
            Dock = DockStyle.Left,
            Width = 232,
            BackColor = Theme.Navy
        };

        protected readonly Panel Header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 76,
            BackColor = Color.White
        };

        protected readonly Panel Body = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Background,
            Padding = new Padding(28, 24, 28, 24)
        };

        private readonly Panel navHost = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Theme.Navy,
            Padding = new Padding(0, 8, 0, 0)
        };

        private readonly Label titleLabel;
        private readonly Label subtitleLabel;
        private readonly Label userLabel;
        private readonly Label roleLabel;
        private readonly Button currencyButton;

        protected ShellForm(string title, string subtitle)
        {
            SuspendLayout();

            Panel brand = new Panel
            {
                Dock = DockStyle.Top,
                Height = 84,
                BackColor = Theme.Navy
            };

            PlaneBadge badge = new PlaneBadge
            {
                Location = new Point(22, 24),
                Size = new Size(34, 34)
            };

            Label brandName = UiKit.MakeLabel(
                "SkyWay",
                Theme.Title,
                Color.White);

            brandName.Location = new Point(64, 25);

            brand.Controls.Add(badge);
            brand.Controls.Add(brandName);

            Button signOut = new Button
            {
                Text = "Sign out",
                Dock = DockStyle.Bottom,
                Height = 52,
                FlatStyle = FlatStyle.Flat,
                ForeColor = Theme.SidebarText,
                BackColor = Theme.Navy,
                Font = Theme.Base,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(26, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            signOut.FlatAppearance.BorderSize = 0;
            signOut.FlatAppearance.MouseOverBackColor = Theme.Navy2;

            signOut.Click += delegate
            {
                SignOut();
            };

            Sidebar.Controls.Add(navHost);
            Sidebar.Controls.Add(signOut);
            Sidebar.Controls.Add(brand);

            titleLabel = UiKit.MakeLabel(
                title,
                Theme.Title,
                Theme.Text);

            titleLabel.AutoSize = false;
            titleLabel.AutoEllipsis = true;
            titleLabel.Location = new Point(28, 12);
            titleLabel.Height = 32;

            subtitleLabel = UiKit.MakeLabel(
                subtitle,
                Theme.Small,
                Theme.Muted);

            subtitleLabel.AutoSize = false;
            subtitleLabel.AutoEllipsis = true;
            subtitleLabel.Location = new Point(30, 46);
            subtitleLabel.Height = 24;

            userLabel = UiKit.MakeLabel(
                string.Empty,
                Theme.Bold,
                Theme.Text);

            userLabel.AutoSize = false;
            userLabel.AutoEllipsis = true;
            userLabel.TextAlign = ContentAlignment.MiddleRight;
            userLabel.Size = new Size(190, 26);

            roleLabel = UiKit.MakeLabel(
                string.Empty,
                Theme.Small,
                Theme.Muted);

            roleLabel.AutoSize = false;
            roleLabel.AutoEllipsis = true;
            roleLabel.TextAlign = ContentAlignment.MiddleRight;
            roleLabel.Size = new Size(190, 24);

            if (Session.IsSignedIn)
            {
                userLabel.Text = Session.Current.FullName;

                roleLabel.Text = Session.Current.IsAdmin
                    ? "Administrator"
                    : "Passenger";
            }

            currencyButton = UiKit.MakeButton(
                "Currency: " + Money.Currency,
                ButtonKind.Secondary,
                150,
                null);

            currencyButton.Height = 36;

            currencyButton.Click += delegate
            {
                // Validate the rate before switching to BDT.
                if (Money.Currency == "USD")
                {
                    try
                    {
                        Money.ToBdt(1m);
                    }
                    catch (System.Configuration.ConfigurationErrorsException ex)
                    {
                        MessageBox.Show(
                            this,
                            ex.Message,
                            "Currency settings",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);

                        return;
                    }
                }

                Money.ToggleCurrency();

                currencyButton.Text =
                    "Currency: " + Money.Currency;

                OnCurrencyChanged();
            };

            Header.Controls.Add(titleLabel);
            Header.Controls.Add(subtitleLabel);
            Header.Controls.Add(userLabel);
            Header.Controls.Add(roleLabel);
            Header.Controls.Add(currencyButton);

            Header.Resize += delegate
            {
                LayoutHeader();
            };

            Header.Paint += delegate (object sender, PaintEventArgs e)
            {
                using (Pen border = new Pen(Theme.Border))
                {
                    e.Graphics.DrawLine(
                        border,
                        0,
                        Header.Height - 1,
                        Header.Width,
                        Header.Height - 1);
                }
            };

            // The last added docked control is laid out first.
            Controls.Add(Body);
            Controls.Add(Header);
            Controls.Add(Sidebar);

            ResumeLayout(true);

            LayoutHeader();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);

            currencyButton.Text =
                "Currency: " + Money.Currency;

            LayoutHeader();
        }

        private void LayoutHeader()
        {
            int right = Header.ClientSize.Width - 28;

            int userLeft = Math.Max(
                28,
                right - userLabel.Width);

            userLabel.Location = new Point(userLeft, 14);
            roleLabel.Location = new Point(userLeft, 42);

            // Do not rely on Control.Visible during construction.
            bool showCurrency = string.Equals(
                titleLabel.Text,
                "Dashboard",
                StringComparison.OrdinalIgnoreCase);

            currencyButton.Visible = showCurrency;

            int buttonLeft = Math.Max(
                28,
                userLeft - currencyButton.Width - 20);

            currencyButton.Location =
                new Point(buttonLeft, 20);

            int titleRight = showCurrency
                ? buttonLeft - 20
                : userLeft - 20;

            titleLabel.Width = Math.Max(
                1,
                titleRight - titleLabel.Left);

            subtitleLabel.Width = Math.Max(
                1,
                titleRight - subtitleLabel.Left);

            currencyButton.BringToFront();
        }

        protected virtual void OnCurrencyChanged()
        {
        }

        protected void SetTitle(string title, string subtitle)
        {
            titleLabel.Text = title;
            subtitleLabel.Text = subtitle;

            LayoutHeader();
        }

        protected Button AddNav(
            string text,
            EventHandler onClick,
            bool active)
        {
            Button button = new Button
            {
                Text = text,
                Dock = DockStyle.Top,
                Height = 48,
                FlatStyle = FlatStyle.Flat,
                ForeColor = active
                    ? Color.White
                    : Theme.SidebarText,
                BackColor = active
                    ? Theme.Navy2
                    : Theme.Navy,
                Font = active
                    ? Theme.Bold
                    : Theme.Base,
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(26, 0, 0, 0),
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false
            };

            button.FlatAppearance.BorderSize = 0;
            button.FlatAppearance.MouseOverBackColor = Theme.Navy2;

            if (active)
            {
                button.Paint += delegate (object sender, PaintEventArgs e)
                {
                    using (Brush accent =
                        new SolidBrush(Theme.Orange))
                    {
                        e.Graphics.FillRectangle(
                            accent,
                            0,
                            0,
                            4,
                            button.Height);
                    }
                };
            }

            if (onClick != null)
            {
                button.Click += onClick;
            }

            navHost.Controls.Add(button);
            navHost.Controls.SetChildIndex(button, 0);

            return button;
        }

        protected void SignOut()
        {
            Session.End();
            NavigateTo(new Login());
        }
    }
}