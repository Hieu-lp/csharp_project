using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages;

public class IndexModel(BookingBUS bus) : PageModel
{
    [BindProperty(SupportsGet = true)] public FlightSearch Search { get; set; } = new();
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }
    public IReadOnlyList<AirportDTO> Airports { get; private set; } = [];
    public ArrayList Offers { get; private set; } = [];
    public string? Error { get; private set; }
    public void OnGet()
    {
        if (!Submitted) Search.DepartureDate = DateOnly.FromDateTime(
            TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime).AddDays(1);
        try
        {
            Airports = bus.GetAirports();
            if (Submitted && ModelState.IsValid) Offers = bus.Search(Search);
            else if (!ModelState.IsValid) Error = "Điều kiện tìm kiếm không hợp lệ.";
        }
        catch (BookingException e) { Error = e.Message; }
    }
}
