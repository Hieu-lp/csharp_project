:ON ERROR EXIT
-- SELECT/metadata verification only. Never connects to either older database.
SET NOCOUNT ON;
IF DB_NAME()<>N'FlightBooking_Final' THROW 51220,'Wrong verification database.',1;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51221,'Wrong server.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalDemo' AND CONVERT(nvarchar(128),value)=N'Final-20261127-v1') THROW 51222,'Missing Final marker.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.SchemaSource' AND CONVERT(nvarchar(128),value)=N'B6297CED0C16E1050E6BA00FBA93A60BBBBC1157622289556918CF61B0416D34') THROW 51223,'Wrong source fingerprint.',1;

;WITH Expected(TableName,ColumnName,SqlType,Length,Prec,Scale,Nullable,IdentityFlag) AS (SELECT * FROM (VALUES
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
) v(TableName,ColumnName,SqlType,Length,Prec,Scale,Nullable,IdentityFlag)),Actual(TableName,ColumnName,SqlType,Length,Prec,Scale,Nullable,IdentityFlag) AS (SELECT t.name,c.name,ty.name,CASE WHEN ty.name IN('varchar','nvarchar','char') THEN c.max_length END,CASE WHEN ty.name='decimal' THEN c.precision END,CASE WHEN ty.name IN('decimal','time','datetime2') THEN c.scale END,CONVERT(int,c.is_nullable),CONVERT(int,c.is_identity)
 FROM sys.tables t JOIN sys.columns c ON t.object_id=c.object_id JOIN sys.types ty ON c.user_type_id=ty.user_type_id WHERE t.is_ms_shipped=0),Mismatch AS (
 SELECT * FROM (SELECT * FROM Expected EXCEPT SELECT * FROM Actual) Missing
 UNION ALL SELECT * FROM (SELECT * FROM Actual EXCEPT SELECT * FROM Expected) Extra)
SELECT * FROM Mismatch;
IF @@ROWCOUNT<>0 THROW 51224,'Schema columns differ. Do not auto-repair.',1;
PRINT 'PASS: columns match source definitions.';

