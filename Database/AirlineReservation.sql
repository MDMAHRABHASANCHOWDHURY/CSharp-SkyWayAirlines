/* =====================================================================
   SkyWay Airline Reservation System - database script (SQL Server)
   ---------------------------------------------------------------------
   * Idempotent: safe to run more than once.
   * The application runs this script automatically on first start,
     so running it by hand in SSMS is optional.
   * Demo user accounts are created by the application (passwords are
     hashed in C#, never stored in this script).
   ===================================================================== */

IF DB_ID(N'AirlineReservationDB') IS NULL
    CREATE DATABASE [AirlineReservationDB];
GO

USE [AirlineReservationDB];
GO

/* ---------------------------------------------------------------- role */
IF OBJECT_ID(N'dbo.role', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.role
    (
        roleid   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_role PRIMARY KEY,
        roletype NVARCHAR(30)      NOT NULL CONSTRAINT UQ_role_roletype UNIQUE
    );
END
GO

IF NOT EXISTS (SELECT 1 FROM dbo.role WHERE roletype = N'admin')
    INSERT dbo.role (roletype) VALUES (N'admin');
IF NOT EXISTS (SELECT 1 FROM dbo.role WHERE roletype = N'passenger')
    INSERT dbo.role (roletype) VALUES (N'passenger');
GO

/* ---------------------------------------------------------- AirportTbl */
IF OBJECT_ID(N'dbo.AirportTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.AirportTbl
    (
        Code        CHAR(3)       NOT NULL CONSTRAINT PK_AirportTbl PRIMARY KEY,
        City        NVARCHAR(60)  NOT NULL,
        Country     NVARCHAR(60)  NOT NULL,
        AirportName NVARCHAR(100) NOT NULL
    );

    INSERT dbo.AirportTbl (Code, City, Country, AirportName) VALUES
    ('DAC', N'Dhaka',        N'Bangladesh', N'Hazrat Shahjalal International Airport'),
    ('CGP', N'Chattogram',   N'Bangladesh', N'Shah Amanat International Airport'),
    ('CXB', N'Cox''s Bazar', N'Bangladesh', N'Cox''s Bazar Airport'),
    ('ZYL', N'Sylhet',       N'Bangladesh', N'Osmani International Airport'),
    ('DXB', N'Dubai',        N'United Arab Emirates', N'Dubai International Airport'),
    ('DOH', N'Doha',         N'Qatar',      N'Hamad International Airport'),
    ('JED', N'Jeddah',       N'Saudi Arabia', N'King Abdulaziz International Airport'),
    ('SIN', N'Singapore',    N'Singapore',  N'Changi Airport'),
    ('KUL', N'Kuala Lumpur', N'Malaysia',   N'Kuala Lumpur International Airport'),
    ('BKK', N'Bangkok',      N'Thailand',   N'Suvarnabhumi Airport'),
    ('DEL', N'Delhi',        N'India',      N'Indira Gandhi International Airport'),
    ('CCU', N'Kolkata',      N'India',      N'Netaji Subhas Chandra Bose International Airport'),
    ('IST', N'Istanbul',     N'Turkey',     N'Istanbul Airport'),
    ('LHR', N'London',       N'United Kingdom', N'Heathrow Airport');
END
GO

/* --------------------------------------------------------- PassengerTbl */
IF OBJECT_ID(N'dbo.PassengerTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PassengerTbl
    (
        PassId      INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PassengerTbl PRIMARY KEY,
        PassName    NVARCHAR(100)     NOT NULL,
        PassAddress NVARCHAR(200)     NULL,
        PassNat     NVARCHAR(60)      NULL,
        PassGender  NVARCHAR(10)      NULL,
        PassPhone   NVARCHAR(25)      NULL,
        PassEmail   NVARCHAR(120)     NULL,
        PassportNo  NVARCHAR(20)      NULL,
        DateOfBirth DATE              NULL,
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_PassengerTbl_CreatedAt DEFAULT SYSDATETIME()
    );
END
GO

/* --------------------------------------------------------------- users */
IF OBJECT_ID(N'dbo.users', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.users
    (
        id             INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_users PRIMARY KEY,
        username       NVARCHAR(50)      NOT NULL CONSTRAINT UQ_users_username UNIQUE,
        PasswordHash   NVARCHAR(200)     NOT NULL,   -- PBKDF2$iterations$salt$hash
        userrole       INT               NOT NULL CONSTRAINT FK_users_role REFERENCES dbo.role(roleid),
        PassId         INT               NULL     CONSTRAINT FK_users_Passenger REFERENCES dbo.PassengerTbl(PassId),
        IsActive       BIT               NOT NULL CONSTRAINT DF_users_IsActive DEFAULT 1,
        FailedAttempts INT               NOT NULL CONSTRAINT DF_users_Failed DEFAULT 0,
        LockoutUntil   DATETIME2(0)      NULL,
        CreatedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_users_CreatedAt DEFAULT SYSDATETIME(),
        LastLoginAt    DATETIME2(0)      NULL
    );
END
GO

/* ----------------------------------------------------------- FlightTbl */
IF OBJECT_ID(N'dbo.FlightTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.FlightTbl
    (
        FCode         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_FlightTbl PRIMARY KEY,
        FlightNo      VARCHAR(8)        NOT NULL,
        Airline       NVARCHAR(60)      NOT NULL,
        Fsrc          CHAR(3)           NOT NULL CONSTRAINT FK_Flight_Src  REFERENCES dbo.AirportTbl(Code),
        FDest         CHAR(3)           NOT NULL CONSTRAINT FK_Flight_Dest REFERENCES dbo.AirportTbl(Code),
        DepartureTime DATETIME2(0)      NOT NULL,
        ArrivalTime   DATETIME2(0)      NOT NULL,
        BusinessSeats INT               NOT NULL CONSTRAINT DF_Flight_Bus DEFAULT 0,
        EconomySeats  INT               NOT NULL CONSTRAINT DF_Flight_Eco DEFAULT 0,
        FCap          AS (BusinessSeats + EconomySeats) PERSISTED,
        EconomyFare   DECIMAL(10,2)     NOT NULL,
        BusinessFare  DECIMAL(10,2)     NOT NULL,
        FStatus       NVARCHAR(12)      NOT NULL CONSTRAINT DF_Flight_Status DEFAULT N'Scheduled',
        CONSTRAINT CK_Flight_Route  CHECK (Fsrc <> FDest),
        CONSTRAINT CK_Flight_Times  CHECK (ArrivalTime > DepartureTime),
        CONSTRAINT CK_Flight_Seats  CHECK (BusinessSeats >= 0 AND EconomySeats >= 0 AND BusinessSeats + EconomySeats > 0),
        CONSTRAINT CK_Flight_Fares  CHECK (EconomyFare >= 0 AND BusinessFare >= 0),
        CONSTRAINT CK_Flight_Status CHECK (FStatus IN (N'Scheduled', N'Delayed', N'Cancelled')),
        CONSTRAINT UQ_Flight_NoTime UNIQUE (FlightNo, DepartureTime)
    );
    CREATE INDEX IX_FlightTbl_Search ON dbo.FlightTbl (Fsrc, FDest, DepartureTime);
END
GO

/* --------------------------------------------------------- BookingTbl */
IF OBJECT_ID(N'dbo.BookingTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BookingTbl
    (
        BookingId     INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_BookingTbl PRIMARY KEY,
        PNR           CHAR(6)           NOT NULL CONSTRAINT UQ_Booking_PNR UNIQUE,
        UserId        INT               NOT NULL CONSTRAINT FK_Booking_User   REFERENCES dbo.users(id),
        FCode         INT               NOT NULL CONSTRAINT FK_Booking_Flight REFERENCES dbo.FlightTbl(FCode),
        CabinClass    NVARCHAR(10)      NOT NULL,
        BookingStatus NVARCHAR(12)      NOT NULL CONSTRAINT DF_Booking_Status DEFAULT N'Confirmed',
        BookedAt      DATETIME2(0)      NOT NULL CONSTRAINT DF_Booking_BookedAt DEFAULT SYSDATETIME(),
        ContactEmail  NVARCHAR(120)     NOT NULL,
        ContactPhone  NVARCHAR(25)      NOT NULL,
        BaseFare      DECIMAL(10,2)     NOT NULL,
        AddOns        DECIMAL(10,2)     NOT NULL CONSTRAINT DF_Booking_AddOns DEFAULT 0,
        Taxes         DECIMAL(10,2)     NOT NULL,
        TotalAmount   DECIMAL(10,2)     NOT NULL,
        CancelledAt   DATETIME2(0)      NULL,
        RefundAmount  DECIMAL(10,2)     NULL,
        CONSTRAINT CK_Booking_Cabin  CHECK (CabinClass IN (N'Economy', N'Business')),
        CONSTRAINT CK_Booking_Status CHECK (BookingStatus IN (N'Confirmed', N'Cancelled'))
    );
    CREATE INDEX IX_BookingTbl_User   ON dbo.BookingTbl (UserId, BookedAt DESC);
    CREATE INDEX IX_BookingTbl_Flight ON dbo.BookingTbl (FCode);
END
GO

/* ----------------------------------------------------------- TicketTbl */
IF OBJECT_ID(N'dbo.TicketTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.TicketTbl
    (
        TicketId       INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_TicketTbl PRIMARY KEY,
        -- 13-digit e-ticket number derived from the key, so it is always unique
        TicketNo       AS ('999' + RIGHT('0000000000' + CAST(TicketId AS VARCHAR(10)), 10)) PERSISTED,
        BookingId      INT               NOT NULL CONSTRAINT FK_Ticket_Booking REFERENCES dbo.BookingTbl(BookingId),
        FCode          INT               NOT NULL CONSTRAINT FK_Ticket_Flight  REFERENCES dbo.FlightTbl(FCode),
        Name           NVARCHAR(100)     NOT NULL,
        Age            INT               NOT NULL,
        Gender         NVARCHAR(10)      NOT NULL,
        PassportNo     NVARCHAR(20)      NULL,
        CabinClass     NVARCHAR(10)      NOT NULL,
        SeatNo         VARCHAR(4)        NOT NULL,
        ExtraBaggageKg INT               NOT NULL CONSTRAINT DF_Ticket_Bag DEFAULT 0,
        Amount         DECIMAL(10,2)     NOT NULL,
        TicketStatus   NVARCHAR(12)      NOT NULL CONSTRAINT DF_Ticket_Status DEFAULT N'Confirmed',
        CONSTRAINT CK_Ticket_Status CHECK (TicketStatus IN (N'Confirmed', N'Cancelled')),
        CONSTRAINT CK_Ticket_Age    CHECK (Age BETWEEN 2 AND 120)
    );
    CREATE INDEX IX_TicketTbl_Booking ON dbo.TicketTbl (BookingId);
END
GO

/* One seat can only be held by ONE active ticket per flight. This index is
   what makes double-booking impossible, even with several users at once. */
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = N'UX_TicketTbl_ActiveSeat')
    CREATE UNIQUE INDEX UX_TicketTbl_ActiveSeat
        ON dbo.TicketTbl (FCode, SeatNo)
        WHERE TicketStatus = N'Confirmed';
GO

/* --------------------------------------------------------- PaymentTbl */
IF OBJECT_ID(N'dbo.PaymentTbl', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.PaymentTbl
    (
        PaymentId   INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_PaymentTbl PRIMARY KEY,
        BookingId   INT               NOT NULL CONSTRAINT FK_Payment_Booking REFERENCES dbo.BookingTbl(BookingId),
        PaymentType NVARCHAR(10)      NOT NULL,           -- Charge | Refund
        CardBrand   NVARCHAR(20)      NULL,
        CardLast4   CHAR(4)           NULL,               -- full card numbers are NEVER stored
        Amount      DECIMAL(10,2)     NOT NULL,
        GatewayRef  NVARCHAR(40)      NOT NULL,
        CreatedAt   DATETIME2(0)      NOT NULL CONSTRAINT DF_Payment_CreatedAt DEFAULT SYSDATETIME(),
        CONSTRAINT CK_Payment_Type   CHECK (PaymentType IN (N'Charge', N'Refund')),
        CONSTRAINT CK_Payment_Amount CHECK (Amount >= 0)
    );
    CREATE INDEX IX_PaymentTbl_Booking ON dbo.PaymentTbl (BookingId);
END
GO

/* ------------------------------------------------ seat availability view */
CREATE OR ALTER VIEW dbo.vw_FlightAvailability
AS
SELECT  f.FCode,
        f.BusinessSeats,
        f.EconomySeats,
        BusinessSold = (SELECT COUNT(*) FROM dbo.TicketTbl t
                         WHERE t.FCode = f.FCode AND t.CabinClass = N'Business' AND t.TicketStatus = N'Confirmed'),
        EconomySold  = (SELECT COUNT(*) FROM dbo.TicketTbl t
                         WHERE t.FCode = f.FCode AND t.CabinClass = N'Economy'  AND t.TicketStatus = N'Confirmed')
FROM    dbo.FlightTbl f;
GO

/* ------------------------------------------------- demo flight schedule
   Generates 45 days of flights counted from today, so the demo always has
   bookable flights. Only runs while FlightTbl is empty.                   */
IF NOT EXISTS (SELECT 1 FROM dbo.FlightTbl)
BEGIN
    DECLARE @Routes TABLE
    (
        RouteId  INT IDENTITY(1,1),
        Src      CHAR(3),
        Dest     CHAR(3),
        DepHour  INT,
        DepMin   INT,
        DurMin   INT,
        EcoFare  DECIMAL(10,2),
        BusFare  DECIMAL(10,2),
        BusSeats INT,
        EcoSeats INT
    );

    INSERT @Routes (Src, Dest, DepHour, DepMin, DurMin, EcoFare, BusFare, BusSeats, EcoSeats) VALUES
    -- domestic
    ('DAC','CGP',  7,30,  55,  42.00,  105.00,  8,  66),
    ('CGP','DAC',  9,15,  55,  42.00,  105.00,  8,  66),
    ('DAC','CGP', 17,45,  55,  46.00,  112.00,  8,  66),
    ('CGP','DAC', 19,30,  55,  46.00,  112.00,  8,  66),
    ('DAC','CXB',  8,10,  75,  58.00,  140.00,  8,  66),
    ('CXB','DAC', 10,20,  75,  58.00,  140.00,  8,  66),
    ('DAC','ZYL', 11, 0,  50,  48.00,  118.00,  8,  66),
    ('ZYL','DAC', 13, 0,  50,  48.00,  118.00,  8,  66),
    -- regional / international
    ('DAC','DXB',  2,40, 405, 285.00,  760.00, 16, 120),
    ('DXB','DAC', 10,15, 330, 285.00,  760.00, 16, 120),
    ('DAC','DOH',  1,50, 330, 310.00,  820.00, 16, 120),
    ('DOH','DAC',  9,30, 300, 310.00,  820.00, 16, 120),
    ('DAC','JED',  3,10, 480, 395.00,  990.00, 16, 120),
    ('JED','DAC', 12,40, 450, 395.00,  990.00, 16, 120),
    ('DAC','SIN',  9,50, 270, 255.00,  690.00, 16, 120),
    ('SIN','DAC', 14,10, 285, 255.00,  690.00, 16, 120),
    ('DAC','KUL', 10,30, 255, 230.00,  640.00, 16, 120),
    ('KUL','DAC', 16, 5, 270, 230.00,  640.00, 16, 120),
    ('DAC','BKK', 12,20, 190, 205.00,  575.00, 16, 120),
    ('BKK','DAC', 15,45, 175, 205.00,  575.00, 16, 120),
    ('DAC','DEL',  6,15, 135, 120.00,  340.00, 12,  96),
    ('DEL','DAC',  9,40, 135, 120.00,  340.00, 12,  96),
    ('DAC','CCU', 14, 0,  60,  72.00,  190.00,  8,  66),
    ('CCU','DAC', 16, 0,  60,  72.00,  190.00,  8,  66),
    ('DAC','IST', 22,30, 570, 610.00, 1650.00, 20, 150),
    ('IST','DAC',  1,15, 540, 610.00, 1650.00, 20, 150),
    ('DAC','LHR', 23,50, 660, 780.00, 2150.00, 24, 180),
    ('LHR','DAC', 21,10, 600, 780.00, 2150.00, 24, 180);

    DECLARE @Day INT = 1;
    WHILE @Day <= 45
    BEGIN
        INSERT dbo.FlightTbl
            (FlightNo, Airline, Fsrc, FDest, DepartureTime, ArrivalTime,
             BusinessSeats, EconomySeats, EconomyFare, BusinessFare, FStatus)
        SELECT  'SW' + CAST(100 + r.RouteId AS VARCHAR(4)),
                N'SkyWay Airlines',
                r.Src, r.Dest,
                d.Dep,
                DATEADD(MINUTE, r.DurMin, d.Dep),
                r.BusSeats, r.EcoSeats,
                CAST(r.EcoFare * (1 + (@Day % 5) * 0.03) AS DECIMAL(10,2)),
                CAST(r.BusFare * (1 + (@Day % 5) * 0.03) AS DECIMAL(10,2)),
                N'Scheduled'
        FROM    @Routes r
        CROSS APPLY (SELECT DATEADD(MINUTE, r.DepHour * 60 + r.DepMin,
                                    CAST(CAST(DATEADD(DAY, @Day, GETDATE()) AS DATE) AS DATETIME2(0))) AS Dep) d;

        SET @Day += 1;
    END
END
GO

/* Revision 2.1 integrity upgrades. Existing revised databases retain their data. */
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Flight_CapacityLimit' AND parent_object_id=OBJECT_ID(N'dbo.FlightTbl'))
    ALTER TABLE dbo.FlightTbl ADD CONSTRAINT CK_Flight_CapacityLimit CHECK (BusinessSeats + EconomySeats <= 600);
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Booking_Money' AND parent_object_id=OBJECT_ID(N'dbo.BookingTbl'))
    ALTER TABLE dbo.BookingTbl ADD CONSTRAINT CK_Booking_Money CHECK
    (BaseFare >= 0 AND AddOns >= 0 AND Taxes >= 0 AND TotalAmount = BaseFare + AddOns + Taxes AND
     (RefundAmount IS NULL OR (RefundAmount >= 0 AND RefundAmount <= TotalAmount)));
GO
IF NOT EXISTS (SELECT 1 FROM sys.check_constraints WHERE name=N'CK_Ticket_Values' AND parent_object_id=OBJECT_ID(N'dbo.TicketTbl'))
    ALTER TABLE dbo.TicketTbl ADD CONSTRAINT CK_Ticket_Values CHECK
    (Amount >= 0 AND CabinClass IN (N'Economy',N'Business') AND ExtraBaggageKg IN (0,10,20,30) AND Gender IN (N'Male',N'Female',N'Other'));
GO
IF NOT EXISTS (SELECT 1 FROM sys.key_constraints WHERE name=N'UQ_Booking_FlightCabin' AND parent_object_id=OBJECT_ID(N'dbo.BookingTbl'))
    ALTER TABLE dbo.BookingTbl ADD CONSTRAINT UQ_Booking_FlightCabin UNIQUE (BookingId,FCode,CabinClass);
GO
IF NOT EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name=N'FK_Ticket_BookingFlightCabin')
    ALTER TABLE dbo.TicketTbl ADD CONSTRAINT FK_Ticket_BookingFlightCabin FOREIGN KEY (BookingId,FCode,CabinClass)
        REFERENCES dbo.BookingTbl (BookingId,FCode,CabinClass);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Payment_BookingType' AND object_id=OBJECT_ID(N'dbo.PaymentTbl'))
    CREATE UNIQUE INDEX UX_Payment_BookingType ON dbo.PaymentTbl (BookingId,PaymentType);
GO
IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name=N'UX_Payment_GatewayRef' AND object_id=OBJECT_ID(N'dbo.PaymentTbl'))
    CREATE UNIQUE INDEX UX_Payment_GatewayRef ON dbo.PaymentTbl (GatewayRef);
GO

/* Profile and password reset extension; preserves existing accounts. */
IF COL_LENGTH(N'dbo.users',N'MustChangePassword') IS NULL
 ALTER TABLE dbo.users ADD MustChangePassword BIT NOT NULL CONSTRAINT DF_users_MustChange DEFAULT 0;
GO
IF COL_LENGTH(N'dbo.PassengerTbl',N'ProfilePhoto') IS NULL
 ALTER TABLE dbo.PassengerTbl ADD ProfilePhoto VARBINARY(MAX) NULL;
GO
IF OBJECT_ID(N'dbo.PasswordResetRequests',N'U') IS NULL
BEGIN
 CREATE TABLE dbo.PasswordResetRequests (
  RequestId INT IDENTITY PRIMARY KEY,
  UserId INT NOT NULL REFERENCES dbo.users(id),
  Reason NVARCHAR(500) NOT NULL,
  Status NVARCHAR(20) NOT NULL DEFAULT N'Pending',
  RequestedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME(),
  ReviewedAt DATETIME2(0) NULL,
  ReviewedBy INT NULL REFERENCES dbo.users(id),
  CONSTRAINT CK_Reset_Status CHECK(Status IN(N'Pending',N'Approved',N'Rejected'))
 );
 CREATE UNIQUE INDEX UX_Reset_Pending ON dbo.PasswordResetRequests(UserId) WHERE Status=N'Pending';
END
GO
IF OBJECT_ID(N'dbo.PasswordResetAudit',N'U') IS NULL
 CREATE TABLE dbo.PasswordResetAudit (
 AuditId INT IDENTITY PRIMARY KEY, UserId INT NOT NULL REFERENCES dbo.users(id),
 AdminId INT NOT NULL REFERENCES dbo.users(id), ChangedAt DATETIME2(0) NOT NULL DEFAULT SYSDATETIME());
GO
