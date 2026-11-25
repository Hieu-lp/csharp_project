using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và chuyển dữ liệu bảng CUSTOMER thành KhachHangDTO.
public class KhachHangDAO
{
    private const string CacCot = "customer_id, full_name, phone, email, date_of_birth, gender, id_passport_number";

    // Đọc nhiều khách hàng; mỗi dòng SQL trở thành một DTO trong List.
    public List<KhachHangDTO> DocDanhSach()
    {
        const string cauSQL = $"SELECT {CacCot} FROM dbo.CUSTOMER ORDER BY customer_id;";
        var danhSachKhachHang = new List<KhachHangDTO>();//tạo 1 danh sách rỗng để chứa các khách hàng tìm được
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ketNoi.Open();//mở kết nối đến sql
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();

        while (docDuLieu.Read())
        {
            danhSachKhachHang.Add(DocKhachHang(docDuLieu));
        }

        return danhSachKhachHang;
    }

    // Đọc một khách hàng theo khóa chính; trả null nếu ID không tồn tại.
    public KhachHangDTO? DocTheoMa(int maKhachHang)
    {
        string cauSQL = $"SELECT {CacCot} FROM dbo.CUSTOMER WHERE customer_id = @CustomerId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@CustomerId", SqlDbType.Int).Value = maKhachHang;
        ketNoi.Open();
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();

        return docDuLieu.Read() ? DocKhachHang(docDuLieu) : null;
    }

    // Ba hàm sau phục vụ BUS kiểm tra trùng; khi sửa sẽ bỏ qua chính ID đang sửa.
    public bool TrungDienThoai(string dienThoai, int maLoaiTru = 0) =>
        TrungGiaTri("phone", SqlDbType.VarChar, 20, dienThoai, maLoaiTru);

    public bool TrungEmail(string email, int maLoaiTru = 0) =>
        TrungGiaTri("email", SqlDbType.VarChar, 100, email, maLoaiTru);

    public bool TrungGiayTo(string soGiayTo, int maLoaiTru = 0) =>
        TrungGiaTri("id_passport_number", SqlDbType.VarChar, 50, soGiayTo, maLoaiTru);

    // column chỉ được truyền từ ba tên cột cố định ở các hàm trên, không lấy từ người dùng.
    private static bool TrungGiaTri(string cot, SqlDbType kieu, int kichThuoc, string giaTri, int maLoaiTru)
    {
        string cauSQL = $"SELECT COUNT(1) FROM dbo.CUSTOMER WHERE {cot} = @Value AND customer_id <> @ExcludedId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@Value", kieu, kichThuoc).Value = giaTri;
        lenhSQL.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = maLoaiTru;
        ketNoi.Open();
        return Convert.ToInt32(lenhSQL.ExecuteScalar()) > 0;
    }

    // Thêm khách hàng; customer_id được DB tự sinh nên không nằm trong INSERT.
    public bool Them(KhachHangDTO khachHang)
    {
        const string cauSQL = """
            INSERT INTO dbo.CUSTOMER(full_name, phone, email, date_of_birth, gender, id_passport_number)
            VALUES (@FullName, @Phone, @Email, @DateOfBirth, @Gender, @PassportNumber);
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoKhachHang(lenhSQL, khachHang);
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Cập nhật các trường được phép sửa, xác định dòng bằng ID.
    public bool Sua(KhachHangDTO khachHang)
    {
        const string cauSQL = """
            UPDATE dbo.CUSTOMER
            SET full_name = @FullName, phone = @Phone, email = @Email,
                date_of_birth = @DateOfBirth, gender = @Gender,
                id_passport_number = @PassportNumber
            WHERE customer_id = @CustomerId;
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoKhachHang(lenhSQL, khachHang);
        lenhSQL.Parameters.Add("@CustomerId", SqlDbType.Int).Value = khachHang.MaKhachHang;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Xóa một dòng; DB có thể từ chối nếu booking đang tham chiếu khách hàng.
    public bool Xoa(int maKhachHang)
    {
        const string cauSQL = "DELETE FROM dbo.CUSTOMER WHERE customer_id = @CustomerId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@CustomerId", SqlDbType.Int).Value = maKhachHang;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Tạo tham số có kiểu/kích thước khớp schema, tránh nối dữ liệu vào SQL.
    private static void ThemThamSoKhachHang(SqlCommand lenhSQL, KhachHangDTO khachHang)
    {
        lenhSQL.Parameters.Add("@FullName", SqlDbType.NVarChar, 100).Value = khachHang.HoTen;
        lenhSQL.Parameters.Add("@Phone", SqlDbType.VarChar, 20).Value = khachHang.DienThoai;
        lenhSQL.Parameters.Add("@Email", SqlDbType.VarChar, 100).Value = khachHang.Email;
        lenhSQL.Parameters.Add("@DateOfBirth", SqlDbType.Date).Value = khachHang.NgaySinh.Date;
        lenhSQL.Parameters.Add("@Gender", SqlDbType.NVarChar, 10).Value = khachHang.GioiTinh;
        lenhSQL.Parameters.Add("@PassportNumber", SqlDbType.VarChar, 50).Value =
            string.IsNullOrWhiteSpace(khachHang.SoGiayTo)
                ? DBNull.Value
                : khachHang.SoGiayTo.Trim();
    }

    // Chuyển cột reader thành property DTO; hộ chiếu cần xử lý SQL NULL.
    private static KhachHangDTO DocKhachHang(SqlDataReader docDuLieu)
    {
        int viTriGiayTo = docDuLieu.GetOrdinal("id_passport_number");
        return new KhachHangDTO
        {
            MaKhachHang = docDuLieu.GetInt32(docDuLieu.GetOrdinal("customer_id")),
            HoTen = docDuLieu.GetString(docDuLieu.GetOrdinal("full_name")),
            DienThoai = docDuLieu.GetString(docDuLieu.GetOrdinal("phone")),
            Email = docDuLieu.GetString(docDuLieu.GetOrdinal("email")),
            NgaySinh = docDuLieu.GetDateTime(docDuLieu.GetOrdinal("date_of_birth")),
            GioiTinh = docDuLieu.GetString(docDuLieu.GetOrdinal("gender")),
            SoGiayTo = docDuLieu.IsDBNull(viTriGiayTo) ? null : docDuLieu.GetString(viTriGiayTo)
        };
    }
}
