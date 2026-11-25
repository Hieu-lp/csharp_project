namespace FlightBooking.WinForms;

public partial class Form1 : Form
{
    private readonly TabControl cacThe;
    private readonly TrangQuanLy[] danhSachTrang;
    public event Action<LoaiDanhMuc>? TrangDaChon;

    public Form1()
    {
        InitializeComponent();
        Text = "FlightBooking · Quản lý chuyến bay";
        Font = new Font("Segoe UI", 11); BackColor = GiaoDien.MauNen;
        StartPosition = FormStartPosition.Manual;
        var vungLamViec = Screen.FromPoint(Cursor.Position).WorkingArea;
        Bounds = GiaoDien.KichThuocCuaSo(vungLamViec, new Size(1340, 840));
        MinimumSize = new Size(Math.Min(1040, vungLamViec.Width - 24), Math.Min(640, vungLamViec.Height - 24));
        var ketNoi = Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        if (string.IsNullOrWhiteSpace(ketNoi))
            ketNoi = Environment.GetEnvironmentVariable("FLIGHTBOOKING_DB_CONNECTION");
        var duongDan = Path.Combine(AppContext.BaseDirectory, "connection.local.txt");
        if (string.IsNullOrWhiteSpace(ketNoi) && File.Exists(duongDan)) ketNoi = File.ReadAllText(duongDan).Trim();
        var nghiepVu = new FlightBooking.BUS.QuanLyBUS(ketNoi ?? "");
        cacThe = new TabControl { Dock = DockStyle.Fill, Padding = new Point(22, 10), Font = Font };
        var sanBay = new TrangQuanLy(nghiepVu, LoaiDanhMuc.SanBay);
        var chuyenBay = new TrangQuanLy(nghiepVu, LoaiDanhMuc.ChuyenBay);
        var hangVe = new TrangQuanLy(nghiepVu, LoaiDanhMuc.HangVe);
        danhSachTrang = [sanBay, chuyenBay, hangVe];
        foreach (var (ten, trang) in new[] { ("Sân bay", sanBay), ("Chuyến bay", chuyenBay), ("Hạng vé", hangVe) })
        {
            var tab = new TabPage(ten) { BackColor = GiaoDien.MauNen };
            tab.Controls.Add(trang); cacThe.TabPages.Add(tab);
        }
        cacThe.Deselecting += (_, loi) =>
        {
            if (loi.TabPageIndex >= 0 && !danhSachTrang[loi.TabPageIndex].CoTheRoiTrang())
                loi.Cancel = true;
        };
        chuyenBay.QuanLyHangVe += async ma => { cacThe.SelectedIndex = 2; if (cacThe.SelectedIndex == 2) await hangVe.HienThiHangVe(ma); };
        cacThe.SelectedIndexChanged += (_, _) => TrangDaChon?.Invoke((LoaiDanhMuc)cacThe.SelectedIndex);
        FormClosing += (_, loi) => loi.Cancel = !CoTheRoiTrang();
        Controls.Add(cacThe);
        Shown += async (_, _) =>
        {
            if (TopLevel) Bounds = GiaoDien.KichThuocCuaSo(Screen.FromControl(this).WorkingArea, new Size(1340, 840));
            await sanBay.DamBaoDaTai();
        };
    }

    public bool ChonTrang(LoaiDanhMuc loai)
    {
        cacThe.SelectedIndex = (int)loai;
        return cacThe.SelectedIndex == (int)loai;
    }

    public bool CoTheRoiTrang()
    {
        foreach (var trang in danhSachTrang)
            if (!trang.CoTheRoiTrang()) return false;
        return true;
    }
}