;WITH Expected(TableName,ConstraintName,Kind) AS (SELECT * FROM (VALUES
 (N'AIRPORT',N'PK_AIRPORT',N'PK'),
 (N'CUSTOMER',N'PK_CUSTOMER',N'PK'),
 (N'EMPLOYEE',N'DF_EMPLOYEE_Status',N'D'),
 (N'EMPLOYEE',N'PK_EMPLOYEE',N'PK'),
 (N'EMPLOYEE',N'CK_EMPLOYEE_Status',N'C'),
 (N'DISCOUNT_CODE',N'DF_DISCOUNT_MinOrder',N'D'),
 (N'DISCOUNT_CODE',N'DF_DISCOUNT_Used',N'D'),
 (N'DISCOUNT_CODE',N'DF_DISCOUNT_Status',N'D'),
 (N'DISCOUNT_CODE',N'PK_DISCOUNT_CODE',N'PK'),
 (N'DISCOUNT_CODE',N'UQ_DISCOUNT_CODE_Code',N'UQ'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_Type',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_Value',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_MinOrder',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_MaxDiscount',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_Dates',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_Usage',N'C'),
 (N'DISCOUNT_CODE',N'CK_DISCOUNT_Status',N'C'),
 (N'FLIGHT',N'DF_FLIGHT_Sold',N'D'),
 (N'FLIGHT',N'DF_FLIGHT_Revenue',N'D'),
 (N'FLIGHT',N'DF_FLIGHT_Status',N'D'),
 (N'FLIGHT',N'PK_FLIGHT',N'PK'),
 (N'FLIGHT',N'FK_FLIGHT_DepartureAirport',N'F'),
 (N'FLIGHT',N'FK_FLIGHT_ArrivalAirport',N'F'),
 (N'FLIGHT',N'CK_FLIGHT_DifferentAirports',N'C'),
 (N'FLIGHT',N'CK_FLIGHT_SeatCount',N'C'),
 (N'FLIGHT',N'CK_FLIGHT_Revenue',N'C'),
 (N'FLIGHT',N'CK_FLIGHT_Chronology',N'C'),
 (N'FLIGHT',N'CK_FLIGHT_Status',N'C'),
 (N'FARE_CLASS',N'PK_FARE_CLASS',N'PK'),
 (N'FARE_CLASS',N'FK_FARE_CLASS_Flight',N'F'),
 (N'FARE_CLASS',N'UQ_FARE_CLASS_FlightClass',N'UQ'),
 (N'FARE_CLASS',N'UQ_FARE_CLASS_FareFlight',N'UQ'),
 (N'FARE_CLASS',N'CK_FARE_CLASS_Price',N'C'),
 (N'FARE_CLASS',N'CK_FARE_CLASS_Baggage',N'C'),
 (N'TICKET',N'DF_TICKET_IssuedAt',N'D'),
 (N'TICKET',N'DF_TICKET_Status',N'D'),
 (N'TICKET',N'PK_TICKET',N'PK'),
 (N'TICKET',N'FK_TICKET_Flight',N'F'),
 (N'TICKET',N'FK_TICKET_FareFlight',N'F'),
 (N'TICKET',N'CK_TICKET_Status',N'C'),
 (N'TICKET',N'CK_TICKET_SeatNotEmpty',N'C'),
 (N'BOOKING',N'DF_BOOKING_Date',N'D'),
 (N'BOOKING',N'DF_BOOKING_Discount',N'D'),
 (N'BOOKING',N'DF_BOOKING_Status',N'D'),
 (N'BOOKING',N'PK_BOOKING',N'PK'),
 (N'BOOKING',N'FK_BOOKING_Customer',N'F'),
 (N'BOOKING',N'FK_BOOKING_Employee',N'F'),
 (N'BOOKING',N'FK_BOOKING_Flight',N'F'),
 (N'BOOKING',N'FK_BOOKING_Discount',N'F'),
 (N'BOOKING',N'CK_BOOKING_Source',N'C'),
 (N'BOOKING',N'CK_BOOKING_SourceEmployee',N'C'),
 (N'BOOKING',N'CK_BOOKING_Quantity',N'C'),
 (N'BOOKING',N'CK_BOOKING_Amounts',N'C'),
 (N'BOOKING',N'CK_BOOKING_Status',N'C'),
 (N'BOOKING_DETAIL',N'PK_BOOKING_DETAIL',N'PK'),
 (N'BOOKING_DETAIL',N'FK_BOOKING_DETAIL_Booking',N'F'),
 (N'BOOKING_DETAIL',N'FK_BOOKING_DETAIL_Ticket',N'F'),
 (N'BOOKING_DETAIL',N'UQ_BOOKING_DETAIL_Ticket',N'UQ'),
 (N'BOOKING_DETAIL',N'CK_BOOKING_DETAIL_Price',N'C'),
 (N'INVOICE',N'DF_INVOICE_Date',N'D'),
 (N'INVOICE',N'DF_INVOICE_Discount',N'D'),
 (N'INVOICE',N'DF_INVOICE_Tax',N'D'),
 (N'INVOICE',N'DF_INVOICE_Status',N'D'),
 (N'INVOICE',N'PK_INVOICE',N'PK'),
 (N'INVOICE',N'FK_INVOICE_Booking',N'F'),
 (N'INVOICE',N'UQ_INVOICE_Booking',N'UQ'),
 (N'INVOICE',N'CK_INVOICE_Amounts',N'C'),
 (N'INVOICE',N'CK_INVOICE_Status',N'C'),
 (N'INVOICE_DETAIL',N'DF_INVOICE_DETAIL_Quantity',N'D'),
 (N'INVOICE_DETAIL',N'PK_INVOICE_DETAIL',N'PK'),
 (N'INVOICE_DETAIL',N'FK_INVOICE_DETAIL_Invoice',N'F'),
 (N'INVOICE_DETAIL',N'FK_INVOICE_DETAIL_Ticket',N'F'),
 (N'INVOICE_DETAIL',N'UQ_INVOICE_DETAIL_Ticket',N'UQ'),
 (N'INVOICE_DETAIL',N'CK_INVOICE_DETAIL_Amounts',N'C'),
 (N'BOOKING_WEB_REQUEST',N'PK_BOOKING_WEB_REQUEST',N'PK'),
 (N'BOOKING_WEB_REQUEST',N'UQ_BOOKING_WEB_REQUEST_Booking',N'UQ'),
 (N'BOOKING_WEB_REQUEST',N'FK_BOOKING_WEB_REQUEST_Booking',N'F')
) v(TableName,ConstraintName,Kind)),Actual(TableName,ConstraintName,Kind) AS (SELECT OBJECT_NAME(o.parent_object_id),o.name,CONVERT(nvarchar(2),o.type) FROM sys.objects o WHERE type IN('PK','UQ','F','C','D')),Mismatch AS (
 SELECT * FROM (SELECT * FROM Expected EXCEPT SELECT * FROM Actual) Missing
 UNION ALL SELECT * FROM (SELECT * FROM Actual EXCEPT SELECT * FROM Expected) Extra)
