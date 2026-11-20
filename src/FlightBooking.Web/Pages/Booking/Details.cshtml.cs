using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class DetailsModel(BookingBUS bus) : PageModel
{
    public BookingDTO? Booking { get; private set; }
    public string? AccessKey { get; private set; }
    public string? Error { get; private set; }
    [TempData] public string? Notice { get; set; }

    public IActionResult OnGet(int id)
    {
        AccessKey = HttpContext.Session.GetString("booking:" + id);
        if (AccessKey is null) return RedirectToPage("Lookup");
        try
        {
            Booking = bus.Get(id, AccessKey);
            if (Booking is null) return NotFound();
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
            bus.Cancel(id, AccessKey);
            Notice = "Đơn đã hủy. Ghế và lượt giảm giá đã được hoàn đúng một lần.";
            return RedirectToPage(new { id });
        }
        catch (BookingException e) { Error = e.Message; }
        try { Booking = bus.Get(id, AccessKey); } catch (BookingException) { }
        return Page();
    }
}
