using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;

internal static class ExceptionChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Các cấu hình này bị từ chối trước khi kết nối; không dùng SQL Server chính.
        var empty = new ManagementDAO("");
        ManagementException read = Failure(() => empty.Read(data => data.Airports()));
        check(HasCause<InvalidOperationException>(read), "DAO read retains missing-configuration cause and stack trace");

        bool executed = false;
        ManagementException write = Failure(() => empty.Write(data => { executed = true; return 1; }));
        check(HasCause<InvalidOperationException>(write), "DAO write retains missing-configuration cause and stack trace");
        check(!executed, "failed connection never executes the write callback or returns success");

        ManagementException deleting = Failure(() => empty.InTransaction(data => { executed = true; }));
        check(HasCause<InvalidOperationException>(deleting), "void transaction uses the same exception boundary");
        check(!executed, "failed void transaction never executes its callback");

        var malformed = new ManagementDAO("NotAConnectionKeyword=value");
        check(HasCause<ArgumentException>(Failure(() => malformed.Read(data => data.Airports()))),
            "malformed read connection has a management message and original cause");
        check(HasCause<ArgumentException>(Failure(() => malformed.Write(data => 1))),
            "malformed write connection has a management message and original cause");

        ManagementException validation = Failure(() => new ManagementBUS("").SaveAirport(new(0, "", "City")));
        check(validation.InnerException == null && validation.Message == "Tên sân bay bắt buộc, tối đa 150 ký tự.",
            "BUS validation retains its specific message before opening a connection");
    }

    private static bool HasCause<T>(ManagementException error) where T : Exception
    {
        return error.Message == "Chưa cấu hình hoặc chưa kết nối được FlightBookingDB." &&
            error.InnerException is T cause && !string.IsNullOrEmpty(cause.StackTrace);
    }

    private static ManagementException Failure(Action action)
    {
        try { action(); }
        catch (ManagementException error) { return error; }
        throw new Exception("FAIL: expected a ManagementException.");
    }
}
