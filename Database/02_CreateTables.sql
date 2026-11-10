/* FlightBookingDB v1 - 11 tables, aligned to flight_booking_erd(2).drawio.
   Intentional fixes/clarifications compared with the uploaded diagram:
     - TICKET.ticket_id is INT (diagram typo 'in').
     - BOOKING.total_amount DECIMAL(18,2).
     - Unicode human-readable fields use NVARCHAR.
     - Optional cap/limit: DISCOUNT_CODE.max_discount_amount, usage_limit nullable.
     - DATETIME in diagram represented as DATETIME2(0) for consistent precision.
     - No airport_code, flight_number or BOOKING.subtotal.
     - Four separate FLIGHT DATE/TIME fields are retained.
   Run only once on a new database; do not drop existing tables/data.
*/
USE FlightBookingDB;
GO
SET XACT_ABORT ON;
GO

CREATE TABLE dbo.AIRPORT
(
    airport_id INT IDENTITY(1,1) NOT NULL,
    airport_name NVARCHAR(150) NOT NULL,
    city NVARCHAR(100) NOT NULL,
    CONSTRAINT PK_AIRPORT PRIMARY KEY (airport_id)
);
GO

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

SELECT name AS TableName FROM sys.tables WHERE schema_id = SCHEMA_ID(N'dbo') ORDER BY name;
GO
