using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class DetailsModel : PageModel
{
    private readonly DatVeBUS nghiepVu;

    public DetailsModel(DatVeBUS nghiepVu)
    {
        this.nghiepVu = nghiepVu;
    }

    public IReadOnlyList<DatVeDTO> Bookings { get; private set; } = [];
    public string? AccessKey { get; private set; }
    public string? Error { get; private set; }
    [TempData] public string? Notice { get; set; }

    public IActionResult OnGet(int id)
    {
        AccessKey = HttpContext.Session.GetString("booking:" + id);
        if (AccessKey is null) return RedirectToPage("Lookup");
        try
        {
            Bookings = nghiepVu.TraCuuHanhTrinh(id, AccessKey);
            if (Bookings.Count == 0) return NotFound();
        }
        catch (LoiDatVe loi) { Error = loi.Message; }
        return Page();
    }

    public IActionResult OnPostCancel(int id)
    {
        AccessKey = HttpContext.Session.GetString("booking:" + id);
        if (AccessKey is null) return RedirectToPage("Lookup");
        try
        {
            nghiepVu.HuyHanhTrinh(id, AccessKey);
            Notice = "Hành trình đã hủy. Ghế và lượt giảm giá đã được hoàn đúng một lần.";
            return RedirectToPage(new { id });
        }
        catch (LoiDatVe loi) { Error = loi.Message; }
        try { Bookings = nghiepVu.TraCuuHanhTrinh(id, AccessKey); }
        catch (LoiDatVe)
        {
            // Lần đọc lại chỉ bổ sung màn hình; thông báo lỗi hủy gốc ở trên vẫn được hiển thị.
        }
        return Page();
    }
}
