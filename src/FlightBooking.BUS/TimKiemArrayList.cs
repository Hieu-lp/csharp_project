using FlightBooking.DTO;
using System.Collections;
using System.Globalization;

namespace FlightBooking.BUS;

// Bộ lọc chung chạy trong BUS; kết quả ArrayList đáp ứng yêu cầu môn học.
// Các điều kiện được nối lần lượt từ trái sang phải; NOT nghĩa là AND NOT điều kiện hiện tại.
public static class TimKiemArrayList
{
    public static ArrayList LocDanhSach(IEnumerable danhSach, ArrayList cacDieuKien)
    {
        ArgumentNullException.ThrowIfNull(danhSach);
        ArgumentNullException.ThrowIfNull(cacDieuKien);

        var dieuKienDaDoc = new List<DieuKienTimKiemDTO>();
        foreach (object? phanTu in cacDieuKien)
        {
            if (phanTu is not DieuKienTimKiemDTO dieuKien)
                throw new ArgumentException("Mỗi phần tử điều kiện phải là DieuKienTimKiemDTO.", nameof(cacDieuKien));
            KiemTraDieuKien(dieuKien);
            dieuKienDaDoc.Add(dieuKien);
        }

        var ketQua = new ArrayList();
        foreach (object dong in danhSach)
        {
            bool khopDong = true;
            bool laDieuKienDau = true;

            foreach (DieuKienTimKiemDTO dieuKien in dieuKienDaDoc)
            {
                object? giaTriThuc = LayGiaTriTruong(dong, dieuKien.Truong);
                bool khopDieuKien = SoSanh(giaTriThuc, dieuKien.PhepSoSanh, dieuKien.GiaTri);

                if (laDieuKienDau)
                {
                    khopDong = khopDieuKien;
                    laDieuKienDau = false;
                }
                else
                {
                    string phepNoi = dieuKien.PhepNoi.ToUpperInvariant();
                    if (phepNoi == "AND")
                        khopDong = khopDong && khopDieuKien;
                    else if (phepNoi == "OR")
                        khopDong = khopDong || khopDieuKien;
                    else if (phepNoi == "NOT")
                        khopDong = khopDong && !khopDieuKien;
                    else
                        throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(cacDieuKien));
                }
            }

            // Không có điều kiện nghĩa là giữ lại tất cả các dòng.
            if (khopDong) ketQua.Add(dong!);
        }

