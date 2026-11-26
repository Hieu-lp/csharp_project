using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public sealed class ThongKeDoanhThuForm : Form
{
    private readonly ThongKeBUS bus = new();
    private readonly CheckBox locKhoang = new() { Text = "Khoảng ngày lập", AutoSize = true };
    private readonly DateTimePicker tuNgay = GiaoDien.TaoONhapNgay();
    private readonly DateTimePicker denNgay = GiaoDien.TaoONhapNgay();
    private readonly CheckBox locNam = new() { Text = "Năm", AutoSize = true };
    private readonly NumericUpDown nam = new() { Minimum = 1900, Maximum = 9998, Value = DateTime.Today.Year, Width = 85 };
    private readonly ComboBox thang = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly ComboBox quy = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100 };
    private readonly Label thongBao = GiaoDien.TaoNhan("Chưa tải số liệu.", 10);
    private readonly Label tongThu = GiaoDien.TaoNhan("Tổng thu: chưa tải", 12, true);
    private readonly Label chuaThu = GiaoDien.TaoNhan("Chưa thanh toán: chưa tải", 11);
    private readonly Label soLuong = GiaoDien.TaoNhan("Hóa đơn / vé: chưa tải", 10);
    private readonly Dictionary<string, ListView> cacBang = new();
    private ThongKeDoanhThuDTO? duLieu;
    private BoLocThongKeDTO? boLocDaTai;
    private bool dangTai;

    public ThongKeDoanhThuForm()
    {
        Text = "Thống kê tài chính";
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        tuNgay.Value = new DateTime(DateTime.Today.Year, 1, 1);
        denNgay.Value = DateTime.Today;
        tuNgay.Width = denNgay.Width = 130;
        tuNgay.Dock = denNgay.Dock = DockStyle.None;
        thang.Items.Add("Mọi tháng");
        for (int i = 1; i <= 12; i++)
            thang.Items.Add("Tháng " + i);
        quy.Items.Add("Mọi quý");
        for (int i = 1; i <= 4; i++)
            quy.Items.Add("Quý " + i);
        thang.SelectedIndex = quy.SelectedIndex = 0;
        var khung = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 6 };
        khung.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < 5; i++)
            khung.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        khung.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        khung.Controls.Add(GiaoDien.TaoNhan("THỐNG KÊ HÓA ĐƠN VÀ TỔNG THU", 14, true), 0, 0);
        var chuThich = GiaoDien.TaoNhan("PAID theo ngày LẬP hóa đơn; chưa có ngày nhận tiền riêng. Tổng chi / lợi nhuận: chưa xác định (chưa có nguồn chi phí).", 9);
        chuThich.MaximumSize = new Size(900, 0);
        khung.Controls.Add(chuThich, 0, 1);
        var loc = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, Margin = Padding.Empty };
        loc.Controls.AddRange([locKhoang, tuNgay, denNgay, locNam, nam, thang, quy]);
        loc.Controls.Add(GiaoDien.TaoNut("Tải số liệu", TaiSoLieu, true));
        loc.Controls.Add(GiaoDien.TaoNut("Xuất Excel .xls", XuatExcel));
        khung.Controls.Add(loc, 0, 2);
        var the = new FlowLayoutPanel { Dock = DockStyle.Fill, AutoSize = true, WrapContents = true, BackColor = Color.White, Padding = new Padding(10) };
        tongThu.Margin = chuaThu.Margin = soLuong.Margin = new Padding(4, 4, 24, 4);
        the.Controls.AddRange([tongThu, chuaThu, soLuong]);
        khung.Controls.Add(the, 0, 3);
        khung.Controls.Add(thongBao, 0, 4);
        var cacThe = new TabControl { Dock = DockStyle.Fill };
        ThemBang(cacThe, "Theo tháng", ["Năm", "Tháng", "Quý", "Hóa đơn PAID", "Vé PAID", "Tổng thu"]);
        ThemBang(cacThe, "Theo quý", ["Năm", "Quý", "Hóa đơn PAID", "Vé PAID", "Tổng thu"]);
        ThemBang(cacThe, "Theo năm", ["Năm", "Hóa đơn PAID", "Vé PAID", "Tổng thu"]);
        foreach (string ten in new[] { "Nhân viên", "Khách hàng", "Chuyến bay", "Hạng vé" })
            ThemBang(cacThe, ten, ["Mã", "Đối tượng", "Năm", "Q1", "Q2", "Q3", "Q4", "Tổng"]);
        khung.Controls.Add(cacThe, 0, 5);
        Controls.Add(khung);
        Load += async (_, _) => await TaiSoLieu();
        SizeChanged += (_, _) => { chuThich.MaximumSize = new Size(Math.Max(200, ClientSize.Width - 40), 0); thongBao.MaximumSize = new Size(Math.Max(200, ClientSize.Width - 40), 0); };
    }

    private void ThemBang(TabControl cacThe, string ten, string[] cot)
    {
        var bang = GiaoDien.TaoDanhSach();
        for (int i = 0; i < cot.Length; i++)
            bang.Columns.Add(cot[i], cot[i] == "Đối tượng" ? 230 : cot[i].StartsWith("Q") || cot[i] == "Tổng" || cot[i] == "Tổng thu" ? 145 : 100);
        var trang = new TabPage(ten) { BackColor = GiaoDien.MauNen, Padding = new Padding(8) };
        trang.Controls.Add(bang);
        cacThe.TabPages.Add(trang);
        cacBang.Add(ten, bang);
    }

    private BoLocThongKeDTO DocBoLoc() => new()
    {
        TuNgay = locKhoang.Checked ? tuNgay.Value.Date : null,
        DenNgay = locKhoang.Checked ? denNgay.Value.Date : null,
        Nam = locNam.Checked ? (int)nam.Value : null,
        Thang = thang.SelectedIndex > 0 ? thang.SelectedIndex : null,
        Quy = quy.SelectedIndex > 0 ? quy.SelectedIndex : null
    };

    private async Task TaiSoLieu()
    {
        if (dangTai)
            return;
        try
        {
            BoLocThongKeDTO loc = DocBoLoc();
            ThongKeBUS.KiemTraBoLoc(loc);
            dangTai = true;
            Enabled = false;
            UseWaitCursor = true;
            thongBao.ForeColor = GiaoDien.MauChuPhu;
            thongBao.Text = "Đang đọc báo cáo…";
            ThongKeDoanhThuDTO ketQua = await Task.Run(() => bus.ThongKeTongHop(loc));
            if (IsDisposed)
                return;
            duLieu = ketQua;
            boLocDaTai = loc;
            tongThu.Text = "Tổng thu PAID: " + ketQua.DoanhThuDaThu.ToString("N2") + " đ";
            chuaThu.Text = "UNPAID: " + ketQua.DoanhThuChuaThu.ToString("N2") + " đ";
            soLuong.Text = $"{ketQua.TongSoHoaDon} hóa đơn · {ketQua.SoHoaDonDaThanhToan} PAID · {ketQua.TongSoVeBan} vé hiệu lực";
            foreach (var bang in cacBang.Values)
                bang.Items.Clear();
            decimal tong = ketQua.DoanhThuDaThu;
            foreach (var ky in ketQua.TheoThoiGian)
            {
                cacBang["Theo tháng"].Items.Add(new ListViewItem([ky.Nam.ToString(), ky.Thang.ToString(), ky.Quy.ToString(), ky.SoHoaDon.ToString(), ky.SoVe.ToString(), ky.DoanhThu.ToString("N2")]));
            }
            cacBang["Theo tháng"].Items.Add(new ListViewItem(["TỔNG", "", "", "", "", tong.ToString("N2")]));
            foreach (var ky in ketQua.DoanhThuTheoQuy)
                cacBang["Theo quý"].Items.Add(new ListViewItem([ky.Nam.ToString(), ky.Quy.ToString(), ky.SoHoaDon.ToString(), ky.SoVe.ToString(), ky.DoanhThu.ToString("N2")]));
            cacBang["Theo quý"].Items.Add(new ListViewItem(["TỔNG", "", "", "", tong.ToString("N2")]));
            foreach (var ky in ketQua.TheoNam)
                cacBang["Theo năm"].Items.Add(new ListViewItem([ky.Nam.ToString(), ky.SoHoaDon.ToString(), ky.SoVe.ToString(), ky.DoanhThu.ToString("N2")]));
            cacBang["Theo năm"].Items.Add(new ListViewItem(["TỔNG", "", "", tong.ToString("N2")]));
            HienThiMaTran("Nhân viên", ketQua.TheoNhanVien);
            HienThiMaTran("Khách hàng", ketQua.TheoKhachHang);
            HienThiMaTran("Chuyến bay", ketQua.TheoChuyenBay);
            HienThiMaTran("Hạng vé", ketQua.TheoHangVe);
            foreach (var bang in cacBang.Values)
                GiaoDien.CanCotDanhSach(bang);
            thongBao.Text = "Đã tải. Hạng vé phân bổ tiền PAID theo thành tiền lịch sử; gốc 0 chia đều, dòng cuối nhận phần lẻ. WEB không bị loại khỏi tổng thu.";
        }
        catch (Exception loi)
        {
            if (IsDisposed)
                return;
            duLieu = null;
            boLocDaTai = null;
            tongThu.Text = "Tổng thu: chưa tải được";
            chuaThu.Text = "UNPAID: chưa tải được";
            soLuong.Text = "Chưa có kết quả";
            foreach (var bang in cacBang.Values)
                bang.Items.Clear();
            thongBao.Text = "Chưa có báo cáo hợp lệ: " + loi.Message;
            thongBao.ForeColor = GiaoDien.MauLoi;
        }
        finally { if (!IsDisposed) { dangTai = false; Enabled = true; UseWaitCursor = false; } }
    }

    private void HienThiMaTran(string ten, List<MaTranDoanhThuDTO> nguon)
    {
        var bang = cacBang[ten];
        foreach (var dong in nguon)
        {
            bang.Items.Add(new ListViewItem([dong.Ma, dong.Ten, dong.Nam.ToString(), dong.Quy1.ToString("N2"), dong.Quy2.ToString("N2"), dong.Quy3.ToString("N2"), dong.Quy4.ToString("N2"), dong.Tong.ToString("N2")]));
        }
        var tong = ThongKeBUS.TinhDongTong(nguon);
        bang.Items.Add(new ListViewItem(["", tong.Ten, "", tong.Quy1.ToString("N2"), tong.Quy2.ToString("N2"), tong.Quy3.ToString("N2"), tong.Quy4.ToString("N2"), tong.Tong.ToString("N2")]));
    }

    private Task XuatExcel()
    {
        if (duLieu is null || boLocDaTai is null)
        {
            thongBao.Text = "Hãy tải báo cáo hợp lệ trước khi xuất.";
            return Task.CompletedTask;
        }
        using var hopThoai = new SaveFileDialog { Filter = "Excel 97–2003 (*.xls)|*.xls", FileName = "ThongKeHoaDon.xls" };
        if (hopThoai.ShowDialog(this) == DialogResult.OK)
        {
            try
            {
                XuatNhapDuLieuBUS.XuatExcelThongKe(duLieu, hopThoai.FileName, boLocDaTai);
                thongBao.Text = "Đã xuất đúng dữ liệu đã tải và bộ lọc đang áp dụng.";
            }
            catch (Exception loi) { thongBao.Text = "Không xuất được: " + loi.Message; }
        }
        return Task.CompletedTask;
    }

    public bool CoTheRoiTrang() => !dangTai;
}
