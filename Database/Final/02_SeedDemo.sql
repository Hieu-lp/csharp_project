:ON ERROR EXIT
-- REVIEWABLE DEMO PROFILE: 8 airports,25 customers,7 employees,6 discounts,
-- 43 flights,85 fares,81 bookings/invoices,141 tickets/details,41 web requests.
-- Confirm required minimum quantities before executing this profile.
-- Derived from 04/07; old scripts are NOT executed and never changed.
-- Future flights are relative to setup day; historical statistics cover prior year.
-- Full-used discount has one real linked demo booking; all counters reconcile.
-- Setup/seed by DBA is distinct from automated CRUD/payment testing by a writer.
USE [FlightBooking_Final];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'FlightBooking_Final' OR CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 THROW 51250,'Wrong server/database for Final seed.',1;
IF N'$(SEED_APPROVAL)'<>N'APPROVE_SEED_FLIGHTBOOKING_FINAL_DEMO' OR N'$(SEED_PROFILE)'<>N'DEMO_8_25_7_6_43_85_81_141'
 THROW 51251,'Expected approved seed profile/approval is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalDemo' AND CONVERT(nvarchar(128),value)=N'Final-20261127-v1')
 THROW 51252,'Final marker missing.',1;
DECLARE @MajorVersion int=TRY_CONVERT(int,SERVERPROPERTY('ProductMajorVersion'));
IF ISNULL(IS_SRVROLEMEMBER('sysadmin'),0)<>1 AND
 (@MajorVersion IS NULL OR @MajorVersion<=0 OR
  (@MajorVersion>=16 AND ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER PERFORMANCE STATE'),0)<>1) OR
  (@MajorVersion<16 AND ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER STATE'),0)<>1))
 THROW 51259,'Session visibility is required to verify exclusive seed use. Do not bypass this guard.',1;
BEGIN TRANSACTION;
DECLARE @LockResult int;
EXEC @LockResult=sys.sp_getapplock @Resource=N'BookingFlight.Final.Setup',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0;
IF @LockResult<0 THROW 51253,'Another Final setup is running.',1;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalSeed')
 OR EXISTS(SELECT 1 FROM sys.identity_columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.is_ms_shipped=0 AND c.last_value IS NOT NULL)
 THROW 51254,'Database was already used or seed attempted. Stop; never overwrite/reseed.',1;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1 AND database_id=DB_ID() AND session_id<>@@SPID)
 THROW 51255,'Close applications/sessions using Final before seeding.',1;
IF EXISTS(SELECT 1 FROM dbo.[AIRPORT])
 OR EXISTS(SELECT 1 FROM dbo.[CUSTOMER])
 OR EXISTS(SELECT 1 FROM dbo.[EMPLOYEE])
 OR EXISTS(SELECT 1 FROM dbo.[DISCOUNT_CODE])
 OR EXISTS(SELECT 1 FROM dbo.[FLIGHT])
 OR EXISTS(SELECT 1 FROM dbo.[FARE_CLASS])
 OR EXISTS(SELECT 1 FROM dbo.[TICKET])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING_DETAIL])
 OR EXISTS(SELECT 1 FROM dbo.[INVOICE])
 OR EXISTS(SELECT 1 FROM dbo.[INVOICE_DETAIL])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING_WEB_REQUEST])
 THROW 51256,'All 12 tables must be empty before first seed.',1;
GO
-- Adapted original demo block
/* Demo data for the new 11-table schema. Fictional people/identifiers.
   One confirmed WEB booking, one paid invoice, one active ticket, plus
   11 round-trip flight pairs (Nov 2026 .. Jan 2027) and their fare classes.
   Run ONCE after steps 01-03 on a fresh DB. No real passenger data. */


SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.AIRPORT)
       OR EXISTS (SELECT 1 FROM dbo.FLIGHT)
       OR EXISTS (SELECT 1 FROM dbo.BOOKING)
    BEGIN
        THROW 51001, N'Da co du lieu. Chi chay seed nay tren Database moi, khong chay lai de tranh trung.', 1;
    END;

    DECLARE @AirportSGN INT, @AirportHAN INT, @AirportDAD INT, @CustomerId INT, @EmployeeId INT;
    DECLARE @DiscountId INT, @FlightId INT, @FareId INT, @TicketId INT;
    DECLARE @BookingId INT, @InvoiceId INT;

    INSERT INTO dbo.AIRPORT(airport_name, city)
    VALUES (N'Sân bay Tân Sơn Nhất', N'TP. Hồ Chí Minh');
    SET @AirportSGN = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.AIRPORT(airport_name, city)
    VALUES (N'Sân bay Nội Bài', N'Hà Nội');
    SET @AirportHAN = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.AIRPORT(airport_name, city)
    VALUES (N'San bay Da Nang', N'Da Nang');
    SET @AirportDAD = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.CUSTOMER(full_name, phone, email, date_of_birth, gender, id_passport_number)
    VALUES (N'Khách hàng Demo', '0900000001', 'customer.demo@example.test', '2000-05-10', N'Khác', NULL);
    SET @CustomerId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.EMPLOYEE(full_name, phone, email, position, hire_date, status)
    VALUES (N'Nhân viên Demo', '0900000002', 'staff.demo@example.test', N'Nhân viên bán vé', '2026-01-01', 'ACTIVE');
    SET @EmployeeId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,
       min_order_amount,max_discount_amount,start_date,end_date,usage_limit,used_count,status)
    VALUES ('DEMO10',N'Giảm 10% cho đơn đặt vé thử nghiệm','PERCENT',10.00,
       0,200000.00,DATEFROMPARTS(YEAR(SYSDATETIME())-1,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())+2,12,31),100,0,'ACTIVE');
    SET @DiscountId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,
       tickets_sold,seats_available,total_revenue,
       departure_date,departure_time,arrival_date,arrival_time,status)
    VALUES (@AirportSGN,@AirportHAN,100,0,100,0,
       DATEADD(DAY,7,CAST(SYSDATETIME() AS date)),'08:30:00',DATEADD(DAY,7,CAST(SYSDATETIME() AS date)),'10:45:00','SCHEDULED');
    SET @FlightId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg)
    VALUES (@FlightId,N'Economy',1200000.00,7);
    SET @FareId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.TICKET(flight_id,fare_id,seat_number,issued_at,status)
    VALUES (@FlightId,@FareId,'12A',SYSDATETIME(),'ISSUED');
    SET @TicketId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.BOOKING(customer_id,employee_id,flight_id,discount_id,
       booking_date,booking_source,ticket_quantity,discount_amount,total_amount,status)
    VALUES (@CustomerId,NULL,@FlightId,@DiscountId,SYSDATETIME(),
       'WEB',1,120000.00,1080000.00,'CONFIRMED');
    SET @BookingId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
    VALUES (@BookingId,@TicketId,N'Hành khách Demo','DEMO-ID-0001',1200000.00);

    INSERT INTO dbo.INVOICE(booking_id,invoice_date,total,discount_amount,tax_amount,total_amount,status)
    VALUES (@BookingId,SYSDATETIME(),1200000.00,120000.00,0,1080000.00,'PAID');
    SET @InvoiceId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal)
    VALUES (@InvoiceId,@TicketId,1200000.00,1,1200000.00);

    /* Current counters must be maintained atomically with ticket/invoice operations. */
    UPDATE dbo.FLIGHT
    SET tickets_sold = tickets_sold + 1,
        seats_available = seats_available - 1,
        total_revenue = total_revenue + 1080000.00
    WHERE flight_id = @FlightId AND seats_available >= 1;
    IF @@ROWCOUNT <> 1 THROW 51002, N'Khong du ghe de cap ve.', 1;

    UPDATE dbo.DISCOUNT_CODE SET used_count = used_count + 1
    WHERE discount_id = @DiscountId AND (usage_limit IS NULL OR used_count < usage_limit);
    IF @@ROWCOUNT <> 1 THROW 51003, N'Ma giam gia het luot.', 1;

    /* Round-trip demo flights are part of the original seed. Each journey is
       two FLIGHT rows; no extra booking, passenger, ticket or invoice is made. */
    DECLARE @RoundTripNowVN DATETIME2(0) = CONVERT(DATETIME2(0),
        SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
    DECLARE @RoundTripAirports TABLE (code VARCHAR(3) PRIMARY KEY, airport_id INT NOT NULL);
    INSERT INTO @RoundTripAirports VALUES
        ('SGN', @AirportSGN), ('HAN', @AirportHAN), ('DAD', @AirportDAD);

    DECLARE @RoundTripPairs TABLE (code VARCHAR(24) PRIMARY KEY,
        origin VARCHAR(3), destination VARCHAR(3), outbound_date DATE,
        return_date DATE, price DECIMAL(18,2));
    INSERT INTO @RoundTripPairs
    SELECT 'FINAL-'+CONVERT(varchar(3),n),origin,destination,
      DATEADD(DAY,10+(n-1)*3,CAST(SYSDATETIME() AS date)),
      DATEADD(DAY,12+(n-1)*3,CAST(SYSDATETIME() AS date)),price
    FROM (VALUES
      (1,'SGN','HAN',1100000),
      (2,'SGN','HAN',1200000),
      (3,'DAD','HAN',950000),
      (4,'SGN','DAD',850000),
      (5,'SGN','HAN',1250000),
      (6,'DAD','HAN',1000000),
      (7,'SGN','DAD',900000),
      (8,'SGN','HAN',1500000),
      (9,'SGN','HAN',1300000),
      (10,'DAD','HAN',1050000),
      (11,'SGN','DAD',950000)) v(n,origin,destination,price);

    DECLARE @RoundTripLegs TABLE (origin VARCHAR(3), destination VARCHAR(3),
        flight_date DATE, departure_time TIME(0), arrival_time TIME(0), price DECIMAL(18,2));
    INSERT INTO @RoundTripLegs
    SELECT origin,destination,outbound_date,'07:15','09:25',price FROM @RoundTripPairs
    UNION ALL
    SELECT destination,origin,return_date,'13:40','15:50',price+50000 FROM @RoundTripPairs;

    INSERT INTO dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,
        tickets_sold,seats_available,total_revenue,
        departure_date,departure_time,arrival_date,arrival_time,status)
    SELECT a.airport_id,b.airport_id,120,0,120,0,
        l.flight_date,l.departure_time,l.flight_date,l.arrival_time,
        CASE WHEN DATEADD(MINUTE,DATEPART(HOUR,l.arrival_time)*60+DATEPART(MINUTE,l.arrival_time),
            CONVERT(DATETIME2(0),l.flight_date)) <= @RoundTripNowVN
            THEN 'COMPLETED' ELSE 'SCHEDULED' END
    FROM @RoundTripLegs l
    JOIN @RoundTripAirports a ON a.code=l.origin
    JOIN @RoundTripAirports b ON b.code=l.destination;
    IF @@ROWCOUNT <> 22 THROW 51004, N'Chua tao du 22 chuyen bay khu hoi.', 1;

    INSERT INTO dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg)
    SELECT f.flight_id,x.class_name,x.price,x.baggage
    FROM @RoundTripLegs l
    JOIN @RoundTripAirports a ON a.code=l.origin
    JOIN @RoundTripAirports b ON b.code=l.destination
    JOIN dbo.FLIGHT f ON f.departure_airport_id=a.airport_id AND f.arrival_airport_id=b.airport_id
        AND f.departure_date=l.flight_date AND f.departure_time=l.departure_time
        AND f.arrival_date=l.flight_date AND f.arrival_time=l.arrival_time
    CROSS APPLY (VALUES (N'Economy',l.price,7),(N'Business',l.price+900000,20))
        x(class_name,price,baggage);
    IF @@ROWCOUNT <> 44 THROW 51005, N'Chua tao du 44 hang ve cho cac chuyen khu hoi.', 1;

    COMMIT TRANSACTION;


