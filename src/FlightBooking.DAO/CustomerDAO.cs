using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và chuyển dữ liệu bảng CUSTOMER thành CustomerDTO.
public class CustomerDAO
{
    private const string SelectColumns = "customer_id, full_name, phone, email, date_of_birth, gender, id_passport_number";

    // Đọc nhiều khách hàng; mỗi dòng SQL trở thành một DTO trong List.
    public List<CustomerDTO> GetAll()
    {
        const string sql = $"SELECT {SelectColumns} FROM dbo.CUSTOMER ORDER BY customer_id;";
        var customers = new List<CustomerDTO>();//tạo 1 danh sách rỗng để chứa các khách hàng tìm được
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        connection.Open();//mở kết nối đến sql
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            customers.Add(MapCustomer(reader));
        }

        return customers;
    }

    // Đọc một khách hàng theo khóa chính; trả null nếu ID không tồn tại.
    public CustomerDTO? GetById(int customerId)
    {
        string sql = $"SELECT {SelectColumns} FROM dbo.CUSTOMER WHERE customer_id = @CustomerId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        return reader.Read() ? MapCustomer(reader) : null;
    }

    // Ba hàm sau phục vụ BUS kiểm tra trùng; khi sửa sẽ bỏ qua chính ID đang sửa.
    public bool ExistsByPhone(string phone, int excludedCustomerId = 0) =>
        ExistsValue("phone", SqlDbType.VarChar, 20, phone, excludedCustomerId);

    public bool ExistsByEmail(string email, int excludedCustomerId = 0) =>
        ExistsValue("email", SqlDbType.VarChar, 100, email, excludedCustomerId);

    public bool ExistsByPassport(string passportNumber, int excludedCustomerId = 0) =>
        ExistsValue("id_passport_number", SqlDbType.VarChar, 50, passportNumber, excludedCustomerId);

    // column chỉ được truyền từ ba tên cột cố định ở các hàm trên, không lấy từ người dùng.
    private static bool ExistsValue(string column, SqlDbType type, int size, string value, int excludedCustomerId)
    {
        string sql = $"SELECT COUNT(1) FROM dbo.CUSTOMER WHERE {column} = @Value AND customer_id <> @ExcludedId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Value", type, size).Value = value;
        command.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = excludedCustomerId;
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    // Thêm khách hàng; customer_id được DB tự sinh nên không nằm trong INSERT.
    public bool Insert(CustomerDTO customer)
    {
        const string sql = """
            INSERT INTO dbo.CUSTOMER(full_name, phone, email, date_of_birth, gender, id_passport_number)
            VALUES (@FullName, @Phone, @Email, @DateOfBirth, @Gender, @PassportNumber);
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddCustomerParameters(command, customer);
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Cập nhật các trường được phép sửa, xác định dòng bằng ID.
    public bool Update(CustomerDTO customer)
    {
        const string sql = """
            UPDATE dbo.CUSTOMER
            SET full_name = @FullName, phone = @Phone, email = @Email,
                date_of_birth = @DateOfBirth, gender = @Gender,
                id_passport_number = @PassportNumber
            WHERE customer_id = @CustomerId;
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddCustomerParameters(command, customer);
        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customer.CustomerId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Xóa một dòng; DB có thể từ chối nếu booking đang tham chiếu khách hàng.
    public bool Delete(int customerId)
    {
        const string sql = "DELETE FROM dbo.CUSTOMER WHERE customer_id = @CustomerId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@CustomerId", SqlDbType.Int).Value = customerId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Tạo tham số có kiểu/kích thước khớp schema, tránh nối dữ liệu vào SQL.
    private static void AddCustomerParameters(SqlCommand command, CustomerDTO customer)
    {
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = customer.FullName;
        command.Parameters.Add("@Phone", SqlDbType.VarChar, 20).Value = customer.Phone;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = customer.Email;
        command.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = customer.DateOfBirth.Date;
        command.Parameters.Add("@Gender", SqlDbType.NVarChar, 10).Value = customer.Gender;
        command.Parameters.Add("@PassportNumber", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(customer.IdPassportNumber)
                ? DBNull.Value
                : customer.IdPassportNumber.Trim();
    }

    // Chuyển cột reader thành property DTO; hộ chiếu cần xử lý SQL NULL.
    private static CustomerDTO MapCustomer(SqlDataReader reader)
    {
        int passportOrdinal = reader.GetOrdinal("id_passport_number");
        return new CustomerDTO
        {
            CustomerId = reader.GetInt32(reader.GetOrdinal("customer_id")),
            FullName = reader.GetString(reader.GetOrdinal("full_name")),
            Phone = reader.GetString(reader.GetOrdinal("phone")),
            Email = reader.GetString(reader.GetOrdinal("email")),
            DateOfBirth = reader.GetDateTime(reader.GetOrdinal("date_of_birth")),
            Gender = reader.GetString(reader.GetOrdinal("gender")),
            IdPassportNumber = reader.IsDBNull(passportOrdinal) ? null : reader.GetString(passportOrdinal)
        };
    }
}
