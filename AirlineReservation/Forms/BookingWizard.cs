using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Linq;
using System.Windows.Forms;
using AirlineReservationDemo.Entities;
using AirlineReservationDemo.Helpers;
using AirlineReservationDemo.Services;
using AirlineReservationDemo.UI;

namespace AirlineReservationDemo
{
    /// <summary>
    /// Four-step booking dialog: 1 Travellers, 2 Seats, 3 Review and pay, 4 Confirmation.
    /// <see cref="CreatedBookingId"/> is greater than zero once the booking has been paid.
    /// </summary>
    internal class BookingWizard : BaseForm
    {
        private const int StepTravellers = 0, StepSeats = 1, StepPayment = 2, StepDone = 3;

        private readonly FlightInfo flight;
        private readonly string cabin;
        private readonly int count;

        private int step;
        private List<TravellerInput> travellers = new List<TravellerInput>();
        private PriceQuote quote;
        private BookingResult booking;

        // Chrome
        private readonly Panel[] stepPanels = new Panel[4];

        private readonly Label[] railSteps = new Label[4];

        private readonly Label railTotal = new Label
        {
            Text = "-",
            Font = Theme.Subtitle,
            ForeColor = Color.White,
            BackColor = Color.Transparent,
            AutoSize = false,
            AutoEllipsis = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Size = new Size(260, 36)
        };

        private Button backButton;
        private Button nextButton;
        private Button cancelButton;

        // step 1: travellers
        private readonly TextBox emailTB = UiKit.MakeTextBox();
        private readonly TextBox phoneTB = UiKit.MakeTextBox();
        private TextBox[] nameTB;
        private DateTimePicker[] dobPicker;
        private ComboBox[] genderCB;
        private TextBox[] passportTB;
        private ComboBox[] baggageCB;

        // step 2: seats
        private readonly SeatMapControl seatMap = new SeatMapControl();
        private readonly Dictionary<int, string> seatByTraveller = new Dictionary<int, string>();
        private List<Seat> allSeats;
        private RadioButton[] travellerRadios;
        private int currentTraveller;

        // step 3: payment
        private readonly DataGridView reviewGrid = new DataGridView();
        private readonly Label baseLabel = UiKit.MakeLabel("-", Theme.Base, Theme.Text);
        private readonly Label bagLabel = UiKit.MakeLabel("-", Theme.Base, Theme.Text);
        private readonly Label taxLabel = UiKit.MakeLabel("-", Theme.Base, Theme.Text);
        private readonly Label totalLabel = UiKit.MakeLabel("-", Theme.Subtitle, Theme.Navy);
        private readonly TextBox cardNameTB = UiKit.MakeTextBox();
        private readonly TextBox cardNumberTB = UiKit.MakeTextBox();
        private readonly TextBox cardExpiryTB = UiKit.MakeTextBox();
        private readonly TextBox cardCvvTB = UiKit.MakeTextBox();
        private readonly CheckBox agreeCheck = new CheckBox();
        private bool formatting;
        private bool seatDataReady;

        // step 4: confirmation
        private readonly Label pnrLabel =
        UiKit.MakeLabel("------", new Font("Consolas", 40f, FontStyle.Bold), Theme.Navy);
        private readonly Label doneDetail = UiKit.MakeLabel(string.Empty, Theme.Base, Theme.Muted);

        public int CreatedBookingId { get; private set; }

        public BookingWizard(FlightInfo flight, string cabin, int travellerCount)
        {
            if(flight==null) throw new ArgumentNullException("flight");
            if(travellerCount<1 || travellerCount>BookingService.MaxTravellers) throw new ArgumentException("Choose 1–9 travellers.");
            if(!Session.IsSignedIn) throw new UnauthorizedAccessException("Please sign in.");
            this.flight = flight;
            this.cabin = cabin;
            this.count = travellerCount;

            SuspendLayout();
            ClientSize = new Size(1120, 680);
            MinimumSize = new Size(1120, 680);
            StartPosition = FormStartPosition.CenterParent;
            ShowInTaskbar = false;
            Text = "Book flight " + flight.FlightNo;

            BuildTravellerInputs();
            Panel host = new Panel { Dock = DockStyle.Fill, BackColor = Theme.Background,
                Padding = new Padding(24, 20, 24, 12) };
            stepPanels[StepTravellers] = BuildTravellersStep();
            stepPanels[StepSeats] = BuildSeatsStep();
            stepPanels[StepPayment] = BuildPaymentStep();
            stepPanels[StepDone] = BuildDoneStep();
            for (int i = stepPanels.Length - 1; i >= 0; i--)
            {
                stepPanels[i].Dock = DockStyle.Fill;
                host.Controls.Add(stepPanels[i]);
            }

            Panel footer = BuildFooter();
            Panel rail = BuildRail();

            Controls.Add(host);
            Controls.Add(footer);
            Controls.Add(rail);
            ResumeLayout(false);

            Prefill();
            Shown += delegate
            {
                GoToStep(StepTravellers);
            };
        }

        // =================================================================== chrome

