:ON ERROR EXIT
-- Targeted correction of 20 owned v1 return-leg fixtures, never a reseed.
-- Exact current counts require review; new orders are preserved. Refuse if
-- a seed return was subsequently paid/cancelled or another order arrives.
USE [FlightBooking_Final];
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 OR N'$(REPAIR_APPROVAL)'<>N'APPROVE_INITIAL_FINAL_RETURN_DISCOUNT_REPAIR'
 THROW 51320,'Wrong server or repair approval missing.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @lock int;
 EXEC @lock=sys.sp_getapplock @Resource=N'BookingFlight.Final.Setup',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0;
 IF @lock<0 THROW 51321,'Another setup is active.',1;
 IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name='BookingFlight.FinalSeed' AND CONVERT(varchar(100),value)='DEMO_15_50_15_15_43_129_81_141')
 OR EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name='BookingFlight.ReturnDiscountRepair')
 OR (SELECT COUNT(*) FROM dbo.BOOKING WITH(UPDLOCK,HOLDLOCK))<>$(EXPECTED_BOOKINGS)
 OR (SELECT COUNT(*) FROM dbo.TICKET WITH(UPDLOCK,HOLDLOCK))<>$(EXPECTED_TICKETS)
 OR (SELECT COUNT(*) FROM dbo.BOOKING_WEB_REQUEST WITH(UPDLOCK,HOLDLOCK))<>$(EXPECTED_REQUESTS)
 THROW 51322,'Database changed since reviewed snapshot. Stop and inspect.',1;
 DECLARE @Returns TABLE(booking_id int PRIMARY KEY);
 INSERT @Returns
 SELECT b.booking_id FROM dbo.BOOKING_WEB_REQUEST w JOIN dbo.BOOKING b ON b.booking_id=w.booking_id
 JOIN dbo.FLIGHT f ON f.flight_id=b.flight_id
 WHERE EXISTS(SELECT 1 FROM dbo.BOOKING_WEB_REQUEST p JOIN dbo.BOOKING pb ON pb.booking_id=p.booking_id
 JOIN dbo.FLIGHT pf ON pf.flight_id=pb.flight_id
 WHERE p.payload_hash=w.payload_hash AND pf.departure_date<f.departure_date
 AND pf.departure_airport_id=f.arrival_airport_id AND pf.arrival_airport_id=f.departure_airport_id)
 AND b.booking_source='WEB' AND b.discount_amount=50000
 AND EXISTS(SELECT 1 FROM dbo.CUSTOMER c WHERE c.customer_id=b.customer_id AND c.email LIKE 'final.customer.%@example.test');
 IF (SELECT COUNT(*) FROM @Returns)<>20 THROW 51323,'Expected exactly 20 initial return legs.',1;
 IF (SELECT COUNT(*) FROM dbo.INVOICE i JOIN @Returns r ON r.booking_id=i.booking_id WHERE i.status='PAID')<>10
 OR (SELECT COUNT(*) FROM dbo.INVOICE i JOIN @Returns r ON r.booking_id=i.booking_id WHERE i.status='UNPAID')<>6
 OR (SELECT COUNT(*) FROM dbo.INVOICE i JOIN @Returns r ON r.booking_id=i.booking_id WHERE i.status='CANCELLED')<>4
 OR EXISTS(SELECT 1 FROM dbo.INVOICE i JOIN @Returns r ON r.booking_id=i.booking_id
   JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE i.discount_amount<>50000 OR i.total_amount<>b.total_amount
   OR i.tax_amount<>0 OR i.total_amount<>i.total-50000)
 THROW 51324,'Seed financial history changed. Do not auto-correct after real payment.',1;
 DECLARE @FlightIds TABLE(id int PRIMARY KEY);
 INSERT @FlightIds SELECT DISTINCT b.flight_id FROM dbo.BOOKING b JOIN @Returns r ON r.booking_id=b.booking_id;
 DECLARE @DiscountIds TABLE(id int PRIMARY KEY);
 INSERT @DiscountIds SELECT DISTINCT b.discount_id FROM dbo.BOOKING b JOIN @Returns r ON r.booking_id=b.booking_id;

 UPDATE b SET discount_id=NULL,discount_amount=0,total_amount=b.total_amount+50000 FROM dbo.BOOKING b JOIN @Returns r ON r.booking_id=b.booking_id;
 UPDATE i SET discount_amount=0,total_amount=total+tax_amount FROM dbo.INVOICE i JOIN @Returns r ON r.booking_id=i.booking_id;
 UPDATE d SET used_count=(SELECT COUNT(*) FROM dbo.BOOKING b WHERE b.discount_id=d.discount_id AND b.status='CONFIRMED') FROM dbo.DISCOUNT_CODE d WHERE d.discount_id IN(SELECT id FROM @DiscountIds);
 UPDATE f SET total_revenue=COALESCE((SELECT SUM(i.total_amount) FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE b.flight_id=f.flight_id AND i.status='PAID'),0) FROM dbo.FLIGHT f WHERE f.flight_id IN(SELECT id FROM @FlightIds);
 EXEC sys.sp_addextendedproperty @name=N'BookingFlight.ReturnDiscountRepair',@value=N'v1';
 COMMIT TRANSACTION;
 PRINT 'PASS: initial return-leg discount corrected; lookup/request hashes unchanged.';
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
