using FlightBooking.DTO;
using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;

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
                    string phepNoi = dieuKien.PhepNoi.Trim().ToUpperInvariant();
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
        if (dong is VeMayBayDTO ve)
        {
            switch (tenTruong)
            {
                case "TICKETID":
                case "MAVE":
                    return ve.MaVe;
                case "FLIGHTID":
                case "MACHUYENBAY":
                    return ve.MaChuyenBay;
                case "FAREID":
                case "MAHANGVE":
                    return ve.MaHangVe;
                case "SEATNUMBER":
                case "SOGHE":
                    return ve.SoGhe;
                case "CLASSNAME":
                case "TENHANGVE":
                    return ve.TenHangVe;
                case "PRICE":
                case "GIAVE":
                    return ve.CoGiaLichSu ? ve.GiaVe : null;
                case "STATUS":
                case "TRANGTHAI":
                    return ve.TrangThai;
                case "PASSENGERNAME":
                case "TENHANHKHACH":
                    return ve.TenHanhKhach;
                case "PASSENGERIDCARD":
                case "SOGIAYTO":
                    return ve.SoGiayTo;
                case "DEPARTUREAIRPORT":
                case "SANBAYDI":
                    return ve.SanBayDi;
                case "ARRIVALAIRPORT":
                case "SANBAYDEN":
                    return ve.SanBayDen;
                case "BOOKINGID":
                case "MADATVE":
                    return ve.MaDatVe;
                case "ISSUEDAT":
                case "NGAYPHATHANH":
                    return ve.NgayPhatHanh;
                case "DEPARTUREDATE":
                case "NGAYKHOIHANH":
                    return ve.NgayKhoiHanh;
                case "INVOICESTATUS":
                case "TRANGTHAIHOADON":
                    return ve.TrangThaiHoaDon;
                default:
                    throw new ArgumentException($"Không có trường TICKET tên '{truong}'.", nameof(truong));
            }
        }
        if (dong is HoaDonDTO hoaDon)
        {
            switch (tenTruong)
            {
                case "INVOICEID":
                case "MAHOADON":
                    return hoaDon.MaHoaDon;
                case "BOOKINGID":
                case "MADATVE":
                    return hoaDon.MaDatVe;
                case "INVOICEDATE":
                case "NGAYHOADON":
                    return hoaDon.NgayHoaDon;
                case "TOTAL":
                case "TONGTIENGOC":
                    return hoaDon.TongTienGoc;
                case "DISCOUNTAMOUNT":
                case "TIENGIAM":
                    return hoaDon.TienGiam;
                case "TAXAMOUNT":
                case "TIENTHUE":
                    return hoaDon.TienThue;
                case "TOTALAMOUNT":
                case "TONGTIEN":
                    return hoaDon.TongTien;
                case "STATUS":
                case "TRANGTHAI":
                    return hoaDon.TrangThai;
                case "CUSTOMERNAME":
                case "TENKHACHHANG":
                    return hoaDon.TenKhachHang;
                case "CUSTOMERPHONE":
                case "DIENTHOAI":
                    return hoaDon.DienThoaiKhachHang;
                case "CUSTOMEREMAIL":
                case "EMAIL":
                    return hoaDon.EmailKhachHang;
                case "FLIGHTID":
                case "MACHUYENBAY":
                    return hoaDon.MaChuyenBay;
                case "TICKETCOUNT":
                case "SOLUONGVE":
                    return hoaDon.SoLuongVe;
                default:
                    throw new ArgumentException($"Không có trường INVOICE tên '{truong}'.", nameof(truong));
            }
        }
        throw new ArgumentException("Bộ lọc chỉ nhận DTO khách hàng, nhân viên, mã giảm giá, vé hoặc hóa đơn.", nameof(dong));
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
        if (giaTriThuc is int || giaTriThuc is decimal)
        {
            string chuoi = chuoiCanSoSanh.Trim();
            if (!Regex.IsMatch(chuoi, @"^[+-]?(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]+)?$") ||
                !decimal.TryParse(chuoi.Replace(".", "").Replace(',', '.'), NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal so))
                throw new ArgumentException("Nhập số theo định dạng Việt Nam, ví dụ 1.000,50. Không so sánh số bằng chuỗi.");
            ketQuaSoSanh = Convert.ToDecimal(giaTriThuc, CultureInfo.InvariantCulture).CompareTo(so);
        }
        else if (giaTriThuc is DateTime ngay)
        {
            if (DateTime.TryParseExact(chuoiCanSoSanh.Trim(), ["dd/MM/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngayLoc))
                ketQuaSoSanh = ngay.Date.CompareTo(ngayLoc.Date);
            else if (DateTime.TryParseExact(chuoiCanSoSanh.Trim(), ["dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd HH:mm:ss", "O"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var thoiDiemLoc))
                ketQuaSoSanh = ngay.CompareTo(thoiDiemLoc);
            else throw new ArgumentException("Ngày tìm kiếm phải có dạng dd/MM/yyyy hoặc yyyy-MM-dd; giờ nếu có dạng HH:mm:ss.");
        }
        else if (giaTriThuc is DateOnly ngayDon)
        {
            if (!DateOnly.TryParseExact(chuoiCanSoSanh.Trim(), ["dd/MM/yyyy", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngayLoc))
                throw new ArgumentException("Ngày tìm kiếm phải có dạng dd/MM/yyyy hoặc yyyy-MM-dd.");
            ketQuaSoSanh = ngayDon.CompareTo(ngayLoc);
        }
        else
            ketQuaSoSanh = string.Compare(Convert.ToString(giaTriThuc, CultureInfo.CurrentCulture), chuoiCanSoSanh, StringComparison.OrdinalIgnoreCase);

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
