using System.Collections;
using System.Text.Json;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public enum LoaiDanhMuc { SanBay, ChuyenBay, HangVe }

public sealed class TrangQuanLy : UserControl
{
    private readonly QuanLyBUS bus;
    private readonly LoaiDanhMuc kind;
    private readonly ListView list = GiaoDien.TaoDanhSach(), children = GiaoDien.TaoDanhSach();
    private readonly Label notice = GiaoDien.TaoNhan("Chưa tải dữ liệu"), count = GiaoDien.TaoNhan(""), detailTitle = GiaoDien.TaoNhan("Chưa chọn bản ghi", 14, true);
    private readonly Label context = GiaoDien.TaoNhan(""), childTitle = GiaoDien.TaoNhan("Hạng vé · Chọn một chuyến", 11, true);
    private readonly Label empty = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = GiaoDien.MauChuPhu };
    private readonly TextBox quick = new() { Dock = DockStyle.Fill, PlaceholderText = "Tên, thành phố hoặc mã…" };
    private readonly TextBox value1 = new() { Dock = DockStyle.Fill }, value2 = new() { Dock = DockStyle.Fill };
    private readonly ComboBox field1 = GiaoDien.TaoHopChon(), field2 = GiaoDien.TaoHopChon(), op1 = GiaoDien.TaoHopChon(), op2 = GiaoDien.TaoHopChon(), join = GiaoDien.TaoHopChon(), fareScope = GiaoDien.TaoHopChon();
    private readonly CheckBox not = new() { Text = "Đảo kết quả lọc nâng cao (NOT), trong phạm vi chuyến đang xem", AutoSize = true };
    private readonly Dictionary<string, Control> fields = new();
    private readonly List<Button> pickers = [];
    private readonly ErrorProvider errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly ToolTip airportTips = new() { ShowAlways = true, AutoPopDelay = 15000 };
    private readonly PhienChinhSua editor = new();
    private readonly Button save, cancel, edit, delete, manage;
    private readonly TableLayoutPanel advanced = GiaoDien.TaoBang(), formFields = GiaoDien.TaoBang();
    private readonly SplitContainer split = new() { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, SplitterWidth = 8 };
    private ArrayList rows = [], airports = [], flights = [], fares = [];
    private object? original, lastViewed;
    private bool binding, busy, loaded;
    private int scopeFlight, pendingSavedId;
    private Task? loading;
    public event Func<int, Task>? QuanLyHangVe;

    public TrangQuanLy(QuanLyBUS nghiepVu, LoaiDanhMuc kind)
    {
        this.bus = nghiepVu; this.kind = kind;
        Font = new Font("Segoe UI", 11); AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill; Padding = new Padding(16); BackColor = GiaoDien.MauNen;
        errors.ContainerControl = this;
        save = GiaoDien.TaoNut("Lưu", LuuDuLieu, chinh: true);
        cancel = GiaoDien.TaoNut("Hủy thay đổi", () => { HuyChinhSua(); return Task.CompletedTask; });
        edit = GiaoDien.TaoNut("Chỉnh sửa", () => { BatDauSua(); return Task.CompletedTask; });
        delete = GiaoDien.TaoNut("Xóa bản ghi…", XoaDuLieu, canhBao: true);
        manage = GiaoDien.TaoNut("Quản lý hạng vé", async () =>
        {
            if (original is QuanLyChuyenBayDTO m && CoTheRoiTrang() && QuanLyHangVe is { } xuLyHangVe)
                await xuLyHangVe(m.ChuyenBay.MaChuyenBay);
        });
        var khungChinh = GiaoDien.TaoBang(); khungChinh.Dock = DockStyle.Fill; khungChinh.AutoSize = false;
        khungChinh.RowCount = 3; khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        khungChinh.Controls.Add(TaoTieuDe(), 0, 0);
        notice.Padding = new Padding(8); notice.Margin = new Padding(0, 4, 0, 10);
        khungChinh.Controls.Add(notice, 0, 1); khungChinh.Controls.Add(split, 0, 2);
        split.Size = new Size(1200, 600); split.SplitterDistance = 760;
        // Parent tabs briefly have a tiny size during construction; apply layout after mounting.
        split.Panel1.Controls.Add(TaoDanhSach()); split.Panel2.Controls.Add(TaoVungChinhSua());
        Controls.Add(khungChinh);
        split.SizeChanged += (_, _) =>
        {
            // Tab có thể co rất nhỏ khi khởi tạo; khôi phục vùng chi tiết khi đã có đủ chỗ.
            if (split.Width > 760 && split.Panel2.Width < 360)
                split.SplitterDistance = Math.Max(360, split.Width - 430);
        };
        list.SelectedIndexChanged += (_, _) => DongChonThayDoi();
        foreach (var lenhSQL in fields.Values)
        {
            switch (lenhSQL)
            {
                case TextBox t: t.TextChanged += (_, _) => DuLieuThayDoi(); break;
                case ComboBox hopChon: hopChon.SelectedIndexChanged += (_, _) => DuLieuThayDoi(); break;
                case NumericUpDown n: n.ValueChanged += (_, _) => DuLieuThayDoi(); break;
                case DateTimePicker boChonNgay: boChonNgay.ValueChanged += (_, _) => DuLieuThayDoi(); break;
            }
        }
        foreach (var lenhSQL in new[] { quick, value1, value2 }) lenhSQL.KeyDown += XuLyPhimTimKiem;
        foreach (var hopChon in new[] { field1, field2, op1, op2, join, fareScope }) hopChon.KeyDown += XuLyPhimTimKiem;
        fareScope.SelectedIndexChanged += (_, _) => PhamViThayDoi();
        VisibleChanged += async (_, _) => { if (Visible && !loaded) await DamBaoDaTai(); };
        SizeChanged += (_, _) =>
        {
            notice.MaximumSize = new Size(Math.Max(200, Width - 48), 0);
            context.MaximumSize = new Size(Math.Max(200, split.Panel2.ClientSize.Width - 40), 0);
        };
        Load += (_, _) =>
        {
            if (split.Width > 760) split.SplitterDistance = Math.Max(360, split.Width - 430);
        };
        TroVeCheDoXem(null);
    }

    private Control TaoTieuDe()
    {
        var tieuDe = GiaoDien.TaoBang();
        var titleRow = GiaoDien.TaoBang(2); titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.RowCount = 1; titleRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.Controls.Add(GiaoDien.TaoNhan(kind switch { LoaiDanhMuc.SanBay => "Sân bay", LoaiDanhMuc.ChuyenBay => "Chuyến bay", _ => "Hạng vé" }, 20, true), 0, 0);
        var vungNut = GiaoDien.TaoVungNut();
        vungNut.Controls.Add(GiaoDien.TaoNut("Thêm mới", () => { if (CoTheRoiTrang()) ThemBanGhi(); return Task.CompletedTask; }, chinh: true));
        vungNut.Controls.Add(GiaoDien.TaoNut("Tải lại", TaiLaiDuLieu)); titleRow.Controls.Add(vungNut, 1, 0); GiaoDien.ThemDong(tieuDe, titleRow);
        var dieuKienTimKiem = GiaoDien.TaoBang(4);
        dieuKienTimKiem.RowCount = 1; dieuKienTimKiem.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        dieuKienTimKiem.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dieuKienTimKiem.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        dieuKienTimKiem.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); dieuKienTimKiem.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var nhan = GiaoDien.TaoNhan("Tìm nhanh"); nhan.Margin = new Padding(0, 8, 12, 0);
        dieuKienTimKiem.Controls.Add(nhan, 0, 0); quick.AccessibleName = "Từ khóa tìm nhanh"; quick.Margin = new Padding(0, 4, 12, 4); dieuKienTimKiem.Controls.Add(quick, 1, 0);
        dieuKienTimKiem.Controls.Add(GiaoDien.TaoNut("Tìm kiếm", () => { ApDungLoc(); return Task.CompletedTask; }), 2, 0);
        Button? toggle = null;
        toggle = GiaoDien.TaoNut("Lọc nâng cao", () => { advanced.Visible = !advanced.Visible; toggle!.Text = advanced.Visible ? "Thu gọn bộ lọc" : "Lọc nâng cao"; return Task.CompletedTask; });
        dieuKienTimKiem.Controls.Add(toggle, 3, 0); GiaoDien.ThemDong(tieuDe, dieuKienTimKiem);
        if (kind == LoaiDanhMuc.HangVe)
        {
            var scope = GiaoDien.TaoBang(2); scope.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); scope.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            scope.RowCount = 1; scope.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scope.Controls.Add(GiaoDien.TaoNhan("Phạm vi chuyến"), 0, 0); fareScope.AccessibleName = "Phạm vi chuyến"; scope.Controls.Add(fareScope, 1, 0); GiaoDien.ThemDong(tieuDe, scope);
        }
        var names = kind switch { LoaiDanhMuc.SanBay => TimKiemQuanLy.TruongSanBay, LoaiDanhMuc.ChuyenBay => TimKiemQuanLy.TruongChuyenBay, _ => TimKiemQuanLy.TruongHangVe };
        foreach (var hopChon in new[] { field1, field2 }) GiaoDien.GanLuaChon(hopChon, names.Select(n => new TuyChonGiaoDien(n == "Mã" ? "Mã bản ghi" : n == "Tên" ? "Tên " + (kind == LoaiDanhMuc.SanBay ? "sân bay" : "hạng vé") : n, n)).ToArray());
        field1.SelectedValue = kind == LoaiDanhMuc.ChuyenBay ? "Nơi đi" : "Tên";
        foreach (var hopChon in new[] { op1, op2 }) GiaoDien.GanLuaChon(hopChon,
            new("Chứa", "chứa"), new("Bằng (=)", "="), new("Khác (<>)", "<>"), new("Lớn hơn (>)", ">"),
            new("Từ (>=)", ">="), new("Nhỏ hơn (<)", "<"), new("Đến (<=)", "<="));
        op2.SelectedValue = "=";
        GiaoDien.GanLuaChon(join, new("Thỏa tất cả điều kiện (AND)", "AND"), new("Thỏa một trong các điều kiện (OR)", "OR"));
        var predicates = GiaoDien.TaoBang(3);
        predicates.RowCount = 3;
        for (var i = 0; i < 3; i++) predicates.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        foreach (var doRong in new[] { 36f, 28f, 36f }) predicates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, doRong));
        predicates.Controls.Add(GiaoDien.TaoNhan("Trường dữ liệu"), 0, 0); predicates.Controls.Add(GiaoDien.TaoNhan("So sánh"), 1, 0); predicates.Controls.Add(GiaoDien.TaoNhan("Giá trị"), 2, 0);
        foreach (var lenhSQL in new Control[] { field1, op1, value1, field2, op2, value2 }) lenhSQL.Margin = new Padding(0, 0, 8, 8);
        field1.AccessibleName = "Trường điều kiện 1"; field2.AccessibleName = "Trường điều kiện 2";
        op1.AccessibleName = "So sánh điều kiện 1"; op2.AccessibleName = "So sánh điều kiện 2";
        value1.AccessibleName = "Giá trị điều kiện 1"; value2.AccessibleName = "Giá trị điều kiện 2";
        predicates.Controls.Add(field1, 0, 1); predicates.Controls.Add(op1, 1, 1); predicates.Controls.Add(value1, 2, 1);
        predicates.Controls.Add(field2, 0, 2); predicates.Controls.Add(op2, 1, 2); predicates.Controls.Add(value2, 2, 2);
        advanced.Padding = new Padding(12); advanced.BackColor = Color.White;
        GiaoDien.ThemDong(advanced, predicates); join.AccessibleName = "Kết hợp điều kiện"; GiaoDien.ThemDong(advanced, join); GiaoDien.ThemDong(advanced, not);
        GiaoDien.ThemDong(advanced, GiaoDien.TaoNhan("Ngày: dd/MM/yyyy · Giá VND: 1.000.000 · Bỏ trống để không thêm điều kiện", 10));
        var filterActions = GiaoDien.TaoVungNut(); filterActions.Controls.Add(GiaoDien.TaoNut("Áp dụng", () => { ApDungLoc(); return Task.CompletedTask; }));
        filterActions.Controls.Add(GiaoDien.TaoNut("Xóa lọc", () =>
        {
            if (!CoTheRoiTrang()) return Task.CompletedTask;
            quick.Clear(); value1.Clear(); value2.Clear(); not.Checked = false; join.SelectedValue = "AND";
            HienThiDanhSach(); ThongBao("Đã xóa bộ lọc. Phạm vi chuyến được giữ nguyên."); return Task.CompletedTask;
        }));
        GiaoDien.ThemDong(advanced, filterActions); advanced.Visible = false; GiaoDien.ThemDong(tieuDe, advanced);
        return tieuDe;
    }

    private Control TaoDanhSach()
    {
        var main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var listPanel = new Panel { Dock = DockStyle.Fill };
        var top = GiaoDien.TaoBang(2); top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.RowCount = 1; top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        top.Controls.Add(GiaoDien.TaoNhan("Danh sách", 12, true), 0, 0); top.Controls.Add(count, 1, 0);
        listPanel.Controls.Add(list); listPanel.Controls.Add(empty); listPanel.Controls.Add(top);
        switch (kind)
        {
            case LoaiDanhMuc.SanBay: TaoCot(list, ("Mã", 65), ("Tên sân bay", 250), ("Thành phố", 180)); break;
            case LoaiDanhMuc.ChuyenBay: TaoCot(list, ("Mã", 60), ("Chặng bay", 235), ("Khởi hành", 155), ("Hạ cánh", 155), ("Còn / tổng", 95), ("Trạng thái", 130)); break;
            case LoaiDanhMuc.HangVe: TaoCot(list, ("Mã", 60), ("Chuyến", 70), ("Tên hạng", 170), ("Giá VND/người", 150), ("Hành lý kg", 95)); break;
        }
        if (kind != LoaiDanhMuc.ChuyenBay) { main.Controls.Add(listPanel); return main; }
        var linked = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 8 };
        linked.Size = new Size(700, 520); linked.SplitterDistance = 300;
        linked.Panel1.Controls.Add(listPanel);
        var sub = GiaoDien.TaoBang(); sub.Dock = DockStyle.Fill; sub.AutoSize = false;
        sub.RowCount = 3; sub.RowStyles.Add(new RowStyle(SizeType.AutoSize)); sub.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); sub.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sub.Controls.Add(childTitle, 0, 0); sub.Controls.Add(children, 0, 1); sub.Controls.Add(manage, 0, 2);
        TaoCot(children, ("Mã", 60), ("Tên hạng", 160), ("Giá VND/người", 140), ("Hành lý kg", 100));
        linked.Panel2.Controls.Add(sub); main.Controls.Add(linked); return main;
    }
    private static void TaoCot(ListView danhSach, params (string Text, int Width)[] cacCot)
    { foreach (var (chuoi, doRong) in cacCot) danhSach.Columns.Add(chuoi, doRong); }

    private Control TaoVungChinhSua()
    {
        var shell = GiaoDien.TaoBang(); shell.Dock = DockStyle.Fill; shell.AutoSize = false; shell.BackColor = Color.White; shell.Padding = new Padding(16);
        shell.RowCount = 3; shell.RowStyles.Add(new RowStyle(SizeType.AutoSize)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var head = GiaoDien.TaoBang(); GiaoDien.ThemDong(head, detailTitle); context.ForeColor = GiaoDien.MauChuPhu; GiaoDien.ThemDong(head, context);
        var vungNut = GiaoDien.TaoVungNut(); vungNut.Controls.Add(edit); vungNut.Controls.Add(delete); GiaoDien.ThemDong(head, vungNut); shell.Controls.Add(head, 0, 0);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 4, 20, 0) };
        scroll.Controls.Add(formFields); shell.Controls.Add(scroll, 0, 1);
        ThemTruong(formFields, "id", "Mã tự sinh · có thể sao chép", new TextBox { ReadOnly = true });
        switch (kind)
        {
            case LoaiDanhMuc.SanBay:
                ThemTruong(formFields, "name", "Tên sân bay", new TextBox { MaxLength = 150 });
                ThemTruong(formFields, "city", "Thành phố", new TextBox { MaxLength = 100 }); break;
            case LoaiDanhMuc.ChuyenBay:
                TaoNhomTruong("Chặng bay"); ThemTruongSanBay("from", "Sân bay đi"); ThemTruongSanBay("to", "Sân bay đến");
                TaoNhomTruong("Lịch bay"); var dates = GiaoDien.TaoBang(2); dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
                dates.RowCount = 2; dates.RowStyles.Add(new RowStyle(SizeType.AutoSize)); dates.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                dates.Controls.Add(TaoTruong("dd", "Ngày đi", GiaoDien.TaoONhapNgay()), 0, 0); dates.Controls.Add(TaoTruong("dt", "Giờ đi", GiaoDien.TaoONhapGio()), 1, 0);
                dates.Controls.Add(TaoTruong("ad", "Ngày đến", GiaoDien.TaoONhapNgay()), 0, 1); dates.Controls.Add(TaoTruong("at", "Giờ đến", GiaoDien.TaoONhapGio()), 1, 1); GiaoDien.ThemDong(formFields, dates);
                TaoNhomTruong("Số ghế và trạng thái"); ThemTruong(formFields, "total", "Tổng số ghế", GiaoDien.TaoONhapSo(int.MaxValue));
                var trangThai = GiaoDien.TaoHopChon(); GiaoDien.GanLuaChon(trangThai, new("Mở bán", "SCHEDULED"), new("Đã hoàn thành", "COMPLETED"), new("Đã hủy", "CANCELLED"));
                ThemTruong(formFields, "status", "Trạng thái", trangThai);
                TaoNhomTruong("Thông tin hệ thống · chỉ đọc");
                ThemTruong(formFields, "sold", "Vé đã bán", new TextBox { ReadOnly = true });
                ThemTruong(formFields, "available", "Ghế còn", new TextBox { ReadOnly = true });
                ThemTruong(formFields, "revenue", "Doanh thu đã thu (VND)", new TextBox { ReadOnly = true }); break;
            case LoaiDanhMuc.HangVe:
                ThemTruong(formFields, "flight", "Chuyến bay", GiaoDien.TaoHopChon());
                var pick = GiaoDien.TaoNut("Chọn chuyến…", ChonChuyenBay); pickers.Add(pick); GiaoDien.ThemDong(formFields, pick);
                ThemTruong(formFields, "name", "Tên hạng vé", new TextBox { MaxLength = 50 });
                ThemTruong(formFields, "price", "Giá vé (VND/người)", GiaoDien.TaoONhapSo(9999999999999999.99m, 2));
                ThemTruong(formFields, "kg", "Hành lý (kg/người)", GiaoDien.TaoONhapSo(int.MaxValue)); break;
        }
        var chanTrang = GiaoDien.TaoVungNut(); chanTrang.Padding = new Padding(0, 12, 0, 0); chanTrang.Controls.Add(save); chanTrang.Controls.Add(cancel); shell.Controls.Add(chanTrang, 0, 2);
        return shell;
    }
    private void TaoNhomTruong(string tieuDe) { var nhan = GiaoDien.TaoNhan(tieuDe, 11, true); nhan.Margin = new Padding(0, 16, 0, 6); GiaoDien.ThemDong(formFields, nhan); }
    private Control TaoTruong(string khoa, string nhan, Control dieuKhien)
    {
        var panel = GiaoDien.TaoBang(); panel.Margin = new Padding(0, 0, 8, 10);
        dieuKhien.Dock = DockStyle.Fill; dieuKhien.AccessibleName = nhan;
        fields[khoa] = dieuKhien; GiaoDien.ThemDong(panel, GiaoDien.TaoNhan(nhan)); GiaoDien.ThemDong(panel, dieuKhien); return panel;
    }
    private void ThemTruong(TableLayoutPanel cha, string khoa, string nhan, Control dieuKhien) => GiaoDien.ThemDong(cha, TaoTruong(khoa, nhan, dieuKhien));
    private void ThemTruongSanBay(string khoa, string nhan)
    {
        var holder = GiaoDien.TaoBang(2); holder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); holder.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        holder.RowCount = 1; holder.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var hopChon = GiaoDien.TaoHopChon(); fields[khoa] = hopChon; hopChon.AccessibleName = nhan; holder.Controls.Add(hopChon, 0, 0);
        hopChon.DropDown += (_, _) => ChinhDoRongSanBay(hopChon);
        var pick = GiaoDien.TaoNut("Chọn…", () => ChonSanBay(khoa)); pick.MinimumSize = new Size(60, 32); pickers.Add(pick); holder.Controls.Add(pick, 1, 0);
        var tieuDe = GiaoDien.TaoNhan(nhan);
        hopChon.SelectedIndexChanged += (_, _) =>
        {
            string chuoi = hopChon.SelectedItem is null ? "" : (hopChon.GetItemText(hopChon.SelectedItem) ?? "");
            airportTips.SetToolTip(hopChon, chuoi);
            airportTips.SetToolTip(tieuDe, chuoi); // Nhãn vẫn bật khi ComboBox ở chế độ xem bị khóa.
            tieuDe.AccessibleDescription = chuoi;
        };
        var wrapper = GiaoDien.TaoBang(); GiaoDien.ThemDong(wrapper, tieuDe); GiaoDien.ThemDong(wrapper, holder); GiaoDien.ThemDong(formFields, wrapper);
    }
    private static void ChinhDoRongSanBay(ComboBox hopChon)
    {
        int doRong = hopChon.Width;
        int padding = SystemInformation.VerticalScrollBarWidth + 16 * hopChon.DeviceDpi / 96;
        foreach (object phanTu in hopChon.Items)
        {
            int textWidth = TextRenderer.MeasureText(hopChon.GetItemText(phanTu), hopChon.Font).Width;
            doRong = Math.Max(doRong, textWidth + padding);
        }
        int screenWidth = Math.Max(hopChon.Width, Screen.FromControl(hopChon).WorkingArea.Width - 24);
        hopChon.DropDownWidth = Math.Min(doRong, screenWidth);
    }
    private void XuLyPhimTimKiem(object? sender, KeyEventArgs loi)
    { if (loi.KeyCode == Keys.Enter) { loi.SuppressKeyPress = true; ApDungLoc(); } }
    private void ThongBao(string chuoi, bool loi = false)
    { notice.Text = (loi ? "Lỗi: " : "") + chuoi; notice.ForeColor = loi ? GiaoDien.MauLoi : GiaoDien.MauChu; notice.BackColor = loi ? Color.FromArgb(254, 242, 242) : Color.FromArgb(239, 246, 255); }
    private static int LayMa(object? dong) => dong switch { SanBayDTO sanBay => sanBay.MaSanBay, QuanLyChuyenBayDTO chuyenBay => chuyenBay.ChuyenBay.MaChuyenBay, HangVeDTO chuyenBay => chuyenBay.MaHangVe, _ => 0 };
    private object? TimTheoMa(int ma) => rows.Cast<object>().FirstOrDefault(dong => LayMa(dong) == ma);
    private string ChupDuLieu() => JsonSerializer.Serialize(fields.Where(phanTu => !LaTruongHeThong(phanTu.Key)).ToDictionary(phanTu => phanTu.Key, phanTu => phanTu.Value switch
    { ComboBox hopChon => Convert.ToString(hopChon.SelectedValue) ?? "", DateTimePicker boChonNgay => boChonNgay.Value.ToString("O"), NumericUpDown n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), _ => phanTu.Value.Text }));
    private static bool LaTruongHeThong(string khoa) => khoa is "id" or "sold" or "available" or "revenue";
    private void DuLieuThayDoi() { if (!binding) { CapNhatTrangThaiNut(); if (kind == LoaiDanhMuc.HangVe) CapNhatThongTinChuyen(); } }
    private void CapNhatTrangThaiNut()
    {
        var danhSach = editor.CheDo == CheDoChinhSua.Xem;
        foreach (var (khoa, dieuKhien) in fields)
        {
            if (dieuKhien is TextBox chuoi) { chuoi.ReadOnly = danhSach || LaTruongHeThong(khoa); chuoi.BackColor = chuoi.ReadOnly ? GiaoDien.MauNen : Color.White; }
            else dieuKhien.Enabled = !danhSach && !LaTruongHeThong(khoa);
        }
        foreach (var hopThoai in pickers) hopThoai.Enabled = !danhSach;
        save.Enabled = !busy && editor.CoTheLuu(ChupDuLieu()); cancel.Enabled = !busy && !danhSach;
        edit.Enabled = !busy && danhSach && original is not null; delete.Enabled = !busy && danhSach && original is not null;
        manage.Enabled = !busy && original is QuanLyChuyenBayDTO;
        detailTitle.Text = editor.CheDo switch
        { CheDoChinhSua.Them => "Thêm mới", CheDoChinhSua.Sua => $"Chỉnh sửa #{LayMa(original)}", _ => original is null ? "Chọn bản ghi để xem" : $"Chi tiết #{LayMa(original)}" };
        if (editor.CoThayDoi(ChupDuLieu())) detailTitle.Text += " · chưa lưu";
    }
    public bool CoTheRoiTrang()
    {
        if (busy) { ThongBao("Đang xử lý, vui lòng đợi hoàn tất."); return false; }
        if (editor.CoThayDoi(ChupDuLieu()) && MessageBox.Show(this, "Bỏ các thay đổi chưa lưu? Nội dung đang nhập sẽ không được lưu.", "Thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return false;
        if (editor.CheDo != CheDoChinhSua.Xem) HuyChinhSua();
        return true;
    }
    private async Task ThucHien(Func<Task> thaoTac, string tienTrinh)
    {
        if (busy) return; busy = true; Enabled = false; UseWaitCursor = true; ThongBao(tienTrinh);
        try { await thaoTac(); }
        catch (Exception loi) when (loi is LoiQuanLy or ArgumentException or FormatException)
        { if (!IsDisposed) { ThongBao(loi.Message, true); DanhDauLoi(loi.Message); } }
        finally { if (!IsDisposed) { busy = false; Enabled = true; UseWaitCursor = false; CapNhatTrangThaiNut(); } }
    }
    private void DanhDauLoi(string thongBao)
    {
        errors.Clear();
        string[] keys = thongBao switch
        {
            var s when s.Contains("Tên sân bay") => ["name"],
            var s when s.Contains("Thành phố") => ["city"],
            var s when kind == LoaiDanhMuc.SanBay && s.Contains("cùng tên và thành phố", StringComparison.OrdinalIgnoreCase) => ["name", "city"],
            var s when s.Contains("tên hạng", StringComparison.OrdinalIgnoreCase) || s.Contains("hạng vé đã tồn tại") => ["name"],
            var s when s.Contains("Giá vé") => ["price"],
            var s when s.Contains("Hành lý") => ["kg"],
            var s when s.Contains("ghế", StringComparison.OrdinalIgnoreCase) => ["total"],
            var s when s.Contains("Ngày giờ") => ["ad", "at"],
            var s when s.Contains("sân bay", StringComparison.OrdinalIgnoreCase) => ["from", "to"],
            var s when kind == LoaiDanhMuc.HangVe && s.Contains("chuyến bay", StringComparison.OrdinalIgnoreCase) => ["flight"],
            _ => []
        };
        foreach (var khoa in keys) if (fields.TryGetValue(khoa, out var dieuKhien)) errors.SetError(dieuKhien, thongBao);
    }
    public Task DamBaoDaTai() => loaded ? Task.CompletedTask : TaiDuLieu();
    public Task TaiLaiDuLieu() => !CoTheRoiTrang() ? Task.CompletedTask : TaiDuLieu();
    private Task TaiDuLieu() => loading is { IsCompleted: false } ? loading : loading = ThucHien(async () =>
    {
        var keep = pendingSavedId > 0 ? pendingSavedId : LayMa(original); await DocToanBo(); if (IsDisposed) return;
        HienThiDanhSach(); TroVeCheDoXem(TimTheoMa(keep)); ThongBao($"Đã tải {rows.Count} bản ghi.");
    }, "Đang tải dữ liệu…");
    private async Task DocToanBo()
    {
        var duLieu = await Task.Run(() => (bus.DocDSSanBay(), bus.DocDSChuyenBay(), bus.DocDSHangVe()));
        if (IsDisposed) return;
        binding = true;
        try
        {
            (airports, flights, fares) = duLieu;
            rows = kind switch { LoaiDanhMuc.SanBay => airports, LoaiDanhMuc.ChuyenBay => flights, _ => fares };
            if (kind == LoaiDanhMuc.ChuyenBay)
            {
                var cacLuaChon = airports.Cast<SanBayDTO>().Select(sanBay => new LuaChonBanGhi(sanBay.MaSanBay, $"{sanBay.TenSanBay} · {sanBay.ThanhPho}")).ToList();
                GiaoDien.GanDuLieu((ComboBox)fields["from"], cacLuaChon); GiaoDien.GanDuLieu((ComboBox)fields["to"], cacLuaChon.ToList());
            }
            if (kind == LoaiDanhMuc.HangVe)
            {
                var cacLuaChon = DocLuaChonChuyenBay(); GiaoDien.GanDuLieu((ComboBox)fields["flight"], cacLuaChon.ToList());
                cacLuaChon.Insert(0, new(0, "Tất cả chuyến")); GiaoDien.GanDuLieu(fareScope, cacLuaChon);
                fareScope.SelectedValue = scopeFlight; if (fareScope.SelectedIndex < 0) { fareScope.SelectedValue = 0; scopeFlight = 0; }
            }
            loaded = true;
        }
        finally { binding = false; }
    }
    private List<LuaChonBanGhi> DocLuaChonChuyenBay() => flights.Cast<QuanLyChuyenBayDTO>().Select(m => new LuaChonBanGhi(m.ChuyenBay.MaChuyenBay,
        $"#{m.ChuyenBay.MaChuyenBay} · {m.ChuyenBay.ThanhPhoDi} → {m.ChuyenBay.ThanhPhoDen} · {m.ChuyenBay.ThoiDiemKhoiHanh:dd/MM/yyyy HH:mm}")).ToList();
    private void PhamViThayDoi()
    {
        if (binding || !loaded) return;
        var requested = fareScope.SelectedValue is int ma ? ma : 0;
        if (!CoTheRoiTrang()) { binding = true; fareScope.SelectedValue = scopeFlight; binding = false; return; }
        scopeFlight = requested; TroVeCheDoXem(null); HienThiDanhSach();
    }
    private static string LayLuaChon(ComboBox hopChon) => Convert.ToString(hopChon.SelectedValue) ?? "";
    private void ApDungLoc() { if (CoTheRoiTrang() && HienThiDanhSach()) TroVeCheDoXem(null); }
    private bool HienThiDanhSach()
    {
        try
        {
            var nguon = rows;
            if (kind == LoaiDanhMuc.HangVe && scopeFlight > 0) nguon = TimKiemQuanLy.LocDanhSach(nguon, [new("Chuyến", "=", scopeFlight.ToString())]);
            var filtered = TimKiemQuanLy.LocDanhSach(nguon, [new(LayLuaChon(field1), LayLuaChon(op1), value1.Text), new(LayLuaChon(field2), LayLuaChon(op2), value2.Text)], LayLuaChon(join), not.Checked);
            var chuoi = quick.Text.Trim();
            if (chuoi.Length > 0)
            {
                var terms = kind switch
                {
                    LoaiDanhMuc.SanBay => new List<DieuKienLoc> { new("Tên", "chứa", chuoi), new("Thành phố", "chứa", chuoi) },
                    LoaiDanhMuc.ChuyenBay => new List<DieuKienLoc> { new("Nơi đi", "chứa", chuoi), new("Nơi đến", "chứa", chuoi) },
                    _ => new List<DieuKienLoc> { new("Tên", "chứa", chuoi) }
                };
                if (int.TryParse(chuoi, out _)) terms.Add(new("Mã", "=", chuoi));
                filtered = TimKiemQuanLy.LocDanhSach(filtered, terms, "OR");
            }
            binding = true; list.BeginUpdate();
            try
            {
                list.Items.Clear(); foreach (var dong in filtered)
                {
                    var phanTu = new ListViewItem(DuLieuCot(dong!)) { Tag = dong };
                    if (list.Items.Count % 2 == 1) phanTu.BackColor = Color.FromArgb(248, 251, 254);
                    list.Items.Add(phanTu);
                }
            }
            finally { list.EndUpdate(); binding = false; }
            count.Text = $"{filtered.Count} / {nguon.Count} bản ghi";
            empty.Text = !loaded ? "Đang tải dữ liệu…" : nguon.Count == 0 ? "Chưa có dữ liệu trong phạm vi này. Chọn Thêm mới để bắt đầu." : "Không tìm thấy kết quả. Thử Xóa lọc hoặc đổi từ khóa.";
            empty.Visible = filtered.Count == 0; list.Visible = filtered.Count > 0;
            ThongBao(filtered.Count == 0 ? empty.Text : $"Tìm thấy {filtered.Count} bản ghi."); return true;
        }
        catch (LoiQuanLy loi) { ThongBao(loi.Message, true); return false; }
    }
    private static string[] DuLieuCot(object dong) => dong switch
    {
        SanBayDTO sanBay => [sanBay.MaSanBay.ToString(), sanBay.TenSanBay, sanBay.ThanhPho],
        QuanLyChuyenBayDTO m => [m.ChuyenBay.MaChuyenBay.ToString(), $"{m.ChuyenBay.ThanhPhoDi} → {m.ChuyenBay.ThanhPhoDen}", m.ChuyenBay.ThoiDiemKhoiHanh.ToString("dd/MM/yyyy HH:mm"), m.ChuyenBay.ThoiDiemDen.ToString("dd/MM/yyyy HH:mm"), $"{m.ChuyenBay.SoGheCon} / {m.ChuyenBay.TongSoGhe}", GiaoDien.TrangThai(m.ChuyenBay.TrangThai)],
        HangVeDTO chuyenBay => [chuyenBay.MaHangVe.ToString(), chuyenBay.MaChuyenBay.ToString(), chuyenBay.TenHangVe, chuyenBay.GiaVe.ToString("N2"), chuyenBay.HanhLyCoBanKg.ToString()],
        _ => []
    };
    private void DongChonThayDoi()
    {
        if (binding || list.SelectedItems.Count == 0) return;
        var requested = list.SelectedItems[0].Tag;
        if (LayMa(requested) == LayMa(original) && editor.CheDo == CheDoChinhSua.Xem) return;
        if (!CoTheRoiTrang()) { ChonDong(LayMa(original)); return; }
        TroVeCheDoXem(requested);
    }
    private void ChonDong(int ma)
    {
        binding = true;
        try { foreach (ListViewItem phanTu in list.Items) phanTu.Selected = LayMa(phanTu.Tag) == ma && ma > 0; }
        finally { binding = false; }
    }
    private void TroVeCheDoXem(object? dong)
    {
        binding = true; original = dong; lastViewed = dong; pendingSavedId = 0; errors.Clear();
        try
        {
            XoaDuLieuTruong(); if (dong is not null) DienDuLieu(dong);
            if (dong is null && kind == LoaiDanhMuc.ChuyenBay) foreach (var khoa in new[] { "sold", "available", "revenue" }) fields[khoa].Text = "";
            editor.BatDau(CheDoChinhSua.Xem, ChupDuLieu());
        }
        finally { binding = false; }
        ChonDong(LayMa(dong)); CapNhatThongTinChuyen(); CapNhatDanhSachCon(); CapNhatTrangThaiNut();
    }
    private void XoaDuLieuTruong()
    {
        foreach (var (khoa, dieuKhien) in fields)
            switch (dieuKhien)
            {
                case TextBox t: t.Clear(); break;
                case NumericUpDown n: n.Value = 0; break;
                case DateTimePicker boChonNgay: boChonNgay.Value = khoa == "dt" ? DateTime.Today.AddHours(8) : khoa == "at" ? DateTime.Today.AddHours(10) : DateTime.Today; break;
                case ComboBox hopChon: if (hopChon.Items.Count > 0) hopChon.SelectedIndex = 0; break;
            }
        if (kind == LoaiDanhMuc.ChuyenBay) { ((NumericUpDown)fields["total"]).Value = 120; fields["sold"].Text = "0"; fields["available"].Text = "120"; fields["revenue"].Text = "0"; }
        if (kind == LoaiDanhMuc.HangVe && scopeFlight > 0) ((ComboBox)fields["flight"]).SelectedValue = scopeFlight;
    }
    private void DienDuLieu(object dong)
    {
        fields["id"].Text = LayMa(dong).ToString();
        switch (dong)
        {
            case SanBayDTO sanBay: fields["name"].Text = sanBay.TenSanBay; fields["city"].Text = sanBay.ThanhPho; break;
            case QuanLyChuyenBayDTO m:
                var chuyenBay = m.ChuyenBay;
                ((ComboBox)fields["from"]).SelectedValue = chuyenBay.MaSanBayDi; ((ComboBox)fields["to"]).SelectedValue = chuyenBay.MaSanBayDen;
                ((NumericUpDown)fields["total"]).Value = chuyenBay.TongSoGhe;
                ((DateTimePicker)fields["dd"]).Value = chuyenBay.NgayKhoiHanh.ToDateTime(TimeOnly.MinValue); ((DateTimePicker)fields["dt"]).Value = DateTime.Today.Add(chuyenBay.GioKhoiHanh.ToTimeSpan());
                ((DateTimePicker)fields["ad"]).Value = chuyenBay.NgayDen.ToDateTime(TimeOnly.MinValue); ((DateTimePicker)fields["at"]).Value = DateTime.Today.Add(chuyenBay.GioDen.ToTimeSpan());
                ((ComboBox)fields["status"]).SelectedValue = chuyenBay.TrangThai;
                fields["sold"].Text = m.SoVeDaBan.ToString(); fields["available"].Text = chuyenBay.SoGheCon.ToString(); fields["revenue"].Text = m.TongDoanhThu.ToString("N2"); break;
            case HangVeDTO hangVe:
                ((ComboBox)fields["flight"]).SelectedValue = hangVe.MaChuyenBay; fields["name"].Text = hangVe.TenHangVe;
                ((NumericUpDown)fields["price"]).Value = hangVe.GiaVe; ((NumericUpDown)fields["kg"]).Value = hangVe.HanhLyCoBanKg; break;
        }
    }
    private void ThemBanGhi()
    {
        lastViewed = original; original = null; binding = true;
        try { XoaDuLieuTruong(); errors.Clear(); editor.BatDau(CheDoChinhSua.Them, ChupDuLieu()); }
        finally { binding = false; }
        ChonDong(0); CapNhatDanhSachCon(); CapNhatThongTinChuyen(); CapNhatTrangThaiNut(); fields[kind == LoaiDanhMuc.ChuyenBay ? "from" : "name"].Focus();
        ThongBao("Đang thêm mới. Chỉ ghi dữ liệu khi bấm Lưu.");
    }
    private void BatDauSua()
    {
        if (original is null || busy) return;
        editor.BatDau(CheDoChinhSua.Sua, ChupDuLieu()); errors.Clear(); CapNhatTrangThaiNut();
        fields[kind == LoaiDanhMuc.ChuyenBay ? "from" : "name"].Focus(); ThongBao("Đang chỉnh sửa. Lưu để áp dụng hoặc Hủy thay đổi để trở lại.");
    }
    private void HuyChinhSua() { TroVeCheDoXem(original ?? lastViewed); ThongBao("Đã trở lại chế độ xem, không ghi dữ liệu."); }
    private void CapNhatThongTinChuyen()
    {
        if (kind != LoaiDanhMuc.HangVe) { context.Text = editor.CheDo == CheDoChinhSua.Xem ? "Chọn dòng để xem; bấm Chỉnh sửa để thay đổi." : "Các trường hệ thống chỉ đọc."; return; }
        if (editor.CheDo == CheDoChinhSua.Xem && original is null && scopeFlight == 0) { context.Text = "Tất cả chuyến · Chọn dòng để xem hoặc Thêm mới."; return; }
        var ma = ((ComboBox)fields["flight"]).SelectedValue is int daChon ? daChon : scopeFlight;
        context.Text = DocLuaChonChuyenBay().FirstOrDefault(x => x.Id == ma)?.Label ?? "Chọn chuyến trước khi thêm hạng vé.";
    }
    private void CapNhatDanhSachCon()
    {
        children.Items.Clear();
        if (original is not QuanLyChuyenBayDTO selectedFlight)
        {
            childTitle.Text = "Hạng vé · Chưa chọn chuyến";
            return;
        }
        int maChuyenBay = selectedFlight.ChuyenBay.MaChuyenBay;
        childTitle.Text = $"Hạng vé · Chuyến #{maChuyenBay}";
        // ListView con chỉ hiển thị các hạng thuộc chuyến đang chọn ở ListView cha.
        foreach (HangVeDTO hangVe in fares)
        {
            if (hangVe.MaChuyenBay != maChuyenBay) continue;
            string[] cells = { hangVe.MaHangVe.ToString(), hangVe.TenHangVe, hangVe.GiaVe.ToString("N2"), hangVe.HanhLyCoBanKg.ToString() };
            children.Items.Add(new ListViewItem(cells));
        }
        if (children.Items.Count == 0) childTitle.Text += " · chưa có hạng vé";
    }
    private int DocSoNguyen(string khoa)
    {
        return (int)((NumericUpDown)fields[khoa]).Value;
    }
    private int LayMaDaChon(string khoa)
    {
        if (((ComboBox)fields[khoa]).SelectedValue is int ma) return ma;
        return 0;
    }
    private Task LuuDuLieu()
    {
        if (editor.CheDo == CheDoChinhSua.Xem || busy) return Task.CompletedTask;
        return ThucHien(async () =>
        {
            errors.Clear();
            int maTruocDo = LayMa(original);
            int saved;
            // GUI đọc control và tạo DTO; việc kiểm tra/ghi dữ liệu thuộc BUS và DAO.
            switch (kind)
            {
                case LoaiDanhMuc.SanBay:
                    var sanBay = new SanBayDTO(maTruocDo, fields["name"].Text, fields["city"].Text);
                    if (maTruocDo == 0)
                        saved = await Task.Run(() => bus.ThemSanBay(sanBay));
                    else
                        saved = await Task.Run(() => bus.SuaSanBay(sanBay, original as SanBayDTO));
                    break;
                case LoaiDanhMuc.ChuyenBay:
                    DateOnly departureDate = DateOnly.FromDateTime(((DateTimePicker)fields["dd"]).Value);
                    TimeOnly departureTime = TimeOnly.FromDateTime(((DateTimePicker)fields["dt"]).Value);
                    DateOnly arrivalDate = DateOnly.FromDateTime(((DateTimePicker)fields["ad"]).Value);
                    TimeOnly arrivalTime = TimeOnly.FromDateTime(((DateTimePicker)fields["at"]).Value);
                    var chuyenBay = new ThongTinSuaChuyenBay(maTruocDo, LayMaDaChon("from"), LayMaDaChon("to"), DocSoNguyen("total"),
                        departureDate, departureTime, arrivalDate, arrivalTime, LayLuaChon((ComboBox)fields["status"]));
                    if (maTruocDo == 0)
                        saved = await Task.Run(() => bus.ThemChuyenBay(chuyenBay));
                    else
                        saved = await Task.Run(() => bus.SuaChuyenBay(chuyenBay, original as QuanLyChuyenBayDTO));
                    break;
                default:
                    var hangVe = new HangVeDTO(maTruocDo, LayMaDaChon("flight"), fields["name"].Text,
                        ((NumericUpDown)fields["price"]).Value, DocSoNguyen("kg"));
                    saved = await Task.Run(() => bus.LuuHangVe(hangVe, original as HangVeDTO));
                    break;
            }
            var ma = maTruocDo > 0 ? maTruocDo : saved;
            // A committed insert must never be retried if only the subsequent reload fails.
            editor.BatDau(CheDoChinhSua.Xem, ChupDuLieu()); pendingSavedId = ma; fields["id"].Text = ma.ToString(); CapNhatTrangThaiNut();
            try
            {
                await DocToanBo(); if (IsDisposed) return;
                HienThiDanhSach(); TroVeCheDoXem(TimTheoMa(ma));
                ThongBao($"Đã lưu #{ma}." + (list.Items.Cast<ListViewItem>().Any(i => LayMa(i.Tag) == ma) ? " Đang hiển thị bản ghi vừa lưu." : " Bản ghi nằm ngoài bộ lọc; chi tiết vẫn được giữ."));
            }
            catch (LoiQuanLy loi) { ThongBao($"Đã lưu #{ma}, nhưng chưa tải lại được: {loi.Message}", true); }
        }, "Đang lưu dữ liệu…");
    }
    private Task XoaDuLieu()
    {
        if (original is null || busy || editor.CheDo != CheDoChinhSua.Xem) return Task.CompletedTask;
        var daChon = original;
        var nhan = daChon switch { SanBayDTO sanBay => sanBay.TenSanBay, HangVeDTO chuyenBay => chuyenBay.TenHangVe, QuanLyChuyenBayDTO chuyenBay => $"{chuyenBay.ChuyenBay.ThanhPhoDi} → {chuyenBay.ChuyenBay.ThanhPhoDen}", _ => "" };
        if (MessageBox.Show(this, $"Xóa #{LayMa(daChon)} — {nhan}?\nDữ liệu đã được tham chiếu sẽ không được xóa.", "Xác nhận xóa bản ghi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return Task.CompletedTask;
        return ThucHien(async () =>
        {
            // GUI gọi đúng nghiệp vụ theo DTO đang chọn; BUS/DAO quyết định có được xóa hay không.
            switch (daChon)
            {
                case SanBayDTO sanBay:
                    await Task.Run(() => bus.XoaSanBay(sanBay));
                    break;
                case QuanLyChuyenBayDTO chuyenBay:
                    await Task.Run(() => bus.XoaChuyenBay(chuyenBay));
                    break;
                case HangVeDTO hangVe:
                    await Task.Run(() => bus.XoaHangVe(hangVe));
                    break;
            }
            TroVeCheDoXem(null);
            await DocToanBo();
            if (!IsDisposed)
            {
                HienThiDanhSach();
                ThongBao($"Đã xóa bản ghi #{LayMa(daChon)}.");
            }
        }, "Đang xóa bản ghi…");
    }
    private Task ChonSanBay(string khoa)
    {
        using var hopThoai = new HopThoaiChon("Chọn sân bay", airports.Cast<object>().ToList(), luaChon => { var sanBay = (SanBayDTO)luaChon; return $"#{sanBay.MaSanBay} · {sanBay.TenSanBay} · {sanBay.ThanhPho}"; });
        if (hopThoai.ShowDialog(this) == DialogResult.OK && HopThoaiChon.GiaTriDaChon is SanBayDTO sanBay) ((ComboBox)fields[khoa]).SelectedValue = sanBay.MaSanBay;
        return Task.CompletedTask;
    }
    private Task ChonChuyenBay()
    {
        using var hopThoai = new HopThoaiChon("Chọn chuyến bay", flights.Cast<object>().ToList(), luaChon => { var chuyenBay = ((QuanLyChuyenBayDTO)luaChon).ChuyenBay; return $"#{chuyenBay.MaChuyenBay} · {chuyenBay.ThanhPhoDi} → {chuyenBay.ThanhPhoDen} · {chuyenBay.ThoiDiemKhoiHanh:dd/MM/yyyy HH:mm}"; });
        if (hopThoai.ShowDialog(this) == DialogResult.OK && HopThoaiChon.GiaTriDaChon is QuanLyChuyenBayDTO m) ((ComboBox)fields["flight"]).SelectedValue = m.ChuyenBay.MaChuyenBay;
        return Task.CompletedTask;
    }
    public async Task HienThiHangVe(int maChuyenBay)
    {
        // Lần mở tab đầu tiên có thể đã bắt đầu tải từ VisibleChanged.
        if (loading is { IsCompleted: false }) await loading;
        if (IsDisposed || !CoTheRoiTrang()) return;
        await ThucHien(async () =>
        {
            // Tab có thể đã tải trước khi chuyến/hạng được thêm hoặc sửa ở nơi khác.
            await DocToanBo(); if (IsDisposed) return;
            if (!flights.Cast<QuanLyChuyenBayDTO>().Any(m => m.ChuyenBay.MaChuyenBay == maChuyenBay))
                throw new LoiQuanLy("Chuyến bay không còn tồn tại. Hãy tải lại và chọn chuyến khác.");
            scopeFlight = maChuyenBay; binding = true;
            try { fareScope.SelectedValue = maChuyenBay; }
            finally { binding = false; }
            TroVeCheDoXem(null); HienThiDanhSach();
        }, "Đang tải hạng vé của chuyến…");
    }
    protected override void Dispose(bool disposing)
    {
        if (disposing) { errors.Dispose(); airportTips.Dispose(); }
        base.Dispose(disposing);
    }
}
