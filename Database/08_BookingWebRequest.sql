/* PROPOSED extension: review with Hieu before applying to the shared database.
   Durable idempotency and private booking access cannot be represented in the
   original 11 tables without overloading business columns. No existing data is
   changed. Apply once after 01..03; safe to rerun. Store hashes, never raw keys. */
USE FlightBookingDB;
GO
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
