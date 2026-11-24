"""Generate reviewable SQL only. No database driver, connection or execution."""
from pathlib import Path
import hashlib
import json
import re
import sys

ROOT = Path(__file__).resolve().parents[2]
OUT = ROOT / 'Database/TestPlan'
TARGET = 'FlightBooking_Hieu_ManagementTests'
APPROVAL = 'APPROVE_WRITES_TO_FLIGHTBOOKING_HIEU_MANAGEMENTTESTS'
FILES = ['02_CreateTables.sql', '03_Constraints_Indexes.sql', '08_BookingWebRequest.sql']
CHECK_ONLY = '--check' in sys.argv
SET_OPTIONS = """SET ANSI_NULLS ON;
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
"""
SESSION_ACCESS_PREDICATE = "MajorVersion IS NOT NULL AND MajorVersion>0 AND (IsSysadmin=1 OR (MajorVersion>=16 AND CanViewPerformance=1) OR (MajorVersion<16 AND CanViewState=1))"
WRITER_PROFILE_PREDICATE = "IsSysadmin=0 AND IsDbcreator=0 AND IsDbOwner=0 AND HasControl=0 AND HasAlter=0"
sources = {name: (ROOT / 'Database' / name).read_text(encoding='utf-8-sig') for name in FILES}
manifest = {name: hashlib.sha256((ROOT / 'Database' / name).read_bytes()).hexdigest().upper() for name in FILES}
fingerprint = hashlib.sha256(json.dumps(manifest, sort_keys=True).encode()).hexdigest().upper()

def save(path, text):
    data = text.replace('\r\n', '\n').replace('\n', '\r\n').encode('utf-8-sig')
    if CHECK_ONLY:
        assert path.exists() and path.read_bytes() == data, 'Generated file differs: ' + str(path)
        return
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(data)

def literal(value):
    return "N'" + value.replace("'", "''") + "'"

def values(rows):
    return ',\n'.join('        (' + ','.join('NULL' if x is None else str(x) if isinstance(x, int) else literal(x) for x in row) + ')' for row in rows)

tables, columns = [], []
for name in ['02_CreateTables.sql', '08_BookingWebRequest.sql']:
    for match in re.finditer(r'^([ \t]*)CREATE TABLE dbo\.(\w+)\s*\((.*?)^\1\);', sources[name], re.M | re.S):
        table, body = match.group(2), match.group(3)
        tables.append(table)
        for line in body.splitlines():
            column = re.match(r'\s*(\w+)\s+(INT|NVARCHAR|VARCHAR|CHAR|DATETIME2|TIME|DATE|DECIMAL)(?:\(([^)]+)\))?\s+(.*)', line)
            if not column:
                continue
            col, kind, size, rest = column.groups()
            length = precision = scale = None
            if kind in ('NVARCHAR', 'VARCHAR', 'CHAR'):
                length = int(size) * (2 if kind == 'NVARCHAR' else 1)
            elif kind == 'DECIMAL':
                precision, scale = map(int, size.split(','))
            elif kind in ('TIME', 'DATETIME2'):
                scale = int(size or 7)
            columns.append((table, col, kind.lower(), length, precision, scale, 0 if 'NOT NULL' in rest else 1, 1 if 'IDENTITY' in rest else 0))
assert len(tables) == 12 and len(columns) == 86, (tables, len(columns))

objects = []
for source in sources.values():
    for match in re.finditer(r'CONSTRAINT\s+(\w+)\s+(PRIMARY KEY|FOREIGN KEY|UNIQUE|CHECK|DEFAULT)', source):
        objects.append((match.group(1), {'PRIMARY KEY':'PK','FOREIGN KEY':'F','UNIQUE':'UQ','CHECK':'C','DEFAULT':'D'}[match.group(2)]))
assert sum(kind == 'F' for _, kind in objects) == 15
indexes = [(name, 1) for name, kind in objects if kind in ('PK', 'UQ')]
for match in re.finditer(r'CREATE\s+(UNIQUE\s+)?(?:NONCLUSTERED\s+)?INDEX\s+(\w+)', sources['03_Constraints_Indexes.sql']):
    indexes.append((match.group(2), int(bool(match.group(1)))))

