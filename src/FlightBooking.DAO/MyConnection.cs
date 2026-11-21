using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class MyConnection(string connectionString)
{
    public SqlConnection Open()
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:FlightBookingDB.");
        var connection = new SqlConnection(connectionString);
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
}