        return ketQua;
    }

    // Chỉ đọc thuộc tính DTO của ba module Phú; không dùng reflection và không truy cập SQL.
    private static object? LayGiaTriTruong(object dong, string truong)
    {
        string tenTruong = truong.Trim().Replace("_", string.Empty).ToUpperInvariant();
        if (dong is KhachHangDTO khachHang)
        {
            switch (tenTruong)
            {
                case "CUSTOMERID":
                case "MAKHACHHANG":
                    return khachHang.MaKhachHang;
                case "FULLNAME":
                case "HOTEN":
                    return khachHang.HoTen;
                case "PHONE":
                case "DIENTHOAI":
                    return khachHang.DienThoai;
                case "EMAIL":
                    return khachHang.Email;
                case "DATEOFBIRTH":
                case "NGAYSINH":
                    return khachHang.NgaySinh;
                case "GENDER":
                case "GIOITINH":
                    return khachHang.GioiTinh;
                case "IDPASSPORTNUMBER":
                case "SOGIAYTO":
                    return khachHang.SoGiayTo;
                default:
                    throw new ArgumentException($"Không có trường CUSTOMER tên '{truong}'.", nameof(truong));
            }
        }
        if (dong is NhanVienDTO nhanVien)
        {
            switch (tenTruong)
            {
                case "EMPLOYEEID":
                case "MANHANVIEN":
                    return nhanVien.MaNhanVien;
                case "FULLNAME":
                case "HOTEN":
                    return nhanVien.HoTen;
                case "PHONE":
                case "DIENTHOAI":
                    return nhanVien.DienThoai;
                case "EMAIL":
                    return nhanVien.Email;
                case "POSITION":
                case "CHUCVU":
                    return nhanVien.ChucVu;
                case "HIREDATE":
                case "NGAYVAOLAM":
                    return nhanVien.NgayVaoLam;
                case "STATUS":
                case "TRANGTHAI":
                    return nhanVien.TrangThai;
                default:
                    throw new ArgumentException($"Không có trường EMPLOYEE tên '{truong}'.", nameof(truong));
            }
        }
        if (dong is MaGiamGiaQuanLyDTO maGiamGia)
        {
            switch (tenTruong)
            {
                case "DISCOUNTID":
                case "MAGIAMGIA":
                    return maGiamGia.MaGiamGia;
                case "CODE":
                case "MA":
                    return maGiamGia.Ma;
                case "DESCRIPTION":
                case "MOTA":
                    return maGiamGia.MoTa;
                case "DISCOUNTTYPE":
                case "LOAIGIAM":
                    return maGiamGia.LoaiGiam;
                case "DISCOUNTVALUE":
                case "GIATRIGIAM":
                    return maGiamGia.GiaTriGiam;
                case "MINORDERAMOUNT":
                case "DONTOITHIEU":
                    return maGiamGia.DonToiThieu;
                case "MAXDISCOUNTAMOUNT":
                case "GIAMTOIDA":
                    return maGiamGia.GiamToiDa;
                case "STARTDATE":
                case "NGAYBATDAU":
                    return maGiamGia.NgayBatDau;
                case "ENDDATE":
                case "NGAYKETTHUC":
                    return maGiamGia.NgayKetThuc;
                case "USAGELIMIT":
                case "GIOIHANLUOTDUNG":
                    return maGiamGia.GioiHanLuotDung;
                case "USEDCOUNT":
                case "SOLUOTDADUNG":
                    return maGiamGia.SoLuotDaDung;
                case "STATUS":
                case "TRANGTHAI":
                    return maGiamGia.TrangThai;
                default:
                    throw new ArgumentException($"Không có trường DISCOUNT_CODE tên '{truong}'.", nameof(truong));
            }
        }
        throw new ArgumentException("Bộ lọc chỉ nhận DTO khách hàng, nhân viên hoặc mã giảm giá.", nameof(dong));
    }

    private static void KiemTraDieuKien(DieuKienTimKiemDTO dieuKien)
    {
        string phepSoSanh = dieuKien.PhepSoSanh.Trim().ToUpperInvariant();
        if (phepSoSanh is not ("=" or "<>" or ">" or ">=" or "<" or "<=" or "CONTAINS"))
            throw new ArgumentException("Toán tử so sánh không được hỗ trợ.", nameof(dieuKien));

        string phepNoi = dieuKien.PhepNoi.Trim().ToUpperInvariant();
        if (phepNoi is not ("AND" or "OR" or "NOT"))
            throw new ArgumentException("Toán tử nối chỉ nhận AND, OR hoặc NOT.", nameof(dieuKien));
    }

    private static bool SoSanh(object? giaTriThuc, string chuoiPhepSoSanh, string chuoiCanSoSanh)
    {
        string phepSoSanh = chuoiPhepSoSanh.Trim().ToUpperInvariant();
        if (phepSoSanh == "CONTAINS")
            return giaTriThuc is string chuoi && chuoi.Contains(chuoiCanSoSanh, StringComparison.OrdinalIgnoreCase);

        if (giaTriThuc is null) return false;

        int ketQuaSoSanh;
        switch (giaTriThuc)
        {
            case int so when int.TryParse(chuoiCanSoSanh, NumberStyles.Integer, CultureInfo.InvariantCulture, out int soCanSoSanh):
                ketQuaSoSanh = so.CompareTo(soCanSoSanh);
                break;
            case decimal soTien when decimal.TryParse(chuoiCanSoSanh, NumberStyles.Number, CultureInfo.CurrentCulture, out decimal tienCanSoSanh):
                ketQuaSoSanh = soTien.CompareTo(tienCanSoSanh);
                break;
            case decimal soTien when decimal.TryParse(chuoiCanSoSanh, NumberStyles.Number, CultureInfo.InvariantCulture, out decimal tienQuocTe):
                ketQuaSoSanh = soTien.CompareTo(tienQuocTe);
                break;
            case DateTime ngay when DateTime.TryParse(chuoiCanSoSanh, CultureInfo.CurrentCulture, DateTimeStyles.None, out DateTime ngayCanSoSanh):
                ketQuaSoSanh = ngay.CompareTo(ngayCanSoSanh);
                break;
            default:
                ketQuaSoSanh = string.Compare(Convert.ToString(giaTriThuc, CultureInfo.CurrentCulture),
                    chuoiCanSoSanh, StringComparison.OrdinalIgnoreCase);
                break;
        }

        return phepSoSanh switch
        {
            "=" => ketQuaSoSanh == 0,
            "<>" => ketQuaSoSanh != 0,
            ">" => ketQuaSoSanh > 0,
            ">=" => ketQuaSoSanh >= 0,
            "<" => ketQuaSoSanh < 0,
            "<=" => ketQuaSoSanh <= 0,
            _ => false
        };
    }
}
