using FlightBooking.BUS;
using FlightBooking.DTO;

internal static class CrudChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Chỉ kiểm tra DTO/định tuyến API; cấu hình rỗng không thể ghi SQL.
        var bus = new ManagementBUS("");
        var airport = new AirportDTO(0, "Airport", "City");
        var flight = new FlightEdit(0, 1, 2, 120, new(2026, 12, 31), new(8, 0),
            new(2026, 12, 31), new(10, 0), "SCHEDULED");
        check(Message(() => bus.AddAirport(airport with { AirportId = 1 })) == "Mã sân bay không hợp lệ.",
            "AddAirport rejects an existing ID before accessing SQL");
        check(Message(() => bus.UpdateAirport(airport, null)) == "Mã sân bay không hợp lệ.",
            "UpdateAirport requires an existing ID before accessing SQL");
        check(Message(() => bus.SaveAirport(airport with { AirportId = -1 })) == "Mã sân bay không hợp lệ.",
            "legacy SaveAirport retains the negative-ID validation");
        check(Message(() => bus.AddFlight(flight with { FlightId = 1 })) == "Mã chuyến bay phải bằng 0 khi thêm mới.",
            "AddFlight rejects an existing ID before accessing SQL");
        check(Message(() => bus.UpdateFlight(flight, null)) == "Mã chuyến bay phải lớn hơn 0 khi chỉnh sửa.",
            "UpdateFlight requires an existing ID before accessing SQL");
        check(Message(() => bus.SaveFlight(flight with { FlightId = -1 })) == "Chọn hai sân bay đi/đến khác nhau.",
            "legacy SaveFlight retains its original validation and message");
    }

    private static string Message(Action action)
    {
        try { action(); }
        catch (ManagementException error) { return error.Message; }
        throw new Exception("FAIL: expected a ManagementException.");
    }
}
