/* Demo data for the new 11-table schema. Fictional people/identifiers.
   One confirmed WEB booking, one paid invoice, one active ticket.
   Run ONCE after steps 01-03 on a fresh DB. No real passenger data. */
USE FlightBookingDB;
GO
SET XACT_ABORT ON;
GO
BEGIN TRY
    BEGIN TRANSACTION;

    IF EXISTS (SELECT 1 FROM dbo.AIRPORT)
       OR EXISTS (SELECT 1 FROM dbo.FLIGHT)
       OR EXISTS (SELECT 1 FROM dbo.BOOKING)
    BEGIN
        THROW 51001, N'Da co du lieu. Chi chay seed nay tren Database moi, khong chay lai de tranh trung.', 1;
    END;

    DECLARE @AirportSGN INT, @AirportHAN INT, @CustomerId INT, @EmployeeId INT;
    DECLARE @DiscountId INT, @FlightId INT, @FareId INT, @TicketId INT;
    DECLARE @BookingId INT, @InvoiceId INT;

    INSERT INTO dbo.AIRPORT(airport_name, city)
    VALUES (N'Sân bay Tân Sơn Nhất', N'TP. Hồ Chí Minh');
    SET @AirportSGN = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.AIRPORT(airport_name, city)
    VALUES (N'Sân bay Nội Bài', N'Hà Nội');
    SET @AirportHAN = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.CUSTOMER(full_name, phone, email, date_of_birth, gender, id_passport_number)
    VALUES (N'Khách hàng Demo', '0900000001', 'customer.demo@example.test', '2000-05-10', N'Khác', NULL);
    SET @CustomerId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.EMPLOYEE(full_name, phone, email, position, hire_date, status)
    VALUES (N'Nhân viên Demo', '0900000002', 'staff.demo@example.test', N'Nhân viên bán vé', '2026-01-01', 'ACTIVE');
    SET @EmployeeId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,
       min_order_amount,max_discount_amount,start_date,end_date,usage_limit,used_count,status)
    VALUES ('DEMO10',N'Giảm 10% cho đơn đặt vé thử nghiệm','PERCENT',10.00,
       0,200000.00,'2026-01-01T00:00:00','2027-12-31T23:59:59',100,0,'ACTIVE');
    SET @DiscountId = CONVERT(INT, SCOPE_IDENTITY());

    INSERT INTO dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,
       tickets_sold,seats_available,total_revenue,
       departure_date,departure_time,arrival_date,arrival_time,status)
    VALUES (@AirportSGN,@AirportHAN,100,0,100,0,
       '2026-12-15','08:30:00','2026-12-15','10:45:00','SCHEDULED');
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

    COMMIT TRANSACTION;
    PRINT N'Seed thanh cong: 11 bang da co du lieu demo.';
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
GO
