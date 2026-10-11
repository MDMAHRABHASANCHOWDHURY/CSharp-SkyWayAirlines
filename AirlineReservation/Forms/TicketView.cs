using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>Shows the e-tickets of a booking and lets the user print, save as PDF or save as an image.</summary>
    internal class TicketView : BaseForm
    {
        private readonly List<TicketData> tickets;
        private readonly ComboBox travellerCombo = UiKit.MakeCombo();
        private readonly PictureBox picture = new PictureBox { SizeMode = PictureBoxSizeMode.Zoom,
            Dock = DockStyle.Fill, BackColor = Theme.Background };

        public TicketView(int bookingId)
        {
            SuspendLayout();
            ClientSize = new Size(980, 600);
            MinimumSize = new Size(980, 600);
            StartPosition = FormStartPosition.CenterParent;
            Text = "E-ticket";

            tickets = BookingService.GetTicketData(bookingId);

            Panel top = new Panel { Dock = DockStyle.Top, Height = 74, BackColor = Color.White,
                Padding = new Padding(24, 14, 24, 10) };
            Label title = UiKit.MakeLabel(tickets.Count > 0 ? "E-ticket \u00B7 PNR " + tickets[0].Pnr : "E-ticket",
            Theme.Title, Theme.Text);
            title.Location = new Point(24, 14);
            travellerCombo.Width = 280;
            travellerCombo.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            travellerCombo.Location = new Point(ClientSize.Width - 24 - travellerCombo.Width, 22);
            foreach (TicketData t in tickets)
            travellerCombo.Items.Add(t.PassengerName + "  \u00B7  seat " + t.Seat);
            top.Controls.Add(title);
            top.Controls.Add(travellerCombo);
            top.Visible = true;
            top.Resize+=delegate {travellerCombo.Left=top.ClientSize.Width-travellerCombo.Width-24;};

            FlowLayoutPanel footer =
            new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 72, BackColor = Color.White,
                Padding = new Padding(24, 15, 24, 10),
                FlowDirection = FlowDirection.LeftToRight };
            Button print = UiKit.MakeButton("Print / Save as PDF", ButtonKind.Primary, 190, delegate { PrintAll(); });
            Button png = UiKit.MakeButton("Save as image", ButtonKind.Secondary, 150, delegate { SaveImage(); });
            Button close = UiKit.MakeButton("Close", ButtonKind.Ghost, 90, delegate { Close(); });
            Button preview = UiKit.MakeButton("Preview", ButtonKind.Secondary, 100, delegate {
                UiKit.Guard(this, delegate { TicketPrinter.Preview(this, tickets); });
            });
            footer.Controls.AddRange(new Control[] { print, preview, png, close });

            Controls.Add(picture);
            Controls.Add(footer);
            Controls.Add(top);
            ResumeLayout(false);

            travellerCombo.SelectedIndexChanged += delegate
            {
                ShowSelected();
            };
            if (tickets.Count > 0)
            travellerCombo.SelectedIndex = 0;
            if (tickets.Count <= 1)
            travellerCombo.Visible = false;
        }

        private void ShowSelected()
        {
            if (travellerCombo.SelectedIndex < 0)
            return;
            Image old = picture.Image;
            picture.Image = TicketRenderer.Render(tickets[travellerCombo.SelectedIndex], 1f);
            if (old != null)
            old.Dispose();
        }

        private void PrintAll()
        {
            UiKit.Guard(this, delegate { TicketPrinter.Print(this, tickets); });
        }

        private void SaveImage()
        {
            if (travellerCombo.SelectedIndex < 0)
            return;
            UiKit.Guard(this, delegate {
                if (TicketPrinter.SavePng(this, tickets[travellerCombo.SelectedIndex]))
                UiKit.Info(this, "The e-ticket image was saved.");
            });
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            if (picture.Image != null)
            picture.Image.Dispose();
            base.OnFormClosed(e);
        }
    }
}
