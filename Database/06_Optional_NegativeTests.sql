/* Optional: negative tests. Each is rolled back; no committed data changes.
   Use only in DEVELOPMENT, not on production. */
USE FlightBookingDB;
GO
SET NOCOUNT ON;

/* Expected: duplicate discount code rejected by UNIQUE. */
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT INTO dbo.DISCOUNT_CODE
      (code, description, discount_type, discount_value,
       min_order_amount, start_date, end_date, used_count, status)
    VALUES ('DEMO10', N'Trung ma demo', 'PERCENT', 10, 0,
       '2026-01-01', '2027-01-01', 0, 'ACTIVE');
    ROLLBACK TRANSACTION;
    THROW 53001, N'FAIL: Duplicate discount code unexpectedly accepted.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() IN (2601,2627)
        PRINT N'PASS: UNIQUE code ngan ma giam gia trung';
    ELSE THROW;
END CATCH;
GO

/* Expected: cannot have sold + available != total seats. */
BEGIN TRY
    BEGIN TRANSACTION;
    UPDATE dbo.FLIGHT SET tickets_sold = total_seats + 1
    WHERE flight_id = (SELECT TOP (1) flight_id FROM dbo.FLIGHT ORDER BY flight_id);
    ROLLBACK TRANSACTION;
    THROW 53002, N'FAIL: Invalid ticket counter unexpectedly accepted.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() = 547
        PRINT N'PASS: CHECK FLIGHT ngan so ve vuot so ghe';
    ELSE THROW;
END CATCH;
GO

/* Expected: one active seat cannot have two tickets. */
BEGIN TRY
    BEGIN TRANSACTION;
    INSERT INTO dbo.TICKET(flight_id,fare_id,seat_number,status)
    SELECT TOP (1) flight_id, fare_id, seat_number, 'ISSUED'
    FROM dbo.TICKET WHERE status = 'ISSUED' ORDER BY ticket_id;
    ROLLBACK TRANSACTION;
    THROW 53003, N'FAIL: Duplicate active seat unexpectedly accepted.', 1;
END TRY
BEGIN CATCH
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
    IF ERROR_NUMBER() IN (2601,2627)
        PRINT N'PASS: UNIQUE filtered index ngan trung ghe';
    ELSE THROW;
END CATCH;
GO
