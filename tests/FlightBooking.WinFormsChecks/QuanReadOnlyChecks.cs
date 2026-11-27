using System.Data;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FlightBooking.DAO;
using FlightBooking.BUS;
using FlightBooking.DTO;
using FlightBooking.WinForms;
using Microsoft.Data.SqlClient;

// Quan sát ứng dụng thật; chỉ gọi điều hướng/tải SELECT, không gọi nghiệp vụ ghi.
internal static class QuanReadOnlyChecks
{
    [StructLayout(LayoutKind.Sequential)]
    private struct VungCuaSo
    {
        public int Trai, Tren, Phai, Duoi;
    }
    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr cuaSo, out VungCuaSo vung);
    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr cuaSo, IntPtr ve, uint co);

    private static IEnumerable<Control> DocDieuKhien(Control cha)
    {
        foreach (Control con in cha.Controls)
        {
            yield return con;
            foreach (Control chau in DocDieuKhien(con))
                yield return chau;
        }
    }

    public static async Task Run(Form chu, string chuoiKetNoi, Action<bool, string> kiemTra)
    {
        var cauHinh = new SqlConnectionStringBuilder(chuoiKetNoi);
        if (cauHinh.InitialCatalog != "FlightBooking_Hieu_ManagementTests")
            throw new InvalidOperationException("Chỉ quan sát trên database test được duyệt.");
        Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB", chuoiKetNoi);
        using (var ketNoi = new MyConnection(chuoiKetNoi).Open())
        {
            using var lenh = new SqlCommand("""
                SELECT CONVERT(nvarchar(128),SERVERPROPERTY('ServerName')),
                 (SELECT CONVERT(nvarchar(128),value) FROM sys.extended_properties
                   WHERE class=0 AND name=N'BookingFlight.TestOnly')
                """, ketNoi);
            using var doc = lenh.ExecuteReader();
            if (!doc.Read() || doc.GetString(0) != "DESKTOP-1MMSBHT" || doc.IsDBNull(1) || doc.GetString(1) != "HieuThinh-W7-20261124")
                throw new InvalidOperationException("Sai server/marker quan sát.");
        }
        // Đo options bằng SQL Server thật cho cả API mở sẵn và API factory đóng.
        for (int lan = 0; lan < 2; lan++)
        {
            using var trucTiep = new MyConnection(chuoiKetNoi).Open();
            KiemTraTuyChon(trucTiep, kiemTra, "Open", lan);
            using var factory = MyConnection.CreateConnection();
            kiemTra(factory.State == ConnectionState.Closed, "factory preserves closed-connection API");
            factory.Open();
            KiemTraTuyChon(factory, kiemTra, "CreateConnection", lan);
        }
        string thuMuc = Environment.GetEnvironmentVariable("FLIGHTBOOKING_UI_EVIDENCE") ??
            Path.Combine(Path.GetTempPath(), "BookingFlight-Quan-ReadOnly");
        Directory.CreateDirectory(thuMuc);
        using var chinh = new MainForm();
        chinh.Show(chu);
        await Task.Delay(350);
        Console.WriteLine($"DISPLAY: {Screen.FromControl(chinh).Bounds}; window={chinh.Bounds}; actual DPI={chinh.DeviceDpi}. Windows settings unchanged.");
        var vungChinh = chinh.RectangleToScreen(chinh.ClientRectangle);
        foreach (var nut in DocDieuKhien(chinh).OfType<Button>())
            kiemTra(nut.Visible && vungChinh.Contains(nut.RectangleToScreen(nut.ClientRectangle)), "navigation button is visible: " + nut.Text);
        string[] muc = { "Tổng quan", "Vé máy bay", "Hóa đơn", "Thống kê" };
        Type?[] kieu = { null, typeof(QuanLyVeForm), typeof(QuanLyHoaDonForm), typeof(ThongKeDoanhThuForm) };
        for (int i = 0; i < muc.Length; i++)
        {
            Button nut = DocDieuKhien(chinh).OfType<Button>().Single(n => n.Text == muc[i]);
            nut.PerformClick();
            await Task.Delay(400);
            if (i == 3)
            {
                var thongKe = DocDieuKhien(chinh).OfType<ThongKeDoanhThuForm>().Single();
                for (int lan = 0; lan < 300 && !thongKe.CoTheRoiTrang(); lan++)
                    await Task.Delay(100);
                kiemTra(thongKe.CoTheRoiTrang(), "statistics SELECT completes without write operations");
                var duLieu = (ThongKeDoanhThuDTO?)typeof(ThongKeDoanhThuForm).GetField("duLieu", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(thongKe);
                kiemTra(duLieu is not null, "statistics executes successfully on the verified test database");
                kiemTra(duLieu!.TongDoanhThu == duLieu.DoanhThuDaThu && duLieu.TongChi is null && duLieu.LoiNhuan is null, "statistics separates paid income and unknown expenses/profit");
                Console.WriteLine($"READ SNAPSHOT: PAID={duLieu.DoanhThuDaThu:0.00}; UNPAID={duLieu.DoanhThuChuaThu:0.00}; invoices={duLieu.TongSoHoaDon}; active tickets={duLieu.TongSoVeBan}");
            }
            if (i == 2)
                await KiemTraMainSub(DocDieuKhien(chinh).OfType<QuanLyHoaDonForm>().Single(), kiemTra);
            if (kieu[i] is not null)
                kiemTra(DocDieuKhien(chinh).Any(d => d.GetType() == kieu[i]), "actual embedded module: " + muc[i]);
            Chup(chinh, Path.Combine(thuMuc, $"screen-{i}.png"));
            Console.WriteLine("CAPTURE: " + muc[i] + " -> " + Path.Combine(thuMuc, $"screen-{i}.png"));
        }
        string[] danhMuc = { "Sân bay", "Chuyến bay", "Hạng vé", "Khách hàng", "Nhân viên", "Mã giảm giá" };
        for (int i = 0; i < danhMuc.Length; i++)
        {
            DocDieuKhien(chinh).OfType<Button>().Single(n => n.Text == danhMuc[i]).PerformClick();
            foreach (var trang in DocDieuKhien(chinh).OfType<TrangQuanLy>())
                await trang.DamBaoDaTai();
            await Task.Delay(200);
            kiemTra(DocDieuKhien(chinh).OfType<Form>().Any(f => !f.TopLevel), "catalog is embedded in the shared content area: " + danhMuc[i]);
            Chup(chinh, Path.Combine(thuMuc, $"catalog-{i}.png"));
        }
        chinh.Close();
    }

    private static async Task KiemTraMainSub(QuanLyHoaDonForm form, Action<bool, string> kiemTra)
    {
        var kieu = typeof(QuanLyHoaDonForm);
        var co = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        var cha = (ListView)kieu.GetField("danhSachHoaDon", co)!.GetValue(form)!;
        var con = (ListView)kieu.GetField("danhSachChiTiet", co)!.GetValue(form)!;
        kiemTra(cha.Items.Count > 0, "invoice master has existing test records to inspect");
        int soHoaDonCoChiTiet=0;
        foreach(ListViewItem dong in cha.Items)
        {
            var hoaDon = (HoaDonDTO)dong.Tag!;
            var dayDu = new HoaDonBUS().DocHoaDonTheoMa(hoaDon.MaHoaDon)!;
            if(dayDu.DanhSachChiTiet.Count==0) continue;
            cha.SelectedItems.Clear();
            dong.EnsureVisible();
            dong.Focused=true;
            dong.Selected=true;
            cha.Select();
            await Task.Delay(150);
            kiemTra(cha.SelectedItems.Count==1,"invoice selection is applied to the actual native ListView #"+hoaDon.MaHoaDon);
            kiemTra(con.Items.Count == dayDu.DanhSachChiTiet.Count && con.Items.Cast<ListViewItem>().All(d => ((ChiTietHoaDonDTO)d.Tag!).MaHoaDon == hoaDon.MaHoaDon), "actual master selection loads only invoice details #" + hoaDon.MaHoaDon);
            var traTien = (Button)kieu.GetField("nutThanhToan", co)!.GetValue(form)!;
            var huy = (Button)kieu.GetField("nutHuyHoaDon", co)!.GetValue(form)!;
            kiemTra(traTien.Enabled == (hoaDon.TrangThai == "UNPAID") && huy.Enabled == (hoaDon.TrangThai == "UNPAID"), "payment and cancellation controls respect selected invoice status #" + hoaDon.MaHoaDon);
            soHoaDonCoChiTiet++;
            if(soHoaDonCoChiTiet==3) break;
        }
        kiemTra(soHoaDonCoChiTiet>0,"master-detail verification includes nonempty historical invoice details");
    }

    private static void KiemTraTuyChon(SqlConnection ketNoi, Action<bool, string> kiemTra, string api, int lan)
    {
        using var lenh = new SqlCommand("""
            SELECT SESSIONPROPERTY('ANSI_NULLS'),SESSIONPROPERTY('ANSI_PADDING'),SESSIONPROPERTY('ANSI_WARNINGS'),
             SESSIONPROPERTY('ARITHABORT'),SESSIONPROPERTY('CONCAT_NULL_YIELDS_NULL'),SESSIONPROPERTY('QUOTED_IDENTIFIER'),
             SESSIONPROPERTY('NUMERIC_ROUNDABORT')
            """, ketNoi);
        using var doc = lenh.ExecuteReader();
        if (!doc.Read())
            throw new InvalidOperationException("Không đọc được options SQL.");
        for (int i = 0; i < 7; i++)
            kiemTra(Convert.ToInt32(doc[i]) == (i == 6 ? 0 : 1), $"{api} SQL option {i + 1}, open {lan + 1}");
    }

    private static void Chup(Form cuaSo, string duongDan)
    {
        if (!GetWindowRect(cuaSo.Handle, out var vung))
            throw new InvalidOperationException("Không đọc được cửa sổ.");
        using var anh = new Bitmap(vung.Phai - vung.Trai, vung.Duoi - vung.Tren);
        using var doHoa = Graphics.FromImage(anh);
        IntPtr ve = doHoa.GetHdc();
        try
        {
            if (!PrintWindow(cuaSo.Handle, ve, 2))
                throw new InvalidOperationException("Không chụp được ứng dụng.");
        }
        finally { doHoa.ReleaseHdc(ve); }
        anh.Save(duongDan, ImageFormat.Png);
    }
}
