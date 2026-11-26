using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

/// <summary>
/// Màn hình thống kê doanh thu và phân tích hiệu quả bán vé máy bay.
/// Thống kê doanh thu tổng, doanh thu theo quý (Q1-Q4) và theo từng tuyến bay.
/// </summary>
public sealed class ThongKeDoanhThuForm : Form
{
    private readonly HoaDonBUS bus = new();

    // Các thẻ tóm tắt số liệu (Summary KPI Cards)
    private readonly Label lblTongDoanhThu = TaoNhanGiaTri("0 VNĐ");
    private readonly Label lblDoanhThuDaThu = TaoNhanGiaTri("0 VNĐ", Color.DarkGreen);
    private readonly Label lblDoanhThuChuaThu = TaoNhanGiaTri("0 VNĐ", GiaoDien.MauChinh);
    private readonly Label lblTongSoVe = TaoNhanGiaTri("0 vé");
    private readonly Label lblTongSoHoaDon = TaoNhanGiaTri("0 HĐ");

    // Bảng 1: Thống kê theo Quý
    private readonly ListView danhSachQuy = GiaoDien.TaoDanhSach();

    // Bảng 2: Thống kê theo Tuyến bay
    private readonly ListView danhSachTuyenBay = GiaoDien.TaoDanhSach();

    private readonly Button nutTaiLai = new()
    {
        Text = "Tải lại số liệu",
        AutoSize = true,
        Height = 36,
        FlatStyle = FlatStyle.Flat,
        BackColor = GiaoDien.MauChinh,
        ForeColor = Color.White,
        Padding = new Padding(12, 2, 12, 2)
    };

    public ThongKeDoanhThuForm()
    {
        Text = "Thống kê doanh thu & Hiệu quả bán vé";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 800);
        Font = new Font("Segoe UI", 10F);
        BackColor = GiaoDien.MauNen;
        ForeColor = GiaoDien.MauChu;

        TaoCotDanhSach();
        TaoBoCuc();

