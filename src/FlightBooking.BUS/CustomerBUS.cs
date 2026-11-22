using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;
using System.Net.Mail;

namespace FlightBooking.BUS;

// BUS kiểm tra quy tắc khách hàng trước khi nhờ DAO truy cập database.
public class CustomerBUS
{
    private readonly CustomerDAO _customerDAO = new();

    public List<CustomerDTO> GetAll() => _customerDAO.GetAll();

    public CustomerDTO? GetById(int customerId) =>
        customerId > 0 ? _customerDAO.GetById(customerId) : null;

    public string? Add(CustomerDTO customer)
    {
        Normalize(customer);
        string? error = Validate(customer);
        if (error is not null) return error;

        if (_customerDAO.ExistsByPhone(customer.Phone)) return "Số điện thoại đã được sử dụng.";
        if (_customerDAO.ExistsByEmail(customer.Email)) return "Email đã được sử dụng.";
        if (customer.IdPassportNumber is not null && _customerDAO.ExistsByPassport(customer.IdPassportNumber))
            return "Số giấy tờ tùy thân đã được sử dụng.";

        return _customerDAO.Insert(customer) ? null : "Không thêm được khách hàng.";
    }

    public string? Update(CustomerDTO customer)
    {
        if (customer.CustomerId <= 0) return "Mã khách hàng không hợp lệ.";
        Normalize(customer);
        string? error = Validate(customer);
        if (error is not null) return error;

        if (_customerDAO.ExistsByPhone(customer.Phone, customer.CustomerId)) return "Số điện thoại đã được khách hàng khác sử dụng.";
        if (_customerDAO.ExistsByEmail(customer.Email, customer.CustomerId)) return "Email đã được khách hàng khác sử dụng.";
        if (customer.IdPassportNumber is not null &&
            _customerDAO.ExistsByPassport(customer.IdPassportNumber, customer.CustomerId))
            return "Số giấy tờ tùy thân đã được khách hàng khác sử dụng.";

        return _customerDAO.Update(customer) ? null : "Không tìm thấy khách hàng để cập nhật.";
    }

    // null nghĩa là xóa thành công; khóa ngoại có thể làm DAO ném SqlException.
    public string? Delete(int customerId)
    {
        if (customerId <= 0) return "Mã khách hàng không hợp lệ.";
        return _customerDAO.Delete(customerId) ? null : "Không tìm thấy khách hàng để xóa.";
    }

    // Tìm gần đúng trong BUS; kết quả dùng ArrayList theo yêu cầu môn học.
    public ArrayList Search(string? keyword)
    {
        string searchText = keyword?.Trim() ?? string.Empty;
        var results = new ArrayList();

        foreach (CustomerDTO customer in _customerDAO.GetAll())
        {
            bool matches = searchText.Length == 0 ||
                Contains(customer.CustomerId.ToString(), searchText) ||
                Contains(customer.FullName, searchText) ||
                Contains(customer.Phone, searchText) ||
                Contains(customer.Email, searchText) ||
                Contains(customer.DateOfBirth.ToString("yyyy-MM-dd"), searchText) ||
                Contains(customer.Gender, searchText) ||
                Contains(customer.IdPassportNumber ?? string.Empty, searchText);

            if (matches) results.Add(customer);
        }

        return results;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList SearchAdvanced(ArrayList conditions) =>
        ArrayListSearch.Filter(_customerDAO.GetAll(), conditions, GetFieldValue);

    private static object? GetFieldValue(CustomerDTO customer, string field) =>
        field.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "CUSTOMERID" => customer.CustomerId,
            "FULLNAME" => customer.FullName,
            "PHONE" => customer.Phone,
            "EMAIL" => customer.Email,
            "DATEOFBIRTH" => customer.DateOfBirth,
            "GENDER" => customer.Gender,
            "IDPASSPORTNUMBER" => customer.IdPassportNumber,
            _ => throw new ArgumentException($"Không có trường CUSTOMER tên '{field}'.", nameof(field))
        };

    private static string? Validate(CustomerDTO customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FullName)) return "Họ tên không được để trống.";
        if (customer.FullName.Length > 100) return "Họ tên không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(customer.Phone)) return "Số điện thoại không được để trống.";
        if (customer.Phone.Length > 20) return "Số điện thoại không được dài quá 20 ký tự.";
        if (string.IsNullOrWhiteSpace(customer.Email) || !IsValidEmail(customer.Email)) return "Email không hợp lệ.";
        if (customer.Email.Length > 100) return "Email không được dài quá 100 ký tự.";
        if (customer.DateOfBirth == default || customer.DateOfBirth.Date > DateTime.Today) return "Ngày sinh không hợp lệ.";
        if (string.IsNullOrWhiteSpace(customer.Gender)) return "Giới tính không được để trống.";
        if (customer.Gender.Length > 10) return "Giới tính không được dài quá 10 ký tự.";
        if (customer.IdPassportNumber?.Length > 50) return "Số giấy tờ tùy thân không được dài quá 50 ký tự.";
        return null;
    }

    private static void Normalize(CustomerDTO customer)
    {
        customer.FullName = customer.FullName.Trim();
        customer.Phone = customer.Phone.Trim();
        customer.Email = customer.Email.Trim();
        customer.Gender = customer.Gender.Trim();
        customer.IdPassportNumber = string.IsNullOrWhiteSpace(customer.IdPassportNumber)
            ? null
            : customer.IdPassportNumber.Trim();
    }

    private static bool IsValidEmail(string email) =>
        MailAddress.TryCreate(email, out MailAddress? address) &&
        string.Equals(address.Address, email, StringComparison.OrdinalIgnoreCase);

    private static bool Contains(string value, string searchText) =>
        value.Contains(searchText, StringComparison.OrdinalIgnoreCase);
}
