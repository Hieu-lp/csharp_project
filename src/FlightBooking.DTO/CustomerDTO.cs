namespace FlightBooking.DTO;

// DTO (Data Transfer Object) chứa dữ liệu của một khách hàng.
// DTO chỉ giữ dữ liệu; kiểm tra nghiệp vụ sẽ đặt ở BUS.
public class CustomerDTO
{
    // Mã do SQL Server tự tạo vì customer_id là IDENTITY.
    public int CustomerId { get; set; }

    // Các cột bắt buộc được khởi tạo bằng chuỗi rỗng để tránh giá trị null.
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;

    // DATE trong SQL Server được SqlDataReader trả về dưới dạng DateTime.
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;

    // Cột này cho phép NULL trong database nên property cũng cho phép null.
    public string? IdPassportNumber { get; set; } // ? có nghĩa là idpassportnumber có thể là null
    //String.empty là khai báo chuỗi rỗng
}
