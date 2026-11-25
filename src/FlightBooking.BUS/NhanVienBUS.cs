using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;
using System.Net.Mail;

namespace FlightBooking.BUS;

// BUS kiểm tra dữ liệu nhân viên trước khi gọi NhanVienDAO.
public class NhanVienBUS
{
    private readonly NhanVienDAO dao = new();

    public ArrayList DocDSNhanVien()
    {
        ArrayList danhSach = new ArrayList();
        foreach (NhanVienDTO nhanVien in dao.DocDSNhanVien())
        {
            danhSach.Add(nhanVien);
        }
        return danhSach;
    }

    public NhanVienDTO? DocNhanVienTheoMa(int maNhanVien)
    {
        if (maNhanVien <= 0) return null;
        return dao.DocNhanVienTheoMa(maNhanVien);
    }

    public string? ThemNhanVien(NhanVienDTO nhanVien)
    {
        ChuanHoaNhanVien(nhanVien);
        string? loi = KiemTraNhanVien(nhanVien);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(nhanVien.DienThoai)) return "Số điện thoại đã được sử dụng.";
        if (dao.TrungEmail(nhanVien.Email)) return "Email đã được sử dụng.";
        if (!dao.ThemNhanVien(nhanVien))
        {
            return "Không thêm được nhân viên.";
        }
        return null;
    }

    public string? SuaNhanVien(NhanVienDTO nhanVien)
    {
        if (nhanVien.MaNhanVien <= 0) return "Mã nhân viên không hợp lệ.";
        ChuanHoaNhanVien(nhanVien);
        string? loi = KiemTraNhanVien(nhanVien);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(nhanVien.DienThoai, nhanVien.MaNhanVien)) return "Số điện thoại đã được nhân viên khác sử dụng.";
        if (dao.TrungEmail(nhanVien.Email, nhanVien.MaNhanVien)) return "Email đã được nhân viên khác sử dụng.";
        if (!dao.SuaNhanVien(nhanVien))
        {
            return "Không tìm thấy nhân viên để cập nhật.";
        }
        return null;
    }

    public string? XoaNhanVien(int maNhanVien)
    {
        if (maNhanVien <= 0) return "Mã nhân viên không hợp lệ.";
        if (!dao.XoaNhanVien(maNhanVien))
        {
            return "Không tìm thấy nhân viên để xóa.";
        }
        return null;
    }

    public ArrayList TimKiemNhanVien(string? tuKhoa)
    {
        string chuoiTim = tuKhoa?.Trim() ?? string.Empty;
        var ketQua = new ArrayList();

        ArrayList danhSach = DocDSNhanVien();
        foreach (NhanVienDTO nhanVien in danhSach)
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
    public ArrayList TimKiemNhanVienNangCao(ArrayList cacDieuKien)
    {
        ArrayList danhSach = DocDSNhanVien();
        return TimKiemArrayList.LocDanhSach(danhSach, cacDieuKien);
    }


    private static string? KiemTraNhanVien(NhanVienDTO nhanVien)
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

    private static void ChuanHoaNhanVien(NhanVienDTO nhanVien)
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
