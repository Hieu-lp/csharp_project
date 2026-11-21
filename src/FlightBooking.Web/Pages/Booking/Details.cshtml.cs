using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class DetailsModel(BookingBUS bus) : PageModel
{
    public IReadOnlyList<BookingDTO> Bookings { get; private set; } = [];
    public string? AccessKey { get; private set; }
    public string? Error { get; private set; }
    [TempData] public string? Notice { get; set; }

    public IActionResult OnGet(int id)
    {
        AccessKey = HttpContext.Session.GetString("booking:" + id);
        if (AccessKey is null) return RedirectToPage("Lookup");
        try
        {
            Bookings = bus.GetJourney(id, AccessKey);
            if (Bookings.Count == 0) return NotFound();
        }
        catch (BookingException e) { Error = e.Message; }
        return Page();
    }

    public IActionResult OnPostCancel(int id)
    {
        AccessKey = HttpContext.Session.GetString("booking:" + id);
        if (AccessKey is null) return RedirectToPage("Lookup");
        try
        {
            bus.CancelJourney(id, AccessKey);
            Notice = "Hành trình đã hủy. Ghế và lượt giảm giá đã được hoàn đúng một lần.";
            return RedirectToPage(new { id });
        }
        catch (BookingException e) { Error = e.Message; }
        try { Bookings = bus.GetJourney(id, AccessKey); } catch (BookingException) { }
        return Page();
    }
}