SELECT * FROM Mismatch;
IF @@ROWCOUNT<>0 THROW 51224,'Schema constraints differ. Do not auto-repair.',1;
PRINT 'PASS: constraints match source definitions.';

;WITH Expected(FkName,TableName,ColumnName,TargetTable,TargetColumn,Ordinal) AS (SELECT * FROM (VALUES
 (N'FK_FLIGHT_DepartureAirport',N'FLIGHT',N'departure_airport_id',N'AIRPORT',N'airport_id',1),
 (N'FK_FLIGHT_ArrivalAirport',N'FLIGHT',N'arrival_airport_id',N'AIRPORT',N'airport_id',1),
 (N'FK_FARE_CLASS_Flight',N'FARE_CLASS',N'flight_id',N'FLIGHT',N'flight_id',1),
 (N'FK_TICKET_Flight',N'TICKET',N'flight_id',N'FLIGHT',N'flight_id',1),
 (N'FK_TICKET_FareFlight',N'TICKET',N'fare_id',N'FARE_CLASS',N'fare_id',1),
 (N'FK_TICKET_FareFlight',N'TICKET',N'flight_id',N'FARE_CLASS',N'flight_id',2),
 (N'FK_BOOKING_Customer',N'BOOKING',N'customer_id',N'CUSTOMER',N'customer_id',1),
 (N'FK_BOOKING_Employee',N'BOOKING',N'employee_id',N'EMPLOYEE',N'employee_id',1),
 (N'FK_BOOKING_Flight',N'BOOKING',N'flight_id',N'FLIGHT',N'flight_id',1),
 (N'FK_BOOKING_Discount',N'BOOKING',N'discount_id',N'DISCOUNT_CODE',N'discount_id',1),
 (N'FK_BOOKING_DETAIL_Booking',N'BOOKING_DETAIL',N'booking_id',N'BOOKING',N'booking_id',1),
 (N'FK_BOOKING_DETAIL_Ticket',N'BOOKING_DETAIL',N'ticket_id',N'TICKET',N'ticket_id',1),
 (N'FK_INVOICE_Booking',N'INVOICE',N'booking_id',N'BOOKING',N'booking_id',1),
 (N'FK_INVOICE_DETAIL_Invoice',N'INVOICE_DETAIL',N'invoice_id',N'INVOICE',N'invoice_id',1),
 (N'FK_INVOICE_DETAIL_Ticket',N'INVOICE_DETAIL',N'ticket_id',N'TICKET',N'ticket_id',1),
 (N'FK_BOOKING_WEB_REQUEST_Booking',N'BOOKING_WEB_REQUEST',N'booking_id',N'BOOKING',N'booking_id',1)
) v(FkName,TableName,ColumnName,TargetTable,TargetColumn,Ordinal)),Actual(FkName,TableName,ColumnName,TargetTable,TargetColumn,Ordinal) AS (SELECT f.name,OBJECT_NAME(f.parent_object_id),COL_NAME(f.parent_object_id,c.parent_column_id),OBJECT_NAME(f.referenced_object_id),COL_NAME(f.referenced_object_id,c.referenced_column_id),c.constraint_column_id
 FROM sys.foreign_keys f JOIN sys.foreign_key_columns c ON f.object_id=c.constraint_object_id),Mismatch AS (
 SELECT * FROM (SELECT * FROM Expected EXCEPT SELECT * FROM Actual) Missing
 UNION ALL SELECT * FROM (SELECT * FROM Actual EXCEPT SELECT * FROM Expected) Extra)
SELECT * FROM Mismatch;
IF @@ROWCOUNT<>0 THROW 51224,'Schema foreign key columns differ. Do not auto-repair.',1;
PRINT 'PASS: foreign key columns match source definitions.';

