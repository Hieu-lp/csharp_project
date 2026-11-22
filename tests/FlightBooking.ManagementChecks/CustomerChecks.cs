using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.ManagementChecks;

internal static class CustomerChecks
{
    public static void Run()
    {
        var bus = new CustomerBUS();

        CustomerDTO missingName = ValidCustomer();
        missingName.FullName = "   ";
        CheckAssert.ErrorContains(bus.Add(missingName), "Họ tên", "Customer rejects a blank name before database access");

        CustomerDTO badEmail = ValidCustomer();
        badEmail.Email = "not-an-email";
        CheckAssert.ErrorContains(bus.Add(badEmail), "Email", "Customer rejects an invalid email before database access");

        CustomerDTO futureBirthDate = ValidCustomer();
        futureBirthDate.DateOfBirth = DateTime.Today.AddDays(1);
        CheckAssert.ErrorContains(bus.Add(futureBirthDate), "Ngày sinh", "Customer rejects a future birth date before database access");
    }

    private static CustomerDTO ValidCustomer() => new()
    {
        FullName = "Nguyen An",
        Phone = "0900000000",
        Email = "an@example.test",
        DateOfBirth = new DateTime(2000, 1, 1),
        Gender = "Nam"
    };
}
