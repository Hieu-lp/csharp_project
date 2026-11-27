:ON ERROR EXIT
-- Approved handover profile: 15 airports,50 customers,15 employees,15 discounts,
-- 43 flights,129 fares,81 bookings/invoices,141 tickets/details,41 WEB legs.
-- Derived from 04/07; old scripts are NOT executed and never changed.
-- Future flights are relative to setup day; historical statistics cover prior year.
-- Full-used discount has one real linked demo booking; all counters reconcile.
-- Setup/seed by DBA is distinct from automated CRUD/payment testing by a writer.
USE [FlightBooking_Final];
GO
SET ANSI_NULLS ON;
SET ANSI_PADDING ON;
SET ANSI_WARNINGS ON;
SET ARITHABORT ON;
SET CONCAT_NULL_YIELDS_NULL ON;
SET QUOTED_IDENTIFIER ON;
SET NUMERIC_ROUNDABORT OFF;
SET NOCOUNT ON;
SET XACT_ABORT ON;
IF DB_NAME()<>N'FlightBooking_Final' OR CONVERT(nvarchar(128),SERVERPROPERTY('ServerName'))<>N'$(EXPECTED_SERVER)'
 THROW 51250,'Wrong server/database for Final seed.',1;
IF N'$(SEED_APPROVAL)'<>N'APPROVE_SEED_FLIGHTBOOKING_FINAL_DEMO' OR N'$(SEED_PROFILE)'<>N'DEMO_15_50_15_15_43_129_81_141'
 THROW 51251,'Expected approved seed profile/approval is missing.',1;
IF NOT EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalDemo' AND CONVERT(nvarchar(128),value)=N'Final-20261127-v1')
 THROW 51252,'Final marker missing.',1;
DECLARE @MajorVersion int=TRY_CONVERT(int,SERVERPROPERTY('ProductMajorVersion'));
IF ISNULL(IS_SRVROLEMEMBER('sysadmin'),0)<>1 AND
 (@MajorVersion IS NULL OR @MajorVersion<=0 OR
  (@MajorVersion>=16 AND ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER PERFORMANCE STATE'),0)<>1) OR
  (@MajorVersion<16 AND ISNULL(HAS_PERMS_BY_NAME(NULL,NULL,'VIEW SERVER STATE'),0)<>1))
 THROW 51259,'Session visibility is required to verify exclusive seed use. Do not bypass this guard.',1;
BEGIN TRANSACTION;
DECLARE @LockResult int;
EXEC @LockResult=sys.sp_getapplock @Resource=N'BookingFlight.Final.Setup',@LockMode='Exclusive',@LockOwner='Transaction',@LockTimeout=0;
IF @LockResult<0 THROW 51253,'Another Final setup is running.',1;
IF EXISTS(SELECT 1 FROM sys.extended_properties WHERE class=0 AND name=N'BookingFlight.FinalSeed')
 OR EXISTS(SELECT 1 FROM sys.identity_columns c JOIN sys.tables t ON c.object_id=t.object_id WHERE t.is_ms_shipped=0 AND c.last_value IS NOT NULL)
 THROW 51254,'Database was already used or seed attempted. Stop; never overwrite/reseed.',1;
IF EXISTS(SELECT 1 FROM sys.dm_exec_sessions WHERE is_user_process=1 AND database_id=DB_ID() AND session_id<>@@SPID)
 THROW 51255,'Close applications/sessions using Final before seeding.',1;
IF EXISTS(SELECT 1 FROM dbo.[AIRPORT])
 OR EXISTS(SELECT 1 FROM dbo.[CUSTOMER])
 OR EXISTS(SELECT 1 FROM dbo.[EMPLOYEE])
 OR EXISTS(SELECT 1 FROM dbo.[DISCOUNT_CODE])
 OR EXISTS(SELECT 1 FROM dbo.[FLIGHT])
 OR EXISTS(SELECT 1 FROM dbo.[FARE_CLASS])
 OR EXISTS(SELECT 1 FROM dbo.[TICKET])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING_DETAIL])
 OR EXISTS(SELECT 1 FROM dbo.[INVOICE])
 OR EXISTS(SELECT 1 FROM dbo.[INVOICE_DETAIL])
 OR EXISTS(SELECT 1 FROM dbo.[BOOKING_WEB_REQUEST])
 THROW 51256,'All 12 tables must be empty before first seed.',1;

