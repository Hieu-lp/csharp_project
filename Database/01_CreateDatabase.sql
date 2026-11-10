/* FlightBookingDB - SQL Server local development
   Chay tren SSMS voi tai khoan co quyen CREATE DATABASE.
   Script khong xoa Database da ton tai. */
USE master;
GO
IF DB_ID(N'FlightBookingDB') IS NULL
BEGIN
    EXEC(N'CREATE DATABASE [FlightBookingDB]');
END;
GO
USE FlightBookingDB;
GO
SELECT DB_NAME() AS CurrentDatabase;
GO
