using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

/// <summary>
/// Connection factory shared by booking, flight management, and customer-management DAOs.
/// </summary>
public sealed class MyConnection
{
    private const string ConnectionStringEnvironmentVariable = "FLIGHTBOOKING_DB_CONNECTION";
    private readonly string chuoiKetNoi;

    public MyConnection(string connectionString)
    {
        this.chuoiKetNoi = connectionString;
    }

    /// <summary>Opens a connection using the explicitly supplied connection string.</summary>
    public SqlConnection Open()
    {
        if (string.IsNullOrWhiteSpace(chuoiKetNoi))
            throw new InvalidOperationException("Chưa cấu hình ConnectionStrings:FlightBookingDB.");
        var ketNoi = new SqlConnection(chuoiKetNoi);
        try
        {
            ketNoi.Open();
            // Thiết lập lại sau mỗi Open, kể cả khi SqlClient lấy kết nối từ pool.
            using var lenhSQL = new SqlCommand("""
                SET ANSI_NULLS ON;
                SET ANSI_PADDING ON;
                SET ANSI_WARNINGS ON;
                SET ARITHABORT ON;
                SET CONCAT_NULL_YIELDS_NULL ON;
                SET QUOTED_IDENTIFIER ON;
                SET NUMERIC_ROUNDABORT OFF;
                """, ketNoi);
            lenhSQL.ExecuteNonQuery();
            return ketNoi;
        }
        catch
        {
            ketNoi.Dispose();
            throw;
        }
    }

    /// <summary>Giữ API kết nối đóng cho các DAO quản lý đã có trên develop.</summary>
    public static SqlConnection CreateConnection()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionStringEnvironmentVariable} environment variable before connecting to the database.");
        }

        return new SqlConnection(connectionString);
    }
}
