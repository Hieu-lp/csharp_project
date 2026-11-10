/* Safe read-only checks. Run after 01-04. */
USE FlightBookingDB;
GO
SET NOCOUNT ON;

DECLARE @Tables TABLE (name SYSNAME PRIMARY KEY);
INSERT INTO @Tables(name) VALUES
 (N'AIRPORT'),(N'FLIGHT'),(N'FARE_CLASS'),(N'TICKET'),(N'CUSTOMER'),
 (N'EMPLOYEE'),(N'BOOKING'),(N'BOOKING_DETAIL'),(N'DISCOUNT_CODE'),
 (N'INVOICE'),(N'INVOICE_DETAIL');

SELECT t.name AS RequiredTable,
       CASE WHEN s.object_id IS NULL THEN N'MISSING' ELSE N'OK' END AS Result
FROM @Tables t
LEFT JOIN sys.tables s ON s.name = t.name AND s.schema_id = SCHEMA_ID(N'dbo')
ORDER BY t.name;

IF EXISTS (SELECT 1 FROM @Tables t
           WHERE OBJECT_ID(QUOTENAME(N'dbo') + N'.' + QUOTENAME(t.name), N'U') IS NULL)
    THROW 52001, N'Thieu mot hoac nhieu bang.', 1;

IF NOT EXISTS (SELECT 1 FROM sys.indexes
   WHERE object_id = OBJECT_ID(N'dbo.TICKET')
     AND name = N'UX_TICKET_ActiveSeat'
     AND is_unique = 1 AND has_filter = 1)
    THROW 52002, N'Thieu filtered unique index ghe dang hieu luc.', 1;

/* Business-level consistency checks for seeded/production data */
IF EXISTS (SELECT 1 FROM dbo.FLIGHT f
  OUTER APPLY (SELECT COUNT_BIG(*) AS count_issued
       FROM dbo.TICKET t
       WHERE t.flight_id = f.flight_id AND t.status = 'ISSUED') x
  WHERE f.tickets_sold <> x.count_issued)
    THROW 52003, N'FLIGHT.tickets_sold khong khop so ve ISSUED.', 1;

IF EXISTS (SELECT 1 FROM dbo.FLIGHT f
  OUTER APPLY (SELECT COALESCE(SUM(i.total_amount),0) AS paid_revenue
       FROM dbo.BOOKING b INNER JOIN dbo.INVOICE i ON i.booking_id = b.booking_id
       WHERE b.flight_id = f.flight_id AND i.status = 'PAID') x
  WHERE f.total_revenue <> x.paid_revenue)
    THROW 52004, N'FLIGHT.total_revenue khong khop hoa don PAID.', 1;

IF EXISTS (SELECT 1 FROM dbo.BOOKING b
  OUTER APPLY (SELECT COUNT_BIG(*) AS detail_count
       FROM dbo.BOOKING_DETAIL d WHERE d.booking_id = b.booking_id) x
  WHERE b.status = 'CONFIRMED' AND b.ticket_quantity <> x.detail_count)
    THROW 52005, N'BOOKING.ticket_quantity khong khop chi tiet.', 1;

IF EXISTS (SELECT 1 FROM dbo.BOOKING_DETAIL d
  INNER JOIN dbo.BOOKING b ON b.booking_id = d.booking_id
  INNER JOIN dbo.TICKET t ON t.ticket_id = d.ticket_id
  WHERE b.flight_id <> t.flight_id)
    THROW 52006, N'Ve trong BOOKING_DETAIL khac chuyen bay cua BOOKING.', 1;

IF EXISTS (SELECT 1 FROM dbo.INVOICE i
  OUTER APPLY (SELECT COALESCE(SUM(d.subtotal),0) AS items_total
       FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id = i.invoice_id) x
  WHERE i.total <> x.items_total)
    THROW 52007, N'INVOICE.total khong khop tong chi tiet.', 1;

IF EXISTS (SELECT 1 FROM dbo.INVOICE i
  INNER JOIN dbo.BOOKING b ON b.booking_id = i.booking_id
  WHERE b.total_amount <> i.total_amount)
    THROW 52008, N'BOOKING.total_amount khong khop INVOICE.total_amount.', 1;

IF EXISTS (SELECT 1 FROM dbo.INVOICE_DETAIL idt
  INNER JOIN dbo.INVOICE i ON i.invoice_id = idt.invoice_id
  INNER JOIN dbo.BOOKING_DETAIL bd ON bd.ticket_id = idt.ticket_id
  WHERE bd.booking_id <> i.booking_id)
    THROW 52009, N'INVOICE_DETAIL chua ve khong thuoc BOOKING cua hoa don.', 1;

SELECT f.flight_id, f.total_seats, f.tickets_sold, f.seats_available,
       f.total_revenue, f.departure_date, f.departure_time,
       f.arrival_date, f.arrival_time
FROM dbo.FLIGHT f;

SELECT b.booking_id, b.booking_source, b.employee_id, b.ticket_quantity,
       b.total_amount, i.invoice_id, i.status AS invoice_status
FROM dbo.BOOKING b LEFT JOIN dbo.INVOICE i ON i.booking_id=b.booking_id;

SELECT N'PASS: 11 bang va cac kiem tra du lieu deu hop le' AS TestResult;
GO
