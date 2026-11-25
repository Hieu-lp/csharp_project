using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>Màn hình CRUD mã giảm giá; BUS kiểm tra trùng mã, giá trị và thời hạn.</summary>
public sealed class QuanLyMaGiamGiaForm : Form
{
    private const decimal GiaTriTienToiDa = 9999999999999999.99m;
    private readonly MaGiamGiaBUS bus = new();

    private readonly TextBox oMa = TaoONhap(chiDoc: true);
    private readonly TextBox oMaGiamGia = TaoONhap();
    private readonly TextBox oMoTa = new() { Multiline = true, Height = 54, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox oLoaiGiam = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox oTrangThai = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown oGiaTriGiam = TaoONhapTien();
    private readonly NumericUpDown oDonToiThieu = TaoONhapTien();
    private readonly NumericUpDown oGiamToiDa = TaoONhapTien();
    private readonly NumericUpDown oGioiHan = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 150
    };
    private readonly CheckBox coGiamToiDa = new() { Text = "Có mức giảm tối đa", AutoSize = true };
    private readonly CheckBox coGioiHan = new() { Text = "Có giới hạn lượt dùng", AutoSize = true };
    private readonly DateTimePicker oNgayBatDau = TaoONhapNgayGio();
    private readonly DateTimePicker oNgayKetThuc = TaoONhapNgayGio();
    private readonly TextBox oDaDung = TaoONhap(chiDoc: true);
    private readonly TextBox oTimKiem = TaoONhap();
    private readonly ListView danhSach = GiaoDien.TaoDanhSach();
    private bool dangGanDuLieu;
    private ListViewItem? dongDangChon;
    private string duLieuBanDau = "";
    private readonly Label thongBao = new() { AutoSize = true, Text = "Sẵn sàng." };

    public QuanLyMaGiamGiaForm()
    {
        Text = "Quản lý mã giảm giá";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 760);
        Size = new Size(1380, 900);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;

        oLoaiGiam.Items.AddRange(["PERCENT", "FIXED"]);
        oLoaiGiam.SelectedIndex = 0;
        oTrangThai.Items.AddRange(["ACTIVE", "INACTIVE"]);
        oTrangThai.SelectedIndex = 0;
        oNgayBatDau.Value = DateTime.Now;
        oNgayKetThuc.Value = DateTime.Now.AddDays(7);
        oGiamToiDa.Enabled = false;
        oGioiHan.Enabled = false;

