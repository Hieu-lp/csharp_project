using System.Text.Json;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.Web;

public sealed class DatVeTam
{
    // Tên JSON của nháp và khóa session được giữ để đọc được phiên đã có.
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AccessKey { get; set; } = DatVeBUS.TaoKhoaTraCuu();
    public YeuCauDatVe Request { get; set; } = new();
    public int? CompletedBookingId { get; set; }
    public string? AppliedDiscountState { get; set; }
    public static DatVeTam? Doc(ISession phien, string? ma)
    {
        if (ma is not { Length: 32 }) return null;
        string? json = phien.GetString("checkout:" + ma);
        if (json is null) return null;
        return JsonSerializer.Deserialize<DatVeTam>(json);
    }

    public void Luu(ISession phien)
    {
        phien.SetString("checkout:" + Id, JsonSerializer.Serialize(this));
    }
}
