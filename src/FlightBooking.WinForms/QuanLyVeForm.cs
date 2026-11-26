using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

/// <summary>
/// Màn hình quản lý vé máy bay.
/// Form hiển thị danh sách vé, tìm kiếm và hỗ trợ hủy vé an toàn.
/// </summary>
public sealed class QuanLyVeForm : Form
{
    private readonly VeMayBayBUS bus = new();
    private ArrayList danhSachGoc = new();
    private readonly BoLocNangCao locNangCao = new(["Mã vé|MaVe", "Mã Booking|MaDatVe", "Hành khách|TenHanhKhach", "Giấy tờ|SoGiayTo", "Chuyến bay|MaChuyenBay", "Hạng vé|TenHangVe", "Ghế|SoGhe", "Ngày bay|NgayKhoiHanh", "Giá vé|GiaVe", "Trạng thái vé|TrangThai"]);

    // Các ô hiển thị thông tin chi tiết vé được chọn
    private readonly TextBox oMaVe = TaoONhap(chiDoc: true);
    private readonly TextBox oMaChuyenBay = TaoONhap(chiDoc: true);
    private readonly TextBox oTuyenBay = TaoONhap(chiDoc: true);
    private readonly TextBox oSoGhe = TaoONhap(chiDoc: true);
    private readonly TextBox oHangVe = TaoONhap(chiDoc: true);
    private readonly TextBox oGiaVe = TaoONhap(chiDoc: true);
    private readonly TextBox oHanhKhach = TaoONhap(chiDoc: true);
    private readonly TextBox oSoGiayTo = TaoONhap(chiDoc: true);
    private readonly TextBox oTrangThai = TaoONhap(chiDoc: true);
    private readonly TextBox oNgayPhatHanh = TaoONhap(chiDoc: true);

