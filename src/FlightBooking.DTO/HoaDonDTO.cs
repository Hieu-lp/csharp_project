using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

/// <summary>
/// DTO chứa dữ liệu hóa đơn (tương ứng bảng INVOICE trong CSDL).
/// </summary>
public class HoaDonDTO
{
    [JsonPropertyName("InvoiceId")]
    public int MaHoaDon { get; set; }

    [JsonPropertyName("BookingId")]
    public int MaDatVe { get; set; }

    [JsonPropertyName("InvoiceDate")]
    public DateTime NgayHoaDon { get; set; }

    [JsonPropertyName("Total")]
    public decimal TongTienGoc { get; set; }

    [JsonPropertyName("DiscountAmount")]
    public decimal TienGiam { get; set; }

    [JsonPropertyName("TaxAmount")]
    public decimal TienThue { get; set; }

    [JsonPropertyName("TotalAmount")]
    public decimal TongTien { get; set; }

    [JsonPropertyName("Status")]
    public string TrangThai { get; set; } = "UNPAID"; // UNPAID, PAID, CANCELLED

    // Thuộc tính hiển thị liên kết
    [JsonPropertyName("CustomerName")]
    public string TenKhachHang { get; set; } = string.Empty;

    [JsonPropertyName("CustomerPhone")]
    public string DienThoaiKhachHang { get; set; } = string.Empty;

    [JsonPropertyName("CustomerEmail")]
    public string EmailKhachHang { get; set; } = string.Empty;

    [JsonPropertyName("FlightId")]
    public int MaChuyenBay { get; set; }

    [JsonPropertyName("TicketCount")]
    public int SoLuongVe { get; set; }

    [JsonPropertyName("EmployeeId")]
    public int? MaNhanVien { get; set; }
    [JsonPropertyName("EmployeeName")]
    public string TenNhanVien { get; set; } = "";

    [JsonPropertyName("Details")]
    public List<ChiTietHoaDonDTO> DanhSachChiTiet { get; set; } = new();
}

/// <summary>
/// DTO chứa chi tiết hóa đơn (tương ứng bảng INVOICE_DETAIL trong CSDL).
/// </summary>
public class ChiTietHoaDonDTO
{
    [JsonPropertyName("InvoiceDetailId")]
    public int MaChiTietHoaDon { get; set; }

    [JsonPropertyName("InvoiceId")]
    public int MaHoaDon { get; set; }

    [JsonPropertyName("TicketId")]
    public int MaVe { get; set; }

    [JsonPropertyName("UnitPrice")]
    public decimal DonGia { get; set; }

    [JsonPropertyName("Quantity")]
    public int SoLuong { get; set; } = 1;

    [JsonPropertyName("Subtotal")]
    public decimal ThanhTien { get; set; }

    // Thuộc tính hiển thị liên kết
    [JsonPropertyName("SeatNumber")]
    public string SoGhe { get; set; } = string.Empty;

    [JsonPropertyName("ClassName")]
    public string TenHangVe { get; set; } = string.Empty;

    [JsonPropertyName("PassengerName")]
    public string TenHanhKhach { get; set; } = string.Empty;

    [JsonPropertyName("PassengerIdCard")]
    public string SoGiayTo { get; set; } = string.Empty;
}
