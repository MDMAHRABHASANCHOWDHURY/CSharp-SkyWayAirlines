using System.Drawing;
using System.Drawing.Drawing2D;

namespace AirlineReservationDemo.UI
{
    /// <summary>Vector paper-plane logo drawn with GDI+ (no image files or special fonts needed).</summary>
    internal static class PlaneIcon
    {
        public static void Draw(Graphics g, RectangleF r, Color color)
        {
            SmoothingMode old = g.SmoothingMode;
            g.SmoothingMode = SmoothingMode.AntiAlias;

            PointF P(float x, float y)
            {
                return new PointF(r.X + x * r.Width, r.Y + y * r.Height);
            }

            using (Brush main = new SolidBrush(color)) using (
            Brush shade = new SolidBrush(
            Color.FromArgb(150, color))) using (Brush tail = new SolidBrush(Color.FromArgb(200, color)))
            {
                g.FillPolygon(main, new[] { P(0.03f, 0.52f), P(0.97f, 0.04f), P(0.44f, 0.62f) });
                g.FillPolygon(shade, new[] { P(0.44f, 0.62f), P(0.97f, 0.04f), P(0.60f, 0.96f) });
                g.FillPolygon(tail, new[] { P(0.44f, 0.62f), P(0.60f, 0.96f), P(0.40f, 0.80f) });
            }
            g.SmoothingMode = old;
        }
    }

    /// <summary>Small control that shows the plane logo.</summary>
    internal sealed class PlaneBadge : System.Windows.Forms.Control
    {
        public Color IconColor { get; set; }

        public PlaneBadge()
        {
            SetStyle(System.Windows.Forms.ControlStyles.SupportsTransparentBackColor |
            System.Windows.Forms.ControlStyles.OptimizedDoubleBuffer |
            System.Windows.Forms.ControlStyles.UserPaint,
            true);
            BackColor = Color.Transparent;
            IconColor = Color.White;
            Size = new Size(36, 36);
        }

        protected override void OnPaint(System.Windows.Forms.PaintEventArgs e)
        {
            PlaneIcon.Draw(e.Graphics, new RectangleF(0, 0, Width - 1, Height - 1), IconColor);
        }
    }
}
