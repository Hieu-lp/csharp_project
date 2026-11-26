using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

public sealed record SanBayDTO([property: JsonPropertyName("AirportId")] int MaSanBay,
    [property: JsonPropertyName("AirportName")] string TenSanBay,
    [property: JsonPropertyName("City")] string ThanhPho);
public sealed record ChuyenBayDTO([property: JsonPropertyName("FlightId")] int MaChuyenBay,
    [property: JsonPropertyName("DepartureAirportId")] int MaSanBayDi,
    [property: JsonPropertyName("ArrivalAirportId")] int MaSanBayDen,
    [property: JsonPropertyName("DepartureCity")] string ThanhPhoDi,
    [property: JsonPropertyName("ArrivalCity")] string ThanhPhoDen,
    [property: JsonPropertyName("DepartureAirport")] string SanBayDi,
    [property: JsonPropertyName("ArrivalAirport")] string SanBayDen,
    [property: JsonPropertyName("DepartureDate")] DateOnly NgayKhoiHanh,
    [property: JsonPropertyName("DepartureTime")] TimeOnly GioKhoiHanh,
    [property: JsonPropertyName("ArrivalDate")] DateOnly NgayDen,
    [property: JsonPropertyName("ArrivalTime")] TimeOnly GioDen,
    [property: JsonPropertyName("TotalSeats")] int TongSoGhe,
    [property: JsonPropertyName("SeatsAvailable")] int SoGheCon,
    [property: JsonPropertyName("Status")] string TrangThai)
{
    [JsonPropertyName("DepartsAt")]
    public DateTime ThoiDiemKhoiHanh => NgayKhoiHanh.ToDateTime(GioKhoiHanh);
    [JsonPropertyName("ArrivesAt")]
    public DateTime ThoiDiemDen => NgayDen.ToDateTime(GioDen);
}
public sealed record HangVeDTO([property: JsonPropertyName("FareId")] int MaHangVe,
    [property: JsonPropertyName("FlightId")] int MaChuyenBay,
    [property: JsonPropertyName("ClassName")] string TenHangVe,
    [property: JsonPropertyName("Price")] decimal GiaVe,
    [property: JsonPropertyName("BaseBaggageKg")] int HanhLyCoBanKg);
public sealed record LuaChonChuyenBayDTO([property: JsonPropertyName("Flight")] ChuyenBayDTO ChuyenBay,
    [property: JsonPropertyName("Fare")] HangVeDTO HangVe);
public sealed record ChuyenBayDaChonDTO([property: JsonPropertyName("Quote")] BaoGiaDatVeDTO BaoGia)
{
    [JsonPropertyName("HasSeats")]
    public bool ConDuGhe => BaoGia.ChuyenBay.SoGheCon >= BaoGia.SoLuong;
}
public sealed record MaGiamGiaDTO([property: JsonPropertyName("DiscountId")] int MaGiamGia,
    [property: JsonPropertyName("Code")] string Ma,
    [property: JsonPropertyName("Type")] string Loai,
    [property: JsonPropertyName("Value")] decimal GiaTri,
    [property: JsonPropertyName("MinOrderAmount")] decimal GiaTriDonToiThieu,
    [property: JsonPropertyName("MaxDiscountAmount")] decimal? GiamToiDa,
    [property: JsonPropertyName("StartDate")] DateTime NgayBatDau,
    [property: JsonPropertyName("EndDate")] DateTime NgayKetThuc,
    [property: JsonPropertyName("UsageLimit")] int? GioiHanSuDung,
    [property: JsonPropertyName("UsedCount")] int SoLanDaDung,
    [property: JsonPropertyName("Status")] string TrangThai);

// Giữ tên thuộc tính đầu vào để tương thích form HTTP và JSON nháp đang lưu.
public sealed class ThongTinKhachDat
{
    [Required(ErrorMessage = "Nhập họ tên khách đặt."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), StringLength(20), RegularExpression(@"\+?[0-9]{8,15}", ErrorMessage = "Số điện thoại cần 8–15 chữ số, có thể bắt đầu bằng +.")]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Nhập ngày sinh khách đặt."), DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
    [Required(ErrorMessage = "Chọn giới tính.")]
    public string Gender { get; set; } = "";
    private string? soGiayTo;
    [Required(ErrorMessage = "Nhập CCCD hoặc hộ chiếu người đặt."), StringLength(50, ErrorMessage = "Giấy tờ tối đa 50 ký tự."), RegularExpression(@"[A-Za-z0-9-]+", ErrorMessage = "Giấy tờ chỉ dùng chữ Latin, số và dấu -.")]
    public string? IdPassportNumber { get => soGiayTo; set => soGiayTo = value?.Trim(); }
}

