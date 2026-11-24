:ON ERROR EXIT
/* REVIEW ONLY. Do not execute until the user approves creation AND this schema snapshot.
   Required sqlcmd variables: EXPECTED_SERVER, CREATE_APPROVAL. No default approval.
   Source: 02,03,08 only. Main DB seed/demo scripts are deliberately excluded.
   Stage 1B: explicit SET options prevent SQL 1934. Use sqlcmd -I as well.
   Original executed version: History/01_CreateAndSchema-6EE11AF9.sql. */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
IF ISNULL(SESSIONPROPERTY('ANSI_NULLS'),-1)<>1 OR ISNULL(SESSIONPROPERTY('ANSI_PADDING'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('ANSI_WARNINGS'),-1)<>1 OR ISNULL(SESSIONPROPERTY('ARITHABORT'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('CONCAT_NULL_YIELDS_NULL'),-1)<>1 OR ISNULL(SESSIONPROPERTY('QUOTED_IDENTIFIER'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('NUMERIC_ROUNDABORT'),-1)<>0 THROW 51115,'Filtered-index SET options are incorrect.',1;

EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=NULL;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'master' THROW 51104,'Connect to master for creation.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Creation approval missing.',1;
IF DB_ID(N'FlightBooking_Hieu_ManagementTests') IS NOT NULL THROW 51105,'Database already exists; stop without changing it.',1;
IF ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE'),0)<>1 THROW 51106,'CREATE DATABASE permission is missing.',1;
EXEC(N'CREATE DATABASE [FlightBooking_Hieu_ManagementTests]');
EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=N'FlightBooking_Hieu_ManagementTests';
GO
USE [FlightBooking_Hieu_ManagementTests];
GO

-- Source: 02_CreateTables.sql; guarded batch 1
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
SET XACT_ABORT ON;
GO

-- Source: 02_CreateTables.sql; guarded batch 2
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.AIRPORT
(
    airport_id INT IDENTITY(1,1) NOT NULL,
    airport_name NVARCHAR(150) NOT NULL,
    city NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_AIRPORT PRIMARY KEY (airport_id)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 3
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.CUSTOMER
(
    customer_id INT IDENTITY(1,1) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    email VARCHAR(100) NOT NULL,
    date_of_birth DATE NOT NULL,
    gender NVARCHAR(10) NOT NULL,
    id_passport_number VARCHAR(50) NULL,
    CONSTRAINT PK_CUSTOMER PRIMARY KEY (customer_id)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 4
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.EMPLOYEE
(
    employee_id INT IDENTITY(1,1) NOT NULL,
    full_name NVARCHAR(100) NOT NULL,
    phone VARCHAR(20) NOT NULL,
    email VARCHAR(100) NOT NULL,
    position NVARCHAR(50) NOT NULL,
    hire_date DATE NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_EMPLOYEE_Status DEFAULT ('ACTIVE'),
    CONSTRAINT PK_EMPLOYEE PRIMARY KEY (employee_id),
    CONSTRAINT CK_EMPLOYEE_Status CHECK (status IN ('ACTIVE','INACTIVE'))
);
GO

-- Source: 02_CreateTables.sql; guarded batch 5
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.DISCOUNT_CODE
(
    discount_id INT IDENTITY(1,1) NOT NULL,
    code VARCHAR(50) NOT NULL,
    description NVARCHAR(255) NOT NULL,
    discount_type VARCHAR(10) NOT NULL,
    discount_value DECIMAL(18,2) NOT NULL,
    min_order_amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_DISCOUNT_MinOrder DEFAULT (0),
    max_discount_amount DECIMAL(18,2) NULL,
    start_date DATETIME2(0) NOT NULL,
    end_date DATETIME2(0) NOT NULL,
    usage_limit INT NULL,
    used_count INT NOT NULL CONSTRAINT DF_DISCOUNT_Used DEFAULT (0),
    status VARCHAR(20) NOT NULL CONSTRAINT DF_DISCOUNT_Status DEFAULT ('ACTIVE'),
    CONSTRAINT PK_DISCOUNT_CODE PRIMARY KEY (discount_id),
    CONSTRAINT UQ_DISCOUNT_CODE_Code UNIQUE (code),
    CONSTRAINT CK_DISCOUNT_Type CHECK (discount_type IN ('PERCENT','FIXED')),
    CONSTRAINT CK_DISCOUNT_Value CHECK
       (discount_value > 0 AND
        (discount_type <> 'PERCENT' OR discount_value <= 100)),
    CONSTRAINT CK_DISCOUNT_MinOrder CHECK (min_order_amount >= 0),
    CONSTRAINT CK_DISCOUNT_MaxDiscount CHECK
       (max_discount_amount IS NULL OR max_discount_amount >= 0),
    CONSTRAINT CK_DISCOUNT_Dates CHECK (end_date > start_date),
    CONSTRAINT CK_DISCOUNT_Usage CHECK
       (used_count >= 0 AND (usage_limit IS NULL OR
         (usage_limit >= 0 AND used_count <= usage_limit))),
    CONSTRAINT CK_DISCOUNT_Status CHECK (status IN ('ACTIVE','INACTIVE'))
);
GO

-- Source: 02_CreateTables.sql; guarded batch 6
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.FLIGHT
(
    flight_id INT IDENTITY(1,1) NOT NULL,
    departure_airport_id INT NOT NULL,
    arrival_airport_id INT NOT NULL,
    total_seats INT NOT NULL,
    tickets_sold INT NOT NULL CONSTRAINT DF_FLIGHT_Sold DEFAULT (0),
    seats_available INT NOT NULL,
    total_revenue DECIMAL(18,2) NOT NULL CONSTRAINT DF_FLIGHT_Revenue DEFAULT (0),
    departure_date DATE NOT NULL,
    departure_time TIME(0) NOT NULL,
    arrival_date DATE NOT NULL,
    arrival_time TIME(0) NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_FLIGHT_Status DEFAULT ('SCHEDULED'),
    CONSTRAINT PK_FLIGHT PRIMARY KEY (flight_id),
    CONSTRAINT FK_FLIGHT_DepartureAirport FOREIGN KEY (departure_airport_id)
        REFERENCES dbo.AIRPORT(airport_id),
    CONSTRAINT FK_FLIGHT_ArrivalAirport FOREIGN KEY (arrival_airport_id)
        REFERENCES dbo.AIRPORT(airport_id),
    CONSTRAINT CK_FLIGHT_DifferentAirports CHECK
        (departure_airport_id <> arrival_airport_id),
    CONSTRAINT CK_FLIGHT_SeatCount CHECK
        (total_seats > 0 AND tickets_sold >= 0 AND seats_available >= 0
         AND tickets_sold + seats_available = total_seats),
    CONSTRAINT CK_FLIGHT_Revenue CHECK (total_revenue >= 0),
    CONSTRAINT CK_FLIGHT_Chronology CHECK
       (arrival_date > departure_date OR
         (arrival_date = departure_date AND arrival_time > departure_time)),
    CONSTRAINT CK_FLIGHT_Status CHECK
       (status IN ('SCHEDULED','CANCELLED','COMPLETED'))
);
GO

-- Source: 02_CreateTables.sql; guarded batch 7
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.FARE_CLASS
(
    fare_id INT IDENTITY(1,1) NOT NULL,
    flight_id INT NOT NULL,
    class_name NVARCHAR(50) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    base_baggage_kg INT NOT NULL,
    CONSTRAINT PK_FARE_CLASS PRIMARY KEY (fare_id),
    CONSTRAINT FK_FARE_CLASS_Flight FOREIGN KEY (flight_id)
       REFERENCES dbo.FLIGHT(flight_id),
    CONSTRAINT UQ_FARE_CLASS_FlightClass UNIQUE (flight_id,class_name),
    /* Composite FK for TICKET: fare_id must belong to the same flight_id */
    CONSTRAINT UQ_FARE_CLASS_FareFlight UNIQUE (fare_id,flight_id),
    CONSTRAINT CK_FARE_CLASS_Price CHECK (price >= 0),
    CONSTRAINT CK_FARE_CLASS_Baggage CHECK (base_baggage_kg >= 0)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 8
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.TICKET
(
    ticket_id INT IDENTITY(1,1) NOT NULL,
    flight_id INT NOT NULL,
    fare_id INT NOT NULL,
    seat_number VARCHAR(10) NOT NULL,
    issued_at DATETIME2(0) NOT NULL CONSTRAINT DF_TICKET_IssuedAt DEFAULT SYSDATETIME(),
    status VARCHAR(20) NOT NULL CONSTRAINT DF_TICKET_Status DEFAULT ('ISSUED'),
    CONSTRAINT PK_TICKET PRIMARY KEY (ticket_id),
    CONSTRAINT FK_TICKET_Flight FOREIGN KEY (flight_id)
       REFERENCES dbo.FLIGHT(flight_id),
    CONSTRAINT FK_TICKET_FareFlight FOREIGN KEY (fare_id,flight_id)
       REFERENCES dbo.FARE_CLASS(fare_id,flight_id),
    CONSTRAINT CK_TICKET_Status CHECK (status IN ('ISSUED','CANCELLED')),
    CONSTRAINT CK_TICKET_SeatNotEmpty CHECK (LEN(LTRIM(RTRIM(seat_number))) > 0)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 9
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.BOOKING
(
    booking_id INT IDENTITY(1,1) NOT NULL,
    customer_id INT NOT NULL,
    employee_id INT NULL,
    flight_id INT NOT NULL,
    discount_id INT NULL,
    booking_date DATETIME2(0) NOT NULL CONSTRAINT DF_BOOKING_Date DEFAULT SYSDATETIME(),
    booking_source VARCHAR(10) NOT NULL,
    ticket_quantity INT NOT NULL,
    discount_amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_BOOKING_Discount DEFAULT (0),
    total_amount DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_BOOKING_Status DEFAULT ('CONFIRMED'),
    CONSTRAINT PK_BOOKING PRIMARY KEY (booking_id),
    CONSTRAINT FK_BOOKING_Customer FOREIGN KEY (customer_id)
       REFERENCES dbo.CUSTOMER(customer_id),
    CONSTRAINT FK_BOOKING_Employee FOREIGN KEY (employee_id)
       REFERENCES dbo.EMPLOYEE(employee_id),
    CONSTRAINT FK_BOOKING_Flight FOREIGN KEY (flight_id)
       REFERENCES dbo.FLIGHT(flight_id),
    CONSTRAINT FK_BOOKING_Discount FOREIGN KEY (discount_id)
       REFERENCES dbo.DISCOUNT_CODE(discount_id),
    CONSTRAINT CK_BOOKING_Source CHECK (booking_source IN ('WEB','COUNTER')),
    CONSTRAINT CK_BOOKING_SourceEmployee CHECK
       ((booking_source = 'WEB' AND employee_id IS NULL)
        OR (booking_source = 'COUNTER' AND employee_id IS NOT NULL)),
    CONSTRAINT CK_BOOKING_Quantity CHECK (ticket_quantity > 0),
    CONSTRAINT CK_BOOKING_Amounts CHECK (discount_amount >= 0 AND total_amount >= 0),
    CONSTRAINT CK_BOOKING_Status CHECK (status IN ('CONFIRMED','CANCELLED'))
);
GO

-- Source: 02_CreateTables.sql; guarded batch 10
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.BOOKING_DETAIL
(
    booking_detail_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    ticket_id INT NOT NULL,
    passenger_full_name NVARCHAR(100) NOT NULL,
    passenger_id_card VARCHAR(50) NOT NULL,
    price DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_BOOKING_DETAIL PRIMARY KEY (booking_detail_id),
    CONSTRAINT FK_BOOKING_DETAIL_Booking FOREIGN KEY (booking_id)
       REFERENCES dbo.BOOKING(booking_id),
    CONSTRAINT FK_BOOKING_DETAIL_Ticket FOREIGN KEY (ticket_id)
       REFERENCES dbo.TICKET(ticket_id),
    CONSTRAINT UQ_BOOKING_DETAIL_Ticket UNIQUE (ticket_id),
    CONSTRAINT CK_BOOKING_DETAIL_Price CHECK (price >= 0)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 11
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.INVOICE
(
    invoice_id INT IDENTITY(1,1) NOT NULL,
    booking_id INT NOT NULL,
    invoice_date DATETIME2(0) NOT NULL CONSTRAINT DF_INVOICE_Date DEFAULT SYSDATETIME(),
    total DECIMAL(18,2) NOT NULL,
    discount_amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_INVOICE_Discount DEFAULT (0),
    tax_amount DECIMAL(18,2) NOT NULL CONSTRAINT DF_INVOICE_Tax DEFAULT (0),
    total_amount DECIMAL(18,2) NOT NULL,
    status VARCHAR(20) NOT NULL CONSTRAINT DF_INVOICE_Status DEFAULT ('UNPAID'),
    CONSTRAINT PK_INVOICE PRIMARY KEY (invoice_id),
    CONSTRAINT FK_INVOICE_Booking FOREIGN KEY (booking_id)
       REFERENCES dbo.BOOKING(booking_id),
    CONSTRAINT UQ_INVOICE_Booking UNIQUE (booking_id),
    CONSTRAINT CK_INVOICE_Amounts CHECK
       (total >= 0 AND discount_amount >= 0 AND tax_amount >= 0
        AND discount_amount <= total
        AND total_amount = total - discount_amount + tax_amount),
    CONSTRAINT CK_INVOICE_Status CHECK
       (status IN ('UNPAID','PAID','CANCELLED'))
);
GO

-- Source: 02_CreateTables.sql; guarded batch 12
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE TABLE dbo.INVOICE_DETAIL
(
    invoice_detail_id INT IDENTITY(1,1) NOT NULL,
    invoice_id INT NOT NULL,
    ticket_id INT NOT NULL,
    unit_price DECIMAL(18,2) NOT NULL,
    quantity INT NOT NULL CONSTRAINT DF_INVOICE_DETAIL_Quantity DEFAULT (1),
    subtotal DECIMAL(18,2) NOT NULL,
    CONSTRAINT PK_INVOICE_DETAIL PRIMARY KEY (invoice_detail_id),
    CONSTRAINT FK_INVOICE_DETAIL_Invoice FOREIGN KEY (invoice_id)
       REFERENCES dbo.INVOICE(invoice_id),
    CONSTRAINT FK_INVOICE_DETAIL_Ticket FOREIGN KEY (ticket_id)
       REFERENCES dbo.TICKET(ticket_id),
    CONSTRAINT UQ_INVOICE_DETAIL_Ticket UNIQUE (ticket_id),
    CONSTRAINT CK_INVOICE_DETAIL_Amounts CHECK
       (unit_price >= 0 AND quantity = 1 AND subtotal = unit_price * quantity)
);
GO

-- Source: 02_CreateTables.sql; guarded batch 13
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
SELECT name AS TableName FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') ORDER BY name;
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 14
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
/* Extra indexes, after 02_CreateTables.sql */
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 15
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
/* One active ticket per seat per flight; keep cancelled ticket history. */
CREATE UNIQUE NONCLUSTERED INDEX UX_TICKET_ActiveSeat
ON dbo.TICKET(flight_id, seat_number)
WHERE status = 'ISSUED';
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 16
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE INDEX IX_FLIGHT_RouteDate
ON dbo.FLIGHT(departure_airport_id, arrival_airport_id, departure_date);
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 17
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE INDEX IX_BOOKING_FlightDate
ON dbo.BOOKING(flight_id, booking_date);
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 18
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE INDEX IX_BOOKING_DETAIL_Booking
ON dbo.BOOKING_DETAIL(booking_id);
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 19
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE INDEX IX_INVOICE_DETAIL_Invoice
ON dbo.INVOICE_DETAIL(invoice_id);
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 20
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
CREATE INDEX IX_INVOICE_StatusDate
ON dbo.INVOICE(status, invoice_date);
GO

-- Source: 03_Constraints_Indexes.sql; guarded batch 21
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
SELECT name, is_unique, has_filter, filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.TICKET') AND name = N'UX_TICKET_ActiveSeat';
GO

-- Source: 08_BookingWebRequest.sql; guarded batch 22
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
/* PROPOSED extension: review with Hieu before applying to the shared database.
   Durable idempotency and private booking access cannot be represented in the
   original 11 tables without overloading business columns. No existing data is
   changed. Apply once after 01..03; safe to rerun. Store hashes, never raw keys. */
GO

-- Source: 08_BookingWebRequest.sql; guarded batch 23
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
IF OBJECT_ID(N'dbo.BOOKING_WEB_REQUEST', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.BOOKING_WEB_REQUEST
    (
        request_hash CHAR(64) NOT NULL CONSTRAINT PK_BOOKING_WEB_REQUEST PRIMARY KEY,
        payload_hash CHAR(64) NOT NULL,
        booking_id INT NOT NULL CONSTRAINT UQ_BOOKING_WEB_REQUEST_Booking UNIQUE,
        CONSTRAINT FK_BOOKING_WEB_REQUEST_Booking FOREIGN KEY (booking_id)
            REFERENCES dbo.BOOKING(booking_id)
    );
END;
GO

IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'FlightBooking_Hieu_ManagementTests' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
EXEC sys.sp_addextendedproperty @name=N'BookingFlight.TestOnly',@value=N'HieuThinh-W7-20261124';
EXEC sys.sp_addextendedproperty @name=N'BookingFlight.SchemaSource',@value=N'B6297CED0C16E1050E6BA00FBA93A60BBBBC1157622289556918CF61B0416D34';
EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=NULL;
SELECT DB_NAME() AS PreparedTestDatabase;
GO
