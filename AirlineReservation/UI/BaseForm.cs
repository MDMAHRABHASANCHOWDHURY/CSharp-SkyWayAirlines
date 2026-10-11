using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace AirlineReservationDemo.UI
{
    /// <summary>
    /// Base class for every window: common font, size, colours and navigation.
    /// Navigating opens the next form in the same place and CLOSES the current one
    /// (the original project only hid forms, which left them running in memory).
    /// The application exits when the last form is closed.
    /// </summary>
    internal class BaseForm : Form
    {
        public BaseForm()
        {
            SuspendLayout();
            AutoScaleDimensions = new SizeF(96F, 96F);
            AutoScaleMode = AutoScaleMode.Dpi;
            Font = Theme.Base;
            BackColor = Theme.Background;
            ClientSize = new Size(1180, 680);
            MinimumSize = new Size(1040, 700);
            StartPosition = FormStartPosition.CenterScreen;
            Text = "SkyWay Airlines - Airline Reservation System";
            DoubleBuffered = true;
            ResumeLayout(false);
        }

        /// <summary>Shows <paramref name="next"/> where this form is, then closes this form.</summary>
        protected void NavigateTo(Form next)
        {
            next.StartPosition = FormStartPosition.Manual;
            next.WindowState =
            WindowState == FormWindowState.Maximized ? FormWindowState.Maximized : FormWindowState.Normal;
            if (WindowState != FormWindowState.Maximized)
            {
                next.Bounds = Bounds;
            }
            next.Show();
            Close();
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            base.OnFormClosed(e);
            bool anotherFormOpen = Application.OpenForms.Cast<Form>().Any(f => f != this && !f.IsDisposed);
            if (!anotherFormOpen)
            {
                Application.ExitThread();
            }
        }
    }
}
