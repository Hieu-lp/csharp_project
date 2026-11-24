using System.Text.Json.Serialization;
namespace FlightBooking.DTO;

public sealed record QuanLyChuyenBayDTO([property: JsonPropertyName("Flight")] ChuyenBayDTO ChuyenBay,
    [property: JsonPropertyName("TicketsSold")] int SoVeDaBan,
    [property: JsonPropertyName("TotalRevenue")] decimal TongDoanhThu);
public sealed record ThongTinSuaChuyenBay([property: JsonPropertyName("FlightId")] int MaChuyenBay,
    [property: JsonPropertyName("DepartureAirportId")] int MaSanBayDi,
    [property: JsonPropertyName("ArrivalAirportId")] int MaSanBayDen,
    [property: JsonPropertyName("TotalSeats")] int TongSoGhe,
    [property: JsonPropertyName("DepartureDate")] DateOnly NgayKhoiHanh,
    [property: JsonPropertyName("DepartureTime")] TimeOnly GioKhoiHanh,
    [property: JsonPropertyName("ArrivalDate")] DateOnly NgayDen,
    [property: JsonPropertyName("ArrivalTime")] TimeOnly GioDen,
    [property: JsonPropertyName("Status")] string TrangThai);
public sealed record DieuKienLoc([property: JsonPropertyName("Field")] string Truong,
    [property: JsonPropertyName("Operator")] string PhepSoSanh,
    [property: JsonPropertyName("Value")] string GiaTri);
public sealed class LoiQuanLy : Exception
{
    public LoiQuanLy(string thongBao) : base(thongBao)
    {
    }

    // DAO giữ nguyên lỗi gốc và stack trace trong InnerException để chẩn đoán.
    public LoiQuanLy(string thongBao, Exception loiGoc) : base(thongBao, loiGoc)
    {
    }
}