        private Panel BuildRail()
        {
            Panel rail = new Panel { Dock = DockStyle.Left, Width = 290, BackColor = Theme.Navy,
                Padding = new Padding(26, 24, 20, 24) };

            Label caption = UiKit.MakeLabel("YOUR FLIGHT", Theme.Bold, Theme.Orange);
            caption.Location = new Point(26, 26);
            Label route = UiKit.MakeLabel(flight.Src + "  \u2192  " + flight.Dest,
            new Font(Theme.Base.FontFamily, 20f, FontStyle.Bold), Color.White);
            route.Location = new Point(24, 52);
            Label cities = UiKit.MakeLabel(flight.SrcCity + " to " + flight.DestCity, Theme.Base, Theme.SidebarText);
            cities.Location = new Point(27, 94);

            Label facts = UiKit.MakeLabel(
            flight.Airline + " \u00B7 " + flight.FlightNo + "\n" + flight.Departure.ToString("ddd, dd MMM yyyy") +
            "\nDeparts " + flight.Departure.ToString("HH:mm") + "   Arrives " +
            flight.Arrival.ToString("HH:mm") + "\nDuration " + FlightInfo.FormatDuration(flight.Duration) +
            "\n" + cabin + " \u00B7 " + count + (count == 1 ? " traveller" : " travellers"),
            Theme.Base, Color.White);
            facts.Location = new Point(27, 128);
            facts.AutoSize = false;
            facts.Size = new Size(250, 120);

            Label stepsCaption = UiKit.MakeLabel("BOOKING STEPS", Theme.Bold, Theme.Orange);
            stepsCaption.Location = new Point(26, 282);
            string[] names = { "1   Travellers", "2   Seats", "3   Review & pay", "4   Confirmation" };
            for (int i = 0; i < 4; i++)
            {
                railSteps[i] = UiKit.MakeLabel(names[i], Theme.Base, Theme.SidebarText);
                railSteps[i].Location = new Point(27, 314 + i * 36);
                rail.Controls.Add(railSteps[i]);
            }

            Label totalCaption = UiKit.MakeLabel("ESTIMATED TOTAL", Theme.Small, Theme.SidebarText);
            totalCaption.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            totalCaption.Location = new Point(27, 0);
            railTotal.Anchor = AnchorStyles.Left | AnchorStyles.Bottom;
            railTotal.Location = new Point(25, 22);
            rail.Resize+=delegate {totalCaption.Top=Math.Max(474,rail.ClientSize.Height-108);railTotal.Top=totalCaption.Top+24;};

            rail.Controls.AddRange(
            new Control[] { caption, route, cities, facts, stepsCaption, totalCaption, railTotal });
            return rail;
        }

        private Panel BuildFooter()
        {
            Panel footer = new Panel { Dock = DockStyle.Bottom, Height = 76, BackColor = Color.White };
            footer.Paint += delegate(object s, PaintEventArgs e) {
                using (Pen p = new Pen(Theme.Border)) { e.Graphics.DrawLine(p, 0, 0, footer.Width, 0);
                }
            };

            cancelButton = UiKit.MakeButton("Cancel booking", ButtonKind.Ghost, 150, delegate { OnCancel(); });
            cancelButton.Location = new Point(20, 18);
            backButton = UiKit.MakeButton("Back", ButtonKind.Secondary, 110, delegate {
                if (step > 0 && step < StepDone)
                GoToStep(step - 1);
            });
            backButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            nextButton = UiKit.MakeButton("Continue", ButtonKind.Primary, 220, delegate { OnNext(); });
            nextButton.Height = 44;
            nextButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;

            footer.Controls.AddRange(new Control[] { cancelButton, backButton, nextButton });
            footer.Resize += delegate
            {
                nextButton.Location = new Point(footer.Width - nextButton.Width - 24, 16);
                backButton.Location = new Point(nextButton.Left - backButton.Width - 12, 18);
            };
            return footer;
        }

        private void GoToStep(int newStep)
        {
            step = newStep;
            nextButton.Enabled=true;
            for (int i = 0; i < stepPanels.Length; i++)
            stepPanels[i].Visible = i == step;

            for (int i = 0; i < railSteps.Length; i++)
            {
                railSteps[i].ForeColor =
                i == step ? Color.White : (i < step ? Color.FromArgb(150, 220, 170) : Theme.SidebarText);
                railSteps[i].Font = i == step ? Theme.Subtitle : Theme.Base;
            }

            backButton.Visible = step == StepSeats || step == StepPayment;
            cancelButton.Visible = step != StepDone;
            switch (step)
            {
                case StepTravellers:
                nextButton.Text = "Continue to seats";
                break;
                case StepSeats:
                nextButton.Text = "Continue to payment";
                OnEnterSeats();
                break;
                case StepPayment:
                OnEnterPayment();
                break;
                default:
                nextButton.Text = "Done";
                break;
            }
        }

        private void OnNext()
        {
            switch (step)
            {
                case StepTravellers:
                if (CollectTravellers())
                GoToStep(StepSeats);
                break;
                case StepSeats:
                if(!seatDataReady) {UiKit.Warn(this,"Reload the seat map before continuing.");return;}
                if (seatByTraveller.Count < count)
                {
                    UiKit.Warn(this, "Please choose a seat for every traveller.");
                    return;
                }
                GoToStep(StepPayment);
                break;
                case StepPayment:
                Pay();
                break;
                default:
                DialogResult = DialogResult.OK;
                Close();
                break;
            }
        }

        private void OnCancel()
        {
            if (UiKit.Confirm(this, "Cancel this booking? Nothing has been charged."))
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        }

