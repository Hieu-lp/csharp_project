using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và ánh xạ bảng DISCOUNT_CODE thành DiscountCodeDTO.
public class DiscountCodeDAO
{
    private const string SelectColumns = "discount_id, code, description, discount_type, discount_value, min_order_amount, max_discount_amount, start_date, end_date, usage_limit, used_count, status";

    // Đọc tất cả mã giảm giá.
    public List<DiscountCodeDTO> GetAll()
    {
        const string sql = $"SELECT {SelectColumns} FROM dbo.DISCOUNT_CODE ORDER BY discount_id;";
        var discounts = new List<DiscountCodeDTO>();
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            discounts.Add(MapDiscountCode(reader));
        }

        return discounts;
    }

    // Đọc một mã theo khóa chính; trả null nếu không tồn tại.
    public DiscountCodeDTO? GetById(int discountId)
    {
        string sql = $"SELECT {SelectColumns} FROM dbo.DISCOUNT_CODE WHERE discount_id = @DiscountId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DiscountId", SqlDbType.Int).Value = discountId;
        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapDiscountCode(reader) : null;
    }

    // Kiểm tra code trùng; khi sửa thì loại trừ chính mã hiện tại.
    public bool ExistsByCode(string code, int excludedDiscountId = 0)
    {
        const string sql = "SELECT COUNT(1) FROM dbo.DISCOUNT_CODE WHERE code = @Code AND discount_id <> @ExcludedId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = code;
        command.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = excludedDiscountId;
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    // Thêm mã mới; DB tự tạo ID và đặt used_count mặc định bằng 0.
    public bool Insert(DiscountCodeDTO discount)
    {
        // used_count không được nhập khi tạo mới; DB sẽ dùng giá trị mặc định 0.
        const string sql = """
            INSERT INTO dbo.DISCOUNT_CODE
                (code, description, discount_type, discount_value, min_order_amount,
                 max_discount_amount, start_date, end_date, usage_limit, status)
            VALUES
                (@Code, @Description, @DiscountType, @DiscountValue, @MinOrderAmount,
                 @MaxDiscountAmount, @StartDate, @EndDate, @UsageLimit, @Status);
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddDiscountParameters(command, discount);
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Cập nhật thông tin cấu hình, nhưng giữ nguyên used_count do hệ thống đặt vé quản lý.
    public bool Update(DiscountCodeDTO discount)
    {
        // Không cập nhật used_count vì số lượt dùng do nghiệp vụ đặt vé quản lý.
        const string sql = """
            UPDATE dbo.DISCOUNT_CODE
            SET code = @Code, description = @Description, discount_type = @DiscountType,
                discount_value = @DiscountValue, min_order_amount = @MinOrderAmount,
                max_discount_amount = @MaxDiscountAmount, start_date = @StartDate,
                end_date = @EndDate, usage_limit = @UsageLimit, status = @Status
            WHERE discount_id = @DiscountId;
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddDiscountParameters(command, discount);
        command.Parameters.Add("@DiscountId", SqlDbType.Int).Value = discount.DiscountId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Xóa theo ID; DB có thể từ chối nếu booking đã tham chiếu mã.
    public bool Delete(int discountId)
    {
        const string sql = "DELETE FROM dbo.DISCOUNT_CODE WHERE discount_id = @DiscountId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@DiscountId", SqlDbType.Int).Value = discountId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Đặt kiểu SQL tường minh cho từng giá trị gửi xuống DB.
    private static void AddDiscountParameters(SqlCommand command, DiscountCodeDTO discount)
    {
        command.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = discount.Code;
        command.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = discount.Description;
        command.Parameters.Add("@DiscountType", SqlDbType.VarChar, 10).Value = discount.DiscountType;
        AddDecimal(command, "@DiscountValue", discount.DiscountValue);
        AddDecimal(command, "@MinOrderAmount", discount.MinOrderAmount);
        AddDecimal(command, "@MaxDiscountAmount", discount.MaxDiscountAmount);
        AddDateTime2(command, "@StartDate", discount.StartDate);
        AddDateTime2(command, "@EndDate", discount.EndDate);
        command.Parameters.Add("@UsageLimit", SqlDbType.Int).Value =
            discount.UsageLimit.HasValue ? discount.UsageLimit.Value : DBNull.Value;
        command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = discount.Status;
    }

    // DECIMAL(18,2) cần khai báo precision và scale cho parameter.
    private static void AddDecimal(SqlCommand command, string name, decimal? value)
    {
        SqlParameter parameter = command.Parameters.Add(name, SqlDbType.Decimal);
        parameter.Precision = 18;
        parameter.Scale = 2;
        parameter.Value = value.HasValue ? value.Value : DBNull.Value;
    }

    // Schema dùng DATETIME2(0), tức độ chính xác tới giây.
    private static void AddDateTime2(SqlCommand command, string name, DateTime value)
    {
        SqlParameter parameter = command.Parameters.Add(name, SqlDbType.DateTime2);
        parameter.Scale = 0;
        parameter.Value = value;
    }

    // Ánh xạ một dòng SQL; hai cột nullable phải kiểm tra IsDBNull trước khi đọc.
    private static DiscountCodeDTO MapDiscountCode(SqlDataReader reader)
    {
        int maxDiscountOrdinal = reader.GetOrdinal("max_discount_amount");
        int usageLimitOrdinal = reader.GetOrdinal("usage_limit");
        return new DiscountCodeDTO
        {
            DiscountId = reader.GetInt32(reader.GetOrdinal("discount_id")),
            Code = reader.GetString(reader.GetOrdinal("code")),
            Description = reader.GetString(reader.GetOrdinal("description")),
            DiscountType = reader.GetString(reader.GetOrdinal("discount_type")),
            DiscountValue = reader.GetDecimal(reader.GetOrdinal("discount_value")),
            MinOrderAmount = reader.GetDecimal(reader.GetOrdinal("min_order_amount")),
            MaxDiscountAmount = reader.IsDBNull(maxDiscountOrdinal) ? null : reader.GetDecimal(maxDiscountOrdinal),
            StartDate = reader.GetDateTime(reader.GetOrdinal("start_date")),
            EndDate = reader.GetDateTime(reader.GetOrdinal("end_date")),
            UsageLimit = reader.IsDBNull(usageLimitOrdinal) ? null : reader.GetInt32(usageLimitOrdinal),
            UsedCount = reader.GetInt32(reader.GetOrdinal("used_count")),
            Status = reader.GetString(reader.GetOrdinal("status"))
        };
    }
}
