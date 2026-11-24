using FlightBooking.BUS;
using FlightBooking.DAO;

internal static class BrowseChecks
{
    // Read-only checks against the same local database as the website. No seed or schema changes.
    public static void Run(Action<bool, string> check)
    {
        var cs = Environment.GetEnvironmentVariable("FLIGHTBOOKING_BROWSE_CONNECTION")
            ?? throw new InvalidOperationException("Set FLIGHTBOOKING_BROWSE_CONNECTION explicitly.");
        var bus = new DatVeBUS(cs);
        var dao = new DatVeDAO(new MyConnection(cs));
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime;
        var deals = bus.DocChuyenGiaThap();
        check(deals.Count <= 4 && deals.Select(x => x.ChuyenBay.MaChuyenBay).Distinct().Count() == deals.Count, "SQL: at most four distinct flights");
        check(deals.All(x => x.ChuyenBay.ThoiDiemKhoiHanh > now && x.ChuyenBay.TrangThai == "SCHEDULED" && x.ChuyenBay.SoGheCon > 0), "SQL: future Vietnam time, open and available");
        check(deals.SequenceEqual(deals.OrderBy(x => x.HangVe.GiaVe).ThenBy(x => x.ChuyenBay.ThoiDiemKhoiHanh).ThenBy(x => x.ChuyenBay.MaChuyenBay).ThenBy(x => x.HangVe.MaHangVe)), "SQL: stable price/departure/id order");
        foreach (var deal in deals)
        {
            var cheapest = dao.DocDuLieu(d => d.DocDSHangVe(deal.ChuyenBay.MaChuyenBay).OrderBy(f => f.GiaVe).ThenBy(f => f.MaHangVe).First());
            check(deal.HangVe == cheapest, "SQL: cheapest fare for flight " + deal.ChuyenBay.MaChuyenBay);
            foreach (var quantity in new[] { 1, 2, 3 })
            {
                var selected = bus.ChonChuyenBay(deal.ChuyenBay.MaChuyenBay, deal.HangVe.MaHangVe, quantity);
                check(selected.BaoGia.ChuyenBay == deal.ChuyenBay && selected.BaoGia.HangVe == deal.HangVe && selected.BaoGia.TongTien == deal.HangVe.GiaVe * quantity && selected.ConDuGhe == (deal.ChuyenBay.SoGheCon >= quantity), "SQL: selection preserves flight/fare and recalculates " + quantity);
            }
            Console.WriteLine($"DEAL: {deal.ChuyenBay.MaChuyenBay}:{deal.HangVe.MaHangVe} {deal.ChuyenBay.ThanhPhoDi} -> {deal.ChuyenBay.ThanhPhoDen} {deal.ChuyenBay.ThoiDiemKhoiHanh:yyyy-MM-dd HH:mm} {deal.HangVe.GiaVe}");
        }
        if (deals.Count > 1)
        {
            try { bus.ChonChuyenBay(deals[0].ChuyenBay.MaChuyenBay, deals[1].HangVe.MaHangVe, 1); throw new Exception("Expected mismatched fare rejection"); }
            catch (FlightBooking.DTO.LoiDatVe) { check(true, "SQL: reject fare belonging to another flight"); }
        }
        check(bus.DocTenHangVe().Count > 0, "SQL: fare dropdown reads database");
        IntegrationReadChecks.Run(check, cs);
    }
}
