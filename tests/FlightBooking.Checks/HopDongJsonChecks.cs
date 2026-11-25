using System.Text.Json;
using FlightBooking.DTO;

internal static class HopDongJsonChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Dữ liệu JSON theo tên trước khi Việt hóa; không lấy tên từ reflection của DTO mới.
        const string sanBayCu = """{"AirportId":7,"AirportName":"Test Airport","City":"Test City"}""";
        var sanBay = JsonSerializer.Deserialize<SanBayDTO>(sanBayCu)!;
        check(sanBay == new SanBayDTO(7, "Test Airport", "Test City"), "JSON: legacy airport loads into Vietnamese properties");
        check(JsonSerializer.Serialize(sanBay) == sanBayCu, "JSON: airport external names and order stay unchanged");

        const string hangVeCu = """{"FareId":9,"FlightId":8,"ClassName":"Economy","Price":1234567.89,"BaseBaggageKg":7}""";
        var hangVe = JsonSerializer.Deserialize<HangVeDTO>(hangVeCu)!;
        check(hangVe.MaHangVe == 9 && hangVe.MaChuyenBay == 8 && hangVe.GiaVe == 1234567.89m && hangVe.HanhLyCoBanKg == 7,
            "JSON: legacy fare preserves identifiers and decimal price");
        check(JsonSerializer.Serialize(hangVe) == hangVeCu, "JSON: fare external names and precision stay unchanged");

        const string chuyenBayCu = """
            {"FlightId":8,"DepartureAirportId":7,"ArrivalAirportId":6,"DepartureCity":"A","ArrivalCity":"B",
            "DepartureAirport":"Airport A","ArrivalAirport":"Airport B","DepartureDate":"2026-12-15",
            "DepartureTime":"08:30:00","ArrivalDate":"2026-12-15","ArrivalTime":"10:45:00",
            "TotalSeats":10,"SeatsAvailable":8,"Status":"SCHEDULED"}
            """;
        var chuyenBay = JsonSerializer.Deserialize<ChuyenBayDTO>(chuyenBayCu)!;
        check(chuyenBay.MaChuyenBay == 8 && chuyenBay.TongSoGhe == 10 && chuyenBay.SoGheCon == 8 &&
            chuyenBay.ThoiDiemKhoiHanh == new DateTime(2026, 12, 15, 8, 30, 0), "JSON: legacy flight retains dates and seat counters");
        using var json = JsonDocument.Parse(JsonSerializer.Serialize(chuyenBay));
        check(json.RootElement.EnumerateObject().Select(p => p.Name).SequenceEqual(new[] {
            "FlightId", "DepartureAirportId", "ArrivalAirportId", "DepartureCity", "ArrivalCity", "DepartureAirport", "ArrivalAirport",
            "DepartureDate", "DepartureTime", "ArrivalDate", "ArrivalTime", "TotalSeats", "SeatsAvailable", "Status", "DepartsAt", "ArrivesAt" }),
            "JSON: all flight names including computed dates retain the original contract");

        string baoGiaCu = "{\"Flight\":" + chuyenBayCu + ",\"Fare\":" + hangVeCu +
            ",\"Quantity\":2,\"Subtotal\":2469135.78,\"DiscountAmount\":100.01,\"TotalAmount\":2469035.77,\"DiscountId\":3}";
        var baoGia = JsonSerializer.Deserialize<BaoGiaDatVeDTO>(baoGiaCu)!;
        check(baoGia.ChuyenBay == chuyenBay && baoGia.HangVe == hangVe && baoGia.SoLuong == 2 && baoGia.TongTien == 2469035.77m,
            "JSON: nested legacy quote loads without losing price history");
        var hanhTrinh = JsonSerializer.Deserialize<BaoGiaHanhTrinhDTO>("{\"Outbound\":" + baoGiaCu + ",\"Return\":null}")!;
        check(hanhTrinh.LuotDi == baoGia && hanhTrinh.LuotVe is null && hanhTrinh.TongTien == baoGia.TongTien,
            "JSON: legacy journey keeps its outbound leg and nullable return leg");
    }
}
