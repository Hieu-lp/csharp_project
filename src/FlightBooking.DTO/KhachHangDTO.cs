using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

// DTO (Data Transfer Object) chứa dữ liệu của một khách hàng.
// DTO chỉ giữ dữ liệu; kiểm tra nghiệp vụ sẽ đặt ở BUS.
public class KhachHangDTO
{
    // Mã do SQL Server tự tạo vì customer_id là IDENTITY.
    [JsonPropertyName("CustomerId")]
    public int MaKhachHang { get; set; }

    // Các cột bắt buộc được khởi tạo bằng chuỗi rỗng để tránh giá trị null.
    [JsonPropertyName("FullName")]
    public string HoTen { get; set; } = string.Empty;
    [JsonPropertyName("Phone")]
    public string DienThoai { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // DATE trong SQL Server được SqlDataReader trả về dưới dạng DateTime.
    [JsonPropertyName("DateOfBirth")]
    public DateTime NgaySinh { get; set; }
    [JsonPropertyName("Gender")]
    public string GioiTinh { get; set; } = string.Empty;

    // Cột này cho phép NULL trong database nên property cũng cho phép null.
    [JsonPropertyName("IdPassportNumber")]
    public string? SoGiayTo { get; set; } // ? có nghĩa là idpassportnumber có thể là null
    //String.empty là khai báo chuỗi rỗng
}
