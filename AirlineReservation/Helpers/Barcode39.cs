using System;
using System.Collections.Generic;
using System.Drawing;

namespace AirlineReservationDemo.Helpers
{
    /// <summary>
    /// Code 39 barcode renderer (letters, digits and - . space $ / + %).
    /// Each character is nine elements (bar, space, bar ...), three of them wide.
    /// </summary>
    internal static class Barcode39
    {
        // '1' = wide element, '0' = narrow element.
        private static readonly Dictionary<char, string> Patterns = new Dictionary<char, string> {
            { '0', "000110100" }, { '1', "100100001" }, { '2', "001100001" }, { '3', "101100000" },
            { '4', "000110001" }, { '5', "100110000" }, { '6', "001110000" }, { '7', "000100101" },
            { '8', "100100100" }, { '9', "001100100" }, { 'A', "100001001" }, { 'B', "001001001" },
            { 'C', "101001000" }, { 'D', "000011001" }, { 'E', "100011000" }, { 'F', "001011000" },
            { 'G', "000001101" }, { 'H', "100001100" }, { 'I', "001001100" }, { 'J', "000011100" },
            { 'K', "100000011" }, { 'L', "001000011" }, { 'M', "101000010" }, { 'N', "000010011" },
            { 'O', "100010010" }, { 'P', "001010010" }, { 'Q', "000000111" }, { 'R', "100000110" },
            { 'S', "001000110" }, { 'T', "000010110" }, { 'U', "110000001" }, { 'V', "011000001" },
            { 'W', "111000000" }, { 'X', "010010001" }, { 'Y', "110010000" }, { 'Z', "011010000" },
            { '-', "010000101" }, { '.', "110000100" }, { ' ', "011000100" }, { '*', "010010100" },
            { '$', "010101000" }, { '/', "010100010" }, { '+', "010001010" }, { '%', "000101010" }
        };

        /// <summary>Exposed so the unit tests can check every pattern.</summary>
        internal static IDictionary<char, string> AllPatterns
        {
            get {
                return Patterns;
            }
        }

        /// <summary>Draws the barcode inside <paramref name="area"/> (quiet zones included).</summary>
        public static void Draw(Graphics g, string text, Rectangle area, Color barColor)
        {
            string data = "*" + (text ?? string.Empty).ToUpperInvariant() + "*";
            foreach (char c in data)
            {
                if (!Patterns.ContainsKey(c))
                {
                    throw new ArgumentException("Character '" + c + "' cannot be encoded in Code 39.");
                }
            }

            // Width in "narrow units": 12 per character (6 narrow + 3 wide x2.5 -> use ratio 3:1)
            const float wideRatio = 3f;
            float totalUnits = 0;
            foreach (char c in data)
            {
                foreach (char e in Patterns[c])
                totalUnits += e == '1' ? wideRatio : 1f;
                totalUnits += 1f; // inter-character gap
            }
            totalUnits -= 1f;

            float unit = area.Width / (totalUnits + 20f);
            if (unit <= 0 || area.Height <= 0)
            return;
            float x = area.X + (area.Width - unit * totalUnits) / 2f;

            using (Brush brush = new SolidBrush(barColor))
            {
                foreach (char c in data)
                {
                    string pattern = Patterns[c];
                    for (int i = 0; i < pattern.Length; i++)
                    {
                        float w = (pattern[i] == '1' ? wideRatio : 1f) * unit;
                        bool isBar = i % 2 == 0;
                        if (isBar)
                        {
                            g.FillRectangle(brush, x, area.Y, w, area.Height);
                        }
                        x += w;
                    }
                    x += unit; // gap between characters
                }
            }
        }
    }
}
