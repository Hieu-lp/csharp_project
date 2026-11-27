using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.BUS;

/// <summary>
/// BUS kiểm tra quy tắc nghiệp vụ vé máy bay và tìm kiếm trên ArrayList.
/// </summary>
public class VeMayBayBUS
{
    private readonly VeMayBayDAO dao = new();

    public void SuaHanhKhach(int maVe, ThongTinHanhKhach thongTin)
        => new DatVeBUS().SuaThongTinHanhKhach(maVe, thongTin);

    public ArrayList DocDSVe()
    {
        var ketQua = new ArrayList();
        foreach (var ve in dao.DocDSVe())
        {
            ketQua.Add(ve);
        }
        return ketQua;
    }

    public VeMayBayDTO? DocVeTheoMa(int maVe)
    {
        if (maVe <= 0) return null;
        return dao.DocVeTheoMa(maVe);
    }

    public ArrayList DocVeTheoChuyenBay(int maChuyenBay)
    {
        var ketQua = new ArrayList();
        if (maChuyenBay <= 0) return ketQua;
        foreach (var ve in dao.DocVeTheoChuyenBay(maChuyenBay))
        {
            ketQua.Add(ve);
        }
        return ketQua;
    }

    public string? HuyDonChuaVe(int maVe)
    {
        if (maVe <= 0) return "Mã vé không hợp lệ.";
        var ve = dao.DocVeTheoMa(maVe);
        if (ve == null) return "Không tìm thấy vé trong hệ thống.";
        if (ve.TrangThai == "CANCELLED") return "Vé này đã bị hủy trước đó.";
        if (ve.TrangThaiHoaDon != "UNPAID") return "Chỉ hủy toàn bộ đơn UNPAID. Không hủy vé lẻ hoặc tự hoàn tiền đơn PAID.";

        bool thanhCong = dao.HuyDonChuaVe(maVe);
        return thanhCong ? null : "Hủy vé thất bại. Vui lòng thử lại.";
    }

    public ArrayList TimKiemVe(ArrayList danhSach, string tuKhoa, string trangThai = "TAT_CA")
    {
        var ketQua = new ArrayList();
        string chuoiTim = (tuKhoa ?? "").Trim();
        string trangThaiLoc = (trangThai ?? "TAT_CA").Trim().ToUpperInvariant();

        foreach (object phanTu in danhSach)
        {
            if (phanTu is not VeMayBayDTO ve) continue;

            bool khopTrangThai = trangThaiLoc == "TAT_CA" ||
                (trangThaiLoc == "ISSUED" && ve.TrangThai.Equals("ISSUED", StringComparison.OrdinalIgnoreCase)) ||
                (trangThaiLoc == "CANCELLED" && ve.TrangThai.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase));

            if (!khopTrangThai) continue;

            if (string.IsNullOrEmpty(chuoiTim))
            {
                ketQua.Add(ve);
                continue;
            }

            bool khopTuKhoa =
                ve.MaVe.ToString().Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                (ve.MaDatVe?.ToString().Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ?? false) ||
                ve.SoGhe.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.TenHanhKhach.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.SoGiayTo.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.SanBayDi.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.SanBayDen.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.TenHangVe.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase) ||
                ve.MaChuyenBay.ToString().Contains(chuoiTim, StringComparison.OrdinalIgnoreCase);

            if (khopTuKhoa)
                ketQua.Add(ve);
        }

        return ketQua;
    }

    public ArrayList LocDanhSach(ArrayList danhSach, ArrayList cacDieuKien)
    {
        return TimKiemArrayList.LocDanhSach(danhSach, cacDieuKien);
    }
}
