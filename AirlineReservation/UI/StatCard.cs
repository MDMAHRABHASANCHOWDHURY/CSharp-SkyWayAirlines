using System.Drawing;
using System.Windows.Forms;

namespace AirlineReservationDemo.UI
{
    /// <summary>A small KPI tile: caption on top, large value below, coloured accent bar on the left.</summary>
    internal sealed class StatCard : Panel
    {
        private readonly Label caption;
        private readonly Label value;
        private readonly Color accent;

        public StatCard(string captionText, Color accentColor)
        {
            accent = accentColor;
            BackColor = Theme.Card;
            Padding = new Padding(20, 14, 12, 10);
            Margin = new Padding(0, 0, 14, 0);
            Height = 98;

            caption = UiKit.MakeLabel(captionText, Theme.Small, Theme.Muted);
            caption.Dock = DockStyle.Top;
            caption.AutoSize = false;
            caption.Height = 22;

            value = UiKit.MakeLabel("-", Theme.Stat, Theme.Text);
            value.Dock = DockStyle.Fill;
            value.AutoSize = false;
            value.TextAlign = ContentAlignment.MiddleLeft;

            Controls.Add(value);
            Controls.Add(caption);

            Paint += delegate(object s, PaintEventArgs e) {
                using (Pen p = new Pen(Theme.Border)) { e.Graphics.DrawRectangle(p, 0, 0, Width - 1, Height - 1);
                }
                using (Brush b = new SolidBrush(accent))
                {
                    e.Graphics.FillRectangle(b, 0, 0, 5, Height);
                }
            };
        }

        public string Value
        {
            get {
                return value.Text;
            }
            set {
                this.value.Text = value;
            }
        }
    }
}
