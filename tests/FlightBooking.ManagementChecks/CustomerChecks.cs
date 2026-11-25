using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class CustomerChecks
{
    public static void Run()
    {
        var bus = new KhachHangBUS();

        KhachHangDTO missingName = ValidCustomer();
        missingName.HoTen = "   ";
        CheckAssert.ErrorContains(bus.ThemKhachHang(missingName), "Họ tên", "Customer rejects a blank name before database access");

        KhachHangDTO badEmail = ValidCustomer();
        badEmail.Email = "not-an-email";
        CheckAssert.ErrorContains(bus.ThemKhachHang(badEmail), "Email", "Customer rejects an invalid email before database access");

        KhachHangDTO futureBirthDate = ValidCustomer();
        futureBirthDate.NgaySinh = DateTime.Today.AddDays(1);
        CheckAssert.ErrorContains(bus.ThemKhachHang(futureBirthDate), "Ngày sinh", "Customer rejects a future birth date before database access");

        KhachHangDTO invalidGender = ValidCustomer();
        invalidGender.GioiTinh = "Other";
        CheckAssert.ErrorContains(bus.ThemKhachHang(invalidGender), "Giới tính", "Customer rejects an unsupported gender before database access");
    }

    private static KhachHangDTO ValidCustomer() => new()
    {
        HoTen = "Nguyen An",
        DienThoai = "0900000000",
        Email = "an@example.test",
        NgaySinh = new DateTime(2000, 1, 1),
        GioiTinh = "Nam"
    };
}
