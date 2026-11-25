using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;
using System.Net.Mail;

namespace FlightBooking.BUS;

// BUS kiểm tra dữ liệu nhân viên trước khi gọi NhanVienDAO.
public class NhanVienBUS
{
    private readonly NhanVienDAO dao = new();

    public List<NhanVienDTO> DocDanhSach() => dao.DocDanhSach();

    public NhanVienDTO? DocTheoMa(int maNhanVien) =>
        maNhanVien > 0 ? dao.DocTheoMa(maNhanVien) : null;

    public string? Them(NhanVienDTO nhanVien)
    {
        ChuanHoa(nhanVien);
        string? loi = KiemTraDuLieu(nhanVien);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(nhanVien.DienThoai)) return "Số điện thoại đã được sử dụng.";
        if (dao.TrungEmail(nhanVien.Email)) return "Email đã được sử dụng.";
        return dao.Them(nhanVien) ? null : "Không thêm được nhân viên.";
    }

    public string? Sua(NhanVienDTO nhanVien)
    {
        if (nhanVien.MaNhanVien <= 0) return "Mã nhân viên không hợp lệ.";
        ChuanHoa(nhanVien);
        string? loi = KiemTraDuLieu(nhanVien);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(nhanVien.DienThoai, nhanVien.MaNhanVien)) return "Số điện thoại đã được nhân viên khác sử dụng.";
        if (dao.TrungEmail(nhanVien.Email, nhanVien.MaNhanVien)) return "Email đã được nhân viên khác sử dụng.";
        return dao.Sua(nhanVien) ? null : "Không tìm thấy nhân viên để cập nhật.";
    }

    public string? Xoa(int maNhanVien)
    {
        if (maNhanVien <= 0) return "Mã nhân viên không hợp lệ.";
        return dao.Xoa(maNhanVien) ? null : "Không tìm thấy nhân viên để xóa.";
    }

    public ArrayList TimKiem(string? tuKhoa)
    {
        string chuoiTim = tuKhoa?.Trim() ?? string.Empty;
        var ketQua = new ArrayList();

        foreach (NhanVienDTO nhanVien in dao.DocDanhSach())
        {
            bool khop = chuoiTim.Length == 0 ||
                ChuaChuoi(nhanVien.MaNhanVien.ToString(), chuoiTim) ||
                ChuaChuoi(nhanVien.HoTen, chuoiTim) ||
                ChuaChuoi(nhanVien.DienThoai, chuoiTim) ||
                ChuaChuoi(nhanVien.Email, chuoiTim) ||
                ChuaChuoi(nhanVien.ChucVu, chuoiTim) ||
                ChuaChuoi(nhanVien.NgayVaoLam.ToString("yyyy-MM-dd"), chuoiTim) ||
                ChuaChuoi(nhanVien.TrangThai, chuoiTim);

            if (khop) ketQua.Add(nhanVien);
        }

        return ketQua;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList TimKiemNangCao(ArrayList cacDieuKien) =>
        TimKiemArrayList.LocDanhSach(dao.DocDanhSach(), cacDieuKien, LayGiaTriTruong);

    private static object? LayGiaTriTruong(NhanVienDTO nhanVien, string truong) =>
        truong.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "EMPLOYEEID" or "MANHANVIEN" => nhanVien.MaNhanVien,
            "FULLNAME" or "HOTEN" => nhanVien.HoTen,
            "PHONE" or "DIENTHOAI" => nhanVien.DienThoai,
            "EMAIL" => nhanVien.Email,
            "POSITION" or "CHUCVU" => nhanVien.ChucVu,
            "HIREDATE" or "NGAYVAOLAM" => nhanVien.NgayVaoLam,
            "STATUS" or "TRANGTHAI" => nhanVien.TrangThai,
            _ => throw new ArgumentException($"Không có trường EMPLOYEE tên '{truong}'.", nameof(truong))
        };

    private static string? KiemTraDuLieu(NhanVienDTO nhanVien)
    {
        if (string.IsNullOrWhiteSpace(nhanVien.HoTen)) return "Họ tên không được để trống.";
        if (nhanVien.HoTen.Length > 100) return "Họ tên không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(nhanVien.DienThoai)) return "Số điện thoại không được để trống.";
        if (nhanVien.DienThoai.Length > 20) return "Số điện thoại không được dài quá 20 ký tự.";
        if (string.IsNullOrWhiteSpace(nhanVien.Email) || !EmailHopLe(nhanVien.Email)) return "Email không hợp lệ.";
        if (nhanVien.Email.Length > 100) return "Email không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(nhanVien.ChucVu)) return "Chức vụ không được để trống.";
        if (nhanVien.ChucVu.Length > 50) return "Chức vụ không được dài quá 50 ký tự.";
        if (nhanVien.NgayVaoLam == default) return "Ngày vào làm không hợp lệ.";
        if (nhanVien.TrangThai is not ("ACTIVE" or "INACTIVE")) return "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.";
        return null;
    }

    private static void ChuanHoa(NhanVienDTO nhanVien)
    {
        nhanVien.HoTen = nhanVien.HoTen.Trim();
        nhanVien.DienThoai = nhanVien.DienThoai.Trim();
        nhanVien.Email = nhanVien.Email.Trim();
        nhanVien.ChucVu = nhanVien.ChucVu.Trim();
        nhanVien.TrangThai = nhanVien.TrangThai.Trim().ToUpperInvariant();
    }

    private static bool EmailHopLe(string email) =>
        MailAddress.TryCreate(email, out MailAddress? diaChi) &&
        string.Equals(diaChi.Address, email, StringComparison.OrdinalIgnoreCase);

    private static bool ChuaChuoi(string giaTri, string chuoiTim) =>
        giaTri.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase);
}
