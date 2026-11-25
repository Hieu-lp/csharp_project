using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>Màn hình CRUD nhân viên; mọi luật dữ liệu được kiểm tra ở NhanVienBUS.</summary>
public sealed class QuanLyNhanVienForm : Form
{
    private readonly NhanVienBUS bus = new();
    private readonly TextBox oMa = TaoONhap(chiDoc: true);
    private readonly TextBox oHoTen = TaoONhap();
    private readonly TextBox oDienThoai = TaoONhap();
    private readonly TextBox oEmail = TaoONhap();
    private readonly TextBox oChucVu = TaoONhap();
    private readonly TextBox oTimKiem = TaoONhap();
    private readonly DateTimePicker oNgayVaoLam = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        MaxDate = DateTime.Today,
        Dock = DockStyle.Fill
    };
    private readonly ComboBox oTrangThai = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };
    private readonly ListView danhSach = GiaoDien.TaoDanhSach();
    private bool dangGanDuLieu;
    private ListViewItem? dongDangChon;
    private string duLieuBanDau = "";
    private readonly Label thongBao = new() { AutoSize = true, Text = "Sẵn sàng." };

    public QuanLyNhanVienForm()
    {
        Text = "Quản lý nhân viên";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Size = new Size(1180, 780);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;
        oTrangThai.Items.AddRange(["ACTIVE", "INACTIVE"]);
        oTrangThai.SelectedIndex = 0;

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
        for (int dong = 0; dong < 5; dong++)
            khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        khungChinh.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ NHÂN VIÊN",
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
        oTimKiem.Width = 360;
        oTimKiem.PlaceholderText = "Tên, điện thoại, email, chức vụ, trạng thái...";
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
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 8)
        };
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        for (int dong = 0; dong < 4; dong++) cacTruong.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ThemTruong(cacTruong, 0, 0, "Mã nhân viên:", oMa);
        ThemTruong(cacTruong, 0, 2, "Họ và tên:", oHoTen);
        ThemTruong(cacTruong, 1, 0, "Điện thoại:", oDienThoai);
        ThemTruong(cacTruong, 1, 2, "Email:", oEmail);
        ThemTruong(cacTruong, 2, 0, "Chức vụ:", oChucVu);
        ThemTruong(cacTruong, 2, 2, "Ngày vào làm:", oNgayVaoLam);
        ThemTruong(cacTruong, 3, 0, "Trạng thái:", oTrangThai);
        return cacTruong;
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
        vung.Controls.Add(TaoNut("Thêm", (_, _) => ThemBanGhi()));
        vung.Controls.Add(TaoNut("Cập nhật", (_, _) => SuaBanGhi()));
        vung.Controls.Add(TaoNut("Xóa", (_, _) => XoaBanGhi()));
        vung.Controls.Add(TaoNut("Làm trống form", (_, _) => XoaDuLieuNhap()));
        return vung;
    }

    private void TaoCotDanhSach()
    {
        ThemCot(nameof(NhanVienDTO.MaNhanVien), "Mã NV", 9);
        ThemCot(nameof(NhanVienDTO.HoTen), "Họ và tên", 20);
        ThemCot(nameof(NhanVienDTO.DienThoai), "Điện thoại", 13);
        ThemCot(nameof(NhanVienDTO.Email), "Email", 22);
        ThemCot(nameof(NhanVienDTO.ChucVu), "Chức vụ", 15);
        ThemCot(nameof(NhanVienDTO.NgayVaoLam), "Ngày vào làm", 11);
        ThemCot(nameof(NhanVienDTO.TrangThai), "Trạng thái", 10);
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
            HienThiDanhSach(bus.DocDanhSach());
            thongBao.Text = $"Đang hiển thị {danhSach.Items.Count} nhân viên.";
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void TimKiem()
    {
        if (!CoTheRoiTrang()) return;
        try
        {
            var ketQua = bus.TimKiem(oTimKiem.Text).Cast<NhanVienDTO>().ToList();
            HienThiDanhSach(ketQua);
            thongBao.Text = $"Tìm thấy {ketQua.Count} nhân viên.";
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void ThemBanGhi()
    {
        try
        {
            string? loi = bus.Them(DocDuLieuNhap());
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã thêm nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void SuaBanGhi()
    {
        if (!DocMaDaChon(out int ma))
        {
            MessageBox.Show("Hãy chọn nhân viên cần cập nhật trong danh sách.", "Chưa chọn nhân viên",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            NhanVienDTO nhanVien = DocDuLieuNhap();
            nhanVien.MaNhanVien = ma;
            string? loi = bus.Sua(nhanVien);
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã cập nhật nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private void XoaBanGhi()
    {
        if (!DocMaDaChon(out int ma))
        {
            MessageBox.Show("Hãy chọn nhân viên cần xóa trong danh sách.", "Chưa chọn nhân viên",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show($"Bạn có chắc muốn xóa nhân viên mã {ma} không?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            string? loi = bus.Xoa(ma);
            if (HienThiLoiNghiepVu(loi)) return;
            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã xóa nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc) { HienThiLoiDuLieu(loiGoc); }
    }

    private NhanVienDTO DocDuLieuNhap() => new()
    {
        HoTen = oHoTen.Text,
        DienThoai = oDienThoai.Text,
        Email = oEmail.Text,
        ChucVu = oChucVu.Text,
        NgayVaoLam = oNgayVaoLam.Value.Date,
        TrangThai = oTrangThai.Text
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
        if (dong.Tag is not NhanVienDTO nhanVien) return;
        dongDangChon = dong;
        oMa.Text = nhanVien.MaNhanVien.ToString();
        oHoTen.Text = nhanVien.HoTen;
        oDienThoai.Text = nhanVien.DienThoai;
        oEmail.Text = nhanVien.Email;
        oChucVu.Text = nhanVien.ChucVu;
        oNgayVaoLam.Value = nhanVien.NgayVaoLam;
        oTrangThai.Text = nhanVien.TrangThai;
        duLieuBanDau = ChupDuLieu();
    }

    private bool DocMaDaChon(out int ma) => int.TryParse(oMa.Text, out ma) && ma > 0;

    private void XoaDuLieuNhap(bool xacNhan = true)
    {
        if (xacNhan && !CoTheRoiTrang()) return;
        dangGanDuLieu = true;
        foreach (ListViewItem dong in danhSach.SelectedItems.Cast<ListViewItem>().ToArray()) dong.Selected = false;
        dongDangChon = null;
        dangGanDuLieu = false;
        oMa.Clear();
        oHoTen.Clear();
        oDienThoai.Clear();
        oEmail.Clear();
        oChucVu.Clear();
        oNgayVaoLam.Value = DateTime.Today;
        oTrangThai.SelectedIndex = 0;
        oHoTen.Focus();
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
        thongBao.Text = "Không tải/thao tác được dữ liệu nhân viên.";
        string chiTiet = loiGoc switch
        {
            SqlException { Number: 547 } => "Dữ liệu đang được tham chiếu hoặc vi phạm ràng buộc. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 2601 or 2627 } => "Dữ liệu bị trùng. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 1205 } => "Dữ liệu đang được xử lý đồng thời. Hãy tải lại trước khi thử lại.",
            _ => loiGoc.Message
        };
        MessageBox.Show($"Không thể hoàn thành thao tác nhân viên.\nChi tiết: {chiTiet}",
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void ThemTruong(TableLayoutPanel bang, int dong, int cotNhan, string nhan, Control oNhap)
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
    }

    private static TextBox TaoONhap(bool chiDoc = false) => new()
    {
        ReadOnly = chiDoc,
        Dock = DockStyle.Fill,
        Height = 30
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

    private void HienThiDanhSach(IEnumerable<NhanVienDTO> nguon)
    {
        dangGanDuLieu = true;
        danhSach.BeginUpdate();
        try
        {
            danhSach.Items.Clear();
            dongDangChon = null;
            foreach (NhanVienDTO nhanVien in nguon)
            {
                string[] cacO = [nhanVien.MaNhanVien.ToString(), nhanVien.HoTen, nhanVien.DienThoai, nhanVien.Email, nhanVien.ChucVu, nhanVien.NgayVaoLam.ToString("dd/MM/yyyy"), nhanVien.TrangThai == "ACTIVE" ? "Đang làm việc" : "Ngừng làm việc"];
                danhSach.Items.Add(new ListViewItem(cacO) { Tag = nhanVien });
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
