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
            ThietLapTuyChon(ketNoi);
            return ketNoi;
        }
        catch
        {
            ketNoi.Dispose();
            throw;
        }
    }

    private static void ThietLapTuyChon(SqlConnection ketNoi)
    {
        // Thiết lập lại sau mỗi lần mở, kể cả khi lấy kết nối từ pool.
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
        // Chẩn đoán local tùy chọn: đọc danh tính thật, không in credential/access key.
        if (Environment.GetEnvironmentVariable("FLIGHTBOOKING_CONNECTION_DIAGNOSTICS") == "1")
        {
            using var danhTinh = new SqlCommand("SELECT @@SERVERNAME,DB_NAME(),ORIGINAL_LOGIN();", ketNoi);
            using var doc = danhTinh.ExecuteReader();
            if (doc.Read())
                Console.WriteLine($"SQL_IDENTITY Server={doc.GetString(0)}; Database={doc.GetString(1)}; Login={doc.GetString(2)}");
        }
    }

    /// <summary>Cấu hình chung: biến chuẩn, biến tương thích, rồi file local cạnh ứng dụng.</summary>
    public static string? DocChuoiKetNoi()
    {
        string? connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        if (string.IsNullOrWhiteSpace(connectionString))
            connectionString = Environment.GetEnvironmentVariable(ConnectionStringEnvironmentVariable);
        var duongDan = Path.Combine(AppContext.BaseDirectory, "connection.local.txt");
        if (string.IsNullOrWhiteSpace(connectionString) && File.Exists(duongDan))
            connectionString = File.ReadAllText(duongDan).Trim();
        return connectionString;
    }

    /// <summary>Giữ API kết nối đóng cho các DAO quản lý đã có trên develop.</summary>
    public static SqlConnection CreateConnection()
    {
        string? connectionString = DocChuoiKetNoi();
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionStringEnvironmentVariable} environment variable before connecting to the database.");
        }

        var ketNoi = new SqlConnection(connectionString);
        // API này trả kết nối đóng; DAO hiện có tự gọi Open(). StateChange bảo đảm
        // các DAO Phú/Quân cũng nhận đủ SET options khi mở hoặc mở lại kết nối.
        ketNoi.StateChange += (_, thayDoi) =>
        {
            if (thayDoi.CurrentState == System.Data.ConnectionState.Open)
                ThietLapTuyChon(ketNoi);
        };
        return ketNoi;
    }
}
