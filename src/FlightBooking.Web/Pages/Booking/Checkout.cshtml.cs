using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class CheckoutModel(BookingBUS bus) : PageModel
{
    [BindProperty] public BookingRequest Input { get; set; } = new();
    [BindProperty] public string AccessKey { get; set; } = "";
    public BookingQuoteDTO? Quote { get; private set; }
    public string? Error { get; private set; }

    public IActionResult OnGet(int flightId, int fareId, int quantity)
    {
        try
        {
            Quote = bus.Quote(flightId, fareId, quantity, null);
            Input = new() { FlightId = flightId, FareId = fareId, ExpectedTotal = Quote.TotalAmount,
                Passengers = Enumerable.Range(0, quantity).Select(_ => new PassengerInput()).ToList() };
            AccessKey = BookingBUS.NewAccessKey();
            HttpContext.Session.SetString("draft:" + AccessKey, $"{flightId}:{fareId}:{quantity}");
        }
        catch (BookingException e) { Error = e.Message; }
        return Page();
    }

    public IActionResult OnPost(string action)
    {
        try
        {
            if (AccessKey?.Length != 64 || Input.Passengers is null ||
                HttpContext.Session.GetString("draft:" + AccessKey) != $"{Input.FlightId}:{Input.FareId}:{Input.Passengers.Count}")
                throw new BookingException("Biểu mẫu đã hết phiên hoặc không hợp lệ. Vui lòng tìm và chọn chuyến lại.");
            if (action == "quote")
            {
                Quote = bus.Quote(Input.FlightId, Input.FareId, Input.Passengers.Count, Input.DiscountCode);
                ModelState.Clear();
                Input.ExpectedTotal = Quote.TotalAmount;
                return Page();
            }
            if (!ModelState.IsValid) { LoadQuote(); return Page(); }
            var bookingId = bus.Create(Input, AccessKey);
            HttpContext.Session.SetString("booking:" + bookingId, AccessKey);
            // Retain the draft so a repeated POST resolves to the same durable request.
            return RedirectToPage("Details", new { id = bookingId });
        }
        catch (BookingException e) { Error = e.Message; LoadQuote(); return Page(); }
    }

    private void LoadQuote()
    {
        if (Input.Passengers?.Count is not (>= 1 and <= 9)) return;
        try { Quote = bus.Quote(Input.FlightId, Input.FareId, Input.Passengers.Count, Input.DiscountCode); }
        catch (BookingException) { /* Retain submitted fields and the original error. */ }
    }
}