        // ============================================================ step 1: travellers

        private void BuildTravellerInputs()
        {
            nameTB = new TextBox[count];
            dobPicker = new DateTimePicker[count];
            genderCB = new ComboBox[count];
            passportTB = new TextBox[count];
            baggageCB = new ComboBox[count];

            string[] bagItems =
            PricingService.BaggageOptions
            .Select(kg => kg == 0 ? "No extra baggage"
            : "+" + kg + " kg  (+" + Money.Format(PricingService.BaggagePrice(kg)) + ")")
            .ToArray();

            for (int i = 0; i < count; i++)
            {
                nameTB[i] = UiKit.MakeTextBox();
                nameTB[i].MaxLength = 100;
                dobPicker[i] = UiKit.MakeDate("dd MMM yyyy");
                dobPicker[i].MaxDate = DateTime.Today;
                dobPicker[i].MinDate = DateTime.Today.AddYears(-120);
                dobPicker[i].Value = new DateTime(1990, 1, 1);
                dobPicker[i].ValueChanged += delegate
                {
                    UpdateEstimate();
                };
                genderCB[i] = UiKit.MakeCombo("Male", "Female", "Other");
                passportTB[i] = UiKit.MakeTextBox();
                passportTB[i].MaxLength = 12;
                baggageCB[i] = UiKit.MakeCombo(bagItems);
                baggageCB[i].SelectedIndexChanged += delegate
                {
                    UpdateEstimate();
                };
            }
            emailTB.MaxLength = 120;
            phoneTB.MaxLength = 25;
        }

        private Panel BuildTravellersStep()
        {
            Panel scroll = new Panel { AutoScroll = true, BackColor = Theme.Background };
            TableLayoutPanel stack =
            new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 1, BackColor = Theme.Background };
            stack.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

            // contact card
    Panel contact = UiKit.MakeCard();
            contact.Dock = DockStyle.Top;
            contact.Height = 148;
            contact.Margin = new Padding(0, 0, 0, 14);
            Label contactTitle = UiKit.MakeLabel("Contact details", Theme.Subtitle, Theme.Text);
            contactTitle.Location = new Point(20, 14);
            TableLayoutPanel contactRow =
            new TableLayoutPanel { Location = new Point(20, 48), Height = 70, ColumnCount = 2, BackColor = Color.White };
            contactRow.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            contactRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            contactRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50));
            contactRow.Controls.Add(Cell("Email", emailTB, new Padding(0, 0, 12, 0)), 0, 0);
            contactRow.Controls.Add(Cell("Mobile phone", phoneTB, new Padding(0)), 1, 0);
            contact.Controls.Add(contactTitle);
            contact.Controls.Add(contactRow);
            contact.Resize += delegate
            {
                contactRow.Width = contact.Width - 40;
            };
            stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            stack.Controls.Add(contact, 0, 0);

            string passportCaption = flight.IsInternational ? "Passport number (required)" : "Passport / ID (optional)";
            for (int i = 0; i < count; i++)
            {
                int index = i;
                Panel card = UiKit.MakeCard();
                card.Dock = DockStyle.Top;
                card.Height = 168;
                card.Margin = new Padding(0, 0, 0, 14);

                Label title = UiKit.MakeLabel("Traveller " + (index + 1) + (index == 0 ? "  (you)" : string.Empty),
                Theme.Subtitle, Theme.Text);
                title.Location = new Point(20, 14);
                Label note = UiKit.MakeLabel("Name exactly as on the passport. Children aged 2-11 pay 75% of the fare.",
                Theme.Small, Theme.Muted);
                note.Location = new Point(20, 42);
                note.AutoSize=false;note.Height=30;

                TableLayoutPanel row = new TableLayoutPanel { Location = new Point(20, 76), Height = 70, ColumnCount = 5,
                    BackColor = Color.White };
                row.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 29));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));
                row.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 19));
                row.Controls.Add(Cell("Full name", nameTB[index], new Padding(0, 0, 10, 0)), 0, 0);
                row.Controls.Add(Cell("Date of birth", dobPicker[index], new Padding(0, 0, 10, 0)), 1, 0);
                row.Controls.Add(Cell("Gender", genderCB[index], new Padding(0, 0, 10, 0)), 2, 0);
                row.Controls.Add(Cell(passportCaption, passportTB[index], new Padding(0, 0, 10, 0)), 3, 0);
                row.Controls.Add(Cell("Extra baggage", baggageCB[index], new Padding(0)), 4, 0);

                card.Controls.Add(title);
                card.Controls.Add(note);
                card.Controls.Add(row);
                card.Resize += delegate
                {
                    row.Width = card.Width - 40;
                    note.Width=row.Width;
                };

                stack.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                stack.Controls.Add(card, 0, i + 1);
            }

            scroll.Controls.Add(stack);
            return scroll;
        }

        private static Panel Cell(string caption, Control input, Padding padding)
        {
            Panel p = UiKit.Field(caption, input);
            p.Dock = DockStyle.Fill;
            p.Padding = padding;
            return p;
        }

        private void Prefill()
        {
            if (!Session.IsSignedIn)
            return;
            emailTB.Text = Session.Current.Email ?? string.Empty;
            nameTB[0].Text = Session.Current.FullName ?? string.Empty;

            try
            {
                if (Session.Current.PassId.HasValue)
                {
                    PassengerEntity p = PassengerEntity.GetById(Session.Current.PassId.Value);
                    if (p != null)
                    {
                        nameTB[0].Text = p.PassName;
                        phoneTB.Text = p.PassPhone ?? string.Empty;
                        emailTB.Text = p.PassEmail ?? string.Empty;
                        passportTB[0].Text = p.PassportNo ?? string.Empty;
                        if (!string.IsNullOrEmpty(p.PassGender))
                        genderCB[0].Text = p.PassGender;
                        if (p.DateOfBirth.HasValue)
                        dobPicker[0].Value = p.DateOfBirth.Value;
                    }
                }
            }
            catch (Exception)
            {
                // Pre-filling is a convenience only; the traveller can type the details.
    }
            UpdateEstimate();
        }

        /// <summary>Validates the form and builds the traveller list. Returns false (after telling the user) on
