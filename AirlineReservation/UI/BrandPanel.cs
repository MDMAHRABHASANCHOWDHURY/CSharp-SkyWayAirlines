using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Windows.Forms;

namespace AirlineReservationDemo.UI
{
    internal sealed class BrandPanel : Panel
    {
        private readonly Label headline;
        private readonly Label tagline;
        private readonly Label footer;

        public BrandPanel(
            string headlineText,
            string taglineText,
            string footerText)
        {
            SetStyle(
                ControlStyles.OptimizedDoubleBuffer |
                ControlStyles.ResizeRedraw |
                ControlStyles.AllPaintingInWmPaint |
                ControlStyles.UserPaint,
                true);

            headline = new Label
            {
                Text = headlineText,
                Font = Theme.Display,
                ForeColor = Color.White,
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false
            };

            tagline = new Label
            {
                Text = taglineText,
                Font = Theme.Base,
                ForeColor = Color.FromArgb(220, 235, 250),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false
            };

            footer = new Label
            {
                Text = footerText,
                Font = Theme.Small,
                ForeColor = Color.FromArgb(170, 200, 235),
                BackColor = Color.Transparent,
                AutoSize = false,
                TextAlign = ContentAlignment.TopLeft,
                UseMnemonic = false
            };

            Controls.Add(headline);
            Controls.Add(tagline);
            Controls.Add(footer);

            Resize += delegate
            {
                LayoutLabels();
            };

            LayoutLabels();
        }

        private void LayoutLabels()
        {
            int margin = 36;
            int availableWidth = Math.Max(
                1, ClientSize.Width - margin * 2);

            int headlineTop = Math.Max(
                200, ClientSize.Height / 3 + 20);

            Size headlineSize = TextRenderer.MeasureText(
                headline.Text,
                headline.Font,
                new Size(availableWidth, int.MaxValue),
                TextFormatFlags.WordBreak |
                TextFormatFlags.TextBoxControl |
                TextFormatFlags.NoPrefix);

            headline.SetBounds(
                margin,
                headlineTop,
                availableWidth,
                headlineSize.Height + 12);

            int taglineTop = headline.Bottom + 24;

            Size taglineSize = TextRenderer.MeasureText(
                tagline.Text,
                tagline.Font,
                new Size(availableWidth, int.MaxValue),
                TextFormatFlags.WordBreak |
                TextFormatFlags.TextBoxControl |
                TextFormatFlags.NoPrefix);

            tagline.SetBounds(
                margin,
                taglineTop,
                availableWidth,
                taglineSize.Height + 12);

            Size footerSize = TextRenderer.MeasureText(
                footer.Text,
                footer.Font,
                new Size(availableWidth, int.MaxValue),
                TextFormatFlags.WordBreak |
                TextFormatFlags.TextBoxControl |
                TextFormatFlags.NoPrefix);

            int footerHeight = footerSize.Height + 8;

            footer.SetBounds(
                margin,
                Math.Max(
                    tagline.Bottom + 24,
                    ClientSize.Height - footerHeight - 28),
                availableWidth,
                footerHeight);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Rectangle bounds = ClientRectangle;

            if (bounds.Width < 2 || bounds.Height < 2)
            {
                return;
            }

            using (LinearGradientBrush background =
                new LinearGradientBrush(
                    bounds,
                    Theme.Navy,
                    Theme.AccentDark,
                    60f))
            {
                e.Graphics.FillRectangle(background, bounds);
            }

            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;

            using (Brush topCircle =
                new SolidBrush(Color.FromArgb(22, 255, 255, 255)))
            using (Brush bottomCircle =
                new SolidBrush(Color.FromArgb(14, 255, 255, 255)))
            {
                e.Graphics.FillEllipse(
                    topCircle,
                    bounds.Width - 260,
                    -120,
                    420,
                    420);

                e.Graphics.FillEllipse(
                    bottomCircle,
                    -140,
                    bounds.Height - 280,
                    420,
                    420);
            }

            PlaneIcon.Draw(
                e.Graphics,
                new RectangleF(36, 70, 90, 90),
                Color.White);

            using (Font logoFont = new Font(
                Theme.Base.FontFamily,
                16f,
                FontStyle.Bold))
            using (Brush logoBrush = new SolidBrush(Color.White))
            {
                e.Graphics.DrawString(
                    "SkyWay",
                    logoFont,
                    logoBrush,
                    138,
                    92);
            }
        }
    }
}