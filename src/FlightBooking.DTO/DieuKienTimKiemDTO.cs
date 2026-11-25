using System.Text.Json.Serialization;

namespace FlightBooking.DTO;

// Một điều kiện lọc do GUI gửi cho BUS khi tìm kiếm nâng cao.
// LogicalOperator nối điều kiện hiện tại với kết quả các điều kiện trước đó.
public class DieuKienTimKiemDTO
{
    [JsonPropertyName("Field")]
    public string Truong { get; set; } = string.Empty;
    [JsonPropertyName("Operator")]
    public string PhepSoSanh { get; set; } = "=";
    [JsonPropertyName("Value")]
    public string GiaTri { get; set; } = string.Empty;
    [JsonPropertyName("LogicalOperator")]
    public string PhepNoi { get; set; } = "AND";
}
