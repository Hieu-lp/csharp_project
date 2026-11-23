using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;
using System.Net.Mail;

namespace FlightBooking.BUS;

// BUS kiểm tra dữ liệu nhân viên trước khi gọi EmployeeDAO.
public class EmployeeBUS
{
    private readonly EmployeeDAO _employeeDAO = new();

    public List<EmployeeDTO> GetAll() => _employeeDAO.GetAll();

    public EmployeeDTO? GetById(int employeeId) =>
        employeeId > 0 ? _employeeDAO.GetById(employeeId) : null;

    public string? Add(EmployeeDTO employee)
    {
        Normalize(employee);
        string? error = Validate(employee);
        if (error is not null) return error;

        if (_employeeDAO.ExistsByPhone(employee.Phone)) return "Số điện thoại đã được sử dụng.";
        if (_employeeDAO.ExistsByEmail(employee.Email)) return "Email đã được sử dụng.";
        return _employeeDAO.Insert(employee) ? null : "Không thêm được nhân viên.";
    }

    public string? Update(EmployeeDTO employee)
    {
        if (employee.EmployeeId <= 0) return "Mã nhân viên không hợp lệ.";
        Normalize(employee);
        string? error = Validate(employee);
        if (error is not null) return error;

        if (_employeeDAO.ExistsByPhone(employee.Phone, employee.EmployeeId)) return "Số điện thoại đã được nhân viên khác sử dụng.";
        if (_employeeDAO.ExistsByEmail(employee.Email, employee.EmployeeId)) return "Email đã được nhân viên khác sử dụng.";
        return _employeeDAO.Update(employee) ? null : "Không tìm thấy nhân viên để cập nhật.";
    }

    public string? Delete(int employeeId)
    {
        if (employeeId <= 0) return "Mã nhân viên không hợp lệ.";
        return _employeeDAO.Delete(employeeId) ? null : "Không tìm thấy nhân viên để xóa.";
    }

    public ArrayList Search(string? keyword)
    {
        string searchText = keyword?.Trim() ?? string.Empty;
        var results = new ArrayList();

        foreach (EmployeeDTO employee in _employeeDAO.GetAll())
        {
            bool matches = searchText.Length == 0 ||
                Contains(employee.EmployeeId.ToString(), searchText) ||
                Contains(employee.FullName, searchText) ||
                Contains(employee.Phone, searchText) ||
                Contains(employee.Email, searchText) ||
                Contains(employee.Position, searchText) ||
                Contains(employee.HireDate.ToString("yyyy-MM-dd"), searchText) ||
                Contains(employee.Status, searchText);

            if (matches) results.Add(employee);
        }

        return results;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList SearchAdvanced(ArrayList conditions) =>
        ArrayListSearch.Filter(_employeeDAO.GetAll(), conditions, GetFieldValue);

    private static object? GetFieldValue(EmployeeDTO employee, string field) =>
        field.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "EMPLOYEEID" => employee.EmployeeId,
            "FULLNAME" => employee.FullName,
            "PHONE" => employee.Phone,
            "EMAIL" => employee.Email,
            "POSITION" => employee.Position,
            "HIREDATE" => employee.HireDate,
            "STATUS" => employee.Status,
            _ => throw new ArgumentException($"Không có trường EMPLOYEE tên '{field}'.", nameof(field))
        };

    private static string? Validate(EmployeeDTO employee)
    {
        if (string.IsNullOrWhiteSpace(employee.FullName)) return "Họ tên không được để trống.";
        if (employee.FullName.Length > 100) return "Họ tên không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(employee.Phone)) return "Số điện thoại không được để trống.";
        if (employee.Phone.Length > 20) return "Số điện thoại không được dài quá 20 ký tự.";
        if (string.IsNullOrWhiteSpace(employee.Email) || !IsValidEmail(employee.Email)) return "Email không hợp lệ.";
        if (employee.Email.Length > 100) return "Email không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(employee.Position)) return "Chức vụ không được để trống.";
        if (employee.Position.Length > 50) return "Chức vụ không được dài quá 50 ký tự.";
        if (employee.HireDate == default) return "Ngày vào làm không hợp lệ.";
        if (employee.Status is not ("ACTIVE" or "INACTIVE")) return "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.";
        return null;
    }

    private static void Normalize(EmployeeDTO employee)
    {
        employee.FullName = employee.FullName.Trim();
        employee.Phone = employee.Phone.Trim();
        employee.Email = employee.Email.Trim();
        employee.Position = employee.Position.Trim();
        employee.Status = employee.Status.Trim().ToUpperInvariant();
    }

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out MailAddress? address) &&
        string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
