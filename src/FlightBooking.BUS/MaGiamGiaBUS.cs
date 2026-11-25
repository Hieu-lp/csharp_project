using FlightBooking.DAO;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.BUS;

// BUS kiểm tra luật mã giảm giá trước khi gọi MaGiamGiaDAO.
public class MaGiamGiaBUS
{
    private readonly MaGiamGiaDAO dao = new();

    public ArrayList DocDSMaGiamGia()
    {
        ArrayList danhSach = new ArrayList();
        foreach (MaGiamGiaQuanLyDTO maGiamGia in dao.DocDSMaGiamGia())
        {
            danhSach.Add(maGiamGia);
        }
        return danhSach;
    }

    public MaGiamGiaQuanLyDTO? DocMaGiamGiaTheoMa(int maGiamGia)
    {
        if (maGiamGia <= 0) return null;
        return dao.DocMaGiamGiaTheoMa(maGiamGia);
    }

    public string? ThemMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia)
    {
        ChuanHoaMaGiamGia(maGiamGia);
        string? loi = KiemTraMaGiamGia(maGiamGia, soLuotDaDung: 0);
        if (loi is not null) return loi;
        if (dao.TrungMa(maGiamGia.Ma)) return "Mã giảm giá đã tồn tại.";

        // Không nhận SoLuotDaDung từ form; DAO để database áp dụng mặc định 0.
        if (!dao.ThemMaGiamGia(maGiamGia))
        {
            return "Không thêm được mã giảm giá.";
        }
        return null;
    }

    public string? SuaMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia)
    {
        if (maGiamGia.MaGiamGia <= 0) return "Mã giảm giá không hợp lệ.";
        ChuanHoaMaGiamGia(maGiamGia);

        // Lấy SoLuotDaDung hiện tại từ DB để không cho giảm hạn mức thấp hơn số đã dùng.
        MaGiamGiaQuanLyDTO? duLieuHienTai = dao.DocMaGiamGiaTheoMa(maGiamGia.MaGiamGia);
        if (duLieuHienTai is null) return "Không tìm thấy mã giảm giá để cập nhật.";

        string? loi = KiemTraMaGiamGia(maGiamGia, duLieuHienTai.SoLuotDaDung);
        if (loi is not null) return loi;
        if (dao.TrungMa(maGiamGia.Ma, maGiamGia.MaGiamGia)) return "Mã giảm giá đã được mã khác sử dụng.";

        // Dùng số lượt dùng thật từ DB; không lấy số này từ form.
        maGiamGia.SoLuotDaDung = duLieuHienTai.SoLuotDaDung;
        if (!dao.SuaMaGiamGia(maGiamGia))
        {
            return "Không cập nhật được mã giảm giá. Hãy tải lại; mã có thể đã bị xóa hoặc lượt dùng đã vượt hạn mức đang nhập.";
        }
        return null;
    }

    public string? XoaMaGiamGia(int maGiamGia)
    {
        if (maGiamGia <= 0) return "Mã giảm giá không hợp lệ.";
        if (!dao.XoaMaGiamGia(maGiamGia))
        {
            return "Không tìm thấy mã giảm giá để xóa.";
        }
        return null;
    }

    public ArrayList TimKiemMaGiamGia(string? tuKhoa)
    {
        string chuoiTim = tuKhoa?.Trim() ?? string.Empty;
        var ketQua = new ArrayList();

        ArrayList danhSach = DocDSMaGiamGia();
        foreach (MaGiamGiaQuanLyDTO maGiamGia in danhSach)
        {
            bool khop = chuoiTim.Length == 0 ||
                ChuaChuoi(maGiamGia.MaGiamGia.ToString(), chuoiTim) ||
                ChuaChuoi(maGiamGia.Ma, chuoiTim) ||
                ChuaChuoi(maGiamGia.MoTa, chuoiTim) ||
                ChuaChuoi(maGiamGia.LoaiGiam, chuoiTim) ||
                ChuaChuoi(maGiamGia.GiaTriGiam.ToString(), chuoiTim) ||
                ChuaChuoi(maGiamGia.DonToiThieu.ToString(), chuoiTim) ||
                ChuaChuoi(maGiamGia.GiamToiDa?.ToString() ?? string.Empty, chuoiTim) ||
                ChuaChuoi(maGiamGia.TrangThai, chuoiTim) ||
                ChuaChuoi(maGiamGia.GioiHanLuotDung?.ToString() ?? string.Empty, chuoiTim) ||
                ChuaChuoi(maGiamGia.SoLuotDaDung.ToString(), chuoiTim) ||
                ChuaChuoi(maGiamGia.NgayBatDau.ToString("yyyy-MM-dd HH:mm:ss"), chuoiTim) ||
                ChuaChuoi(maGiamGia.NgayKetThuc.ToString("yyyy-MM-dd HH:mm:ss"), chuoiTim);

            if (khop) ketQua.Add(maGiamGia);
        }

        return ketQua;
    }

    // Tìm kiếm nhiều trường/điều kiện; lọc trong BUS bằng ArrayList.
    public ArrayList TimKiemMaGiamGiaNangCao(ArrayList cacDieuKien)
    {
        ArrayList danhSach = DocDSMaGiamGia();
        return TimKiemArrayList.LocDanhSach(danhSach, cacDieuKien);
    }


    private static string? KiemTraMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia, int soLuotDaDung)
    {
        if (string.IsNullOrWhiteSpace(maGiamGia.Ma)) return "Mã giảm giá không được để trống.";
        if (maGiamGia.Ma.Length > 50) return "Mã giảm giá không được dài quá 50 ký tự.";
        if (string.IsNullOrWhiteSpace(maGiamGia.MoTa)) return "Mô tả không được để trống.";
        if (maGiamGia.MoTa.Length > 255) return "Mô tả không được dài quá 255 ký tự.";
        if (maGiamGia.LoaiGiam is not ("PERCENT" or "FIXED")) return "Loại giảm chỉ nhận PERCENT hoặc FIXED.";
        if (maGiamGia.GiaTriGiam <= 0) return "Giá trị giảm phải lớn hơn 0.";
        if (maGiamGia.LoaiGiam == "PERCENT" && maGiamGia.GiaTriGiam > 100) return "Giảm theo phần trăm không được vượt quá 100.";
        if (!DungKieuDecimal18_2(maGiamGia.GiaTriGiam)) return "Giá trị giảm phải có tối đa 16 chữ số nguyên và 2 chữ số thập phân.";
        if (maGiamGia.DonToiThieu < 0 || !DungKieuDecimal18_2(maGiamGia.DonToiThieu)) return "Đơn tối thiểu không hợp lệ.";
        if (maGiamGia.GiamToiDa < 0 ||
            (maGiamGia.GiamToiDa.HasValue && !DungKieuDecimal18_2(maGiamGia.GiamToiDa.Value)))
            return "Mức giảm tối đa không hợp lệ.";
        if (maGiamGia.NgayBatDau == default || maGiamGia.NgayKetThuc <= maGiamGia.NgayBatDau) return "Ngày kết thúc phải sau ngày bắt đầu.";
        if (maGiamGia.GioiHanLuotDung < 0) return "Giới hạn lượt dùng không được âm.";
        if (maGiamGia.GioiHanLuotDung.HasValue && maGiamGia.GioiHanLuotDung.Value < soLuotDaDung)
            return "Giới hạn lượt dùng không được thấp hơn số lượt đã sử dụng.";
        if (soLuotDaDung < 0) return "Số lượt đã dùng không hợp lệ.";
        if (maGiamGia.TrangThai is not ("ACTIVE" or "INACTIVE")) return "Trạng thái chỉ nhận ACTIVE hoặc INACTIVE.";
        return null;
    }

    private static bool DungKieuDecimal18_2(decimal giaTri) =>
        decimal.Abs(giaTri) <= 9999999999999999.99m && decimal.Round(giaTri, 2) == giaTri;

    private static void ChuanHoaMaGiamGia(MaGiamGiaQuanLyDTO maGiamGia)
    {
        maGiamGia.Ma = maGiamGia.Ma.Trim().ToUpperInvariant();
        maGiamGia.MoTa = maGiamGia.MoTa.Trim();
        maGiamGia.LoaiGiam = maGiamGia.LoaiGiam.Trim().ToUpperInvariant();
        maGiamGia.TrangThai = maGiamGia.TrangThai.Trim().ToUpperInvariant();
    }

    private static bool ChuaChuoi(string giaTri, string chuoiTim) =>
        giaTri.Contains(chuoiTim, StringComparison.OrdinalIgnoreCase);
}