        TaoCotDanhSach();
        TaoBoCuc();
        GanSuKien();
        duLieuBanDau = ChupDuLieu();
        FormClosing += (_, suKien) => suKien.Cancel = !CoTheRoiTrang();
        Load += (_, _) => TaiDanhSach();
    }

    private void TaoBoCuc()
    {
        var khungChinh = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 6
        };
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int dong = 0; dong < 5; dong++) khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        khungChinh.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ MÃ GIẢM GIÁ",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 14)
        }, 0, 0);
        khungChinh.Controls.Add(TaoVungTimKiem(), 0, 1);
        var vungNhap = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        vungNhap.Controls.Add(TaoTruongNhap());
        khungChinh.Controls.Add(vungNhap, 0, 2);
        // Nút và thông báo nằm ngoài vùng cuộn của các trường nhập.
        khungChinh.RowStyles[2] = new RowStyle(SizeType.Percent, 45);
        khungChinh.RowStyles[5] = new RowStyle(SizeType.Percent, 55);
        khungChinh.Controls.Add(TaoVungNut(), 0, 3);
        khungChinh.Controls.Add(thongBao, 0, 4);
        khungChinh.Controls.Add(danhSach, 0, 5);
        Controls.Add(khungChinh);
    }

    private Control TaoVungTimKiem()
    {
        var vung = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 12)
        };
        vung.Controls.Add(new Label { AutoSize = true, Text = "Từ khóa:", Margin = new Padding(3, 9, 8, 3) });
        oTimKiem.Width = 380;
        oTimKiem.PlaceholderText = "Mã, mô tả, loại giảm, trạng thái...";
        vung.Controls.Add(oTimKiem);
        vung.Controls.Add(TaoNut("Tìm kiếm", (_, _) => TimKiem()));
        vung.Controls.Add(TaoNut("Tải lại", (_, _) => TaiDanhSach()));
        return vung;
    }

    private Control TaoTruongNhap()
    {
        var cacTruong = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 7,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 8)
        };
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        for (int dong = 0; dong < 7; dong++) cacTruong.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ThemTruong(cacTruong, 0, 0, "Mã bản ghi:", oMa);
        ThemTruong(cacTruong, 0, 2, "Mã giảm giá:", oMaGiamGia);
        ThemTruong(cacTruong, 1, 0, "Mô tả:", oMoTa, soCot: 3);
        ThemTruong(cacTruong, 2, 0, "Loại giảm:", oLoaiGiam);
        ThemTruong(cacTruong, 2, 2, "Giá trị giảm (%/đồng):", oGiaTriGiam);
        ThemTruong(cacTruong, 3, 0, "Đơn tối thiểu (đồng):", oDonToiThieu);
        ThemTruong(cacTruong, 3, 2, "Giảm tối đa (đồng):", TaoTruongTuyChon(coGiamToiDa, oGiamToiDa));
        ThemTruong(cacTruong, 4, 0, "Bắt đầu:", oNgayBatDau);
        ThemTruong(cacTruong, 4, 2, "Kết thúc:", oNgayKetThuc);
        ThemTruong(cacTruong, 5, 0, "Giới hạn lượt:", TaoTruongTuyChon(coGioiHan, oGioiHan));
        ThemTruong(cacTruong, 5, 2, "Đã sử dụng:", oDaDung);
        ThemTruong(cacTruong, 6, 0, "Trạng thái:", oTrangThai);
        return cacTruong;
    }

    private Control TaoTruongTuyChon(CheckBox oDanhDau, Control oNhap)
    {
        var vung = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0)
        };
        oDanhDau.CheckedChanged += (_, _) => oNhap.Enabled = oDanhDau.Checked;
        vung.Controls.Add(oDanhDau);
        vung.Controls.Add(oNhap);
        return vung;
    }

    private Control TaoVungNut()
    {
        var vung = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 8)
        };
        vung.Controls.Add(TaoNut("Thêm", ThemMaGiamGia_Click));
        vung.Controls.Add(TaoNut("Cập nhật", SuaMaGiamGia_Click));
        vung.Controls.Add(TaoNut("Xóa", XoaMaGiamGia_Click));
        vung.Controls.Add(TaoNut("Làm trống form", (_, _) => XoaDuLieuNhap()));
        return vung;
    }

    private void TaoCotDanhSach()
    {
        ThemCot(nameof(MaGiamGiaQuanLyDTO.MaGiamGia), "Mã", 6);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.Ma), "Mã giảm giá", 10);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.MoTa), "Mô tả", 15);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.LoaiGiam), "Loại", 7);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.GiaTriGiam), "Giá trị", 8);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.DonToiThieu), "Đơn tối thiểu", 9);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.GiamToiDa), "Giảm tối đa", 9);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.NgayBatDau), "Bắt đầu", 11);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.NgayKetThuc), "Kết thúc", 11);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.GioiHanLuotDung), "Giới hạn", 6);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.SoLuotDaDung), "Đã dùng", 5);
        ThemCot(nameof(MaGiamGiaQuanLyDTO.TrangThai), "Trạng thái", 7);
    }

    private void ThemCot(string tenThuocTinh, string tieuDe, float doRong)
    {
        // tenThuocTinh là tên cột ổn định; định dạng ô được ghi rõ khi đưa DTO vào danh sách.
        danhSach.Columns.Add(new ColumnHeader { Name = tenThuocTinh, Text = tieuDe, Width = (int)(doRong * 10) });
    }

    private void GanSuKien()
    {
        danhSach.SelectedIndexChanged += (_, _) => DienDuLieuDaChon();
        oTimKiem.KeyDown += (_, suKien) =>
        {
            if (suKien.KeyCode != Keys.Enter) return;
            TimKiem();
            suKien.SuppressKeyPress = true;
        };
    }

    private void TaiDanhSach()
    {
        if (!CoTheRoiTrang()) return;
        try
        {
            HienThiDanhSach(bus.DocDSMaGiamGia());
            thongBao.Text = $"Đang hiển thị {danhSach.Items.Count} mã giảm giá.";
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void TimKiem()
    {
        if (!CoTheRoiTrang()) return;
        try
        {
            ArrayList ketQua = bus.TimKiemMaGiamGia(oTimKiem.Text);
            HienThiDanhSach(ketQua);
            thongBao.Text = $"Tìm thấy {ketQua.Count} mã giảm giá.";
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void ThemMaGiamGia_Click(object? nguoiGui, EventArgs suKien)
    {
        try
        {
            MaGiamGiaQuanLyDTO maGiamGia = DocDuLieuNhap();
            string? loi = bus.ThemMaGiamGia(maGiamGia);
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã thêm mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void SuaMaGiamGia_Click(object? nguoiGui, EventArgs suKien)
    {
        if (!DocMaDaChon(out int ma))
        {
            MessageBox.Show("Hãy chọn mã giảm giá cần cập nhật trong danh sách.", "Chưa chọn mã",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            MaGiamGiaQuanLyDTO maGiamGia = DocDuLieuNhap();
            maGiamGia.MaGiamGia = ma;
            string? loi = bus.SuaMaGiamGia(maGiamGia);
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã cập nhật mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void XoaMaGiamGia_Click(object? nguoiGui, EventArgs suKien)
    {
        if (!DocMaDaChon(out int ma))
        {
            MessageBox.Show("Hãy chọn mã giảm giá cần xóa trong danh sách.", "Chưa chọn mã",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show($"Bạn có chắc muốn xóa mã giảm giá mã {ma} không?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            string? loi = bus.XoaMaGiamGia(ma);
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã xóa mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private MaGiamGiaQuanLyDTO DocDuLieuNhap() => new()
    {
        Ma = oMaGiamGia.Text,
        MoTa = oMoTa.Text,
        LoaiGiam = oLoaiGiam.Text,
        GiaTriGiam = oGiaTriGiam.Value,
        DonToiThieu = oDonToiThieu.Value,
        GiamToiDa = coGiamToiDa.Checked ? oGiamToiDa.Value : null,
        NgayBatDau = oNgayBatDau.Value,
        NgayKetThuc = oNgayKetThuc.Value,
        GioiHanLuotDung = coGioiHan.Checked ? decimal.ToInt32(oGioiHan.Value) : null,
        TrangThai = oTrangThai.Text
        // Không nhận SoLuotDaDung từ ô nhập; BUS đọc số lượt thật khi cập nhật.
    };

    private void DienDuLieuDaChon()
    {
        if (dangGanDuLieu || danhSach.SelectedItems.Count == 0) return;
        var dong = danhSach.SelectedItems[0];
        if (dong == dongDangChon) return;
        if (!CoTheRoiTrang())
        {
            dangGanDuLieu = true;
            dong.Selected = false;
            if (dongDangChon is not null) dongDangChon.Selected = true;
            dangGanDuLieu = false;
            return;
        }
        if (dong.Tag is not MaGiamGiaQuanLyDTO maGiamGia) return;
        dongDangChon = dong;
        oMa.Text = maGiamGia.MaGiamGia.ToString();
        oMaGiamGia.Text = maGiamGia.Ma;
        oMoTa.Text = maGiamGia.MoTa;
        oLoaiGiam.Text = maGiamGia.LoaiGiam;
        oGiaTriGiam.Value = Math.Clamp(maGiamGia.GiaTriGiam, oGiaTriGiam.Minimum, oGiaTriGiam.Maximum);
        oDonToiThieu.Value = Math.Clamp(maGiamGia.DonToiThieu, oDonToiThieu.Minimum, oDonToiThieu.Maximum);
        coGiamToiDa.Checked = maGiamGia.GiamToiDa.HasValue;
        if (maGiamGia.GiamToiDa.HasValue)
            oGiamToiDa.Value = Math.Clamp(maGiamGia.GiamToiDa.Value, oGiamToiDa.Minimum, oGiamToiDa.Maximum);
        oNgayBatDau.Value = maGiamGia.NgayBatDau;
        oNgayKetThuc.Value = maGiamGia.NgayKetThuc;
        coGioiHan.Checked = maGiamGia.GioiHanLuotDung.HasValue;
        if (maGiamGia.GioiHanLuotDung.HasValue) oGioiHan.Value = maGiamGia.GioiHanLuotDung.Value;
        oDaDung.Text = maGiamGia.SoLuotDaDung.ToString();
        oTrangThai.Text = maGiamGia.TrangThai;
        duLieuBanDau = ChupDuLieu();
    }

    private bool DocMaDaChon(out int ma) => int.TryParse(oMa.Text, out ma) && ma > 0;

    private void XoaDuLieuNhap(bool xacNhan = true)
    {
        if (xacNhan && !CoTheRoiTrang()) return;
        dangGanDuLieu = true;
        while (danhSach.SelectedItems.Count > 0)
        {
            danhSach.SelectedItems[0].Selected = false;
        }
        dongDangChon = null;
        dangGanDuLieu = false;
        oMa.Clear();
        oMaGiamGia.Clear();
        oMoTa.Clear();
        oLoaiGiam.SelectedIndex = 0;
        oGiaTriGiam.Value = 0;
        oDonToiThieu.Value = 0;
        coGiamToiDa.Checked = false;
        oGiamToiDa.Value = 0;
        oNgayBatDau.Value = DateTime.Now;
        oNgayKetThuc.Value = DateTime.Now.AddDays(7);
        coGioiHan.Checked = false;
        oGioiHan.Value = 0;
        oDaDung.Text = "0";
        oTrangThai.SelectedIndex = 0;
        oMaGiamGia.Focus();
        duLieuBanDau = ChupDuLieu();
    }

    private static bool HienThiLoiNghiepVu(string? loi)
    {
        if (loi is null) return false;
        MessageBox.Show(loi, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private void HienThiLoiDuLieu(Exception loiGoc)
    {
        thongBao.Text = "Không tải/thao tác được mã giảm giá.";
        string chiTiet = loiGoc switch
        {
            SqlException { Number: 547 } => "Dữ liệu đang được tham chiếu hoặc vi phạm ràng buộc. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 2601 or 2627 } => "Dữ liệu bị trùng. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 1205 } => "Dữ liệu đang được xử lý đồng thời. Hãy tải lại trước khi thử lại.",
            _ => loiGoc.Message
        };
        MessageBox.Show($"Không thể hoàn thành thao tác mã giảm giá.\nChi tiết: {chiTiet}",
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void ThemTruong(TableLayoutPanel bang, int dong, int cotNhan, string nhan, Control oNhap, int soCot = 1)
    {
        bang.Controls.Add(new Label
        {
            AutoSize = true,
            Text = nhan,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 3)
        }, cotNhan, dong);
        oNhap.Dock = DockStyle.Fill;
        oNhap.Margin = new Padding(3, 3, 12, 8);
        bang.Controls.Add(oNhap, cotNhan + 1, dong);
        if (soCot > 1) bang.SetColumnSpan(oNhap, soCot);
    }

    private static TextBox TaoONhap(bool chiDoc = false) => new()
    {
        ReadOnly = chiDoc,
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static NumericUpDown TaoONhapTien() => new()
    {
        Minimum = 0,
        Maximum = GiaTriTienToiDa,
        DecimalPlaces = 2,
        Increment = 0.01m,
        ThousandsSeparator = true,
        Dock = DockStyle.Fill
    };

    private static DateTimePicker TaoONhapNgayGio() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        ShowUpDown = true,
        Dock = DockStyle.Fill
    };

    private static Button TaoNut(string chuoi, EventHandler xuLy)
    {
        var nut = new Button
        {
            Text = chuoi,
            AutoSize = true,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            BackColor = chuoi is "Thêm" or "Cập nhật" ? GiaoDien.MauChinh : Color.White,
            ForeColor = chuoi is "Thêm" or "Cập nhật" ? Color.White : chuoi == "Xóa" ? GiaoDien.MauLoi : GiaoDien.MauChu,
            Margin = new Padding(3, 3, 8, 3),
            Padding = new Padding(8, 2, 8, 2)
        };
        nut.Click += xuLy;
        return nut;
    }

    private void HienThiDanhSach(IEnumerable nguon)
    {
        dangGanDuLieu = true;
        danhSach.BeginUpdate();
        try
        {
            danhSach.Items.Clear();
            dongDangChon = null;
            foreach (MaGiamGiaQuanLyDTO maGiamGia in nguon)
            {
                ListViewItem dong = new ListViewItem(maGiamGia.MaGiamGia.ToString());
                dong.SubItems.Add(maGiamGia.Ma);
                dong.SubItems.Add(maGiamGia.MoTa);
                dong.SubItems.Add(maGiamGia.LoaiGiam);
                dong.SubItems.Add(maGiamGia.GiaTriGiam.ToString("N2"));
                dong.SubItems.Add(maGiamGia.DonToiThieu.ToString("N2"));
                dong.SubItems.Add(maGiamGia.GiamToiDa?.ToString("N2") ?? "Không giới hạn");
                dong.SubItems.Add(maGiamGia.NgayBatDau.ToString("dd/MM/yyyy HH:mm"));
                dong.SubItems.Add(maGiamGia.NgayKetThuc.ToString("dd/MM/yyyy HH:mm"));
                dong.SubItems.Add(maGiamGia.GioiHanLuotDung?.ToString() ?? "Không giới hạn");
                dong.SubItems.Add(maGiamGia.SoLuotDaDung.ToString());
                dong.SubItems.Add(maGiamGia.TrangThai == "ACTIVE" ? "Đang áp dụng" : "Ngừng áp dụng");
                dong.Tag = maGiamGia;
                danhSach.Items.Add(dong);
            }
        }
        finally
        {
            danhSach.EndUpdate();
            dangGanDuLieu = false;
        }
        XoaDuLieuNhap(false);
    }

    private string ChupDuLieu() => System.Text.Json.JsonSerializer.Serialize(DocDuLieuNhap());

    public bool CoTheRoiTrang()
    {
        if (ChupDuLieu() == duLieuBanDau) return true;
        return MessageBox.Show(this, "Bỏ các thay đổi chưa lưu? Nội dung đang nhập sẽ không được lưu.",
            "Thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question,
            MessageBoxDefaultButton.Button2) == DialogResult.Yes;
    }
}
