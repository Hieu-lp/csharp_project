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
USE FlightBookingDB;
GO
SET NOCOUNT ON;
SET XACT_ABORT ON;
GO
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

    /* Extra airports: existing SGN/HAN remain in place. */
    INSERT INTO dbo.AIRPORT(airport_name,city) VALUES
        (N'San bay Da Nang',N'Da Nang'),
        (N'San bay Cam Ranh',N'Khanh Hoa'),
        (N'San bay Phu Quoc',N'Kien Giang'),
        (N'San bay Cat Bi',N'Hai Phong'),
        (N'San bay Phu Bai',N'Hue'),
        (N'San bay Lien Khuong',N'Lam Dong');

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
       ('EXPAND5',N'Demo: giam 5 phan tram','PERCENT',5,0,200000,'2026-01-01','2028-12-31',100,0,'ACTIVE'),
       ('SAVE100',N'Demo: giam 100 nghin','FIXED',100000,500000,NULL,'2026-01-01','2028-12-31',100,0,'ACTIVE'),
       ('EXPIRED10',N'Demo: ma het han','PERCENT',10,0,200000,'2025-01-01','2025-12-31',30,0,'ACTIVE'),
       ('FULLUSED',N'Demo: ma het luot','FIXED',30000,0,NULL,'2026-01-01','2028-12-31',1,1,'ACTIVE'),
       ('FUTURE15',N'Demo: ma chua den ngay','PERCENT',15,0,150000,'2028-01-01','2029-12-31',100,0,'ACTIVE');

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
            SET @FlightDate=DATEFROMPARTS(2026,1+(((@i-1)%4)*3),11+((@i-1)/4)*5);
        ELSE
            SET @FlightDate=DATEADD(DAY,(@i-17)*10,CONVERT(DATE,'20261120',112));

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
            SET @InvoiceDate=DATEADD(HOUR,1,@BookingDate);

            SELECT @CustomerId=customer_id FROM @Customers WHERE rn=((@i+@j-2)%24)+1;
            SET @EmployeeId=NULL;
            IF @BookingSource='COUNTER'
                SELECT @EmployeeId=employee_id FROM @Employees WHERE rn=((@i+@j-2)%5)+1;

            SET @DiscountId=NULL;
            IF @j=2 AND @BookingStatus='CONFIRMED'
                SELECT @DiscountId=discount_id FROM dbo.DISCOUNT_CODE WHERE code='SAVE100';
            IF @j=3 AND @BookingStatus='CONFIRMED'
                SELECT @DiscountId=discount_id FROM dbo.DISCOUNT_CODE WHERE code='EXPAND5';

            SET @Subtotal=0;
            SET @Price=CASE WHEN @j=3 THEN 1800000+(@i%5)*200000
                             ELSE 900000+(@i%6)*100000 END;
            SET @Subtotal=@Price*@Qty;
            SET @DiscountAmount=CASE WHEN @j=2 AND @BookingStatus='CONFIRMED' THEN 100000
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
    PRINT N'Thanh cong: them 6 san bay, 24 khach, 6 nhan vien, 5 ma giam gia, 20 chuyen bay, 40 hang ve, 80 booking va 140 ve.';
    PRINT N'Da kiem tra so ve, doanh thu va so hanh khach tren toan database.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO

/* Counts by table after extended seed. */
SELECT 'AIRPORT' AS TableName,COUNT(*) AS SoLuong FROM dbo.AIRPORT
UNION ALL SELECT 'CUSTOMER',COUNT(*) FROM dbo.CUSTOMER
UNION ALL SELECT 'EMPLOYEE',COUNT(*) FROM dbo.EMPLOYEE
UNION ALL SELECT 'DISCOUNT_CODE',COUNT(*) FROM dbo.DISCOUNT_CODE
UNION ALL SELECT 'FLIGHT',COUNT(*) FROM dbo.FLIGHT
UNION ALL SELECT 'FARE_CLASS',COUNT(*) FROM dbo.FARE_CLASS
UNION ALL SELECT 'TICKET',COUNT(*) FROM dbo.TICKET
UNION ALL SELECT 'BOOKING',COUNT(*) FROM dbo.BOOKING
UNION ALL SELECT 'BOOKING_DETAIL',COUNT(*) FROM dbo.BOOKING_DETAIL
UNION ALL SELECT 'INVOICE',COUNT(*) FROM dbo.INVOICE
UNION ALL SELECT 'INVOICE_DETAIL',COUNT(*) FROM dbo.INVOICE_DETAIL;
GO

/* Useful demonstrations: revenue per flight and per quarter, and booking channels. */
SELECT f.flight_id, f.departure_date, f.status,
       f.total_seats, f.tickets_sold, f.seats_available, f.total_revenue
FROM dbo.FLIGHT f ORDER BY f.departure_date, f.flight_id;

SELECT YEAR(i.invoice_date) AS [year], DATEPART(QUARTER,i.invoice_date) AS [quarter],
       COUNT(*) AS paid_invoices, SUM(i.total_amount) AS total_revenue
FROM dbo.INVOICE i WHERE i.status='PAID'
GROUP BY YEAR(i.invoice_date), DATEPART(QUARTER,i.invoice_date)
ORDER BY [year],[quarter];

SELECT booking_source, status, COUNT(*) AS booking_count
FROM dbo.BOOKING GROUP BY booking_source, status
ORDER BY booking_source, status;
GO