;WITH Expected(TableName,ColumnName,ConstraintName) AS (SELECT * FROM (VALUES
 (N'EMPLOYEE',N'status',N'DF_EMPLOYEE_Status'),
 (N'DISCOUNT_CODE',N'min_order_amount',N'DF_DISCOUNT_MinOrder'),
 (N'DISCOUNT_CODE',N'used_count',N'DF_DISCOUNT_Used'),
 (N'DISCOUNT_CODE',N'status',N'DF_DISCOUNT_Status'),
 (N'FLIGHT',N'tickets_sold',N'DF_FLIGHT_Sold'),
 (N'FLIGHT',N'total_revenue',N'DF_FLIGHT_Revenue'),
 (N'FLIGHT',N'status',N'DF_FLIGHT_Status'),
 (N'TICKET',N'issued_at',N'DF_TICKET_IssuedAt'),
 (N'TICKET',N'status',N'DF_TICKET_Status'),
 (N'BOOKING',N'booking_date',N'DF_BOOKING_Date'),
 (N'BOOKING',N'discount_amount',N'DF_BOOKING_Discount'),
 (N'BOOKING',N'status',N'DF_BOOKING_Status'),
 (N'INVOICE',N'invoice_date',N'DF_INVOICE_Date'),
 (N'INVOICE',N'discount_amount',N'DF_INVOICE_Discount'),
 (N'INVOICE',N'tax_amount',N'DF_INVOICE_Tax'),
 (N'INVOICE',N'status',N'DF_INVOICE_Status'),
 (N'INVOICE_DETAIL',N'quantity',N'DF_INVOICE_DETAIL_Quantity')
) v(TableName,ColumnName,ConstraintName)),Actual(TableName,ColumnName,ConstraintName) AS (SELECT OBJECT_NAME(d.parent_object_id),COL_NAME(d.parent_object_id,d.parent_column_id),d.name FROM sys.default_constraints d),Mismatch AS (
 SELECT * FROM (SELECT * FROM Expected EXCEPT SELECT * FROM Actual) Missing
 UNION ALL SELECT * FROM (SELECT * FROM Actual EXCEPT SELECT * FROM Expected) Extra)
SELECT * FROM Mismatch;
IF @@ROWCOUNT<>0 THROW 51224,'Schema default column bindings differ. Do not auto-repair.',1;
PRINT 'PASS: default column bindings match source definitions.';

;WITH Expected(TableName,IndexName,UniqueFlag,KeyColumns,IndexType,FilterText) AS (SELECT * FROM (VALUES
 (N'AIRPORT',N'PK_AIRPORT',1,N'airport_id',1,N''),
 (N'CUSTOMER',N'PK_CUSTOMER',1,N'customer_id',1,N''),
 (N'EMPLOYEE',N'PK_EMPLOYEE',1,N'employee_id',1,N''),
 (N'DISCOUNT_CODE',N'PK_DISCOUNT_CODE',1,N'discount_id',1,N''),
 (N'DISCOUNT_CODE',N'UQ_DISCOUNT_CODE_Code',1,N'code',2,N''),
 (N'FLIGHT',N'PK_FLIGHT',1,N'flight_id',1,N''),
 (N'FARE_CLASS',N'PK_FARE_CLASS',1,N'fare_id',1,N''),
 (N'FARE_CLASS',N'UQ_FARE_CLASS_FlightClass',1,N'flight_id,class_name',2,N''),
 (N'FARE_CLASS',N'UQ_FARE_CLASS_FareFlight',1,N'fare_id,flight_id',2,N''),
 (N'TICKET',N'PK_TICKET',1,N'ticket_id',1,N''),
 (N'BOOKING',N'PK_BOOKING',1,N'booking_id',1,N''),
 (N'BOOKING_DETAIL',N'PK_BOOKING_DETAIL',1,N'booking_detail_id',1,N''),
 (N'BOOKING_DETAIL',N'UQ_BOOKING_DETAIL_Ticket',1,N'ticket_id',2,N''),
 (N'INVOICE',N'PK_INVOICE',1,N'invoice_id',1,N''),
 (N'INVOICE',N'UQ_INVOICE_Booking',1,N'booking_id',2,N''),
 (N'INVOICE_DETAIL',N'PK_INVOICE_DETAIL',1,N'invoice_detail_id',1,N''),
 (N'INVOICE_DETAIL',N'UQ_INVOICE_DETAIL_Ticket',1,N'ticket_id',2,N''),
 (N'BOOKING_WEB_REQUEST',N'PK_BOOKING_WEB_REQUEST',1,N'request_hash',1,N''),
 (N'BOOKING_WEB_REQUEST',N'UQ_BOOKING_WEB_REQUEST_Booking',1,N'booking_id',2,N''),
 (N'TICKET',N'UX_TICKET_ActiveSeat',1,N'flight_id,seat_number',2,N'status=''ISSUED'''),
 (N'FLIGHT',N'IX_FLIGHT_RouteDate',0,N'departure_airport_id,arrival_airport_id,departure_date',2,N''),
 (N'BOOKING',N'IX_BOOKING_FlightDate',0,N'flight_id,booking_date',2,N''),
 (N'BOOKING_DETAIL',N'IX_BOOKING_DETAIL_Booking',0,N'booking_id',2,N''),
 (N'INVOICE_DETAIL',N'IX_INVOICE_DETAIL_Invoice',0,N'invoice_id',2,N''),
 (N'INVOICE',N'IX_INVOICE_StatusDate',0,N'status,invoice_date',2,N'')
) v(TableName,IndexName,UniqueFlag,KeyColumns,IndexType,FilterText)),Actual(TableName,IndexName,UniqueFlag,KeyColumns,IndexType,FilterText) AS (SELECT t.name,x.name,CONVERT(int,x.is_unique),STRING_AGG(c.name,',') WITHIN GROUP(ORDER BY k.key_ordinal),x.type,
 REPLACE(REPLACE(REPLACE(REPLACE(COALESCE(x.filter_definition,''),'[',''),']',''),'(',''),')','')
 FROM sys.tables t JOIN sys.indexes x ON t.object_id=x.object_id JOIN sys.index_columns k ON x.object_id=k.object_id AND x.index_id=k.index_id AND k.key_ordinal>0 JOIN sys.columns c ON c.object_id=k.object_id AND c.column_id=k.column_id
 WHERE t.is_ms_shipped=0 GROUP BY t.name,x.name,x.is_unique,x.type,x.filter_definition),Mismatch AS (
 SELECT * FROM (SELECT * FROM Expected EXCEPT SELECT * FROM Actual) Missing
 UNION ALL SELECT * FROM (SELECT * FROM Actual EXCEPT SELECT * FROM Expected) Extra)
