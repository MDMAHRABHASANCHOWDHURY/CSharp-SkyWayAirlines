using System;
using System.Data.SqlClient;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Services;

namespace AirlineReservationDemo.UI
{
    internal enum ButtonKind
    {
        Primary,
        Secondary,
        Danger,
        Ghost
    }

    /// <summary>Factory methods for consistently styled controls, grids and message boxes.</summary>
    internal static class UiKit
    {
        // ------------------------------------------------------------ buttons

        public static Button MakeButton(string text, ButtonKind kind, int width, EventHandler onClick)
        {
            Color back, fore, hover, border;
            switch (kind)
            {
                case ButtonKind.Secondary:
                back = Color.White;
                fore = Theme.Accent;
                hover = Theme.AccentLight;
                border = Theme.Accent;
                break;
                case ButtonKind.Danger:
                back = Theme.Danger;
                fore = Color.White;
                hover = ControlPaint.Dark(Theme.Danger, 0.1f);
                border = Theme.Danger;
                break;
                case ButtonKind.Ghost:
                back = Color.Transparent;
                fore = Theme.Muted;
                hover = Theme.Border;
                border = Color.Transparent;
                break;
                default:
                back = Theme.Accent;
                fore = Color.White;
                hover = Theme.AccentDark;
                border = Theme.Accent;
                break;
            }

            Button b = new Button { Text = text,
                Width = width,
                Height = 40,
                FlatStyle = FlatStyle.Flat,
                BackColor = back,
                ForeColor = fore,
                Font = Theme.Bold,
                Cursor = Cursors.Hand,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter };
            b.FlatAppearance.BorderSize = kind == ButtonKind.Secondary ? 1 : 0;
            b.FlatAppearance.BorderColor = border.A == 255 ? border : Theme.Border;
            b.FlatAppearance.MouseOverBackColor = hover;
            b.FlatAppearance.MouseDownBackColor = hover;
            if (onClick != null)
            b.Click += onClick;
            return b;
        }

        // ------------------------------------------------------------- labels

        public static Label MakeLabel(string text, Font font, Color color)
        {
            return new Label { Text = text, Font = font, ForeColor = color, AutoSize = true,
                BackColor = Color.Transparent };
        }

        // -------------------------------------------------------------- cards

        /// <summary>White panel with a thin border; used as the "card" surface.</summary>
        public static Panel MakeCard()
        {
            Panel p = new Panel { BackColor = Theme.Card, Padding = new Padding(20) };
            p.Paint += delegate(object s, PaintEventArgs e)
            {
                Panel panel = (Panel)s;
                using (Pen pen = new Pen(Theme.Border))
                {
                    e.Graphics.DrawRectangle(pen, 0, 0, panel.Width - 1, panel.Height - 1);
                }
            };
            return p;
        }

        // ------------------------------------------------------------- inputs

        public static TextBox MakeTextBox()
        {
            return new TextBox { Font = Theme.Base, BorderStyle = BorderStyle.FixedSingle, BackColor = Color.White,
                ForeColor = Theme.Text };
        }

        public static ComboBox MakeCombo(params string[] items)
        {
            ComboBox c = new ComboBox { Font = Theme.Base, DropDownStyle = ComboBoxStyle.DropDownList,
                FlatStyle = FlatStyle.Standard, BackColor = Color.White };
            if (items != null && items.Length > 0)
            {
                c.Items.AddRange(items);
                c.SelectedIndex = 0;
            }
            return c;
        }

        public static DateTimePicker MakeDate(string format)
        {
            return new DateTimePicker { Font = Theme.Base, Format = DateTimePickerFormat.Custom,
                CustomFormat = format };
        }

        /// <summary>A small caption above an input. Add to a container with Dock=Top, or a table cell.</summary>
        public static Panel Field(string caption, Control input)
        {
            Panel p = new Panel { Dock = DockStyle.Top, Height = 62, Padding = new Padding(0, 0, 0, 8),
                BackColor = Color.Transparent };
            input.Dock = DockStyle.Top;
            Label l =
            new Label { Text = caption,     Dock = DockStyle.Top,    Height = 22,
                Font = Theme.Small, ForeColor = Theme.Muted, TextAlign = ContentAlignment.BottomLeft };
            p.Controls.Add(input);
            p.Controls.Add(l);
            return p;
        }

        public static void SelectByCode(ComboBox combo, string code)
        {
            for (int i = 0; i < combo.Items.Count; i++)
            {
                var row = combo.Items[i] as System.Data.DataRowView;
                if (row != null && Convert.ToString(row["Code"]).Trim() == (code ?? string.Empty).Trim())
                {
                    combo.SelectedIndex = i;
                    return;
                }
            }
        }
        public static void OnTypingPause(TextBox box, Action reload)
        {
            Timer timer = new Timer { Interval = 350 };
            timer.Tick += delegate
            {
                timer.Stop();
                reload();
            };
            box.TextChanged += delegate
            {
                timer.Stop();
                timer.Start();
            };
            box.Disposed += delegate
            {
                timer.Dispose();
            };
        }
        public static void MoneyColumns(DataGridView g, params string[] names)
        {
            g.CellFormatting += delegate(object sender, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || e.Value == null || e.Value == DBNull.Value ||
                Array.IndexOf(names, g.Columns[e.ColumnIndex].Name) < 0)
                return;
                e.Value = Helpers.Money.Format(Convert.ToDecimal(e.Value));
                e.FormattingApplied = true;
            };
        }
        // -------------------------------------------------------------- grids

