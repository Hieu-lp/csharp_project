using FlightBooking.BUS;
using FlightBooking.DTO;
using System.Collections;

namespace FlightBooking.ManagementChecks;

internal static class TicketInvoiceChecks
{
    public static void Run()
    {
        KiemTraVeMayBayBUS();
        KiemTraHoaDonBUS();
        KiemTraTimKiemArrayList();
        KiemTraXuatNhapDuLieu();
    }

    private static void KiemTraVeMayBayBUS()
    {
        var bus = new VeMayBayBUS();

        // 1. Kiểm tra mã không hợp lệ
        CheckAssert.ErrorContains(bus.HuyVe(0), "Mã vé không hợp lệ", "Ticket cancellation rejects invalid ticket ID 0");
        CheckAssert.ErrorContains(bus.HuyVe(-5), "Mã vé không hợp lệ", "Ticket cancellation rejects negative ticket ID");

        // 2. Kiểm tra tìm kiếm trên ArrayList
        var dsVe = new ArrayList
        {
            new VeMayBayDTO
            {
                MaVe = 101, MaChuyenBay = 1, MaHangVe = 1, SoGhe = "12A",
                TenHanhKhach = "Nguyen Van Quan", SoGiayTo = "012345678901",
                SanBayDi = "Noi Bai", SanBayDen = "Tan Son Nhat",
                TenHangVe = "Economy", GiaVe = 1500000m, TrangThai = "ISSUED"
            },
            new VeMayBayDTO
            {
                MaVe = 102, MaChuyenBay = 1, MaHangVe = 2, SoGhe = "01B",
                TenHanhKhach = "Tran Thi Mai", SoGiayTo = "098765432109",
                SanBayDi = "Noi Bai", SanBayDen = "Tan Son Nhat",
                TenHangVe = "Business", GiaVe = 3500000m, TrangThai = "CANCELLED"
            }
        };

        var ketQuaTuKhoa = bus.TimKiemVe(dsVe, "Quan");
        CheckAssert.True(ketQuaTuKhoa.Count == 1, "Ticket keyword search matches passenger name");

        var ketQuaGhe = bus.TimKiemVe(dsVe, "12A");
        CheckAssert.True(ketQuaGhe.Count == 1, "Ticket keyword search matches seat number");

        var ketQuaTrangThai = bus.TimKiemVe(dsVe, "", "CANCELLED");
        CheckAssert.True(ketQuaTrangThai.Count == 1 && ((VeMayBayDTO)ketQuaTrangThai[0]!).MaVe == 102,
            "Ticket search filters by CANCELLED status");

        var ketQuaTatCa = bus.TimKiemVe(dsVe, "", "TAT_CA");
        CheckAssert.True(ketQuaTatCa.Count == 2, "Ticket search with TAT_CA returns all tickets");
    }

    private static void KiemTraHoaDonBUS()
    {
        var bus = new HoaDonBUS();

        // 1. Kiểm tra validation mã không hợp lệ
        CheckAssert.ErrorContains(bus.ThanhToanHoaDon(0), "Mã hóa đơn không hợp lệ", "Invoice payment rejects ID 0");
        CheckAssert.ErrorContains(bus.ThanhToanHoaDon(-1), "Mã hóa đơn không hợp lệ", "Invoice payment rejects negative ID");
        CheckAssert.ErrorContains(bus.HuyHoaDon(0), "Mã hóa đơn không hợp lệ", "Invoice cancellation rejects ID 0");

        // 2. Kiểm tra tìm kiếm trên ArrayList
        var dsHD = new ArrayList
        {
            new HoaDonDTO
            {
                MaHoaDon = 201, MaDatVe = 11, TenKhachHang = "Nguyen Van Quan",
                DienThoaiKhachHang = "0912345678", EmailKhachHang = "quan@test.com",
                TongTienGoc = 2000000m, TienGiam = 200000m, TongTien = 1800000m,
                TrangThai = "UNPAID", NgayHoaDon = DateTime.Now
            },
            new HoaDonDTO
            {
                MaHoaDon = 202, MaDatVe = 12, TenKhachHang = "Le Van Hieu",
                DienThoaiKhachHang = "0987654321", EmailKhachHang = "hieu@test.com",
                TongTienGoc = 5000000m, TienGiam = 0m, TongTien = 5000000m,
                TrangThai = "PAID", NgayHoaDon = DateTime.Now
            }
        };

        var timKhach = bus.TimKiemHoaDon(dsHD, "0912345678");
        CheckAssert.True(timKhach.Count == 1 && ((HoaDonDTO)timKhach[0]!).MaHoaDon == 201, "Invoice search matches phone");

        var timUnpaid = bus.TimKiemHoaDon(dsHD, "", "UNPAID");
        CheckAssert.True(timUnpaid.Count == 1 && ((HoaDonDTO)timUnpaid[0]!).TrangThai == "UNPAID", "Invoice search filters by UNPAID status");

        var timPaid = bus.TimKiemHoaDon(dsHD, "", "PAID");
        CheckAssert.True(timPaid.Count == 1 && ((HoaDonDTO)timPaid[0]!).TrangThai == "PAID", "Invoice search filters by PAID status");
    }

