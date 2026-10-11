using System;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    internal sealed class MyProfile : PassengerShell
    {
        private readonly TextBox name=new TextBox{MaxLength=100};
        private readonly TextBox email=new TextBox{MaxLength=120};
        private readonly TextBox phone=new TextBox{MaxLength=25};
        private readonly TextBox nationality=new TextBox{MaxLength=60};
        private readonly TextBox passport=new TextBox{MaxLength=12};
        private readonly TextBox address=new TextBox{MaxLength=200};
        private readonly ComboBox gender=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList};
        private readonly DateTimePicker dob=new DateTimePicker{Format=DateTimePickerFormat.Short,MinDate=DateTime.Today.AddYears(-120),MaxDate=DateTime.Today.AddYears(-12)};
        private readonly PictureBox picture=new PictureBox{SizeMode=PictureBoxSizeMode.Zoom,BackColor=Theme.AccentLight};
        private readonly DataGridView requests=new DataGridView();
        private readonly TextBox reason=new TextBox{MaxLength=500};
        private byte[] photo;
        private bool loaded;
        public MyProfile():base("My profile","Update your details, photo and password requests","profile")
        {
            Panel page=new Panel{Dock=DockStyle.Fill,AutoScroll=true,AutoScrollMinSize=new Size(780,910)};Body.Controls.Add(page);
            picture.SetBounds(24,20,130,130);page.Controls.Add(picture);
            Button upload=UiKit.MakeButton("Choose photo",ButtonKind.Secondary,160,delegate {ChoosePhoto();});upload.Location=new Point(180,30);page.Controls.Add(upload);
            Button remove=UiKit.MakeButton("Remove photo",ButtonKind.Ghost,160,delegate {photo=null;SetPicture(null);});remove.Location=new Point(180,80);page.Controls.Add(remove);
            Label photoNote=UiKit.MakeLabel("JPG or PNG, up to 2 MB. Save to apply your changes.",Theme.Small,Theme.Muted);photoNote.Location=new Point(24,166);page.Controls.Add(photoNote);
            gender.Items.AddRange(new object[]{"Male","Female","Other"});gender.SelectedIndex=0;
            Field(page,"Full name",name,24,205);Field(page,"Email",email,400,205);
            Field(page,"Mobile phone",phone,24,280);Field(page,"Nationality",nationality,400,280);
            Field(page,"Gender",gender,24,355);Field(page,"Date of birth",dob,400,355);
            Field(page,"Passport number",passport,24,430);Field(page,"Address",address,400,430);
            Button save=UiKit.MakeButton("Save profile",ButtonKind.Primary,180,delegate {Save();});save.Location=new Point(24,510);page.Controls.Add(save);
            Label title=UiKit.MakeLabel("Request a password reset",Theme.Subtitle,Theme.Text);title.Location=new Point(24,580);page.Controls.Add(title);
            Label note=UiKit.MakeLabel("Explain your request. Do not enter a password here.",Theme.Small,Theme.Muted);note.Location=new Point(24,612);page.Controls.Add(note);
            reason.SetBounds(24,645,480,30);page.Controls.Add(reason);
            Button send=UiKit.MakeButton("Send request",ButtonKind.Primary,180,delegate {
                if(UiKit.Guard(this,delegate {AccountService.RequestReset(reason.Text);})) {reason.Clear();LoadRequests();UiKit.Warn(this,"Your request was sent to the administrator.");}
            });send.Location=new Point(520,641);page.Controls.Add(send);
            UiKit.StyleGrid(requests);requests.AutoGenerateColumns=true;requests.ReadOnly=true;requests.AllowUserToAddRows=false;
            requests.SetBounds(24,700,720,165);page.Controls.Add(requests);
            Button refresh=UiKit.MakeButton("Refresh requests",ButtonKind.Secondary,180,delegate {LoadRequests();});refresh.Location=new Point(520,575);page.Controls.Add(refresh);
            Shown+=delegate {LoadProfile();LoadRequests();};
        }
        private static void Field(Control parent,string caption,Control control,int x,int y)
        {
            Label label=UiKit.MakeLabel(caption,Theme.Small,Theme.Muted);label.Location=new Point(x,y);parent.Controls.Add(label);
            control.SetBounds(x,y+25,340,30);parent.Controls.Add(control);
        }
        private void LoadProfile()
        {
            loaded=UiKit.Guard(this,delegate {
                DataRow r=AccountService.Profile();name.Text=Convert.ToString(r["PassName"]);email.Text=Convert.ToString(r["PassEmail"]);
                phone.Text=Convert.ToString(r["PassPhone"]);nationality.Text=Convert.ToString(r["PassNat"]);
                address.Text=Convert.ToString(r["PassAddress"]);passport.Text=Convert.ToString(r["PassportNo"]);
                gender.SelectedItem=Convert.ToString(r["PassGender"]);if(gender.SelectedIndex<0)gender.SelectedIndex=2;
                if(r["DateOfBirth"]!=DBNull.Value) {DateTime date=Convert.ToDateTime(r["DateOfBirth"]);dob.Value=date<dob.MinDate?dob.MinDate:date>dob.MaxDate?dob.MaxDate:date;}
                photo=r["ProfilePhoto"]==DBNull.Value?null:(byte[])r["ProfilePhoto"];SetPicture(photo);
            });
        }
        private void LoadRequests() {UiKit.Guard(this,delegate {requests.DataSource=AccountService.MyRequests();});}
        private void Save()
        {
            if(!loaded){UiKit.Warn(this,"Profile did not load. Reopen this screen before saving.");return;}
            if(UiKit.Guard(this,delegate {AccountService.SaveProfile(new RegistrationRequest{FullName=name.Text,Email=email.Text,Phone=phone.Text,Nationality=nationality.Text,
                Address=address.Text,PassportNo=passport.Text,Gender=Convert.ToString(gender.SelectedItem),DateOfBirth=dob.Value},photo);}))
            {SetTitle("My profile","Profile saved — "+Session.Current.FullName);UiKit.Warn(this,"Your profile has been saved.");}
        }
        private void ChoosePhoto()
        {
            using(var dialog=new OpenFileDialog{Filter="Profile pictures|*.jpg;*.jpeg;*.png",CheckFileExists=true})
            {
                if(dialog.ShowDialog(this)!=DialogResult.OK)return;
                try
                {
                    if(new FileInfo(dialog.FileName).Length>2*1024*1024)throw new ArgumentException("Choose a picture under 2 MB.");
                    byte[] data=File.ReadAllBytes(dialog.FileName);
                    using(var stream=new MemoryStream(data))using(var image=Image.FromStream(stream,false,true))
                    {
                        if(image.Width>8000 || image.Height>8000 || (long)image.Width*image.Height>16000000)throw new ArgumentException("Choose an image no larger than 16 megapixels.");
                        int w=Math.Max(1,(int)(image.Width*Math.Min(1d,512d/Math.Max(image.Width,image.Height))));
                        int h=Math.Max(1,(int)(image.Height*Math.Min(1d,512d/Math.Max(image.Width,image.Height))));
                        using(var bitmap=new Bitmap(w,h))using(var g=Graphics.FromImage(bitmap))using(var output=new MemoryStream())
                        {g.Clear(Color.White);g.InterpolationMode=InterpolationMode.HighQualityBicubic;g.DrawImage(image,0,0,w,h);bitmap.Save(output,ImageFormat.Jpeg);photo=output.ToArray();}
                    }
                    SetPicture(photo);
                }
                catch(Exception ex) when(ex is ArgumentException || ex is IOException || ex is UnauthorizedAccessException || ex is OutOfMemoryException || ex is System.Runtime.InteropServices.ExternalException)
                {UiKit.Warn(this,"Cannot use this picture. "+ex.Message);}
            }
        }
        private void SetPicture(byte[] data)
        {
            Image replacement=null;
            if(data!=null)using(var stream=new MemoryStream(data))using(var image=Image.FromStream(stream))replacement=new Bitmap(image);
            Image old=picture.Image;picture.Image=replacement;if(old!=null)old.Dispose();
        }
        protected override void Dispose(bool disposing)
        {if(disposing && picture.Image!=null){picture.Image.Dispose();picture.Image=null;}base.Dispose(disposing);}
    }
}