/// error.</summary>
private bool CollectTravellers()
        {
            if (!Validator.IsEmail(emailTB.Text))
            return Fail("Enter a valid contact email address.", emailTB);
            if (!Validator.IsPhone(phoneTB.Text))
            return Fail("Enter a valid contact phone number, for example +8801712345678.", phoneTB);

            List<TravellerInput> list = new List<TravellerInput>();
            HashSet<string> passports = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < count; i++)
            {
                string label = "Traveller " + (i + 1) + ": ";
                if (!Validator.IsPersonName(nameTB[i].Text))
                return Fail(label + "enter the name as shown on the passport (letters only).", nameTB[i]);

                int age = Validator.AgeOn(dobPicker[i].Value, flight.Departure);
                if (age < PricingService.MinimumAge)
                return Fail(label + "infants under 2 years cannot be booked online.", dobPicker[i]);
                if (age > 120)
                return Fail(label + "check the date of birth.", dobPicker[i]);

                string passport = passportTB[i].Text.Trim();
                if (flight.IsInternational && !Validator.IsPassport(passport))
                return Fail(label +
                "a valid passport number (6-12 letters/digits) is required for international flights.",
                passportTB[i]);
                if (!flight.IsInternational && passport.Length > 0 && !Validator.IsPassport(passport))
                return Fail(label + "the passport / ID must be 6-12 letters or digits.", passportTB[i]);
                if (passport.Length > 0 && !passports.Add(passport))
                return Fail(label + "this passport number is already used by another traveller.", passportTB[i]);

                list.Add(new TravellerInput { Name = nameTB[i].Text.Trim(), DateOfBirth = dobPicker[i].Value.Date,
                    Gender = genderCB[i].Text, PassportNo = passport,
                    BaggageKg = PricingService.BaggageOptions[baggageCB[i].SelectedIndex] });
            }
            travellers = list;
            return true;
        }

        private bool Fail(string message, Control focus)
        {
            UiKit.Warn(this, message);
            focus.Focus();
            return false;
        }

        /// <summary>Live total in the side rail while the user edits dates of birth and baggage.</summary>
