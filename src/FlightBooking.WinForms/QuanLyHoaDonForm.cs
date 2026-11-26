using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

/// <summary>
/// Màn hình quản lý hóa đơn theo kiến trúc Master - Detail (Cha - Con) bằng 2 ListView.
/// ListView trên: Danh sách hóa đơn (Master).
/// ListView dưới: Danh sách chi tiết vé thuộc hóa đơn được chọn (Detail).
/// </summary>
public sealed class QuanLyHoaDonForm : Form
{
    private readonly HoaDonBUS bus = new();
    private ArrayList danhSachGoc = new();
    private HoaDonDTO? hoaDonDangChon;

    // Vùng tìm kiếm
    private readonly TextBox oTimKiem = TaoONhap();
    private readonly ComboBox cboTrangThai = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };
    private readonly Button nutTimKiem = TaoNut("Tìm kiếm");
    private readonly Button nutTaiLai = TaoNut("Tải lại");

    // Master: Danh sách hóa đơn
    private readonly ListView danhSachHoaDon = GiaoDien.TaoDanhSach();

    // Detail: Danh sách chi tiết vé
    private readonly ListView danhSachChiTiet = GiaoDien.TaoDanhSach();

    // Các nhãn tóm tắt thông tin hóa đơn đang chọn
    private readonly Label lblThongTinChon = new()
    {
        AutoSize = true,
        Font = new Font("Segoe UI", 10F, FontStyle.Bold),
        ForeColor = GiaoDien.MauChinh,
        Text = "Chưa chọn hóa đơn nào."
    };

    private readonly Label thongBao = new()
    {
        AutoSize = true,
        Text = "Sẵn sàng."
    };

    // Các nút thao tác nghiệp vụ
    private readonly Button nutThanhToan = TaoNut("Xác nhận thanh toán (PAID)", chinh: true);
    private readonly Button nutHuyHoaDon = TaoNut("Hủy hóa đơn", canhBao: true);

    public QuanLyHoaDonForm()
    {
        Text = "Quản lý hóa đơn & Thanh toán";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1050, 700);
        Size = new Size(1250, 820);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;

        cboTrangThai.Items.AddRange(["Tất cả trạng thái", "UNPAID - Chưa thanh toán", "PAID - Đã thanh toán", "CANCELLED - Đã hủy"]);
        cboTrangThai.SelectedIndex = 0;

        TaoCotDanhSach();
        TaoBoCuc();
        GanSuKien();

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
            RowCount = 8
        };
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        // Rows
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Tiêu đề
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Vùng tìm kiếm
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 2: Label danh sách hóa đơn + nút
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 55)); // 3: ListView Master (Hóa đơn)
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 4: Vùng thao tác & nhãn chi tiết
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 45)); // 5: ListView Detail (Chi tiết vé)
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 6: Thông báo trạng thái

        var tieuDe = new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ HÓA ĐƠN & THANH TOÁN (MASTER - DETAIL)",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 12)
        };
        khungChinh.Controls.Add(tieuDe, 0, 0);
        khungChinh.Controls.Add(TaoVungTimKiem(), 0, 1);

        var thanhMaster = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 4, 0, 6)
        };
        thanhMaster.Controls.Add(new Label
        {
            Text = "DANH SÁCH HÓA ĐƠN (Bấm vào dòng để xem chi tiết các vé bên dưới):",
            AutoSize = true,
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(0, 4, 0, 0)
        });
        khungChinh.Controls.Add(thanhMaster, 0, 2);
        khungChinh.Controls.Add(danhSachHoaDon, 0, 3);

        var vungThaoTacDetail = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 8, 0, 4)
        };
        vungThaoTacDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));
        vungThaoTacDetail.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));

        vungThaoTacDetail.Controls.Add(lblThongTinChon, 0, 0);

        var vungNut = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Margin = Padding.Empty
        };
        vungNut.Controls.Add(nutHuyHoaDon);
        vungNut.Controls.Add(nutThanhToan);
        vungThaoTacDetail.Controls.Add(vungNut, 1, 0);

        khungChinh.Controls.Add(vungThaoTacDetail, 0, 4);
        khungChinh.Controls.Add(danhSachChiTiet, 0, 5);
        khungChinh.Controls.Add(thongBao, 0, 6);

        Controls.Add(khungChinh);
    }

    private Control TaoVungTimKiem()
    {
        var vung = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = true,
            Margin = new Padding(0, 0, 0, 10)
        };

        vung.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Từ khóa:",
            Margin = new Padding(3, 9, 8, 3)
        });

        oTimKiem.Width = 320;
        oTimKiem.PlaceholderText = "Mã hóa đơn, mã đặt vé, khách hàng, SĐT...";
        vung.Controls.Add(oTimKiem);

        vung.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Trạng thái:",
            Margin = new Padding(12, 9, 8, 3)
        });
        cboTrangThai.Width = 220;
        vung.Controls.Add(cboTrangThai);

        vung.Controls.Add(nutTimKiem);
        vung.Controls.Add(nutTaiLai);
        return vung;
    }

    private void TaoCotDanhSach()
    {
        // 1. Cột Master (Hóa đơn)
        ThemCot(danhSachHoaDon, "MaHoaDon", "Mã HĐ", 8);
        ThemCot(danhSachHoaDon, "MaDatVe", "Mã Đặt Vé", 10);
        ThemCot(danhSachHoaDon, "KhachHang", "Khách hàng", 17);
        ThemCot(danhSachHoaDon, "DienThoai", "Điện thoại", 12);
        ThemCot(danhSachHoaDon, "TamTinh", "Tạm tính (VNĐ)", 12);
        ThemCot(danhSachHoaDon, "TienGiam", "Giảm giá", 10);
        ThemCot(danhSachHoaDon, "TongTien", "Tổng cộng (VNĐ)", 13);
        ThemCot(danhSachHoaDon, "TrangThai", "Trạng thái", 12);
        ThemCot(danhSachHoaDon, "NgayHoaDon", "Ngày lập HĐ", 14);

        // 2. Cột Detail (Chi tiết vé trong hóa đơn)
        ThemCot(danhSachChiTiet, "MaCT", "Mã CTHĐ", 8);
        ThemCot(danhSachChiTiet, "MaVe", "Mã vé", 8);
        ThemCot(danhSachChiTiet, "SoGhe", "Ghế", 8);
        ThemCot(danhSachChiTiet, "HangVe", "Hạng vé", 14);
        ThemCot(danhSachChiTiet, "HanhKhach", "Hành khách", 20);
        ThemCot(danhSachChiTiet, "CCCD", "Số CCCD/Hộ chiếu", 16);
        ThemCot(danhSachChiTiet, "DonGia", "Đơn giá (VNĐ)", 13);
        ThemCot(danhSachChiTiet, "ThanhTien", "Thành tiền (VNĐ)", 13);
    }

    private void ThemCot(ListView lv, string key, string ten, int phanTram)
    {
        var cot = lv.Columns.Add(key, ten);
        cot.Width = Math.Max(70, (Width * phanTram) / 100);
    }

    private void GanSuKien()
    {
        nutTimKiem.Click += (_, _) => ThucHienTimKiem();
        oTimKiem.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ThucHienTimKiem(); } };
        cboTrangThai.SelectedIndexChanged += (_, _) => ThucHienTimKiem();
        nutTaiLai.Click += (_, _) => TaiDanhSach();

        danhSachHoaDon.SelectedIndexChanged += (_, _) =>
        {
            if (danhSachHoaDon.SelectedItems.Count > 0 && danhSachHoaDon.SelectedItems[0].Tag is HoaDonDTO hd)
                ChonHoaDon(hd);
            else
                BoChonHoaDon();
        };

        nutThanhToan.Click += (_, _) => ThucHienThanhToan();
        nutHuyHoaDon.Click += (_, _) => ThucHienHuyHoaDon();
    }

    private void TaiDanhSach()
    {
        try
        {
            thongBao.ForeColor = GiaoDien.MauChuPhu;
            thongBao.Text = "Đang tải danh sách hóa đơn...";
            danhSachGoc = bus.DocDSHoaDon();
            HienThiDanhSachHoaDon(danhSachGoc);
            thongBao.Text = $"Đã tải {danhSachGoc.Count} hóa đơn.";
        }
        catch (Exception ex)
        {
            thongBao.ForeColor = GiaoDien.MauLoi;
            thongBao.Text = $"Lỗi tải dữ liệu: {ex.Message}";
        }
    }

    private void ThucHienTimKiem()
    {
        string tuKhoa = oTimKiem.Text.Trim();
        string trangThai = cboTrangThai.SelectedIndex switch
        {
            1 => "UNPAID",
            2 => "PAID",
            3 => "CANCELLED",
            _ => "TAT_CA"
        };

        var ketQua = bus.TimKiemHoaDon(danhSachGoc, tuKhoa, trangThai);
        HienThiDanhSachHoaDon(ketQua);
        thongBao.Text = $"Tìm thấy {ketQua.Count} hóa đơn phù hợp.";
    }

    private void HienThiDanhSachHoaDon(IEnumerable nguon)
    {
        danhSachHoaDon.BeginUpdate();
        try
        {
            danhSachHoaDon.Items.Clear();
            foreach (object item in nguon)
            {
                if (item is not HoaDonDTO hd) continue;

                var dong = new ListViewItem(hd.MaHoaDon.ToString());
                dong.SubItems.Add(hd.MaDatVe.ToString());
                dong.SubItems.Add(hd.TenKhachHang);
                dong.SubItems.Add(hd.DienThoaiKhachHang);
                dong.SubItems.Add(hd.TongTienGoc.ToString("N0"));
                dong.SubItems.Add(hd.TienGiam.ToString("N0"));
                dong.SubItems.Add(hd.TongTien.ToString("N0"));

                string tt = hd.TrangThai switch
                {
                    "PAID" => "Đã thanh toán",
                    "UNPAID" => "Chờ thanh toán",
                    "CANCELLED" => "Đã hủy",
                    _ => hd.TrangThai
                };
                dong.SubItems.Add(tt);
                dong.SubItems.Add(hd.NgayHoaDon.ToString("dd/MM/yyyy HH:mm"));

                if (hd.TrangThai == "PAID")
                    dong.ForeColor = Color.DarkGreen;
                else if (hd.TrangThai == "CANCELLED")
                    dong.ForeColor = Color.Gray;

                dong.Tag = hd;
                danhSachHoaDon.Items.Add(dong);
            }
        }
        finally
        {
            danhSachHoaDon.EndUpdate();
        }
        BoChonHoaDon();
    }

    private void ChonHoaDon(HoaDonDTO hd)
    {
        hoaDonDangChon = hd;
        string tt = hd.TrangThai switch
        {
            "PAID" => "ĐÃ THANH TOÁN",
            "UNPAID" => "CHỜ THANH TOÁN (UNPAID)",
            "CANCELLED" => "ĐÃ HỦY",
            _ => hd.TrangThai
        };

        lblThongTinChon.Text = $"Hóa đơn #{hd.MaHoaDon} · Khách: {hd.TenKhachHang} ({hd.DienThoaiKhachHang}) · Tổng tiền: {hd.TongTien:N0} VNĐ · [{tt}]";
        lblThongTinChon.ForeColor = hd.TrangThai == "PAID" ? Color.DarkGreen : hd.TrangThai == "UNPAID" ? GiaoDien.MauChinh : GiaoDien.MauLoi;

        nutThanhToan.Enabled = hd.TrangThai == "UNPAID";
        nutHuyHoaDon.Enabled = hd.TrangThai != "CANCELLED";

        TaiChiTietHoaDon(hd.MaHoaDon);
    }

    private void BoChonHoaDon()
    {
        hoaDonDangChon = null;
        lblThongTinChon.Text = "Chưa chọn hóa đơn nào. (Chọn một hóa đơn ở danh sách trên để xem chi tiết vé)";
        lblThongTinChon.ForeColor = GiaoDien.MauChuPhu;
        nutThanhToan.Enabled = false;
        nutHuyHoaDon.Enabled = false;

        danhSachChiTiet.BeginUpdate();
        danhSachChiTiet.Items.Clear();
        danhSachChiTiet.EndUpdate();
    }

    private void TaiChiTietHoaDon(int maHoaDon)
    {
        try
        {
            var dsChiTiet = bus.DocChiTietHoaDon(maHoaDon);
            danhSachChiTiet.BeginUpdate();
            danhSachChiTiet.Items.Clear();

            foreach (object item in dsChiTiet)
            {
                if (item is not ChiTietHoaDonDTO ct) continue;

                var dong = new ListViewItem(ct.MaChiTietHoaDon.ToString());
                dong.SubItems.Add(ct.MaVe.ToString());
                dong.SubItems.Add(ct.SoGhe);
                dong.SubItems.Add(ct.TenHangVe);
                dong.SubItems.Add(ct.TenHanhKhach);
                dong.SubItems.Add(ct.SoGiayTo);
                dong.SubItems.Add(ct.DonGia.ToString("N0"));
                dong.SubItems.Add(ct.ThanhTien.ToString("N0"));

                dong.Tag = ct;
                danhSachChiTiet.Items.Add(dong);
            }
        }
        catch (Exception ex)
        {
            thongBao.ForeColor = GiaoDien.MauLoi;
            thongBao.Text = $"Lỗi tải chi tiết hóa đơn #{maHoaDon}: {ex.Message}";
        }
        finally
        {
            danhSachChiTiet.EndUpdate();
        }
    }

    private void ThucHienThanhToan()
    {
        if (hoaDonDangChon == null) return;

        var xacNhan = MessageBox.Show(this,
            $"Xác nhận thanh toán cho Hóa đơn #{hoaDonDangChon.MaHoaDon}?\nTổng số tiền: {hoaDonDangChon.TongTien:N0} VNĐ\nTrạng thái sẽ chuyển sang PAID và doanh thu được ghi nhận.",
            "Xác nhận thanh toán", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

        if (xacNhan != DialogResult.Yes) return;

        try
        {
            string? loi = bus.ThanhToanHoaDon(hoaDonDangChon.MaHoaDon);
            if (loi != null)
            {
                MessageBox.Show(this, loi, "Lỗi thanh toán", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, $"Thanh toán thành công cho hóa đơn #{hoaDonDangChon.MaHoaDon}!", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TaiDanhSach();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi thanh toán: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ThucHienHuyHoaDon()
    {
        if (hoaDonDangChon == null) return;

        var xacNhan = MessageBox.Show(this,
            $"Bạn có chắc chắn muốn hủy Hóa đơn #{hoaDonDangChon.MaHoaDon} (Mã đặt vé: {hoaDonDangChon.MaDatVe})?\nToàn bộ vé kèm theo sẽ bị hủy và hoàn trả ghế.",
            "Xác nhận hủy hóa đơn", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (xacNhan != DialogResult.Yes) return;

        try
        {
            string? loi = bus.HuyHoaDon(hoaDonDangChon.MaHoaDon);
            if (loi != null)
            {
                MessageBox.Show(this, loi, "Lỗi hủy hóa đơn", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, $"Đã hủy thành công hóa đơn #{hoaDonDangChon.MaHoaDon}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TaiDanhSach();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi hủy: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static TextBox TaoONhap() => new()
    {
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static Button TaoNut(string chuoi, bool chinh = false, bool canhBao = false) => new()
    {
        Text = chuoi,
        AutoSize = true,
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = chinh ? GiaoDien.MauChinh : Color.White,
        ForeColor = chinh ? Color.White : canhBao ? GiaoDien.MauLoi : GiaoDien.MauChu,
        Margin = new Padding(3, 3, 8, 3),
        Padding = new Padding(12, 2, 12, 2)
    };

    public bool CoTheRoiTrang() => true;
}
