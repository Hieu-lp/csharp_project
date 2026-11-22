using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

// Tập trung cách tạo kết nối SQL Server để các DAO dùng chung.
public static class MyConnection
{
    // Tên biến môi trường chứa connection string; không ghi mật khẩu vào source code.
    private const string ConnectionStringEnvironmentVariable = "FLIGHTBOOKING_DB_CONNECTION";

    // Tạo kết nối mới. DAO sẽ chịu trách nhiệm mở và đóng kết nối đó.
    public static SqlConnection CreateConnection()
    {
        // Đọc connection string từ cấu hình môi trường của máy đang chạy ứng dụng.
        string? connectionString = Environment.GetEnvironmentVariable(
            ConnectionStringEnvironmentVariable);

        // Báo lỗi sớm với thông báo rõ ràng nếu người dùng chưa cấu hình biến môi trường.
        if (string.IsNullOrWhiteSpace(connectionString))
        {
            throw new InvalidOperationException(
                $"Set the {ConnectionStringEnvironmentVariable} environment variable before connecting to the database.");
        }

        // Chưa mở kết nối ở đây; trả về đối tượng để DAO chủ động quản lý vòng đời.
        return new SqlConnection(connectionString);
    }
}
