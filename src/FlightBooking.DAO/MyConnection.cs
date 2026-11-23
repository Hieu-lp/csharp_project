using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

/// <summary>
/// Connection factory shared by the DAO layer.
/// Booking passes its configured connection string; management DAOs read the
/// FLIGHTBOOKING_DB_CONNECTION environment variable.
/// </summary>
public sealed class MyConnection
{
    private const string ConnectionStringEnvironmentVariable = "FLIGHTBOOKING_DB_CONNECTION";
    private readonly string _connectionString;

    public MyConnection(string connectionString)
    {
        _connectionString = connectionString;
    }

    /// <summary>Opens and returns a connection for BookingDAO, which owns its disposal.</summary>
    public SqlConnection Open()
    {
        if (string.IsNullOrWhiteSpace(_connectionString))
        {
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:FlightBookingDB.");
        }

        var connection = new SqlConnection(_connectionString);
        try
        {
            connection.Open();
            return connection;
        }
        catch
        {
            connection.Dispose();
            throw;
        }
    }

    /// <summary>Creates a closed connection for management DAOs from the environment variable.</summary>
    public static SqlConnection CreateConnection()
    {
        string? connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionStringEnvironmentVariable} environment variable before connecting to the database.");
        }

        return new SqlConnection(connectionString);
    }
}
