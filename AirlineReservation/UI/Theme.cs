using System.Drawing;

namespace AirlineReservationDemo.UI
{
    internal static class Theme
    {
        public static readonly Color Navy =
            ColorTranslator.FromHtml("#0B2545");

        public static readonly Color Navy2 =
            ColorTranslator.FromHtml("#13315C");

        public static readonly Color Accent =
            ColorTranslator.FromHtml("#1E88E5");

        public static readonly Color AccentDark =
            ColorTranslator.FromHtml("#1565C0");

        public static readonly Color AccentLight =
            ColorTranslator.FromHtml("#E3F2FD");

        public static readonly Color Orange =
            ColorTranslator.FromHtml("#F57C00");

        public static readonly Color Success =
            ColorTranslator.FromHtml("#2E7D32");

        public static readonly Color SuccessLight =
            ColorTranslator.FromHtml("#E8F5E9");

        public static readonly Color Danger =
            ColorTranslator.FromHtml("#C62828");

        public static readonly Color DangerLight =
            ColorTranslator.FromHtml("#FFEBEE");

        public static readonly Color Background =
            ColorTranslator.FromHtml("#F3F6FB");

        public static readonly Color Card = Color.White;

        public static readonly Color Border =
            ColorTranslator.FromHtml("#DDE4EE");

        public static readonly Color Text =
            ColorTranslator.FromHtml("#1B2733");

        public static readonly Color Muted =
            ColorTranslator.FromHtml("#6B7A8C");

        public static readonly Color SidebarText =
            ColorTranslator.FromHtml("#C9D6EA");

        private const string Family = "Segoe UI";

        public static readonly Font Base =
            new Font(Family, 11f, FontStyle.Regular, GraphicsUnit.Point);

        public static readonly Font Small =
            new Font(Family, 10f, FontStyle.Regular, GraphicsUnit.Point);

        public static readonly Font Bold =
            new Font(Family, 11f, FontStyle.Bold, GraphicsUnit.Point);

        public static readonly Font Subtitle =
            new Font(Family, 12f, FontStyle.Bold, GraphicsUnit.Point);

        public static readonly Font Title =
            new Font(Family, 20f, FontStyle.Bold, GraphicsUnit.Point);

        public static readonly Font Display =
            new Font(Family, 30f, FontStyle.Bold, GraphicsUnit.Point);

        public static readonly Font Stat =
            new Font(Family, 22f, FontStyle.Bold, GraphicsUnit.Point);

        public static readonly Font Mono =
            new Font("Consolas", 11f, FontStyle.Bold, GraphicsUnit.Point);
    }
}