namespace FlightBooking.WinForms;

internal sealed record TuyChonGiaoDien(string Label, string Value);
internal sealed record LuaChonBanGhi(int Id, string Label);

internal static class GiaoDien
{
    public static readonly Color MauChu = Color.FromArgb(15, 23, 42);
    public static readonly Color MauChinh = Color.FromArgb(37, 99, 235);
    public static readonly Color MauNen = Color.FromArgb(241, 245, 249);
    public static readonly Color MauChuPhu = Color.FromArgb(100, 116, 139);
    public static readonly Color MauVien = Color.FromArgb(203, 213, 225);
    public static readonly Color MauLoi = Color.FromArgb(185, 28, 28);

    public static Label TaoNhan(string chuoi, float coChu = 11, bool inDam = false) => new()
    {
        Text = chuoi,
        AutoSize = true,
        ForeColor = MauChu,
        Font = new Font("Segoe UI", coChu, inDam ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(0, 3, 0, 5),
        Anchor = AnchorStyles.Left | AnchorStyles.Right
    };
    public static Button TaoNut(string chuoi, Func<Task> thaoTac, bool chinh = false, bool canhBao = false)
    {
        var nut = new Button
        {
            Text = chuoi,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(92, 36),
            Padding = new Padding(12, 4, 12, 4),
            FlatStyle = FlatStyle.Flat,
            BackColor = chinh ? MauChinh : Color.White,
            ForeColor = chinh ? Color.White : canhBao ? MauLoi : MauChu,
            Margin = new Padding(0, 0, 8, 0),
            UseVisualStyleBackColor = false
        };
        nut.FlatAppearance.BorderColor = chinh ? MauChinh : canhBao ? MauLoi : MauVien;
        nut.FlatAppearance.MouseOverBackColor = chinh ? Color.FromArgb(29, 78, 216) : Color.FromArgb(239, 246, 255);
        nut.Click += async (_, _) => await thaoTac();
        return nut;
    }
    public static TableLayoutPanel TaoBang(int cacCot = 1)
    {
        var bang = new TableLayoutPanel
        {
            ColumnCount = cacCot,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Dock = DockStyle.Top,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        if (cacCot == 1) bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return bang;
    }
    public static FlowLayoutPanel TaoVungNut() => new()
    {
        Dock = DockStyle.Fill,
        AutoSize = true,
        AutoSizeMode = AutoSizeMode.GrowAndShrink,
        WrapContents = false,
        Margin = Padding.Empty,
        Padding = new Padding(0, 4, 0, 4)
    };
    public static void ThemDong(TableLayoutPanel bang, Control dieuKhien)
    {
        var dong = bang.RowCount++;
        bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        bang.Controls.Add(dieuKhien, 0, dong); bang.SetColumnSpan(dieuKhien, bang.ColumnCount);
    }
    public static ComboBox TaoHopChon() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill,
        BindingContext = new BindingContext(),
        IntegralHeight = false,
        MaxDropDownItems = 12,
        Margin = new Padding(0, 0, 0, 8)
    };
    public static void GanDuLieu<T>(ComboBox hopChon, List<T> nguon, string truongHienThi = "Label", string giaTri = "Id")
    { hopChon.DataSource = null; hopChon.DisplayMember = truongHienThi; hopChon.ValueMember = giaTri; hopChon.DataSource = nguon; }
    public static void GanLuaChon(ComboBox hopChon, params TuyChonGiaoDien[] cacLuaChon) => GanDuLieu(hopChon, cacLuaChon.ToList(), "Label", "Value");
    public static ListView TaoDanhSach() => new()
    {
        Dock = DockStyle.Fill,
        View = View.Details,
        FullRowSelect = true,
        MultiSelect = false,
        HideSelection = false,
        GridLines = false,
        BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White,
        ForeColor = MauChu,
        Margin = Padding.Empty
    };
    public static NumericUpDown TaoONhapSo(decimal toiDa, int soLe = 0) => new()
    { Minimum = 0, Maximum = toiDa, DecimalPlaces = soLe, ThousandsSeparator = true, Dock = DockStyle.Fill };
    public static DateTimePicker TaoONhapNgay() => new()
    { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Dock = DockStyle.Fill };
    public static DateTimePicker TaoONhapGio() => new()
    { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm:ss", ShowUpDown = true, Dock = DockStyle.Fill };
    public static string TrangThai(string giaTri) => giaTri switch
    { "SCHEDULED" => "Mở bán", "COMPLETED" => "Đã hoàn thành", "CANCELLED" => "Đã hủy", _ => giaTri };
    public static Rectangle KichThuocCuaSo(Rectangle vungLamViec, Size kichThuocMongMuon) => new(
        vungLamViec.Left + Math.Max(0, (vungLamViec.Width - Math.Min(kichThuocMongMuon.Width, vungLamViec.Width - 24)) / 2),
        vungLamViec.Top + Math.Max(0, (vungLamViec.Height - Math.Min(kichThuocMongMuon.Height, vungLamViec.Height - 24)) / 2),
        Math.Min(kichThuocMongMuon.Width, vungLamViec.Width - 24), Math.Min(kichThuocMongMuon.Height, vungLamViec.Height - 24));
}