public sealed class ThongTinHanhKhach
{
    [Required(ErrorMessage = "Nhập họ tên hành khách."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập giấy tờ hành khách."), StringLength(50, ErrorMessage = "Giấy tờ tối đa 50 ký tự."), RegularExpression(@"[A-Za-z0-9-]+", ErrorMessage = "Giấy tờ chỉ dùng chữ Latin, số và dấu -.")]
    public string IdCard { get; set; } = "";
}

public sealed class YeuCauDatVe
{
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? Seats { get; set; }
    [JsonIgnore(Condition = JsonIgnoreCondition.WhenWritingNull)]
    public List<string>? ReturnSeats { get; set; }
    public int Quantity { get; set; } = 1;
    public string TripType { get; set; } = "OneWay";
    public DateOnly? ReturnDate { get; set; }
    public int? ReturnFlightId { get; set; }
    public int? ReturnFareId { get; set; }
    public int FlightId { get; set; }
    public int FareId { get; set; }
    public ThongTinKhachDat Customer { get; set; } = new();
    public List<ThongTinHanhKhach> Passengers { get; set; } = [];
    [StringLength(50), RegularExpression(@"[A-Za-z0-9_-]*")]
    public string? DiscountCode { get; set; }
    public decimal ExpectedTotal { get; set; }
}

public sealed record BaoGiaDatVeDTO([property: JsonPropertyName("Flight")] ChuyenBayDTO ChuyenBay,
    [property: JsonPropertyName("Fare")] HangVeDTO HangVe,
    [property: JsonPropertyName("Quantity")] int SoLuong,
    [property: JsonPropertyName("Subtotal")] decimal TamTinh,
    [property: JsonPropertyName("DiscountAmount")] decimal TienGiam,
    [property: JsonPropertyName("TotalAmount")] decimal TongTien,
    [property: JsonPropertyName("DiscountId")] int? MaGiamGia);
public sealed record BaoGiaHanhTrinhDTO([property: JsonPropertyName("Outbound")] BaoGiaDatVeDTO LuotDi,
    [property: JsonPropertyName("Return")] BaoGiaDatVeDTO? LuotVe)
{
    [JsonPropertyName("TotalAmount")]
    public decimal TongTien => LuotDi.TongTien + (LuotVe?.TongTien ?? 0);
}
public sealed record ChiTietDatVeDTO([property: JsonPropertyName("BookingDetailId")] int MaChiTietDatVe,
    [property: JsonPropertyName("TicketId")] int MaVe,
    [property: JsonPropertyName("PassengerFullName")] string HoTenHanhKhach,
    [property: JsonPropertyName("PassengerIdCard")] string GiayToHanhKhach,
    [property: JsonPropertyName("Price")] decimal GiaVe,
    [property: JsonPropertyName("SeatNumber")] string SoGhe,
    [property: JsonPropertyName("ClassName")] string TenHangVe,
    [property: JsonPropertyName("TicketStatus")] string TrangThaiVe);
public sealed record DatVeDTO([property: JsonPropertyName("BookingId")] int MaDatVe,
    [property: JsonPropertyName("CustomerId")] int MaKhachHang,
    [property: JsonPropertyName("FlightId")] int MaChuyenBay,
    [property: JsonPropertyName("DiscountId")] int? MaGiamGia,
    [property: JsonPropertyName("BookingDate")] DateTime NgayDat,
    [property: JsonPropertyName("BookingSource")] string NguonDat,
    [property: JsonPropertyName("TicketQuantity")] int SoLuongVe,
    [property: JsonPropertyName("DiscountAmount")] decimal TienGiam,
    [property: JsonPropertyName("TotalAmount")] decimal TongTien,
    [property: JsonPropertyName("Status")] string TrangThai,
    [property: JsonPropertyName("CustomerName")] string TenKhachHang, string Email,
    [property: JsonPropertyName("Phone")] string SoDienThoai,
    [property: JsonPropertyName("InvoiceStatus")] string TrangThaiHoaDon,
    [property: JsonPropertyName("Flight")] ChuyenBayDTO ChuyenBay,
    [property: JsonPropertyName("Details")] IReadOnlyList<ChiTietDatVeDTO> ChiTiet);
public sealed class LoiDatVe : Exception
{
    public LoiDatVe(string thongBao) : base(thongBao)
    {
    }

    public LoiDatVe(string thongBao, Exception loiGoc) : base(thongBao, loiGoc)
    {
    }
}

public sealed class DieuKienTimChuyenBay
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? FareClassName { get; set; }
    public string TripType { get; set; } = "OneWay";
    public DateOnly? ReturnDate { get; set; }
    public int DepartureAirportId { get; set; }
    public int ArrivalAirportId { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public int Passengers { get; set; } = 1;
    public string Operator { get; set; } = "<=";
    public decimal? Price { get; set; }
    public string? ClassName { get; set; }
    public string Join { get; set; } = "AND";
    public bool Not { get; set; }
}
