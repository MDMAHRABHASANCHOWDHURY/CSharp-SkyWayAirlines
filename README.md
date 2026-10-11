# SkyWay Airline Reservation System

SkyWay is a C# Windows Forms application for booking flights and managing airline reservations. Passengers can search for flights, choose seats, view fare details and receive an e-ticket. Administrators can manage flight schedules, passenger records and bookings.

The project uses .NET Framework 4.7.2, SQL Server and ADO.NET. Payments and refunds are simulated, so no real money is charged and the tickets are not valid for airline travel.

## Before you start

You will need:

- Windows and Visual Studio with the **.NET desktop development** workload.
- The **.NET Framework 4.7.2 targeting pack**.
- SQL Server Express LocalDB or a running SQL Server instance.
- SQL Server Management Studio (SSMS), if you want to inspect the database manually.

SSMS is a management tool. Installing SSMS alone does not install the database engine.

## Open the project

Extract the project into a folder on your computer. For this setup,

Open Visual Studio, select **Open a project or solution**, and choose:

```text
SkyWay Airline\AirlineReservation.sln
```

Solution Explorer should show two projects:

| Project | Purpose |
|---|---|
| AirlineReservationDemo | The Windows Forms application |
| SkyWay.LogicTests | The console application containing logic tests |

Right-click **AirlineReservationDemo** and select **Set as Startup Project**.

## Set up LocalDB

This setup uses a named LocalDB instance called `SkyWayDB` with SQL Server 2025 LocalDB, version 17.

If the instance has not been created, run this in **Command Prompt**:

```bat
sqllocaldb create SkyWayDB 17.0 -s
```

If it already exists, start it with:

```bat
sqllocaldb start SkyWayDB
```

Check its details with:

```bat
sqllocaldb info SkyWayDB
```

A running instance shows `State: Running`. LocalDB may stop when idle; this does not mean the instance was deleted.

## Configure the connection

In Visual Studio, open **AirlineReservationDemo → App.config**. Use this connection section:

```xml
<connectionStrings>
  <add name="AirlineDb"
       connectionString="Data Source=(localdb)\SkyWayDB;Initial Catalog=AirlineReservationDB;Integrated Security=True;Encrypt=False"
       providerName="System.Data.SqlClient" />
</connectionStrings>
```

Paste this XML into **App.config**, not Command Prompt or an SSMS query window. Keep the remaining configuration sections.

If you use a full SQL Server instance instead, replace `Data Source` with the server name that connects successfully in SSMS. Use a fresh database for the revised project. The original database/BACPAC has a different schema and should not be imported into this database.

## Build and run

1. Save all files.
2. Select **Build → Rebuild Solution**.
3. Check the Build output for errors.
4. Press **F5** once the build succeeds.

On the first run, the application attempts to create the database, tables, airports, sample flights and default accounts. The initial flight schedule covers 45 days from the first setup date. Administrators can add flights for later dates.

If your database account cannot create a database, use an account with the required permissions to run `Database\AirlineReservation.sql` in SSMS, then restart the application.

## Login accounts

After database setup succeeds, use these default accounts:

| Role | Username | Password |
|---|---|---|
| Administrator | `admin` | `Admin@123` |
| Passenger | `passenger` | `pass123@` |

Passwords are case-sensitive. Default account creation requires this setting:

```xml
<add key="SeedDemoAccounts" value="true" />
```

The login hints can stay hidden:

```xml
<add key="ShowDemoHint" value="false" />
```

New passengers can register and sign in using their chosen username and password. Hiding the login hints does not remove the default accounts. Disabling account seeding does not delete accounts already created.

## Book a flight

Sign in as a passenger and open **Book a Flight**.

1. Choose the departure airport, destination, travel date, cabin and traveller count.
2. Search and select an available flight.
3. Enter traveller details and contact information.
4. Choose extra baggage.
5. Select a separate available seat for each traveller, or use auto-assignment.
6. Review the fare and complete the test payment.
7. Save the booking reference and view the e-ticket.

For a first test, try **DAC → DXB**, Economy, one adult traveller and a date seven days after first database setup. International bookings require valid passport details.

The application supports up to nine travellers per booking. Children aged 2–11 receive 75% of the base fare. Infants under two are not supported by the online booking flow. Booking closes one hour before departure.

## Test payment details

Use test details only:

| Field | Value |
|---|---|
| Cardholder name | Mahrab Hasan |
| Approved card | `4242 4242 4242 4242` |
| Expiry | `12/30`, or another valid future expiry |
| CVV | `123` |
| Declined card | `4000 0000 0000 0002` |

No money is charged. Full card numbers and CVVs are not stored in the database. Real payments require a separate provider integration and payment reconciliation workflow.

## USD and BDT display

With the currency changes applied to `Money.cs`, `ShellForm.cs` and both dashboards, the home-screen **Currency** button switches the displayed prices between USD and BDT. Newly opened booking screens use the selected currency. The choice lasts for the current application session.

