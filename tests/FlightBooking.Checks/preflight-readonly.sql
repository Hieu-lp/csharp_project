/* Chỉ đọc: không đổi database đang kết nối, không tạo schema hoặc fixture. */
SET NOCOUNT ON;

SELECT DB_NAME() AS database_name,
       CASE WHEN DB_NAME() LIKE '%[_]ManagementTests' OR DB_NAME() LIKE '%[_]BookingTests'
            THEN 1 ELSE 0 END AS has_test_database_suffix,
       CONVERT(nvarchar(20), DATABASEPROPERTYEX(DB_NAME(), 'Updateability')) AS updateability,
       HAS_PERMS_BY_NAME(DB_NAME(), 'DATABASE', 'VIEW DEFINITION') AS can_view_schema;

SELECT required.table_name,
       CASE WHEN OBJECT_ID(N'dbo.' + required.table_name, 'U') IS NULL THEN 0 ELSE 1 END AS table_exists
FROM (VALUES (N'AIRPORT'), (N'CUSTOMER'), (N'EMPLOYEE'), (N'DISCOUNT_CODE'),
             (N'FLIGHT'), (N'FARE_CLASS'), (N'TICKET'), (N'BOOKING'),
             (N'BOOKING_DETAIL'), (N'INVOICE'), (N'INVOICE_DETAIL'),
             (N'BOOKING_WEB_REQUEST')) required(table_name);

SELECT required.column_name,
       CASE WHEN EXISTS (SELECT 1 FROM sys.columns c
                         WHERE c.object_id = OBJECT_ID('dbo.BOOKING_WEB_REQUEST')
                           AND c.name = required.column_name) THEN 1 ELSE 0 END AS column_exists
FROM (VALUES (N'request_hash'), (N'payload_hash'), (N'booking_id')) required(column_name);

SELECT name, is_disabled, is_not_trusted
FROM sys.foreign_keys
WHERE parent_object_id IN (OBJECT_ID('dbo.FLIGHT'), OBJECT_ID('dbo.FARE_CLASS'),
                           OBJECT_ID('dbo.TICKET'), OBJECT_ID('dbo.BOOKING'),
                           OBJECT_ID('dbo.BOOKING_DETAIL'), OBJECT_ID('dbo.BOOKING_WEB_REQUEST'));

SELECT name, is_unique, has_filter, filter_definition, is_disabled
FROM sys.indexes
WHERE object_id = OBJECT_ID('dbo.TICKET') AND name = 'UX_TICKET_ActiveSeat';

IF OBJECT_ID('dbo.FLIGHT', 'U') IS NOT NULL AND OBJECT_ID('dbo.TICKET', 'U') IS NOT NULL
    SELECT 'flight_counters' AS check_name, COUNT(*) AS issue_count
    FROM dbo.FLIGHT f
    OUTER APPLY (SELECT COUNT(*) AS active_count FROM dbo.TICKET t
                 WHERE t.flight_id = f.flight_id AND t.status = 'ISSUED') seats
    WHERE f.tickets_sold <> seats.active_count OR f.total_seats <> f.tickets_sold + f.seats_available;

IF OBJECT_ID('dbo.BOOKING', 'U') IS NOT NULL AND OBJECT_ID('dbo.BOOKING_DETAIL', 'U') IS NOT NULL
    SELECT 'booking_detail_quantity' AS check_name, COUNT(*) AS issue_count
    FROM dbo.BOOKING b
    OUTER APPLY (SELECT COUNT(*) AS detail_count FROM dbo.BOOKING_DETAIL d
                 WHERE d.booking_id = b.booking_id) details
    WHERE b.ticket_quantity <> details.detail_count;

IF OBJECT_ID('dbo.BOOKING', 'U') IS NOT NULL AND OBJECT_ID('dbo.BOOKING_DETAIL', 'U') IS NOT NULL
   AND OBJECT_ID('dbo.TICKET', 'U') IS NOT NULL
    SELECT 'booking_ticket_flight' AS check_name, COUNT(*) AS issue_count
    FROM dbo.BOOKING_DETAIL d JOIN dbo.BOOKING b ON b.booking_id = d.booking_id
    JOIN dbo.TICKET t ON t.ticket_id = d.ticket_id
    WHERE b.flight_id <> t.flight_id;

/* Kết quả chỉ là preflight. Không thay thế quyền dùng DB test hoặc kiểm thử ghi/rollback. */
