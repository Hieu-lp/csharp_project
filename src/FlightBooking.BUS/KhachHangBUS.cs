using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;
using System.Net.Mail;

namespace FlightBooking.BUS;

// BUS kiểm tra quy tắc khách hàng trước khi nhờ DAO truy cập database.
public class KhachHangBUS
{
    private readonly KhachHangDAO dao = new();

    public List<KhachHangDTO> DocDanhSach() => dao.DocDanhSach();

    public KhachHangDTO? DocTheoMa(int maKhachHang) =>
        maKhachHang > 0 ? dao.DocTheoMa(maKhachHang) : null;

    public string? Them(KhachHangDTO khachHang)
    {
        ChuanHoa(khachHang);
        string? loi = KiemTraDuLieu(khachHang);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(khachHang.DienThoai)) return "Số điện thoại đã được sử dụng.";
        if (dao.TrungEmail(khachHang.Email)) return "Email đã được sử dụng.";
        if (khachHang.SoGiayTo is not null && dao.TrungGiayTo(khachHang.SoGiayTo))
            return "Số giấy tờ tùy thân đã được sử dụng.";

        return dao.Them(khachHang) ? null : "Không thêm được khách hàng.";
    }

    public string? Sua(KhachHangDTO khachHang)
    {
        if (khachHang.MaKhachHang <= 0) return "Mã khách hàng không hợp lệ.";
        ChuanHoa(khachHang);
        string? loi = KiemTraDuLieu(khachHang);
        if (loi is not null) return loi;

        if (dao.TrungDienThoai(khachHang.DienThoai, khachHang.MaKhachHang)) return "Số điện thoại đã được khách hàng khác sử dụng.";
        if (dao.TrungEmail(khachHang.Email, khachHang.MaKhachHang)) return "Email đã được khách hàng khác sử dụng.";
        if (khachHang.SoGiayTo is not null &&
            dao.TrungGiayTo(khachHang.SoGiayTo, khachHang.MaKhachHang))
            return "Số giấy tờ tùy thân đã được khách hàng khác sử dụng.";

        return dao.Sua(khachHang) ? null : "Không tìm thấy khách hàng để cập nhật.";
    }

    // null nghĩa là xóa thành công; khóa ngoại có thể làm DAO ném SqlException.
    public string? Xoa(int maKhachHang)
    {
        if (maKhachHang <= 0) return "Mã khách hàng không hợp lệ.";
        return dao.Xoa(maKhachHang) ? null : "Không tìm thấy khách hàng để xóa.";
    }

    // Tìm gần đúng trong BUS; kết quả dùng ArrayList theo yêu cầu môn học.
    public ArrayList TimKiem(string? tuKhoa)
    {
        string chuoiTim = tuKhoa?.Trim() ?? string.Empty;
        var ketQua = new ArrayList();

        foreach (KhachHangDTO khachHang in dao.DocDanhSach())
        {
            bool khop = chuoiTim.Length == 0 ||
                ChuaChuoi(khachHang.MaKhachHang.ToString(), chuoiTim) ||
                ChuaChuoi(khachHang.HoTen, chuoiTim) ||
                ChuaChuoi(khachHang.DienThoai, chuoiTim) ||
                ChuaChuoi(khachHang.Email, chuoiTim) ||
                ChuaChuoi(khachHang.NgaySinh.ToString("yyyy-MM-dd"), chuoiTim) ||
                ChuaChuoi(khachHang.GioiTinh, chuoiTim) ||
                ChuaChuoi(khachHang.SoGiayTo ?? string.Empty, chuoiTim);

            if (khop) ketQua.Add(khachHang);
        }

        return ketQua;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList TimKiemNangCao(ArrayList cacDieuKien) =>
        TimKiemArrayList.LocDanhSach(dao.DocDanhSach(), cacDieuKien, LayGiaTriTruong);

    private static object? LayGiaTriTruong(KhachHangDTO khachHang, string truong) =>
        truong.Trim().Replace("_", string.Empty).ToUpperInvariant() switch
        {
            "CUSTOMERID" or "MAKHACHHANG" => khachHang.MaKhachHang,
            "FULLNAME" or "HOTEN" => khachHang.HoTen,
            "PHONE" or "DIENTHOAI" => khachHang.DienThoai,
            "EMAIL" => khachHang.Email,
            "DATEOFBIRTH" or "NGAYSINH" => khachHang.NgaySinh,
            "GENDER" or "GIOITINH" => khachHang.GioiTinh,
            "IDPASSPORTNUMBER" or "SOGIAYTO" => khachHang.SoGiayTo,
            _ => throw new ArgumentException($"Không có trường CUSTOMER tên '{truong}'.", nameof(truong))
        };

    private static string? KiemTraDuLieu(KhachHangDTO khachHang)
    {
        if (string.IsNullOrWhiteSpace(khachHang.HoTen)) return "Họ tên không được để trống.";
        if (khachHang.HoTen.Length > 100) return "Họ tên không được dài quá 100 ký tự.";
        if (string.IsNullOrWhiteSpace(khachHang.DienThoai)) return "Số điện thoại không được để trống.";
        if (khachHang.DienThoai.Length > 20) return "Số điện thoại không được dài quá 20 ký tự.";
        if (string.IsNullOrWhiteSpace(khachHang.Email) || !EmailHopLe(khachHang.Email)) return "Email không hợp lệ.";
        if (khachHang.Email.Length > 100) return "Email không được dài quá 100 ký tự.";
        if (khachHang.NgaySinh == default || khachHang.NgaySinh.Date > DateTime.Today) return "Ngày sinh không hợp lệ.";
        if (string.IsNullOrWhiteSpace(khachHang.GioiTinh)) return "Giới tính không được để trống.";
        if (khachHang.GioiTinh.Length > 10) return "Giới tính không được dài quá 10 ký tự.";
        if (khachHang.GioiTinh is not ("Nam" or "Nữ" or "Nu" or "Khác")) return "Giới tính không hợp lệ.";
        if (khachHang.SoGiayTo?.Length > 50) return "Số giấy tờ tùy thân không được dài quá 50 ký tự.";
        return null;
    }

    private static void ChuanHoa(KhachHangDTO khachHang)
    {
        khachHang.HoTen = khachHang.HoTen.Trim();
        khachHang.DienThoai = khachHang.DienThoai.Trim();
        khachHang.Email = khachHang.Email.Trim();
        khachHang.GioiTinh = khachHang.GioiTinh.Trim();
        khachHang.SoGiayTo = string.IsNullOrWhiteSpace(khachHang.SoGiayTo)
            ? null
            : khachHang.SoGiayTo.Trim();
    }

    private static bool EmailHopLe(string email) =>
        MailAddress.TryCreate(email, out MailAddress? diaChi) &&
        string.Equals(diaChi.Address, email, StringComparison.OrdinalIgnoreCase);

    private static bool ChuaChuoi(string giaTri, string chuoiTim) =>
        giaTri.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase);
}
