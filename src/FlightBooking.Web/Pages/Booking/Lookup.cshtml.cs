using System.ComponentModel.DataAnnotations;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class LookupModel(BookingBUS bus) : PageModel
{
    [BindProperty, Range(1, int.MaxValue, ErrorMessage = "Mã đơn phải là số nguyên dương.")] public int BookingId { get; set; }
    [BindProperty, Required(ErrorMessage = "Nhập khóa tra cứu."), StringLength(64, MinimumLength = 64, ErrorMessage = "Khóa tra cứu cần đủ 64 ký tự.")] public string AccessKey { get; set; } = "";
    public string? Error { get; private set; }
    public void OnGet() { }
    public IActionResult OnPost()
    {
        if (!ModelState.IsValid) return Page();
        try
        {
            if (bus.Get(BookingId, AccessKey.Trim()) is null) throw new BookingException("Mã đơn hoặc khóa tra cứu không đúng.");
            HttpContext.Session.SetString("booking:" + BookingId, AccessKey.Trim());
            return RedirectToPage("Details", new { id = BookingId });
        }
        catch (BookingException e) { Error = e.Message; return Page(); }
    }
}
