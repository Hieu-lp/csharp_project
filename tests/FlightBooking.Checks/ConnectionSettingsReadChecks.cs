using System.Reflection;
using FlightBooking.DAO;
using Microsoft.Data.SqlClient;

internal static class ConnectionSettingsReadChecks
{
    public static void Run(Action<bool, string> check)
    {
        string cs = Environment.GetEnvironmentVariable("FLIGHTBOOKING_BROWSE_CONNECTION")
            ?? throw new InvalidOperationException("Set an explicit read connection first.");
        var settings = new SqlConnectionStringBuilder(cs);
        if (settings.InitialCatalog != "FlightBooking_Hieu_ManagementTests")
            throw new InvalidOperationException("SET/pool checks require the dedicated test database.");
        settings.ApplicationName = "FlightBooking.SetOptionsReadChecks";
        settings.MaxPoolSize = 1;
        cs = settings.ConnectionString;
        int count = 0;
        void Verify(SqlConnection connection, string label)
        {
            using var command = new SqlCommand("""
                SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),DB_NAME(),
                 CONVERT(int,SESSIONPROPERTY('ANSI_NULLS')),CONVERT(int,SESSIONPROPERTY('ANSI_PADDING')),
                 CONVERT(int,SESSIONPROPERTY('ANSI_WARNINGS')),CONVERT(int,SESSIONPROPERTY('ARITHABORT')),
                 CONVERT(int,SESSIONPROPERTY('CONCAT_NULL_YIELDS_NULL')),CONVERT(int,SESSIONPROPERTY('QUOTED_IDENTIFIER')),
                 CONVERT(int,SESSIONPROPERTY('NUMERIC_ROUNDABORT'))
                """, connection);
            using var reader = command.ExecuteReader();
            bool valid = reader.Read() && reader.GetString(0) == "DESKTOP-1MMSBHT" && reader.GetString(1) == settings.InitialCatalog;
            for (int i = 2; i <= 8 && valid; i++) valid = reader.GetInt32(i) == (i == 8 ? 0 : 1);
            check(valid, "SQL-READ SET: " + label);
            count++;
        }
        SqlConnection ReadContextConnection(object context)
        {
            return (SqlConnection)(context.GetType().GetField("ketNoi", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(context)
                ?? throw new InvalidOperationException("DAO read connection not found."));
        }
        try
        {
            Guid firstConnection;
            using (var first = new MyConnection(cs).Open())
            {
                firstConnection = first.ClientConnectionId;
                Verify(first, "MyConnection.Open has all seven explicit values");
                // Only alter our own session before returning it to our dedicated pool.
                using var command = new SqlCommand("SET ARITHABORT OFF; SELECT CONVERT(int,SESSIONPROPERTY('ARITHABORT'));", first);
                check((int)command.ExecuteScalar()! == 0, "SQL-READ SET: test session deliberately changed before pooling");
                count++;
            }
            using (var second = new MyConnection(cs).Open())
            {
                check(second.ClientConnectionId == firstConnection, "SQL-READ SET: same physical connection reopened from pool");
                count++;
                Verify(second, "pooled reopen restores all seven values");
            }
            new DatVeDAO(cs).DocDuLieu(data => { Verify(ReadContextConnection(data), "BookingDAO.Read"); return 0; });
            new QuanLyDAO(cs).DocDuLieu(data => { Verify(ReadContextConnection(data), "ManagementDAO.Read"); return 0; });
        }
        finally { SqlConnection.ClearAllPools(); }
        Console.WriteLine($"{count} SET/pool read checks passed. No business DML or write approval.");
    }
}
