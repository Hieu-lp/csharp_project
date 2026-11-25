namespace FlightBooking.WinForms;

public sealed class HopThoaiChon : Form
{
    public static object? GiaTriDaChon { get; private set; }
    public HopThoaiChon(string tieuDe, List<object> nguon, Func<object, string> nhan)
    {
        GiaTriDaChon = null; Text = tieuDe; Font = new Font("Segoe UI", 11);
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; BackColor = GiaoDien.MauNen;
        StartPosition = FormStartPosition.CenterParent;
        var vungLamViec = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = GiaoDien.KichThuocCuaSo(vungLamViec, new Size(900, 580)).Size;
        MinimumSize = new Size(Math.Min(600, vungLamViec.Width - 24), Math.Min(360, vungLamViec.Height - 24));
        Padding = new Padding(20);
        var khungChinh = GiaoDien.TaoBang(); khungChinh.Dock = DockStyle.Fill; khungChinh.AutoSize = false;
        khungChinh.RowCount = 4; khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var nhanTieuDe = GiaoDien.TaoNhan(tieuDe, 17, true); khungChinh.Controls.Add(nhanTieuDe, 0, 0);
        var oTimKiem = new TextBox { Dock = DockStyle.Fill, AccessibleName = "Từ khóa chọn dữ liệu", PlaceholderText = "Nhập tên, thành phố, tuyến hoặc mã…", Margin = new Padding(0, 4, 0, 12) };
        var vungTimKiem = GiaoDien.TaoBang(); GiaoDien.ThemDong(vungTimKiem, GiaoDien.TaoNhan("Tìm dữ liệu")); GiaoDien.ThemDong(vungTimKiem, oTimKiem); khungChinh.Controls.Add(vungTimKiem, 0, 1);
        var danhSach = GiaoDien.TaoDanhSach(); danhSach.Columns.Add("Mã · Thông tin lựa chọn", 800); khungChinh.Controls.Add(danhSach, 0, 2);
        var soLuong = GiaoDien.TaoNhan("");
        var chanTrang = GiaoDien.TaoBang(2); chanTrang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); chanTrang.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        chanTrang.RowCount = 1; chanTrang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        chanTrang.Controls.Add(soLuong, 0, 0);
        void ChonDong()
        {
            if (danhSach.SelectedItems.Count == 0) { soLuong.Text = "Chọn một dòng trước khi xác nhận."; return; }
            GiaTriDaChon = danhSach.SelectedItems[0].Tag; DialogResult = DialogResult.OK; Close();
        }
        var nutChon = GiaoDien.TaoNut("Chọn", () => { ChonDong(); return Task.CompletedTask; }, chinh: true);
        var nutHuy = GiaoDien.TaoNut("Hủy", () => { DialogResult = DialogResult.Cancel; Close(); return Task.CompletedTask; });
        nutHuy.DialogResult = DialogResult.Cancel;
        var vungNut = GiaoDien.TaoVungNut(); vungNut.Padding = new Padding(0, 12, 0, 0); vungNut.Controls.Add(nutChon); vungNut.Controls.Add(nutHuy); chanTrang.Controls.Add(vungNut, 1, 0);
        khungChinh.Controls.Add(chanTrang, 0, 3); Controls.Add(khungChinh); CancelButton = nutHuy; AcceptButton = nutChon;
        void HienThiDanhSach()
        {
            danhSach.BeginUpdate(); danhSach.Items.Clear();
            foreach (var phanTu in nguon.Where(luaChon => nhan(luaChon).Contains(oTimKiem.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                danhSach.Items.Add(new ListViewItem(nhan(phanTu)) { Tag = phanTu });
            danhSach.EndUpdate(); soLuong.Text = danhSach.Items.Count == 0 ? "Không tìm thấy kết quả." : $"{danhSach.Items.Count} kết quả · Enter chọn · Esc hủy";
        }
        danhSach.DoubleClick += (_, _) => ChonDong(); oTimKiem.TextChanged += (_, _) => HienThiDanhSach();
        FormClosed += (_, _) => { if (DialogResult != DialogResult.OK) GiaTriDaChon = null; };
        HienThiDanhSach();
    }
}
