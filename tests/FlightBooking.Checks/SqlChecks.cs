using System.Text.RegularExpressions;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

internal static class SqlChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var connectionString = Environment.GetEnvironmentVariable("FLIGHTBOOKING_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set FLIGHTBOOKING_TEST_CONNECTION first.");
        var settings = new SqlConnectionStringBuilder(connectionString);
        if (!settings.InitialCatalog.EndsWith("_BookingTests", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing writes: use a dedicated database ending in _BookingTests.");
        var connection = new MyConnection(connectionString);
        int Scalar(string sql, params (string, object)[] values)
        {
            using var c = connection.Open();
            using var command = new SqlCommand(sql, c);
            foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value);
            return Convert.ToInt32(command.ExecuteScalar());
        }
        // Apply only to the explicitly selected test database. No database is created/dropped.
        var root = new DirectoryInfo(AppContext.BaseDirectory);
        while (root is not null && !File.Exists(Path.Combine(root.FullName, "FlightBookingSystem.slnx"))) root = root.Parent;
        if (root is null) throw new InvalidOperationException("Run from this repository.");
        foreach (var file in new[] { "02_CreateTables.sql", "03_Constraints_Indexes.sql", "08_BookingWebRequest.sql" })
        {
            if (file == "02_CreateTables.sql" && Scalar("SELECT COUNT(*) FROM sys.tables WHERE name='AIRPORT'") > 0) continue;
            if (file == "03_Constraints_Indexes.sql" && Scalar("SELECT COUNT(*) FROM sys.indexes WHERE name='UX_TICKET_ActiveSeat'") > 0) continue;
            var script = File.ReadAllText(Path.Combine(root.FullName, "Database", file));
            script = Regex.Replace(script, @"USE FlightBookingDB;", "", RegexOptions.IgnoreCase);
            foreach (var batch in Regex.Split(script, @"^\s*GO\s*$", RegexOptions.Multiline | RegexOptions.IgnoreCase))
            {
                if (string.IsNullOrWhiteSpace(batch)) continue;
                using var c = connection.Open(); using var cmd = new SqlCommand(batch, c); cmd.ExecuteNonQuery();
            }
        }
        var label = "CHECK-" + Guid.NewGuid().ToString("N")[..10].ToUpperInvariant();
        var departure = Scalar("INSERT dbo.AIRPORT(airport_name,city) OUTPUT INSERTED.airport_id VALUES (@name,N'Test A')", ("@name", label));
        var arrival = Scalar("INSERT dbo.AIRPORT(airport_name,city) OUTPUT INSERTED.airport_id VALUES (@name,N'Test B')", ("@name", label));
        (int Flight, int Fare) Fixture(int seats)
        {
            var f = Scalar("""
                INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,
                    departure_date,departure_time,arrival_date,arrival_time,status)
                OUTPUT INSERTED.flight_id VALUES (@departure,@arrival,@seats,0,@seats,0,@date,'08:30',@date,'10:45','SCHEDULED')
                """, ("@departure", departure), ("@arrival", arrival), ("@seats", seats), ("@date", DateTime.UtcNow.Date.AddDays(3)));
            var fare = Scalar("INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) OUTPUT INSERTED.fare_id VALUES (@id,N'Test',100000,7)", ("@id", f));
            return (f, fare);
        }
        BookingRequest Request((int Flight, int Fare) f, int quantity = 1, string? code = null) => new()
        {
            FlightId = f.Flight, FareId = f.Fare, Quantity = quantity, DiscountCode = code, ExpectedTotal = quantity * 100000 - (code is null ? 0 : 10000),
            Customer = new() { FullName = "Khách kiểm thử", Email = label + "@example.test", Phone = "0900000000", DateOfBirth = new(2000,1,1), Gender = "Khác", IdPassportNumber = "TEST-BOOKER-001" },
            Passengers = Enumerable.Range(1, quantity).Select(i => new PassengerInput { FullName = "Hành khách " + i, IdCard = label + "-" + i }).ToList()
        };
        void Reject(Action action, string name)
        {
            try { action(); } catch (BookingException) { check(true, name); return; }
            throw new Exception("FAIL: " + name);
        }
        var bus = new BookingBUS(connectionString);
        var fixture = Fixture(2);
        var code = label;
        var discountId = Scalar("""
            INSERT dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,min_order_amount,start_date,end_date,usage_limit,used_count,status)
            OUTPUT INSERTED.discount_id VALUES (@code,N'Test','FIXED',10000,0,@start,@end,1,0,'ACTIVE')
            """, ("@code", code), ("@start", DateTime.UtcNow.Date.AddDays(-1)), ("@end", DateTime.UtcNow.Date.AddDays(10)));
        var request = Request(fixture, 2, code);
        var key = BookingBUS.NewAccessKey();
        var id = bus.Create(request, key);
        var booking = bus.Get(id, key)!;
        check(booking.Details.Count == 2 && booking.TotalAmount == 190000 && booking.InvoiceStatus == "UNPAID", "SQL create booking/tickets/invoice and discount");
        check(booking.Details.Select(d => d.SeatNumber).Distinct().Count() == 2, "SQL distinct seats");
        check(Scalar("SELECT total_revenue FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 0, "SQL unpaid booking does not record revenue");
        check(bus.Create(request, key) == id, "SQL repeated request returns same booking");
        request.Passengers[0].FullName = "Changed";
        Reject(() => bus.Create(request, key), "SQL same key different payload rejected");
        check(bus.Get(id, BookingBUS.NewAccessKey()) is null, "SQL wrong key cannot read booking");
        Reject(() => bus.Cancel(id, BookingBUS.NewAccessKey()), "SQL wrong key cannot cancel");
        Reject(() => bus.Create(Request(fixture), BookingBUS.NewAccessKey()), "SQL full flight rejected");
        bus.Cancel(id, key); bus.Cancel(id, key);
        check(Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 2 &&
            Scalar("SELECT used_count FROM dbo.DISCOUNT_CODE WHERE discount_id=@id", ("@id", discountId)) == 0,
            "SQL repeated cancel restores seats and discount exactly once");
        check(bus.Get(id, key) is { Status: "CANCELLED", InvoiceStatus: "CANCELLED" } cancelled && cancelled.Details.All(d => d.TicketStatus == "CANCELLED"), "SQL cancel synchronizes statuses");
        var second = Fixture(1);
        Reject(() => bus.Quote(fixture.Flight, second.Fare, 1, null), "SQL fare must belong to flight");
        var tampered = Request(second); tampered.ExpectedTotal = 1;
        Reject(() => bus.Create(tampered, BookingBUS.NewAccessKey()), "SQL recomputes total");
        check(Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", second.Flight)) == 1, "SQL bad total leaves seats unchanged");

        var totalsSql = """
            SELECT (SELECT COUNT(*) FROM dbo.CUSTOMER)+(SELECT COUNT(*) FROM dbo.BOOKING)+
            (SELECT COUNT(*) FROM dbo.TICKET)+(SELECT COUNT(*) FROM dbo.BOOKING_DETAIL)+
            (SELECT COUNT(*) FROM dbo.INVOICE)+(SELECT COUNT(*) FROM dbo.INVOICE_DETAIL)
            """;
        var before = Scalar(totalsSql);
        try
        {
            new BookingDAO(connection).Write<int>(data =>
            {
                var quote = bus.Quote(fixture.Flight, fixture.Fare, 1, code);
                data.Flight(fixture.Flight, true); data.ReserveSeats(fixture.Flight, 1); data.UseDiscount(discountId);
                var customer = data.InsertCustomer(Request(fixture).Customer);
                var bookingId = data.InsertBooking(customer, quote, DateTime.Now);
                var invoice = data.InsertInvoice(bookingId, quote, DateTime.Now);
                data.InsertPassenger(bookingId, invoice, quote, Request(fixture).Passengers[0], "1A", DateTime.Now);
                throw new InvalidOperationException("Injected failure after multi-table writes");
            });
            throw new Exception("Expected failure missing");
        }
        catch (InvalidOperationException error) when (error.Message.StartsWith("Injected failure")) { }
        check(Scalar(totalsSql) == before && Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 2 &&
            Scalar("SELECT used_count FROM dbo.DISCOUNT_CODE WHERE discount_id=@id", ("@id", discountId)) == 0, "SQL rollback restores all tables/counters");

        var oneSeat = Fixture(1);
        var repeatedKey = BookingBUS.NewAccessKey();
        var repeated = await Task.WhenAll(Enumerable.Range(0, 2).Select(_ => Task.Run(() => bus.Create(Request(oneSeat), repeatedKey))));
        check(repeated[0] == repeated[1], "SQL concurrent duplicate requests create once");
        var lastSeat = Fixture(1);
        async Task<bool> TryBook((int Flight, int Fare) target, string? discountCode = null)
        {
            try { await Task.Run(() => bus.Create(Request(target, 1, discountCode), BookingBUS.NewAccessKey())); return true; }
            catch (BookingException) { return false; }
        }
        var seatsRace = await Task.WhenAll(TryBook(lastSeat), TryBook(lastSeat));
        check(seatsRace.Count(v => v) == 1 && Scalar("SELECT COUNT(*) FROM dbo.TICKET WHERE flight_id=@id AND status='ISSUED'", ("@id", lastSeat.Flight)) == 1, "SQL concurrent last-seat race");
        var discountRace = await Task.WhenAll(TryBook(Fixture(1), code), TryBook(Fixture(1), code));
        check(discountRace.Count(v => v) == 1 && Scalar("SELECT used_count FROM dbo.DISCOUNT_CODE WHERE discount_id=@id", ("@id", discountId)) == 1, "SQL concurrent last-discount race");
        (int Flight, int Fare) ReturnFixture(int seats)
        {
            var result = Fixture(seats);
            Scalar("UPDATE dbo.FLIGHT SET departure_airport_id=@arrival,arrival_airport_id=@departure,departure_time='12:00',arrival_time='14:00' WHERE flight_id=@id; SELECT 1",
                ("@arrival", arrival), ("@departure", departure), ("@id", result.Flight));
            return result;
        }
        var outLeg = Fixture(3); var backLeg = ReturnFixture(3);
        var roundTrip = Request(outLeg, 2);
        roundTrip.TripType = "RoundTrip"; roundTrip.ReturnDate = DateOnly.FromDateTime(DateTime.UtcNow.Date.AddDays(3));
        roundTrip.ReturnFlightId = backLeg.Flight; roundTrip.ReturnFareId = backLeg.Fare; roundTrip.ExpectedTotal = 400000;
        var roundKey = BookingBUS.NewAccessKey(); var roundId = bus.Create(roundTrip, roundKey);
        var journey = bus.GetJourney(roundId, roundKey);
        check(journey.Count == 2 && journey.All(b => b.Details.Count == 2) && journey.Sum(b => b.TotalAmount) == 400000,
            "SQL round trip has two bookings and two passengers per leg");
        check(journey[0].Details.Select(d => d.PassengerIdCard).SequenceEqual(journey[1].Details.Select(d => d.PassengerIdCard)), "SQL same passenger list on both legs");
        check(bus.Create(roundTrip, roundKey) == roundId && bus.GetJourney(roundId, roundKey).Count == 2, "SQL round-trip retry creates no extra leg");
        bus.CancelJourney(roundId, roundKey); bus.CancelJourney(roundId, roundKey);
        check(bus.GetJourney(roundId, roundKey).All(b => b.Status == "CANCELLED") &&
            Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", outLeg.Flight)) == 3 &&
            Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", backLeg.Flight)) == 3, "SQL cancel journey twice restores both legs once");
        var rollbackOut = Fixture(2); var rollbackBack = ReturnFixture(2);
        // A deliberately inconsistent return counter fails after the outbound writes.
        Scalar("UPDATE dbo.FLIGHT SET tickets_sold=1,seats_available=1 WHERE flight_id=@id; SELECT 1", ("@id", rollbackBack.Flight));
        var brokenTrip = Request(rollbackOut); brokenTrip.TripType = "RoundTrip"; brokenTrip.ReturnDate = roundTrip.ReturnDate;
        brokenTrip.ReturnFlightId = rollbackBack.Flight; brokenTrip.ReturnFareId = rollbackBack.Fare; brokenTrip.ExpectedTotal = 200000;
        var beforeJourney = Scalar(totalsSql);
        Reject(() => bus.Create(brokenTrip, BookingBUS.NewAccessKey()), "SQL return-leg failure rejects entire journey");
        check(Scalar(totalsSql) == beforeJourney && Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", rollbackOut.Flight)) == 2,
            "SQL second-leg failure rolls back outbound rows and seats");
        Scalar("UPDATE dbo.FLIGHT SET tickets_sold=0,seats_available=2 WHERE flight_id=@id; SELECT 1", ("@id", rollbackBack.Flight));
        Console.WriteLine("SQL fixtures retained in the dedicated test database: " + label);
    }
}