END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;

GO
-- Adapted expanded demo block
/*
  FLIGHT BOOKING DB — EXTENDED FICTIONAL DEMO DATA
  ------------------------------------------------
  Requires the schema from 01..03 and the original seed from 04.
  Intended for demonstration and grading: many flights, customers, web/counter
  bookings, multiple fare classes, paid/unpaid/cancelled invoices.
  Run ONCE on a development/demo database. This file uses a transaction and
  refuses to run twice. It DOES NOT wipe existing data.
  Everything is fictional; no real passenger information is used.
*/


SET NOCOUNT ON;
SET XACT_ABORT ON;

BEGIN TRY
    BEGIN TRANSACTION;

    IF OBJECT_ID(N'dbo.BOOKING_DETAIL', N'U') IS NULL
       OR OBJECT_ID(N'dbo.INVOICE_DETAIL', N'U') IS NULL
       OR OBJECT_ID(N'dbo.TICKET', N'U') IS NULL
       THROW 52001, N'Chua co day du schema. Hay chay cac script 01-03.', 1;

    IF NOT EXISTS (SELECT 1 FROM dbo.DISCOUNT_CODE WHERE code = 'DEMO10')
       THROW 52002, N'Chua co du lieu goc. Hay chay script 04 truoc.', 1;

    IF EXISTS (SELECT 1 FROM dbo.DISCOUNT_CODE WHERE code = 'EXPAND5')
       THROW 52003, N'Du lieu mo rong da ton tai. Khong chay lai file 07.', 1;

    /* Add only missing airports; seed 04 now also provides Da Nang. */
    INSERT INTO dbo.AIRPORT(airport_name,city)
    SELECT a.airport_name,a.city
    FROM (VALUES
        (N'San bay Da Nang',N'Da Nang'),
        (N'San bay Cam Ranh',N'Khanh Hoa'),
        (N'San bay Phu Quoc',N'Kien Giang'),
        (N'San bay Cat Bi',N'Hai Phong'),
        (N'San bay Phu Bai',N'Hue'),
        (N'San bay Lien Khuong',N'Lam Dong')) a(airport_name,city)
    WHERE NOT EXISTS (SELECT 1 FROM dbo.AIRPORT existing
        WHERE existing.airport_name=a.airport_name AND existing.city=a.city);

    /* 24 fictional customers, all identifiable by reserved demo email pattern. */
    DECLARE @i INT = 1;
    WHILE @i <= 24
    BEGIN
        INSERT INTO dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
        VALUES (
            N'Khach demo ' + RIGHT('00'+CAST(@i AS VARCHAR(2)),2),
            '0888'+RIGHT('000000'+CAST(@i AS VARCHAR(6)),6),
            'flight.demo.'+RIGHT('00'+CAST(@i AS VARCHAR(2)),2)+'@example.test',
            DATEFROMPARTS(1989+(@i%16),1+(@i%12),1+(@i%27)),
            CASE WHEN @i%2=0 THEN N'Nam' ELSE N'Nu' END,
            'FICTIONAL-'+RIGHT('0000'+CAST(@i AS VARCHAR(4)),4));
        SET @i += 1;
    END;

    /* 6 fictional employees. */
    SET @i = 1;
    WHILE @i <= 6
    BEGIN
        INSERT INTO dbo.EMPLOYEE(full_name,phone,email,position,hire_date,status)
        VALUES (
            N'Nhan vien demo '+CAST(@i AS NVARCHAR(3)),
            '0777'+RIGHT('000000'+CAST(@i AS VARCHAR(6)),6),
            'flight.employee.'+CAST(@i AS VARCHAR(3))+'@example.test',
            CASE WHEN @i <= 4 THEN N'Nhan vien ban ve' ELSE N'Nhan vien quan ly' END,
            DATEFROMPARTS(2023+(@i%3),1+(@i%10),10),
            CASE WHEN @i=6 THEN 'INACTIVE' ELSE 'ACTIVE' END);
        SET @i += 1;
    END;

    /* More discount cases: percent, fixed, expired, exhausted, future. */
    INSERT INTO dbo.DISCOUNT_CODE
       (code,description,discount_type,discount_value,min_order_amount,
        max_discount_amount,start_date,end_date,usage_limit,used_count,status)
    VALUES
       ('EXPAND5',N'Demo: giam 5 phan tram','PERCENT',5,0,200000,DATEFROMPARTS(YEAR(SYSDATETIME())-1,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())+2,12,31),100,0,'ACTIVE'),
       ('SAVE100',N'Demo: giam 100 nghin','FIXED',100000,500000,NULL,DATEFROMPARTS(YEAR(SYSDATETIME())-1,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())+2,12,31),100,0,'ACTIVE'),
       ('EXPIRED10',N'Demo: ma het han','PERCENT',10,0,200000,DATEFROMPARTS(YEAR(SYSDATETIME())-2,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())-1,12,31),30,0,'ACTIVE'),
       ('FULLUSED',N'Demo: ma het luot','FIXED',30000.01,0,NULL,DATEFROMPARTS(YEAR(SYSDATETIME())-1,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())+2,12,31),1,0,'ACTIVE'),
       ('FUTURE15',N'Demo: ma chua den ngay','PERCENT',15,0,150000,DATEFROMPARTS(YEAR(SYSDATETIME())+2,1,1),DATEFROMPARTS(YEAR(SYSDATETIME())+3,12,31),100,0,'ACTIVE');

    DECLARE @Airports TABLE (rn INT IDENTITY(1,1) PRIMARY KEY, airport_id INT NOT NULL);
    DECLARE @Customers TABLE (rn INT IDENTITY(1,1) PRIMARY KEY, customer_id INT NOT NULL);
    DECLARE @Employees TABLE (rn INT IDENTITY(1,1) PRIMARY KEY, employee_id INT NOT NULL);

    INSERT INTO @Airports(airport_id) SELECT airport_id FROM dbo.AIRPORT ORDER BY airport_id;
    INSERT INTO @Customers(customer_id) SELECT customer_id FROM dbo.CUSTOMER
       WHERE email LIKE 'flight.demo.%@example.test' ORDER BY customer_id;
    INSERT INTO @Employees(employee_id) SELECT employee_id FROM dbo.EMPLOYEE
       WHERE email LIKE 'flight.employee.%@example.test' AND status='ACTIVE' ORDER BY employee_id;

    IF (SELECT COUNT(*) FROM @Airports) < 8
       OR (SELECT COUNT(*) FROM @Customers) <> 24
       OR (SELECT COUNT(*) FROM @Employees) < 5
       THROW 52004, N'Du lieu danh muc demo khong dung so luong du kien.', 1;

    DECLARE @j INT, @p INT, @FlightId INT, @FareEconomy INT, @FareBusiness INT;
    DECLARE @DepAirport INT, @ArrAirport INT, @FlightDate DATE, @ArrivalDate DATE;
    DECLARE @DepTime TIME(0), @ArrTime TIME(0), @FlightStatus VARCHAR(20);
    DECLARE @CustomerId INT, @EmployeeId INT, @BookingId INT, @InvoiceId INT;
    DECLARE @TicketId INT, @Qty INT, @SeatNo VARCHAR(10);
    DECLARE @BookingStatus VARCHAR(20), @TicketStatus VARCHAR(20), @InvoiceStatus VARCHAR(20);
    DECLARE @BookingSource VARCHAR(10), @DiscountId INT, @DiscountAmount DECIMAL(18,2);
    DECLARE @Subtotal DECIMAL(18,2), @Total DECIMAL(18,2), @Price DECIMAL(18,2);
    DECLARE @BookingDate DATETIME2(0), @InvoiceDate DATETIME2(0);
    DECLARE @IssuedTickets INT = 0, @PaidRevenue DECIMAL(18,2) = 0;

    /* 20 flights across all four quarters of 2026, including some upcoming flights. */
    SET @i = 1;
    WHILE @i <= 20
    BEGIN
        SELECT @DepAirport=airport_id FROM @Airports WHERE rn=((@i-1)%8)+1;
        SELECT @ArrAirport=airport_id FROM @Airports WHERE rn=((@i+2)%8)+1;

        IF @i <= 16
            SET @FlightDate=DATEFROMPARTS(YEAR(SYSDATETIME())-1,1+(((@i-1)%4)*3),11+((@i-1)/4)*5);
        ELSE
            SET @FlightDate=DATEADD(DAY,14+(@i-17)*10,CAST(SYSDATETIME() AS date));

        IF @i%3=0
        BEGIN
            SET @DepTime='23:10:00'; SET @ArrTime='01:20:00';
            SET @ArrivalDate=DATEADD(DAY,1,@FlightDate);
        END
        ELSE
        BEGIN
            SET @DepTime='08:30:00'; SET @ArrTime='10:45:00';
            SET @ArrivalDate=@FlightDate;
        END;

        SET @FlightStatus=CASE WHEN @i IN (8,12) THEN 'CANCELLED'
                               WHEN @i<=16 THEN 'COMPLETED' ELSE 'SCHEDULED' END;

        INSERT INTO dbo.FLIGHT
          (departure_airport_id,arrival_airport_id,total_seats,tickets_sold,
           seats_available,total_revenue,departure_date,departure_time,
           arrival_date,arrival_time,status)
        VALUES (@DepAirport,@ArrAirport,90+(@i%3)*30,0,90+(@i%3)*30,0,
                @FlightDate,@DepTime,@ArrivalDate,@ArrTime,@FlightStatus);
        SET @FlightId=CONVERT(INT,SCOPE_IDENTITY());

        INSERT INTO dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg)
        VALUES (@FlightId,N'Economy',900000+(@i%6)*100000,7);
        SET @FareEconomy=CONVERT(INT,SCOPE_IDENTITY());

        INSERT INTO dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg)
        VALUES (@FlightId,N'Business',1800000+(@i%5)*200000,20);
        SET @FareBusiness=CONVERT(INT,SCOPE_IDENTITY());

        /* Four bookings per flight; includes WEB/COUNTER, 1-3 passengers.
           j=4 is cancelled; j=3 is confirmed but unpaid;
           flights 8 and 12 are fully cancelled. */
        SET @j=1;
        WHILE @j<=4
        BEGIN
            SET @Qty=CASE WHEN @j=2 THEN 2 WHEN @j=3 THEN 3 ELSE 1 END;
            SET @BookingSource=CASE WHEN @j IN (1,3) THEN 'WEB' ELSE 'COUNTER' END;
            SET @BookingStatus=CASE WHEN @j=4 OR @FlightStatus='CANCELLED'
                                    THEN 'CANCELLED' ELSE 'CONFIRMED' END;
            SET @TicketStatus=CASE WHEN @BookingStatus='CANCELLED' THEN 'CANCELLED' ELSE 'ISSUED' END;
            SET @InvoiceStatus=CASE WHEN @BookingStatus='CANCELLED' THEN 'CANCELLED'
                                    WHEN @j=3 THEN 'UNPAID' ELSE 'PAID' END;
            SET @BookingDate=DATEADD(HOUR,9,DATEADD(DAY,-7,CAST(@FlightDate AS DATETIME2(0))));
            IF @BookingDate>SYSDATETIME() SET @BookingDate=DATEADD(HOUR,-@j,SYSDATETIME());
            SET @InvoiceDate=@BookingDate;

            SELECT @CustomerId=customer_id FROM @Customers WHERE rn=((@i+@j-2)%24)+1;
            SET @EmployeeId=NULL;
            IF @BookingSource='COUNTER'
                SELECT @EmployeeId=employee_id FROM @Employees WHERE rn=((@i+@j-2)%5)+1;

            SET @DiscountId=NULL;
            IF @j=2 AND @BookingStatus='CONFIRMED'
                SELECT @DiscountId=discount_id FROM dbo.DISCOUNT_CODE WHERE code=CASE WHEN @i=1 THEN 'FULLUSED' ELSE 'SAVE100' END;
            IF @j=3 AND @BookingStatus='CONFIRMED'
                SELECT @DiscountId=discount_id FROM dbo.DISCOUNT_CODE WHERE code='EXPAND5';

            SET @Subtotal=0;
            SET @Price=CASE WHEN @j=3 THEN 1800000+(@i%5)*200000
                             ELSE 900000+(@i%6)*100000 END;
            SET @Subtotal=@Price*@Qty;
            SET @DiscountAmount=CASE WHEN @j=2 AND @BookingStatus='CONFIRMED' THEN CASE WHEN @i=1 THEN 30000.01 ELSE 100000 END
                                      WHEN @j=3 AND @BookingStatus='CONFIRMED' THEN ROUND(@Subtotal*0.05,2)
                                      ELSE 0 END;
            SET @Total=@Subtotal-@DiscountAmount;

            INSERT INTO dbo.BOOKING
              (customer_id,employee_id,flight_id,discount_id,booking_date,
               booking_source,ticket_quantity,discount_amount,total_amount,status)
            VALUES (@CustomerId,@EmployeeId,@FlightId,@DiscountId,@BookingDate,
                    @BookingSource,@Qty,@DiscountAmount,@Total,@BookingStatus);
            SET @BookingId=CONVERT(INT,SCOPE_IDENTITY());

            INSERT INTO dbo.INVOICE
              (booking_id,invoice_date,total,discount_amount,tax_amount,total_amount,status)
            VALUES (@BookingId,@InvoiceDate,@Subtotal,@DiscountAmount,0,@Total,@InvoiceStatus);
            SET @InvoiceId=CONVERT(INT,SCOPE_IDENTITY());

            SET @p=1;
            WHILE @p<=@Qty
            BEGIN
                /* Seat number is unique within each flight, including cancelled history. */
                SET @SeatNo=CAST(((@j-1)*3)+@p AS VARCHAR(4))+'A';
                INSERT INTO dbo.TICKET(flight_id,fare_id,seat_number,issued_at,status)
                VALUES (@FlightId,CASE WHEN @j=3 THEN @FareBusiness ELSE @FareEconomy END,
                        @SeatNo,@BookingDate,@TicketStatus);
                SET @TicketId=CONVERT(INT,SCOPE_IDENTITY());

                INSERT INTO dbo.BOOKING_DETAIL
                    (booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
                VALUES (@BookingId,@TicketId,
                        N'Hanh khach gia lap '+CAST(@i AS NVARCHAR(3))+N'-'+CAST(@j AS NVARCHAR(3))+N'-'+CAST(@p AS NVARCHAR(3)),
                        'DEMO-'+CAST(@FlightId AS VARCHAR(6))+'-'+CAST(@j AS VARCHAR(3))+'-'+CAST(@p AS VARCHAR(3)),@Price);

                INSERT INTO dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal)
                VALUES (@InvoiceId,@TicketId,@Price,1,@Price);
                SET @p+=1;
            END;

            IF @BookingStatus='CONFIRMED'
            BEGIN
                UPDATE dbo.FLIGHT
                   SET tickets_sold=tickets_sold+@Qty,
                       seats_available=seats_available-@Qty
                 WHERE flight_id=@FlightId AND seats_available>=@Qty;
                IF @@ROWCOUNT<>1 THROW 52005, N'Khong du ghe de ban khi seed.',1;
                SET @IssuedTickets+=@Qty;

                IF @DiscountId IS NOT NULL
                BEGIN
                    UPDATE dbo.DISCOUNT_CODE SET used_count=used_count+1
                    WHERE discount_id=@DiscountId
                      AND (usage_limit IS NULL OR used_count<usage_limit);
                    IF @@ROWCOUNT<>1 THROW 52006, N'Ma giam gia da het luot khi seed.',1;
                END;
            END;

            IF @InvoiceStatus='PAID'
            BEGIN
                UPDATE dbo.FLIGHT SET total_revenue=total_revenue+@Total
                WHERE flight_id=@FlightId;
                SET @PaidRevenue+=@Total;
            END;
            SET @j+=1;
        END;
        SET @i+=1;
    END;

    /* Assert consistency before COMMIT. */
    IF EXISTS (
        SELECT 1 FROM dbo.FLIGHT f
        WHERE f.tickets_sold <> (
            SELECT COUNT(*) FROM dbo.TICKET t
            WHERE t.flight_id=f.flight_id AND t.status='ISSUED'
        )
    ) THROW 52007, N'So ve ban khong khop voi TICKET.',1;

    IF EXISTS (
        SELECT 1 FROM dbo.FLIGHT f
        WHERE f.total_revenue <> ISNULL((
            SELECT SUM(inv.total_amount)
            FROM dbo.INVOICE inv
            JOIN dbo.BOOKING b ON b.booking_id=inv.booking_id
            WHERE b.flight_id=f.flight_id AND inv.status='PAID'
        ),0)
    ) THROW 52008, N'Doanh thu FLIGHT khong khop INVOICE PAID.',1;

    IF EXISTS (
        SELECT 1 FROM dbo.BOOKING b
        WHERE b.ticket_quantity <> (SELECT COUNT(*) FROM dbo.BOOKING_DETAIL bd WHERE bd.booking_id=b.booking_id)
    ) THROW 52009, N'So hanh khach khong khop BOOKING.ticket_quantity.',1;

    COMMIT TRANSACTION;