    private static void KiemTraTimKiemArrayList()
    {
        // 1. Lọc vé máy bay với DieuKienTimKiemDTO
        var dsVe = new ArrayList
        {
            new VeMayBayDTO { MaVe = 1, SoGhe = "10A", GiaVe = 1000000m, TrangThai = "ISSUED" },
            new VeMayBayDTO { MaVe = 2, SoGhe = "15B", GiaVe = 2500000m, TrangThai = "ISSUED" },
            new VeMayBayDTO { MaVe = 3, SoGhe = "20C", GiaVe = 3000000m, TrangThai = "CANCELLED" }
        };

        var dkVe = new ArrayList
        {
            new DieuKienTimKiemDTO { Truong = "TRANGTHAI", PhepSoSanh = "=", GiaTri = "ISSUED", PhepNoi = "AND" },
            new DieuKienTimKiemDTO { Truong = "GIAVE", PhepSoSanh = ">=", GiaTri = "2000000", PhepNoi = "AND" }
        };

        var ketQuaVe = TimKiemArrayList.LocDanhSach(dsVe, dkVe);
        CheckAssert.True(ketQuaVe.Count == 1 && ((VeMayBayDTO)ketQuaVe[0]!).MaVe == 2,
            "ArrayList search accurately filters VeMayBayDTO by status and price threshold");

        // 2. Lọc hóa đơn với DieuKienTimKiemDTO
        var dsHD = new ArrayList
        {
            new HoaDonDTO { MaHoaDon = 1, TongTien = 1000000m, TrangThai = "UNPAID" },
            new HoaDonDTO { MaHoaDon = 2, TongTien = 3000000m, TrangThai = "PAID" },
            new HoaDonDTO { MaHoaDon = 3, TongTien = 4000000m, TrangThai = "UNPAID" }
        };

        var dkHD = new ArrayList
        {
            new DieuKienTimKiemDTO { Truong = "TONGTIEN", PhepSoSanh = ">", GiaTri = "2000000", PhepNoi = "AND" },
            new DieuKienTimKiemDTO { Truong = "TRANGTHAI", PhepSoSanh = "=", GiaTri = "UNPAID", PhepNoi = "AND" }
        };

        var ketQuaHD = TimKiemArrayList.LocDanhSach(dsHD, dkHD);
        CheckAssert.True(ketQuaHD.Count == 1 && ((HoaDonDTO)ketQuaHD[0]!).MaHoaDon == 3,
            "ArrayList search accurately filters HoaDonDTO by amount and status");
    }

    private static void KiemTraXuatNhapDuLieu()
    {
        string fileTam = Path.Combine(Path.GetTempPath(), $"test_export_{Guid.NewGuid():N}.csv");
        try
        {
            var dsVe = new ArrayList
            {
                new VeMayBayDTO
                {
                    MaVe = 999, MaChuyenBay = 1, SoGhe = "99A",
                    TenHanhKhach = "Hanh Khach Test", TenHangVe = "Pho Thong",
                    GiaVe = 1200000m, TrangThai = "ISSUED", NgayPhatHanh = DateTime.Now
                }
            };

            XuatNhapDuLieuBUS.XuatExcelVe(dsVe, fileTam);
            CheckAssert.True(File.Exists(fileTam), "Export Excel creates CSV file successfully");

            var dongDoc = XuatNhapDuLieuBUS.DocCSV(fileTam);
            CheckAssert.True(dongDoc.Count >= 2, "Read CSV returns header and exported ticket record");
            CheckAssert.True(dongDoc[1][0] == "999", "Read CSV correctly parses exported ticket ID");
        }
        finally
        {
            if (File.Exists(fileTam))
                File.Delete(fileTam);
        }
    }
}
