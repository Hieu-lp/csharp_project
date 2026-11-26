using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

/// <summary>
/// DTO tổng hợp số liệu thống kê doanh thu và vé.
/// </summary>
public class ThongKeDoanhThuDTO
{
    [JsonPropertyName("TotalRevenue")]
    public decimal TongDoanhThu { get; set; }

    [JsonPropertyName("PaidRevenue")]
    public decimal DoanhThuDaThu { get; set; }

    [JsonPropertyName("UnpaidRevenue")]
    public decimal DoanhThuChuaThu { get; set; }

    [JsonPropertyName("TotalTicketsSold")]
    public int TongSoVeBan { get; set; }

    [JsonPropertyName("TotalInvoices")]
    public int TongSoHoaDon { get; set; }

    [JsonPropertyName("PaidInvoicesCount")]
    public int SoHoaDonDaThanhToan { get; set; }

    [JsonPropertyName("UnpaidInvoicesCount")]
    public int SoHoaDonChuaThanhToan { get; set; }

    [JsonPropertyName("CancelledInvoicesCount")]
    public int SoHoaDonDaHuy { get; set; }

    [JsonPropertyName("QuarterlyRevenue")]
    public List<DoanhThuTheoQuyDTO> DoanhThuTheoQuy { get; set; } = new();

    [JsonPropertyName("FlightRevenue")]
    public List<DoanhThuTheoChuyenBayDTO> DoanhThuTheoChuyenBay { get; set; } = new();

    // Chưa có sổ chi phí được duyệt: null nghĩa là chưa xác định, không phải bằng 0.
    public decimal? TongChi { get; set; }
    public decimal? LoiNhuan { get; set; }
    public List<DoanhThuTheoKyDTO> TheoThoiGian { get; set; } = new();
    public List<DoanhThuTheoKyDTO> TheoNam { get; set; } = new();
    public List<MaTranDoanhThuDTO> TheoNhanVien { get; set; } = new();
    public List<MaTranDoanhThuDTO> TheoKhachHang { get; set; } = new();
    public List<MaTranDoanhThuDTO> TheoChuyenBay { get; set; } = new();
    public List<MaTranDoanhThuDTO> TheoHangVe { get; set; } = new();
}

public sealed class BoLocThongKeDTO
{
    public DateTime? TuNgay { get; set; }
    public DateTime? DenNgay { get; set; }
    public int? Nam { get; set; }
    public int? Thang { get; set; }
    public int? Quy { get; set; }
}

public sealed class DoanhThuTheoKyDTO
{
    public int Nam { get; set; }
    public int Thang { get; set; }
    public int Quy { get; set; }
    public int SoHoaDon { get; set; }
    public int SoVe { get; set; }
    public decimal DoanhThu { get; set; }
}

public sealed class MaTranDoanhThuDTO
{
    public string Ma { get; set; } = "";
    public string Ten { get; set; } = "";
    public int Nam { get; set; }
    public decimal Quy1 { get; set; }
    public decimal Quy2 { get; set; }
    public decimal Quy3 { get; set; }
    public decimal Quy4 { get; set; }
    public decimal Tong { get; set; }
}

/// <summary>
/// DTO doanh thu theo từng quý trong năm.
/// </summary>
public class DoanhThuTheoQuyDTO
{
    [JsonPropertyName("Year")]
    public int Nam { get; set; }

    [JsonPropertyName("Quarter")]
    public int Quy { get; set; }

    [JsonPropertyName("InvoiceCount")]
    public int SoHoaDon { get; set; }

    [JsonPropertyName("TicketCount")]
    public int SoVe { get; set; }

    [JsonPropertyName("Revenue")]
    public decimal DoanhThu { get; set; }
}

/// <summary>
/// DTO doanh thu theo từng chuyến bay.
/// </summary>
public class DoanhThuTheoChuyenBayDTO
{
    [JsonPropertyName("FlightId")]
    public int MaChuyenBay { get; set; }

    [JsonPropertyName("Route")]
    public string TuyenBay { get; set; } = string.Empty;

    [JsonPropertyName("TicketsSold")]
    public int SoVeDaBan { get; set; }

    [JsonPropertyName("Revenue")]
    public decimal DoanhThu { get; set; }
}
