:ON ERROR EXIT
/* REVIEW ONLY. Entirely fake fixtures; never copies rows from any other database.
   Run only after approval, new schema creation, and closing all other test sessions.
   Required variables: EXPECTED_SERVER, WRITE_APPROVAL, FIXTURE_APPROVAL. */
SET NOCOUNT ON;
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET XACT_ABORT ON;
IF ISNULL(SESSIONPROPERTY('ANSI_NULLS'),-1)<>1 OR ISNULL(SESSIONPROPERTY('ANSI_PADDING'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('ANSI_WARNINGS'),-1)<>1 OR ISNULL(SESSIONPROPERTY('ARITHABORT'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('CONCAT_NULL_YIELDS_NULL'),-1)<>1 OR ISNULL(SESSIONPROPERTY('QUOTED_IDENTIFIER'),-1)<>1
 OR ISNULL(SESSIONPROPERTY('NUMERIC_ROUNDABORT'),-1)<>0 THROW 51115,'Filtered-index SET options are incorrect.',1;

DECLARE @ExpectedServer nvarchar(128)=N'$(EXPECTED_SERVER)';
DECLARE @WriteApproval nvarchar(128)=N'$(WRITE_APPROVAL)';
DECLARE @Failed nvarchar(2048);
IF N'$(FIXTURE_APPROVAL)'<>N'APPROVE_FAKE_FIXTURES_FLIGHTBOOKING_HIEU_TESTS' THROW 51112,'Fixture approval missing.',1;
/* Generated from repository schema; SELECT only. Parameters supplied by caller.
   Target: FlightBooking_Hieu_ManagementTests. Missing approval is a blocker, never a reason to skip checks. */
SET NOCOUNT ON;
;WITH SessionAccess AS (
 SELECT TRY_CONVERT(int,SERVERPROPERTY('ProductMajorVersion')) MajorVersion,
 ISNULL(IS_SRVROLEMEMBER('sysadmin'),0) IsSysadmin,
 ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER STATE'),0) CanViewState,
 ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER PERFORMANCE STATE'),0) CanViewPerformance
), WriterProfile AS (
 SELECT ISNULL(IS_SRVROLEMEMBER('sysadmin'),1) IsSysadmin,
 ISNULL(IS_SRVROLEMEMBER('dbcreator'),1) IsDbcreator,ISNULL(IS_MEMBER('db_owner'),1) IsDbOwner,
 ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL'),1) HasControl,
 ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER'),1) HasAlter
), MainColumns(TableName,ColumnName) AS (SELECT * FROM (VALUES
        (N'AIRPORT',N'airport_id'),
        (N'AIRPORT',N'airport_name'),
        (N'AIRPORT',N'city'),
        (N'CUSTOMER',N'customer_id'),
        (N'CUSTOMER',N'full_name'),
        (N'CUSTOMER',N'phone'),
        (N'CUSTOMER',N'email'),
        (N'CUSTOMER',N'date_of_birth'),
        (N'CUSTOMER',N'gender'),
        (N'CUSTOMER',N'id_passport_number'),
        (N'EMPLOYEE',N'employee_id'),
        (N'EMPLOYEE',N'full_name'),
        (N'EMPLOYEE',N'phone'),
        (N'EMPLOYEE',N'email'),
        (N'EMPLOYEE',N'position'),
        (N'EMPLOYEE',N'hire_date'),
        (N'EMPLOYEE',N'status'),
        (N'DISCOUNT_CODE',N'discount_id'),
        (N'DISCOUNT_CODE',N'code'),
        (N'DISCOUNT_CODE',N'description'),
        (N'DISCOUNT_CODE',N'discount_type'),
        (N'DISCOUNT_CODE',N'discount_value'),
        (N'DISCOUNT_CODE',N'min_order_amount'),
        (N'DISCOUNT_CODE',N'max_discount_amount'),
        (N'DISCOUNT_CODE',N'start_date'),
        (N'DISCOUNT_CODE',N'end_date'),
        (N'DISCOUNT_CODE',N'usage_limit'),
        (N'DISCOUNT_CODE',N'used_count'),
        (N'DISCOUNT_CODE',N'status'),
        (N'FLIGHT',N'flight_id'),
        (N'FLIGHT',N'departure_airport_id'),
        (N'FLIGHT',N'arrival_airport_id'),
        (N'FLIGHT',N'total_seats'),
        (N'FLIGHT',N'tickets_sold'),
        (N'FLIGHT',N'seats_available'),
        (N'FLIGHT',N'total_revenue'),
        (N'FLIGHT',N'departure_date'),
        (N'FLIGHT',N'departure_time'),
        (N'FLIGHT',N'arrival_date'),
        (N'FLIGHT',N'arrival_time'),
        (N'FLIGHT',N'status'),
        (N'FARE_CLASS',N'fare_id'),
        (N'FARE_CLASS',N'flight_id'),
        (N'FARE_CLASS',N'class_name'),
        (N'FARE_CLASS',N'price'),
        (N'FARE_CLASS',N'base_baggage_kg'),
        (N'TICKET',N'ticket_id'),
        (N'TICKET',N'flight_id'),
        (N'TICKET',N'fare_id'),
        (N'TICKET',N'seat_number'),
        (N'TICKET',N'issued_at'),
        (N'TICKET',N'status'),
        (N'BOOKING',N'booking_id'),
        (N'BOOKING',N'customer_id'),
        (N'BOOKING',N'employee_id'),
        (N'BOOKING',N'flight_id'),
        (N'BOOKING',N'discount_id'),
        (N'BOOKING',N'booking_date'),
        (N'BOOKING',N'booking_source'),
        (N'BOOKING',N'ticket_quantity'),
        (N'BOOKING',N'discount_amount'),
        (N'BOOKING',N'total_amount'),
        (N'BOOKING',N'status'),
        (N'BOOKING_DETAIL',N'booking_detail_id'),
        (N'BOOKING_DETAIL',N'booking_id'),
        (N'BOOKING_DETAIL',N'ticket_id'),
        (N'BOOKING_DETAIL',N'passenger_full_name'),
        (N'BOOKING_DETAIL',N'passenger_id_card'),
        (N'BOOKING_DETAIL',N'price'),
        (N'INVOICE',N'invoice_id'),
        (N'INVOICE',N'booking_id'),
        (N'INVOICE',N'invoice_date'),
        (N'INVOICE',N'total'),
        (N'INVOICE',N'discount_amount'),
        (N'INVOICE',N'tax_amount'),
        (N'INVOICE',N'total_amount'),
        (N'INVOICE',N'status'),
        (N'INVOICE_DETAIL',N'invoice_detail_id'),
        (N'INVOICE_DETAIL',N'invoice_id'),
        (N'INVOICE_DETAIL',N'ticket_id'),
        (N'INVOICE_DETAIL',N'unit_price'),
        (N'INVOICE_DETAIL',N'quantity'),
        (N'INVOICE_DETAIL',N'subtotal')
) v(TableName,ColumnName)), MainTables(Name) AS (SELECT Name FROM (VALUES
        (N'AIRPORT'),
        (N'CUSTOMER'),
        (N'EMPLOYEE'),
        (N'DISCOUNT_CODE'),
        (N'FLIGHT'),
        (N'FARE_CLASS'),
        (N'TICKET'),
        (N'BOOKING'),
        (N'BOOKING_DETAIL'),
        (N'INVOICE'),
        (N'INVOICE_DETAIL')
) v(Name)), ExpectedTables(Name) AS (SELECT Name FROM (VALUES
        (N'AIRPORT'),
        (N'CUSTOMER'),
        (N'EMPLOYEE'),
        (N'DISCOUNT_CODE'),
        (N'FLIGHT'),
        (N'FARE_CLASS'),
        (N'TICKET'),
        (N'BOOKING'),
        (N'BOOKING_DETAIL'),
        (N'INVOICE'),
        (N'INVOICE_DETAIL'),
        (N'BOOKING_WEB_REQUEST')
) v(Name)), ExpectedColumns(TableName,ColumnName,TypeName,MaxLength,PrecisionValue,ScaleValue,NullableValue,IdentityValue) AS (
SELECT * FROM (VALUES
        (N'AIRPORT',N'airport_id',N'int',NULL,NULL,NULL,0,1),
        (N'AIRPORT',N'airport_name',N'nvarchar',300,NULL,NULL,0,0),
        (N'AIRPORT',N'city',N'nvarchar',200,NULL,NULL,0,0),
        (N'CUSTOMER',N'customer_id',N'int',NULL,NULL,NULL,0,1),
        (N'CUSTOMER',N'full_name',N'nvarchar',200,NULL,NULL,0,0),
        (N'CUSTOMER',N'phone',N'varchar',20,NULL,NULL,0,0),
        (N'CUSTOMER',N'email',N'varchar',100,NULL,NULL,0,0),
        (N'CUSTOMER',N'date_of_birth',N'date',NULL,NULL,NULL,0,0),
        (N'CUSTOMER',N'gender',N'nvarchar',20,NULL,NULL,0,0),
        (N'CUSTOMER',N'id_passport_number',N'varchar',50,NULL,NULL,1,0),
        (N'EMPLOYEE',N'employee_id',N'int',NULL,NULL,NULL,0,1),
        (N'EMPLOYEE',N'full_name',N'nvarchar',200,NULL,NULL,0,0),
        (N'EMPLOYEE',N'phone',N'varchar',20,NULL,NULL,0,0),
        (N'EMPLOYEE',N'email',N'varchar',100,NULL,NULL,0,0),
        (N'EMPLOYEE',N'position',N'nvarchar',100,NULL,NULL,0,0),
        (N'EMPLOYEE',N'hire_date',N'date',NULL,NULL,NULL,0,0),
        (N'EMPLOYEE',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'DISCOUNT_CODE',N'discount_id',N'int',NULL,NULL,NULL,0,1),
        (N'DISCOUNT_CODE',N'code',N'varchar',50,NULL,NULL,0,0),
        (N'DISCOUNT_CODE',N'description',N'nvarchar',510,NULL,NULL,0,0),
        (N'DISCOUNT_CODE',N'discount_type',N'varchar',10,NULL,NULL,0,0),
        (N'DISCOUNT_CODE',N'discount_value',N'decimal',NULL,18,2,0,0),
        (N'DISCOUNT_CODE',N'min_order_amount',N'decimal',NULL,18,2,0,0),
        (N'DISCOUNT_CODE',N'max_discount_amount',N'decimal',NULL,18,2,1,0),
        (N'DISCOUNT_CODE',N'start_date',N'datetime2',NULL,NULL,0,0,0),
        (N'DISCOUNT_CODE',N'end_date',N'datetime2',NULL,NULL,0,0,0),
        (N'DISCOUNT_CODE',N'usage_limit',N'int',NULL,NULL,NULL,1,0),
        (N'DISCOUNT_CODE',N'used_count',N'int',NULL,NULL,NULL,0,0),
        (N'DISCOUNT_CODE',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'FLIGHT',N'flight_id',N'int',NULL,NULL,NULL,0,1),
        (N'FLIGHT',N'departure_airport_id',N'int',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'arrival_airport_id',N'int',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'total_seats',N'int',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'tickets_sold',N'int',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'seats_available',N'int',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'total_revenue',N'decimal',NULL,18,2,0,0),
        (N'FLIGHT',N'departure_date',N'date',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'departure_time',N'time',NULL,NULL,0,0,0),
        (N'FLIGHT',N'arrival_date',N'date',NULL,NULL,NULL,0,0),
        (N'FLIGHT',N'arrival_time',N'time',NULL,NULL,0,0,0),
        (N'FLIGHT',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'FARE_CLASS',N'fare_id',N'int',NULL,NULL,NULL,0,1),
        (N'FARE_CLASS',N'flight_id',N'int',NULL,NULL,NULL,0,0),
        (N'FARE_CLASS',N'class_name',N'nvarchar',100,NULL,NULL,0,0),
        (N'FARE_CLASS',N'price',N'decimal',NULL,18,2,0,0),
        (N'FARE_CLASS',N'base_baggage_kg',N'int',NULL,NULL,NULL,0,0),
        (N'TICKET',N'ticket_id',N'int',NULL,NULL,NULL,0,1),
        (N'TICKET',N'flight_id',N'int',NULL,NULL,NULL,0,0),
        (N'TICKET',N'fare_id',N'int',NULL,NULL,NULL,0,0),
        (N'TICKET',N'seat_number',N'varchar',10,NULL,NULL,0,0),
        (N'TICKET',N'issued_at',N'datetime2',NULL,NULL,0,0,0),
        (N'TICKET',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'BOOKING',N'booking_id',N'int',NULL,NULL,NULL,0,1),
        (N'BOOKING',N'customer_id',N'int',NULL,NULL,NULL,0,0),
        (N'BOOKING',N'employee_id',N'int',NULL,NULL,NULL,1,0),
        (N'BOOKING',N'flight_id',N'int',NULL,NULL,NULL,0,0),
        (N'BOOKING',N'discount_id',N'int',NULL,NULL,NULL,1,0),
        (N'BOOKING',N'booking_date',N'datetime2',NULL,NULL,0,0,0),
        (N'BOOKING',N'booking_source',N'varchar',10,NULL,NULL,0,0),
        (N'BOOKING',N'ticket_quantity',N'int',NULL,NULL,NULL,0,0),
        (N'BOOKING',N'discount_amount',N'decimal',NULL,18,2,0,0),
        (N'BOOKING',N'total_amount',N'decimal',NULL,18,2,0,0),
        (N'BOOKING',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'BOOKING_DETAIL',N'booking_detail_id',N'int',NULL,NULL,NULL,0,1),
        (N'BOOKING_DETAIL',N'booking_id',N'int',NULL,NULL,NULL,0,0),
        (N'BOOKING_DETAIL',N'ticket_id',N'int',NULL,NULL,NULL,0,0),
        (N'BOOKING_DETAIL',N'passenger_full_name',N'nvarchar',200,NULL,NULL,0,0),
        (N'BOOKING_DETAIL',N'passenger_id_card',N'varchar',50,NULL,NULL,0,0),
        (N'BOOKING_DETAIL',N'price',N'decimal',NULL,18,2,0,0),
        (N'INVOICE',N'invoice_id',N'int',NULL,NULL,NULL,0,1),
        (N'INVOICE',N'booking_id',N'int',NULL,NULL,NULL,0,0),
        (N'INVOICE',N'invoice_date',N'datetime2',NULL,NULL,0,0,0),
        (N'INVOICE',N'total',N'decimal',NULL,18,2,0,0),
        (N'INVOICE',N'discount_amount',N'decimal',NULL,18,2,0,0),
        (N'INVOICE',N'tax_amount',N'decimal',NULL,18,2,0,0),
        (N'INVOICE',N'total_amount',N'decimal',NULL,18,2,0,0),
        (N'INVOICE',N'status',N'varchar',20,NULL,NULL,0,0),
        (N'INVOICE_DETAIL',N'invoice_detail_id',N'int',NULL,NULL,NULL,0,1),
        (N'INVOICE_DETAIL',N'invoice_id',N'int',NULL,NULL,NULL,0,0),
        (N'INVOICE_DETAIL',N'ticket_id',N'int',NULL,NULL,NULL,0,0),
        (N'INVOICE_DETAIL',N'unit_price',N'decimal',NULL,18,2,0,0),
        (N'INVOICE_DETAIL',N'quantity',N'int',NULL,NULL,NULL,0,0),
        (N'INVOICE_DETAIL',N'subtotal',N'decimal',NULL,18,2,0,0),
        (N'BOOKING_WEB_REQUEST',N'request_hash',N'char',64,NULL,NULL,0,0),
        (N'BOOKING_WEB_REQUEST',N'payload_hash',N'char',64,NULL,NULL,0,0),
        (N'BOOKING_WEB_REQUEST',N'booking_id',N'int',NULL,NULL,NULL,0,0)
) v(TableName,ColumnName,TypeName,MaxLength,PrecisionValue,ScaleValue,NullableValue,IdentityValue)),
ExpectedObjects(Name,Kind) AS (SELECT * FROM (VALUES
        (N'PK_AIRPORT',N'PK'),
        (N'PK_CUSTOMER',N'PK'),
        (N'DF_EMPLOYEE_Status',N'D'),
        (N'PK_EMPLOYEE',N'PK'),
        (N'CK_EMPLOYEE_Status',N'C'),
        (N'DF_DISCOUNT_MinOrder',N'D'),
        (N'DF_DISCOUNT_Used',N'D'),
        (N'DF_DISCOUNT_Status',N'D'),
        (N'PK_DISCOUNT_CODE',N'PK'),
        (N'UQ_DISCOUNT_CODE_Code',N'UQ'),
        (N'CK_DISCOUNT_Type',N'C'),
        (N'CK_DISCOUNT_Value',N'C'),
        (N'CK_DISCOUNT_MinOrder',N'C'),
        (N'CK_DISCOUNT_MaxDiscount',N'C'),
        (N'CK_DISCOUNT_Dates',N'C'),
        (N'CK_DISCOUNT_Usage',N'C'),
        (N'CK_DISCOUNT_Status',N'C'),
        (N'DF_FLIGHT_Sold',N'D'),
        (N'DF_FLIGHT_Revenue',N'D'),
        (N'DF_FLIGHT_Status',N'D'),
        (N'PK_FLIGHT',N'PK'),
        (N'FK_FLIGHT_DepartureAirport',N'F'),
        (N'FK_FLIGHT_ArrivalAirport',N'F'),
        (N'CK_FLIGHT_DifferentAirports',N'C'),
        (N'CK_FLIGHT_SeatCount',N'C'),
        (N'CK_FLIGHT_Revenue',N'C'),
        (N'CK_FLIGHT_Chronology',N'C'),
        (N'CK_FLIGHT_Status',N'C'),
        (N'PK_FARE_CLASS',N'PK'),
        (N'FK_FARE_CLASS_Flight',N'F'),
        (N'UQ_FARE_CLASS_FlightClass',N'UQ'),
        (N'UQ_FARE_CLASS_FareFlight',N'UQ'),
        (N'CK_FARE_CLASS_Price',N'C'),
        (N'CK_FARE_CLASS_Baggage',N'C'),
        (N'DF_TICKET_IssuedAt',N'D'),
        (N'DF_TICKET_Status',N'D'),
        (N'PK_TICKET',N'PK'),
        (N'FK_TICKET_Flight',N'F'),
        (N'FK_TICKET_FareFlight',N'F'),
        (N'CK_TICKET_Status',N'C'),
        (N'CK_TICKET_SeatNotEmpty',N'C'),
        (N'DF_BOOKING_Date',N'D'),
        (N'DF_BOOKING_Discount',N'D'),
        (N'DF_BOOKING_Status',N'D'),
        (N'PK_BOOKING',N'PK'),
        (N'FK_BOOKING_Customer',N'F'),
        (N'FK_BOOKING_Employee',N'F'),
        (N'FK_BOOKING_Flight',N'F'),
        (N'FK_BOOKING_Discount',N'F'),
        (N'CK_BOOKING_Source',N'C'),
        (N'CK_BOOKING_SourceEmployee',N'C'),
        (N'CK_BOOKING_Quantity',N'C'),
        (N'CK_BOOKING_Amounts',N'C'),
        (N'CK_BOOKING_Status',N'C'),
        (N'PK_BOOKING_DETAIL',N'PK'),
        (N'FK_BOOKING_DETAIL_Booking',N'F'),
        (N'FK_BOOKING_DETAIL_Ticket',N'F'),
        (N'UQ_BOOKING_DETAIL_Ticket',N'UQ'),
        (N'CK_BOOKING_DETAIL_Price',N'C'),
        (N'DF_INVOICE_Date',N'D'),
        (N'DF_INVOICE_Discount',N'D'),
        (N'DF_INVOICE_Tax',N'D'),
        (N'DF_INVOICE_Status',N'D'),
        (N'PK_INVOICE',N'PK'),
        (N'FK_INVOICE_Booking',N'F'),
        (N'UQ_INVOICE_Booking',N'UQ'),
        (N'CK_INVOICE_Amounts',N'C'),
        (N'CK_INVOICE_Status',N'C'),
        (N'DF_INVOICE_DETAIL_Quantity',N'D'),
        (N'PK_INVOICE_DETAIL',N'PK'),
        (N'FK_INVOICE_DETAIL_Invoice',N'F'),
        (N'FK_INVOICE_DETAIL_Ticket',N'F'),
        (N'UQ_INVOICE_DETAIL_Ticket',N'UQ'),
        (N'CK_INVOICE_DETAIL_Amounts',N'C'),
        (N'PK_BOOKING_WEB_REQUEST',N'PK'),
        (N'UQ_BOOKING_WEB_REQUEST_Booking',N'UQ'),
        (N'FK_BOOKING_WEB_REQUEST_Booking',N'F')
) v(Name,Kind)), ExpectedIndexes(Name,UniqueValue) AS (SELECT * FROM (VALUES
        (N'PK_AIRPORT',1),
        (N'PK_CUSTOMER',1),
        (N'PK_EMPLOYEE',1),
        (N'PK_DISCOUNT_CODE',1),
        (N'UQ_DISCOUNT_CODE_Code',1),
        (N'PK_FLIGHT',1),
        (N'PK_FARE_CLASS',1),
        (N'UQ_FARE_CLASS_FlightClass',1),
        (N'UQ_FARE_CLASS_FareFlight',1),
        (N'PK_TICKET',1),
        (N'PK_BOOKING',1),
        (N'PK_BOOKING_DETAIL',1),
        (N'UQ_BOOKING_DETAIL_Ticket',1),
        (N'PK_INVOICE',1),
        (N'UQ_INVOICE_Booking',1),
        (N'PK_INVOICE_DETAIL',1),
        (N'UQ_INVOICE_DETAIL_Ticket',1),
        (N'PK_BOOKING_WEB_REQUEST',1),
        (N'UQ_BOOKING_WEB_REQUEST_Booking',1),
        (N'UX_TICKET_ActiveSeat',1),
        (N'IX_FLIGHT_RouteDate',0),
        (N'IX_BOOKING_FlightDate',0),
        (N'IX_BOOKING_DETAIL_Booking',0),
        (N'IX_INVOICE_DETAIL_Invoice',0),
        (N'IX_INVOICE_StatusDate',0)
) v(Name,UniqueValue)), MissingColumns AS (
SELECT e.ColumnName FROM ExpectedColumns e
LEFT JOIN sys.tables t ON t.name=e.TableName AND t.schema_id=SCHEMA_ID(N'dbo')
LEFT JOIN sys.columns c ON c.object_id=t.object_id AND c.name=e.ColumnName
LEFT JOIN sys.types ty ON ty.user_type_id=c.user_type_id
WHERE c.column_id IS NULL OR ty.name<>e.TypeName OR c.is_nullable<>e.NullableValue OR c.is_identity<>e.IdentityValue
 OR (e.MaxLength IS NOT NULL AND c.max_length<>e.MaxLength)
 OR (e.PrecisionValue IS NOT NULL AND c.precision<>e.PrecisionValue)
 OR (e.ScaleValue IS NOT NULL AND c.scale<>e.ScaleValue)
), Checks AS (
SELECT N'server_identity' CheckName, CASE WHEN CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))=@ExpectedServer THEN 1 ELSE 0 END Passed,
 N'Actual SQL Server must match the separately verified expected server.' Detail
UNION ALL SELECT N'database_identity', CASE WHEN DB_NAME()=N'FlightBooking_Hieu_ManagementTests' AND DB_NAME()<>N'FlightBookingDB' AND DB_NAME() LIKE N'%[_]ManagementTests' THEN 1 ELSE 0 END, N'Exact test database only; main database is forbidden.'
UNION ALL SELECT N'write_approval', CASE WHEN @WriteApproval=N'APPROVE_WRITES_TO_FLIGHTBOOKING_HIEU_MANAGEMENTTESTS' THEN 1 ELSE 0 END, N'Human write approval must be explicitly supplied after review.'
UNION ALL SELECT N'writer_profile', CASE WHEN EXISTS(SELECT 1 FROM WriterProfile WHERE IsSysadmin=0 AND IsDbcreator=0 AND IsDbOwner=0 AND HasControl=0 AND HasAlter=0) THEN 1 ELSE 0 END, N'Use a separately approved least-privilege writer, never sysadmin/dbcreator/db_owner/CONTROL/ALTER.'
UNION ALL SELECT N'main_write_isolation', CASE WHEN HAS_DBACCESS(N'FlightBookingDB')=0 THEN 1 WHEN HAS_DBACCESS(N'FlightBookingDB')=1
 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','CONTROL'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','ALTER'),1)=0
 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','INSERT'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','UPDATE'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','DELETE'),1)=0
 AND NOT EXISTS(SELECT 1 FROM MainTables WHERE ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','INSERT'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','UPDATE'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','DELETE'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','ALTER'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','CONTROL'),1)<>0)
 AND NOT EXISTS(SELECT 1 FROM MainColumns WHERE ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+TableName,'OBJECT','UPDATE',ColumnName,'COLUMN'),1)<>0) THEN 1 ELSE 0 END, N'No effective DML/ALTER/CONTROL or column UPDATE on the verified main schema; unknown permissions block writes.'