        nutTaiLai.Click += (_, _) => TaiSoLieu();
        Load += (_, _) => TaiSoLieu();
    }

    private void TaoBoCuc()
    {
        var khungChinh = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 5
        };
        khungChinh.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));

        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 0: Tiêu đề + Nút tải lại
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 1: Vùng KPI Cards
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // 2: Bảng Quý
        khungChinh.RowStyles.Add(new RowStyle(SizeType.AutoSize)); // 3: Tiêu đề Bảng Tuyến bay
        khungChinh.RowStyles.Add(new RowStyle(SizeType.Percent, 50)); // 4: Bảng Tuyến bay

        // Tiêu đề
        var thanhTieuDe = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 14)
        };
        thanhTieuDe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 80));
        thanhTieuDe.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        thanhTieuDe.Controls.Add(new Label
        {
            Text = "BÁO CÁO THỐNG KÊ DOANH THU & SỐ LƯỢNG VÉ BÁN",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft
        }, 0, 0);

        thanhTieuDe.Controls.Add(nutTaiLai, 1, 0);
        khungChinh.Controls.Add(thanhTieuDe, 0, 0);

        // Vùng thẻ KPI
        khungChinh.Controls.Add(TaoVungKpiCards(), 0, 1);

        // Bảng Doanh thu theo Quý
        var tieuDeQuy = new Label
        {
            Text = "DOANH THU THEO TỪNG QUÝ (HÓA ĐƠN ĐÃ THANH TOÁN):",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 10, 0, 6)
        };
        khungChinh.Controls.Add(tieuDeQuy, 0, 2);
        khungChinh.Controls.Add(danhSachQuy, 0, 2);

        // Bảng Doanh thu theo Tuyến bay
        var tieuDeTuyenBay = new Label
        {
            Text = "DOANH THU THEO TỪNG TUYẾN / CHUYẾN BAY:",
            Font = new Font(Font, FontStyle.Bold),
            AutoSize = true,
            Margin = new Padding(0, 12, 0, 6)
        };
        khungChinh.Controls.Add(tieuDeTuyenBay, 0, 3);
        khungChinh.Controls.Add(danhSachTuyenBay, 0, 4);

        Controls.Add(khungChinh);
    }

    private Control TaoVungKpiCards()
    {
        var bangKpi = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 5,
            RowCount = 1,
            Margin = new Padding(0, 0, 0, 14)
        };

        for (int i = 0; i < 5; i++)
            bangKpi.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 20));

        bangKpi.Controls.Add(TaoTheKpi("Tổng doanh thu", lblTongDoanhThu), 0, 0);
        bangKpi.Controls.Add(TaoTheKpi("Doanh thu đã thu (PAID)", lblDoanhThuDaThu), 1, 0);
        bangKpi.Controls.Add(TaoTheKpi("Doanh thu chưa thu (UNPAID)", lblDoanhThuChuaThu), 2, 0);
        bangKpi.Controls.Add(TaoTheKpi("Tổng số vé đã bán", lblTongSoVe), 3, 0);
        bangKpi.Controls.Add(TaoTheKpi("Tổng số hóa đơn", lblTongSoHoaDon), 4, 0);

        return bangKpi;
    }

    private static Panel TaoTheKpi(string tieuDe, Label lblGiaTri)
    {
        var card = new Panel
        {
            Dock = DockStyle.Fill,
            BackColor = Color.White,
            Padding = new Padding(12),
            Margin = new Padding(4)
        };

        var layout = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2
        };
        layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var lblTieuDe = new Label
        {
            Text = tieuDe,
            ForeColor = GiaoDien.MauChuPhu,
            Font = new Font("Segoe UI", 9F),
            AutoSize = true
        };

        layout.Controls.Add(lblTieuDe, 0, 0);
        layout.Controls.Add(lblGiaTri, 0, 1);
        card.Controls.Add(layout);
        return card;
    }

    private static Label TaoNhanGiaTri(string giaTri, Color? mau = null) => new()
    {
        Text = giaTri,
        ForeColor = mau ?? GiaoDien.MauChu,
        Font = new Font("Segoe UI", 13F, FontStyle.Bold),
        AutoSize = true,
        Dock = DockStyle.Fill,
        TextAlign = ContentAlignment.MiddleLeft
    };

    private void TaoCotDanhSach()
    {
        // Bảng Quý
        danhSachQuy.Columns.Add("Nam", "Năm", 120);
        danhSachQuy.Columns.Add("Quy", "Quý", 120);
        danhSachQuy.Columns.Add("SoHD", "Số hóa đơn đã thanh toán", 200);
        danhSachQuy.Columns.Add("SoVe", "Số vé bán", 160);
        danhSachQuy.Columns.Add("DoanhThu", "Doanh thu (VNĐ)", 250);

        // Bảng Tuyến bay
        danhSachTuyenBay.Columns.Add("MaChuyen", "Mã chuyến bay", 140);
        danhSachTuyenBay.Columns.Add("TuyenBay", "Tuyến bay (Nơi đi → Nơi đến)", 320);
        danhSachTuyenBay.Columns.Add("SoVeBan", "Số vé đã bán", 160);
        danhSachTuyenBay.Columns.Add("DoanhThu", "Tổng doanh thu (VNĐ)", 250);
    }

    private void TaiSoLieu()
    {
        try
        {
            var tk = bus.ThongKeTongHop();

            lblTongDoanhThu.Text = tk.TongDoanhThu.ToString("N0") + " đ";
            lblDoanhThuDaThu.Text = tk.DoanhThuDaThu.ToString("N0") + " đ";
            lblDoanhThuChuaThu.Text = tk.DoanhThuChuaThu.ToString("N0") + " đ";
            lblTongSoVe.Text = tk.TongSoVeBan.ToString("N0") + " vé";
            lblTongSoHoaDon.Text = $"{tk.TongSoHoaDon} HĐ ({tk.SoHoaDonDaThanhToan} đã thu)";

            // Hiển thị Quý
            danhSachQuy.BeginUpdate();
            danhSachQuy.Items.Clear();
            foreach (var q in tk.DoanhThuTheoQuy)
            {
                var dong = new ListViewItem(q.Nam.ToString());
                dong.SubItems.Add($"Quý {q.Quy}");
                dong.SubItems.Add(q.SoHoaDon.ToString("N0"));
                dong.SubItems.Add(q.SoVe.ToString("N0"));
                dong.SubItems.Add(q.DoanhThu.ToString("N0"));
                danhSachQuy.Items.Add(dong);
            }
            danhSachQuy.EndUpdate();

            // Hiển thị Tuyến bay
            danhSachTuyenBay.BeginUpdate();
            danhSachTuyenBay.Items.Clear();
            foreach (var cb in tk.DoanhThuTheoChuyenBay)
            {
                var dong = new ListViewItem(cb.MaChuyenBay.ToString());
                dong.SubItems.Add(cb.TuyenBay);
                dong.SubItems.Add(cb.SoVeDaBan.ToString("N0"));
                dong.SubItems.Add(cb.DoanhThu.ToString("N0"));
                danhSachTuyenBay.Items.Add(dong);
            }
            danhSachTuyenBay.EndUpdate();
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Lỗi tải dữ liệu thống kê: {ex.Message}", "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    public bool CoTheRoiTrang() => true;
}
