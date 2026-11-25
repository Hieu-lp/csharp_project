:ON ERROR EXIT
-- Run with Integrated Security in the real BookingFlightTest Windows process.
-- SELECT only: includes effective token/group permissions, not EXECUTE AS.
SET NOCOUNT ON;
IF CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'DESKTOP-1MMSBHT'
 OR DB_NAME()<>N'FlightBooking_Hieu_ManagementTests'
    THROW 51420,'Wrong SQL server/database.',1;
IF ORIGINAL_LOGIN()<>N'DESKTOP-1MMSBHT\BookingFlightTest'
 OR SUSER_SNAME()<>N'DESKTOP-1MMSBHT\BookingFlightTest'
    THROW 51421,'Not the actual approved test login.',1;

SELECT ORIGINAL_LOGIN() OriginalLogin,SUSER_SNAME() EffectiveLogin,DB_NAME() DatabaseName,
 IS_SRVROLEMEMBER('sysadmin') Sysadmin,IS_SRVROLEMEMBER('dbcreator') Dbcreator,
 IS_MEMBER('db_owner') DbOwner,
 HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL') HasControl,
 HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER') HasAlter,
 HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER PERFORMANCE STATE') ViewSessions,
 HAS_DBACCESS(N'FlightBookingDB') MainAccess;
SELECT name,type,usage FROM sys.login_token ORDER BY name;
SELECT name,type,usage FROM sys.user_token ORDER BY name;
SELECT permission_name FROM sys.fn_my_permissions(NULL,'SERVER') ORDER BY permission_name;
SELECT permission_name FROM sys.fn_my_permissions(NULL,'DATABASE') ORDER BY permission_name;
SELECT name,IS_SRVROLEMEMBER(name) IsMember FROM sys.server_principals
 WHERE type='R' ORDER BY name;
SELECT name,IS_MEMBER(name) IsMember FROM sys.database_principals
 WHERE type='R' ORDER BY name;
SELECT t.name TableName,p.subentity_name ColumnName,p.permission_name
 FROM sys.tables t CROSS APPLY sys.fn_my_permissions(N'dbo.'+QUOTENAME(t.name),'OBJECT') p
 ORDER BY t.name,p.permission_name;

IF ISNULL(IS_SRVROLEMEMBER('sysadmin'),1)<>0
 OR ISNULL(IS_SRVROLEMEMBER('dbcreator'),1)<>0
 OR ISNULL(IS_MEMBER('db_owner'),1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','CONTROL'),1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(DB_NAME(),'DATABASE','ALTER'),1)<>0
 OR ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER PERFORMANCE STATE'),0)<>1
 OR ISNULL(HAS_DBACCESS(N'FlightBookingDB'),1)<>0
    THROW 51422,'Writer profile, session visibility or main isolation failed.',1;
IF EXISTS(SELECT 1 FROM sys.server_principals WHERE type='R' AND name<>N'public'
          AND ISNULL(IS_SRVROLEMEMBER(name),1)<>0)
 OR EXISTS(SELECT 1 FROM sys.database_principals WHERE type='R'
           AND name NOT IN (N'public',N'FlightBookingTestWriter') AND ISNULL(IS_MEMBER(name),1)<>0)
 OR ISNULL(IS_MEMBER(N'FlightBookingTestWriter'),0)<>1
    THROW 51423,'Unexpected or unverifiable role membership.',1;
-- SQL 17's own hierarchy proves the three implied metadata permissions below.
-- VIEW DEFINITION covers both definition subsets; the approved server permission
-- covers VIEW DATABASE PERFORMANCE STATE. These are not additional GRANTs.
IF NOT EXISTS(SELECT 1 FROM sys.fn_builtin_permissions(DEFAULT) WHERE class_desc='DATABASE'
              AND permission_name='VIEW SECURITY DEFINITION' AND covering_permission_name='VIEW DEFINITION')
 OR NOT EXISTS(SELECT 1 FROM sys.fn_builtin_permissions(DEFAULT) WHERE class_desc='DATABASE'
               AND permission_name='VIEW PERFORMANCE DEFINITION' AND covering_permission_name='VIEW DEFINITION')
 OR NOT EXISTS(SELECT 1 FROM sys.fn_builtin_permissions(DEFAULT) WHERE class_desc='DATABASE'
               AND permission_name='VIEW DATABASE PERFORMANCE STATE' AND parent_class_desc='SERVER'
               AND parent_covering_permission_name='VIEW SERVER PERFORMANCE STATE')
    THROW 51426,'Permission hierarchy differs: review before writes.',1;
-- Standard public metadata visibility is measured above; no additional server grant.
IF EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'SERVER')
          WHERE permission_name NOT IN (N'CONNECT SQL',N'VIEW ANY DATABASE',N'VIEW SERVER PERFORMANCE STATE'))
 OR EXISTS(SELECT 1 FROM sys.fn_my_permissions(NULL,'DATABASE')
           WHERE permission_name NOT IN (N'CONNECT',N'VIEW DEFINITION',N'VIEW SECURITY DEFINITION',
               N'VIEW PERFORMANCE DEFINITION',N'VIEW DATABASE PERFORMANCE STATE',
               N'VIEW ANY COLUMN MASTER KEY DEFINITION',N'VIEW ANY COLUMN ENCRYPTION KEY DEFINITION'))
    THROW 51424,'Unexpected inherited/effective server or database permission.',1;
IF (SELECT COUNT(*) FROM sys.tables)<>12
 OR EXISTS(SELECT 1 FROM sys.tables t WHERE SCHEMA_NAME(t.schema_id)<>N'dbo'
           OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','SELECT'),0)<>1
           OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','INSERT'),0)<>1
           OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','UPDATE'),0)<>1
           OR ISNULL(HAS_PERMS_BY_NAME(N'dbo.'+QUOTENAME(t.name),'OBJECT','DELETE'),0)<>1)
 OR EXISTS(SELECT 1 FROM sys.tables t
           CROSS APPLY sys.fn_my_permissions(N'dbo.'+QUOTENAME(t.name),'OBJECT') p
           WHERE p.permission_name NOT IN (N'SELECT',N'INSERT',N'UPDATE',N'DELETE',N'VIEW DEFINITION'))
    THROW 51425,'Missing DML or unexpected object permission.',1;
SELECT N'PASS: actual writer identity, token/roles, effective permissions and main isolation' Result;
