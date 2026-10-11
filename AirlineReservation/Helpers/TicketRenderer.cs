using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Text;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo.Helpers
{
    /// <summary>Draws one demonstration e-ticket as an image (used for screen, print and PNG export).</summary>
    internal static class TicketRenderer
    {
        public const int Width = 900;
        public const int Height = 380;

        public static Bitmap Render(TicketData t, float scale)
        {
            if (t == null) throw new ArgumentNullException("t");
            if (float.IsNaN(scale) || float.IsInfinity(scale) || scale < 0.25f || scale > 4f) throw new ArgumentOutOfRangeException("scale");
            Bitmap bmp = new Bitmap((int)(Width * scale), (int)(Height * scale));
            try
            {
                using (Graphics g = Graphics.FromImage(bmp))
                {
                    g.SmoothingMode = SmoothingMode.AntiAlias;
                    g.TextRenderingHint = TextRenderingHint.AntiAlias;
                    g.Clear(Color.White);
                    g.ScaleTransform(scale, scale);
                    Draw(g, t);
                }
                return bmp;
            }
            catch { bmp.Dispose(); throw; }
        }

        private static void Draw(Graphics g, TicketData t)
        {
            var fonts = new System.Collections.Generic.List<Font>();
            Func<FontFamily, float, FontStyle, Font> makeFont = (family, size, style) => { var font = new Font(family, size, style); fonts.Add(font); return font; };
            try
            {
                Rectangle card = new Rectangle(0, 0, Width - 1, Height - 1);
                using (GraphicsPath clip = RoundedRect(card, 16))
                {
                    g.SetClip(clip);
                    g.FillPath(Brushes.White, clip);

                    // header band
                using (Brush navy = new SolidBrush(Theme.Navy))
                    {
                        g.FillRectangle(navy, 0, 0, Width, 64);
                    }
                    PlaneIcon.Draw(g, new RectangleF(24, 15, 34, 34), Color.White);
                    Text(g, t.Airline, makeFont(Theme.Base.FontFamily, 16f, FontStyle.Bold), Color.White, 68, 16);
                    Text(g, "E-TICKET  \u00B7  " + t.Cabin.ToUpperInvariant(),
                    makeFont(Theme.Base.FontFamily, 11f, FontStyle.Bold), Theme.Orange, Width - 24, 22,
                    StringAlignment.Far);

                    // passenger
                Label(g, "PASSENGER", 30, 82);
                    Text(g, t.PassengerName.ToUpperInvariant(), makeFont(Theme.Base.FontFamily, 17f, FontStyle.Bold),
                    Theme.Text, 30, 98);

                    // route
                Font code = makeFont(Theme.Base.FontFamily, 40f, FontStyle.Bold);
                    Text(g, t.Src, code, Theme.Navy, 26, 138);
                    Text(g, t.Dest, code, Theme.Navy, 330, 138);
                    using (Pen line = new Pen(Theme.Border, 3f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(line, 170, 172, 320, 172);
                    }
                    PlaneIcon.Draw(g, new RectangleF(228, 158, 28, 28), Theme.Accent);
                    Text(g, t.SrcCity, Theme.Base, Theme.Muted, 30, 202);
                    Text(g, t.DestCity, Theme.Base, Theme.Muted, 334, 202);

                    // flight facts
                Font value = makeFont(Theme.Base.FontFamily, 12.5f, FontStyle.Bold);
                    string arrivalNote = t.Arrival.Date != t.Departure.Date
                    ? " (+" + (t.Arrival.Date - t.Departure.Date).Days + ")"
                    : string.Empty;
                    Fact(g, "DATE", t.Departure.ToString("ddd, dd MMM yyyy"), value, 30, 236);
                    Fact(g, "DEPARTS", t.Departure.ToString("HH:mm"), value, 230, 236);
                    Fact(g, "ARRIVES", t.Arrival.ToString("HH:mm") + arrivalNote, value, 350, 236);
                    Fact(g, "FLIGHT", t.FlightNo, value, 30, 290);
                    Fact(g, "SEAT", t.Seat, makeFont(Theme.Base.FontFamily, 16f, FontStyle.Bold), 230, 284, Theme.Accent);
                    Fact(g, "CLASS", t.Cabin, value, 350, 290);
                    Fact(g, "EXTRA BAGGAGE", t.BaggageKg > 0 ? "+" + t.BaggageKg + " kg" : "None", value, 480, 290);

                    // footer note
                using (Pen p = new Pen(Theme.Border))
                    {
                        g.DrawLine(p, 30, Height - 40, 636, Height - 40);
                    }
                    Text(
                    g,
                    "Arrive 3 h before international and 2 h before domestic departures. Carry a valid passport / ID " +
                    "matching the name above.",
                    makeFont(Theme.Base.FontFamily, 7.5f, FontStyle.Regular), Theme.Muted, 30, Height - 34);

                    // stub
                using (Pen dash = new Pen(Color.FromArgb(170, 180, 195), 1.5f) { DashStyle = DashStyle.Dash })
                    {
                        g.DrawLine(dash, 660, 76, 660, Height - 14);
                    }
                    Label(g, "BOOKING REF (PNR)", 684, 82);
                    Text(g, t.Pnr, makeFont(Theme.Mono.FontFamily, 28f, FontStyle.Bold), Theme.Navy, 682, 98);
                    Label(g, "E-TICKET NUMBER", 684, 160);
                    Text(g, t.TicketNo, makeFont(Theme.Mono.FontFamily, 13f, FontStyle.Bold), Theme.Text, 684, 178);
                    Barcode39.Draw(g, t.TicketNo, new Rectangle(678, 232, 200, 76), Color.Black);
                    Text(g, t.TicketNo, makeFont(Theme.Mono.FontFamily, 9f, FontStyle.Regular), Theme.Muted, 778, 314, StringAlignment.Center);

                    if (t.Status == "Cancelled")
                    {
                        GraphicsState state = g.Save();
                        g.TranslateTransform(Width / 2f, Height / 2f + 20);
                        g.RotateTransform(-14f);
                        using (Font f = new Font(Theme.Base.FontFamily, 72f, FontStyle.Bold)) using (
                        Brush b = new SolidBrush(Color.FromArgb(
                        120, Theme.Danger))) using (StringFormat sf =
                        new StringFormat { Alignment = StringAlignment.Center,
                            LineAlignment = StringAlignment.Center })
                        {
                            g.DrawString("CANCELLED", f, b, 0, 0, sf);
                        }
                        g.Restore(state);
                    }
                    g.ResetClip();
                }

                using (GraphicsPath outline = RoundedRect(card, 16)) using (Pen border = new Pen(Theme.Border, 1.5f))
                {
                    g.DrawPath(border, outline);
                }
            }
            finally { foreach (Font font in fonts) font.Dispose(); }
        }

        private static void Label(Graphics g, string text, float x, float y)
        {
            using (Font font = new Font(Theme.Base.FontFamily, 7.5f, FontStyle.Bold))
            Text(g, text, font, Theme.Muted, x, y);
        }

        private static void Fact(Graphics g, string caption, string value, Font font, float x, float y)
        {
            Fact(g, caption, value, font, x, y, Theme.Text);
        }

        private static void Fact(Graphics g, string caption, string value, Font font, float x, float y, Color color)
        {
            Label(g, caption, x, y);
            Text(g, value, font, color, x, y + 15);
        }

        private static void Text(Graphics g, string text, Font font, Color color, float x, float y)
        {
            Text(g, text, font, color, x, y, StringAlignment.Near);
        }

        private static void Text(Graphics g, string text, Font font, Color color, float x, float y,
        StringAlignment align)
        {
            using (Brush b = new SolidBrush(color)) using (
            StringFormat sf = new StringFormat { Alignment = align, FormatFlags = StringFormatFlags.NoWrap })
            {
                if (align == StringAlignment.Near)
                {
                    sf.Trimming = StringTrimming.EllipsisCharacter;
                    float available = x < 660 ? 636 - x : Width - 20 - x;
                    g.DrawString(text ?? string.Empty, font, b, new RectangleF(x, y, Math.Max(1, available), font.GetHeight(g) * 1.5f), sf);
                }
                else g.DrawString(text ?? string.Empty, font, b, x, y, sf);
            }
        }

        private static GraphicsPath RoundedRect(Rectangle r, int radius)
        {
            int d = radius * 2;
            GraphicsPath path = new GraphicsPath();
            path.AddArc(r.X, r.Y, d, d, 180, 90);
            path.AddArc(r.Right - d, r.Y, d, d, 270, 90);
            path.AddArc(r.Right - d, r.Bottom - d, d, d, 0, 90);
            path.AddArc(r.X, r.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }
}
