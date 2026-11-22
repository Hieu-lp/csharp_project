namespace FlightBooking.ManagementChecks;

internal static class Program
{
    private static void Main()
    {
        CustomerChecks.Run();
        EmployeeChecks.Run();
        DiscountCodeChecks.Run();
        ArrayListSearchChecks.Run();

        Console.WriteLine($"PASS: Tất cả {CheckAssert.PassedCount} ManagementChecks đều đạt.");
    }
}
