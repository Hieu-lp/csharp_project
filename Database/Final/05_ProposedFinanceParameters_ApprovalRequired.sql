:ON ERROR EXIT
-- PROPOSAL: review before applying. No legacy paid_at dates are fabricated.
USE [FlightBooking_Final];
GO
SET XACT_ABORT ON;
SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 OR DB_NAME()<>N'FlightBooking_Final'
 THROW 51300,'Wrong server/database.',1;
IF N'$(SCHEMA_APPROVAL)'<>N'APPROVE_FINAL_FINANCE_PARAMETERS'
 THROW 51301,'Finance/parameter schema needs explicit approval.',1;
IF OBJECT_ID('dbo.EXPENSE') IS NOT NULL OR OBJECT_ID('dbo.EXPENSE_DETAIL') IS NOT NULL
 OR OBJECT_ID('dbo.SYSTEM_PARAMETER') IS NOT NULL OR COL_LENGTH('dbo.INVOICE','paid_at') IS NOT NULL
 THROW 51302,'Extension already present or partly applied. Inspect; do not overwrite.',1;
BEGIN TRY
 BEGIN TRANSACTION;
 CREATE TABLE dbo.EXPENSE(
  expense_id int IDENTITY PRIMARY KEY,
  document_number varchar(50) NOT NULL UNIQUE,
  expense_date date NOT NULL,
  employee_id int NOT NULL REFERENCES dbo.EMPLOYEE(employee_id),
  description nvarchar(255) NOT NULL,
  status varchar(20) NOT NULL CONSTRAINT DF_EXPENSE_Status DEFAULT 'DRAFT'
    CONSTRAINT CK_EXPENSE_Status CHECK(status IN('DRAFT','POSTED','CANCELLED')),
  total_amount decimal(18,2) NOT NULL CONSTRAINT CK_EXPENSE_Total CHECK(total_amount>=0)
 );
 CREATE TABLE dbo.EXPENSE_DETAIL(
  expense_detail_id int IDENTITY PRIMARY KEY,
  expense_id int NOT NULL REFERENCES dbo.EXPENSE(expense_id),
  flight_id int NULL REFERENCES dbo.FLIGHT(flight_id),
  expense_type nvarchar(50) NOT NULL,
  description nvarchar(255) NOT NULL,
  quantity decimal(12,2) NOT NULL CONSTRAINT CK_EXPENSE_DETAIL_Qty CHECK(quantity>0),
  unit_price decimal(18,2) NOT NULL CONSTRAINT CK_EXPENSE_DETAIL_Price CHECK(unit_price>=0),
  subtotal AS CONVERT(decimal(18,2),ROUND(quantity*unit_price,2)) PERSISTED
 );
 CREATE INDEX IX_EXPENSE_DateStatus ON dbo.EXPENSE(expense_date,status);
 CREATE INDEX IX_EXPENSE_DETAIL_Expense ON dbo.EXPENSE_DETAIL(expense_id);
 CREATE TABLE dbo.SYSTEM_PARAMETER(
  parameter_key varchar(80) NOT NULL PRIMARY KEY,
  integer_value int NOT NULL,
  minimum_value int NOT NULL,
  maximum_value int NOT NULL,
  description nvarchar(255) NOT NULL,
  CONSTRAINT CK_SYSTEM_PARAMETER_Bounds CHECK(minimum_value<=integer_value AND integer_value<=maximum_value)
 );
 -- Values match existing rules; upper bounds cannot exceed SQL column capacities.
 INSERT dbo.SYSTEM_PARAMETER VALUES
 ('BOOKING_MAX_PASSENGERS',9,1,9,N'Số hành khách tối đa mỗi lần đặt'),
 ('CONTACT_MIN_AGE_EXCLUSIVE',18,18,100,N'Người liên hệ phải qua sinh nhật tuổi này'),
 ('CONTACT_MAX_NAME_LENGTH',100,1,100,N'Độ dài tên người liên hệ'),
 ('PASSENGER_MAX_NAME_LENGTH',100,1,100,N'Độ dài tên hành khách'),
 ('CONTACT_MAX_DOCUMENT_LENGTH',50,1,50,N'Độ dài giấy tờ người liên hệ'),
 ('PASSENGER_MAX_DOCUMENT_LENGTH',50,1,50,N'Độ dài giấy tờ hành khách'),
 ('CONTACT_MIN_PHONE_DIGITS',8,8,15,N'Số chữ số điện thoại tối thiểu'),
 ('CONTACT_MAX_PHONE_DIGITS',15,8,15,N'Số chữ số điện thoại tối đa'),
 ('BOOKING_MIN_LEAD_MINUTES',0,0,10080,N'Số phút tối thiểu trước giờ bay để đặt'),
 ('CANCEL_MIN_LEAD_MINUTES',0,0,10080,N'Số phút tối thiểu trước giờ bay để hủy');
 ALTER TABLE dbo.INVOICE ADD paid_at datetime2(0) NULL;
 ALTER TABLE dbo.INVOICE ADD CONSTRAINT CK_INVOICE_PaidAt CHECK(paid_at IS NULL OR status='PAID');
 -- Old PAID rows retain NULL: their actual receipt time is unknown.
 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
