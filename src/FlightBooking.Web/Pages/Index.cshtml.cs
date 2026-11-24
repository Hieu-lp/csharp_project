using System.Collections;
using System.Globalization;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages;

public class IndexModel : PageModel
{
    private readonly BookingBUS bus;

    public IndexModel(BookingBUS bus)
    {
        this.bus = bus;
    }

    [BindProperty(SupportsGet = true)] public FlightSearch Search { get; set; } = new();
    [BindProperty(SupportsGet = true)] public bool Submitted { get; set; }
    [BindProperty(SupportsGet = true)] public string? DraftId { get; set; }
    [BindProperty(SupportsGet = true)] public int OutboundFlightId { get; set; }
    [BindProperty(SupportsGet = true)] public int OutboundFareId { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedOffer { get; set; }
    [BindProperty(SupportsGet = true)] public string? SelectedDeal { get; set; }
    [BindProperty(SupportsGet = true)] public int DealFlightId { get; set; }
    [BindProperty(SupportsGet = true)] public int DealFareId { get; set; }
    [BindProperty(SupportsGet = true)] public bool ContinueDeal { get; set; }
    [BindProperty(SupportsGet = true)] public string? MinPriceText { get; set; }
    [BindProperty(SupportsGet = true)] public string? MaxPriceText { get; set; }
    [BindProperty(SupportsGet = true)] public string? FilterAction { get; set; }
    public bool SelectingReturn => Search.TripType == "RoundTrip" && OutboundFlightId > 0;
    public bool IsDealSelection { get; private set; }
    public FlightSelectionDTO? DealSelection { get; private set; }
    public BookingQuoteDTO? Outbound { get; private set; }
    public IReadOnlyList<AirportDTO> Airports { get; private set; } = [];
    public IReadOnlyList<string> FareClassNames { get; private set; } = [];
    public IReadOnlyList<FlightOfferDTO> Deals { get; private set; } = [];
    public ArrayList Offers { get; private set; } = [];
    public string? Error { get; private set; }
    private static string? Money(decimal? amount) => amount?.ToString("#,0.##", CultureInfo.GetCultureInfo("vi-VN"));

    public IActionResult OnGet()
    {
        IsDealSelection = Request.Query.ContainsKey(nameof(DealFlightId)) || Request.Query.ContainsKey(nameof(DealFareId));
        try
        {
            if (SelectedDeal is not null)
            {
                return RedirectDeal(SelectedDeal);
            }
            if (IsDealSelection)
            {
                return ShowDealSelection();
            }

            PrepareSearchFilters();
            if (!Submitted) Search.DepartureDate = DateOnly.FromDateTime(
                TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime).AddDays(1);
            BookingBUS.ValidateSearchDates(Search);
            if (!string.IsNullOrEmpty(Search.FareClassName) && !FareClassNames.Contains(Search.FareClassName, StringComparer.OrdinalIgnoreCase))
                throw new BookingException("Hạng vé không có trong dữ liệu hiện tại. Hãy chọn lại hoặc xóa bộ lọc.");
            if (!Submitted) { Deals = bus.GetCheapestFlights(); return Page(); }
            if (!ModelState.IsValid) { Error = "Điều kiện tìm kiếm không hợp lệ."; return Page(); }
            if (SelectingReturn)
            {
                Offers = SearchReturningFlights();
            }
            else Offers = bus.Search(Search);
            if (SelectedOffer is not null)
            {
                return RedirectSelectedOffer(SelectedOffer);
            }
        }
        catch (BookingException e) { Error = e.Message; }
        return Page();
    }

    private IActionResult RedirectDeal(string selection)
    {
        var (flight, fare) = ParseOffer(selection);
        ValidatePassengers();
        bus.SelectFlight(flight, fare, Search.Passengers);
        return RedirectToPage("/Index", null, new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            ["DealFlightId"] = flight, ["DealFareId"] = fare,
            ["Search.Passengers"] = Search.Passengers, ["DraftId"] = DraftId
        }, "flight-results");
    }

    private IActionResult ShowDealSelection()
    {
        ValidatePassengers();
        if (DealFlightId <= 0 || DealFareId <= 0 || !ModelState.IsValid)
            throw new BookingException("Mã chuyến/hạng vé hoặc số hành khách không hợp lệ. Hãy chọn chuyến khác.");
        DealSelection = bus.SelectFlight(DealFlightId, DealFareId, Search.Passengers);
        BookingQuoteDTO quote = DealSelection.Quote;
        // DTO từ BUS quyết định tuyến/ngày/giá; không lấy các giá trị này từ query của trình duyệt.
        Search = new FlightSearch
        {
            DepartureAirportId = quote.Flight.DepartureAirportId,
            ArrivalAirportId = quote.Flight.ArrivalAirportId,
            DepartureDate = quote.Flight.DepartureDate, Passengers = quote.Quantity, TripType = "OneWay"
        };
        Submitted = true;
        OutboundFlightId = 0;
        OutboundFareId = 0;
        MinPriceText = null;
        MaxPriceText = null;
        ModelState.Clear();
        Airports = bus.GetAirports();
        if (ContinueDeal && DealSelection.HasSeats)
            return RedirectToPage("/Booking/Checkout", new { flightId = DealFlightId, fareId = DealFareId, quantity = Search.Passengers, tripType = "OneWay", draftId = DraftId });
        return Page();
    }

