namespace FlightBooking.DTO;

// Một điều kiện lọc do GUI gửi cho BUS khi tìm kiếm nâng cao.
// LogicalOperator nối điều kiện hiện tại với kết quả các điều kiện trước đó.
public class SearchConditionDTO
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "=";
    public string Value { get; set; } = string.Empty;
    public string LogicalOperator { get; set; } = "AND";
}
