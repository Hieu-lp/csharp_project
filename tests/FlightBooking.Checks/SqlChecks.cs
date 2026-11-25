using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Security.Cryptography;

internal static class SqlChecks
{
    public static async Task Run(Action<bool, string> check)
    {
        var connectionString = Environment.GetEnvironmentVariable("FLIGHTBOOKING_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Set FLIGHTBOOKING_TEST_CONNECTION first.");
        FlightBooking.Testing.SqlTestPreflight.Verify(connectionString);
        var connection = new MyConnection(connectionString);
        int Scalar(string sql, params (string, object)[] values)
        {
            using var c = connection.Open();
            using var command = new SqlCommand(sql, c);
            foreach (var (name, value) in values) command.Parameters.AddWithValue(name, value);
            return Convert.ToInt32(command.ExecuteScalar());
        }
        // Schema và index đã được preflight xác minh. Không tự áp dụng script 02/03/08.
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
        YeuCauDatVe Request((int Flight, int Fare) f, int quantity = 1, string? code = null) => new()
        {
            FlightId = f.Flight, FareId = f.Fare, Quantity = quantity, DiscountCode = code, ExpectedTotal = quantity * 100000 - (code is null ? 0 : 10000),
            Customer = new() { FullName = "Khách kiểm thử", Email = label + "@example.test", Phone = "0900000000", DateOfBirth = new(2000,1,1), Gender = "Khác", IdPassportNumber = "TEST-BOOKER-001" },
            Passengers = Enumerable.Range(1, quantity).Select(i => new ThongTinHanhKhach { FullName = "Hành khách " + i, IdCard = label + "-" + i }).ToList()
        };
        void Reject(Action action, string name)
        {
            try { action(); } catch (LoiDatVe) { check(true, name); return; }
            throw new Exception("FAIL: " + name);
        }
        var bus = new DatVeBUS(connectionString);
        var fixture = Fixture(2);
        var code = label;
        var discountId = Scalar("""
            INSERT dbo.DISCOUNT_CODE(code,description,discount_type,discount_value,min_order_amount,start_date,end_date,usage_limit,used_count,status)
            OUTPUT INSERTED.discount_id VALUES (@code,N'Test','FIXED',10000,0,@start,@end,1,0,'ACTIVE')
            """, ("@code", code), ("@start", DateTime.UtcNow.Date.AddDays(-1)), ("@end", DateTime.UtcNow.Date.AddDays(10)));
        var request = Request(fixture, 2, code);
        var key = DatVeBUS.TaoKhoaTraCuu();
        var id = bus.TaoDatVe(request, key);
        var booking = bus.TraCuuDatVe(id, key)!;
        check(booking.ChiTiet.Count == 2 && booking.TongTien == 190000 && booking.TrangThaiHoaDon == "UNPAID", "SQL create booking/tickets/invoice and discount");
        check(booking.ChiTiet.Select(d => d.SoGhe).Distinct().Count() == 2, "SQL distinct seats");
        check(Scalar("SELECT total_revenue FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 0, "SQL unpaid booking does not record revenue");
        check(bus.TaoDatVe(request, key) == id, "SQL repeated request returns same booking");
        request.Passengers[0].FullName = "Changed";
        Reject(() => bus.TaoDatVe(request, key), "SQL same key different payload rejected");
        check(bus.TraCuuDatVe(id, DatVeBUS.TaoKhoaTraCuu()) is null, "SQL wrong key cannot read booking");
        Reject(() => bus.HuyDatVe(id, DatVeBUS.TaoKhoaTraCuu()), "SQL wrong key cannot cancel");
        Reject(() => bus.TaoDatVe(Request(fixture), DatVeBUS.TaoKhoaTraCuu()), "SQL full flight rejected");
        bus.HuyDatVe(id, key); bus.HuyDatVe(id, key);
        check(Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 2 &&
            Scalar("SELECT used_count FROM dbo.DISCOUNT_CODE WHERE discount_id=@id", ("@id", discountId)) == 0,
            "SQL repeated cancel restores seats and discount exactly once");
        check(bus.TraCuuDatVe(id, key) is { TrangThai: "CANCELLED", TrangThaiHoaDon: "CANCELLED" } cancelled && cancelled.ChiTiet.All(d => d.TrangThaiVe == "CANCELLED"), "SQL cancel synchronizes statuses");
        var second = Fixture(1);
        Reject(() => bus.TinhGiaVe(fixture.Flight, second.Fare, 1, null), "SQL fare must belong to flight");
        var tampered = Request(second); tampered.ExpectedTotal = 1;
        Reject(() => bus.TaoDatVe(tampered, DatVeBUS.TaoKhoaTraCuu()), "SQL recomputes total");
        check(Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", second.Flight)) == 1, "SQL bad total leaves seats unchanged");

        string Snapshot()
        {
            using var sql = connection.Open();
            using var command = new SqlCommand("""
                SELECT CONCAT('CUSTOMER=',(SELECT COUNT(*) FROM dbo.CUSTOMER),
                 ';BOOKING=',(SELECT COUNT(*) FROM dbo.BOOKING),';TICKET=',(SELECT COUNT(*) FROM dbo.TICKET),
                 ';BOOKING_DETAIL=',(SELECT COUNT(*) FROM dbo.BOOKING_DETAIL),
                 ';INVOICE=',(SELECT COUNT(*) FROM dbo.INVOICE),';INVOICE_DETAIL=',(SELECT COUNT(*) FROM dbo.INVOICE_DETAIL),
                 ';BOOKING_WEB_REQUEST=',(SELECT COUNT(*) FROM dbo.BOOKING_WEB_REQUEST))
                """, sql);
            return (string)command.ExecuteScalar()!;
        }
        foreach (int faultAt in new[] { 1, 2, 3, 4 })
        {
            string before = Snapshot();
            try
            {
                new DatVeDAO(connection).GhiDuLieu<int>(data =>
                {
                    var quote = bus.TinhGiaVe(fixture.Flight, fixture.Fare, 1, code);
                    data.DocChuyenBay(fixture.Flight, true); data.GiuGhe(fixture.Flight, 1); data.DungMaGiamGia(discountId);
                    if (faultAt == 1) throw new RollbackProbe();
                    var customer = data.ThemKhachDat(Request(fixture).Customer);
                    if (faultAt == 2) throw new RollbackProbe();
                    var bookingId = data.ThemDatVe(customer, quote, DateTime.Now);
                    if (faultAt == 3) throw new RollbackProbe();
                    var invoice = data.ThemHoaDon(bookingId, quote, DateTime.Now);
                    data.ThemHanhKhach(bookingId, invoice, quote, Request(fixture).Passengers[0], "1A", DateTime.Now);
                    data.LuuYeuCau(Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), Convert.ToHexString(RandomNumberGenerator.GetBytes(32)), bookingId);
                    throw new RollbackProbe();
                });
                throw new Exception("Expected failure missing");
            }
            catch (RollbackProbe) { }
            string after = Snapshot();
            Console.WriteLine($"ROLLBACK stage {faultAt}: before [{before}], after [{after}]");
            check(after == before && Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", fixture.Flight)) == 2 &&
                Scalar("SELECT used_count FROM dbo.DISCOUNT_CODE WHERE discount_id=@id", ("@id", discountId)) == 0, $"SQL rollback stage {faultAt} restores each table/counter");
        }

        async Task<T[]> ConcurrentPair<T>(Func<T> first, Func<T> second)
        {
            using var start = new Barrier(2);
            T Run(Func<T> action)
            {
                if (!start.SignalAndWait(TimeSpan.FromSeconds(15))) throw new TimeoutException("Concurrent SQL workers did not both start.");
                return action();
            }
            return await Task.WhenAll(Task.Run(() => Run(first)), Task.Run(() => Run(second)));
        }

        var oneSeat = Fixture(1);
        var repeatedKey = DatVeBUS.TaoKhoaTraCuu();
        var repeated = await ConcurrentPair(() => bus.TaoDatVe(Request(oneSeat), repeatedKey), () => bus.TaoDatVe(Request(oneSeat), repeatedKey));
        check(repeated[0] == repeated[1], "SQL concurrent duplicate requests create once");
        var lastSeat = Fixture(1);
        bool TryBook((int Flight, int Fare) target, string? discountCode = null)
        {
            try { bus.TaoDatVe(Request(target, 1, discountCode), DatVeBUS.TaoKhoaTraCuu()); return true; }
            catch (LoiDatVe) { return false; }
        }
        var seatsRace = await ConcurrentPair(() => TryBook(lastSeat), () => TryBook(lastSeat));
        check(seatsRace.Count(v => v) == 1 && Scalar("SELECT COUNT(*) FROM dbo.TICKET WHERE flight_id=@id AND status='ISSUED'", ("@id", lastSeat.Flight)) == 1, "SQL concurrent last-seat race");
        var discountTargetA = Fixture(1); var discountTargetB = Fixture(1);
        var discountRace = await ConcurrentPair(() => TryBook(discountTargetA, code), () => TryBook(discountTargetB, code));
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
        var roundKey = DatVeBUS.TaoKhoaTraCuu(); var roundId = bus.TaoDatVe(roundTrip, roundKey);
        var journey = bus.TraCuuHanhTrinh(roundId, roundKey);
        check(journey.Count == 2 && journey.All(b => b.ChiTiet.Count == 2) && journey.Sum(b => b.TongTien) == 400000,
            "SQL round trip has two bookings and two passengers per leg");
        check(journey[0].ChiTiet.Select(d => d.GiayToHanhKhach).SequenceEqual(journey[1].ChiTiet.Select(d => d.GiayToHanhKhach)), "SQL same passenger list on both legs");
        check(bus.TaoDatVe(roundTrip, roundKey) == roundId && bus.TraCuuHanhTrinh(roundId, roundKey).Count == 2, "SQL round-trip retry creates no extra leg");
        bus.HuyHanhTrinh(roundId, roundKey); bus.HuyHanhTrinh(roundId, roundKey);
        check(bus.TraCuuHanhTrinh(roundId, roundKey).All(b => b.TrangThai == "CANCELLED") &&
            Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", outLeg.Flight)) == 3 &&
            Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", backLeg.Flight)) == 3, "SQL cancel journey twice restores both legs once");
        var rollbackOut = Fixture(2); var rollbackBack = ReturnFixture(2);
        // A deliberately inconsistent return counter fails after the outbound writes.
        Scalar("UPDATE dbo.FLIGHT SET tickets_sold=1,seats_available=1 WHERE flight_id=@id; SELECT 1", ("@id", rollbackBack.Flight));
        var brokenTrip = Request(rollbackOut); brokenTrip.TripType = "RoundTrip"; brokenTrip.ReturnDate = roundTrip.ReturnDate;
        brokenTrip.ReturnFlightId = rollbackBack.Flight; brokenTrip.ReturnFareId = rollbackBack.Fare; brokenTrip.ExpectedTotal = 200000;
        var beforeJourney = Snapshot();
        Reject(() => bus.TaoDatVe(brokenTrip, DatVeBUS.TaoKhoaTraCuu()), "SQL return-leg failure rejects entire journey");
        check(Snapshot() == beforeJourney && Scalar("SELECT seats_available FROM dbo.FLIGHT WHERE flight_id=@id", ("@id", rollbackOut.Flight)) == 2,
            "SQL second-leg failure rolls back outbound rows and seats");
        Scalar("UPDATE dbo.FLIGHT SET tickets_sold=0,seats_available=2 WHERE flight_id=@id; SELECT 1", ("@id", rollbackBack.Flight));
        Console.WriteLine("SQL fixtures retained in the dedicated test database: " + label);
    }

    // Separate from InvalidOperationException translated by the DAO connection boundary.
    private sealed class RollbackProbe : Exception { }
}
