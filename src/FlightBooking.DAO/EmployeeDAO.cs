using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và ánh xạ bảng EMPLOYEE thành EmployeeDTO.
public class EmployeeDAO
{
    private const string SelectColumns = "employee_id, full_name, phone, email, position, hire_date, status";

    // Đọc toàn bộ nhân viên, mỗi dòng SQL thành một DTO.
    public List<EmployeeDTO> GetAll()
    {
        const string sql = $"SELECT {SelectColumns} FROM dbo.EMPLOYEE ORDER BY employee_id;";
        var employees = new List<EmployeeDTO>();
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();

        while (reader.Read())
        {
            employees.Add(MapEmployee(reader));
        }

        return employees;
    }

    // Đọc theo khóa chính; trả null khi không có nhân viên đó.
    public EmployeeDTO? GetById(int employeeId)
    {
        string sql = $"SELECT {SelectColumns} FROM dbo.EMPLOYEE WHERE employee_id = @EmployeeId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
        connection.Open();
        using SqlDataReader reader = command.ExecuteReader();
        return reader.Read() ? MapEmployee(reader) : null;
    }

    // Kiểm tra trùng phone/email; có thể loại trừ ID hiện tại khi cập nhật.
    public bool ExistsByPhone(string phone, int excludedEmployeeId = 0) =>
        ExistsValue("phone", 20, phone, excludedEmployeeId);

    public bool ExistsByEmail(string email, int excludedEmployeeId = 0) =>
        ExistsValue("email", 100, email, excludedEmployeeId);

    // column chỉ nhận tên cột cố định từ các hàm ExistsByPhone/ExistsByEmail.
    // column chỉ nhận tên cột cố định từ các hàm ExistsByPhone/ExistsByEmail.
    private static bool ExistsValue(string column, int size, string value, int excludedEmployeeId)
    {
        string sql = $"SELECT COUNT(1) FROM dbo.EMPLOYEE WHERE {column} = @Value AND employee_id <> @ExcludedId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@Value", SqlDbType.VarChar, size).Value = value;
        command.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = excludedEmployeeId;
        connection.Open();
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }

    // Thêm nhân viên; employee_id do SQL Server tự tạo.
    public bool Insert(EmployeeDTO employee)
    {
        const string sql = """
            INSERT INTO dbo.EMPLOYEE(full_name, phone, email, position, hire_date, status)
            VALUES (@FullName, @Phone, @Email, @Position, @HireDate, @Status);
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Cập nhật nhân viên theo ID.
    public bool Update(EmployeeDTO employee)
    {
        const string sql = """
            UPDATE dbo.EMPLOYEE
            SET full_name = @FullName, phone = @Phone, email = @Email,
                position = @Position, hire_date = @HireDate, status = @Status
            WHERE employee_id = @EmployeeId;
            """;
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        AddEmployeeParameters(command, employee);
        command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employee.EmployeeId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Xóa theo ID; khóa ngoại từ BOOKING có thể từ chối xóa.
    public bool Delete(int employeeId)
    {
        const string sql = "DELETE FROM dbo.EMPLOYEE WHERE employee_id = @EmployeeId;";
        using SqlConnection connection = MyConnection.CreateConnection();
        using var command = new SqlCommand(sql, connection);
        command.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = employeeId;
        connection.Open();
        return command.ExecuteNonQuery() == 1;
    }

    // Dùng SQL parameters có kiểu và kích thước theo schema.
    private static void AddEmployeeParameters(SqlCommand command, EmployeeDTO employee)
    {
        command.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = employee.FullName;
        command.Parameters.Add("@Phone", SqlDbType.VarChar, 20).Value = employee.Phone;
        command.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = employee.Email;
        command.Parameters.Add("@Position", SqlDbType.NVarChar, 50).Value = employee.Position;
        command.Parameters.Add("@HireDate", SqlDbType.Date).Value = employee.HireDate.Date;
        command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = employee.Status;
    }

    // Ánh xạ một dòng trong reader thành EmployeeDTO.
    private static EmployeeDTO MapEmployee(SqlDataReader reader) => new()
    {
        EmployeeId = reader.GetInt32(reader.GetOrdinal("employee_id")),
        FullName = reader.GetString(reader.GetOrdinal("full_name")),
        Phone = reader.GetString(reader.GetOrdinal("phone")),
        Email = reader.GetString(reader.GetOrdinal("email")),
        Position = reader.GetString(reader.GetOrdinal("position")),
        HireDate = reader.GetDateTime(reader.GetOrdinal("hire_date")),
        Status = reader.GetString(reader.GetOrdinal("status"))
    };
}
