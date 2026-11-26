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

    public string? HuyVe(int maVe)
    {
        if (maVe <= 0) return "Mã vé không hợp lệ.";
        var ve = dao.DocVeTheoMa(maVe);
        if (ve == null) return "Không tìm thấy vé trong hệ thống.";
        if (ve.TrangThai == "CANCELLED") return "Vé này đã bị hủy trước đó.";

        bool thanhCong = dao.HuyVe(maVe);
        return thanhCong ? null : "Hủy vé thất bại. Vui lòng thử lại.";
    }

    public ArrayList TimKiemVe(ArrayList danhSach, string tuKhoa, string trangThai = "TAT_CA")
    {
        var ketQua = new ArrayList();
        string key = (tuKhoa ?? "").Trim();
        string tt = (trangThai ?? "TAT_CA").Trim().ToUpperInvariant();

        foreach (object item in danhSach)
        {
            if (item is not VeMayBayDTO ve) continue;

            bool khopTrangThai = tt == "TAT_CA" ||
                (tt == "ISSUED" && ve.TrangThai.Equals("ISSUED", StringComparison.OrdinalIgnoreCase)) ||
                (tt == "CANCELLED" && ve.TrangThai.Equals("CANCELLED", StringComparison.OrdinalIgnoreCase));

            if (!khopTrangThai) continue;

            if (string.IsNullOrEmpty(key))
            {
                ketQua.Add(ve);
                continue;
            }

            bool khopTuKhoa =
                ve.MaVe.ToString().Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.SoGhe.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.TenHanhKhach.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.SoGiayTo.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.SanBayDi.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.SanBayDen.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.TenHangVe.Contains(key, StringComparison.OrdinalIgnoreCase) ||
                ve.MaChuyenBay.ToString().Contains(key, StringComparison.OrdinalIgnoreCase);

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
