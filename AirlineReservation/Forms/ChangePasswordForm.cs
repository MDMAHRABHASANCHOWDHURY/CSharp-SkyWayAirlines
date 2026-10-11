using System;
using System.Drawing;
using System.Windows.Forms;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    internal sealed class ChangePasswordForm : Form
    {
        private readonly TextBox current=new TextBox {UseSystemPasswordChar=true,MaxLength=128};
        private readonly TextBox password=new TextBox {UseSystemPasswordChar=true,MaxLength=128};
        private readonly TextBox confirm=new TextBox {UseSystemPasswordChar=true,MaxLength=128};
        public ChangePasswordForm()
        {
            Text="Set your new password";Font=Theme.Base;BackColor=Color.White;
            ClientSize=new Size(470,360);FormBorderStyle=FormBorderStyle.FixedDialog;
            StartPosition=FormStartPosition.CenterParent;MaximizeBox=false;MinimizeBox=false;
            Label note=UiKit.MakeLabel("Replace your temporary password before continuing.",Theme.Small,Theme.Muted);
            note.Location=new Point(24,20);Controls.Add(note);
            AddField("Current / temporary password",current,60);
            AddField("New password (8+ characters, letters and numbers)",password,130);
            AddField("Confirm new password",confirm,200);
            Button save=UiKit.MakeButton("Save password",ButtonKind.Primary,200,delegate {
                if(password.Text!=confirm.Text) {UiKit.Warn(this,"The passwords do not match.");return;}
                if(UiKit.Guard(this,delegate {AccountService.ChangeOwnPassword(current.Text,password.Text);}))
                {current.Clear();password.Clear();confirm.Clear();DialogResult=DialogResult.OK;Close();}
            });save.Location=new Point(24,286);Controls.Add(save);AcceptButton=save;
        }
        private void AddField(string caption,Control control,int y)
        {
            Label label=UiKit.MakeLabel(caption,Theme.Small,Theme.Muted);label.Location=new Point(24,y);Controls.Add(label);
            control.SetBounds(24,y+24,420,28);Controls.Add(control);
        }
    }
}