SELECT * FROM Mismatch;
IF @@ROWCOUNT<>0 THROW 51224,'Schema indexes differ. Do not auto-repair.',1;
PRINT 'PASS: indexes match source definitions.';

IF (SELECT COUNT(*) FROM sys.tables WHERE is_ms_shipped=0)<>12 THROW 51225,'Expected 12 tables.',1;
IF EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1 OR delete_referential_action<>0 OR update_referential_action<>0)
 OR EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1)
 OR EXISTS(SELECT 1 FROM sys.indexes x JOIN sys.tables t ON t.object_id=x.object_id WHERE t.is_ms_shipped=0 AND (x.is_disabled=1 OR x.is_hypothetical=1))
 OR EXISTS(SELECT 1 FROM sys.index_columns x JOIN sys.tables t ON t.object_id=x.object_id WHERE t.is_ms_shipped=0 AND (x.is_descending_key=1 OR x.is_included_column=1))
 THROW 51226,'Unexpected disabled/untrusted constraints, cascade, included/descending index columns.',1;
IF EXISTS(SELECT 1 FROM sys.identity_columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.is_ms_shipped=0 AND (CONVERT(bigint,c.seed_value)<>1 OR CONVERT(bigint,c.increment_value)<>1 OR c.is_not_for_replication=1))
 THROW 51227,'Identity properties differ.',1;
