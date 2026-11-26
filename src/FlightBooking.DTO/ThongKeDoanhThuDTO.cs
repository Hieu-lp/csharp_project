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
