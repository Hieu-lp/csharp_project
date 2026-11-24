using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;

internal static class BookingDataAccessChecks
{
    public static void Run(Action<bool, string> check, Func<YeuCauDatVe> valid)
    {
        // Cấu hình rỗng/sai bị từ chối trước kết nối, không thể ghi SQL Server chính.
        var bus = new DatVeBUS("");
        Action[] reads =
        [
            () => bus.DocDSSanBay(), () => bus.DocChuyenBay(1), () => bus.DocTenHangVe(),
            () => bus.DocChuyenGiaThap(), () => bus.TinhGiaVe(1, 1, 1, null),
            () => bus.TraCuuDatVe(1, new string('A', 64))
        ];
        for (int i = 0; i < reads.Length; i++)
        {
            check(HasCause<InvalidOperationException>(Failure(reads[i])), "Booking read API keeps configuration cause/stack " + i);
        }

        var empty = new DatVeDAO("");
        bool executed = false;
        LoiDatVe writing = Failure(() => empty.GhiDuLieu(data => { executed = true; return 1; }));
        check(HasCause<InvalidOperationException>(writing), "Booking Write compatibility uses the DAO exception boundary");
        check(!executed, "failed Booking connection cannot execute write callback or report success");
        LoiDatVe deleting = Failure(() => empty.TrongGiaoDich(data => { executed = true; }));
        check(HasCause<InvalidOperationException>(deleting), "Booking void transaction keeps its original cause/stack");
        check(!executed, "failed Booking void transaction cannot execute its callback");

        var malformed = new DatVeDAO("NotAConnectionKeyword=value");
        check(HasCause<ArgumentException>(Failure(() => malformed.DocDuLieu(data => data.DocDSSanBay()))), "malformed Booking read configuration is translated by DAO");
        check(HasCause<ArgumentException>(Failure(() => malformed.GhiDuLieu(data => 1))), "malformed Booking write configuration is translated by DAO");

        LoiDatVe invalidKey = Failure(() => bus.TaoDatVe(valid(), "invalid"));
        check(invalidKey.InnerException == null && invalidKey.Message == "Khóa tra cứu cần đủ 64 ký tự được cấp khi đặt vé.",
            "Create keeps access-key validation before opening a transaction");
    }

    private static bool HasCause<T>(LoiDatVe error) where T : Exception
    {
        return error.Message == "Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB." &&
            error.InnerException is T cause && !string.IsNullOrEmpty(cause.StackTrace);
    }

    private static LoiDatVe Failure(Action action)
    {
        try { action(); }
        catch (LoiDatVe error) { return error; }
        throw new Exception("FAIL: expected a BookingException.");
    }
}
