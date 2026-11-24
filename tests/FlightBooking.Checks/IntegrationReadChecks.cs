using FlightBooking.BUS;
using FlightBooking.DTO;

internal static class IntegrationReadChecks
{
    public static void Run(Action<bool, string> check, string connectionString)
    {
        // Chỉ đọc danh mục/tìm chuyến/xem giá; không gọi Save, Create, Cancel hoặc SQL script.
        var management = new QuanLyBUS(connectionString);
        var booking = new DatVeBUS(connectionString);
        var airports = management.DocDSSanBay().Cast<SanBayDTO>().OrderBy(a => a.MaSanBay).ToArray();
        var flights = management.DocDSChuyenBay().Cast<QuanLyChuyenBayDTO>().ToArray();
        var fares = management.DocDSHangVe().Cast<HangVeDTO>().ToArray();
        int checks = 0;
        void Compare(bool value, string label)
        {
            check(value, "SQL-READ integration: " + label);
            checks++;
        }

        Compare(airports.SequenceEqual(booking.DocDSSanBay().OrderBy(a => a.MaSanBay)), "same airport catalog in Management and Booking");
        foreach (QuanLyChuyenBayDTO managed in flights)
        {
            Compare(booking.DocChuyenBay(managed.ChuyenBay.MaChuyenBay) == managed.ChuyenBay,
                "same route/schedule/seats/status for flight " + managed.ChuyenBay.MaChuyenBay);
        }
        foreach (LuaChonChuyenBayDTO deal in booking.DocChuyenGiaThap())
        {
            HangVeDTO fare = fares.Single(f => f.MaHangVe == deal.HangVe.MaHangVe);
            Compare(fare == deal.HangVe, "same fare and server price for fare " + fare.MaHangVe);
            int quantity = Math.Min(2, deal.ChuyenBay.SoGheCon);
            BaoGiaDatVeDTO quote = booking.TinhGiaVe(deal.ChuyenBay.MaChuyenBay, fare.MaHangVe, quantity, null);
            Compare(quote.HangVe == fare && quote.TamTinh == fare.GiaVe * quantity &&
                quote.TongTien == quote.TamTinh && quote.TienGiam == 0,
                "server quote uses Management fare price for flight " + deal.ChuyenBay.MaChuyenBay);
            var search = new DieuKienTimChuyenBay
            {
                DepartureAirportId = deal.ChuyenBay.MaSanBayDi,
                ArrivalAirportId = deal.ChuyenBay.MaSanBayDen,
                DepartureDate = deal.ChuyenBay.NgayKhoiHanh, Passengers = quantity
            };
            Compare(booking.TimChuyenBay(search).Cast<LuaChonChuyenBayDTO>().Any(o => o.ChuyenBay.MaChuyenBay == deal.ChuyenBay.MaChuyenBay && o.HangVe == fare),
                "eligible catalog flight/fare appears in Booking search " + deal.ChuyenBay.MaChuyenBay);
        }

        DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime;
        ChuyenBayDTO[] future = flights.Select(m => m.ChuyenBay).Where(f => f.TrangThai == "SCHEDULED" && f.ThoiDiemKhoiHanh > now && f.SoGheCon > 0).ToArray();
        bool hasReturnPair = future.Any(outbound => future.Any(returning =>
            returning.MaChuyenBay != outbound.MaChuyenBay && returning.MaSanBayDi == outbound.MaSanBayDen &&
            returning.MaSanBayDen == outbound.MaSanBayDi && returning.ThoiDiemKhoiHanh > outbound.ThoiDiemDen));
        Console.WriteLine(hasReturnPair ? "READ-ONLY DATA: future reverse-route pair exists." :
            "SKIP: no future reverse-route pair in existing data; do not create flights on the main database for round-trip testing.");
        Console.WriteLine($"{checks} Management/Booking integration read checks passed. No SQL writes.");
    }
}
