using System.Collections;
using System.Globalization;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages;

public class IndexModel : PageModel
{
    private readonly DatVeBUS nghiepVu;

    public IndexModel(DatVeBUS nghiepVu)
    {
        this.nghiepVu = nghiepVu;
    }

    [BindProperty(SupportsGet = true)] public DieuKienTimChuyenBay Search { get; set; } = new();
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }
    [BindProperty(SupportsGet = true)] public string? DraftId { get; set; }
    [BindProperty(SupportsGet = true)] public int OutboundFlightId { get; set; }
    [BindProperty(SupportsGet = true)] public int OutboundFareId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedOffer { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedDeal { get; set; }
    [BindProperty(SupportsGet = true)] public int DealFlightId { get; set; }
    [BindProperty(SupportsGet = true)] public int DealFareId { get; set; }
    [BindProperty(SupportsGet = true)] public bool ContinueDeal { get; set; }
    [BindProperty(SupportsGet = true)] public string? MinPriceText { get; set; }
    [BindProperty(SupportsGet = true)] public string? MaxPriceText { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterAction { get; set; }
    public bool SelectingReturn => Search.TripType == "RoundTrip" && OutboundFlightId > 0;
    public bool IsDealSelection { get; private set; }
    public ChuyenBayDaChonDTO? DealSelection { get; private set; }
    public BaoGiaDatVeDTO? Outbound { get; private set; }
    public IReadOnlyList<SanBayDTO> Airports { get; private set; } = [];
    public IReadOnlyList<string> FareClassNames { get; private set; } = [];
    public IReadOnlyList<LuaChonChuyenBayDTO> Deals { get; private set; } = [];
    public ArrayList Offers { get; private set; } = [];
    public string? Error { get; private set; }
    private static string? DinhDangTien(decimal? soTien) => soTien?.ToString("#,0.##", CultureInfo.GetCultureInfo("vi-VN"));

    public IActionResult OnGet()
    {
        IsDealSelection = Request.Query.ContainsKey(nameof(DealFlightId)) || Request.Query.ContainsKey(nameof(DealFareId));
        try
        {
            if (SelectedDeal is not null)
            {
                return ChuyenDenUuDai(SelectedDeal);
            }
            if (IsDealSelection)
            {
                return HienThiUuDai();
            }

            ChuanBiBoLoc();
            if (!Submitted) Search.DepartureDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime).AddDays(1);
            DatVeBUS.KiemTraNgayTimKiem(Search);
            if (!string.IsNullOrEmpty(Search.FareClassName) && !FareClassNames.Contains(Search.FareClassName, StringComparer.OrdinalIgnoreCase))
                throw new LoiDatVe("Hạng vé không có trong dữ liệu hiện tại. Hãy chọn lại hoặc xóa bộ lọc.");
            if (!Submitted) { Deals = nghiepVu.DocChuyenGiaThap(); return Page(); }
            if (!ModelState.IsValid) { Error = "Điều kiện tìm kiếm không hợp lệ."; return Page(); }
            if (SelectingReturn)
            {
                Offers = TimChuyenVe();
            }
            else Offers = nghiepVu.TimChuyenBay(Search);
            if (SelectedOffer is not null)
            {
                return ChuyenDenChuyenDaChon(SelectedOffer);
            }
        }
        catch (LoiDatVe loi) { Error = loi.Message; }
        return Page();
    }

    private IActionResult ChuyenDenUuDai(string luaChon)
    {
        var (chuyenBay, hangVe) = DocLuaChon(luaChon);
        KiemTraSoHanhKhach();
        nghiepVu.ChonChuyenBay(chuyenBay, hangVe, Search.Passengers);
        return RedirectToPage("/Index", null, new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            ["DealFlightId"] = chuyenBay,
            ["DealFareId"] = hangVe,
            ["Search.Passengers"] = Search.Passengers,
            ["DraftId"] = DraftId
        }, "flight-results");
    }

    private IActionResult HienThiUuDai()
    {
        KiemTraSoHanhKhach();
        if (DealFlightId <= 0 || DealFareId <= 0 || !ModelState.IsValid)
            throw new LoiDatVe("Mã chuyến/hạng vé hoặc số hành khách không hợp lệ. Hãy chọn chuyến khác.");
        DealSelection = nghiepVu.ChonChuyenBay(DealFlightId, DealFareId, Search.Passengers);
        BaoGiaDatVeDTO baoGia = DealSelection.BaoGia;
        // DTO từ BUS quyết định tuyến/ngày/giá; không lấy các giá trị này từ query của trình duyệt.
        Search = new DieuKienTimChuyenBay
        {
            DepartureAirportId = baoGia.ChuyenBay.MaSanBayDi,
            ArrivalAirportId = baoGia.ChuyenBay.MaSanBayDen,
            DepartureDate = baoGia.ChuyenBay.NgayKhoiHanh,
            Passengers = baoGia.SoLuong,
            TripType = "OneWay"
        };
        Submitted = true;
        OutboundFlightId = 0;
        OutboundFareId = 0;
        MinPriceText = null;
        MaxPriceText = null;
        ModelState.Clear();
        Airports = nghiepVu.DocDSSanBay();
        if (ContinueDeal && DealSelection.ConDuGhe)
            return RedirectToPage("/Booking/Checkout", new { flightId = DealFlightId, fareId = DealFareId, quantity = Search.Passengers, tripType = "OneWay", draftId = DraftId });
        return Page();
    }

    private void ChuanBiBoLoc()
    {
        // Giữ danh sách lựa chọn khi biểu mẫu có lỗi; phép lọc nâng cao vẫn thực hiện tại BUS.
        Airports = nghiepVu.DocDSSanBay();
        FareClassNames = nghiepVu.DocTenHangVe();
        Search.Price = null;
        Search.ClassName = null;
        Search.Operator = "<=";
        Search.Join = "AND";
        Search.Not = false;
        foreach (string khoa in new[] { "Search.Price", "Search.ClassName", "Search.Operator", "Search.Join", "Search.Not", "Search.MinPrice", "Search.MaxPrice" })
            ModelState.Remove(khoa);
        if (FilterAction == "clear")
        {
            MinPriceText = null;
            MaxPriceText = null;
            Search.FareClassName = null;
            foreach (string khoa in new[] { nameof(MinPriceText), nameof(MaxPriceText), "Search.FareClassName" }) ModelState.Remove(khoa);
        }
        Search.MinPrice = DatVeBUS.DocTienViet(MinPriceText);
        Search.MaxPrice = DatVeBUS.DocTienViet(MaxPriceText);
        DatVeBUS.KiemTraKhoangGia(Search);
        MinPriceText = DinhDangTien(Search.MinPrice);
        MaxPriceText = DinhDangTien(Search.MaxPrice);
        ModelState.Remove(nameof(MinPriceText));
        ModelState.Remove(nameof(MaxPriceText));
    }

    private ArrayList TimChuyenVe()
    {
        Outbound = nghiepVu.TinhGiaVe(OutboundFlightId, OutboundFareId, Search.Passengers, null);
        if (Outbound.ChuyenBay.MaSanBayDi != Search.DepartureAirportId ||
            Outbound.ChuyenBay.MaSanBayDen != Search.ArrivalAirportId ||
            Outbound.ChuyenBay.NgayKhoiHanh != Search.DepartureDate)
            throw new LoiDatVe("Lượt đi không khớp hành trình tìm kiếm.");
        var luotVe = new DieuKienTimChuyenBay
        {
            DepartureAirportId = Search.ArrivalAirportId,
            ArrivalAirportId = Search.DepartureAirportId,
            DepartureDate = Search.ReturnDate!.Value,
            Passengers = Search.Passengers,
            MinPrice = Search.MinPrice,
            MaxPrice = Search.MaxPrice,
            FareClassName = Search.FareClassName
        };
        ArrayList ketQua = new ArrayList();
        foreach (LuaChonChuyenBayDTO luaChon in nghiepVu.TimChuyenBay(luotVe))
        {
            if (luaChon.ChuyenBay.ThoiDiemKhoiHanh > Outbound.ChuyenBay.ThoiDiemDen) ketQua.Add(luaChon);
        }
        return ketQua;
    }

    private IActionResult ChuyenDenChuyenDaChon(string chuoiLuaChon)
    {
        var (chuyenBay, hangVe) = DocLuaChon(chuoiLuaChon);
        LuaChonChuyenBayDTO luaChon = Offers.Cast<LuaChonChuyenBayDTO>().SingleOrDefault(luaChon => luaChon.ChuyenBay.MaChuyenBay == chuyenBay && luaChon.HangVe.MaHangVe == hangVe)
            ?? throw new LoiDatVe("Chuyến/hạng vé không còn phù hợp với tìm kiếm và số người hiện tại. Hãy chọn lại.");
        var duongDan = new Microsoft.AspNetCore.Routing.RouteValueDictionary();
        foreach (var phanTu in TaoDuongDanLuaChon(luaChon)) duongDan[phanTu.Key] = phanTu.Value;
        return RedirectToPage(SelectionPage, duongDan);
    }

    private void KiemTraSoHanhKhach()
    {
        if (Search.Passengers is < 1 or > 9 || ModelState.TryGetValue("Search.Passengers", out var trangThai) && trangThai.Errors.Count > 0)
            throw new LoiDatVe("Chọn số hành khách từ 1 đến 9.");
    }
    private static (int Flight, int Fare) DocLuaChon(string chuoi)
    {
        var cacPhan = chuoi.Split(':');
        if (cacPhan.Length != 2 || !int.TryParse(cacPhan[0], out var chuyenBay) || !int.TryParse(cacPhan[1], out var hangVe) || chuyenBay <= 0 || hangVe <= 0)
            throw new LoiDatVe("Lựa chọn chuyến/hạng vé không hợp lệ.");
        return (chuyenBay, hangVe);
    }

    public string SelectionPage => Search.TripType == "RoundTrip" && !SelectingReturn ? "/Index" : "/Booking/Checkout";
    public Dictionary<string, string?> TaoDuongDanLuaChon(LuaChonChuyenBayDTO luaChon)
    {
        if (SelectionPage == "/Index")
        {
            var duongDan = TaoDuongDanTimKiem();
            duongDan["OutboundFlightId"] = luaChon.ChuyenBay.MaChuyenBay.ToString(); duongDan["OutboundFareId"] = luaChon.HangVe.MaHangVe.ToString();
            return duongDan;
        }
        return new()
        {
            ["flightId"] = (SelectingReturn ? OutboundFlightId : luaChon.ChuyenBay.MaChuyenBay).ToString(),
            ["fareId"] = (SelectingReturn ? OutboundFareId : luaChon.HangVe.MaHangVe).ToString(),
            ["quantity"] = Search.Passengers.ToString(),
            ["tripType"] = Search.TripType,
            ["returnDate"] = Search.ReturnDate?.ToString("yyyy-MM-dd"),
            ["draftId"] = DraftId,
            ["returnFlightId"] = SelectingReturn ? luaChon.ChuyenBay.MaChuyenBay.ToString() : null,
            ["returnFareId"] = SelectingReturn ? luaChon.HangVe.MaHangVe.ToString() : null
        };
    }

    public Dictionary<string, string?> TaoDuongDanTimKiem() => new()
    {
        ["Submitted"] = "true",
        ["DraftId"] = DraftId,
        ["Search.TripType"] = Search.TripType,
        ["Search.ReturnDate"] = Search.ReturnDate?.ToString("yyyy-MM-dd"),
        ["Search.DepartureAirportId"] = Search.DepartureAirportId.ToString(),
        ["Search.ArrivalAirportId"] = Search.ArrivalAirportId.ToString(),
        ["Search.DepartureDate"] = Search.DepartureDate.ToString("yyyy-MM-dd"),
        ["Search.Passengers"] = Search.Passengers.ToString(),
        ["MinPriceText"] = DinhDangTien(Search.MinPrice),
        ["MaxPriceText"] = DinhDangTien(Search.MaxPrice),
        ["Search.FareClassName"] = Search.FareClassName
    };
}
