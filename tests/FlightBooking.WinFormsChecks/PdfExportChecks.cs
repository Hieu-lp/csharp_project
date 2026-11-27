using System.Text;
using FlightBooking.DTO;
using FlightBooking.WinForms;

internal static class PdfExportChecks
{
    public static void Run(Action<bool, string> kiemTra)
    {
        string thuMuc = Environment.GetEnvironmentVariable("FLIGHTBOOKING_PDF_EVIDENCE") ?? Path.Combine(Path.GetTempPath(), "BookingFlight-Fake-PDF");
        Directory.CreateDirectory(thuMuc);
        foreach (string trangThai in new[] { "UNPAID", "PAID", "CANCELLED" })
        {
            var hoaDon = new HoaDonDTO
            {
                MaHoaDon = 999001,
                MaDatVe = 999002,
                MaChuyenBay = 999003,
                NgayHoaDon = new DateTime(2026, 11, 27, 10, 30, 0),
                TrangThai = trangThai,
                TenKhachHang = "KHÁCH KIỂM THỬ PDF – DTO GIẢ, KHÔNG PHẢI GIAO DỊCH THẬT",
                DienThoaiKhachHang = "0000000000",
                TenNhanVien = "QA – dữ liệu giả",
                SoLuongVe = trangThai == "CANCELLED" ? 60 : 9
            };
            for (int i = 0; i < hoaDon.SoLuongVe; i++)
            {
                decimal gia = 123456.78m + i;
                hoaDon.DanhSachChiTiet.Add(new ChiTietHoaDonDTO { MaChiTietHoaDon = i + 1, MaHoaDon = hoaDon.MaHoaDon, MaVe = 9000 + i, SoGhe = (i + 1) + "A", TenHangVe = "Hạng kiểm thử tiếng Việt", TenHanhKhach = "Hành khách kiểm thử Nguyễn Thị Ánh Hồng số " + (i + 1), DonGia = gia, SoLuong = 1, ThanhTien = gia });
                hoaDon.TongTienGoc += gia;
            }
            hoaDon.TienGiam = 1000.01m;
            hoaDon.TienThue = 123.45m;
            hoaDon.TongTien = hoaDon.TongTienGoc - hoaDon.TienGiam + hoaDon.TienThue;
            string tep = Path.Combine(thuMuc, "DTO-GIA-" + trangThai + ".pdf");
            InHoaDonPDF.XuatPDF(hoaDon, tep);
            byte[] noiDung = File.ReadAllBytes(tep);
            kiemTra(noiDung.Length > 1000 && Encoding.ASCII.GetString(noiDung, 0, 5) == "%PDF-", "actual PDF file generated for fake DTO state " + trangThai);
            Console.WriteLine($"PDF_FAKE: {tep} · {hoaDon.SoLuongVe} dòng · tổng {hoaDon.TongTien:0.00}");
        }
    }
}
