namespace FlightBooking.WinForms;

/// <summary>Một màn hình chính, các chức năng dùng chung vùng nội dung.</summary>
public sealed class MainForm : Form
{
    private readonly Panel vungNoiDung = new() { Dock = DockStyle.Fill, Margin = Padding.Empty };
    private readonly Dictionary<string, Button> cacNut = new();
    private Form? moduleDangMo;
    private string mucDangChon = "";

    public MainForm()
    {
        Text = "FlightBooking · Quản trị";
        StartPosition = FormStartPosition.Manual;
        var vungLamViec = Screen.FromPoint(Cursor.Position).WorkingArea;
        Bounds = GiaoDien.KichThuocCuaSo(vungLamViec, new Size(1450, 920));
        MinimumSize = new Size(Math.Min(1040, vungLamViec.Width - 24), Math.Min(640, vungLamViec.Height - 24));
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;
        TaoBoCuc();
        HienThiTongQuan();
        FormClosing += (_, suKien) => suKien.Cancel = !CoTheRoiModule();
    }

    private void TaoBoCuc()
    {
        var khungChinh = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = Padding.Empty
        };
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 200));
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khungChinh.Controls.Add(TaoThanhDieuHuong(), 0, 0);
        khungChinh.Controls.Add(vungNoiDung, 1, 0);
        Controls.Add(khungChinh);
    }

    private Control TaoThanhDieuHuong()
    {
        var thanhBen = new Panel
        {
            Dock = DockStyle.Fill,
            AutoScroll = true,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(30, 41, 59)
        };
        var danhSachNut = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };
        danhSachNut.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "FLIGHTBOOKING",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Margin = new Padding(4, 10, 4, 14)
        });
        ThemNutDieuHuong(danhSachNut, "Tổng quan", HienThiTongQuan);
        ThemNutDieuHuong(danhSachNut, "Sân bay", () => MoQuanLyChuyenBay(LoaiDanhMuc.SanBay, "Sân bay"));
        ThemNutDieuHuong(danhSachNut, "Chuyến bay", () => MoQuanLyChuyenBay(LoaiDanhMuc.ChuyenBay, "Chuyến bay"));
        ThemNutDieuHuong(danhSachNut, "Hạng vé", () => MoQuanLyChuyenBay(LoaiDanhMuc.HangVe, "Hạng vé"));
        ThemNutDieuHuong(danhSachNut, "Khách hàng", () => MoModule("Khách hàng", () => new QuanLyKhachHangForm()));
        ThemNutDieuHuong(danhSachNut, "Nhân viên", () => MoModule("Nhân viên", () => new QuanLyNhanVienForm()));
        ThemNutDieuHuong(danhSachNut, "Mã giảm giá", () => MoModule("Mã giảm giá", () => new QuanLyMaGiamGiaForm()));
        ThemNutDieuHuong(danhSachNut, "Vé máy bay", () => MoModule("Vé máy bay", () => new QuanLyVeForm()));
        ThemNutDieuHuong(danhSachNut, "Hóa đơn", () => MoModule("Hóa đơn", () => new QuanLyHoaDonForm()));
        ThemNutDieuHuong(danhSachNut, "Thống kê", () => MoModule("Thống kê", () => new ThongKeDoanhThuForm()));
        ThemNutDieuHuong(danhSachNut, "Giới thiệu", () => MessageBox.Show(this,
            "Ứng dụng quản lý hệ thống đặt vé máy bay.", "Giới thiệu", MessageBoxButtons.OK, MessageBoxIcon.Information));
        ThemNutDieuHuong(danhSachNut, "Thoát", Close);
        thanhBen.Controls.Add(danhSachNut);
        return thanhBen;
    }

    private void ThemNutDieuHuong(FlowLayoutPanel danhSach, string ten, Action xuLy)
    {
        var nut = new Button
        {
            Text = ten,
            Width = 168,
            Height = 38,
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(30, 41, 59),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 6),
            Padding = new Padding(10, 0, 4, 0)
        };
        nut.FlatAppearance.BorderSize = 0;
        nut.FlatAppearance.MouseOverBackColor = Color.FromArgb(29, 78, 216);
        nut.Click += (_, _) => xuLy();
        cacNut.Add(ten, nut);
        danhSach.Controls.Add(nut);
    }

    private bool CoTheRoiModule() => moduleDangMo switch
    {
        Form1 quanLy => quanLy.CoTheRoiTrang(),
        QuanLyKhachHangForm khachHang => khachHang.CoTheRoiTrang(),
        QuanLyNhanVienForm nhanVien => nhanVien.CoTheRoiTrang(),
        QuanLyMaGiamGiaForm maGiamGia => maGiamGia.CoTheRoiTrang(),
        QuanLyVeForm ve => ve.CoTheRoiTrang(),
        QuanLyHoaDonForm hoaDon => hoaDon.CoTheRoiTrang(),
        ThongKeDoanhThuForm thongKe => thongKe.CoTheRoiTrang(),
        _ => true
    };

    private void MoQuanLyChuyenBay(LoaiDanhMuc loai, string ten)
    {
        if (moduleDangMo is Form1 quanLy)
        {
            if (quanLy.ChonTrang(loai)) DanhDauMuc(ten);
            return;
        }
        if (!CoTheRoiModule()) return;
        var manHinh = new Form1();
        manHinh.TrangDaChon += trang => DanhDauMuc(trang switch
        {
            LoaiDanhMuc.SanBay => "Sân bay",
            LoaiDanhMuc.ChuyenBay => "Chuyến bay",
            _ => "Hạng vé"
        });
        // Chọn tab khi form chưa hiện để việc tải tab đầu không chặn điều hướng.
        manHinh.ChonTrang(loai);
        HienThiModule(manHinh);
        DanhDauMuc(ten);
    }

    private void MoModule(string ten, Func<Form> taoModule)
    {
        if (mucDangChon == ten || !CoTheRoiModule()) return;
        HienThiModule(taoModule());
        DanhDauMuc(ten);
    }

    private void HienThiModule(Form module)
    {
        DongModule();
        moduleDangMo = module;
        module.TopLevel = false;
        module.FormBorderStyle = FormBorderStyle.None;
        module.MinimumSize = Size.Empty;
        module.Dock = DockStyle.Fill;
        vungNoiDung.Controls.Add(module);
        module.Show();
    }

    private void HienThiTongQuan()
    {
        if (mucDangChon == "Tổng quan" || !CoTheRoiModule()) return;
        DongModule();
        vungNoiDung.Controls.Add(new Label
        {
            Dock = DockStyle.Fill,
            Text = "HỆ THỐNG QUẢN LÝ ĐẶT VÉ MÁY BAY\n\nChọn chức năng ở thanh điều hướng để bắt đầu.",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16F),
            ForeColor = GiaoDien.MauChu
        });
        DanhDauMuc("Tổng quan");
    }

    private void DongModule()
    {
        // Mỗi đường gọi đã kiểm tra dữ liệu chưa lưu trước khi giải phóng giao diện cũ.
        foreach (Control dieuKhien in vungNoiDung.Controls.Cast<Control>().ToArray())
        {
            vungNoiDung.Controls.Remove(dieuKhien);
            dieuKhien.Dispose();
        }
        moduleDangMo = null;
    }

    private void DanhDauMuc(string ten)
    {
        mucDangChon = ten;
        foreach (var (nhan, nut) in cacNut)
            nut.BackColor = nhan == ten ? GiaoDien.MauChinh : Color.FromArgb(30, 41, 59);
    }
}