-- Fictional fixtures. ASCII request fields keep the System.Text.Json wire contract
-- reproducible in SQL; unused catalogue rows also exercise Vietnamese Unicode.
DECLARE @Today date=CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
DECLARE @Airports TABLE(n int PRIMARY KEY,id int);
DECLARE @Customers TABLE(n int PRIMARY KEY,id int);
DECLARE @Employees TABLE(n int PRIMARY KEY,id int);
DECLARE @Discounts TABLE(n int PRIMARY KEY,id int);
DECLARE @Flights TABLE(n int PRIMARY KEY,id int,fare int,day date);
DECLARE @Bookings TABLE(n int PRIMARY KEY,id int,flight_n int);
DECLARE @LookupKeys TABLE(booking_id int PRIMARY KEY,lookup_key varchar(64),payload varchar(max));
DECLARE @i int=1,@id int,@name nvarchar(150),@city nvarchar(100);
WHILE @i<=15
BEGIN
 SELECT @name=ten,@city=thanhPho FROM (VALUES
 (1,N'Sân bay Tân Sơn Nhất',N'TP. Hồ Chí Minh'),(2,N'Sân bay Nội Bài',N'Hà Nội'),
 (3,N'Sân bay Đà Nẵng',N'Đà Nẵng'),(4,N'Sân bay Phú Quốc',N'Kiên Giang'),
 (5,N'Sân bay Cam Ranh',N'Khánh Hòa'),(6,N'Sân bay Cát Bi',N'Hải Phòng'),
 (7,N'Sân bay Phú Bài',N'Huế'),(8,N'Sân bay Liên Khương',N'Lâm Đồng'),
 (9,N'Sân bay Vinh',N'Nghệ An'),(10,N'Sân bay Cần Thơ',N'Cần Thơ'),
 (11,N'Sân bay Buôn Ma Thuột',N'Đắk Lắk'),(12,N'Sân bay Pleiku',N'Gia Lai'),
 (13,N'Sân bay Chu Lai',N'Quảng Nam'),(14,N'Sân bay Tuy Hòa',N'Phú Yên'),
 (15,N'Sân bay Đồng Hới',N'Quảng Bình')) a(n,ten,thanhPho) WHERE n=@i;
 INSERT dbo.AIRPORT(airport_name,city) VALUES(@name,@city);
 INSERT @Airports VALUES(@i,CONVERT(int,SCOPE_IDENTITY())); SET @i+=1;
END;
SET @i=1;
WHILE @i<=50
BEGIN
 INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
 VALUES(CASE WHEN @i<=22 THEN N'Khach demo '+CONVERT(nvarchar(3),@i) ELSE N'Khách hàng giả lập '+CONVERT(nvarchar(3),@i) END,
 '09000'+RIGHT('00000'+CONVERT(varchar(5),@i),5),'final.customer.'+CONVERT(varchar(3),@i)+'@example.test',
 '1990-05-10',CASE WHEN @i%2=0 THEN N'Nu' ELSE N'Nam' END,'FINAL-C'+CONVERT(varchar(3),@i));
 INSERT @Customers VALUES(@i,CONVERT(int,SCOPE_IDENTITY())); SET @i+=1;
END;
SET @i=1;
WHILE @i<=15
BEGIN
 INSERT dbo.EMPLOYEE(full_name,phone,email,position,hire_date,status)
 VALUES(N'Nhân viên giả lập '+CONVERT(nvarchar(3),@i),'09100'+RIGHT('00000'+CONVERT(varchar(5),@i),5),
 'final.employee.'+CONVERT(varchar(3),@i)+'@example.test',N'Nhân viên bán vé','2024-01-01',CASE WHEN @i=15 THEN 'INACTIVE' ELSE 'ACTIVE' END);
 INSERT @Employees VALUES(@i,CONVERT(int,SCOPE_IDENTITY())); SET @i+=1;