    private void PrepareSearchFilters()
    {
        // Giữ danh sách lựa chọn khi biểu mẫu có lỗi; phép lọc nâng cao vẫn thực hiện tại BUS.
        Airports = bus.GetAirports();
        FareClassNames = bus.GetFareClassNames();
        Search.Price = null;
        Search.ClassName = null;
        Search.Operator = "<=";
        Search.Join = "AND";
        Search.Not = false;
        foreach (string key in new[] { "Search.Price", "Search.ClassName", "Search.Operator", "Search.Join", "Search.Not", "Search.MinPrice", "Search.MaxPrice" })
            ModelState.Remove(key);
        if (FilterAction == "clear")
        {
            MinPriceText = null;
            MaxPriceText = null;
            Search.FareClassName = null;
            foreach (string key in new[] { nameof(MinPriceText), nameof(MaxPriceText), "Search.FareClassName" }) ModelState.Remove(key);
        }
        Search.MinPrice = BookingBUS.ParseVnd(MinPriceText);
        Search.MaxPrice = BookingBUS.ParseVnd(MaxPriceText);
        BookingBUS.ValidatePriceRange(Search);
        MinPriceText = Money(Search.MinPrice);
        MaxPriceText = Money(Search.MaxPrice);
        ModelState.Remove(nameof(MinPriceText));
        ModelState.Remove(nameof(MaxPriceText));
    }

    private ArrayList SearchReturningFlights()
    {
        Outbound = bus.Quote(OutboundFlightId, OutboundFareId, Search.Passengers, null);
        if (Outbound.Flight.DepartureAirportId != Search.DepartureAirportId ||
            Outbound.Flight.ArrivalAirportId != Search.ArrivalAirportId ||
            Outbound.Flight.DepartureDate != Search.DepartureDate)
            throw new BookingException("Lượt đi không khớp hành trình tìm kiếm.");
        var returning = new FlightSearch
        {
            DepartureAirportId = Search.ArrivalAirportId, ArrivalAirportId = Search.DepartureAirportId,
            DepartureDate = Search.ReturnDate!.Value, Passengers = Search.Passengers,
            MinPrice = Search.MinPrice, MaxPrice = Search.MaxPrice, FareClassName = Search.FareClassName
        };
        ArrayList result = new ArrayList();
        foreach (FlightOfferDTO offer in bus.Search(returning))
        {
            if (offer.Flight.DepartsAt > Outbound.Flight.ArrivesAt) result.Add(offer);
        }
        return result;
    }

    private IActionResult RedirectSelectedOffer(string selection)
    {
        var (flight, fare) = ParseOffer(selection);
        FlightOfferDTO offer = Offers.Cast<FlightOfferDTO>().SingleOrDefault(o => o.Flight.FlightId == flight && o.Fare.FareId == fare)
            ?? throw new BookingException("Chuyến/hạng vé không còn phù hợp với tìm kiếm và số người hiện tại. Hãy chọn lại.");
        var route = new Microsoft.AspNetCore.Routing.RouteValueDictionary();
        foreach (var item in SelectionRoute(offer)) route[item.Key] = item.Value;
        return RedirectToPage(SelectionPage, route);
    }

    private void ValidatePassengers()
    {
        if (Search.Passengers is < 1 or > 9 || ModelState.TryGetValue("Search.Passengers", out var state) && state.Errors.Count > 0)
            throw new BookingException("Chọn số hành khách từ 1 đến 9.");
    }
    private static (int Flight, int Fare) ParseOffer(string text)
    {
        var parts = text.Split(':');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var flight) || !int.TryParse(parts[1], out var fare) || flight <= 0 || fare <= 0)
            throw new BookingException("Lựa chọn chuyến/hạng vé không hợp lệ.");
        return (flight, fare);
    }

    public string SelectionPage => Search.TripType == "RoundTrip" && !SelectingReturn ? "/Index" : "/Booking/Checkout";
    public Dictionary<string, string?> SelectionRoute(FlightOfferDTO offer)
    {
        if (SelectionPage == "/Index")
        {
            var route = SearchRoute();
            route["OutboundFlightId"] = offer.Flight.FlightId.ToString(); route["OutboundFareId"] = offer.Fare.FareId.ToString();
            return route;
        }
        return new()
        {
            ["flightId"] = (SelectingReturn ? OutboundFlightId : offer.Flight.FlightId).ToString(),
            ["fareId"] = (SelectingReturn ? OutboundFareId : offer.Fare.FareId).ToString(),
            ["quantity"] = Search.Passengers.ToString(), ["tripType"] = Search.TripType,
            ["returnDate"] = Search.ReturnDate?.ToString("yyyy-MM-dd"), ["draftId"] = DraftId,
            ["returnFlightId"] = SelectingReturn ? offer.Flight.FlightId.ToString() : null,
            ["returnFareId"] = SelectingReturn ? offer.Fare.FareId.ToString() : null
        };
    }

    public Dictionary<string, string?> SearchRoute() => new()
    {
        ["Submitted"] = "true", ["DraftId"] = DraftId,
        ["Search.TripType"] = Search.TripType, ["Search.ReturnDate"] = Search.ReturnDate?.ToString("yyyy-MM-dd"),
        ["Search.DepartureAirportId"] = Search.DepartureAirportId.ToString(), ["Search.ArrivalAirportId"] = Search.ArrivalAirportId.ToString(),
        ["Search.DepartureDate"] = Search.DepartureDate.ToString("yyyy-MM-dd"), ["Search.Passengers"] = Search.Passengers.ToString(),
        ["MinPriceText"] = Money(Search.MinPrice), ["MaxPriceText"] = Money(Search.MaxPrice), ["Search.FareClassName"] = Search.FareClassName
    };
}
