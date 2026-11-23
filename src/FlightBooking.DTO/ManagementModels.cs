namespace FlightBooking.DTO;

public sealed record FlightManagementDTO(FlightDTO Flight, int TicketsSold, decimal TotalRevenue);
public sealed record FlightEdit(int FlightId, int DepartureAirportId, int ArrivalAirportId, int TotalSeats,
    DateOnly DepartureDate, TimeOnly DepartureTime, DateOnly ArrivalDate, TimeOnly ArrivalTime, string Status);
public sealed record ManagementFilter(string Field, string Operator, string Value);
public sealed class ManagementException : Exception
{
    public ManagementException(string message) : base(message)
    {
    }

    // DAO giữ nguyên lỗi gốc và stack trace trong InnerException để chẩn đoán.
    public ManagementException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