preflight = f"""/* Generated from repository schema; SELECT only. Parameters supplied by caller.
   Target: {TARGET}. Missing approval is a blocker, never a reason to skip checks. */
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
{values([(c[0],c[1]) for c in columns if c[0] != 'BOOKING_WEB_REQUEST'])}
) v(TableName,ColumnName)), MainTables(Name) AS (SELECT Name FROM (VALUES
{values([(t,) for t in tables if t != 'BOOKING_WEB_REQUEST'])}
) v(Name)), ExpectedTables(Name) AS (SELECT Name FROM (VALUES
{values([(t,) for t in tables])}
) v(Name)), ExpectedColumns(TableName,ColumnName,TypeName,MaxLength,PrecisionValue,ScaleValue,NullableValue,IdentityValue) AS (
SELECT * FROM (VALUES
{values(columns)}
) v(TableName,ColumnName,TypeName,MaxLength,PrecisionValue,ScaleValue,NullableValue,IdentityValue)),
ExpectedObjects(Name,Kind) AS (SELECT * FROM (VALUES
{values(objects)}
) v(Name,Kind)), ExpectedIndexes(Name,UniqueValue) AS (SELECT * FROM (VALUES
{values(indexes)}
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
UNION ALL SELECT N'database_identity', CASE WHEN DB_NAME()=N'{TARGET}' AND DB_NAME()<>N'FlightBookingDB' AND DB_NAME() LIKE N'%[_]ManagementTests' THEN 1 ELSE 0 END, N'Exact test database only; main database is forbidden.'
UNION ALL SELECT N'write_approval', CASE WHEN @WriteApproval=N'{APPROVAL}' THEN 1 ELSE 0 END, N'Human write approval must be explicitly supplied after review.'
UNION ALL SELECT N'writer_profile', CASE WHEN EXISTS(SELECT 1 FROM WriterProfile WHERE {WRITER_PROFILE_PREDICATE}) THEN 1 ELSE 0 END, N'Use a separately approved least-privilege writer, never sysadmin/dbcreator/db_owner/CONTROL/ALTER.'
UNION ALL SELECT N'main_write_isolation', CASE WHEN HAS_DBACCESS(N'FlightBookingDB')=0 THEN 1 WHEN HAS_DBACCESS(N'FlightBookingDB')=1
 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','CONTROL'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','ALTER'),1)=0
 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','INSERT'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','UPDATE'),1)=0 AND ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB','DATABASE','DELETE'),1)=0
 AND NOT EXISTS(SELECT 1 FROM MainTables WHERE ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','INSERT'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','UPDATE'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','DELETE'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','ALTER'),1)<>0 OR ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+Name,'OBJECT','CONTROL'),1)<>0)
 AND NOT EXISTS(SELECT 1 FROM MainColumns WHERE ISNULL(HAS_PERMS_BY_NAME(N'FlightBookingDB.dbo.'+TableName,'OBJECT','UPDATE',ColumnName,'COLUMN'),1)<>0) THEN 1 ELSE 0 END, N'No effective DML/ALTER/CONTROL or column UPDATE on the verified main schema; unknown permissions block writes.'
UNION ALL SELECT N'online_read_write', CASE WHEN DATABASEPROPERTYEX(DB_NAME(),'Status')=N'ONLINE' AND DATABASEPROPERTYEX(DB_NAME(),'Updateability')=N'READ_WRITE' THEN 1 ELSE 0 END, N'Test database must be online and writable.'
UNION ALL SELECT N'test_only_marker', CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.TestOnly' AND CONVERT(nvarchar(128),value)=N'HieuThinh-W7-20261124') THEN 1 ELSE 0 END, N'Database must have been created by the approved test-only setup.'
UNION ALL SELECT N'source_manifest', CASE WHEN EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.SchemaSource' AND CONVERT(nvarchar(128),value)=N'{fingerprint}') THEN 1 ELSE 0 END, N'Schema must correspond to the reviewed repository snapshot.'
UNION ALL SELECT N'tables', CASE WHEN (SELECT COUNT(*) FROM sys.tables)=12 AND NOT EXISTS(SELECT 1 FROM ExpectedTables e WHERE OBJECT_ID(N'dbo.'+e.Name,'U') IS NULL) THEN 1 ELSE 0 END, N'All 12 repository tables, including EMPLOYEE and BOOKING_WEB_REQUEST, are required.'
UNION ALL SELECT N'columns', CASE WHEN NOT EXISTS(SELECT 1 FROM MissingColumns) AND (SELECT COUNT(*) FROM sys.columns c JOIN sys.tables t ON t.object_id=c.object_id)= {len(columns)} THEN 1 ELSE 0 END, N'Column names, SQL types, lengths/scales, nullability and identities must match source.'
UNION ALL SELECT N'constraints', CASE WHEN NOT EXISTS(SELECT 1 FROM ExpectedObjects e LEFT JOIN sys.objects o ON o.name=e.Name AND o.type=e.Kind AND o.schema_id=SCHEMA_ID(N'dbo') WHERE o.object_id IS NULL) THEN 1 ELSE 0 END, N'All named PK/UQ/FK/CHECK/DEFAULT constraints are required.'
UNION ALL SELECT N'fk_check_trust', CASE WHEN NOT EXISTS(SELECT 1 FROM sys.foreign_keys WHERE is_disabled=1 OR is_not_trusted=1) AND NOT EXISTS(SELECT 1 FROM sys.check_constraints WHERE is_disabled=1 OR is_not_trusted=1) THEN 1 ELSE 0 END, N'Foreign keys and CHECK constraints must be enabled and trusted.'
UNION ALL SELECT N'indexes', CASE WHEN NOT EXISTS(SELECT 1 FROM ExpectedIndexes e LEFT JOIN sys.indexes i ON i.name=e.Name AND i.is_unique=e.UniqueValue AND i.is_disabled=0 WHERE i.index_id IS NULL) THEN 1 ELSE 0 END, N'Repository indexes, including enabled unique indexes, are required.'
UNION ALL SELECT N'active_seat_index', CASE WHEN EXISTS(SELECT 1 FROM sys.indexes i WHERE i.object_id=OBJECT_ID(N'dbo.TICKET') AND i.name=N'UX_TICKET_ActiveSeat' AND i.is_unique=1 AND i.has_filter=1 AND i.is_disabled=0 AND REPLACE(REPLACE(REPLACE(REPLACE(REPLACE(i.filter_definition,N'(',N''),N')',N''),N'[',N''),N']',N''),N' ',N'')=N'status=''ISSUED''' AND (SELECT STRING_AGG(c.name,N',') WITHIN GROUP(ORDER BY ic.key_ordinal) FROM sys.index_columns ic JOIN sys.columns c ON c.object_id=ic.object_id AND c.column_id=ic.column_id WHERE ic.object_id=i.object_id AND ic.index_id=i.index_id AND ic.key_ordinal>0)=N'flight_id,seat_number') THEN 1 ELSE 0 END, N'Unique active-seat key/filter must protect flight_id and seat_number.'
UNION ALL SELECT N'permissions', CASE WHEN HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','VIEW DEFINITION')=1 AND NOT EXISTS(SELECT 1 FROM ExpectedTables WHERE ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','SELECT'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','INSERT'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','UPDATE'),0)<>1 OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+Name,'OBJECT','DELETE'),0)<>1) THEN 1 ELSE 0 END, N'Test principal needs schema visibility and DML rights on test tables.'
UNION ALL SELECT N'exclusive_use', CASE WHEN EXISTS(SELECT 1 FROM SessionAccess WHERE {SESSION_ACCESS_PREDICATE}) AND NOT EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1 AND database_id=DB_ID() AND session_id<>@@SPID) THEN 1 ELSE 0 END, N'Known version required: SQL 16+ needs VIEW SERVER PERFORMANCE STATE; older versions need VIEW SERVER STATE. Other test sessions are forbidden.'
)
SELECT CheckName,CONVERT(bit,Passed) Passed,Detail FROM Checks;
"""
save(ROOT / 'tests/Shared/SqlTestPreflight.sql', preflight)
blocking_preflight = preflight.replace(
    'SELECT CheckName,CONVERT(bit,Passed) Passed,Detail FROM Checks;',
    "SELECT @Failed=STRING_AGG(CheckName,N',') FROM Checks WHERE Passed=0;\nIF @Failed IS NOT NULL THROW 51110,@Failed,1;")
