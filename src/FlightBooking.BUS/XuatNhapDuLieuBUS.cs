using FlightBooking.DTO;
using System.Collections;
using System.Text;

namespace FlightBooking.BUS;

/// <summary>
/// Tiện ích xuất nhập dữ liệu Excel (CSV UTF-8 BOM chuẩn Microsoft Excel) và báo cáo.
/// </summary>
public static class XuatNhapDuLieuBUS
{
    public static void XuatExcelHoaDon(IEnumerable danhSach, string duongDanFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mã hóa đơn,Mã đặt vé,Khách hàng,Điện thoại,Email,Tạm tính,Giảm giá,Tổng tiền,Trạng thái,Ngày lập");

        foreach (object item in danhSach)
        {
            if (item is not HoaDonDTO hd) continue;
            string tt = hd.TrangThai == "PAID" ? "Đã thanh toán" : hd.TrangThai == "UNPAID" ? "Chờ thanh toán" : "Đã hủy";
            sb.AppendLine($"\"{hd.MaHoaDon}\",\"{hd.MaDatVe}\",\"{ChuanHoaCSV(hd.TenKhachHang)}\",\"{ChuanHoaCSV(hd.DienThoaiKhachHang)}\",\"{ChuanHoaCSV(hd.EmailKhachHang)}\",\"{hd.TongTienGoc}\",\"{hd.TienGiam}\",\"{hd.TongTien}\",\"{tt}\",\"{hd.NgayHoaDon:dd/MM/yyyy HH:mm}\"");
        }

        File.WriteAllText(duongDanFile, sb.ToString(), new UTF8Encoding(true));
    }

    public static void XuatExcelVe(IEnumerable danhSach, string duongDanFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Mã vé,Mã chuyến,Tuyến bay,Số ghế,Hạng vé,Giá vé,Hành khách,Số CCCD,Trạng thái,Ngày phát hành");

        foreach (object item in danhSach)
        {
            if (item is not VeMayBayDTO ve) continue;
            string tuyen = !string.IsNullOrEmpty(ve.SanBayDi) ? $"{ve.SanBayDi} - {ve.SanBayDen}" : $"Chuyến {ve.MaChuyenBay}";
            string tt = ve.TrangThai == "ISSUED" ? "Hiệu lực" : "Đã hủy";
            sb.AppendLine($"\"{ve.MaVe}\",\"{ve.MaChuyenBay}\",\"{ChuanHoaCSV(tuyen)}\",\"{ve.SoGhe}\",\"{ChuanHoaCSV(ve.TenHangVe)}\",\"{ve.GiaVe}\",\"{ChuanHoaCSV(ve.TenHanhKhach)}\",\"{ChuanHoaCSV(ve.SoGiayTo)}\",\"{tt}\",\"{ve.NgayPhatHanh:dd/MM/yyyy HH:mm}\"");
        }

        File.WriteAllText(duongDanFile, sb.ToString(), new UTF8Encoding(true));
    }

    public static void XuatExcelThongKe(ThongKeDoanhThuDTO tk, string duongDanFile)
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== BÁO CÁO THỐNG KÊ DOANH THU HỆ THỐNG ===");
        sb.AppendLine($"Tổng doanh thu,{tk.TongDoanhThu:N0} VNĐ");
        sb.AppendLine($"Doanh thu đã thu (PAID),{tk.DoanhThuDaThu:N0} VNĐ");
        sb.AppendLine($"Doanh thu chưa thu (UNPAID),{tk.DoanhThuChuaThu:N0} VNĐ");
        sb.AppendLine($"Tổng số vé đã bán,{tk.TongSoVeBan}");
        sb.AppendLine($"Tổng số hóa đơn,{tk.TongSoHoaDon} (Đã thanh toán: {tk.SoHoaDonDaThanhToan})");
        sb.AppendLine();
        sb.AppendLine("=== DOANH THU THEO QUÝ ===");
        sb.AppendLine("Năm,Quý,Số hóa đơn,Số vé bán,Doanh thu (VNĐ)");
        foreach (var q in tk.DoanhThuTheoQuy)
        {
            sb.AppendLine($"\"{q.Nam}\",\"Quý {q.Quy}\",\"{q.SoHoaDon}\",\"{q.SoVe}\",\"{q.DoanhThu:N0}\"");
        }
        sb.AppendLine();
        sb.AppendLine("=== DOANH THU THEO TUYẾN BAY ===");
        sb.AppendLine("Mã chuyến,Tuyến bay,Số vé bán,Tổng doanh thu (VNĐ)");
        foreach (var cb in tk.DoanhThuTheoChuyenBay)
        {
            sb.AppendLine($"\"{cb.MaChuyenBay}\",\"{ChuanHoaCSV(cb.TuyenBay)}\",\"{cb.SoVeDaBan}\",\"{cb.DoanhThu:N0}\"");
        }

        File.WriteAllText(duongDanFile, sb.ToString(), new UTF8Encoding(true));
    }

    public static List<string[]> DocCSV(string duongDanFile)
    {
        var ketQua = new List<string[]>();
        if (!File.Exists(duongDanFile)) return ketQua;

        foreach (string line in File.ReadAllLines(duongDanFile, Encoding.UTF8))
        {
            if (string.IsNullOrWhiteSpace(line)) continue;
            var parts = line.Split(',');
            for (int i = 0; i < parts.Length; i++)
                parts[i] = parts[i].Trim('\"', ' ');
            ketQua.Add(parts);
        }
        return ketQua;
    }

    private static string ChuanHoaCSV(string? chuoi) =>
        (chuoi ?? "").Replace("\"", "\"\"");
}
