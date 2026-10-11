using System;
using System.Data;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    internal sealed class AccountSecurity : AdminShell
    {
        private readonly DataGridView grid=new DataGridView();
        public AccountSecurity():base("Account security","Review passenger requests or reset a password at any time","security")
        {
            Panel actions=new Panel{Dock=DockStyle.Top,Height=70};
            Button reset=UiKit.MakeButton("Reset password",ButtonKind.Primary,180,delegate {Reset();});reset.Location=new Point(0,8);actions.Controls.Add(reset);
            Button reject=UiKit.MakeButton("Reject request",ButtonKind.Secondary,180,delegate {Reject();});reject.Location=new Point(194,8);actions.Controls.Add(reject);
            Button refresh=UiKit.MakeButton("Refresh",ButtonKind.Secondary,120,delegate {LoadData();});refresh.Location=new Point(388,8);actions.Controls.Add(refresh);
            UiKit.StyleGrid(grid);grid.Dock=DockStyle.Fill;grid.AutoGenerateColumns=true;grid.ReadOnly=true;grid.MultiSelect=false;grid.SelectionMode=DataGridViewSelectionMode.FullRowSelect;grid.AllowUserToAddRows=false;
            Body.Controls.Add(grid);Body.Controls.Add(actions);Shown+=delegate {LoadData();};
        }
        private void LoadData(){UiKit.Guard(this,delegate {grid.DataSource=AccountService.Accounts();});}
        private DataRow Selected()
        {
            var row=grid.CurrentRow==null?null:grid.CurrentRow.DataBoundItem as DataRowView;
            return row==null?null:row.Row;
        }
        private void Reject()
        {
            DataRow r=Selected();if(r==null || r["RequestId"]==DBNull.Value || Convert.ToString(r["Status"])!="Pending") {UiKit.Warn(this,"Select a pending request.");return;}
            if(MessageBox.Show(this,"Reject this password request?","Review request",MessageBoxButtons.YesNo,MessageBoxIcon.Question)!=DialogResult.Yes)return;
            if(UiKit.Guard(this,delegate {AccountService.RejectRequest(Convert.ToInt32(r["RequestId"]));}))LoadData();
        }
        private void Reset()
        {
            DataRow r=Selected();if(r==null){UiKit.Warn(this,"Select a passenger account.");return;}
            int id=Convert.ToInt32(r["id"]);
            using(var dialog=new Form{Text="Reset password — "+Convert.ToString(r["username"]),Font=Theme.Base,ClientSize=new Size(490,300),StartPosition=FormStartPosition.CenterParent,FormBorderStyle=FormBorderStyle.FixedDialog,MaximizeBox=false,MinimizeBox=false})
            {
                Label note=UiKit.MakeLabel("Give the temporary password to the passenger privately.\nThey must replace it at their next sign-in.",Theme.Small,Theme.Muted);note.Location=new Point(24,20);dialog.Controls.Add(note);
                Label label=UiKit.MakeLabel("Temporary password (8+ characters, letters and numbers)",Theme.Small,Theme.Muted);label.Location=new Point(24,80);dialog.Controls.Add(label);
                TextBox password=new TextBox{UseSystemPasswordChar=true,MaxLength=128};password.SetBounds(24,108,440,28);dialog.Controls.Add(password);
                Label confirmLabel=UiKit.MakeLabel("Confirm temporary password",Theme.Small,Theme.Muted);confirmLabel.Location=new Point(24,150);dialog.Controls.Add(confirmLabel);
                TextBox confirm=new TextBox{UseSystemPasswordChar=true,MaxLength=128};confirm.SetBounds(24,178,440,28);dialog.Controls.Add(confirm);
                Button save=UiKit.MakeButton("Reset password",ButtonKind.Primary,180,delegate {
                    if(password.Text!=confirm.Text){UiKit.Warn(dialog,"The passwords do not match.");return;}
                    if(UiKit.Guard(dialog,delegate {AccountService.ResetPassword(id,password.Text);})) {password.Clear();confirm.Clear();dialog.DialogResult=DialogResult.OK;dialog.Close();}
                });save.Location=new Point(24,238);dialog.Controls.Add(save);dialog.AcceptButton=save;
                if(dialog.ShowDialog(this)==DialogResult.OK){LoadData();UiKit.Warn(this,"Password reset saved. The passenger must change it at next sign-in.");}
            }
        }
    }
}
