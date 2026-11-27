using System.Collections;
using System.Globalization;
using FlightBooking.DTO;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace FlightBooking.BUS;

// Excel 97–2003 BIFF8 (.xls) thật; không đổi đuôi CSV thành Excel.
public static class XuatNhapDuLieuBUS
{
    private static void KiemTraDuongDan(string duongDan)
    {
        if (!string.Equals(Path.GetExtension(duongDan), ".xls", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chọn định dạng Excel 97–2003 .xls.");
    }

    private static void GhiBang(HSSFWorkbook so, string ten, string[] tieuDe, IEnumerable<object?[]> cacDong)
    {
        ISheet trang = so.CreateSheet(ten);
        ICellStyle tien = so.CreateCellStyle();
        tien.DataFormat = so.CreateDataFormat().GetFormat("#,##0.00");
        ICellStyle ngay = so.CreateCellStyle();
        ngay.DataFormat = so.CreateDataFormat().GetFormat("dd/mm/yyyy hh:mm");
        ICellStyle dau = so.CreateCellStyle();
        IFont chu = so.CreateFont();
        chu.IsBold = true;
        dau.SetFont(chu);
        var dongDau = trang.CreateRow(0);
        for (int cot = 0; cot < tieuDe.Length; cot++)
        {
            var o = dongDau.CreateCell(cot);
            o.SetCellValue(tieuDe[cot]);
            o.CellStyle = dau;
            trang.SetColumnWidth(cot, 24 * 256);
        }
        int viTri = 1;
        foreach (var duLieu in cacDong)
        {
            if (viTri >= 65536)
                throw new ArgumentException("Một sheet .xls chứa tối đa 65.535 dòng dữ liệu; hãy thu hẹp bộ lọc.");
            IRow dong = trang.CreateRow(viTri++);
            for (int cot = 0; cot < duLieu.Length; cot++)
            {
                ICell o = dong.CreateCell(cot);
                switch (duLieu[cot])
                {
                    case null:
                        o.SetCellValue("");
                        break;
                    case DateTime giaTriNgay:
                        o.SetCellValue(giaTriNgay);
                        o.CellStyle = ngay;
                        break;
                    case decimal giaTriTien:
                        // Excel chỉ giữ 15 chữ số có nghĩa; tiền quá lớn được ghi text chính xác.
                        if (decimal.Abs(giaTriTien) <= 9999999999999.99m)
                        {
                            o.SetCellValue((double)giaTriTien);
                            o.CellStyle = tien;
                        }
                        else
                            o.SetCellValue(giaTriTien.ToString("0.00", CultureInfo.InvariantCulture));
                        break;
                    case int soNguyen:
                        o.SetCellValue(soNguyen);
                        break;
                    default:
                        o.SetCellValue(Convert.ToString(duLieu[cot], CultureInfo.InvariantCulture) ?? "");
                        break;
                }
            }
        }
        trang.CreateFreezePane(0, 1);
    }

    private static void Luu(HSSFWorkbook so, string duongDan)
    {
        KiemTraDuongDan(duongDan);
        using var tep = File.Create(duongDan);
        so.Write(tep, true);
    }

    public static void XuatExcelHoaDon(IEnumerable danhSach, string duongDan)
    {
        KiemTraDuongDan(duongDan);
        using var so = new HSSFWorkbook();
        var cacDong = new List<object?[]>();
        foreach (object phanTu in danhSach)
            if (phanTu is HoaDonDTO hd)
                cacDong.Add([hd.MaHoaDon, hd.MaDatVe, hd.TenKhachHang, hd.DienThoaiKhachHang, hd.EmailKhachHang, hd.TenNhanVien, hd.TongTienGoc, hd.TienGiam, hd.TienThue, hd.TongTien, hd.TrangThai, hd.NgayHoaDon]);
        GhiBang(so, "HoaDon", ["Mã hóa đơn", "Mã đặt vé", "Khách hàng", "Điện thoại", "Email", "Nhân viên / WEB", "Tạm tính", "Giảm giá", "Thuế", "Tổng tiền", "Trạng thái", "Ngày lập"], cacDong);
        Luu(so, duongDan);
    }

    public static void XuatExcelVe(IEnumerable danhSach, string duongDan)
    {
        KiemTraDuongDan(duongDan);
        using var so = new HSSFWorkbook();
        var cacDong = new List<object?[]>();
        foreach (object phanTu in danhSach)
            if (phanTu is VeMayBayDTO ve)
                cacDong.Add([ve.MaVe, ve.MaChuyenBay, ve.SanBayDi + " → " + ve.SanBayDen, ve.SoGhe, ve.TenHangVe, ve.CoGiaLichSu ? ve.GiaVe : "Chưa có chứng từ", ve.TenHanhKhach, ve.SoGiayTo, ve.TrangThai, ve.NgayPhatHanh]);
        GhiBang(so, "Ve", ["Mã vé", "Mã chuyến", "Tuyến bay", "Ghế", "Hạng vé", "Giá lịch sử", "Hành khách", "Giấy tờ", "Trạng thái", "Ngày phát hành"], cacDong);
        Luu(so, duongDan);
    }

    public static void XuatExcelThongKe(ThongKeDoanhThuDTO tk, string duongDan, BoLocThongKeDTO? boLoc = null)
    {
        KiemTraDuongDan(duongDan);
        using var so = new HSSFWorkbook();
        GhiBang(so, "TongHop", ["Chỉ tiêu", "Giá trị"], new List<object?[]>{
            new object?[]{"Tổng thu hóa đơn PAID theo ngày lập",tk.DoanhThuDaThu},new object?[]{"Giá trị UNPAID (chưa ghi nhận thu)",tk.DoanhThuChuaThu},
            new object?[]{"Tổng chi",tk.TongChi.HasValue?tk.TongChi.Value:"Chưa có nguồn chi phí"},new object?[]{"Lợi nhuận",tk.LoiNhuan.HasValue?tk.LoiNhuan.Value:"Chưa xác định"},
            new object?[]{"Tổng hóa đơn",tk.TongSoHoaDon},new object?[]{"Hóa đơn PAID",tk.SoHoaDonDaThanhToan},new object?[]{"Vé hiệu lực trong phạm vi ngày lập hóa đơn",tk.TongSoVeBan},
            new object?[]{"Từ ngày",boLoc?.TuNgay},new object?[]{"Đến ngày (bao gồm)",boLoc?.DenNgay},new object?[]{"Năm",boLoc?.Nam},new object?[]{"Tháng",boLoc?.Thang},new object?[]{"Quý",boLoc?.Quy},
            new object?[]{"Thời gian báo cáo","Ngày lập hóa đơn, không phải ngày nhận tiền"},
            new object?[]{"Phân bổ hạng vé","Theo tỷ trọng thành tiền lịch sử; gốc 0 chia đều, dòng cuối nhận phần lẻ"}});
        var thang = new List<object?[]>();
        foreach (var ky in tk.TheoThoiGian)
            thang.Add([ky.Nam, ky.Thang, ky.Quy, ky.SoHoaDon, ky.SoVe, ky.DoanhThu]);
        thang.Add(["TỔNG", null, null, null, null, tk.DoanhThuDaThu]);
        GhiBang(so, "TheoThang", ["Năm", "Tháng", "Quý", "Hóa đơn PAID", "Vé PAID", "Tổng thu"], thang);
        var quy = new List<object?[]>();
        foreach (var ky in tk.DoanhThuTheoQuy)
            quy.Add([ky.Nam, ky.Quy, ky.SoHoaDon, ky.SoVe, ky.DoanhThu]);
        quy.Add(["TỔNG", null, null, null, tk.DoanhThuDaThu]);
        GhiBang(so, "TheoQuy", ["Năm", "Quý", "Hóa đơn PAID", "Vé PAID", "Tổng thu"], quy);
        var nam = new List<object?[]>();
        foreach (var ky in tk.TheoNam)
            nam.Add([ky.Nam, ky.SoHoaDon, ky.SoVe, ky.DoanhThu]);
        nam.Add(["TỔNG", null, null, tk.DoanhThuDaThu]);
        GhiBang(so, "TheoNam", ["Năm", "Hóa đơn PAID", "Vé PAID", "Tổng thu"], nam);
        GhiMaTran(so, "NhanVien", tk.TheoNhanVien);
        GhiMaTran(so, "KhachHang", tk.TheoKhachHang);
        GhiMaTran(so, "ChuyenBay", tk.TheoChuyenBay);
        GhiMaTran(so, "HangVe", tk.TheoHangVe);
        Luu(so, duongDan);
    }

    private static void GhiMaTran(HSSFWorkbook so, string ten, List<MaTranDoanhThuDTO> nguon)
    {
        var cacDong = new List<object?[]>();
        foreach (var dong in nguon)
            cacDong.Add([dong.Ma, dong.Ten, dong.Nam, dong.Quy1, dong.Quy2, dong.Quy3, dong.Quy4, dong.Tong]);
        var tong = ThongKeBUS.TinhDongTong(nguon);
        cacDong.Add(["", tong.Ten, null, tong.Quy1, tong.Quy2, tong.Quy3, tong.Quy4, tong.Tong]);
        GhiBang(so, ten, ["Mã", "Đối tượng", "Năm", "Q1", "Q2", "Q3", "Q4", "Tổng"], cacDong);
    }

    public static List<string[]> DocExcel(string duongDan)
    {
        KiemTraDuongDan(duongDan);
        using var tep = File.OpenRead(duongDan);
        using var so = new HSSFWorkbook(tep);
        if (so.NumberOfSheets == 0)
            throw new ArgumentException("File Excel chưa có sheet.");
        var trang = so.GetSheetAt(0);
        var ketQua = new List<string[]>();
        var dinhDang = new DataFormatter(CultureInfo.GetCultureInfo("vi-VN"));
        for (int viTri = 0; viTri <= trang.LastRowNum; viTri++)
        {
            var dong = trang.GetRow(viTri);
            if (dong is null)
                continue;
            var cacO = new string[Math.Max(0, (int)dong.LastCellNum)];
            bool coDuLieu = false;
            for (int cot = 0; cot < cacO.Length; cot++)
            {
                ICell? o = dong.GetCell(cot);
                if (o?.CellType is CellType.Formula or CellType.Error)
                    throw new ArgumentException($"Không nhập công thức hoặc ô lỗi Excel tại dòng {viTri + 1}, cột {cot + 1}.");
                if (o?.CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(o) && o.DateCellValue is DateTime ngay)
                    cacO[cot] = ngay.ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture);
                else
                    cacO[cot] = o is null ? "" : dinhDang.FormatCellValue(o).Trim();
                coDuLieu |= cacO[cot].Length > 0;
            }
            if (coDuLieu)
                ketQua.Add(cacO);
        }
        return ketQua;
    }

    public static void TaoMauKhachHang(string duongDan)
    {
        using var so = new HSSFWorkbook();
        GhiBang(so, "KhachHang", ["Họ tên", "Điện thoại", "Email", "Ngày sinh", "Giới tính", "Số giấy tờ"], Array.Empty<object?[]>());
        var chuoi = so.CreateCellStyle();
        chuoi.DataFormat = so.CreateDataFormat().GetFormat("@");
        so.GetSheetAt(0).SetDefaultColumnStyle(1, chuoi);
        so.GetSheetAt(0).SetDefaultColumnStyle(5, chuoi);
        var ngaySinh = so.CreateCellStyle();
        ngaySinh.DataFormat = so.CreateDataFormat().GetFormat("dd/mm/yyyy");
        so.GetSheetAt(0).SetDefaultColumnStyle(3, ngaySinh);
        Luu(so, duongDan);
    }

    public static List<KhachHangDTO> DocKhachHangTuExcel(string duongDan)
    {
        List<string[]> cacDong = DocExcel(duongDan);
        string[] tieuDe = { "Họ tên", "Điện thoại", "Email", "Ngày sinh", "Giới tính", "Số giấy tờ" };
        if (cacDong.Count == 0 || cacDong[0].Length != 6)
            throw new ArgumentException("Cần 6 cột: Họ tên, Điện thoại, Email, Ngày sinh, Giới tính, Số giấy tờ.");
        for (int i = 0; i < 6; i++)
        if (cacDong[0][i] != tieuDe[i])
            throw new ArgumentException("Sai tiêu đề cột " + (i + 1) + ": cần " + tieuDe[i]);
        var ketQua = new List<KhachHangDTO>();
        var dienThoai = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var email = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var giayTo = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        for (int i = 1; i < cacDong.Count; i++)
        {
            var dong = cacDong[i];
            if (dong.Length > 6)
                throw new ArgumentException("Thừa cột tại dòng " + (i + 1));
            Array.Resize(ref dong, 6);
            if (!DateTime.TryParseExact(dong[3], ["dd/MM/yyyy", "dd/MM/yyyy HH:mm", "dd/MM/yyyy HH:mm:ss", "yyyy-MM-dd"], CultureInfo.InvariantCulture, DateTimeStyles.None, out var ngaySinh))
                throw new ArgumentException("Ngày sinh sai tại dòng " + (i + 1));
            var khach = new KhachHangDTO { HoTen = dong[0] ?? "", DienThoai = dong[1] ?? "", Email = dong[2] ?? "", NgaySinh = ngaySinh.Date, GioiTinh = dong[4] ?? "", SoGiayTo = string.IsNullOrWhiteSpace(dong[5]) ? null : dong[5] };
            string? loi = KhachHangBUS.KiemTraKhachHang(khach);
            if (loi is not null)
                throw new ArgumentException($"Dòng {i + 1}: {loi}");
            if (!dienThoai.Add(khach.DienThoai) || !email.Add(khach.Email) || (khach.SoGiayTo is not null && !giayTo.Add(khach.SoGiayTo)))
                throw new ArgumentException("Dữ liệu trùng trong file tại dòng " + (i + 1));
            ketQua.Add(khach);
        }
        if (ketQua.Count == 0)
            throw new ArgumentException("File không có khách hàng để nhập.");
        return ketQua;
    }
}
