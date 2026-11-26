using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;

namespace FlightBooking.ManagementChecks;

internal static class FinalIntegrationChecks
{
    private static void TuChoi(Action thaoTac, string ten)
    {
        Exception? loi = null;
        try
        {
            thaoTac();
        }
        catch (Exception e) { loi = e; }
        CheckAssert.True(loi is ArgumentException, ten);
    }

    public static void Run()
    {
        KiemTraChungTu();
        KiemTraThongKe();
        KiemTraBoLoc();
        KiemTraExcel();
        KiemTraUuTienKetNoi();
        using var ketNoi = new SqlConnection();
        var duLieu = new DuLieuDatVe(ketNoi, null);
        TuChoi(() => duLieu.DocDatVe(1, null!), "web read rejects null access hash before SQL instead of using admin access");
    }

    private static void KiemTraUuTienKetNoi()
    {
        string? bienChuan=Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        string? bienCu=Environment.GetEnvironmentVariable("FLIGHTBOOKING_DB_CONNECTION");
        const string chuan="Server=.;Database=FlightBooking_Final;Integrated Security=True;";
        const string cu="Server=.;Database=FlightBooking_Hieu_ManagementTests;Integrated Security=True;";
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB",chuan);
            Environment.SetEnvironmentVariable("FLIGHTBOOKING_DB_CONNECTION",cu);
            CheckAssert.True(MyConnection.DocChuoiKetNoi()==chuan,"shared configuration prefers the standard environment key over the legacy key");
            using(var ketNoi=MyConnection.CreateConnection())
                CheckAssert.True(ketNoi.State==System.Data.ConnectionState.Closed && ketNoi.Database=="FlightBooking_Final","factory preserves the closed API and selected catalog without connecting");
            Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB",null);
            CheckAssert.True(MyConnection.DocChuoiKetNoi()==cu,"legacy environment key remains an explicit override of the local fallback");
            Exception? loi=null;
            try{using var ketNoi=new MyConnection("").Open();}catch(InvalidOperationException e){loi=e;}
            CheckAssert.True(loi is not null,"an explicitly empty constructor never falls back to a configured database");
        }
        finally
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB",bienChuan);
            Environment.SetEnvironmentVariable("FLIGHTBOOKING_DB_CONNECTION",bienCu);
        }
    }

    private static HoaDonDTO HoaDonHopLe() => new()
    {
        MaHoaDon = 1,
        MaDatVe = 2,
        SoLuongVe = 2,
        TongTienGoc = 30.03m,
        TienGiam = 0.03m,
        TienThue = 1m,
        TongTien = 31m,
        TrangThai = "UNPAID",
        DanhSachChiTiet = new() { new() { MaHoaDon = 1, MaVe = 10, DonGia = 10.01m, SoLuong = 1, ThanhTien = 10.01m }, new() { MaHoaDon = 1, MaVe = 11, DonGia = 20.02m, SoLuong = 1, ThanhTien = 20.02m } }
    };

    private static void KiemTraChungTu()
    {
        CheckAssert.True(HoaDonBUS.KiemTraChungTu(HoaDonHopLe()) is null, "invoice accepts exact historical decimals, discount and tax");
        var hd = HoaDonHopLe();
        hd.TongTien = 30.99m;
        CheckAssert.ErrorContains(HoaDonBUS.KiemTraChungTu(hd), "Tổng thanh toán", "invoice rejects even a one-cent total mismatch");
        hd = HoaDonHopLe();
        hd.DanhSachChiTiet[1].MaVe = 10;
        CheckAssert.ErrorContains(HoaDonBUS.KiemTraChungTu(hd), "trùng", "invoice rejects the same ticket twice");
        hd = HoaDonHopLe();
        hd.DanhSachChiTiet[1].ThanhTien = 20.01m;
        CheckAssert.ErrorContains(HoaDonBUS.KiemTraChungTu(hd), "Thành tiền", "invoice rejects changed historical line subtotal");
        hd = HoaDonHopLe();
        hd.SoLuongVe = 3;
        CheckAssert.ErrorContains(HoaDonBUS.KiemTraChungTu(hd), "chi tiết", "invoice rejects missing ticket detail");
        hd = HoaDonHopLe();
        hd.TienGiam = -1;
        CheckAssert.True(HoaDonBUS.KiemTraChungTu(hd) is not null, "invoice rejects a negative discount");
        hd = HoaDonHopLe();
        hd.TrangThai = "UNKNOWN";
        CheckAssert.ErrorContains(HoaDonBUS.KiemTraChungTu(hd), "Trạng thái", "invoice rejects an unknown state");
    }

    private static void KiemTraThongKe()
    {
        TuChoi(() => new ThongKeBUS().ThongKeTongHop(new() { TuNgay = new DateTime(2026, 12, 31), DenNgay = new DateTime(2026, 1, 1) }), "invalid statistics date range is rejected before SQL");
        TuChoi(() => ThongKeBUS.KiemTraBoLoc(new() { DenNgay = DateTime.MaxValue }), "statistics prevents exclusive-end date overflow");
        TuChoi(() => ThongKeBUS.KiemTraBoLoc(new() { Thang = 13 }), "statistics rejects month 13");
        TuChoi(() => ThongKeBUS.KiemTraBoLoc(new() { Quy = 0 }), "statistics rejects quarter zero");
        TuChoi(() => ThongKeBUS.KiemTraBoLoc(new() { Thang = 12, Quy = 1 }), "statistics rejects conflicting month and quarter");
        ThongKeBUS.KiemTraBoLoc(new()
        {
            TuNgay = new DateTime(2026, 12, 31),
            DenNgay = new DateTime(2027, 1, 1),
            Thang = 12,
            Quy = 4
        });
        CheckAssert.True(true, "statistics accepts a cross-year range and Q4 month");
        var cacNam = ThongKeBUS.TongHopTheoNam(new[] { new DoanhThuTheoKyDTO { Nam = 2026, DoanhThu = 10.01m, SoHoaDon = 1, SoVe = 2 }, new DoanhThuTheoKyDTO { Nam = 2026, DoanhThu = 20.02m, SoHoaDon = 2, SoVe = 3 }, new DoanhThuTheoKyDTO { Nam = 2027, DoanhThu = 5.67m, SoHoaDon = 1, SoVe = 1 } });
        CheckAssert.True(cacNam.Count == 2 && cacNam[0].DoanhThu == 30.03m && cacNam[0].SoHoaDon == 3 && cacNam[0].SoVe == 5 && cacNam[1].DoanhThu == 5.67m, "annual roll-up preserves cents, quantities and year boundaries");
        var tong = ThongKeBUS.TinhDongTong(new[] { new MaTranDoanhThuDTO { Quy1 = 10.01m, Quy4 = 20.02m, Tong = 30.03m }, new MaTranDoanhThuDTO { Quy2 = 0.01m, Tong = 0.01m } });
        CheckAssert.True(tong.Quy1 == 10.01m && tong.Quy2 == 0.01m && tong.Quy4 == 20.02m && tong.Tong == 30.04m, "matrix grand total keeps all quarter cents");
    }

    private static void KiemTraBoLoc()
    {
        var ds = new ArrayList { new VeMayBayDTO { MaVe = 1, GiaVe = 1000.50m, NgayPhatHanh = new DateTime(2026, 11, 27, 18, 30, 0), NgayKhoiHanh = new DateOnly(2026, 12, 1), TrangThai = "ISSUED" } };
        ArrayList Loc(string truong, string phep, string giaTri) => TimKiemArrayList.LocDanhSach(ds, new() { new DieuKienTimKiemDTO { Truong = truong, PhepSoSanh = phep, GiaTri = giaTri } });
        CheckAssert.True(Loc("GIAVE", "=", "1.000,50").Count == 1, "ticket amount filter parses Vietnamese grouping and decimal cents");
        CheckAssert.True(Loc("GIAVE", ">", "-1").Count == 1, "numeric filter preserves negative thresholds");
        TuChoi(() => Loc("GIAVE", ">", "1.00.000"), "invalid grouped number is rejected instead of string comparison");
        TuChoi(() => Loc("GIAVE", ">", "abc"), "invalid numeric text is rejected");
        CheckAssert.True(Loc("NGAYPHATHANH", "=", "27/11/2026").Count == 1, "date-only invoice/ticket filter includes the whole day");
        CheckAssert.True(Loc("NGAYKHOIHANH", "=", "2026-12-01").Count == 1, "DateOnly departure filters compare chronologically");
        TuChoi(() => Loc("NGAYPHATHANH", ">", "not a date"), "invalid date does not fall back to alphabetical comparison");
        var dieuKien = new ArrayList { new DieuKienTimKiemDTO { Truong = "MAVE", PhepSoSanh = "=", GiaTri = "999" }, new DieuKienTimKiemDTO { Truong = "TRANGTHAI", PhepSoSanh = "=", GiaTri = "ISSUED", PhepNoi = " OR " } };
        CheckAssert.True(TimKiemArrayList.LocDanhSach(ds, dieuKien).Count == 1, "logical connectors tolerate surrounding whitespace");
        ds.Add(new VeMayBayDTO{MaVe=2,CoGiaLichSu=false});
        CheckAssert.True(Loc("GIAVE","=","0").Count==0,"a ticket missing historical documents is not treated as a zero-price ticket");
    }

    private static void KiemTraExcel()
    {
        string tep = Path.Combine(Path.GetTempPath(), "BookingFlight-" + Guid.NewGuid().ToString("N") + ".xls");
        try
        {
            XuatNhapDuLieuBUS.XuatExcelVe(new ArrayList { new VeMayBayDTO { MaVe = 1, GiaVe = 1234.56m, TenHanhKhach = "Đặng Minh, \"Hà\"\nLan", SoGiayTo = "001234", NgayPhatHanh = new DateTime(2026, 11, 27) } }, tep);
            byte[] dau = File.ReadAllBytes(tep);
            CheckAssert.True(dau.Length > 8 && dau[0] == 0xd0 && dau[1] == 0xcf && dau[2] == 0x11 && dau[3] == 0xe0, "Excel output has real OLE2 XLS signature, not renamed CSV");
            using (var vao = File.OpenRead(tep))
            using (var so = new HSSFWorkbook(vao))
            {
                var dong = so.GetSheetAt(0).GetRow(1);
                CheckAssert.True(dong.GetCell(5).CellType == CellType.Numeric && Math.Abs(dong.GetCell(5).NumericCellValue - 1234.56) < 0.00001, "XLS currency is a typed number with cents");
                CheckAssert.True(dong.GetCell(6).StringCellValue == "Đặng Minh, \"Hà\"\nLan" && dong.GetCell(7).StringCellValue == "001234", "XLS retains Vietnamese, commas, quotes, newlines and leading-zero documents");
                CheckAssert.True(dong.GetCell(9).CellType == CellType.Numeric && DateUtil.IsCellDateFormatted(dong.GetCell(9)), "XLS date is a typed date cell");
            }
            XuatNhapDuLieuBUS.TaoMauKhachHang(tep);
            using (var vao = File.OpenRead(tep))
            using (var so = new HSSFWorkbook(vao))
            {
                var dong = so.GetSheetAt(0).CreateRow(1);
                string[] giaTri = { "Khách kiểm thử", "0090123456", "khach@example.test", "01/02/2000", "Nữ", "001234" };
                for (int i = 0; i < giaTri.Length; i++)
                    dong.CreateCell(i).SetCellValue(giaTri[i]);
                var dongNgay=so.GetSheetAt(0).CreateRow(2);
                string[] khachNgay={"Khách ngày Excel","0090123457","typeddate@example.test","","Nam","001235"};
                for(int i=0;i<khachNgay.Length;i++) dongNgay.CreateCell(i).SetCellValue(khachNgay[i]);
                dongNgay.GetCell(3).SetCellValue(new DateTime(2001,4,3));
                var kieuNgay=so.CreateCellStyle();
                kieuNgay.DataFormat=so.CreateDataFormat().GetFormat("m/d/yyyy");
                dongNgay.GetCell(3).CellStyle=kieuNgay;
                using var ra = File.Create(tep + ".copy");
                so.Write(ra, true);
            }
            File.Copy(tep + ".copy", tep, true);
            var khach = XuatNhapDuLieuBUS.DocKhachHangTuExcel(tep);
            CheckAssert.True(khach.Count == 2 && khach[0].DienThoai == "0090123456" && khach[0].NgaySinh == new DateTime(2000, 2, 1), "XLS import preview preserves phone and exact day/month without database writes");
            CheckAssert.True(khach[1].NgaySinh==new DateTime(2001,4,3),"XLS import reads the typed date independently of the cell's US display format");
            TuChoi(() => XuatNhapDuLieuBUS.DocExcel(tep + ".csv"), "Excel importer refuses a CSV extension");
            TuChoi(() => new KhachHangBUS().NhapKhachHang(Array.Empty<KhachHangDTO>()), "customer batch import refuses an empty batch before SQL");
        }
        finally { if (File.Exists(tep)) File.Delete(tep); if (File.Exists(tep + ".copy")) File.Delete(tep + ".copy"); }
    }
}
