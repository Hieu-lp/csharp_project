using Microsoft.Data.SqlClient;
using FlightBooking.DAO;

namespace FlightBooking.Testing;

internal static class SqlTestPreflight
{
    public const string DatabaseName = "FlightBooking_Hieu_ManagementTests";
    public const string Approval = "APPROVE_WRITES_TO_FLIGHTBOOKING_HIEU_MANAGEMENTTESTS";

    // Chỉ kiểm tra cấu hình, không kết nối; từ chối trước khi có thể ghi nhầm DB.
    public static void ValidateSettings(string connectionString, string? expectedServer, string? approval)
    {
        var settings = new SqlConnectionStringBuilder(connectionString);
        if (!string.Equals(settings.InitialCatalog, DatabaseName, StringComparison.Ordinal) ||
            !settings.InitialCatalog.EndsWith("_ManagementTests", StringComparison.Ordinal) ||
            string.Equals(settings.InitialCatalog, "FlightBookingDB", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("Refusing SQL writes: only " + DatabaseName + " is allowlisted.");
        if (string.IsNullOrWhiteSpace(expectedServer))
            throw new InvalidOperationException("Set FLIGHTBOOKING_EXPECTED_TEST_SERVER to the verified SQL Server name.");
        if (approval != Approval)
            throw new InvalidOperationException("SQL write approval is missing. Do not run SQL tests before user approval.");
        if (!string.IsNullOrEmpty(settings.AttachDBFilename) || settings.UserInstance ||
            !string.IsNullOrEmpty(settings.FailoverPartner) || settings.ApplicationIntent != ApplicationIntent.ReadWrite)
            throw new InvalidOperationException("Refusing attachment, user-instance, failover or read-only test connections.");
    }

    // Chỉ SELECT. Schema/fixture phải được chuẩn bị và duyệt riêng; runner không tự áp dụng DDL.
    public static void Verify(string connectionString)
    {
        string? server = Environment.GetEnvironmentVariable("FLIGHTBOOKING_EXPECTED_TEST_SERVER");
        string? approval = Environment.GetEnvironmentVariable("FLIGHTBOOKING_SQL_WRITE_APPROVAL");
        ValidateSettings(connectionString, server, approval);
        using var connection = new MyConnection(connectionString).Open();
        string script = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "SqlTestPreflight.sql"));
        using var command = new SqlCommand(script, connection);
        command.Parameters.AddWithValue("@ExpectedServer", server!);
        command.Parameters.AddWithValue("@WriteApproval", approval!);
        using var reader = command.ExecuteReader();
        int checks = 0;
        while (reader.Read())
        {
            string name = reader.GetString(0);
            bool passed = reader.GetBoolean(1);
            string detail = reader.GetString(2);
            if (!passed) throw new InvalidOperationException("SQL write preflight blocked: " + name + ": " + detail);
            checks++;
        }
        if (checks != 16) throw new InvalidOperationException("SQL write preflight did not return all 16 required checks.");
        Console.WriteLine($"{checks} SQL write preflight checks passed for {DatabaseName}. No schema applied.");
    }
}
