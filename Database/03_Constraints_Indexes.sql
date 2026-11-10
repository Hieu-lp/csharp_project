/* Extra indexes, after 02_CreateTables.sql */
USE FlightBookingDB;
GO

/* One active ticket per seat per flight; keep cancelled ticket history. */
CREATE UNIQUE NONCLUSTERED INDEX UX_TICKET_ActiveSeat
ON dbo.TICKET(flight_id, seat_number)
WHERE status = 'ISSUED';
GO

CREATE INDEX IX_FLIGHT_RouteDate
ON dbo.FLIGHT(departure_airport_id, arrival_airport_id, departure_date);
GO
CREATE INDEX IX_BOOKING_FlightDate
ON dbo.BOOKING(flight_id, booking_date);
GO
CREATE INDEX IX_BOOKING_DETAIL_Booking
ON dbo.BOOKING_DETAIL(booking_id);
GO
CREATE INDEX IX_INVOICE_DETAIL_Invoice
ON dbo.INVOICE_DETAIL(invoice_id);
GO
CREATE INDEX IX_INVOICE_StatusDate
ON dbo.INVOICE(status, invoice_date);
GO

SELECT name, is_unique, has_filter, filter_definition
FROM sys.indexes
WHERE object_id = OBJECT_ID(N'dbo.TICKET') AND name = N'UX_TICKET_ActiveSeat';
GO
