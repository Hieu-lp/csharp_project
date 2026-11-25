using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class ArrayListSearchChecks
{
    public static void Run()
    {
        var customers = new List<KhachHangDTO>
        {
            new() { MaKhachHang = 1, HoTen = "Lan Nguyen", DienThoai = "0901" },
            new() { MaKhachHang = 2, HoTen = "An Tran", DienThoai = "0902" },
            new() { MaKhachHang = 3, HoTen = "Binh Le", DienThoai = "0903" }
        };

        var containsName = new ArrayList
        {
            new DieuKienTimKiemDTO
            {
                Truong = "FullName",
                PhepSoSanh = "CONTAINS",
                GiaTri = "an",
                PhepNoi = "AND"
            }
        };
        ArrayList nameResults = TimKiemArrayList.LocDanhSach(customers, containsName, GetCustomerField);
        CheckAssert.True(nameResults.Count == 2, "ArrayList search matches text without case sensitivity");

        var combinedConditions = new ArrayList
        {
            new DieuKienTimKiemDTO
            {
                Truong = "CustomerId",
                PhepSoSanh = ">=",
                GiaTri = "2",
                PhepNoi = "AND"
            },
            new DieuKienTimKiemDTO
            {
                Truong = "FullName",
                PhepSoSanh = "CONTAINS",
                GiaTri = "Binh",
                PhepNoi = "AND"
            }
        };
        ArrayList combinedResults = TimKiemArrayList.LocDanhSach(customers, combinedConditions, GetCustomerField);
        CheckAssert.True(combinedResults.Count == 1 && ((KhachHangDTO)combinedResults[0]!).MaKhachHang == 3,
            "ArrayList search combines numeric and text conditions");
    }

    private static object? GetCustomerField(KhachHangDTO customer, string field) =>
        field.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "CUSTOMERID" => customer.MaKhachHang,
            "FULLNAME" => customer.HoTen,
            "PHONE" => customer.DienThoai,
            _ => throw new ArgumentException($"Unknown test field '{field}'.", nameof(field))
        };
}
