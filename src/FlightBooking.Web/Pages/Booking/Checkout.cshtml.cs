using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace FlightBooking.Web.Pages.Booking;

public class CheckoutModel : PageModel
{
    private readonly BookingBUS bus;

    public CheckoutModel(BookingBUS bus)
    {
        this.bus = bus;
    }

    [BindProperty] public BookingRequest Input { get; set; } = new();
    [BindProperty] public string DraftId { get; set; } = "";
    public JourneyQuoteDTO? Quote { get; private set; }
    public string? Error { get; private set; }
    public string? DiscountMessage { get; private set; }
    public DateOnly Today => bus.Today;
    public DateOnly LatestBirthDate => BookingBUS.LatestContactBirthDate(Today);

    public IActionResult OnGet(int flightId, int fareId, int quantity, string tripType = "OneWay",
        DateOnly? returnDate = null, int? returnFlightId = null, int? returnFareId = null, string? draftId = null)
    {
        var draft = BookingDraft.Load(HttpContext.Session, draftId) ?? new BookingDraft();
        if (draft.CompletedBookingId is int completed) return RedirectToPage("Details", new { id = completed });
        try
        {
            Input = draft.Request;
            var oldSelection = Selection(Input);
            Input.FlightId = flightId; Input.FareId = fareId; Input.TripType = tripType;
            Input.ReturnDate = returnDate; Input.ReturnFlightId = returnFlightId; Input.ReturnFareId = returnFareId;
            BookingBUS.NormalizeJourney(Input);
            BookingBUS.ResizePassengers(Input, quantity);
            if (oldSelection != Selection(Input)) { draft.AccessKey = BookingBUS.NewAccessKey(); draft.AppliedDiscountState = null; }
            DraftId = draft.Id;
            draft.Save(HttpContext.Session);
            Quote = bus.QuoteJourney(Input);
            Input.ExpectedTotal = Quote.TotalAmount;
            if (draft.AppliedDiscountState != DiscountState(Input, Quote)) draft.AppliedDiscountState = null;
            draft.Save(HttpContext.Session);
        }
        catch (BookingException e) { Error = e.Message; }
        return Page();
    }

    private static string Selection(BookingRequest input)
    {
        return $"{input.FlightId}:{input.FareId}:{input.Quantity}:{input.TripType}:{input.ReturnDate}:{input.ReturnFlightId}:{input.ReturnFareId}";
    }

    private static string DiscountState(BookingRequest input, JourneyQuoteDTO quote)
    {
        return System.Text.Json.JsonSerializer.Serialize(new { Journey = Selection(input), Code = BookingBUS.NormalizeCode(input.DiscountCode),
            quote.Outbound.Subtotal, quote.Outbound.DiscountId, quote.Outbound.DiscountAmount,
            ReturnSubtotal = quote.Return?.Subtotal, quote.TotalAmount });
    }

    public IActionResult OnPost(string action)
    {
        if (string.IsNullOrEmpty(action)) action = "quote";
        var draft = BookingDraft.Load(HttpContext.Session, DraftId);
        try
        {
            if (action is not ("book" or "quote" or "back")) throw new BookingException("Thao tác không hợp lệ. Muốn đổi số người, quay lại tìm chuyến.");
            if (draft is null || Selection(Input) != Selection(draft.Request) || Input.Passengers is null || Input.Passengers.Count != draft.Request.Quantity)
                throw new BookingException("Biểu mẫu đã hết phiên hoặc số hành khách/hành trình không khớp. Hãy chọn chuyến lại.");
            if (draft.CompletedBookingId is int completed) return RedirectToPage("Details", new { id = completed });
            draft.Request = Input;
            draft.Save(HttpContext.Session);
            if (action == "back")
            {
                // Bản nháp đã lưu ở trên, kể cả khi thông tin người đặt chưa nhập đủ.
                return BackToSearch();
            }
            if (BookingBUS.ContactBirthDateError(Input.Customer?.DateOfBirth, Today) is { } birthError)
            {
                if (ModelState.TryGetValue("Input.Customer.DateOfBirth", out var birthState)) birthState.Errors.Clear();
                ModelState.AddModelError("Input.Customer.DateOfBirth", birthError);
            }
            var appliedState = draft.AppliedDiscountState;
            draft.AppliedDiscountState = null;
            Input.DiscountCode = BookingBUS.NormalizeCode(Input.DiscountCode);
            ModelState.Remove("Input.DiscountCode");
            draft.Save(HttpContext.Session);
            if (action == "quote")
            {
                return PreviewQuote(draft, appliedState);
            }
            // Chỉ xác nhận khi model hợp lệ. Giá/ghế/transaction vẫn do BUS và DAO xử lý.
            if (!ModelState.IsValid) { LoadQuote(); return Page(); }
            return ConfirmBooking(draft);
        }
        catch (BookingException e) { Error = e.Message; LoadQuote(); draft?.Save(HttpContext.Session); return Page(); }
    }

    private IActionResult BackToSearch()
    {
        FlightDTO flight = bus.GetFlight(Input.FlightId);
        return RedirectToPage("/Index", new Microsoft.AspNetCore.Routing.RouteValueDictionary
        {
            ["Submitted"] = "true", ["DraftId"] = DraftId,
            ["Search.DepartureAirportId"] = flight.DepartureAirportId.ToString(),
            ["Search.ArrivalAirportId"] = flight.ArrivalAirportId.ToString(),
            ["Search.DepartureDate"] = flight.DepartureDate.ToString("yyyy-MM-dd"),
            ["Search.Passengers"] = Input.Quantity.ToString(), ["Search.TripType"] = Input.TripType,
            ["Search.ReturnDate"] = Input.ReturnDate?.ToString("yyyy-MM-dd")
        });
    }

    private IActionResult PreviewQuote(BookingDraft draft, string? appliedState)
    {
        // Nhận giá từ BUS; PageModel không tự tính giá hoặc cập nhật số ghế.
        Quote = bus.QuoteJourney(Input);
        Input.ExpectedTotal = Quote.TotalAmount;
        ModelState.Remove("Input.ExpectedTotal");
        if (Quote.Outbound.DiscountId is not null)
        {
            string state = DiscountState(Input, Quote);
            DiscountMessage = appliedState == state ? "Mã giảm giá này đã được áp dụng" : "Đã áp dụng mã giảm giá.";
            draft.AppliedDiscountState = state;
        }
        else DiscountMessage = "Đã cập nhật giá, không áp dụng mã giảm giá.";
        draft.Save(HttpContext.Session);
        return Page();
    }

    private IActionResult ConfirmBooking(BookingDraft draft)
    {
        int bookingId = bus.Create(Input, draft.AccessKey);
        HttpContext.Session.SetString("booking:" + bookingId, draft.AccessKey);
        draft.CompletedBookingId = bookingId;
        draft.Save(HttpContext.Session);
        return RedirectToPage("Details", new { id = bookingId });
    }

    private void LoadQuote()
    {
        if (Input.Passengers?.Count is not (>= 1 and <= 9)) return;
        try { Quote = bus.QuoteJourney(Input); }
        catch (BookingException)
        {
            var code = Input.DiscountCode;
            try { Input.DiscountCode = null; Quote = bus.QuoteJourney(Input); }
            catch (BookingException)
            {
                // Quote dự phòng không bắt buộc: giữ lỗi gốc/ModelState để khách sửa biểu mẫu.
            }
            finally { Input.DiscountCode = code; }
        }
    }
}
