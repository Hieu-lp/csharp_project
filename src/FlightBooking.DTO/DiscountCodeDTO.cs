namespace FlightBooking.DTO;

// DTO mang dữ liệu của một mã giảm giá.
public sealed record DiscountCodeDTO
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

    // BookingBUS trước đây dùng các tên Type/Value; giữ alias để code booking tiếp tục tương thích.
    public string Type
    {
        get => DiscountType;
        init => DiscountType = value;
    }

    public decimal Value
    {
        get => DiscountValue;
        init => DiscountValue = value;
    }

    public DiscountCodeDTO() { }

    // Constructor giữ tương thích với code booking và các check hiện có.
    public DiscountCodeDTO(
        int discountId,
        string code,
        string type,
        decimal value,
        decimal minOrderAmount,
        decimal? maxDiscountAmount,
        DateTime startDate,
        DateTime endDate,
        int? usageLimit,
        int usedCount,
        string status)
    {
        DiscountId = discountId;
        Code = code;
        DiscountType = type;
        DiscountValue = value;
        MinOrderAmount = minOrderAmount;
        MaxDiscountAmount = maxDiscountAmount;
        StartDate = startDate;
        EndDate = endDate;
        UsageLimit = usageLimit;
        UsedCount = usedCount;
        Status = status;
    }
}