Use these entries inside App.config's existing `<appSettings>` section:

```xml
<add key="DisplayCurrency" value="USD" />
<add key="UsdToBdtRate" value="120.00" />
```

`DisplayCurrency` controls the starting currency. The exchange rate above is an example: **1 USD = 120 BDT**. Replace it with your chosen rate and rebuild. It is not fetched automatically.

All stored fares, charges and refunds remain in USD. Selecting BDT converts the displayed amount; it does not change the payment settlement currency. For example, USD 100 displays as BDT 12,000 at the example rate.

Use the single-currency version of `Money.Format` and remove `ShowBothPrices` to avoid placing both amounts in narrow labels. The booking wizard's estimated-total label should use the smaller fixed-width label provided with the currency changes.

## Manage tickets and bookings

Open **My Bookings** to view reservations and individual e-tickets. Tickets can be saved as PNG images or printed. To save a PDF, choose **Microsoft Print to PDF** in the print dialog.

Cancellation shows a refund preview before confirmation. The passenger refund rules are:

| Time before departure | Refund |
|---|---|
| At least 72 hours | 90% |
| At least 24 hours but less than 72 hours | 50% |
| Less than 24 hours | 0% |
| After departure | Passenger cancellation blocked |

Administrator or airline cancellation gives a full simulated refund. These are project rules, not real airline fare conditions.

## Administrator features

The administrator can view dashboard statistics, add and edit flights, manage passenger records, search bookings, view tickets, export bookings to CSV and cancel bookings or entire flights.

Flights with booking history retain their route, identity, schedule and cabin layout. Fares and delay status remain editable. Some deletions are blocked because the record is linked to bookings or login accounts.

## Connect through SSMS

Enter the following manually in the connection window:

```text
Server Name: (localdb)\SkyWayDB
Authentication: Windows Authentication
Database Name: <default>
```

LocalDB may not appear in the server dropdown. Type its name into the Server Name textbox. For this local development setup, select **Trust Server Certificate** if the connection requires it.

After connecting, refresh **Databases** to find `AirlineReservationDB`. You can also open and execute `Database\AirlineReservation.sql` here for manual database setup.

## Run the logic tests

Stop the application. Right-click **SkyWay.LogicTests → Set as Startup Project**, then press **Ctrl + F5**. Read the passed and failed checks in the console. These logic tests do not require SQL Server.

Afterwards, set **AirlineReservationDemo** as the startup project again.

Alternatively, open **Developer Command Prompt for Visual Studio** and run:

```bat
cd /d "F:\PROJECT\C#\SkyWay Airline"
BUILD_AND_TEST.cmd
```

This rebuilds the solution in Release mode and runs the logic tests. Database and UI acceptance checks are listed in `VALIDATION.md`.

## Project folders

| Folder | Contents |
|---|---|
| Forms | Login, registration, dashboards, booking wizard and management screens |
| UI | Shared theme, navigation, controls and seat map |
| Entities | Flight, passenger, user and ticket-related data |
| Data | SQL connection helpers and database initialization |
| Services | Authentication, pricing, bookings, payments, refunds and reports |
| Helpers | Validation, currency formatting, CSV, barcode and ticket rendering |
| Database | Database schema and initial flight schedule |
| Tests | Logic regression checks |

Forms are built using C# controls rather than drag-and-drop Designer layouts. Run the application to see the complete interface.

## Project limits and verification

This is a coursework booking application. It uses local flight inventory, simulated payments and project-generated ticket numbers. It has no live airline connection, real payment processing or automatic email delivery. Flight times use SQL Server/machine time rather than airport-local time zones.

Source syntax and package integrity checks were performed for the delivered revision. That does not establish runtime correctness. Build the solution, execute the tests and check the booking, cancellation and UI flows on Windows. Currency changes supplied as code snippets also need to be built and tested after they are applied.


## Profile and password requests

Passengers can open **My profile** to update their name, contact details, nationality, gender, date of birth, passport and address. Choose a JPG or PNG up to 2 MB, or remove the existing photo, then click **Save profile**. The picture is stored in the database and is available after restarting the app. Changes do not rewrite traveller details on tickets already booked.

On the same page, enter a short reason and click **Send request** to ask the administrator for a password reset. Do not enter passwords in the reason. Only one pending request is allowed per account. Use **Refresh requests** to check its status. This request flow requires the passenger to be signed in; it is not a public forgotten-password recovery form.

Administrators can open **Account security**, select a passenger and click **Reset password** at any time, whether or not a request exists. Enter and confirm a temporary password, then give it to the passenger privately. A pending request is marked Approved; administrators can also reject a pending request. Resetting a password clears login lockout. The passenger must replace the temporary password before entering their dashboard at the next sign-in. Existing sessions are not forcibly disconnected.

Existing accounts, bookings, profile details and currency settings are preserved. Rebuild and launch the app once to apply the additive database upgrade. Do not delete your database. Photo uploads and password changes use the existing local database connection.
