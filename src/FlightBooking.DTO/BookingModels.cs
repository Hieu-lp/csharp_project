using System.ComponentModel.DataAnnotations;

namespace FlightBooking.DTO;

public sealed record AirportDTO(int AirportId, string AirportName, string City);
public sealed record FlightDTO(int FlightId, int DepartureAirportId, int ArrivalAirportId,
    string DepartureCity, string ArrivalCity, string DepartureAirport, string ArrivalAirport,
    DateOnly DepartureDate, TimeOnly DepartureTime, DateOnly ArrivalDate, TimeOnly ArrivalTime,
    int TotalSeats, int SeatsAvailable, string Status)
{
    public DateTime DepartsAt => DepartureDate.ToDateTime(DepartureTime);
    public DateTime ArrivesAt => ArrivalDate.ToDateTime(ArrivalTime);
}
public sealed record FareClassDTO(int FareId, int FlightId, string ClassName, decimal Price, int BaseBaggageKg);
public sealed record FlightOfferDTO(FlightDTO Flight, FareClassDTO Fare);
public sealed record FlightSelectionDTO(BookingQuoteDTO Quote)
{
    public bool HasSeats => Quote.Flight.SeatsAvailable >= Quote.Quantity;
}
public sealed record DiscountCodeDTO(int DiscountId, string Code, string Type, decimal Value,
    decimal MinOrderAmount, decimal? MaxDiscountAmount, DateTime StartDate, DateTime EndDate,
    int? UsageLimit, int UsedCount, string Status);

public sealed class CustomerInput
{
    [Required(ErrorMessage = "Nhập họ tên khách đặt."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập số điện thoại."), StringLength(20), RegularExpression(@"\+?[0-9]{8,15}", ErrorMessage = "Số điện thoại cần 8–15 chữ số, có thể bắt đầu bằng +.")]
    public string Phone { get; set; } = "";
    [Required(ErrorMessage = "Nhập email."), EmailAddress(ErrorMessage = "Email không hợp lệ."), StringLength(100, ErrorMessage = "Email tối đa 100 ký tự.")]
    public string Email { get; set; } = "";
    [Required(ErrorMessage = "Nhập ngày sinh khách đặt."), DataType(DataType.Date)]
    public DateOnly? DateOfBirth { get; set; }
    [Required(ErrorMessage = "Chọn giới tính.")]
    public string Gender { get; set; } = "";
    private string? idPassportNumber;
    [Required(ErrorMessage = "Nhập CCCD hoặc hộ chiếu người đặt."), StringLength(50, ErrorMessage = "Giấy tờ tối đa 50 ký tự."), RegularExpression(@"[A-Za-z0-9-]+", ErrorMessage = "Giấy tờ chỉ dùng chữ Latin, số và dấu -.")]
    public string? IdPassportNumber { get => idPassportNumber; set => idPassportNumber = value?.Trim(); }
}

public sealed class PassengerInput
{
    [Required(ErrorMessage = "Nhập họ tên hành khách."), StringLength(100, ErrorMessage = "Họ tên tối đa 100 ký tự.")]
    public string FullName { get; set; } = "";
    [Required(ErrorMessage = "Nhập giấy tờ hành khách."), StringLength(50, ErrorMessage = "Giấy tờ tối đa 50 ký tự."), RegularExpression(@"[A-Za-z0-9-]+", ErrorMessage = "Giấy tờ chỉ dùng chữ Latin, số và dấu -.")]
    public string IdCard { get; set; } = "";
}

public sealed class BookingRequest
{
    public int Quantity { get; set; } = 1;
    public string TripType { get; set; } = "OneWay";
    public DateOnly? ReturnDate { get; set; }
    public int? ReturnFlightId { get; set; }
    public int? ReturnFareId { get; set; }
    public int FlightId { get; set; }
    public int FareId { get; set; }
    public CustomerInput Customer { get; set; } = new();
    public List<PassengerInput> Passengers { get; set; } = [];
    [StringLength(50), RegularExpression(@"[A-Za-z0-9_-]*")]
    public string? DiscountCode { get; set; }
    public decimal ExpectedTotal { get; set; }
}

public sealed record BookingQuoteDTO(FlightDTO Flight, FareClassDTO Fare, int Quantity,
    decimal Subtotal, decimal DiscountAmount, decimal TotalAmount, int? DiscountId);
public sealed record JourneyQuoteDTO(BookingQuoteDTO Outbound, BookingQuoteDTO? Return)
{
    public decimal TotalAmount => Outbound.TotalAmount + (Return?.TotalAmount ?? 0);
}
public sealed record BookingDetailDTO(int BookingDetailId, int TicketId, string PassengerFullName,
    string PassengerIdCard, decimal Price, string SeatNumber, string ClassName, string TicketStatus);
public sealed record BookingDTO(int BookingId, int CustomerId, int FlightId, int? DiscountId,
    DateTime BookingDate, string BookingSource, int TicketQuantity, decimal DiscountAmount,
    decimal TotalAmount, string Status, string CustomerName, string Email, string Phone,
    string InvoiceStatus, FlightDTO Flight, IReadOnlyList<BookingDetailDTO> Details);
public sealed class BookingException : Exception
{
    public BookingException(string message) : base(message)
    {
    }

    public BookingException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed class FlightSearch
{
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? FareClassName { get; set; }
    public string TripType { get; set; } = "OneWay";
    public DateOnly? ReturnDate { get; set; }
    public int DepartureAirportId { get; set; }
    public int ArrivalAirportId { get; set; }
    public DateOnly DepartureDate { get; set; }
    public DateOnly? ArrivalDate { get; set; }
    public int Passengers { get; set; } = 1;
    public string Operator { get; set; } = "<=";
    public decimal? Price { get; set; }
    public string? ClassName { get; set; }
    public string Join { get; set; } = "AND";
    public bool Not { get; set; }
}
