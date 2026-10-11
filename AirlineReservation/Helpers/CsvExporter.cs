using System;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
namespace AirlineReservationDemo.Helpers
{
    internal static class CsvExporter
    {
        internal static string Escape(object value)
        {
            string text = Convert.ToString(value);
            // Avoid spreadsheet formula evaluation when opening an exported CSV.
            if (text.TrimStart().Length > 0 && "=+-@".IndexOf(text.TrimStart()[0]) >= 0)
            text = "'" + text;
            return "\"" + text.Replace("\"", "\"\"") + "\"";
        }
        public static void Save(DataTable data, string path)
        {
            using (var writer = new StreamWriter(path, false, new UTF8Encoding(true)))
            {
                writer.WriteLine(string.Join(",", data.Columns.Cast<DataColumn>().Select(c => Escape(c.ColumnName))));
                foreach (DataRow row in data.Rows)
                writer.WriteLine(string.Join(",", row.ItemArray.Select(Escape)));
            }
        }
    }
}
