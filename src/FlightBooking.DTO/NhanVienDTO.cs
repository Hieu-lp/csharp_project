using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

// DTO mang dữ liệu của một nhân viên giữa GUI, BUS và DAO.
public class NhanVienDTO
{
    // Khóa chính IDENTITY do SQL Server tạo.
    [JsonPropertyName("EmployeeId")]
    public int MaNhanVien { get; set; }
    [JsonPropertyName("FullName")]
    public string HoTen { get; set; } = string.Empty;
    [JsonPropertyName("Phone")]
    public string DienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    [JsonPropertyName("Position")]
    public string ChucVu { get; set; } = string.Empty;
    [JsonPropertyName("HireDate")]
    public DateTime NgayVaoLam { get; set; }

    // Database mặc định ACTIVE và chỉ nhận ACTIVE hoặc INACTIVE.
    [JsonPropertyName("Status")]
    public string TrangThai { get; set; } = "ACTIVE";
}