save(OUT / '03_Preflight.sql', ':ON ERROR EXIT\n-- SELECT only; connect directly to the exact test database after creation.\nDECLARE @ExpectedServer nvarchar(128)=N\'$(EXPECTED_SERVER)\';\nDECLARE @WriteApproval nvarchar(128)=N\'$(WRITE_APPROVAL)\';\nDECLARE @Failed nvarchar(2048);\n' + blocking_preflight + "\nSELECT N'All 16 write-preflight checks passed; no SQL writes performed.' AS Result;\n")

def guard():
    return f"""IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'{TARGET}' THROW 51101,'Wrong database; test target only.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Create/schema approval missing.',1;
IF CONVERT(nvarchar(128),SESSION_CONTEXT(N'BookingFlight.SetupTarget'))<>N'{TARGET}' OR SESSION_CONTEXT(N'BookingFlight.SetupTarget') IS NULL THROW 51103,'This session did not create the new test database.',1;
"""

setup = f""":ON ERROR EXIT
/* REVIEW ONLY. Do not execute until the user approves creation AND this schema snapshot.
   Required sqlcmd variables: EXPECTED_SERVER, CREATE_APPROVAL. No default approval.
   Source: 02,03,08 only. Main DB seed/demo scripts are deliberately excluded.
   Stage 1B: explicit SET options prevent SQL 1934. Use sqlcmd -I as well.
   Original executed version: History/01_CreateAndSchema-6EE11AF9.sql. */
SET NOCOUNT ON;
{SET_OPTIONS}
EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=NULL;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)' THROW 51100,'Wrong SQL Server.',1;
IF DB_NAME()<>N'master' THROW 51104,'Connect to master for creation.',1;
IF N'$(CREATE_APPROVAL)'<>N'APPROVE_CREATE_FLIGHTBOOKING_HIEU_TESTS' THROW 51102,'Creation approval missing.',1;
IF DB_ID(N'{TARGET}') IS NOT NULL THROW 51105,'Database already exists; stop without changing it.',1;
IF ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'CREATE ANY DATABASE'),0)<>1 THROW 51106,'CREATE DATABASE permission is missing.',1;
EXEC(N'CREATE DATABASE [{TARGET}]');
EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=N'{TARGET}';
GO
USE [{TARGET}];
GO
"""
batch_count = 0
for name in FILES:
    source = sources[name]
    if re.search(r'^\s*(DROP|ALTER|DELETE|TRUNCATE|INSERT|UPDATE|EXEC(?:UTE)?)\b', source, re.M | re.I):
        raise ValueError('Unexpected operation in schema-only source: '+name)
    source, replacements = re.subn(r'^\s*USE\s+FlightBookingDB;[ \t]*$', '', source, flags=re.M | re.I)
    assert replacements == 1, (name, replacements)
    assert not re.search(r'^\s*(USE\b|:)', source, re.M | re.I), name
    for batch in re.split(r'^\s*GO\s*$', source, flags=re.M | re.I):
        if not batch.strip(): continue
        batch_count += 1
        setup += '\n-- Source: '+name+'; guarded batch '+str(batch_count)+'\n' + guard() + batch.strip() + '\nGO\n'
