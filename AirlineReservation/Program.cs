using System;
using System.Windows.Forms;

namespace AirlineReservationDemo
{
    internal static class Program
    {
        /// <summary>Application entry point.</summary>
        [STAThread]
        private static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += delegate(object sender, System.Threading.ThreadExceptionEventArgs e)
            {
                MessageBox.Show("An unexpected error occurred:\n\n" + e.Exception.Message, "SkyWay Airlines",
                MessageBoxButtons.OK, MessageBoxIcon.Error);
            };

            // No main form: each screen closes itself when it navigates, and
            // BaseForm ends the application when the last window is closed.
            new Splash().Show();
            Application.Run();
        }
    }
}
