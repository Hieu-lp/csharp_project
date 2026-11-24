using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

internal static class DemoRollbackChecks
{
    // Explicit opt-in only. All test writes are inside an intentionally rolled-back
    // transaction. Does not apply migrations or use BOOKING_WEB_REQUEST.
    public static void Run(Action<bool, string> check)
    {
        var cs = Environment.GetEnvironmentVariable("FLIGHTBOOKING_DEMO_CONNECTION")
            ?? throw new InvalidOperationException("Set FLIGHTBOOKING_DEMO_CONNECTION explicitly.");
        // Legacy demo từng yêu cầu DB chính. Nay mọi lối ghi phải qua cùng preflight DB test.
        FlightBooking.Testing.SqlTestPreflight.Verify(cs);
        using var connection = new MyConnection(cs).Open();
        using var select = new SqlCommand("""
            SELECT TOP(1) f.flight_id,a.fare_id,r.flight_id,b.fare_id
            FROM dbo.FLIGHT f JOIN dbo.FLIGHT r
              ON r.departure_airport_id=f.arrival_airport_id AND r.arrival_airport_id=f.departure_airport_id
              AND r.departure_date>=f.arrival_date
            JOIN dbo.FARE_CLASS a ON a.flight_id=f.flight_id AND a.class_name=N'Economy'
            JOIN dbo.FARE_CLASS b ON b.flight_id=r.flight_id AND b.class_name=N'Economy'
            WHERE f.departure_time='07:15' AND r.departure_time='13:40'
              AND f.departure_date BETWEEN '20261101' AND '20270131'
              AND r.departure_date BETWEEN '20261101' AND '20270131'
              AND f.departure_date>CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time')
              AND f.status='SCHEDULED' AND r.status='SCHEDULED'
              AND f.seats_available>=3 AND r.seats_available>=3
            ORDER BY f.departure_date,r.departure_date,f.flight_id,r.flight_id
            """, connection);
        int outbound, outboundFare, returning, returnFare;
        using (var reader = select.ExecuteReader())
        {
            if (!reader.Read()) throw new InvalidOperationException("No future seed-04 round-trip pair with three seats.");
            outbound = reader.GetInt32(0); outboundFare = reader.GetInt32(1);
            returning = reader.GetInt32(2); returnFare = reader.GetInt32(3);
        }
        string Snapshot()
        {
            using var command = new SqlCommand("""
                SELECT CONCAT((SELECT COUNT(*) FROM dbo.CUSTOMER),':',
                  (SELECT COUNT(*) FROM dbo.BOOKING),':',(SELECT COUNT(*) FROM dbo.BOOKING_DETAIL),':',
                  (SELECT COUNT(*) FROM dbo.TICKET),':',(SELECT COUNT(*) FROM dbo.INVOICE),':',
                  (SELECT COUNT(*) FROM dbo.INVOICE_DETAIL),':',
                  (SELECT SUM(tickets_sold) FROM dbo.FLIGHT WHERE flight_id IN (@out,@back)),':',
                  (SELECT SUM(seats_available) FROM dbo.FLIGHT WHERE flight_id IN (@out,@back)),':',
                  (SELECT SUM(total_revenue) FROM dbo.FLIGHT WHERE flight_id IN (@out,@back)))
                """, connection);
            command.Parameters.AddWithValue("@out", outbound); command.Parameters.AddWithValue("@back", returning);
            return (string)command.ExecuteScalar()!;
        }
        var bus = new DatVeBUS(cs);
        for (var quantity = 1; quantity <= 3; quantity++)
        {
            var request = new YeuCauDatVe
            {
                FlightId = outbound, FareId = outboundFare, Quantity = quantity, TripType = "RoundTrip",
                ReturnFlightId = returning, ReturnFareId = returnFare,
                ReturnDate = bus.DocChuyenBay(returning).NgayKhoiHanh,
                Customer = new() { FullName = "Người đặt thử độc lập", Email = "roundtrip.rollback@example.test", Phone = "0900000000", DateOfBirth = new(2000, 1, 1), Gender = "Nam", IdPassportNumber = "TEST-BOOKER-001" },
                Passengers = Enumerable.Range(1, quantity).Select(i => new ThongTinHanhKhach
                  { FullName = "Hành khách thử " + i, IdCard = "ROLLBACK-" + i }).ToList()
            };
            var quote = bus.TinhGiaHanhTrinh(request);
            request.ExpectedTotal = quote.TongTien;
            DatVeBUS.KiemTraYeuCauDatVe(request, DateOnly.FromDateTime(DateTime.Today));
            var before = Snapshot();
            try
            {
                new DatVeDAO(new MyConnection(cs)).GhiDuLieu<int>(data =>
                {
                    foreach (var id in new[] { outbound, returning }.Order()) data.DocChuyenBay(id, true);
                    var customer = data.ThemKhachDat(request.Customer);
                    foreach (var leg in new[] { quote.LuotDi, quote.LuotVe! })
                    {
                        var seats = DatVeBUS.CapGhe(leg.ChuyenBay.TongSoGhe, data.DocGheHieuLuc(leg.ChuyenBay.MaChuyenBay), quantity);
                        data.GiuGhe(leg.ChuyenBay.MaChuyenBay, quantity);
                        var booking = data.ThemDatVe(customer, leg, DateTime.Now);
                        var invoice = data.ThemHoaDon(booking, leg, DateTime.Now);
                        for (var i = 0; i < quantity; i++)
                            data.ThemHanhKhach(booking, invoice, leg, request.Passengers[i], seats[i], DateTime.Now);
                        var details = data.DocChiTietDatVe(booking);
                        check(details.Count == quantity && details.Select(d => d.HoTenHanhKhach).SequenceEqual(request.Passengers.Select(p => p.FullName)) &&
                            details.Select(d => d.GiayToHanhKhach).SequenceEqual(request.Passengers.Select(p => p.IdCard)) &&
                            details.Select(d => d.SoGhe).Distinct().Count() == quantity,
                            $"SQL DAO: {quantity} independent passengers/tickets on flight {leg.ChuyenBay.MaChuyenBay}");
                    }
                    throw new RollbackProbe();
                });
            }
            catch (RollbackProbe) { }
            check(before == Snapshot(), $"SQL rollback: {quantity} passengers leaves no customer/booking/ticket/invoice or counter changes");
        }
        Console.WriteLine("NOTE: DAO transaction checks passed; full Web confirmation/idempotency still requires approved script 08.");
    }

    private sealed class RollbackProbe : Exception;
}
