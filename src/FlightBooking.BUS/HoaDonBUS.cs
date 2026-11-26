using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.BUS;

/// <summary>
/// BUS kiểm tra quy tắc hóa đơn, thanh toán, hủy và thống kê doanh thu.
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
        if (maHoaDon <= 0) return null;
        return dao.DocHoaDonTheoMa(maHoaDon);
    }

    public ArrayList DocChiTietHoaDon(int maHoaDon)
    {
        var ketQua = new ArrayList();
        if (maHoaDon <= 0) return ketQua;
        foreach (var ct in dao.DocChiTietHoaDon(maHoaDon))
        {
            ketQua.Add(ct);
        }
        return ketQua;
    }

    public string? ThanhToanHoaDon(int maHoaDon)
    {
        if (maHoaDon <= 0) return "Mã hóa đơn không hợp lệ.";
        var hd = dao.DocHoaDonTheoMa(maHoaDon);
        if (hd == null) return "Không tìm thấy hóa đơn.";
        if (hd.TrangThai == "PAID") return "Hóa đơn này đã được thanh toán.";
        if (hd.TrangThai == "CANCELLED") return "Hóa đơn này đã bị hủy, không thể thanh toán.";

        bool thanhCong = dao.ThanhToanHoaDon(maHoaDon);
        return thanhCong ? null : "Thanh toán hóa đơn thất bại. Vui lòng kiểm tra lại.";
    }

    public string? HuyHoaDon(int maHoaDon)
    {
        if (maHoaDon <= 0) return "Mã hóa đơn không hợp lệ.";
        var hd = dao.DocHoaDonTheoMa(maHoaDon);
        if (hd == null) return "Không tìm thấy hóa đơn.";
        if (hd.TrangThai == "CANCELLED") return "Hóa đơn này đã bị hủy.";

        bool thanhCong = dao.HuyHoaDon(maHoaDon);
        return thanhCong ? null : "Hủy hóa đơn thất bại. Vui lòng kiểm tra lại.";
    }

    public ThongKeDoanhThuDTO ThongKeTongHop()
    {
        return dao.ThongKeTongHop();
    }

    public ArrayList TimKiemHoaDon(ArrayList danhSach, string tuKhoa, string trangThai = "TAT_CA")
    {
        var ketQua = new ArrayList();
        string key = (tuKhoa ?? "").Trim();
        string tt = (trangThai ?? "TAT_CA").Trim().ToUpperInvariant();

        foreach (object item in danhSach)
        {
            if (item is not HoaDonDTO hd) continue;

            bool khopTrangThai = tt == "TAT_CA" ||
                (tt == "UNPAID" && hd.TrangThai.Equals("UNPAID", StringComparison.OrdinalIgnoreCase)) ||
                (tt == "PAID" && hd.TrangThai.Equals("PAID", StringComparison.OrdinalIgnoreCase)) ||
                (tt == "CANCELLED" && hd.TrangThai.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase));

            if (!khopTrangThai) continue;

            if (string.IsNullOrEmpty(key))
            {
                ketQua.Add(hd);
                continue;
            }

            bool khopTuKhoa =
                hd.MaHoaDon.ToString().Contains(key, StringComparison.OrdinalIgnoreCase) ||
                hd.MaDatVe.ToString().Contains(key, StringComparison.OrdinalIgnoreCase) ||
                hd.TenKhachHang.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                hd.DienThoaiKhachHang.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                hd.EmailKhachHang.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                hd.TongTien.ToString("N0").Contains(key, StringComparison.OrdinalIgnoreCase);

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
