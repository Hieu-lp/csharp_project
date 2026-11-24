using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class DiscountCodeChecks
{
    public static void Run()
    {
        var bus = new DiscountCodeBUS();

        DiscountCodeDTO overOneHundredPercent = ValidDiscount();
        overOneHundredPercent.DiscountValue = 100.01m;
        CheckAssert.ErrorContains(bus.Add(overOneHundredPercent), "100", "Discount rejects a percent above 100 before database access");

        DiscountCodeDTO reversedPeriod = ValidDiscount();
        reversedPeriod.StartDate = new DateTime(2026, 12, 10);
        reversedPeriod.EndDate = new DateTime(2026, 12, 9);
        CheckAssert.ErrorContains(bus.Add(reversedPeriod), "Ngày kết thúc", "Discount rejects an end date before start date");

        DiscountCodeDTO negativeUsageLimit = ValidDiscount();
        negativeUsageLimit.UsageLimit = -1;
        CheckAssert.ErrorContains(bus.Add(negativeUsageLimit), "Giới hạn lượt", "Discount rejects a negative usage limit");

        DiscountCodeDTO unsupportedType = ValidDiscount();
        unsupportedType.DiscountType = "BOGO";
        CheckAssert.ErrorContains(bus.Add(unsupportedType), "PERCENT", "Discount rejects an unsupported discount type");
    }

    private static DiscountCodeDTO ValidDiscount() => new()
    {
        Code = "CHECK10",
        Description = "Test validation only",
        DiscountType = "PERCENT",
        DiscountValue = 10,
        MinOrderAmount = 0,
        StartDate = DateTime.Today,
        EndDate = DateTime.Today.AddDays(7),
        Status = "ACTIVE"
    };
}
