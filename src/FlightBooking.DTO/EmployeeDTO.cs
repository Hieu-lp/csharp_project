    namespace FlightBooking.DTO;

// DTO mang dữ liệu của một nhân viên giữa GUI, BUS và DAO.
public class EmployeeDTO
{
    // Khóa chính IDENTITY do SQL Server tạo.
    public int EmployeeId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public DateTime HireDate { get; set; }

    // Database mặc định ACTIVE và chỉ nhận ACTIVE hoặc INACTIVE.
    public string Status { get; set; } = "ACTIVE";
}