END;
SET @i=1;
WHILE @i<=15
BEGIN
 INSERT dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,min_order_amount,max_discount_amount,start_date,end_date,usage_limit,used_count,status)
 VALUES('FINAL'+RIGHT('00'+CONVERT(varchar(2),@i),2),N'Mã giảm giá giả lập '+CONVERT(nvarchar(3),@i),
 CASE WHEN @i%2=0 THEN 'PERCENT' ELSE 'FIXED' END,CASE WHEN @i%2=0 THEN 10 ELSE 50000 END,
 0,CASE WHEN @i%2=0 THEN 200000 ELSE NULL END,
 CASE WHEN @i=5 THEN DATEADD(day,30,@Today) ELSE CONVERT(date,'20240101') END,
 CASE WHEN @i=4 THEN DATEADD(day,-1,@Today) ELSE DATEADD(year,3,@Today) END,
 CASE WHEN @i=3 THEN 1 ELSE 500 END,0,CASE WHEN @i=6 THEN 'INACTIVE' ELSE 'ACTIVE' END);
 INSERT @Discounts VALUES(@i,CONVERT(int,SCOPE_IDENTITY())); SET @i+=1;
END;
DECLARE @pair int,@from int,@to int,@day date,@seats int,@fare int,@price decimal(18,2);
SET @i=1;
WHILE @i<=43
BEGIN
 SET @pair=(@i-1)/2;
 SELECT @from=id FROM @Airports WHERE n=1+(@pair%3);
 SELECT @to=id FROM @Airports WHERE n=1+((@pair+1)%3);
 IF @i%2=0 BEGIN SET @id=@from; SET @from=@to; SET @to=@id; END;
 SET @day=CASE WHEN @i<=20 THEN DATEFROMPARTS(YEAR(@Today)-1+((@pair/4)%2),1+(@pair%4)*3,10+(@pair/4)*3)
               ELSE DATEADD(day,14+(@pair-10)*7,@Today) END;
 IF @i%2=0 SET @day=DATEADD(day,3,@day);
 SET @seats=CASE WHEN @i=31 THEN 2 WHEN @i=32 THEN 3 ELSE 120 END;
 INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status)
 VALUES(@from,@to,@seats,0,@seats,0,@day,'08:00',@day,'10:15',
 CASE WHEN @i BETWEEN 33 AND 40 THEN 'CANCELLED' WHEN @day<@Today THEN 'COMPLETED' ELSE 'SCHEDULED' END);
 SET @id=CONVERT(int,SCOPE_IDENTITY()); SET @price=900000+(@pair%5)*100000;
 INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) VALUES(@id,N'Economy',@price,7);
 SET @fare=CONVERT(int,SCOPE_IDENTITY()); INSERT @Flights VALUES(@i,@id,@fare,@day);
 INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) VALUES(@id,N'Premium',@price+500000,15),(@id,N'Business',@price+1500000,25);
 SET @i+=1;
END;
DECLARE @n int=1,@fn int,@flight int,@customer int,@employee int,@discount int,@code varchar(50),@qty int,
 @state varchar(20),@invoiceState varchar(20),@source varchar(10),@sub decimal(18,2),@off decimal(18,2),
 @booking int,@invoice int,@ticket int,@p int,@bookDate datetime2(0),@person int;
