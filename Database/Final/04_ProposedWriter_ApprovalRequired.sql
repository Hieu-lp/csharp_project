:ON ERROR EXIT
-- PROPOSAL ONLY. Do not execute before separate approval of Final DB grants.
-- Existing Windows login required. No login/account/server grants are created.
USE [FlightBooking_Final];
GO
SET XACT_ABORT ON;
IF DB_NAME()<>N'FlightBooking_Final'
 OR CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 THROW 51240,'Wrong server/database.',1;
IF N'$(SECURITY_APPROVAL)'<>N'APPROVE_FINAL_TEST_WRITER_SECURITY'
 THROW 51241,'Separate approval of Final writer grants is required.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalDemo' AND CONVERT(nvarchar(128),value)=N'Final-20261127-v1')
 THROW 51242,'Final marker not verified.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>12
 THROW 51243,'Review explicit table grant list after schema changes.',1;
DECLARE @Login sysname=N'$(WRITER_LOGIN)';
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@Login AND type='U' AND is_disabled=0)
 THROW 51244,'Expected enabled Windows login is missing. Do not provision another identity here.',1;
IF DATABASE_PRINCIPAL_ID(@Login) IS NOT NULL OR DATABASE_PRINCIPAL_ID(N'FlightBookingFinalWriter') IS NOT NULL
 THROW 51245,'User/role already exists. Inspect effective rights; do not rerun blindly.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Sql nvarchar(max)=N'CREATE USER '+QUOTENAME(@Login)+N' FOR LOGIN '+QUOTENAME(@Login)+N';';
 EXEC sys.sp_executesql @Sql;
 CREATE ROLE [FlightBookingFinalWriter] AUTHORIZATION dbo;
 SET @Sql=N'ALTER ROLE [FlightBookingFinalWriter] ADD MEMBER '+QUOTENAME(@Login)+N';';
 EXEC sys.sp_executesql @Sql;
 GRANT CONNECT,VIEW DEFINITION TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.AIRPORT TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.FLIGHT TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.FARE_CLASS TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.CUSTOMER TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.EMPLOYEE TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.DISCOUNT_CODE TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.BOOKING TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.BOOKING_DETAIL TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.TICKET TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.INVOICE TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.INVOICE_DETAIL TO [FlightBookingFinalWriter];
 GRANT SELECT,INSERT,UPDATE,DELETE ON OBJECT::dbo.BOOKING_WEB_REQUEST TO [FlightBookingFinalWriter];
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
-- Grants alone are not acceptance: verify effective permissions through Windows
-- groups/tokens, server/database roles and access to both older databases using
-- the real writer account. Never substitute EXECUTE AS or a sysadmin writer.
-- Existing TestPlan runners remain restricted to their original test database.
