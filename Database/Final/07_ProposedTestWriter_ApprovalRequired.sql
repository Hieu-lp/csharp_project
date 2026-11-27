:ON ERROR EXIT
-- Review before execution. Existing separate Windows identity only.
-- No OS account, SQL login, server role or access to the demo DB is created.
USE [FlightBooking_Final_Tests];
GO
SET XACT_ABORT ON;
IF DB_NAME()<>N'FlightBooking_Final_Tests'
 OR CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 OR N'$(SECURITY_APPROVAL)'<>N'APPROVE_FINAL_TESTS_WINDOWS_WRITER'
 THROW 51340,'Wrong target or separate writer approval missing.',1;
DECLARE @Login sysname=N'$(WRITER_LOGIN)';
IF NOT EXISTS(SELECT 1 FROM sys.server_principals WHERE name=@Login AND type='U' AND is_disabled=0)
 OR IS_SRVROLEMEMBER('sysadmin',@Login)=1
 THROW 51341,'An existing separate non-sysadmin Windows login is required.',1;
IF DATABASE_PRINCIPAL_ID(@Login) IS NOT NULL OR DATABASE_PRINCIPAL_ID('FlightBookingFinalTestsWriter') IS NOT NULL
 THROW 51342,'Principal already exists; review effective rights instead of rerunning.',1;
IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>12
 THROW 51343,'Review schema/grant list after approved extensions.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 DECLARE @Sql nvarchar(max)=N'CREATE USER '+QUOTENAME(@Login)+N' FOR LOGIN '+QUOTENAME(@Login)+N';';
 EXEC sys.sp_executesql @Sql;
 CREATE ROLE FlightBookingFinalTestsWriter AUTHORIZATION dbo;
 SET @Sql=N'ALTER ROLE FlightBookingFinalTestsWriter ADD MEMBER '+QUOTENAME(@Login)+N';';
 EXEC sys.sp_executesql @Sql;
 GRANT CONNECT,VIEW DEFINITION TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.AIRPORT TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.CUSTOMER TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.EMPLOYEE TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.DISCOUNT_CODE TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.FLIGHT TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.FARE_CLASS TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.BOOKING TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.BOOKING_DETAIL TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.TICKET TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.INVOICE TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.INVOICE_DETAIL TO FlightBookingFinalTestsWriter;
 GRANT SELECT,INSERT,UPDATE,DELETE ON dbo.BOOKING_WEB_REQUEST TO FlightBookingFinalTestsWriter;
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
-- Verify using the real Windows writer session: no sysadmin/db_owner/DDL or
-- inherited access to other databases. Never replace this with EXECUTE AS.
-- Existing TestPlan whitelist remains unchanged; a new test target needs its
-- own guarded runner/fixtures and approval before any business write check.