WHILE @n<=81
BEGIN
 SET @fn=CASE WHEN @n=81 THEN 43 ELSE (@n+1)/2 END;
 SELECT @flight=id,@fare=fare,@day=day FROM @Flights WHERE n=@fn;
 SET @person=(@fn-1)/2+1; SELECT @customer=id FROM @Customers WHERE n=@person;
 SELECT @employee=id FROM @Employees WHERE n=1+(@fn%10);
 SET @source=CASE WHEN @n%2=1 THEN 'WEB' ELSE 'COUNTER' END;
 IF @source='WEB' SET @employee=NULL;
 SET @qty=CASE WHEN @n<=60 THEN 2 ELSE 1 END;
 SET @invoiceState=CASE WHEN @fn<=20 THEN 'PAID' WHEN @fn<=32 OR @n=81 THEN 'UNPAID' ELSE 'CANCELLED' END;
 SET @state=CASE WHEN @invoiceState='CANCELLED' THEN 'CANCELLED' ELSE 'CONFIRMED' END;
 -- Historical prices deliberately differ from current catalogue prices.
 SELECT @price=price-10000 FROM dbo.FARE_CLASS WHERE fare_id=@fare;
 SET @sub=@price*@qty; SET @off=50000;
 SELECT @discount=id FROM @Discounts WHERE n=CASE WHEN @n=2 THEN 3 ELSE 1 END;
 -- A WEB round trip consumes the discount once, on its outbound leg only.
 IF @source='WEB' AND @fn%2=0 BEGIN SET @discount=NULL; SET @off=0; END;
 SET @bookDate=DATEADD(day,-7,CONVERT(datetime2(0),@day));
 IF @bookDate>CONVERT(datetime2(0),@Today) SET @bookDate=CONVERT(datetime2(0),@Today);
 INSERT dbo.BOOKING(customer_id,employee_id,flight_id,discount_id,booking_date,booking_source,ticket_quantity,discount_amount,total_amount,status)
 VALUES(@customer,@employee,@flight,@discount,@bookDate,@source,@qty,@off,@sub-@off,@state);
 SET @booking=CONVERT(int,SCOPE_IDENTITY()); INSERT @Bookings VALUES(@n,@booking,@fn);
 INSERT dbo.INVOICE(booking_id,invoice_date,total,discount_amount,tax_amount,total_amount,status)
 VALUES(@booking,@bookDate,@sub,@off,0,@sub-@off,@invoiceState);
 SET @invoice=CONVERT(int,SCOPE_IDENTITY()); SET @p=1;
 WHILE @p<=@qty
 BEGIN
  INSERT dbo.TICKET(flight_id,fare_id,seat_number,issued_at,status)
  VALUES(@flight,@fare,CONVERT(varchar(3),((CASE WHEN @n%2=1 THEN @p ELSE @qty+@p END)-1)/6+1)+CHAR(65+((CASE WHEN @n%2=1 THEN @p ELSE @qty+@p END)-1)%6),@bookDate,CASE WHEN @state='CANCELLED' THEN 'CANCELLED' ELSE 'ISSUED' END);
  SET @ticket=CONVERT(int,SCOPE_IDENTITY());
  INSERT dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
  VALUES(@booking,@ticket,N'Hanh khach '+CONVERT(nvarchar(3),@person)+N'-'+CONVERT(nvarchar(2),@p),
  'FINAL-P'+CONVERT(varchar(3),@person)+'-'+CONVERT(varchar(2),@p),@price);
  INSERT dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal) VALUES(@invoice,@ticket,@price,1,@price);
  SET @p+=1;
 END;
 SET @n+=1;
END;
UPDATE f SET tickets_sold=s.qty,seats_available=f.total_seats-s.qty,total_revenue=r.revenue
 FROM dbo.FLIGHT f CROSS APPLY(SELECT COUNT(*) qty FROM dbo.TICKET t WHERE t.flight_id=f.flight_id AND t.status='ISSUED') s
 CROSS APPLY(SELECT COALESCE(SUM(i.total_amount),0) revenue FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE b.flight_id=f.flight_id AND i.status='PAID') r;
UPDATE d SET used_count=(SELECT COUNT(*) FROM dbo.BOOKING b WHERE b.discount_id=d.discount_id AND b.status='CONFIRMED') FROM dbo.DISCOUNT_CODE d;

-- 20 genuine WEB round trips (same customer/passengers/payload) and one one-way.
-- Serialization field order matches DatVeBUS.NoiDungChongGuiLap; hash UTF-8,
-- not SQL nvarchar UTF-16. No random payload fingerprints.
DECLARE @key varchar(64),@hash char(64),@payload varchar(max),@contact varchar(max),@passengers varchar(max),
 @returnId int,@returnFlight int,@returnFare int,@returnDay date,@outBooking int;
