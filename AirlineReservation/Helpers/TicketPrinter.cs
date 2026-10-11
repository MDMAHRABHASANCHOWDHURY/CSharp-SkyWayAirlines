using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Printing;
using System.IO;
using System.Windows.Forms;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo.Helpers
{
    /// <summary>Print preview / print-to-PDF and PNG export for e-tickets.</summary>
    internal static class TicketPrinter
    {
        /// <summary>
        /// Opens print preview. Use the separate Print / Save as PDF action to choose a PDF printer
        /// (for example "Microsoft Print to PDF") to save the ticket as a PDF.
        /// </summary>
        public static void Preview(IWin32Window owner, IList<TicketData> tickets)
        {
            if (tickets == null || tickets.Count == 0)
            return;
            using (PrintDocument doc = CreateDocument(tickets)) using (
            PrintPreviewDialog dialog = new PrintPreviewDialog { Document = doc, Width = 1100, Height = 780 })
            dialog.ShowDialog(owner);
        }

        public static void Print(IWin32Window owner, IList<TicketData> tickets)
        {
            if (tickets == null || tickets.Count == 0)
            return;
            using (PrintDocument doc = CreateDocument(tickets)) using (PrintDialog dialog = new PrintDialog {
                Document = doc, UseEXDialog = true
            }) if (dialog.ShowDialog(owner) == DialogResult.OK) doc.Print();
        }

        private static PrintDocument CreateDocument(IList<TicketData> tickets)
        {
            int index = 0;
            PrintDocument doc = new PrintDocument { DocumentName = "E-Ticket " + tickets[0].Pnr };
            doc.BeginPrint += delegate
            {
                index = 0;
            };
            doc.DefaultPageSettings.Landscape = true;
            doc.DefaultPageSettings.Margins = new Margins(40, 40, 40, 40);

            doc.PrintPage += delegate(object s, PrintPageEventArgs e)
            {
                using (Bitmap bmp = TicketRenderer.Render(tickets[index], 2f))
                {
                    RectangleF area = e.MarginBounds;
                    float ratio = Math.Min(area.Width / bmp.Width, area.Height / bmp.Height);
                    float w = bmp.Width * ratio, h = bmp.Height * ratio;
                    e.Graphics.DrawImage(bmp, area.Left + (area.Width - w) / 2f, area.Top, w, h);
                }
                index++;
                e.HasMorePages = index < tickets.Count;
            };

            return doc;
        }

        /// <summary>Saves a ticket as a high-resolution PNG image chosen by the user.</summary>
        public static bool SavePng(IWin32Window owner, TicketData ticket)
        {
            using (SaveFileDialog dlg = new SaveFileDialog())
            {
                dlg.Filter = "PNG image (*.png)|*.png";
                dlg.FileName = "E-Ticket_" + ticket.Pnr + "_" + ticket.Seat + ".png";
                if (dlg.ShowDialog(owner) != DialogResult.OK)
                return false;

                using (Bitmap bmp = TicketRenderer.Render(ticket, 2f))
                {
                    bmp.Save(dlg.FileName, System.Drawing.Imaging.ImageFormat.Png);
                }
                return true;
            }
        }
    }
}
