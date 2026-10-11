using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.UI;
namespace AirlineReservationDemo
{
    internal abstract class AdminListForm : AdminShell
    {
        protected readonly DataGridView Grid = new DataGridView();
        protected readonly TextBox SearchBox = UiKit.MakeTextBox();
        protected readonly FlowLayoutPanel Actions = new FlowLayoutPanel();
        protected AdminListForm(string title, string subtitle, string page) : base(title, subtitle, page)
        {
            Panel card = UiKit.MakeCard();
            card.Dock = DockStyle.Fill;
            card.Padding = new Padding(0);
            Panel tools = new Panel { Dock = DockStyle.Top, Height = 114, Padding = new Padding(16) };
            SearchBox.Width = 240;
            SearchBox.Location = new Point(16, 14);
            Actions.Dock = DockStyle.Bottom;
            Actions.Height = 55;
            Actions.FlowDirection = FlowDirection.RightToLeft;
            Actions.Padding = new Padding(0, 0, 6, 0);
            Actions.WrapContents = false;
            tools.Controls.Add(SearchBox);
            tools.Controls.Add(Actions);
            UiKit.StyleGrid(Grid);
            Grid.Dock = DockStyle.Fill;
            card.Controls.Add(Grid);
            card.Controls.Add(tools);
            Body.Controls.Add(card);
            UiKit.OnTypingPause(SearchBox, Reload);
            Shown += delegate
            {
                Reload();
            };
        }
        protected DataRow Selected
        {
            get {
                var x = Grid.CurrentRow == null ? null : Grid.CurrentRow.DataBoundItem as DataRowView;
                return x == null ? null : x.Row;
            }
        }
        protected void ActionButton(string label, ButtonKind kind, EventHandler handler)
        {
            Actions.Controls.Add(UiKit.MakeButton(label, kind, 115, handler));
        }
        protected abstract void Reload();
    }
    internal abstract class EditorForm : BaseForm
    {
        protected readonly TableLayoutPanel Fields = new TableLayoutPanel();
        private int index;
        protected EditorForm(string title)
        {
            Text = title;
            MinimumSize = new Size(620, 560);
            ClientSize = new Size(650, 620);
            MaximizeBox = false;
            MinimizeBox = false;
            Fields.Dock = DockStyle.Fill;
            Fields.ColumnCount = 2;
            Fields.Padding = new Padding(20);
            Fields.AutoScroll = true;
            Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            Fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            FlowLayoutPanel buttons =
            new FlowLayoutPanel { Dock = DockStyle.Bottom, Height = 65, Padding = new Padding(20, 8, 20, 8),
                FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(UiKit.MakeButton("Save changes", ButtonKind.Primary, 150, delegate {
                if (UiKit.Guard(this, Save))
                {
                    DialogResult = DialogResult.OK;
                    Close();
                }
            }));
            Button cancel = UiKit.MakeButton("Cancel", ButtonKind.Ghost, 90, delegate { Close(); });
            cancel.DialogResult = DialogResult.Cancel;
            buttons.Controls.Add(cancel);
            CancelButton = cancel;
            Controls.Add(Fields);
            Controls.Add(buttons);
        }
        protected void Field(string caption, Control input)
        {
            int row = index / 2;
            if (index % 2 == 0)
            {
                Fields.RowCount = row + 1;
                Fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 70));
            }
            Panel field = UiKit.Field(caption, input);
            field.Dock = DockStyle.Fill;
            field.Margin = new Padding(5);
            Fields.Controls.Add(field, index % 2, row);
            index++;
        }
        protected abstract void Save();
    }
}
