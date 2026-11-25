using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class CheckoutModel : PageModel
{
    private readonly DatVeBUS nghiepVu;

    public CheckoutModel(DatVeBUS nghiepVu)
    {
        this.nghiepVu = nghiepVu;
    }

    [BindProperty] public YeuCauDatVe Input { get; set; } = new();
    [BindProperty] public string DraftId { get; set; } = "";
    public BaoGiaHanhTrinhDTO? Quote { get; private set; }
    public string? Error { get; private set; }
    public string? DiscountMessage { get; private set; }
    public DateOnly Today => nghiepVu.HomNay;
    public DateOnly LatestBirthDate => DatVeBUS.NgaySinhKhachDatToiDa(Today);

    public IActionResult OnGet(int flightId, int fareId, int quantity, string tripType = "OneWay",
        DateOnly? returnDate = null, int? returnFlightId = null, int? returnFareId = null, string? draftId = null)
    {
        var banNhap = DatVeTam.Doc(HttpContext.Session, draftId) ?? new DatVeTam();
        if (banNhap.CompletedBookingId is int maDaDat) return RedirectToPage("Details", new { id = maDaDat });
        try
        {
            Input = banNhap.Request;
            var luaChonCu = ChuoiHanhTrinh(Input);
            Input.FlightId = flightId; Input.FareId = fareId; Input.TripType = tripType;
            Input.ReturnDate = returnDate; Input.ReturnFlightId = returnFlightId; Input.ReturnFareId = returnFareId;
            DatVeBUS.ChuanHoaHanhTrinh(Input);
            DatVeBUS.DieuChinhSoHanhKhach(Input, quantity);
            if (luaChonCu != ChuoiHanhTrinh(Input)) { banNhap.AccessKey = DatVeBUS.TaoKhoaTraCuu(); banNhap.AppliedDiscountState = null; }
            DraftId = banNhap.Id;
            banNhap.Luu(HttpContext.Session);
            Quote = nghiepVu.TinhGiaHanhTrinh(Input);
            Input.ExpectedTotal = Quote.TongTien;
            if (banNhap.AppliedDiscountState != TrangThaiGiamGia(Input, Quote)) banNhap.AppliedDiscountState = null;
            banNhap.Luu(HttpContext.Session);
        }
        catch (LoiDatVe loi) { Error = loi.Message; }
        return Page();
    }

    private static string ChuoiHanhTrinh(YeuCauDatVe dauVao)
    {
        return $"{dauVao.FlightId}:{dauVao.FareId}:{dauVao.Quantity}:{dauVao.TripType}:{dauVao.ReturnDate}:{dauVao.ReturnFlightId}:{dauVao.ReturnFareId}";
    }

    private static string TrangThaiGiamGia(YeuCauDatVe dauVao, BaoGiaHanhTrinhDTO baoGia)
    {
        return System.Text.Json.JsonSerializer.Serialize(new
        {
            Journey = ChuoiHanhTrinh(dauVao),
            Code = DatVeBUS.ChuanHoaMa(dauVao.DiscountCode),
            Subtotal = baoGia.LuotDi.TamTinh,
            DiscountId = baoGia.LuotDi.MaGiamGia,
            DiscountAmount = baoGia.LuotDi.TienGiam,
            ReturnSubtotal = baoGia.LuotVe?.TamTinh,
            TotalAmount = baoGia.TongTien
        });
    }

    public IActionResult OnPost(string action)
    {
        if (string.IsNullOrEmpty(action)) action = "quote";
        var banNhap = DatVeTam.Doc(HttpContext.Session, DraftId);
        try
        {
            if (action is not ("book" or "quote" or "back")) throw new LoiDatVe("Thao tác không hợp lệ. Muốn đổi số người, quay lại tìm chuyến.");
            if (banNhap is null || ChuoiHanhTrinh(Input) != ChuoiHanhTrinh(banNhap.Request) || Input.Passengers is null || Input.Passengers.Count != banNhap.Request.Quantity)
                throw new LoiDatVe("Biểu mẫu đã hết phiên hoặc số hành khách/hành trình không khớp. Hãy chọn chuyến lại.");
            if (banNhap.CompletedBookingId is int maDaDat) return RedirectToPage("Details", new { id = maDaDat });
            banNhap.Request = Input;
            banNhap.Luu(HttpContext.Session);
            if (action == "back")
            {
                // Bản nháp đã lưu ở trên, kể cả khi thông tin người đặt chưa nhập đủ.
                return QuayLaiTimKiem();
            }
            if (DatVeBUS.LoiNgaySinhKhachDat(Input.Customer?.DateOfBirth, Today) is { } loiNgaySinh)
            {
                if (ModelState.TryGetValue("Input.Customer.DateOfBirth", out var trangThaiNgaySinh)) trangThaiNgaySinh.Errors.Clear();
                ModelState.AddModelError("Input.Customer.DateOfBirth", loiNgaySinh);
            }
            var trangThaiDaApDung = banNhap.AppliedDiscountState;
            banNhap.AppliedDiscountState = null;
            Input.DiscountCode = DatVeBUS.ChuanHoaMa(Input.DiscountCode);
            ModelState.Remove("Input.DiscountCode");
            banNhap.Luu(HttpContext.Session);
            if (action == "quote")
            {
                return XemBaoGia(banNhap, trangThaiDaApDung);
            }
            // Chỉ xác nhận khi model hợp lệ. Giá/ghế/transaction vẫn do BUS và DAO xử lý.
            if (!ModelState.IsValid) { TaiBaoGia(); return Page(); }
            return XacNhanDatVe(banNhap);
        }
        catch (LoiDatVe loi) { Error = loi.Message; TaiBaoGia(); banNhap?.Luu(HttpContext.Session); return Page(); }
    }

    private IActionResult QuayLaiTimKiem()
    {
        ChuyenBayDTO chuyenBay = nghiepVu.DocChuyenBay(Input.FlightId);
        return RedirectToPage("/Index", new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            ["Submitted"] = "true",
            ["DraftId"] = DraftId,
            ["Search.DepartureAirportId"] = chuyenBay.MaSanBayDi.ToString(),
            ["Search.ArrivalAirportId"] = chuyenBay.MaSanBayDen.ToString(),
            ["Search.DepartureDate"] = chuyenBay.NgayKhoiHanh.ToString("yyyy-MM-dd"),
            ["Search.Passengers"] = Input.Quantity.ToString(),
            ["Search.TripType"] = Input.TripType,
            ["Search.ReturnDate"] = Input.ReturnDate?.ToString("yyyy-MM-dd")
        });
    }

    private IActionResult XemBaoGia(DatVeTam banNhap, string? trangThaiDaApDung)
    {
        // Nhận giá từ BUS; PageModel không tự tính giá hoặc cập nhật số ghế.
        Quote = nghiepVu.TinhGiaHanhTrinh(Input);
        Input.ExpectedTotal = Quote.TongTien;
        ModelState.Remove("Input.ExpectedTotal");
        if (Quote.LuotDi.MaGiamGia is not null)
        {
            string trangThai = TrangThaiGiamGia(Input, Quote);
            DiscountMessage = trangThaiDaApDung == trangThai ? "Mã giảm giá này đã được áp dụng" : "Đã áp dụng mã giảm giá.";
            banNhap.AppliedDiscountState = trangThai;
        }
        else DiscountMessage = "Đã cập nhật giá, không áp dụng mã giảm giá.";
        banNhap.Luu(HttpContext.Session);
        return Page();
    }

    private IActionResult XacNhanDatVe(DatVeTam banNhap)
    {
        int maDatVe = nghiepVu.TaoDatVe(Input, banNhap.AccessKey);
        HttpContext.Session.SetString("booking:" + maDatVe, banNhap.AccessKey);
        banNhap.CompletedBookingId = maDatVe;
        banNhap.Luu(HttpContext.Session);
        return RedirectToPage("Details", new { id = maDatVe });
    }

    private void TaiBaoGia()
    {
        if (Input.Passengers?.Count is not (>= 1 and <= 9)) return;
        try { Quote = nghiepVu.TinhGiaHanhTrinh(Input); }
        catch (LoiDatVe)
        {
            var maGiamGia = Input.DiscountCode;
            try { Input.DiscountCode = null; Quote = nghiepVu.TinhGiaHanhTrinh(Input); }
            catch (LoiDatVe)
            {
                // Quote dự phòng không bắt buộc: giữ lỗi gốc/ModelState để khách sửa biểu mẫu.
            }
            finally { Input.DiscountCode = maGiamGia; }
        }
    }
}
