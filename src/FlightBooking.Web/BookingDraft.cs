using System.Text.Json;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.Web;

public sealed class BookingDraft
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string AccessKey { get; set; } = BookingBUS.NewAccessKey();
    public BookingRequest Request { get; set; } = new();
    public int? CompletedBookingId { get; set; }
    public string? AppliedDiscountState { get; set; }
    public static BookingDraft? Load(ISession session, string? id) =>
        id is { Length: 32 } && session.GetString("checkout:" + id) is string json
            ? JsonSerializer.Deserialize<BookingDraft>(json) : null;
    public void Save(ISession session) => session.SetString("checkout:" + Id, JsonSerializer.Serialize(this));
}
