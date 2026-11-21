/* Supplemental LOCAL demo flights, Nov 2026 .. Jan 2027.
   Legacy backfill for databases created before 04_SeedDemoData.sql included
   these flights. Fresh databases already receive them from seed 04.
   Run against the database selected by the caller (no USE, no schema changes).
   Inserts only AIRPORT-linked FLIGHT and FARE_CLASS rows. No booking/customer/invoice.
   Reruns match a full route + departure/arrival date/time signature; existing
   flights/fares/counters/statuses are never updated or deleted. */
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF DB_NAME() <> N'FlightBookingDB'
    THROW 54001, N'Chon dung database local FlightBookingDB truoc khi chay seed 09.', 1;

DECLARE @NowVN DATETIME2(0) = CONVERT(DATETIME2(0),
    SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time');
DECLARE @Airports TABLE (code VARCHAR(3) PRIMARY KEY, airport_id INT NOT NULL);
DECLARE @Pairs TABLE (code VARCHAR(24) PRIMARY KEY, origin VARCHAR(3), destination VARCHAR(3),
    outbound_date DATE, return_date DATE, price DECIMAL(18,2));
DECLARE @Legs TABLE (pair_code VARCHAR(24), leg INT, origin VARCHAR(3), destination VARCHAR(3),
    departure_date DATE, departure_time TIME(0), arrival_date DATE, arrival_time TIME(0),
    price DECIMAL(18,2), flight_id INT NULL, PRIMARY KEY(pair_code,leg));

BEGIN TRY
    BEGIN TRANSACTION;
    DECLARE @LockResult INT;
    EXEC @LockResult = sys.sp_getapplock @Resource=N'FlightBooking:SeedRoundTrip09',
        @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000;
    IF @LockResult < 0 THROW 54002, N'Seed 09 dang duoc chay, hay thu lai.', 1;

    /* Resolve actual airport IDs. No hardcoded identity values. */
    INSERT @Airports(code,airport_id)
    SELECT 'SGN',MIN(airport_id) FROM dbo.AIRPORT WHERE airport_name=N'Sân bay Tân Sơn Nhất' HAVING COUNT(*)>0
    UNION ALL
    SELECT 'HAN',MIN(airport_id) FROM dbo.AIRPORT WHERE airport_name=N'Sân bay Nội Bài' HAVING COUNT(*)>0
    UNION ALL
    SELECT 'DAD',MIN(airport_id) FROM dbo.AIRPORT WHERE airport_name IN (N'San bay Da Nang',N'Sân bay Đà Nẵng') HAVING COUNT(*)>0;
    IF (SELECT COUNT(*) FROM @Airports)<>3 OR (SELECT COUNT(DISTINCT airport_id) FROM @Airports)<>3
        THROW 54003, N'Khong tim thay du 3 san bay SGN/HAN/DAD, chua them du lieu.', 1;

    INSERT @Pairs VALUES
        ('NOV-PAST','SGN','HAN','20261110','20261112',1100000),
        ('NOV-SGN-HAN','SGN','HAN','20261124','20261127',1200000),
        ('NOV-DAD-HAN','DAD','HAN','20261125','20261128',950000),
        ('NOV-SGN-DAD','SGN','DAD','20261126','20261129',850000),
        ('DEC-SGN-HAN','SGN','HAN','20261205','20261208',1250000),
        ('DEC-DAD-HAN','DAD','HAN','20261212','20261215',1000000),
        ('DEC-SGN-DAD','SGN','DAD','20261220','20261223',900000),
        ('DEC-JAN','SGN','HAN','20261229','20270103',1500000),
        ('JAN-SGN-HAN','SGN','HAN','20270110','20270113',1300000),
        ('JAN-DAD-HAN','DAD','HAN','20270117','20270120',1050000),
        ('JAN-SGN-DAD','SGN','DAD','20270124','20270127',950000);

    INSERT @Legs(pair_code,leg,origin,destination,departure_date,departure_time,arrival_date,arrival_time,price)
    SELECT code,1,origin,destination,outbound_date,'07:15',outbound_date,'09:25',price FROM @Pairs
    UNION ALL
    SELECT code,2,destination,origin,return_date,'13:40',return_date,'15:50',price+50000 FROM @Pairs;

    DECLARE @Pair VARCHAR(24),@Leg INT,@From INT,@To INT,@DepDate DATE,@DepTime TIME(0),
        @ArrDate DATE,@ArrTime TIME(0),@Price DECIMAL(18,2),@FlightId INT,@ArrivesAt DATETIME2(0);
    DECLARE @InsertedFlights INT=0,@InsertedFares INT=0;
    DECLARE leg_cursor CURSOR LOCAL FAST_FORWARD FOR
        SELECT l.pair_code,l.leg,a.airport_id,b.airport_id,l.departure_date,l.departure_time,
            l.arrival_date,l.arrival_time,l.price
        FROM @Legs l JOIN @Airports a ON a.code=l.origin JOIN @Airports b ON b.code=l.destination
        ORDER BY l.pair_code,l.leg;
    OPEN leg_cursor;
    FETCH NEXT FROM leg_cursor INTO @Pair,@Leg,@From,@To,@DepDate,@DepTime,@ArrDate,@ArrTime,@Price;
    WHILE @@FETCH_STATUS=0
    BEGIN
        SET @FlightId=NULL;
        SELECT @FlightId=MIN(flight_id) FROM dbo.FLIGHT WITH (UPDLOCK,HOLDLOCK)
        WHERE departure_airport_id=@From AND arrival_airport_id=@To
            AND departure_date=@DepDate AND departure_time=@DepTime
            AND arrival_date=@ArrDate AND arrival_time=@ArrTime;
        IF @FlightId IS NULL
        BEGIN
            SET @ArrivesAt=DATEADD(MINUTE,DATEPART(HOUR,@ArrTime)*60+DATEPART(MINUTE,@ArrTime),CONVERT(DATETIME2(0),@ArrDate));
            INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,
                seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status)
            VALUES (@From,@To,120,0,120,0,@DepDate,@DepTime,@ArrDate,@ArrTime,
                CASE WHEN @ArrivesAt<=@NowVN THEN 'COMPLETED' ELSE 'SCHEDULED' END);
            SET @FlightId=CONVERT(INT,SCOPE_IDENTITY());
            SET @InsertedFlights+=1;
        END;
        UPDATE @Legs SET flight_id=@FlightId WHERE pair_code=@Pair AND leg=@Leg;
        INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg)
        SELECT @FlightId,x.class_name,x.price,x.baggage
        FROM (VALUES (N'Economy',@Price,7),(N'Business',@Price+900000,20)) x(class_name,price,baggage)
        WHERE NOT EXISTS (SELECT 1 FROM dbo.FARE_CLASS WITH (UPDLOCK,HOLDLOCK)
            WHERE flight_id=@FlightId AND class_name=x.class_name);
        SET @InsertedFares+=@@ROWCOUNT;
        FETCH NEXT FROM leg_cursor INTO @Pair,@Leg,@From,@To,@DepDate,@DepTime,@ArrDate,@ArrTime,@Price;
    END;
    CLOSE leg_cursor;
    DEALLOCATE leg_cursor;

    IF EXISTS (SELECT 1 FROM @Legs l JOIN dbo.FLIGHT f ON f.flight_id=l.flight_id
        WHERE f.total_seats<>f.tickets_sold+f.seats_available OR f.total_revenue<0)
        THROW 54004, N'So lieu ghe/doanh thu khong hop le.',1;
    COMMIT TRANSACTION;

    SELECT DB_NAME() AS DatabaseName,@NowVN AS CheckedAtVietnamTime,
        @InsertedFlights AS InsertedFlights,@InsertedFares AS InsertedFares;
    SELECT p.code AS PairCode, a.airport_name AS Origin,b.airport_name AS Destination,
        f.flight_id AS OutboundFlightId,f.departure_date AS OutboundDate,f.departure_time AS OutboundDeparture,
        f.arrival_time AS OutboundArrival,f.status AS OutboundStatus,
        r.flight_id AS ReturnFlightId,r.departure_date AS ReturnDate,r.departure_time AS ReturnDeparture,
        r.arrival_time AS ReturnArrival,r.status AS ReturnStatus,
        f.seats_available AS OutboundSeats,r.seats_available AS ReturnSeats
    FROM @Pairs p JOIN @Legs o ON o.pair_code=p.code AND o.leg=1
    JOIN @Legs v ON v.pair_code=p.code AND v.leg=2
    JOIN dbo.FLIGHT f ON f.flight_id=o.flight_id JOIN dbo.FLIGHT r ON r.flight_id=v.flight_id
    JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id
    JOIN dbo.AIRPORT b ON b.airport_id=f.arrival_airport_id
    ORDER BY f.departure_date,p.code;
END TRY
BEGIN CATCH
    IF XACT_STATE()<>0 ROLLBACK TRANSACTION;
    THROW;
END CATCH;
