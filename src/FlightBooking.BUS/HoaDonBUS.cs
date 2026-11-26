using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.BUS;

/// <summary>
/// BUS kiểm tra chứng từ và quy tắc thanh toán, hủy hóa đơn.
/// </summary>
public class HoaDonBUS
{
    private readonly HoaDonDAO dao = new();

    public ArrayList DocDSHoaDon()
    {
        var ketQua = new ArrayList();
        foreach (var hd in dao.DocDSHoaDon())
        {
            ketQua.Add(hd);
        }
        return ketQua;
    }

    public HoaDonDTO? DocHoaDonTheoMa(int maHoaDon)
    {
        if (maHoaDon <= 0)
            return null;
        return dao.DocHoaDonTheoMa(maHoaDon);
    }

    public ArrayList DocChiTietHoaDon(int maHoaDon)
    {
        var ketQua = new ArrayList();
        if (maHoaDon <= 0)
            return ketQua;
        foreach (var ct in dao.DocChiTietHoaDon(maHoaDon))
        {
            ketQua.Add(ct);
        }
        return ketQua;
    }

    public string? ThanhToanHoaDon(int maHoaDon, decimal? soTienDaXacNhan = null)
    {
        if (maHoaDon <= 0)
            return "Mã hóa đơn không hợp lệ.";
        var hd = dao.DocHoaDonTheoMa(maHoaDon);
        if (hd == null)
            return "Không tìm thấy hóa đơn.";
        if (hd.TrangThai == "PAID")
            return "Hóa đơn này đã được thanh toán.";
        if (hd.TrangThai == "CANCELLED")
            return "Hóa đơn này đã bị hủy, không thể thanh toán.";
        string? loi = KiemTraChungTu(hd);
        if (loi is not null)
            return loi;
        if (soTienDaXacNhan.HasValue && soTienDaXacNhan.Value != hd.TongTien)
            return "Số tiền vừa thay đổi. Hãy tải lại và xác nhận hóa đơn mới.";

        bool thanhCong = dao.ThanhToanHoaDon(maHoaDon, soTienDaXacNhan ?? hd.TongTien);
        return thanhCong ? null : "Thanh toán hóa đơn thất bại. Vui lòng kiểm tra lại.";
    }

    public static string? KiemTraChungTu(HoaDonDTO hoaDon)
    {
        if (hoaDon.MaHoaDon <= 0 || hoaDon.MaDatVe <= 0)
            return "Mã chứng từ không hợp lệ.";
        if (hoaDon.TrangThai is not ("PAID" or "UNPAID" or "CANCELLED"))
            return "Trạng thái hóa đơn không hợp lệ.";
        if (hoaDon.TongTienGoc < 0 || hoaDon.TienGiam < 0 || hoaDon.TienGiam > hoaDon.TongTienGoc || hoaDon.TienThue < 0 || hoaDon.TongTien != hoaDon.TongTienGoc - hoaDon.TienGiam + hoaDon.TienThue)
            return "Tổng thanh toán không khớp tạm tính, giảm giá và thuế.";
        if (hoaDon.DanhSachChiTiet.Count == 0 || hoaDon.SoLuongVe != hoaDon.DanhSachChiTiet.Count)
            return "Chưa có đủ chi tiết vé của hóa đơn.";
        decimal tong = 0;
        var cacMaVe = new HashSet<int>();
        foreach (var dong in hoaDon.DanhSachChiTiet)
        {
            if (dong.MaHoaDon != hoaDon.MaHoaDon)
                return "Chi tiết không thuộc hóa đơn đang chọn.";
            if (dong.MaVe <= 0 || !cacMaVe.Add(dong.MaVe))
                return "Mã vé bị thiếu hoặc trùng trong hóa đơn.";
            if (dong.DonGia < 0 || dong.SoLuong != 1 || dong.ThanhTien != dong.DonGia * dong.SoLuong)
                return "Thành tiền chi tiết không hợp lệ.";
            tong += dong.ThanhTien;
        }
        if (tong != hoaDon.TongTienGoc)
            return "Tạm tính không bằng tổng chi tiết lịch sử.";
        return null;
    }

    public string? HuyHoaDon(int maHoaDon)
    {
        if (maHoaDon <= 0)
            return "Mã hóa đơn không hợp lệ.";
        var hd = dao.DocHoaDonTheoMa(maHoaDon);
        if (hd == null)
            return "Không tìm thấy hóa đơn.";
        if (hd.TrangThai == "CANCELLED")
            return "Hóa đơn này đã bị hủy.";
        if (hd.TrangThai == "PAID")
            return "Không hủy hóa đơn đã thanh toán. Hoàn tiền cần quy trình riêng được phê duyệt.";

        bool thanhCong = dao.HuyHoaDon(maHoaDon);
        return thanhCong ? null : "Hủy hóa đơn thất bại. Vui lòng kiểm tra lại.";
    }

    public ArrayList TimKiemHoaDon(ArrayList danhSach, string tuKhoa, string trangThai = "TAT_CA")
    {
        var ketQua = new ArrayList();
        string chuoiTim = (tuKhoa ?? "").Trim();
        string trangThaiLoc = (trangThai ?? "TAT_CA").Trim().ToUpperInvariant();

        foreach (object phanTu in danhSach)
        {
            if (phanTu is not HoaDonDTO hd)
                continue;

            bool khopTrangThai = trangThaiLoc == "TAT_CA" ||
                (trangThaiLoc == "UNPAID" && hd.TrangThai.Equals("UNPAID", StringComparison.OrdinalIgnoreCase)) ||
                (trangThaiLoc == "PAID" && hd.TrangThai.Equals("PAID", StringComparison.OrdinalIgnoreCase)) ||
                (trangThaiLoc == "CANCELLED" && hd.TrangThai.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase));

            if (!khopTrangThai)
                continue;

            if (string.IsNullOrEmpty(chuoiTim))
            {
                ketQua.Add(hd);
                continue;
            }

            bool khopTuKhoa =
                hd.MaHoaDon.ToString().Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                hd.MaDatVe.ToString().Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                hd.TenKhachHang.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                hd.DienThoaiKhachHang.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                hd.EmailKhachHang.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                hd.TongTien.ToString("N2").Contains(chuoiTim, StringComparison.OrdinalIgnoreCase);

            if (khopTuKhoa)
                ketQua.Add(hd);
        }

        return ketQua;
    }

    public ArrayList LocDanhSach(ArrayList danhSach, ArrayList cacDieuKien)
    {
        return TimKiemArrayList.LocDanhSach(danhSach, cacDieuKien);
    }
}