PRINT 'PASS: 12 tables; enabled/trusted FK/CHECK; index and identity properties.';
SELECT CONVERT(varchar(128),@@SERVERNAME) ServerName,DB_NAME() DatabaseName,CONVERT(varchar(128),ORIGINAL_LOGIN()) LoginName;
SELECT type,COUNT(*) Quantity FROM sys.objects WHERE type IN('PK','UQ','F','C','D') GROUP BY type ORDER BY type;
SELECT COUNT(*) ColumnCount FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id WHERE t.is_ms_shipped=0;
SELECT COUNT(*) IndexCount FROM sys.indexes x JOIN sys.tables t ON x.object_id=t.object_id WHERE t.is_ms_shipped=0 AND x.index_id>0;
SELECT OBJECT_NAME(parent_object_id) TableName,name,definition FROM sys.check_constraints ORDER BY 1,name;
SELECT OBJECT_NAME(parent_object_id) TableName,COL_NAME(parent_object_id,parent_column_id) ColumnName,name,definition FROM sys.default_constraints ORDER BY 1,name;
SELECT N'AIRPORT' TableName,COUNT_BIG(*) SoDong FROM dbo.[AIRPORT]
UNION ALL SELECT N'CUSTOMER' TableName,COUNT_BIG(*) SoDong FROM dbo.[CUSTOMER]
UNION ALL SELECT N'EMPLOYEE' TableName,COUNT_BIG(*) SoDong FROM dbo.[EMPLOYEE]
UNION ALL SELECT N'DISCOUNT_CODE' TableName,COUNT_BIG(*) SoDong FROM dbo.[DISCOUNT_CODE]
UNION ALL SELECT N'FLIGHT' TableName,COUNT_BIG(*) SoDong FROM dbo.[FLIGHT]
UNION ALL SELECT N'FARE_CLASS' TableName,COUNT_BIG(*) SoDong FROM dbo.[FARE_CLASS]
UNION ALL SELECT N'TICKET' TableName,COUNT_BIG(*) SoDong FROM dbo.[TICKET]
UNION ALL SELECT N'BOOKING' TableName,COUNT_BIG(*) SoDong FROM dbo.[BOOKING]
UNION ALL SELECT N'BOOKING_DETAIL' TableName,COUNT_BIG(*) SoDong FROM dbo.[BOOKING_DETAIL]
UNION ALL SELECT N'INVOICE' TableName,COUNT_BIG(*) SoDong FROM dbo.[INVOICE]
UNION ALL SELECT N'INVOICE_DETAIL' TableName,COUNT_BIG(*) SoDong FROM dbo.[INVOICE_DETAIL]
UNION ALL SELECT N'BOOKING_WEB_REQUEST' TableName,COUNT_BIG(*) SoDong FROM dbo.[BOOKING_WEB_REQUEST];

-- Cross-table business consistency. These checks on empty tables are NOT seed acceptance.
IF EXISTS(SELECT 1 FROM dbo.FLIGHT f WHERE f.tickets_sold<>(SELECT COUNT(*) FROM dbo.TICKET t WHERE t.flight_id=f.flight_id AND t.status='ISSUED') OR f.tickets_sold+f.seats_available<>f.total_seats OR f.total_revenue<>COALESCE((SELECT SUM(i.total_amount) FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE b.flight_id=f.flight_id AND i.status='PAID'),0)) THROW 51228,'Seat/revenue counters differ.',1;
IF EXISTS(SELECT 1 FROM dbo.BOOKING b WHERE b.ticket_quantity<>(SELECT COUNT(*) FROM dbo.BOOKING_DETAIL d WHERE d.booking_id=b.booking_id)) THROW 51229,'Booking quantities differ.',1;
IF EXISTS(SELECT 1 FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE i.total<>COALESCE((SELECT SUM(d.subtotal) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id),0) OR i.total_amount<>b.total_amount OR i.discount_amount<>b.discount_amount OR b.ticket_quantity<>(SELECT COUNT(*) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id) OR (b.status='CANCELLED' AND i.status<>'CANCELLED') OR (b.status='CONFIRMED' AND i.status='CANCELLED')) THROW 51230,'Invoice/Booking history differs.',1;
IF EXISTS(SELECT 1 FROM dbo.BOOKING_DETAIL d JOIN dbo.BOOKING b ON b.booking_id=d.booking_id JOIN dbo.TICKET t ON t.ticket_id=d.ticket_id JOIN dbo.INVOICE_DETAIL i ON i.ticket_id=t.ticket_id JOIN dbo.INVOICE h ON h.invoice_id=i.invoice_id WHERE d.price<>i.unit_price OR b.flight_id<>t.flight_id OR h.booking_id<>b.booking_id OR (b.status='CANCELLED' AND t.status<>'CANCELLED') OR (b.status='CONFIRMED' AND t.status<>'ISSUED')) THROW 51231,'Ticket/history/status links differ.',1;
IF EXISTS(SELECT 1 FROM dbo.DISCOUNT_CODE d WHERE d.used_count<>(SELECT COUNT(*) FROM dbo.BOOKING b WHERE b.discount_id=d.discount_id AND b.status='CONFIRMED')) THROW 51232,'Discount counter differs.',1;
PRINT 'PASS: current cross-table consistency (empty database does not prove seed/payment/rollback).';