    // Bộ lọc và tìm kiếm
    private readonly TextBox oTimKiem = TaoONhap();
    private readonly ComboBox cboTrangThai = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };

    private readonly ListView danhSach = GiaoDien.TaoDanhSach();
    private readonly Label thongBao = new()
    {
        AutoSize = true,
        Text = "Sẵn sàng."
    };

    private readonly Button nutTimKiem = TaoNut("Tìm kiếm");
    private readonly Button nutTaiLai = TaoNut("Tải lại");
    private readonly Button nutXuatExcel = TaoNut("Xuất Excel");
    private readonly Button nutHuyVe = TaoNut("Hủy đơn chứa vé", canhBao: true);
    private readonly Button nutLamTrong = TaoNut("Bỏ chọn");

    public QuanLyVeForm()
    {
        Text = "Quản lý vé máy bay";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 800);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;

        cboTrangThai.Items.AddRange(["Tất cả trạng thái", "ISSUED - Có hiệu lực", "CANCELLED - Đã hủy"]);
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
            Text = "QUẢN LÝ VÉ MÁY BAY",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 14)
        };
        khungChinh.Controls.Add(tieuDe, 0, 0);
        var vungTim = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2 };
        vungTim.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        vungTim.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        vungTim.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        vungTim.Controls.Add(TaoVungTimKiem(), 0, 0);
        var nutNangCao = new CheckBox { Text = "Tìm kiếm nâng cao", AutoSize = true };
        var khungNangCao = new TableLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, ColumnCount = 1, RowCount = 2 };
        khungNangCao.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        locNangCao.Visible = false;
        nutNangCao.CheckedChanged += (_, _) => locNangCao.Visible = nutNangCao.Checked;
        khungNangCao.Controls.AddRange([nutNangCao, locNangCao]);
        vungTim.Controls.Add(khungNangCao, 0, 1);
        khungChinh.Controls.Add(vungTim, 0, 1);

        var vungChiTiet = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Margin = Padding.Empty };
        vungChiTiet.Controls.Add(TaoTruongChiTiet());
        khungChinh.Controls.Add(vungChiTiet, 0, 2);

        khungChinh.RowStyles[2] = new RowStyle(SizeType.Percent, 40);
        khungChinh.RowStyles[5] = new RowStyle(SizeType.Percent, 60);

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

        oTimKiem.Width = 320;
        oTimKiem.PlaceholderText = "Mã vé, ghế, hành khách, CCCD, tuyến bay...";
        vung.Controls.Add(oTimKiem);

        vung.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Trạng thái:",
            Margin = new Padding(12, 9, 8, 3)
        });
        cboTrangThai.Width = 200;
        vung.Controls.Add(cboTrangThai);

        vung.Controls.Add(GiaoDien.TaoNut("Xóa tìm kiếm", () => { oTimKiem.Clear(); cboTrangThai.SelectedIndex=0;locNangCao.XoaDieuKien();ThucHienTimKiem();return Task.CompletedTask; }));
        vung.Controls.Add(nutTimKiem);
        vung.Controls.Add(nutTaiLai);
        vung.Controls.Add(nutXuatExcel);
        return vung;
    }

    private Control TaoTruongChiTiet()
    {
        var cacTruong = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 5,
            Padding = new Padding(0, 0, 0, 8),
            Margin = new Padding(0)
        };

        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 14));
        cacTruong.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 36));

        for (int dong = 0; dong < 5; dong++)
            cacTruong.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        ThemTruong(cacTruong, 0, 0, "Mã vé:", oMaVe);
        ThemTruong(cacTruong, 0, 2, "Trạng thái:", oTrangThai);
        ThemTruong(cacTruong, 1, 0, "Chuyến bay:", oMaChuyenBay);
        ThemTruong(cacTruong, 1, 2, "Tuyến bay:", oTuyenBay);
        ThemTruong(cacTruong, 2, 0, "Số ghế:", oSoGhe);
        ThemTruong(cacTruong, 2, 2, "Hạng vé:", oHangVe);
        ThemTruong(cacTruong, 3, 0, "Giá vé:", oGiaVe);
        ThemTruong(cacTruong, 3, 2, "Ngày phát hành:", oNgayPhatHanh);
        ThemTruong(cacTruong, 4, 0, "Hành khách:", oHanhKhach);
        ThemTruong(cacTruong, 4, 2, "CCCD/Hộ chiếu:", oSoGiayTo);

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

        vung.Controls.Add(GiaoDien.TaoNut("Đặt vé tại quầy", () =>
        {
            using var form = new DatVeTaiQuayForm();
            if (form.ShowDialog(this) == DialogResult.OK) TaiDanhSach();
            return Task.CompletedTask;
        }, true));
        vung.Controls.Add(GiaoDien.TaoNut("Sửa hành khách", () => { SuaHanhKhach(); return Task.CompletedTask; }));
        vung.Controls.Add(GiaoDien.TaoNut("Đổi ghế", () =>
        {
            if(danhSach.SelectedItems.Count==0 || danhSach.SelectedItems[0].Tag is not VeMayBayDTO ve)return Task.CompletedTask;
            using var f=new Form{Text="Đổi ghế cùng chuyến",Size=new Size(420,210),StartPosition=FormStartPosition.CenterParent};
            var chon=new ComboBox{DropDownStyle=ComboBoxStyle.DropDownList,Dock=DockStyle.Top};
            try{chon.Items.AddRange(new DatVeBUS().DocGheTrong(ve.MaChuyenBay,1).ToArray());}catch(Exception e){MessageBox.Show(this,e.Message);return Task.CompletedTask;}
            var luu=GiaoDien.TaoNut("Lưu đổi ghế",()=>{try{new DatVeBUS().DoiGheVe(ve.MaVe,ve.SoGhe,chon.Text);f.DialogResult=DialogResult.OK;}catch(Exception e){MessageBox.Show(f,e.Message);}return Task.CompletedTask;},true);
            luu.Dock=DockStyle.Bottom;f.Controls.Add(chon);f.Controls.Add(luu);
            if(f.ShowDialog(this)==DialogResult.OK)TaiDanhSach();return Task.CompletedTask;
        }));
        vung.Controls.Add(nutHuyVe);
        vung.Controls.Add(nutLamTrong);
        return vung;
    }

    private void TaoCotDanhSach()
    {
        ThemCot("MaVe", "Mã vé", 8);
        ThemCot("ChuyenBay", "Mã chuyến", 9);
        ThemCot("TuyenBay", "Tuyến bay", 18);
        ThemCot("SoGhe", "Ghế", 7);
        ThemCot("HangVe", "Hạng vé", 12);
        ThemCot("GiaVe", "Giá vé (VNĐ)", 12);
        ThemCot("HanhKhach", "Hành khách", 16);
        ThemCot("TrangThai", "Trạng thái", 10);
        ThemCot("NgayPhatHanh", "Ngày phát hành", 12);
    }

    private void ThemCot(string key, string ten, int phanTram)
    {
        var cot = danhSach.Columns.Add(key, ten);
        cot.Width = Math.Max(70, (Width * phanTram) / 100);
    }

    private void GanSuKien()
    {
        locNangCao.YeuCauTimKiem += (_,_) => ThucHienTimKiem();
        nutTimKiem.Click += (_, _) => ThucHienTimKiem();
        oTimKiem.KeyDown += (_, e) => { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ThucHienTimKiem(); } };
        cboTrangThai.SelectedIndexChanged += (_, _) => ThucHienTimKiem();
        nutTaiLai.Click += (_, _) => TaiDanhSach();
        nutLamTrong.Click += (_, _) => XoaChiTiet();

        danhSach.SelectedIndexChanged += (_, _) =>
        {
            if (danhSach.SelectedItems.Count > 0 && danhSach.SelectedItems[0].Tag is VeMayBayDTO ve)
                HienThiChiTiet(ve);
            else
                XoaChiTiet();
        };

        nutHuyVe.Click += (_, _) => ThucHienHuyDonChuaVe();
        nutXuatExcel.Click += (_, _) => ThucHienXuatExcel();
    }

    private void TaiDanhSach()
    {
        try
        {
            thongBao.ForeColor = GiaoDien.MauChuPhu;
            thongBao.Text = "Đang tải dữ liệu...";
            danhSachGoc = bus.DocDSVe();
            HienThiDanhSach(danhSachGoc);
            thongBao.Text = $"Đã tải {danhSachGoc.Count} vé máy bay.";
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
            1 => "ISSUED",
            2 => "CANCELLED",
            _ => "TAT_CA"
        };

        try
        {
            var ketQua = bus.TimKiemVe(danhSachGoc, tuKhoa, trangThai);
            if (locNangCao.Visible) ketQua = bus.LocDanhSach(ketQua, locNangCao.DocDieuKien());
            HienThiDanhSach(ketQua);
            thongBao.ForeColor = GiaoDien.MauChuPhu;
            thongBao.Text = $"Tìm thấy {ketQua.Count} vé phù hợp.";
        }
        catch (ArgumentException loi)
        {
            thongBao.ForeColor = Color.Firebrick;
            thongBao.Text = loi.Message;
        }
    }

    private void HienThiDanhSach(IEnumerable nguon)
    {
        danhSach.BeginUpdate();
        try
        {
            danhSach.Items.Clear();
            foreach (object item in nguon)
            {
                if (item is not VeMayBayDTO ve) continue;

                var dong = new ListViewItem(ve.MaVe.ToString());
                dong.SubItems.Add(ve.MaChuyenBay.ToString());
                string tuyen = !string.IsNullOrEmpty(ve.SanBayDi) ? $"{ve.SanBayDi} → {ve.SanBayDen}" : $"Chuyến #{ve.MaChuyenBay}";
                dong.SubItems.Add(tuyen);
                dong.SubItems.Add(ve.SoGhe);
                dong.SubItems.Add(ve.TenHangVe);
                dong.SubItems.Add(ve.CoGiaLichSu ? ve.GiaVe.ToString("N2") : "Chưa có chứng từ");
                dong.SubItems.Add(string.IsNullOrEmpty(ve.TenHanhKhach) ? "(Chưa rõ)" : ve.TenHanhKhach);
                dong.SubItems.Add(ve.TrangThai == "ISSUED" ? "Hiệu lực" : "Đã hủy");
                dong.SubItems.Add(ve.NgayPhatHanh.ToString("dd/MM/yyyy HH:mm"));

                if (ve.TrangThai == "CANCELLED")
                    dong.ForeColor = Color.Gray;

                dong.Tag = ve;
                danhSach.Items.Add(dong);
            }
        }
        finally
        {
            danhSach.EndUpdate();
        }
        GiaoDien.CanCotDanhSach(danhSach);
        XoaChiTiet();
    }

    private void SuaHanhKhach()
    {
        if (danhSach.SelectedItems.Count == 0 || danhSach.SelectedItems[0].Tag is not VeMayBayDTO ve)
        { MessageBox.Show(this, "Chọn vé cần sửa."); return; }
        using var form = new Form { Text = "Sửa hành khách", Size = new Size(580, 330), StartPosition = FormStartPosition.CenterParent, Font = Font, MinimizeBox = false, MaximizeBox = false };
        var bang = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 6, Padding = new Padding(18) };
        var ten = new TextBox { Text = ve.TenHanhKhach, MaxLength = 100, Dock = DockStyle.Fill, AccessibleName = "Họ tên hành khách" };
        var giay = new TextBox { Text = ve.SoGiayTo, MaxLength = 50, Dock = DockStyle.Fill, AccessibleName = "CCCD hoặc hộ chiếu" };
        bang.Controls.Add(GiaoDien.TaoNhan("Chỉ sửa đơn UNPAID trước giờ bay. Khứ hồi: áp dụng cho cùng hành khách ở cả hai lượt.", 9));
        bang.Controls.Add(GiaoDien.TaoNhan("Họ tên hành khách")); bang.Controls.Add(ten);
        bang.Controls.Add(GiaoDien.TaoNhan("CCCD / hộ chiếu")); bang.Controls.Add(giay);
        var nut = GiaoDien.TaoNut("Lưu thông tin", () =>
        {
            try
            {
                bus.SuaHanhKhach(ve.MaVe, new ThongTinHanhKhach { FullName = ten.Text, IdCard = giay.Text });
                form.DialogResult = DialogResult.OK;
            }
            catch (Exception loi) { MessageBox.Show(form, loi.Message, "Chưa sửa được vé"); }
            return Task.CompletedTask;
        }, true);
        bang.Controls.Add(nut); form.Controls.Add(bang); form.AcceptButton = nut;
        if (form.ShowDialog(this) == DialogResult.OK) TaiDanhSach();
    }

    private void HienThiChiTiet(VeMayBayDTO ve)
    {
        oMaVe.Text = ve.MaVe.ToString();
        oMaChuyenBay.Text = ve.MaChuyenBay.ToString();
        oTuyenBay.Text = !string.IsNullOrEmpty(ve.SanBayDi) ? $"{ve.SanBayDi} → {ve.SanBayDen}" : $"Chuyến #{ve.MaChuyenBay}";
        oSoGhe.Text = ve.SoGhe;
        oHangVe.Text = ve.TenHangVe;
        oGiaVe.Text = ve.CoGiaLichSu ? ve.GiaVe.ToString("N2") + " VNĐ" : "Chưa có chứng từ giá lịch sử";
        oHanhKhach.Text = ve.TenHanhKhach;
        oSoGiayTo.Text = ve.SoGiayTo;
        oTrangThai.Text = ve.TrangThai == "ISSUED" ? "ISSUED (Có hiệu lực)" : "CANCELLED (Đã hủy)";
        oTrangThai.ForeColor = ve.TrangThai == "ISSUED" ? Color.Green : GiaoDien.MauLoi;
        oNgayPhatHanh.Text = ve.NgayPhatHanh.ToString("dd/MM/yyyy HH:mm:ss");

        nutHuyVe.Enabled = ve.TrangThai == "ISSUED" && ve.TrangThaiHoaDon == "UNPAID" && ve.MaDatVe.HasValue;
    }

    private void XoaChiTiet()
    {
        oMaVe.Clear();
        oMaChuyenBay.Clear();
        oTuyenBay.Clear();
        oSoGhe.Clear();
        oHangVe.Clear();
        oGiaVe.Clear();
        oHanhKhach.Clear();
        oSoGiayTo.Clear();
        oTrangThai.Clear();
        oTrangThai.ForeColor = GiaoDien.MauChu;
        oNgayPhatHanh.Clear();
        nutHuyVe.Enabled = false;
    }

    private void ThucHienHuyDonChuaVe()
    {
        if (danhSach.SelectedItems.Count == 0 || danhSach.SelectedItems[0].Tag is not VeMayBayDTO ve)
        {
            MessageBox.Show(this, "Vui lòng chọn vé cần hủy.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (ve.TrangThai == "CANCELLED")
        {
            MessageBox.Show(this, "Vé này đã bị hủy trước đó.", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var xacNhan = MessageBox.Show(this,
            $"Hủy toàn bộ đơn #{ve.MaDatVe} chứa vé #{ve.MaVe}?\nChỉ áp dụng đơn UNPAID trước giờ bay. Tất cả vé thuộc đơn sẽ bị hủy, hoàn ghế và lượt mã giảm giá đúng một lần.",
            "Xác nhận hủy vé", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);

        if (xacNhan != DialogResult.Yes) return;

        try
        {
            string? loi = bus.HuyDonChuaVe(ve.MaVe);
            if (loi != null)
            {
                MessageBox.Show(this, loi, "Lỗi hủy vé", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            MessageBox.Show(this, $"Đã hủy đơn chứa vé #{ve.MaVe}.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
            TaiDanhSach();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi xử lý: {ex.Message}", "Lỗi hệ thống", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ThucHienXuatExcel()
    {
        using var sfd = new SaveFileDialog
        {
            Filter = "Excel 97–2003 (*.xls)|*.xls",
            FileName = $"DanhSachVe_{DateTime.Now:yyyyMMdd_HHmmss}.xls"
        };
        if (sfd.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                XuatNhapDuLieuBUS.XuatExcelVe(danhSachGoc, sfd.FileName);
                MessageBox.Show(this, "Xuất danh sách vé ra Excel thành công!", "Thông báo", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, $"Lỗi xuất file: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }

    private static void ThemTruong(TableLayoutPanel bang, int dong, int cot, string nhan, Control oNhap, int soCot = 1)
    {
        var lbl = new Label
        {
            Text = nhan,
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleRight,
            Margin = new Padding(0, 0, 8, 8)
        };
        bang.Controls.Add(lbl, cot, dong);
        bang.Controls.Add(oNhap, cot + 1, dong);
        if (soCot > 1) bang.SetColumnSpan(oNhap, soCot);
    }

    private static TextBox TaoONhap(bool chiDoc = false) => new()
    {
        ReadOnly = chiDoc,
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static Button TaoNut(string chuoi, bool canhBao = false) => new()
    {
        Text = chuoi,
        AutoSize = true,
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = chuoi == "Tìm kiếm" ? GiaoDien.MauChinh : Color.White,
        ForeColor = chuoi == "Tìm kiếm" ? Color.White : canhBao ? GiaoDien.MauLoi : GiaoDien.MauChu,
        Margin = new Padding(3, 3, 8, 3),
        Padding = new Padding(10, 2, 10, 2)
    };

    public bool CoTheRoiTrang() => true;
}