setup += '\n' + guard() + f"""EXEC sys.sp_addextendedproperty @name=N'BookingFlight.TestOnly',@value=N'HieuThinh-W7-20261124';
EXEC sys.sp_addextendedproperty @name=N'BookingFlight.SchemaSource',@value=N'{fingerprint}';
EXEC sys.sp_set_session_context @key=N'BookingFlight.SetupTarget',@value=NULL;
SELECT DB_NAME() AS PreparedTestDatabase;
GO
"""
assert not re.search(r'USE\s+\[?FlightBookingDB\]?\s*;', setup, re.I)
save(OUT / '01_CreateAndSchema.sql', setup)
fixture_guard = f"""IF DB_NAME()<>N'{TARGET}' OR DB_NAME()=N'FlightBookingDB' THROW 51111,'Fixture target changed; stop.',1;
"""
fixture = f""":ON ERROR EXIT
/* REVIEW ONLY. Entirely fake fixtures; never copies rows from any other database.
   Run only after approval, new schema creation, and closing all other test sessions.
   Required variables: EXPECTED_SERVER, WRITE_APPROVAL, FIXTURE_APPROVAL. */
SET NOCOUNT ON;
{SET_OPTIONS}
DECLARE @ExpectedServer nvarchar(128)=N'$(EXPECTED_SERVER)';
DECLARE @WriteApproval nvarchar(128)=N'$(WRITE_APPROVAL)';
DECLARE @Failed nvarchar(2048);
IF N'$(FIXTURE_APPROVAL)'<>N'APPROVE_FAKE_FIXTURES_FLIGHTBOOKING_HIEU_TESTS' THROW 51112,'Fixture approval missing.',1;
{blocking_preflight}
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
 {fixture_guard}
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
 {fixture_guard}
 INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
 VALUES(N'Khach TEST ONLY','0900000000','hieu-thinh-fixture@example.test','20000101',N'Nam','TEST-BOOKER-ONLY');
 SET @Customer=CONVERT(int,SCOPE_IDENTITY());
 INSERT dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,min_order_amount,max_discount_amount,start_date,end_date,usage_limit,used_count,status)
 VALUES('HIEUTHINH_TEST_ONLY',N'Ma gia chi dung DB test','FIXED',100000,0,NULL,DATEADD(day,-1,@Today),DATEADD(day,60,@Today),10,1,'ACTIVE');
 SET @Discount=CONVERT(int,SCOPE_IDENTITY());

 -- Phase C: referenced, unpaid booking; all dependent rows/counters commit together.
 {fixture_guard}
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
 {fixture_guard}
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
"""
save(OUT / '02_FakeFixtures.sql', fixture)
save(OUT / 'source-manifest.json', json.dumps({'target':TARGET,'source_sha256':manifest,'schema_fingerprint':fingerprint,'tables':tables,'column_count':len(columns),'constraint_count':len(objects),'guarded_source_batches':batch_count}, indent=2)+'\n')
print(f'{"Verified existing files" if CHECK_ONLY else "Prepared only"}: {len(tables)} tables, {len(columns)} columns, {len(objects)} constraints, {batch_count} guarded source batches. No SQL executed.')
