using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class EmployeeChecks
{
    public static void Run()
    {
        var bus = new NhanVienBUS();

        NhanVienDTO missingPosition = ValidEmployee();
        missingPosition.ChucVu = " ";
        CheckAssert.ErrorContains(bus.ThemNhanVien(missingPosition), "Chức vụ", "Employee rejects a blank position before database access");

        NhanVienDTO badStatus = ValidEmployee();
        badStatus.TrangThai = "PAUSED";
        CheckAssert.ErrorContains(bus.ThemNhanVien(badStatus), "Trạng thái", "Employee rejects an unsupported status before database access");

        NhanVienDTO badEmail = ValidEmployee();
        badEmail.Email = "not-an-email";
        CheckAssert.ErrorContains(bus.ThemNhanVien(badEmail), "Email", "Employee rejects an invalid email before database access");
    }

    private static NhanVienDTO ValidEmployee() => new()
    {
        HoTen = "Nhan vien Demo",
        DienThoai = "0900000001",
        Email = "staff@example.test",
        ChucVu = "Nhân viên",
        NgayVaoLam = new DateTime(2024, 1, 1),
        TrangThai = "ACTIVE"
    };
}
