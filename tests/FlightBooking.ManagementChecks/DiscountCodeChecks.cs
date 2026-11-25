using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class DiscountCodeChecks
{
    public static void Run()
    {
        var bus = new MaGiamGiaBUS();

        MaGiamGiaQuanLyDTO overOneHundredPercent = ValidDiscount();
        overOneHundredPercent.GiaTriGiam = 100.01m;
        CheckAssert.ErrorContains(bus.ThemMaGiamGia(overOneHundredPercent), "100", "Discount rejects a percent above 100 before database access");

        MaGiamGiaQuanLyDTO reversedPeriod = ValidDiscount();
        reversedPeriod.NgayBatDau = new DateTime(2026, 12, 10);
        reversedPeriod.NgayKetThuc = new DateTime(2026, 12, 9);
        CheckAssert.ErrorContains(bus.ThemMaGiamGia(reversedPeriod), "Ngày kết thúc", "Discount rejects an end date before start date");

        MaGiamGiaQuanLyDTO negativeUsageLimit = ValidDiscount();
        negativeUsageLimit.GioiHanLuotDung = -1;
        CheckAssert.ErrorContains(bus.ThemMaGiamGia(negativeUsageLimit), "Giới hạn lượt", "Discount rejects a negative usage limit");

        MaGiamGiaQuanLyDTO unsupportedType = ValidDiscount();
        unsupportedType.LoaiGiam = "BOGO";
        CheckAssert.ErrorContains(bus.ThemMaGiamGia(unsupportedType), "PERCENT", "Discount rejects an unsupported discount type");
    }

    private static MaGiamGiaQuanLyDTO ValidDiscount() => new()
    {
        Ma = "CHECK10",
        MoTa = "Test validation only",
        LoaiGiam = "PERCENT",
        GiaTriGiam = 10,
        DonToiThieu = 0,
        NgayBatDau = DateTime.Today,
        NgayKetThuc = DateTime.Today.AddDays(7),
        TrangThai = "ACTIVE"
    };
}