SET @n=1;
WHILE @n<=81
BEGIN
 SELECT @outBooking=id,@fn=flight_n FROM @Bookings WHERE n=@n;
 SELECT @flight=id,@fare=fare FROM @Flights WHERE n=@fn;
 SET @returnId=NULL; SET @returnFlight=NULL; SET @returnFare=NULL; SET @returnDay=NULL;
 IF @n<81
 BEGIN
  SELECT @returnId=id FROM @Bookings WHERE n=@n+2;
  SELECT @returnFlight=id,@returnFare=fare,@returnDay=day FROM @Flights WHERE n=@fn+1;
 END;
 SELECT @qty=ticket_quantity,@customer=customer_id,@discount=discount_id FROM dbo.BOOKING WHERE booking_id=@outBooking;
 SELECT @code=code FROM dbo.DISCOUNT_CODE WHERE discount_id=@discount;
 SELECT @contact=(SELECT full_name FullName,phone Phone,email Email,CONVERT(char(10),date_of_birth,23) DateOfBirth,gender Gender,id_passport_number IdPassportNumber
 FROM dbo.CUSTOMER WHERE customer_id=@customer FOR JSON PATH,WITHOUT_ARRAY_WRAPPER);
 SELECT @passengers=(SELECT passenger_full_name FullName,passenger_id_card IdCard FROM dbo.BOOKING_DETAIL WHERE booking_id=@outBooking ORDER BY booking_detail_id FOR JSON PATH);
 SET @payload=CONCAT('{"FlightId":',@flight,',"FareId":',@fare,',"Quantity":',@qty,',"TripType":"',CASE WHEN @returnId IS NULL THEN 'OneWay' ELSE 'RoundTrip' END,
 '","ReturnDate":',CASE WHEN @returnId IS NULL THEN 'null' ELSE '"'+CONVERT(char(10),@returnDay,23)+'"' END,
 ',"ReturnFlightId":',COALESCE(CONVERT(varchar(12),@returnFlight),'null'),',"ReturnFareId":',COALESCE(CONVERT(varchar(12),@returnFare),'null'),
 ',"Customer":',@contact,',"Passengers":',@passengers,',"DiscountCode":"',@code,'"}');
 SET @key=CONVERT(varchar(64),CRYPT_GEN_RANDOM(32),2);
 SET @hash=CONVERT(char(64),HASHBYTES('SHA2_256',@payload),2);
 INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id)
 VALUES(CONVERT(char(64),HASHBYTES('SHA2_256',@key),2),@hash,@outBooking);
 INSERT @LookupKeys VALUES(@outBooking,@key,@payload);
 IF @returnId IS NOT NULL
 BEGIN
  INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id)
  VALUES(CONVERT(char(64),HASHBYTES('SHA2_256',CONVERT(varchar(64),HASHBYTES('SHA2_256',CONVERT(varchar(71),'return:'+@key)),2)),2),@hash,@returnId);
 END;
 SET @n+=4;
END;
IF (SELECT COUNT(*) FROM dbo.AIRPORT)<>15 OR (SELECT COUNT(*) FROM dbo.CUSTOMER)<>50
 OR (SELECT COUNT(*) FROM dbo.EMPLOYEE)<>15 OR (SELECT COUNT(*) FROM dbo.DISCOUNT_CODE)<>15
 OR (SELECT COUNT(*) FROM dbo.FLIGHT)<>43 OR (SELECT COUNT(*) FROM dbo.FARE_CLASS)<>129
 OR (SELECT COUNT(*) FROM dbo.BOOKING)<>81 OR (SELECT COUNT(*) FROM dbo.TICKET)<>141
 OR (SELECT COUNT(*) FROM dbo.BOOKING_DETAIL)<>141 OR (SELECT COUNT(*) FROM dbo.INVOICE)<>81
 OR (SELECT COUNT(*) FROM dbo.INVOICE_DETAIL)<>141 OR (SELECT COUNT(*) FROM dbo.BOOKING_WEB_REQUEST)<>41
 THROW 51257,'Final seed counts differ; seed must roll back.',1;
IF (SELECT COUNT(*) FROM dbo.INVOICE WHERE status='PAID')<>40
 OR (SELECT COUNT(*) FROM dbo.INVOICE WHERE status='UNPAID')<>25
 OR (SELECT COUNT(*) FROM dbo.INVOICE WHERE status='CANCELLED')<>16
 THROW 51260,'Invoice status distribution differs.',1;
EXEC sys.sp_addextendedproperty @name=N'BookingFlight.FinalSeed',@value=N'DEMO_15_50_15_15_43_129_81_141';
:r Database/Final/03_Verify_ReadOnly.sql
IF @@TRANCOUNT<>1 THROW 51258,'Unexpected transaction depth.',1;
COMMIT TRANSACTION;
PRINT 'PASS: Final seed committed; 20 round trips and one one-way WEB journey.';
SELECT booking_id,lookup_key AS PRIVATE_DEMO_LOOKUP_KEY,payload AS PRIVATE_DEMO_PAYLOAD FROM @LookupKeys ORDER BY booking_id;
