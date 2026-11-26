using FlightBooking.DTO;
using System.Drawing.Printing;

namespace FlightBooking.WinForms;

/// <summary>
/// Tiện ích kết xuất và in hóa đơn sang PDF sử dụng System.Drawing.Printing.PrintDocument.
/// Hỗ trợ xem trước (Print Preview) và lưu/in trực tiếp sang file PDF chuẩn.
/// </summary>
public static class InHoaDonPDF
{
    public static void InHoaDon(Form formCha, HoaDonDTO hoaDon)
    {
        using var printDoc = new PrintDocument();
        printDoc.DocumentName = $"HoaDon_{hoaDon.MaHoaDon}";

        printDoc.PrintPage += (_, e) =>
        {
            Graphics g = e.Graphics!;
            Font fontTieuDeChinh = new("Segoe UI", 16F, FontStyle.Bold);
            Font fontTieuDePhu = new("Segoe UI", 11F, FontStyle.Bold);
            Font fontNoiDung = new("Segoe UI", 10F, FontStyle.Regular);
            Font fontNoiDungDam = new("Segoe UI", 10F, FontStyle.Bold);
            Font fontNho = new("Segoe UI", 9F, FontStyle.Italic);

            Brush brushDen = Brushes.Black;
            Pen penVien = Pens.LightGray;
            Pen penDam = new(Color.Black, 1.5f);

            int x = 50;
            int y = 50;
            int width = e.MarginBounds.Width + 50;

            // Header công ty
            g.DrawString("HỆ THỐNG BÁN VÉ MÁY BAY · FLIGHTBOOKING", fontTieuDePhu, Brushes.DarkSlateBlue, x, y);
            y += 24;
            g.DrawString("Địa chỉ: Tòa nhà FPT, Phố Duy Tân, Cầu Giấy, Hà Nội | Hotline: 1900 6868", fontNho, Brushes.Gray, x, y);
            y += 26;
            g.DrawLine(penDam, x, y, x + width, y);
            y += 18;

            // Tiêu đề hóa đơn
            string tieuDe = "HÓA ĐƠN GIÁ TRỊ GIA TĂNG / VÉ MÁY BAY ĐIỆN TỬ";
            var sizeTieuDe = g.MeasureString(tieuDe, fontTieuDeChinh);
            g.DrawString(tieuDe, fontTieuDeChinh, Brushes.DarkBlue, x + (width - sizeTieuDe.Width) / 2, y);
            y += 30;

            string trangThaiStr = hoaDon.TrangThai == "PAID" ? "(ĐÃ THANH TOÁN)" : "(CHỜ THANH TOÁN)";
            var sizeTT = g.MeasureString(trangThaiStr, fontTieuDePhu);
            g.DrawString(trangThaiStr, fontTieuDePhu, hoaDon.TrangThai == "PAID" ? Brushes.DarkGreen : Brushes.DarkOrange, x + (width - sizeTT.Width) / 2, y);
            y += 32;

            // Thông tin chung
            g.DrawString($"Số hóa đơn: #{hoaDon.MaHoaDon}", fontNoiDungDam, brushDen, x, y);
            g.DrawString($"Mã đặt vé (Booking): #{hoaDon.MaDatVe}", fontNoiDungDam, brushDen, x + 350, y);
            y += 22;

            g.DrawString($"Ngày lập: {hoaDon.NgayHoaDon:dd/MM/yyyy HH:mm:ss}", fontNoiDung, brushDen, x, y);
            g.DrawString($"Chuyến bay: #{hoaDon.MaChuyenBay}", fontNoiDung, brushDen, x + 350, y);
            y += 22;

            g.DrawString($"Khách hàng: {hoaDon.TenKhachHang}", fontNoiDung, brushDen, x, y);
            g.DrawString($"Điện thoại: {hoaDon.DienThoaiKhachHang}", fontNoiDung, brushDen, x + 350, y);
            y += 30;

            // Bảng danh sách vé
            int[] colWidths = [45, 60, 60, 100, 180, 110, 120];
            string[] headers = ["STT", "Mã vé", "Ghế", "Hạng vé", "Hành khách", "Đơn giá", "Thành tiền"];

            int curX = x;
            g.FillRectangle(Brushes.AliceBlue, x, y, width, 26);
            g.DrawRectangle(penVien, x, y, width, 26);

            for (int i = 0; i < headers.Length; i++)
            {
                g.DrawString(headers[i], fontNoiDungDam, brushDen, curX + 4, y + 4);
                curX += colWidths[i];
            }
            y += 26;

            int stt = 1;
            foreach (var ct in hoaDon.DanhSachChiTiet)
            {
                curX = x;
                g.DrawRectangle(penVien, x, y, width, 24);

                string[] values =
                [
                    stt.ToString(),
                    ct.MaVe.ToString(),
                    ct.SoGhe,
                    ct.TenHangVe,
                    ct.TenHanhKhach,
                    ct.DonGia.ToString("N0") + " đ",
                    ct.ThanhTien.ToString("N0") + " đ"
                ];

                for (int i = 0; i < values.Length; i++)
                {
                    g.DrawString(values[i], fontNoiDung, brushDen, curX + 4, y + 4);
                    curX += colWidths[i];
                }

                y += 24;
                stt++;
            }

            y += 15;
            // Tổng kết số tiền
            int summaryX = x + 350;
            g.DrawString($"Tạm tính: {hoaDon.TongTienGoc:N0} VNĐ", fontNoiDung, brushDen, summaryX, y);
            y += 20;
            g.DrawString($"Giảm giá khuyến mãi: -{hoaDon.TienGiam:N0} VNĐ", fontNoiDung, brushDen, summaryX, y);
            y += 20;
            g.DrawString($"Thuế & Phí: {hoaDon.TienThue:N0} VNĐ", fontNoiDung, brushDen, summaryX, y);
            y += 24;
            g.DrawLine(penDam, summaryX, y, x + width, y);
            y += 6;
            g.DrawString($"TỔNG CỘNG: {hoaDon.TongTien:N0} VNĐ", fontTieuDePhu, Brushes.DarkRed, summaryX, y);
            y += 50;

            // Chữ ký
            g.DrawString("NGƯỜI MUA HÀNG", fontNoiDungDam, brushDen, x + 60, y);
            g.DrawString("NGƯỜI LẬP HÓA ĐƠN", fontNoiDungDam, brushDen, x + 450, y);
            y += 18;
            g.DrawString("(Ký, ghi rõ họ tên)", fontNho, Brushes.Gray, x + 60, y);
            g.DrawString("(Ký, ghi rõ họ tên)", fontNho, Brushes.Gray, x + 450, y);

            e.HasMorePages = false;
        };

        using var preview = new PrintPreviewDialog
        {
            Document = printDoc,
            StartPosition = FormStartPosition.CenterParent,
            Width = 900,
            Height = 700
        };
        preview.ShowDialog(formCha);
    }
}
