using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và ánh xạ bảng EMPLOYEE thành NhanVienDTO.
public class NhanVienDAO
{
    private const string CacCot = "employee_id, full_name, phone, email, position, hire_date, status";

    // Đọc toàn bộ nhân viên, mỗi dòng SQL thành một DTO.
    public List<NhanVienDTO> DocDanhSach()
    {
        const string cauSQL = $"SELECT {CacCot} FROM dbo.EMPLOYEE ORDER BY employee_id;";
        var danhSachNhanVien = new List<NhanVienDTO>();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ketNoi.Open();
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();

        while (docDuLieu.Read())
        {
            danhSachNhanVien.Add(DocNhanVien(docDuLieu));
        }

        return danhSachNhanVien;
    }

    // Đọc theo khóa chính; trả null khi không có nhân viên đó.
    public NhanVienDTO? DocTheoMa(int maNhanVien)
    {
        string cauSQL = $"SELECT {CacCot} FROM dbo.EMPLOYEE WHERE employee_id = @EmployeeId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = maNhanVien;
        ketNoi.Open();
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();
        return docDuLieu.Read() ? DocNhanVien(docDuLieu) : null;
    }

    // Kiểm tra trùng phone/email; có thể loại trừ ID hiện tại khi cập nhật.
    public bool TrungDienThoai(string dienThoai, int maLoaiTru = 0) =>
        TrungGiaTri("phone", 20, dienThoai, maLoaiTru);

    public bool TrungEmail(string email, int maLoaiTru = 0) =>
        TrungGiaTri("email", 100, email, maLoaiTru);

    // column chỉ nhận tên cột cố định từ các hàm ExistsByPhone/ExistsByEmail.
    // column chỉ nhận tên cột cố định từ các hàm ExistsByPhone/ExistsByEmail.
    private static bool TrungGiaTri(string cot, int kichThuoc, string giaTri, int maLoaiTru)
    {
        string cauSQL = $"SELECT COUNT(1) FROM dbo.EMPLOYEE WHERE {cot} = @Value AND employee_id <> @ExcludedId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@Value", SqlDbType.VarChar, kichThuoc).Value = giaTri;
        lenhSQL.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = maLoaiTru;
        ketNoi.Open();
        return Convert.ToInt32(lenhSQL.ExecuteScalar()) > 0;
    }

    // Thêm nhân viên; employee_id do SQL Server tự tạo.
    public bool Them(NhanVienDTO nhanVien)
    {
        const string cauSQL = """
            INSERT INTO dbo.EMPLOYEE(full_name, phone, email, position, hire_date, status)
            VALUES (@FullName, @Phone, @Email, @Position, @HireDate, @Status);
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoNhanVien(lenhSQL, nhanVien);
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Cập nhật nhân viên theo ID.
    public bool Sua(NhanVienDTO nhanVien)
    {
        const string cauSQL = """
            UPDATE dbo.EMPLOYEE
            SET full_name = @FullName, phone = @Phone, email = @Email,
                position = @Position, hire_date = @HireDate, status = @Status
            WHERE employee_id = @EmployeeId;
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoNhanVien(lenhSQL, nhanVien);
        lenhSQL.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = nhanVien.MaNhanVien;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Xóa theo ID; khóa ngoại từ BOOKING có thể từ chối xóa.
    public bool Xoa(int maNhanVien)
    {
        const string cauSQL = "DELETE FROM dbo.EMPLOYEE WHERE employee_id = @EmployeeId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@EmployeeId", SqlDbType.Int).Value = maNhanVien;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Dùng SQL parameters có kiểu và kích thước theo schema.
    private static void ThemThamSoNhanVien(SqlCommand lenhSQL, NhanVienDTO nhanVien)
    {
        lenhSQL.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = nhanVien.HoTen;
        lenhSQL.Parameters.Add("@Phone", SqlDbType.VarChar, 20).Value = nhanVien.DienThoai;
        lenhSQL.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = nhanVien.Email;
        lenhSQL.Parameters.Add("@Position", SqlDbType.NVarChar, 50).Value = nhanVien.ChucVu;
        lenhSQL.Parameters.Add("@HireDate", SqlDbType.Date).Value = nhanVien.NgayVaoLam.Date;
        lenhSQL.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = nhanVien.TrangThai;
    }

    // Ánh xạ một dòng trong reader thành NhanVienDTO.
    private static NhanVienDTO DocNhanVien(SqlDataReader docDuLieu) => new()
    {
        MaNhanVien = docDuLieu.GetInt32(docDuLieu.GetOrdinal("employee_id")),
        HoTen = docDuLieu.GetString(docDuLieu.GetOrdinal("full_name")),
        DienThoai = docDuLieu.GetString(docDuLieu.GetOrdinal("phone")),
        Email = docDuLieu.GetString(docDuLieu.GetOrdinal("email")),
        ChucVu = docDuLieu.GetString(docDuLieu.GetOrdinal("position")),
        NgayVaoLam = docDuLieu.GetDateTime(docDuLieu.GetOrdinal("hire_date")),
        TrangThai = docDuLieu.GetString(docDuLieu.GetOrdinal("status"))
    };
}
