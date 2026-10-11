using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Services;

namespace AirlineReservationDemo.UI
{
    /// <summary>
    /// Clickable cabin plan. Free seats are white, taken seats grey, and seats picked by a
    /// traveller show that traveller's number on a blue background.
    /// </summary>
    internal sealed class SeatMapControl : Panel
    {
        private const int SeatW = 42, SeatH = 36, Gap = 8, AisleW = 36, RowLabelW = 34, HeaderH = 30;

        private readonly Dictionary<string, Button> buttons =
        new Dictionary<string, Button>(StringComparer.OrdinalIgnoreCase);
        private readonly List<Control> decorations = new List<Control>();
        private HashSet<string> taken = new HashSet<string>();
        private List<Seat> cabinSeats = new List<Seat>();
        private char[] letters = SeatLayout.EconomyLetters;
        private int aisleAfter = 3;

        public event Action<string> SeatClicked;

        public SeatMapControl()
        {
            AutoScroll = true;
            BackColor = Color.White;
            Resize += delegate
            {
                PositionControls();
            };
        }

        public void LoadCabin(List<Seat> allSeats, string cabin, HashSet<string> takenSeats)
        {
            SuspendLayout();
            foreach (Button b in buttons.Values)
            {
                Controls.Remove(b);
                b.Dispose();
            }
            foreach (Control c in decorations)
            {
                Controls.Remove(c);
                c.Dispose();
            }
            buttons.Clear();
            decorations.Clear();

            taken = takenSeats ?? new HashSet<string>();
            cabinSeats = allSeats.Where(s => s.CabinClass == cabin).ToList();
            bool business = cabin == Cabin.Business;
            letters = business ? SeatLayout.BusinessLetters : SeatLayout.EconomyLetters;
            aisleAfter = business ? 2 : 3;

            foreach (char letter in letters)
            {
                decorations.Add(
                AddText(letter.ToString(), Theme.Bold, Theme.Muted, ContentAlignment.MiddleCenter, SeatW, HeaderH));
            }
            foreach (int row in cabinSeats.Select(s => s.Row).Distinct())
            {
                decorations.Add(
                AddText(row.ToString(), Theme.Small, Theme.Muted, ContentAlignment.MiddleRight, RowLabelW, SeatH));
            }

            foreach (Seat seat in cabinSeats)
            {
                Seat captured = seat;
                bool isTaken = taken.Contains(seat.Number);
                Button b = new Button { Size = new Size(SeatW, SeatH), FlatStyle = FlatStyle.Flat, Font = Theme.Bold,
                    Cursor = isTaken ? Cursors.No : Cursors.Hand, Tag = seat };
                b.FlatAppearance.BorderSize = 1;
                b.Click += delegate
                {
                    if (!taken.Contains(captured.Number) && SeatClicked != null)
                    SeatClicked(captured.Number);
                };
                buttons[seat.Number] = b;
                Controls.Add(b);
            }

            ShowAssignments(new Dictionary<string, int>());
            ResumeLayout(true);
            PositionControls();
        }

        /// <summary>Repaints seats. <paramref name="assignments"/> maps seat number to traveller number
        /// (1-based).</summary>
        public void ShowAssignments(IDictionary<string, int> assignments)
        {
            foreach (KeyValuePair<string, Button> pair in buttons)
            {
                Button b = pair.Value;
                int traveller;
                if (taken.Contains(pair.Key))
                {
                    b.Text = "x";
                    b.BackColor = ColorTranslator.FromHtml("#CFD8DC");
                    b.ForeColor = Theme.Muted;
                    b.FlatAppearance.BorderColor = ColorTranslator.FromHtml("#CFD8DC");
                    b.FlatAppearance.MouseOverBackColor = b.BackColor;
                }
                else if (assignments != null && assignments.TryGetValue(pair.Key, out traveller))
                {
                    b.Text = traveller.ToString();
                    b.BackColor = Theme.Accent;
                    b.ForeColor = Color.White;
                    b.FlatAppearance.BorderColor = Theme.AccentDark;
                    b.FlatAppearance.MouseOverBackColor = Theme.AccentDark;
                }
                else
                {
                    b.Text = ((Seat)b.Tag).Letter.ToString();
                    b.BackColor = Color.White;
                    b.ForeColor = Theme.Text;
                    b.FlatAppearance.BorderColor = Theme.Border;
                    b.FlatAppearance.MouseOverBackColor = Theme.AccentLight;
                }
            }
        }

        private Label AddText(string text, Font font, Color color, ContentAlignment align, int w, int h)
        {
            Label l = new Label { Text = text,       Font = font,           ForeColor = color,
                TextAlign = align, Size = new Size(w, h), BackColor = Color.Transparent };
            Controls.Add(l);
            return l;
        }

        private int ColumnX(char letter)
        {
            int col = Array.IndexOf(letters, letter);
            int blockW = letters.Length * (SeatW + Gap) + AisleW - Gap;
            int left =
            Math.Max(16, (ClientSize.Width - SystemInformation.VerticalScrollBarWidth - (RowLabelW + 10 + blockW)) /
            2) +
            RowLabelW + 10;
            return AutoScrollPosition.X + left + col * (SeatW + Gap) + (col >= aisleAfter ? AisleW - Gap : 0);
        }

        private void PositionControls()
        {
            if (cabinSeats.Count == 0)
            return;
            int scrollY = AutoScrollPosition.Y;

            foreach (Control c in decorations)
            {
                Label l = c as Label;
                if (l == null)
                continue;
                int n;
                if (int.TryParse(l.Text, out n))
                {
                    int rowIndex = cabinSeats.Select(s => s.Row).Distinct().ToList().IndexOf(n);
                    l.Location = new Point(ColumnX(letters[0]) - RowLabelW - 10,
                    scrollY + HeaderH + 8 + rowIndex * (SeatH + Gap));
                }
                else
                {
                    l.Location = new Point(ColumnX(l.Text[0]), scrollY + 4);
                }
            }

            List<int> rows = cabinSeats.Select(s => s.Row).Distinct().ToList();
            foreach (Seat seat in cabinSeats)
            {
                Button b = buttons[seat.Number];
                b.Location =
                new Point(ColumnX(seat.Letter), scrollY + HeaderH + 8 + rows.IndexOf(seat.Row) * (SeatH + Gap));
            }
            Size minimum = new Size(RowLabelW + 42 + letters.Length * (SeatW + Gap) + AisleW, HeaderH + 24 + rows.Count * (SeatH + Gap));
            if (AutoScrollMinSize != minimum) AutoScrollMinSize = minimum;
        }
    }
}
