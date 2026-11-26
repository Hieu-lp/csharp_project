using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

/// <summary>
/// DTO chứa dữ liệu vé máy bay (tương ứng bảng TICKET trong CSDL).
/// </summary>
public class VeMayBayDTO
{
    [JsonPropertyName("TicketId")]
    public int MaVe { get; set; }

    [JsonPropertyName("FlightId")]
    public int MaChuyenBay { get; set; }

    [JsonPropertyName("FareId")]
    public int MaHangVe { get; set; }

    [JsonPropertyName("SeatNumber")]
    public string SoGhe { get; set; } = string.Empty;

    [JsonPropertyName("IssuedAt")]
    public DateTime NgayPhatHanh { get; set; }

    [JsonPropertyName("Status")]
    public string TrangThai { get; set; } = "ISSUED"; // ISSUED hoặc CANCELLED

    // Các thuộc tính mở rộng phục vụ hiển thị trên giao diện
    [JsonPropertyName("ClassName")]
    public string TenHangVe { get; set; } = string.Empty;

    [JsonPropertyName("Price")]
    public decimal GiaVe { get; set; }

    [JsonPropertyName("DepartureAirport")]
    public string SanBayDi { get; set; } = string.Empty;

    [JsonPropertyName("ArrivalAirport")]
    public string SanBayDen { get; set; } = string.Empty;

    [JsonPropertyName("DepartureDate")]
    public DateOnly? NgayKhoiHanh { get; set; }

    [JsonPropertyName("DepartureTime")]
    public TimeOnly? GioKhoiHanh { get; set; }

    [JsonPropertyName("PassengerName")]
    public string TenHanhKhach { get; set; } = string.Empty;

    [JsonPropertyName("PassengerIdCard")]
    public string SoGiayTo { get; set; } = string.Empty;

    [JsonPropertyName("BookingId")]
    public int? MaDatVe { get; set; }
}
