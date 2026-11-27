using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>
/// Màn hình quản lý khách hàng.
/// Form chỉ đọc/hiển thị dữ liệu và gọi BUS; câu lệnh SQL nằm trong DAO.
/// </summary>
public sealed class QuanLyKhachHangForm : Form
{
    private readonly KhachHangBUS bus = new();

    // Các ô nhập liệu tương ứng với thông tin trong KhachHangDTO.
    private readonly TextBox oMa = TaoONhap(chiDoc: true);
    private readonly TextBox oHoTen = TaoONhap();
    private readonly TextBox oDienThoai = TaoONhap();
    private readonly TextBox oEmail = TaoONhap();
    private readonly TextBox oGiayTo = TaoONhap();
    private readonly DateTimePicker oNgaySinh = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        MaxDate = DateTime.Today,
        Dock = DockStyle.Fill
    };
    private readonly ComboBox oGioiTinh = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };

    private readonly TextBox oTimKiem = TaoONhap();
    private readonly ListView danhSach = GiaoDien.TaoDanhSach();
    private bool dangGanDuLieu;
    private ListViewItem? dongDangChon;
    private string duLieuBanDau = "";
    private readonly Label thongBao = new()
    {
        AutoSize = true,
        Text = "Sẵn sàng."
    };

    private readonly Button nutTimKiem = TaoNut("Tìm kiếm");
    private readonly Button nutTaiLai = TaoNut("Tải lại");
    private readonly Button nutThem = TaoNut("Thêm");
    private readonly Button nutSua = TaoNut("Cập nhật");
    private readonly Button nutXoa = TaoNut("Xóa");
    private readonly Button nutLamTrong = TaoNut("Làm trống form");
    private readonly Button nutNhapExcel = TaoNut("Nhập .xls");
    private readonly Button nutMauExcel = TaoNut("Mẫu .xls");

    public QuanLyKhachHangForm()
    {
        Text = "Quản lý khách hàng";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 800);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;

        // Chỉ hiện một lựa chọn Nữ; dữ liệu cũ "Nu" được quy đổi khi nạp lên form.
        oGioiTinh.Items.AddRange(["Nam", "Nữ", "Khác"]);
        oGioiTinh.Text = "Nam";

        TaoCotDanhSach();
        TaoBoCuc();
        GanSuKien();

        // Chỉ truy cập database khi form chạy; không sửa cấu trúc database.
        duLieuBanDau = ChupDuLieu();
        FormClosing += (_, suKien) => suKien.Cancel = !CoTheRoiTrang();
        Load += (_, _) => TaiDanhSach();
    }

    /// <summary>
    /// Tạo bố cục từ trên xuống: tiêu đề, tìm kiếm, form nhập, nút, trạng thái, bảng.
    /// </summary>
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
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var tieuDe = new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ KHÁCH HÀNG",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 14)
        };
        khungChinh.Controls.Add(tieuDe, 0, 0);
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

        vung.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Từ khóa:",
            Margin = new Padding(3, 9, 8, 3)
        });

        oTimKiem.Width = 340;
        oTimKiem.PlaceholderText = "Tên, điện thoại, email, mã khách hàng...";
        vung.Controls.Add(oTimKiem);
        vung.Controls.Add(nutTimKiem);
        vung.Controls.Add(nutTaiLai);
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
            Padding = new Padding(0, 0, 0, 8),
            Margin = new Padding(0)
        };

        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));

        for (int dong = 0; dong < 4; dong++)
        {
            cacTruong.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        ThemTruong(cacTruong, 0, 0, "Mã khách hàng:", oMa);
        ThemTruong(cacTruong, 0, 2, "Họ và tên:", oHoTen);
        ThemTruong(cacTruong, 1, 0, "Điện thoại:", oDienThoai);
        ThemTruong(cacTruong, 1, 2, "Email:", oEmail);
        ThemTruong(cacTruong, 2, 0, "Ngày sinh:", oNgaySinh);
        ThemTruong(cacTruong, 2, 2, "Giới tính:", oGioiTinh);
        ThemTruong(cacTruong, 3, 0, "Số hộ chiếu/CCCD:", oGiayTo, soCot: 3);

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

        vung.Controls.Add(nutThem);
        vung.Controls.Add(nutSua);
        vung.Controls.Add(nutXoa);
        vung.Controls.Add(nutLamTrong);
        vung.Controls.Add(nutNhapExcel);
        vung.Controls.Add(nutMauExcel);
        return vung;
    }

    private void TaoCotDanhSach()
    {
        ThemCot(nameof(KhachHangDTO.MaKhachHang), "Mã KH", 8);
        ThemCot(nameof(KhachHangDTO.HoTen), "Họ và tên", 18);
        ThemCot(nameof(KhachHangDTO.DienThoai), "Điện thoại", 12);
        ThemCot(nameof(KhachHangDTO.Email), "Email", 19);
        ThemCot(nameof(KhachHangDTO.NgaySinh), "Ngày sinh", 11);
        ThemCot(nameof(KhachHangDTO.GioiTinh), "Giới tính", 9);
        ThemCot(nameof(KhachHangDTO.SoGiayTo), "Hộ chiếu/CCCD", 15);
    }

    private void ThemCot(string tenThuocTinh, string tieuDe, float doRong)
    {
        // tenThuocTinh là tên cột ổn định; định dạng ô được ghi rõ khi đưa DTO vào danh sách.
        danhSach.Columns.Add(new ColumnHeader { Name = tenThuocTinh, Text = tieuDe, Width = (int)(doRong * 10) });
    }

    private void GanSuKien()
    {
        nutTimKiem.Click += (_, _) => TimKiem();
        nutTaiLai.Click += (_, _) => TaiDanhSach();
        nutThem.Click += ThemKhachHang_Click;
        nutSua.Click += SuaKhachHang_Click;
        nutXoa.Click += XoaKhachHang_Click;
        nutLamTrong.Click += (_, _) => XoaDuLieuNhap();
        nutNhapExcel.Click += NhapExcel_Click;
        nutMauExcel.Click += MauExcel_Click;
        danhSach.SelectedIndexChanged += (_, _) => DienDuLieuDaChon();
        oTimKiem.KeyDown += (_, suKien) =>
        {
            if (suKien.KeyCode == Keys.Enter)
            {
                TimKiem();
                suKien.SuppressKeyPress = true;
            }
        };
    }

    private void TaiDanhSach()
    {
        if (!CoTheRoiTrang()) return;
        try
        {
            HienThiDanhSach(bus.DocDSKhachHang());
            thongBao.Text = $"Đang hiển thị {danhSach.Items.Count} khách hàng.";
        }
        catch (Exception loiGoc)
        {
            HienThiLoiDuLieu(loiGoc);
        }
    }

    private void TimKiem()
    {
        if (!CoTheRoiTrang()) return;
        try
        {
            // BUS trả ArrayList; GUI giữ DTO trong Tag của từng dòng ListView.
            ArrayList ketQua = bus.TimKiemKhachHang(oTimKiem.Text);

            HienThiDanhSach(ketQua);
            thongBao.Text = $"Tìm thấy {ketQua.Count} khách hàng.";
        }
        catch (Exception loiGoc)
        {
            HienThiLoiDuLieu(loiGoc);
        }
    }

    private void ThemKhachHang_Click(object? nguoiGui, EventArgs suKien)
    {
        try
        {
            KhachHangDTO khachHang = DocDuLieuNhap();
            string? loi = bus.ThemKhachHang(khachHang);
            if (HienThiLoiNghiepVu(loi)) return;

            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã thêm khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc)
        {
            HienThiLoiDuLieu(loiGoc);
        }
    }

    private void NhapExcel_Click(object? nguoiGui, EventArgs suKien)
    {
        if (!CoTheRoiTrang()) return;
        using var hopThoai = new OpenFileDialog { Filter = "Excel 97–2003 (*.xls)|*.xls", CheckFileExists = true };
        if (hopThoai.ShowDialog(this) != DialogResult.OK) return;
        try
        {
            List<KhachHangDTO> cacKhach = XuatNhapDuLieuBUS.DocKhachHangTuExcel(hopThoai.FileName);
            if (MessageBox.Show(this, $"Đã đọc và kiểm tra {cacKhach.Count} khách hàng. Nhập tất cả trong một giao dịch?\nChỉ thêm mới; không ghi đè khách hàng hay chứng từ hiện có.", "Xác nhận nhập", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return;
            int soDong = bus.NhapKhachHang(cacKhach);
            duLieuBanDau = ChupDuLieu(); TaiDanhSach();
            thongBao.Text = $"Đã nhập {soDong} khách hàng.";
        }
        catch (Exception loi) { HienThiLoiDuLieu(loi); }
    }

    private void MauExcel_Click(object? nguoiGui, EventArgs suKien)
    {
        using var hopThoai = new SaveFileDialog { Filter = "Excel 97–2003 (*.xls)|*.xls", FileName = "MauKhachHang.xls" };
        if (hopThoai.ShowDialog(this) != DialogResult.OK) return;
        try { XuatNhapDuLieuBUS.TaoMauKhachHang(hopThoai.FileName); thongBao.Text = "Đã tạo mẫu 6 cột. Ngày sinh dd/MM/yyyy; điện thoại/giấy tờ giữ dạng text."; }
        catch (Exception loi) { HienThiLoiDuLieu(loi); }
    }

    private void SuaKhachHang_Click(object? nguoiGui, EventArgs suKien)
    {
        if (!DocMaDaChon(out int maKhachHang))
        {
            MessageBox.Show("Hãy chọn khách hàng cần cập nhật trong danh sách.", "Chưa chọn khách hàng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            KhachHangDTO khachHang = DocDuLieuNhap();
            khachHang.MaKhachHang = maKhachHang;
            string? loi = bus.SuaKhachHang(khachHang);
            if (HienThiLoiNghiepVu(loi)) return;

            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã cập nhật khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc)
        {
            HienThiLoiDuLieu(loiGoc);
        }
    }

    private void XoaKhachHang_Click(object? nguoiGui, EventArgs suKien)
    {
        if (!DocMaDaChon(out int maKhachHang))
        {
            MessageBox.Show("Hãy chọn khách hàng cần xóa trong danh sách.", "Chưa chọn khách hàng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult traLoi = MessageBox.Show(
            $"Bạn có chắc muốn xóa khách hàng mã {maKhachHang} không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (traLoi != DialogResult.Yes) return;

        try
        {
            string? loi = bus.XoaKhachHang(maKhachHang);
            if (HienThiLoiNghiepVu(loi)) return;

            duLieuBanDau = ChupDuLieu();
            TaiDanhSach();
            XoaDuLieuNhap(false);
            MessageBox.Show("Đã xóa khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception loiGoc)
        {
            HienThiLoiDuLieu(loiGoc);
        }
    }

    private KhachHangDTO DocDuLieuNhap() => new()
    {
        HoTen = oHoTen.Text,
        DienThoai = oDienThoai.Text,
        Email = oEmail.Text,
        NgaySinh = oNgaySinh.Value.Date,
        GioiTinh = oGioiTinh.Text,
        SoGiayTo = oGiayTo.Text
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
        if (dong.Tag is not KhachHangDTO khachHang) return;
        dongDangChon = dong;

        oMa.Text = khachHang.MaKhachHang.ToString();
        oHoTen.Text = khachHang.HoTen;
        oDienThoai.Text = khachHang.DienThoai;
        oEmail.Text = khachHang.Email;
        oNgaySinh.Value = khachHang.NgaySinh;
        oGioiTinh.Text = khachHang.GioiTinh == "Nu" ? "Nữ" : khachHang.GioiTinh;
        oGiayTo.Text = khachHang.SoGiayTo ?? string.Empty;
        duLieuBanDau = ChupDuLieu();
    }

    private bool DocMaDaChon(out int maKhachHang) =>
        int.TryParse(oMa.Text, out maKhachHang) && maKhachHang > 0;

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
        oHoTen.Clear();
        oDienThoai.Clear();
        oEmail.Clear();
        oGiayTo.Clear();
        oNgaySinh.Value = DateTime.Today;
        oGioiTinh.Text = "Nam";
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
        thongBao.Text = "Không tải/thao tác được dữ liệu khách hàng.";
        string chiTiet = loiGoc switch
        {
            SqlException { Number: 547 } => "Dữ liệu đang được tham chiếu hoặc vi phạm ràng buộc. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 2601 or 2627 } => "Dữ liệu bị trùng. Hãy tải lại và kiểm tra thông tin.",
            SqlException { Number: 1205 } => "Dữ liệu đang được xử lý đồng thời. Hãy tải lại trước khi thử lại.",
            _ => loiGoc.Message
        };
        MessageBox.Show(
            $"Không thể hoàn thành thao tác với dữ liệu khách hàng.\nChi tiết: {chiTiet}",
            "Lỗi",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void ThemTruong(
        TableLayoutPanel bang,
        int dong,
        int cotNhan,
        string nhan,
        Control oNhap,
        int soCot = 1)
    {
        var nhanHienThi = new Label
        {
            AutoSize = true,
            Text = nhan,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 3)
        };

        oNhap.Dock = DockStyle.Fill;
        oNhap.Margin = new Padding(3, 3, 12, 8);
        bang.Controls.Add(nhanHienThi, cotNhan, dong);
        bang.Controls.Add(oNhap, cotNhan + 1, dong);

        if (soCot > 1)
        {
            bang.SetColumnSpan(oNhap, soCot);
        }
    }

    private static TextBox TaoONhap(bool chiDoc = false) => new()
    {
        ReadOnly = chiDoc,
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static Button TaoNut(string chuoi) => new()
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

    private void HienThiDanhSach(IEnumerable nguon)
    {
        dangGanDuLieu = true;
        danhSach.BeginUpdate();
        try
        {
            danhSach.Items.Clear();
            dongDangChon = null;
            foreach (KhachHangDTO khachHang in nguon)
            {
                ListViewItem dong = new ListViewItem(khachHang.MaKhachHang.ToString());
                dong.SubItems.Add(khachHang.HoTen);
                dong.SubItems.Add(khachHang.DienThoai);
                dong.SubItems.Add(khachHang.Email);
                dong.SubItems.Add(khachHang.NgaySinh.ToString("dd/MM/yyyy"));
                dong.SubItems.Add(khachHang.GioiTinh);
                dong.SubItems.Add(khachHang.SoGiayTo ?? "");
                dong.Tag = khachHang;
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
