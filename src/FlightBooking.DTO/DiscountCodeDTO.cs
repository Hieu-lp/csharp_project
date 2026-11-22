namespace FlightBooking.DTO;

// DTO mang dữ liệu của một mã giảm giá.
public class DiscountCodeDTO
{
    // Khóa chính IDENTITY do SQL Server tạo.
    public int DiscountId { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string DiscountType { get; set; } = string.Empty;
    public decimal DiscountValue { get; set; }
    public decimal MinOrderAmount { get; set; }

    // Hai trường này nullable theo schema: không giới hạn thì để null.
    public decimal? MaxDiscountAmount { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int? UsageLimit { get; set; }

    // used_count do nghiệp vụ đặt vé tăng; không nên để form nhập tùy ý.
    public int UsedCount { get; set; }
    public string Status { get; set; } = "ACTIVE";
}