UNION ALL SELECT N'online_read_write', CASE WHEN DATABASEPROPERTYEX(DB_NAME(),'Status')=N'ONLINE' AND DATABASEPROPERTYEX(DB_NAME(),'Updateability')=N'READ_WRITE' THEN 1 ELSE 0 END, N'Test database must be online and writable.'
UNION ALL SELECT N'test_only_marker', CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.TestOnly' AND CONVERT(nvarchar(128),value)=N'HieuThinh-W7-20261124') THEN 1 ELSE 0 END, N'Database must have been created by the approved test-only setup.'
UNION ALL SELECT N'source_manifest', CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.SchemaSource' AND CONVERT(nvarchar(128),value)=N'B6297CED0C16E1050E6BA00FBA93A60BBBBC1157622289556918CF61B0416D34') THEN 1 ELSE 0 END, N'Schema must correspond to the reviewed repository snapshot.'
UNION ALL SELECT N'tables', CASE WHEN (SELECT COUNT(*) FROM sys.tables)=12 AND NOT EXISTS(SELECT 1 FROM ExpectedTables e WHERE OBJECT_ID(N'dbo.'+e.Name,'U') IS NULL) THEN 1 ELSE 0 END, N'All 12 repository tables, including EMPLOYEE and BOOKING_WEB_REQUEST, are required.'
UNION ALL SELECT N'columns', CASE WHEN NOT EXISTS(SELECT 1 FROM MissingColumns) AND (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id)= 86 THEN 1 ELSE 0 END, N'Column names, SQL types, lengths/scales, nullability and identities must match source.'
UNION ALL SELECT N'constraints', CASE WHEN NOT EXISTS(SELECT 1 FROM ExpectedObjects e LEFT JOIN sys.objects o ON o.name=e.Name AND o.type=e.Kind AND o.schema_id=SCHEMA_ID(N'dbo') WHERE o.object_id IS NULL) THEN 1 ELSE 0 END, N'All named PK/UQ/FK/CHECK/DEFAULT constraints are required.'
UNION ALL SELECT N'fk_check_trust', CASE WHEN NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) AND NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THEN 1 ELSE 0 END, N'Foreign keys and CHECK constraints must be enabled and trusted.'
UNION ALL SELECT N'indexes', CASE WHEN NOT EXISTS(SELECT 1 FROM ExpectedIndexes e LEFT JOIN sys.indexes i ON i.name=e.Name AND i.is_unique=e.UniqueValue AND i.is_disabled=0 WHERE i.index_id IS NULL) THEN 1 ELSE 0 END, N'Repository indexes, including enabled unique indexes, are required.'
UNION ALL SELECT N'active_seat_index', CASE WHEN EXISTS(SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.TICKET') AND i.name=N'UX_TICKET_ActiveSeat' AND i.is_unique=1 AND i.has_filter=1 AND i.is_disabled=0 AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(i.filter_definition,N'(',N''),N')',N''),N'[',N''),N']',N''),N' ',N'')=N'status=''ISSUED''' AND (SELECT STRING_AGG(c.name,N',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0)=N'flight_id,seat_number') THEN 1 ELSE 0 END, N'Unique active-seat key/filter must protect flight_id and seat_number.'
UNION ALL SELECT N'permissions', CASE WHEN HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')=1 AND NOT EXISTS(SELECT 1 FROM ExpectedTables WHERE ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','SELECT'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','INSERT'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','UPDATE'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','DELETE'),0)<>1) THEN 1 ELSE 0 END, N'Test principal needs schema visibility and DML rights on test tables.'
UNION ALL SELECT N'exclusive_use', CASE WHEN EXISTS(SELECT 1 FROM SessionAccess WHERE MajorVersion IS NOT NULL AND MajorVersion>0 AND (IsSysadmin=1 OR (MajorVersion>=16 AND CanViewPerformance=1) OR (MajorVersion<16 AND CanViewState=1))) AND NOT EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1 AND database_id=DB_ID() AND session_id<>@@SPID) THEN 1 ELSE 0 END, N'Known version required: SQL 16+ needs VIEW SERVER PERFORMANCE STATE; older versions need VIEW SERVER STATE. Other test sessions are forbidden.'
)
SELECT @Failed=STRING_AGG(CheckName,N',') FROM Checks WHERE Passed=0;
IF @Failed IS NOT NULL THROW 51110,@Failed,1;

-- Only an empty, newly prepared test database is eligible. Never erase old fixtures.
IF EXISTS(SELECT 1 FROM dbo.AIRPORT) OR EXISTS(SELECT 1 FROM dbo.FLIGHT)
 OR EXISTS(SELECT 1 FROM dbo.FARE_CLASS) OR EXISTS(SELECT 1 FROM dbo.CUSTOMER)
 OR EXISTS(SELECT 1 FROM dbo.EMPLOYEE) OR EXISTS(SELECT 1 FROM dbo.DISCOUNT_CODE)
 OR EXISTS(SELECT 1 FROM dbo.BOOKING) OR EXISTS(SELECT 1 FROM dbo.BOOKING_DETAIL)
 OR EXISTS(SELECT 1 FROM dbo.TICKET) OR EXISTS(SELECT 1 FROM dbo.INVOICE)
 OR EXISTS(SELECT 1 FROM dbo.INVOICE_DETAIL) OR EXISTS(SELECT 1 FROM dbo.BOOKING_WEB_REQUEST)
 THROW 51113,'Fixture requires empty tables; stop without deleting existing data.',1;

DECLARE @Today date=CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
DECLARE @OutDate date=DATEADD(day,30,@Today),@BackDate date=DATEADD(day,31,@Today);
DECLARE @AirportA int,@AirportB int,@Outbound int,@Returning int,@LastSeat int;
DECLARE @OutFare int,@BackFare int,@LastFare int,@Customer int,@Discount int,@Booking int,@Ticket int,@Invoice int;
DECLARE @AccessKey varchar(64)=CONVERT(varchar(64),CRYPT_GEN_RANDOM(32),2);
-- ASCII hexadecimal key: SQL VARCHAR bytes equal the application's UTF-8 input.
DECLARE @RequestHash char(64)=CONVERT(char(64),HASHBYTES('SHA2_256',@AccessKey),2);
DECLARE @PayloadHash char(64)=CONVERT(char(64),HASHBYTES('SHA2_256','FAKE-FIXTURE-NOT-A-REPLAYABLE-REQUEST'),2);
BEGIN TRY
 BEGIN TRANSACTION;
 -- Phase A: routes, future round trip and a separate flight with just one seat.
 IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' OR DB_NAME()=N'FlightBookingDB' THROW 51111,'Fixture target changed; stop.',1;

 INSERT dbo.AIRPORT(airport_name,city) VALUES(N'TEST ONLY - San bay A',N'Thanh pho gia A');
 SET @AirportA=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.AIRPORT(airport_name,city) VALUES(N'TEST ONLY - San bay B',N'Thanh pho gia B');
 SET @AirportB=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status)
 VALUES(@AirportA,@AirportB,10,0,10,0,@OutDate,'07:15',@OutDate,'09:15','SCHEDULED');
 SET @Outbound=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status)
 VALUES(@AirportB,@AirportA,10,0,10,0,@BackDate,'13:40',@BackDate,'15:40','SCHEDULED');
 SET @Returning=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status)
 VALUES(@AirportA,@AirportB,1,0,1,0,DATEADD(day,32,@Today),'10:00',DATEADD(day,32,@Today),'12:00','SCHEDULED');
 SET @LastSeat=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) VALUES(@Outbound,N'Economy',1000000,7);
 SET @OutFare=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) VALUES(@Returning,N'Economy',1000000,7);
 SET @BackFare=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) VALUES(@LastSeat,N'Economy',1000000,7);
 SET @LastFare=CONVERT(int,SCOPE_IDENTITY());

 -- Phase B: synthetic customer and an active, limited discount; no real contact data.
 IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' OR DB_NAME()=N'FlightBookingDB' THROW 51111,'Fixture target changed; stop.',1;

 INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
 VALUES(N'Khach TEST ONLY','0900000000','hieu-thinh-fixture@example.test','20000101',N'Nam','TEST-BOOKER-ONLY');
 SET @Customer=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,min_order_amount,max_discount_amount,start_date,end_date,usage_limit,used_count,status)
 VALUES('HIEUTHINH_TEST_ONLY',N'Ma gia chi dung DB test','FIXED',100000,0,NULL,DATEADD(day,-1,@Today),DATEADD(day,60,@Today),10,1,'ACTIVE');
 SET @Discount=CONVERT(int,SCOPE_IDENTITY());

 -- Phase C: referenced, unpaid booking; all dependent rows/counters commit together.
 IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' OR DB_NAME()=N'FlightBookingDB' THROW 51111,'Fixture target changed; stop.',1;

 INSERT dbo.BOOKING(customer_id,employee_id,flight_id,discount_id,booking_source,ticket_quantity,discount_amount,total_amount,status)
 VALUES(@Customer,NULL,@Outbound,@Discount,'WEB',1,100000,900000,'CONFIRMED');
 SET @Booking=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.TICKET(flight_id,fare_id,seat_number,status) VALUES(@Outbound,@OutFare,'1A','ISSUED');
 SET @Ticket=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
 VALUES(@Booking,@Ticket,N'Hanh khach TEST ONLY','TEST-PASSENGER-ONLY',1000000);
 INSERT dbo.INVOICE(booking_id,total,discount_amount,tax_amount,total_amount,status) VALUES(@Booking,1000000,100000,0,900000,'UNPAID');
 SET @Invoice=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal) VALUES(@Invoice,@Ticket,1000000,1,1000000);
 INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id) VALUES(@RequestHash,@PayloadHash,@Booking);
 UPDATE dbo.FLIGHT SET tickets_sold=1,seats_available=9 WHERE flight_id=@Outbound;
 IF @@ROWCOUNT<>1 THROW 51114,'Fixture seat update failed.',1;
 -- UNPAID: total_revenue remains zero. No fake payment is recorded.
 IF DB_NAME()<>N'FlightBooking_Hieu_ManagementTests' OR DB_NAME()=N'FlightBookingDB' THROW 51111,'Fixture target changed; stop.',1;

 COMMIT TRANSACTION;
END TRY
BEGIN CATCH
 IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
 THROW;
END CATCH;
SELECT @AirportA AirportA,@AirportB AirportB,@Outbound OutboundId,@OutFare OutboundFareId,
 @Returning ReturnId,@BackFare ReturnFareId,@LastSeat LastSeatFlightId,@LastFare LastSeatFareId,
 @Customer CustomerId,@Discount DiscountId,@Booking UnpaidBookingId,@AccessKey FakeLookupKey,
 @OutDate OutboundDate,@BackDate ReturnDate;
-- Keep this result for manual lookup/cancel. The synthetic payload is deliberately
-- not an application retry payload; use a fresh key for all create/idempotency tests.
