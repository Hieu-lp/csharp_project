using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;

internal static class ExceptionChecks
{
    public static void Run(Action<bool, string> check)
    {
        // Các cấu hình này bị từ chối trước khi kết nối; không dùng SQL Server chính.
        var empty = new QuanLyDAO("");
        LoiQuanLy read = Failure(() => empty.DocDuLieu(data => data.DocDSSanBay()));
        check(HasCause<InvalidOperationException>(read), "DAO read retains missing-configuration cause and stack trace");

        bool executed = false;
        LoiQuanLy write = Failure(() => empty.GhiDuLieu(data => { executed = true; return 1; }));
        check(HasCause<InvalidOperationException>(write), "DAO write retains missing-configuration cause and stack trace");
        check(!executed, "failed connection never executes the write callback or returns success");

        LoiQuanLy deleting = Failure(() => empty.TrongGiaoDich(data => { executed = true; }));
        check(HasCause<InvalidOperationException>(deleting), "void transaction uses the same exception boundary");
        check(!executed, "failed void transaction never executes its callback");

        var malformed = new QuanLyDAO("NotAConnectionKeyword=value");
        check(HasCause<ArgumentException>(Failure(() => malformed.DocDuLieu(data => data.DocDSSanBay()))),
            "malformed read connection has a management message and original cause");
        check(HasCause<ArgumentException>(Failure(() => malformed.GhiDuLieu(data => 1))),
            "malformed write connection has a management message and original cause");

        LoiQuanLy validation = Failure(() => new QuanLyBUS("").LuuSanBay(new(0, "", "City")));
        check(validation.InnerException == null && validation.Message == "Tên sân bay bắt buộc, tối đa 150 ký tự.",
            "BUS validation retains its specific message before opening a connection");
    }

    private static bool HasCause<T>(LoiQuanLy error) where T : Exception
    {
        return error.Message == "Chưa cấu hình hoặc chưa kết nối được FlightBookingDB." &&
            error.InnerException is T cause && !string.IsNullOrEmpty(cause.StackTrace);
    }

    private static LoiQuanLy Failure(Action action)
    {
        try { action(); }
        catch (LoiQuanLy error) { return error; }
        throw new Exception("FAIL: expected a ManagementException.");
    }
}