private void UpdateEstimate()
        {
            try
            {
                List<TravellerInput> draft = new List<TravellerInput>();
                for (int i = 0; i < count; i++)
                {
                    draft.Add(
                    new TravellerInput { Name = "Traveller " + (i + 1), DateOfBirth = dobPicker[i].Value.Date,
                        BaggageKg =
                        PricingService.BaggageOptions[Math.Max(0, baggageCB[i].SelectedIndex)] });
                }
                railTotal.Text = Money.Format(PricingService.Quote(flight, cabin, draft).Total);
            }
            catch (ArgumentException)
            {
                railTotal.Text = "-";
            }
        }

        // ================================================================ step 2: seats

        private Panel BuildSeatsStep()
        {
            Panel panel = new Panel { BackColor = Theme.Background };

            Panel rightFrame=UiKit.MakeCard();
            rightFrame.Dock=DockStyle.Right;rightFrame.Width=320;rightFrame.Padding=new Padding(0);
            Panel right=new Panel {Dock=DockStyle.Fill,AutoScroll=true,BackColor=Color.White};
            right.AutoScrollMinSize=new Size(0,94+count*50+110);
            rightFrame.Controls.Add(right);

            Panel left = UiKit.MakeCard();
            left.Dock = DockStyle.Fill;
            left.Padding = new Padding(1);
            Label leftTitle =
            UiKit.MakeLabel(cabin + " cabin  \u00B7  " + flight.Src + " \u2192 " + flight.Dest, Theme.Subtitle, Theme.Text);
            leftTitle.Dock = DockStyle.Top;
            leftTitle.AutoSize = false;
            leftTitle.Height = 46;
            leftTitle.Padding = new Padding(18, 12, 0, 0);
            seatMap.Dock = DockStyle.Fill;
            seatMap.SeatClicked += OnSeatClicked;
            left.Controls.Add(seatMap);
            left.Controls.Add(leftTitle);

            Label rt = UiKit.MakeLabel("Who is sitting where?", Theme.Subtitle, Theme.Text);
            rt.Location = new Point(20, 16);
            Label hint = UiKit.MakeLabel("Select a traveller, then click a seat.\nClick the same seat again to clear it.",
            Theme.Small, Theme.Muted);
            hint.Location = new Point(22, 48);

            travellerRadios = new RadioButton[count];
            for (int i = 0; i < count; i++)
            {
                int index = i;
                RadioButton rb = new RadioButton { Appearance = Appearance.Button,
                    FlatStyle = FlatStyle.Flat,
                    TextAlign = ContentAlignment.MiddleLeft,
                    Font = Theme.Base,
                    Size = new Size(278, 44),
                    Location = new Point(20, 94 + i * 50),
                    Cursor = Cursors.Hand,
                    BackColor = Color.White };
                rb.FlatAppearance.BorderColor = Theme.Border;
                rb.FlatAppearance.CheckedBackColor = Theme.AccentLight;
                rb.CheckedChanged += delegate
                {
                    if (rb.Checked)
                    currentTraveller = index;
                };
                travellerRadios[i] = rb;
                right.Controls.Add(rb);
            }

            int legendY = 94 + count * 50 + 14;
            AddLegend(right, Color.White, Theme.Border, "Free", legendY);
            AddLegend(right, Theme.Accent, Theme.AccentDark, "Your pick", legendY + 28);
            AddLegend(right, ColorTranslator.FromHtml("#CFD8DC"), ColorTranslator.FromHtml("#CFD8DC"), "Taken", legendY + 56);

            Button auto = UiKit.MakeButton("Auto-assign seats", ButtonKind.Secondary, 278, delegate { AutoAssign(); });
            auto.Dock=DockStyle.Fill;
            Panel autoFooter=new Panel {Dock=DockStyle.Bottom,Height=68,Padding=new Padding(20,14,20,14),BackColor=Color.White};
            autoFooter.Controls.Add(auto);rightFrame.Controls.Add(autoFooter);
            right.Controls.AddRange(new Control[] { rt, hint });

            // Docked panels ignore Margin, so a thin spacer panel makes the gap between the cards.
    Panel gap = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Theme.Background };
            panel.Controls.Add(left);
            panel.Controls.Add(gap);
            panel.Controls.Add(rightFrame);
            right.Resize+=delegate {foreach(var rb in travellerRadios) rb.Width=Math.Max(180,right.ClientSize.Width-40);};
            return panel;
        }

        private static void AddLegend(Panel parent, Color fill, Color border, string text, int y)
        {
            Panel swatch = new Panel { Size = new Size(22, 18), Location = new Point(22, y), BackColor = fill };
            swatch.Paint += delegate(object s, PaintEventArgs e) {
                using (Pen p = new Pen(border)) { e.Graphics.DrawRectangle(p, 0, 0, swatch.Width - 1, swatch.Height - 1);
                }
            }
            ;
            Label l = UiKit.MakeLabel(text, Theme.Small, Theme.Muted);
            l.Location = new Point(52, y);
            parent.Controls.Add(swatch);
            parent.Controls.Add(l);
        }

        private void OnEnterSeats()
        {
            seatDataReady=UiKit.Guard(this, delegate {
                allSeats = SeatLayout.Build(flight);
                HashSet<string> taken = FlightEntity.GetTakenSeats(flight.FCode);
                seatMap.LoadCabin(allSeats, cabin, taken);

                // drop choices that somebody else has taken in the meantime
        foreach (int key in seatByTraveller.Keys.ToList())
                {
                    if (taken.Contains(seatByTraveller[key]))
                    seatByTraveller.Remove(key);
                }
            });
            nextButton.Enabled=seatDataReady;
            if(!seatDataReady) return;
            currentTraveller = FirstUnassigned();
            if (currentTraveller < 0)
            currentTraveller = 0;
            RefreshSeats();
        }

        private int FirstUnassigned()
        {
            for (int i = 0; i < count; i++)
            if (!seatByTraveller.ContainsKey(i))
            return i;
            return -1;
        }

        private void OnSeatClicked(string seat)
        {
            if (seatByTraveller.ContainsKey(currentTraveller) && seatByTraveller[currentTraveller] == seat)
            {
                seatByTraveller.Remove(currentTraveller);
            }
            else
            {
                foreach (int key in seatByTraveller.Where(kv => kv.Value == seat).Select(kv => kv.Key).ToList())
                {
                    seatByTraveller.Remove(key);
                }
                seatByTraveller[currentTraveller] = seat;
                int next = FirstUnassigned();
                if (next >= 0)
                currentTraveller = next;
            }
            RefreshSeats();
        }

        private void AutoAssign()
        {
            if (allSeats == null) { OnEnterSeats(); if (!seatDataReady) return; }
            HashSet<string> taken = null;
            if (!UiKit.Guard(this, delegate { taken = FlightEntity.GetTakenSeats(flight.FCode); }))
            return;

            foreach(int key in seatByTraveller.Keys.ToList()) if(taken.Contains(seatByTraveller[key])) seatByTraveller.Remove(key);
            seatMap.LoadCabin(allSeats,cabin,taken);
            HashSet<string> used = new HashSet<string>(seatByTraveller.Values, StringComparer.OrdinalIgnoreCase);
            Queue<Seat> free = new Queue<Seat>(
            allSeats.Where(s => s.CabinClass == cabin && !taken.Contains(s.Number) && !used.Contains(s.Number)));
            for (int i = 0; i < count; i++)
            {
                if (seatByTraveller.ContainsKey(i))
                continue;
                if (free.Count == 0)
                {
                    UiKit.Warn(this, "Not enough free seats left in this cabin.");
                    break;
                }
                seatByTraveller[i] = free.Dequeue().Number;
            }
            currentTraveller = Math.Max(0, FirstUnassigned());
            RefreshSeats();
        }

        private void RefreshSeats()
        {
            Dictionary<string, int> map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (KeyValuePair<int, string> kv in seatByTraveller)
            map[kv.Value] = kv.Key + 1;
            seatMap.ShowAssignments(map);

            for (int i = 0; i < count; i++)
            {
                string name = travellers.Count > i ? travellers[i].Name : "Traveller " + (i + 1);
                string seat = seatByTraveller.ContainsKey(i) ? "Seat " + seatByTraveller[i] : "no seat yet";
                travellerRadios[i].Text = "  " + (i + 1) + "   " + Shorten(name, 20) + "  \u00B7  " + seat;
                travellerRadios[i].Checked = i == currentTraveller;
            }
        }

        private static string Shorten(string text, int max)
        {
            return text.Length <= max ? text : text.Substring(0, max - 1) + "\u2026";
        }

        // ======================================================== step 3: review and pay

        private Panel BuildPaymentStep()
        {
            Panel panel = new Panel { BackColor = Theme.Background };

            Panel pay = UiKit.MakeCard();
            pay.Dock = DockStyle.Right;
            pay.Width = 380;
            pay.Padding = new Padding(22);

            Panel review = UiKit.MakeCard();
            review.Dock = DockStyle.Fill;
            review.Padding = new Padding(20);

            // ---- review (left)
    Label reviewTitle = UiKit.MakeLabel("Review your trip", Theme.Subtitle, Theme.Text);
            reviewTitle.Dock = DockStyle.Top;
            reviewTitle.AutoSize = false;
            reviewTitle.Height = 36;

            UiKit.StyleGrid(reviewGrid);
            reviewGrid.Dock = DockStyle.Top;
            reviewGrid.Height = 40 + 38 * Math.Min(count, 5) + 2;
            reviewGrid.ScrollBars = ScrollBars.Vertical;
            UiKit.AddColumn(reviewGrid, "Traveller", "Traveller", 36, null);
            UiKit.AddColumn(reviewGrid, "Type", "Fare", 14, null);
            UiKit.AddColumn(reviewGrid, "Seat", "Seat", 12, null);
            UiKit.AddColumn(reviewGrid, "Baggage", "Extra bag", 16, null);
            UiKit.AddColumn(reviewGrid, "Price", "Price", 22, null);
            UiKit.MoneyColumns(reviewGrid, "Price");
            reviewGrid.Columns["Price"].DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;

            TableLayoutPanel totals = new TableLayoutPanel {
                Dock = DockStyle.Top,   Height = 150, ColumnCount = 2, RowCount = 4, Padding = new Padding(0, 14, 0, 0),
                BackColor = Color.White
            };
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
            totals.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            for (int i = 0; i < 4; i++)
            totals.RowStyles.Add(new RowStyle(SizeType.Percent, 25));
            AddTotalRow(totals, 0, "Base fare", baseLabel, Theme.Base);
            AddTotalRow(totals, 1, "Extra baggage", bagLabel, Theme.Base);
            AddTotalRow(totals, 2, "Taxes & airport fees", taxLabel, Theme.Base);
            AddTotalRow(totals, 3, "Total to pay", totalLabel, Theme.Subtitle);

            Label policy = UiKit.MakeLabel(
            "Cancellation: 90% refund 72 h or more before departure, 50% from 24 to 72 h, none within 24 h. " +
            "Flights cancelled by the airline are refunded in full. Online booking closes 1 hour before departure.",
            Theme.Small, Theme.Muted);
            policy.AutoSize = false;
            policy.Dock = DockStyle.Top;
            policy.Height = 60;
            policy.Padding = new Padding(0, 14, 0, 0);

            review.Controls.Add(policy);
            review.Controls.Add(totals);
            review.Controls.Add(reviewGrid);
            review.Controls.Add(reviewTitle);

            // ---- payment (right)
    Label payTitle = UiKit.MakeLabel("Payment", Theme.Subtitle, Theme.Text);
            payTitle.Location = new Point(22, 16);
            Panel demo = new Panel { Location = new Point(22, 52), Size = new Size(334, 78),
                BackColor = ColorTranslator.FromHtml("#FFF8E1") };
            Label demoText = UiKit.MakeLabel("Demo mode - no real money is charged.\nTest card: 4242 4242 4242 4242, any " +
            "future expiry and CVV.\nDeclined test card: 4000 0000 0000 0002.",
            Theme.Small, ColorTranslator.FromHtml("#7A5A00"));
            demoText.Location = new Point(12, 9);
            demoText.AutoSize = false;
            demoText.Size = new Size(310, 62);
            demo.Controls.Add(demoText);

            cardNameTB.MaxLength = 60;
            cardNumberTB.MaxLength = 23;
            cardExpiryTB.MaxLength = 5;
            cardCvvTB.MaxLength = 4;
            cardCvvTB.UseSystemPasswordChar = true;
            cardNumberTB.TextChanged += delegate
            {
                FormatCardNumber();
            };
            cardExpiryTB.TextChanged += delegate
            {
                FormatExpiry();
            };

            Panel fName = UiKit.Field("Name on card", cardNameTB);
            Panel fNumber = UiKit.Field("Card number", cardNumberTB);
            Panel fExpiry = UiKit.Field("Expiry (MM/YY)", cardExpiryTB);
            Panel fCvv = UiKit.Field("CVV", cardCvvTB);
            foreach (Panel f in new[] { fName, fNumber, fExpiry, fCvv })
            f.Dock = DockStyle.None;
            fName.Size = new Size(334, 62);
            fName.Location = new Point(22, 146);
            fNumber.Size = new Size(334, 62);
            fNumber.Location = new Point(22, 214);
            fExpiry.Size = new Size(160, 62);
            fExpiry.Location = new Point(22, 282);
            fCvv.Size = new Size(160, 62);
            fCvv.Location = new Point(196, 282);

            agreeCheck.Text = "I agree to the fare rules and cancellation policy.";
            agreeCheck.Font = Theme.Small;
            agreeCheck.AutoSize = false;
            agreeCheck.Size = new Size(334, 40);
            agreeCheck.Location = new Point(22, 362);

            Label secure = UiKit.MakeLabel("Card number and CVV are never saved - only the brand and last 4 digits are kept.",
            Theme.Small, Theme.Muted);
            secure.AutoSize = false;
            secure.Size = new Size(334, 44);
            secure.Location = new Point(22, 408);

            pay.Controls.AddRange(new Control[] { payTitle, demo, fName, fNumber, fExpiry, fCvv, agreeCheck, secure });

            Panel gap = new Panel { Dock = DockStyle.Right, Width = 14, BackColor = Theme.Background };
            panel.Controls.Add(review);
            panel.Controls.Add(gap);
            panel.Controls.Add(pay);
            return panel;
        }

        private static void AddTotalRow(TableLayoutPanel table, int row, string caption, Label value, Font font)
        {
            Label c = UiKit.MakeLabel(caption, font, Theme.Muted);
            c.Dock = DockStyle.Fill;
            c.AutoSize = false;
            c.TextAlign = ContentAlignment.MiddleLeft;
            value.Dock = DockStyle.Fill;
            value.AutoSize = false;
            value.TextAlign = ContentAlignment.MiddleRight;
            if (row == 3)
            c.ForeColor = Theme.Text;
            table.Controls.Add(c, 0, row);
            table.Controls.Add(value, 1, row);
        }

        private void OnEnterPayment()
        {
            for (int i = 0; i < count; i++)
            travellers[i].SeatNo = seatByTraveller[i];
            quote = PricingService.Quote(flight, cabin, travellers);

            DataTable table = new DataTable();
            table.Columns.Add("Traveller", typeof(string));
            table.Columns.Add("Type", typeof(string));
            table.Columns.Add("Seat", typeof(string));
            table.Columns.Add("Baggage", typeof(string));
            table.Columns.Add("Price", typeof(decimal));
            for (int i = 0; i < count; i++)
            {
                FareLine line = quote.Lines[i];
                table.Rows.Add(line.Name, line.FareType, travellers[i].SeatNo,
                travellers[i].BaggageKg > 0 ? "+" + travellers[i].BaggageKg + " kg" : "-", line.Total);
            }
            reviewGrid.DataSource = table;

            baseLabel.Text = Money.Format(quote.BaseFare);
            bagLabel.Text = Money.Format(quote.AddOns);
            taxLabel.Text = Money.Format(quote.Taxes);
            totalLabel.Text = Money.Format(quote.Total);
            railTotal.Text = Money.Format(quote.Total);

            nextButton.Text = "Confirm Payment";
        }

        private void FormatCardNumber()
        {
            if (formatting)
            return;
            formatting = true;
            string digits = CardValidator.Normalize(cardNumberTB.Text);
            if (digits.Length > 19)
            digits = digits.Substring(0, 19);
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            for (int i = 0; i < digits.Length; i++)
            {
                if (i > 0 && i % 4 == 0)
                sb.Append(' ');
                sb.Append(digits[i]);
            }
            cardNumberTB.Text = sb.ToString();
            cardNumberTB.SelectionStart = cardNumberTB.Text.Length;
            formatting = false;
        }

        private void FormatExpiry()
        {
            if (formatting)
            return;
            formatting = true;
            string digits = CardValidator.Normalize(cardExpiryTB.Text);
            if (digits.Length > 4)
            digits = digits.Substring(0, 4);
            cardExpiryTB.Text = digits.Length > 2 ? digits.Substring(0, 2) + "/" + digits.Substring(2) : digits;
            cardExpiryTB.SelectionStart = cardExpiryTB.Text.Length;
            formatting = false;
        }

        private CardDetails ReadCard()
        {
            if (!Validator.IsPersonName(cardNameTB.Text))
            {
                Fail("Enter the name printed on the card.", cardNameTB);
                return null;
            }

            string number = CardValidator.Normalize(cardNumberTB.Text);
            if (!CardValidator.PassesLuhn(number))
            {
                Fail("The card number is not valid. Please check it.", cardNumberTB);
                return null;
            }

            string[] parts = cardExpiryTB.Text.Split('/');
            int month, year;
            if (parts.Length != 2 || !int.TryParse(parts[0], out month) || !int.TryParse(parts[1], out year) ||
            parts[1].Length != 2)
            {
                Fail("Enter the expiry date as MM/YY.", cardExpiryTB);
                return null;
            }
            year += 2000;
            if (!CardValidator.IsExpiryValid(month, year, DateTime.Today))
            {
                Fail("This card has expired or the date is invalid.", cardExpiryTB);
                return null;
            }

            string brand = CardValidator.Brand(number);
            if (!CardValidator.IsCvvValid(cardCvvTB.Text, brand))
            {
                Fail("Enter the security code (CVV): " + (brand == "Amex" ? "4" : "3") + " digits.", cardCvvTB);
                return null;
            }
            if (!agreeCheck.Checked)
            {
                UiKit.Warn(this, "Please accept the fare rules and cancellation policy to continue.");
                return null;
            }

            return new CardDetails { HolderName = cardNameTB.Text.Trim(), Number = number, ExpiryMonth = month,
                ExpiryYear = year, Cvv = cardCvvTB.Text };
        }

        private void Pay()
        {
            CardDetails card = ReadCard();
            if (card == null)
            return;

            BookingRequest request = new BookingRequest { UserId = Session.Current.Id,
                Flight = flight,
                Cabin = cabin,
                Travellers = travellers,
                ContactEmail = emailTB.Text.Trim(),
                ContactPhone = phoneTB.Text.Trim(),
                Card = card };

            nextButton.Enabled = false;
            backButton.Enabled = false;
            Cursor = Cursors.WaitCursor;
            try
            {
                booking = BookingService.Book(request);
            }
            catch (BookingException ex)
            {
                Cursor = Cursors.Default;
                UiKit.Warn(this, ex.Message);
                if(ex.FlightChanged) {DialogResult=DialogResult.Retry;Close();return;}
                if (ex.SeatConflict)
                GoToStep(StepSeats);
                return;
            }
            catch (Exception ex)
            {
                Cursor = Cursors.Default;
                UiKit.Error(this, "Your booking could not be completed and your card was not charged.\n\n" + ex.Message);
                return;
            }
            finally
            {
                Cursor = Cursors.Default;
                nextButton.Enabled = true;
                backButton.Enabled = true;
            }

            // Wipe sensitive data from the form as soon as it is no longer needed.
    cardNumberTB.Clear();
            cardCvvTB.Clear();
            cardExpiryTB.Clear();
            card.Number = null;
            card.Cvv = null;

            CreatedBookingId = booking.BookingId;
            pnrLabel.Text = booking.Pnr;
            doneDetail.Text = Money.Format(booking.Total) + " paid  \u00B7  payment ref " + booking.PaymentReference + "\n" +
            count + (count == 1 ? " e-ticket has" : " e-tickets have") + " been issued for " +
            flight.FlightNo + " on " + flight.Departure.ToString("dddd, dd MMMM yyyy") + ".";
            GoToStep(StepDone);
        }

        // ============================================================ step 4: confirmation

        private Panel BuildDoneStep()
        {
            Panel panel = new Panel { BackColor = Theme.Background };
            Panel card = UiKit.MakeCard();
            card.Size = new Size(620, 440);
            card.Anchor = AnchorStyles.None;

            Panel badge = new Panel { Size = new Size(84, 84), Location = new Point(268, 34), BackColor = Color.White };
            badge.Paint += delegate(object s, PaintEventArgs e)
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                using (Brush b = new SolidBrush(Theme.SuccessLight))
                {
                    e.Graphics.FillEllipse(b, 0, 0, 82, 82);
                }
                using (Pen p = new Pen(Theme.Success, 7f) { StartCap = LineCap.Round, EndCap = LineCap.Round,
                    LineJoin = LineJoin.Round })
                {
                    e.Graphics.DrawLines(p, new[] { new Point(24, 43), new Point(37, 56), new Point(60, 29) });
                }
            };

            Label title = UiKit.MakeLabel("Booking confirmed", Theme.Title, Theme.Text);
            title.AutoSize = false;
            title.Size = new Size(620, 40);
            title.TextAlign = ContentAlignment.MiddleCenter;
            title.Location = new Point(0, 134);
            Label refCaption = UiKit.MakeLabel("YOUR BOOKING REFERENCE (PNR)", Theme.Small, Theme.Muted);
            refCaption.AutoSize = false;
            refCaption.Size = new Size(620, 24);
            refCaption.TextAlign = ContentAlignment.MiddleCenter;
            refCaption.Location = new Point(0, 184);
            pnrLabel.AutoSize = false;
            pnrLabel.Size = new Size(620, 70);
            pnrLabel.TextAlign = ContentAlignment.MiddleCenter;
            pnrLabel.Location = new Point(0, 208);
            doneDetail.AutoSize = false;
            doneDetail.Size = new Size(540, 64);
            doneDetail.TextAlign = ContentAlignment.TopCenter;
            doneDetail.Location = new Point(40, 290);

            Button view = UiKit.MakeButton("View e-tickets", ButtonKind.Primary, 200, delegate {
                using (TicketView tv = new TicketView(CreatedBookingId)) tv.ShowDialog(this);
            });
            view.Height = 46;
            view.Location = new Point(210, 362);

            card.Controls.AddRange(new Control[] { badge, title, refCaption, pnrLabel, doneDetail, view });
            panel.Controls.Add(card);
            panel.Resize += delegate
            {
                card.Location = new Point((panel.Width - card.Width) / 2, Math.Max(0, (panel.Height - card.Height) / 2));
            };
            return panel;
        }
    }
}
