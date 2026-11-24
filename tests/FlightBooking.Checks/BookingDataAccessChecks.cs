using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;

internal static class BookingDataAccessChecks
{
    public static void Run(Action<bool, string> check, Func<BookingRequest> valid)
    {
        // Cấu hình rỗng/sai bị từ chối trước kết nối, không thể ghi SQL Server chính.
        var bus = new BookingBUS("");
        Action[] reads =
        [
            () => bus.GetAirports(), () => bus.GetFlight(1), () => bus.GetFareClassNames(),
            () => bus.GetCheapestFlights(), () => bus.Quote(1, 1, 1, null),
            () => bus.Get(1, new string('A', 64))
        ];
        for (int i = 0; i < reads.Length; i++)
        {
            check(HasCause<InvalidOperationException>(Failure(reads[i])), "Booking read API keeps configuration cause/stack " + i);
        }

        var empty = new BookingDAO("");
        bool executed = false;
        BookingException writing = Failure(() => empty.Write(data => { executed = true; return 1; }));
        check(HasCause<InvalidOperationException>(writing), "Booking Write compatibility uses the DAO exception boundary");
        check(!executed, "failed Booking connection cannot execute write callback or report success");
        BookingException deleting = Failure(() => empty.InTransaction(data => { executed = true; }));
        check(HasCause<InvalidOperationException>(deleting), "Booking void transaction keeps its original cause/stack");
        check(!executed, "failed Booking void transaction cannot execute its callback");

        var malformed = new BookingDAO("NotAConnectionKeyword=value");
        check(HasCause<ArgumentException>(Failure(() => malformed.Read(data => data.Airports()))), "malformed Booking read configuration is translated by DAO");
        check(HasCause<ArgumentException>(Failure(() => malformed.Write(data => 1))), "malformed Booking write configuration is translated by DAO");

        BookingException invalidKey = Failure(() => bus.Create(valid(), "invalid"));
        check(invalidKey.InnerException == null && invalidKey.Message == "Khóa tra cứu cần đủ 64 ký tự được cấp khi đặt vé.",
            "Create keeps access-key validation before opening a transaction");
    }

    private static bool HasCause<T>(BookingException error) where T : Exception
    {
        return error.Message == "Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB." &&
            error.InnerException is T cause && !string.IsNullOrEmpty(cause.StackTrace);
    }

    private static BookingException Failure(Action action)
    {
        try { action(); }
        catch (BookingException error) { return error; }
        throw new Exception("FAIL: expected a BookingException.");
    }
}
