using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class EmployeeChecks
{
    public static void Run()
    {
        var bus = new EmployeeBUS();

        EmployeeDTO missingPosition = ValidEmployee();
        missingPosition.Position = " ";
        CheckAssert.ErrorContains(bus.Add(missingPosition), "Chức vụ", "Employee rejects a blank position before database access");

        EmployeeDTO badStatus = ValidEmployee();
        badStatus.Status = "PAUSED";
        CheckAssert.ErrorContains(bus.Add(badStatus), "Trạng thái", "Employee rejects an unsupported status before database access");

        EmployeeDTO badEmail = ValidEmployee();
        badEmail.Email = "not-an-email";
        CheckAssert.ErrorContains(bus.Add(badEmail), "Email", "Employee rejects an invalid email before database access");
    }

    private static EmployeeDTO ValidEmployee() => new()
    {
        FullName = "Nhan vien Demo",
        Phone = "0900000001",
        Email = "staff@example.test",
        Position = "Nhân viên",
        HireDate = new DateTime(2024, 1, 1),
        Status = "ACTIVE"
    };
}
