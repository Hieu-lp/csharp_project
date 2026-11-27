using System.Drawing.Printing;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public static class InHoaDonPDF
{
    public static void InHoaDon(Form formCha, HoaDonDTO hoaDon)
    {
        using var hopThoai = new SaveFileDialog { Filter = "PDF (*.pdf)|*.pdf", FileName = $"HoaDon_{hoaDon.MaHoaDon}.pdf" };
        if (hopThoai.ShowDialog(formCha) != DialogResult.OK)
            return;
        XuatPDF(hoaDon, hopThoai.FileName);
        MessageBox.Show(formCha, "Đã tạo file PDF hóa đơn.", "Xuất PDF", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    public static void XuatPDF(HoaDonDTO hoaDon, string duongDan)
    {
        if (!string.Equals(Path.GetExtension(duongDan), ".pdf", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Chọn file .pdf.");
        string? loi = HoaDonBUS.KiemTraChungTu(hoaDon);
        if (loi is not null)
            throw new ArgumentException(loi);
        if (hoaDon.DanhSachChiTiet.Count == 0)
            throw new ArgumentException("Chưa tải chi tiết hóa đơn; không xuất hóa đơn trống.");
        using var taiLieu = TaoTaiLieu(hoaDon);
        taiLieu.PrinterSettings.PrinterName = "Microsoft Print to PDF";
        if (!taiLieu.PrinterSettings.IsValid)
            throw new InvalidOperationException("Máy chưa có máy in Microsoft Print to PDF.");
        string dich = Path.GetFullPath(duongDan);
        string tepTam = Path.Combine(Path.GetDirectoryName(dich)!, "." + Path.GetFileName(dich) + "." + Guid.NewGuid().ToString("N") + ".pdf");
        taiLieu.PrinterSettings.PrintToFile = true;
        taiLieu.PrinterSettings.PrintFileName = tepTam;
        taiLieu.PrintController = new StandardPrintController();
        try
        {
            taiLieu.Print();
            for (int lan = 0; lan < 100 && (!File.Exists(tepTam) || new FileInfo(tepTam).Length == 0); lan++)
                Thread.Sleep(100);
            if (!File.Exists(tepTam) || new FileInfo(tepTam).Length == 0)
                throw new IOException("Chưa nhận được file PDF; kiểm tra máy in PDF.");
            File.Move(tepTam, dich, true);
        }
        finally
        {
            // Chỉ dọn file tạm do lần xuất này tạo; không xóa file đích cũ khi xuất lỗi.
            if (File.Exists(tepTam))
                try
                {
                    File.Delete(tepTam);
                }
                catch (IOException) { }
        }
    }

    private static PrintDocument TaoTaiLieu(HoaDonDTO hoaDon)
    {
        var taiLieu = new PrintDocument { DocumentName = $"HoaDon_{hoaDon.MaHoaDon}" };
        taiLieu.DefaultPageSettings.Margins = new Margins(45, 45, 45, 45);
        int viTriDong = 0, soTrang = 0;
        taiLieu.BeginPrint += (_, _) => { viTriDong = 0; soTrang = 0; };
        taiLieu.PrintPage += (_, trang) =>
        {
            var doHoa = trang.Graphics ?? throw new InvalidOperationException("Không khởi tạo được trang PDF.");
            using var chu = new Font("Segoe UI", 10F);
            using var dam = new Font("Segoe UI", 10F, FontStyle.Bold);
            using var tieuDe = new Font("Segoe UI", 15F, FontStyle.Bold);
            using var nho = new Font("Segoe UI", 8F);
            float x = trang.MarginBounds.Left, y = trang.MarginBounds.Top, rong = trang.MarginBounds.Width;
            soTrang++;
            doHoa.DrawString("FLIGHTBOOKING · HÓA ĐƠN ĐẶT VÉ", tieuDe, Brushes.Black, x, y);
            y += 32;
            string trangThai = hoaDon.TrangThai switch
            {
                "PAID" => "ĐÃ THANH TOÁN",
                "UNPAID" => "CHƯA THANH TOÁN",
                "CANCELLED" => "ĐÃ HỦY",
                _ => hoaDon.TrangThai
            };
            doHoa.DrawString($"Hóa đơn #{hoaDon.MaHoaDon} · Booking #{hoaDon.MaDatVe} · {trangThai}", dam, Brushes.Black, x, y);
            y += 25;
            VeThongTin(doHoa, chu, $"Khách hàng: {hoaDon.TenKhachHang}", $"Điện thoại: {hoaDon.DienThoaiKhachHang}", x, ref y, rong);
            VeThongTin(doHoa, chu, $"Ngày lập: {hoaDon.NgayHoaDon:dd/MM/yyyy HH:mm}", $"Chuyến: #{hoaDon.MaChuyenBay}", x, ref y, rong);
            VeThongTin(doHoa, chu, $"Nhân viên: {hoaDon.TenNhanVien}", $"Trang {soTrang}", x, ref y, rong);
            y += 10;
            float[] tiLe = { 0.07f, 0.09f, 0.08f, 0.16f, 0.26f, 0.17f, 0.17f };
            string[] dau = { "STT", "Mã vé", "Ghế", "Hạng vé", "Hành khách", "Đơn giá", "Thành tiền" };
            VeDong(doHoa, dam, dau, tiLe, x, y, rong, 30, true);
            y += 30;
            float chieuCaoNoiDung = trang.MarginBounds.Bottom - 160 - y;
            while (viTriDong < hoaDon.DanhSachChiTiet.Count)
            {
                var ct = hoaDon.DanhSachChiTiet[viTriDong];
                string[] cacO = { (viTriDong + 1).ToString(), ct.MaVe.ToString(), ct.SoGhe, ct.TenHangVe, ct.TenHanhKhach, ct.DonGia.ToString("N2"), ct.ThanhTien.ToString("N2") };
                float cao = 28;
                for (int i = 0; i < cacO.Length; i++)
                    cao = Math.Max(cao, doHoa.MeasureString(cacO[i], chu, (int)(rong * tiLe[i] - 10)).Height + 10);
                if (cao > chieuCaoNoiDung)
                    throw new ArgumentException("Chi tiết hoặc thông tin đầu hóa đơn quá dài cho một trang PDF. Hãy kiểm tra dữ liệu xuống dòng; chưa ghi đè file đích.");
                if (y + cao > trang.MarginBounds.Bottom - 160)
                {
                    trang.HasMorePages = true;
                    return;
                }
                VeDong(doHoa, chu, cacO, tiLe, x, y, rong, cao, false);
                y += cao;
                viTriDong++;
            }
            if (y + 150 > trang.MarginBounds.Bottom)
            {
                trang.HasMorePages = true;
                return;
            }
            y += 14;
            doHoa.DrawString($"Tạm tính: {hoaDon.TongTienGoc:N2} VND", chu, Brushes.Black, x, y);
            y += 24;
            doHoa.DrawString($"Giảm giá: {hoaDon.TienGiam:N2} VND · Thuế: {hoaDon.TienThue:N2} VND", chu, Brushes.Black, x, y);
            y += 24;
            doHoa.DrawString($"TỔNG THANH TOÁN: {hoaDon.TongTien:N2} VND", dam, Brushes.Black, x, y);
            y += 30;
            doHoa.DrawString("Giá theo chứng từ lịch sử. Hóa đơn UNPAID/CANCELLED không xác nhận đã thu tiền.", nho, Brushes.Black, new RectangleF(x, y, rong, 40));
            trang.HasMorePages = false;
        };
        return taiLieu;
    }

    private static void VeThongTin(Graphics doHoa, Font chu, string trai, string phai, float x, ref float y, float rong)
    {
        float nua = rong / 2, cao = Math.Max(doHoa.MeasureString(trai, chu, (int)nua - 10).Height, doHoa.MeasureString(phai, chu, (int)nua - 10).Height) + 8;
        doHoa.DrawString(trai, chu, Brushes.Black, new RectangleF(x, y, nua - 10, cao));
        doHoa.DrawString(phai, chu, Brushes.Black, new RectangleF(x + nua, y, nua - 10, cao));
        y += cao;
    }

    private static void VeDong(Graphics doHoa, Font chu, string[] cacO, float[] tiLe, float x, float y, float rong, float cao, bool laTieuDe)
    {
        if (laTieuDe)
            doHoa.FillRectangle(Brushes.AliceBlue, x, y, rong, cao);
        for (int i = 0; i < cacO.Length; i++)
        {
            float cot = rong * tiLe[i];
            doHoa.DrawRectangle(Pens.LightGray, x, y, cot, cao);
            doHoa.DrawString(cacO[i], chu, Brushes.Black, new RectangleF(x + 4, y + 4, cot - 8, cao - 8));
            x += cot;
        }
    }
}
