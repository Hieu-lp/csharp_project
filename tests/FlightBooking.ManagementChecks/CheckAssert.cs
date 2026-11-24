namespace FlightBooking.ManagementChecks;

internal static class CheckAssert
{
    private static int _passed;

    public static void ErrorContains(string? actualError, string expectedText, string checkName)
    {
        if (actualError is null || !actualError.Contains(expectedText, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"FAIL: {checkName}. Mong đợi lỗi chứa '{expectedText}', nhận được '{actualError ?? "null"}'.");
        }

        _passed++;
        Console.WriteLine($"PASS: {checkName}");
    }

    public static void True(bool condition, string checkName)
    {
        if (!condition) throw new InvalidOperationException($"FAIL: {checkName}.");
        _passed++;
        Console.WriteLine($"PASS: {checkName}");
    }

    public static int PassedCount => _passed;
}