        public static void StyleGrid(DataGridView g)
        {
            g.AutoGenerateColumns = false;
            g.AllowUserToAddRows = false;
            g.AllowUserToDeleteRows = false;
            g.AllowUserToResizeRows = false;
            g.ReadOnly = true;
            g.MultiSelect = false;
            g.RowHeadersVisible = false;
            g.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            g.BorderStyle = BorderStyle.None;
            g.BackgroundColor = Color.White;
            g.GridColor = Theme.Border;
            g.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            g.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            g.EnableHeadersVisualStyles = false;
            g.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            g.ColumnHeadersHeight = 40;
            g.RowTemplate.Height = 38;
            g.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            g.Font = Theme.Base;

            g.ColumnHeadersDefaultCellStyle.BackColor = Theme.Navy;
            g.ColumnHeadersDefaultCellStyle.ForeColor = Color.White;
            g.ColumnHeadersDefaultCellStyle.Font = Theme.Bold;
            g.ColumnHeadersDefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.ColumnHeadersDefaultCellStyle.SelectionBackColor = Theme.Navy;

            g.DefaultCellStyle.BackColor = Color.White;
            g.DefaultCellStyle.ForeColor = Theme.Text;
            g.DefaultCellStyle.SelectionBackColor = Theme.AccentLight;
            g.DefaultCellStyle.SelectionForeColor = Theme.Text;
            g.DefaultCellStyle.Padding = new Padding(8, 0, 0, 0);
            g.AlternatingRowsDefaultCellStyle.BackColor = ColorTranslator.FromHtml("#F8FAFD");
        }

        public static DataGridViewTextBoxColumn AddColumn(DataGridView g, string dataProperty, string header,
        float weight, string format)
        {
            DataGridViewTextBoxColumn c =
            new DataGridViewTextBoxColumn { DataPropertyName = dataProperty, Name = dataProperty,
                HeaderText = header, FillWeight = weight,
                SortMode = DataGridViewColumnSortMode.Automatic };
            if (!string.IsNullOrEmpty(format))
            c.DefaultCellStyle.Format = format;
            g.Columns.Add(c);
            return c;
        }

        /// <summary>Colours Confirmed / Cancelled / Delayed text in a status column.</summary>
        public static void ColorStatus(DataGridView g, string columnName)
        {
            g.CellFormatting += delegate(object s, DataGridViewCellFormattingEventArgs e)
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0 || g.Columns[e.ColumnIndex].Name != columnName)
                return;
                string v = Convert.ToString(e.Value);
                if (v == "Confirmed" || v == "Scheduled")
                e.CellStyle.ForeColor = Theme.Success;
                else if (v == "Cancelled")
                e.CellStyle.ForeColor = Theme.Danger;
                else if (v == "Delayed")
                e.CellStyle.ForeColor = Theme.Orange;
                e.CellStyle.Font = Theme.Bold;
            };
        }

        // ------------------------------------------------------ message boxes

        public static void Info(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "SkyWay Airlines", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        public static void Warn(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Please check", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        public static void Error(IWin32Window owner, string message)
        {
            MessageBox.Show(owner, message, "Something went wrong", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        public static bool Confirm(IWin32Window owner, string message)
        {
            return MessageBox.Show(owner, message, "Please confirm", MessageBoxButtons.YesNo,
            MessageBoxIcon.Question) == DialogResult.Yes;
        }

        /// <summary>
        /// Runs an action and turns known failures into friendly messages instead of crashes.
        /// Returns true when the action completed.
        /// </summary>
        public static bool Guard(IWin32Window owner, Action action)
        {
            try
            {
                action();
                return true;
            }
            catch (ArgumentException ex) { Warn(owner, ex.Message); }
            catch (UnauthorizedAccessException ex) { Warn(owner, ex.Message); }
            catch (AuthException ex)
            {
                Warn(owner, ex.Message);
            }
            catch (BookingException ex)
            {
                Warn(owner, ex.Message);
            }
            catch (SqlException ex)
            {
                if (Data.Db.IsForeignKeyViolation(ex))
                Warn(owner,
                "This record is still used by other data (for example bookings), so it cannot be deleted.");
                else if (Data.Db.IsDuplicateKey(ex))
                Warn(owner, "A record with the same unique value already exists.");
                else
                Error(owner, "Database error: " + ex.Message + "\n\nFor source builds, edit App.config and rebuild; for an EXE, edit AirlineReservationDemo.exe.config beside it and restart.");
            }
            catch (Exception ex)
            {
                Error(owner, ex.Message);
            }
            return false;
        }
    }
}
