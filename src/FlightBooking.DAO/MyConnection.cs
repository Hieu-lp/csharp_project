using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class MyConnection
{
    private readonly string chuoiKetNoi;

    public MyConnection(string connectionString)
    {
        this.chuoiKetNoi = connectionString;
    }

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
}