END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;


GO

-- Store hashes only in SQL. Private synthetic demo lookup keys are returned only
-- after COMMIT; direct sqlcmd output to an ignored LOCAL file, never Git/Docs.
DECLARE @LookupKeys TABLE(booking_id int PRIMARY KEY,lookup_key varchar(64) NOT NULL);
INSERT @LookupKeys SELECT booking_id,CONVERT(varchar(64),CRYPT_GEN_RANDOM(32),2)
FROM dbo.BOOKING WHERE booking_source='WEB';
INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id)
SELECT CONVERT(char(64),HASHBYTES('SHA2_256',lookup_key),2),
 CONVERT(char(64),HASHBYTES('SHA2_256',CONCAT('FINAL-DEMO-PAYLOAD:',booking_id)),2),booking_id FROM @LookupKeys;
IF (SELECT COUNT_BIG(*) FROM dbo.[AIRPORT])<>8
 OR (SELECT COUNT_BIG(*) FROM dbo.[CUSTOMER])<>25
 OR (SELECT COUNT_BIG(*) FROM dbo.[EMPLOYEE])<>7
 OR (SELECT COUNT_BIG(*) FROM dbo.[DISCOUNT_CODE])<>6
 OR (SELECT COUNT_BIG(*) FROM dbo.[FLIGHT])<>43
 OR (SELECT COUNT_BIG(*) FROM dbo.[FARE_CLASS])<>85
 OR (SELECT COUNT_BIG(*) FROM dbo.[TICKET])<>141
 OR (SELECT COUNT_BIG(*) FROM dbo.[BOOKING])<>81
 OR (SELECT COUNT_BIG(*) FROM dbo.[BOOKING_DETAIL])<>141
 OR (SELECT COUNT_BIG(*) FROM dbo.[INVOICE])<>81
 OR (SELECT COUNT_BIG(*) FROM dbo.[INVOICE_DETAIL])<>141
 OR (SELECT COUNT_BIG(*) FROM dbo.[BOOKING_WEB_REQUEST])<>41
 THROW 51257,'Final seed count mismatch; whole seed remains uncommitted.',1;
EXEC sys.sp_addextendedproperty @name=N'BookingFlight.FinalSeed',@value=N'DEMO_8_25_7_6_43_85_81_141';
:r Database/Final/03_Verify_ReadOnly.sql
IF @@TRANCOUNT<>1 THROW 51258,'Unexpected seed transaction depth. Stop.',1;
COMMIT TRANSACTION;
PRINT 'PASS: Final demo seed committed once; schema/row counts/business consistency verified.';
SELECT k.booking_id,i.status,i.total_amount,k.lookup_key AS PRIVATE_DEMO_LOOKUP_KEY
FROM @LookupKeys k JOIN dbo.INVOICE i ON i.booking_id=k.booking_id ORDER BY k.booking_id;
