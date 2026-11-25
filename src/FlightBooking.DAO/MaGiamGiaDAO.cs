using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

// DAO chứa SQL và ánh xạ bảng DISCOUNT_CODE thành MaGiamGiaQuanLyDTO.
public class MaGiamGiaDAO
{
    private const string CacCot = "discount_id, code, description, discount_type, discount_value, min_order_amount, max_discount_amount, start_date, end_date, usage_limit, used_count, status";

    // Đọc tất cả mã giảm giá.
    public List<MaGiamGiaQuanLyDTO> DocDSMaGiamGia()
    {
        const string cauSQL = $"SELECT {CacCot} FROM dbo.DISCOUNT_CODE ORDER BY discount_id;";
        var danhSachMaGiamGia = new List<MaGiamGiaQuanLyDTO>();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ketNoi.Open();
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();

        while (docDuLieu.Read())
        {
            danhSachMaGiamGia.Add(DocMaGiamGia(docDuLieu));
        }

        return danhSachMaGiamGia;
    }

    // Đọc một mã theo khóa chính; trả null nếu không tồn tại.
    public MaGiamGiaQuanLyDTO? DocMaGiamGiaTheoMa(int maGiamGia)
    {
        string cauSQL = $"SELECT {CacCot} FROM dbo.DISCOUNT_CODE WHERE discount_id = @DiscountId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@DiscountId", SqlDbType.Int).Value = maGiamGia;
        ketNoi.Open();
        using SqlDataReader docDuLieu = lenhSQL.ExecuteReader();
        return docDuLieu.Read() ? DocMaGiamGia(docDuLieu) : null;
    }

    // Kiểm tra code trùng; khi sửa thì loại trừ chính mã hiện tại.
    public bool TrungMa(string ma, int maLoaiTru = 0)
    {
        const string cauSQL = "SELECT COUNT(1) FROM dbo.DISCOUNT_CODE WHERE code = @Code AND discount_id <> @ExcludedId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = ma;
        lenhSQL.Parameters.Add("@ExcludedId", SqlDbType.Int).Value = maLoaiTru;
        ketNoi.Open();
        return Convert.ToInt32(lenhSQL.ExecuteScalar()) > 0;
    }

    // Thêm mã mới; DB tự tạo ID và đặt used_count mặc định bằng 0.
    public bool ThemMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia)
    {
        // used_count không được nhập khi tạo mới; DB sẽ dùng giá trị mặc định 0.
        const string cauSQL = """
            INSERT INTO dbo.DISCOUNT_CODE
                (code, description, discount_type, discount_value, min_order_amount,
                 max_discount_amount, start_date, end_date, usage_limit, status)
            VALUES
                (@Code, @Description, @DiscountType, @DiscountValue, @MinOrderAmount,
                 @MaxDiscountAmount, @StartDate, @EndDate, @UsageLimit, @Status);
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoMaGiamGia(lenhSQL, maGiamGia);
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Cập nhật thông tin cấu hình, nhưng giữ nguyên used_count do hệ thống đặt vé quản lý.
    public bool SuaMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia)
    {
        // Không cập nhật used_count vì số lượt dùng do nghiệp vụ đặt vé quản lý.
        const string cauSQL = """
            UPDATE dbo.DISCOUNT_CODE
            SET code = @Code, description = @Description, discount_type = @DiscountType,
                discount_value = @DiscountValue, min_order_amount = @MinOrderAmount,
                max_discount_amount = @MaxDiscountAmount, start_date = @StartDate,
                end_date = @EndDate, usage_limit = @UsageLimit, status = @Status
            WHERE discount_id = @DiscountId
              AND (@UsageLimit IS NULL OR @UsageLimit >= used_count);
            """;
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        ThemThamSoMaGiamGia(lenhSQL, maGiamGia);
        lenhSQL.Parameters.Add("@DiscountId", SqlDbType.Int).Value = maGiamGia.MaGiamGia;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Xóa theo ID; DB có thể từ chối nếu booking đã tham chiếu mã.
    public bool XoaMaGiamGia(int maGiamGia)
    {
        const string cauSQL = "DELETE FROM dbo.DISCOUNT_CODE WHERE discount_id = @DiscountId;";
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand(cauSQL, ketNoi);
        lenhSQL.Parameters.Add("@DiscountId", SqlDbType.Int).Value = maGiamGia;
        ketNoi.Open();
        return lenhSQL.ExecuteNonQuery() == 1;
    }

    // Đặt kiểu SQL tường minh cho từng giá trị gửi xuống DB.
    private static void ThemThamSoMaGiamGia(SqlCommand lenhSQL, MaGiamGiaQuanLyDTO maGiamGia)
    {
        lenhSQL.Parameters.Add("@Code", SqlDbType.VarChar, 50).Value = maGiamGia.Ma;
        lenhSQL.Parameters.Add("@Description", SqlDbType.NVarChar, 255).Value = maGiamGia.MoTa;
        lenhSQL.Parameters.Add("@DiscountType", SqlDbType.VarChar, 10).Value = maGiamGia.LoaiGiam;
        ThemThamSoDecimal(lenhSQL, "@DiscountValue", maGiamGia.GiaTriGiam);
        ThemThamSoDecimal(lenhSQL, "@MinOrderAmount", maGiamGia.DonToiThieu);
        ThemThamSoDecimal(lenhSQL, "@MaxDiscountAmount", maGiamGia.GiamToiDa);
        ThemThamSoNgayGio(lenhSQL, "@StartDate", maGiamGia.NgayBatDau);
        ThemThamSoNgayGio(lenhSQL, "@EndDate", maGiamGia.NgayKetThuc);
        lenhSQL.Parameters.Add("@UsageLimit", SqlDbType.Int).Value =
            maGiamGia.GioiHanLuotDung.HasValue ? maGiamGia.GioiHanLuotDung.Value : DBNull.Value;
        lenhSQL.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = maGiamGia.TrangThai;
    }

    // DECIMAL(18,2) cần khai báo precision và scale cho parameter.
    private static void ThemThamSoDecimal(SqlCommand lenhSQL, string ten, decimal? giaTri)
    {
        SqlParameter thamSo = lenhSQL.Parameters.Add(ten, SqlDbType.Decimal);
        thamSo.Precision = 18;
        thamSo.Scale = 2;
        thamSo.Value = giaTri.HasValue ? giaTri.Value : DBNull.Value;
    }

    // Schema dùng DATETIME2(0), tức độ chính xác tới giây.
    private static void ThemThamSoNgayGio(SqlCommand lenhSQL, string ten, DateTime giaTri)
    {
        SqlParameter thamSo = lenhSQL.Parameters.Add(ten, SqlDbType.DateTime2);
        thamSo.Scale = 0;
        thamSo.Value = giaTri;
    }

    // Ánh xạ một dòng SQL; hai cột nullable phải kiểm tra IsDBNull trước khi đọc.
    private static MaGiamGiaQuanLyDTO DocMaGiamGia(SqlDataReader docDuLieu)
    {
        int viTriGiamToiDa = docDuLieu.GetOrdinal("max_discount_amount");
        int viTriGioiHan = docDuLieu.GetOrdinal("usage_limit");
        return new MaGiamGiaQuanLyDTO
        {
            MaGiamGia = docDuLieu.GetInt32(docDuLieu.GetOrdinal("discount_id")),
            Ma = docDuLieu.GetString(docDuLieu.GetOrdinal("code")),
            MoTa = docDuLieu.GetString(docDuLieu.GetOrdinal("description")),
            LoaiGiam = docDuLieu.GetString(docDuLieu.GetOrdinal("discount_type")),
            GiaTriGiam = docDuLieu.GetDecimal(docDuLieu.GetOrdinal("discount_value")),
            DonToiThieu = docDuLieu.GetDecimal(docDuLieu.GetOrdinal("min_order_amount")),
            GiamToiDa = docDuLieu.IsDBNull(viTriGiamToiDa) ? null : docDuLieu.GetDecimal(viTriGiamToiDa),
            NgayBatDau = docDuLieu.GetDateTime(docDuLieu.GetOrdinal("start_date")),
            NgayKetThuc = docDuLieu.GetDateTime(docDuLieu.GetOrdinal("end_date")),
            GioiHanLuotDung = docDuLieu.IsDBNull(viTriGioiHan) ? null : docDuLieu.GetInt32(viTriGioiHan),
            SoLuotDaDung = docDuLieu.GetInt32(docDuLieu.GetOrdinal("used_count")),
            TrangThai = docDuLieu.GetString(docDuLieu.GetOrdinal("status"))
        };
    }
}
