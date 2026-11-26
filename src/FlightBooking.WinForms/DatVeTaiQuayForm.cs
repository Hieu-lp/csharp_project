using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public sealed partial class DatVeTaiQuayForm : Form
{
    private readonly DatVeBUS bus = new();
    private readonly ComboBox khach = TaoChon(), nhanVien = TaoChon(), luotDi = TaoChon(), luotVe = TaoChon();
    private readonly CheckBox khuHoi = new() { Text = "Khứ hồi", AutoSize = true };
    private readonly NumericUpDown soNguoi = new() { Minimum = 1, Maximum = 9, Value = 1, Width = 90 };
    private readonly TextBox maGiam = new() { Width = 200, MaxLength = 50 };
    private readonly Label lienHe = GiaoDien.TaoNhan("Chọn khách hàng đã có. Quản lý khách hàng để bổ sung thông tin còn thiếu.", 9);
    private readonly Label gia = GiaoDien.TaoNhan("Chưa tính giá. Hóa đơn được tạo ở trạng thái UNPAID.", 10, true);
    private readonly FlowLayoutPanel hanhKhach = new() { Dock = DockStyle.Top, AutoSize = true, FlowDirection = FlowDirection.TopDown, WrapContents = false };
    private readonly List<TextBox> hoTen = new(), giayTo = new();
    private readonly string khoa = DatVeBUS.TaoKhoaTraCuu();
    private decimal? giaDaXem;
    private bool dangKhoiTao = true;
    private bool daDat;
    private readonly Button xacNhan = GiaoDien.TaoNut("Xác nhận đặt tại quầy", () => Task.CompletedTask, true);

    public DatVeTaiQuayForm()
    {
        Text = "Đặt vé tại quầy"; Font = new Font("Segoe UI", 10);
        Size = new Size(860, 740); MinimumSize = new Size(640, 580); StartPosition = FormStartPosition.CenterParent;
        BackColor = GiaoDien.MauNen;
        var bang = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(20) };
        bang.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 160)); bang.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        var cuon = new Panel { Dock = DockStyle.Fill, AutoScroll = true }; cuon.Controls.Add(bang); Controls.Add(cuon);
        void Them(string nhan, Control o)
        {
            int dong = bang.RowCount++; bang.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            bang.Controls.Add(GiaoDien.TaoNhan(nhan), 0, dong); bang.Controls.Add(o, 1, dong); o.Margin = new Padding(4, 6, 4, 10);
        }
        Them("Khách đặt / liên hệ", khach); Them("Thông tin liên hệ", lienHe); Them("Nhân viên bán", nhanVien);
        KhoiTaoTimChuyen(Them); Them("Số hành khách", soNguoi); Them("Loại hành trình", khuHoi); Them("Chuyến / hạng lượt đi", luotDi);
        Them("Chuyến / hạng lượt về", luotVe); Them("Mã giảm giá", maGiam); Them("Hành khách đi vé", hanhKhach);
        KhoiTaoGhe(Them);
        Them("Báo giá", gia);
        var nutGia = GiaoDien.TaoNut("Tính lại giá / áp mã", () => Task.CompletedTask);
        var dongNut = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill };
        dongNut.Controls.AddRange([nutGia, xacNhan]); Them("", dongNut);
        khach.Format += (_, e) => { if (e.ListItem is KhachHangDTO k) e.Value = $"#{k.MaKhachHang} · {k.HoTen}"; };
        nhanVien.Format += (_, e) => { if (e.ListItem is NhanVienDTO n) e.Value = $"#{n.MaNhanVien} · {n.HoTen}"; };
        foreach (var o in new[] { luotDi, luotVe }) o.Format += (_, e) =>
        {
            if (e.ListItem is LuaChonChuyenBayDTO c)
                e.Value = $"#{c.ChuyenBay.MaChuyenBay} · {c.ChuyenBay.SanBayDi} → {c.ChuyenBay.SanBayDen} · {c.ChuyenBay.NgayKhoiHanh:dd/MM/yyyy} {c.ChuyenBay.GioKhoiHanh:HH:mm} · {c.HangVe.TenHangVe} · {c.HangVe.GiaVe:N0} đ";
        };
        khach.SelectedIndexChanged += (_, _) =>
        {
            if (khach.SelectedItem is KhachHangDTO k) lienHe.Text = $"{k.DienThoai} · {k.Email} · Sinh {k.NgaySinh:dd/MM/yyyy} · Giấy tờ {k.SoGiayTo}";
            BoGia();
        };
        nhanVien.SelectedIndexChanged += (_, _) => BoGia();
        luotDi.SelectedIndexChanged += (_, _) => BoGia();
        luotVe.SelectedIndexChanged += (_, _) => BoGia();
        khuHoi.CheckedChanged += (_, _) => { luotVe.Enabled = khuHoi.Checked; BoGia(); };
        soNguoi.ValueChanged += (_, _) => { CapNhatHanhKhach(); BoGia(); };
        maGiam.TextChanged += (_, _) => BoGia();
        nutGia.Click += (_, _) =>
        {
            try
            {
                var yc = DocYeuCau(); var baoGia = bus.TinhGiaHanhTrinh(yc); XemGhe(yc); giaDaXem = baoGia.TongTien;
                gia.Text = $"Đi: {baoGia.LuotDi.TongTien:N0} đ; về: {baoGia.LuotVe?.TongTien ?? 0:N0} đ. Tổng: {baoGia.TongTien:N0} đ (chưa thanh toán).";
            }
            catch (Exception loi) { giaDaXem = null; MessageBox.Show(this, loi.Message, "Không tính được giá"); }
        };
        xacNhan.Click += (_, _) => DatVe();
        CapNhatHanhKhach();
        Load += (_, _) => TaiDuLieu();
        FormClosing += (_, e) =>
        {
            if (!daDat && (hoTen.Any(o => o.TextLength > 0) || giayTo.Any(o => o.TextLength > 0)) && MessageBox.Show(this, "Bỏ thông tin hành khách chưa đặt?", "Đóng đặt tại quầy", MessageBoxButtons.YesNo) != DialogResult.Yes) e.Cancel = true;
        };
    }

    private void TaiDuLieu() => TaiDanhMuc();

    private void CapNhatHanhKhach()
    {
        int n = (int)soNguoi.Value;
        while (hoTen.Count < n)
        {
            hoTen.Add(new TextBox { Width = 220, MaxLength = 100, PlaceholderText = "Họ tên hành khách" });
            giayTo.Add(new TextBox { Width = 185, MaxLength = 50, PlaceholderText = "CCCD / hộ chiếu" });
        }
        foreach (Control dongCu in hanhKhach.Controls)
        {
            foreach (var o in hoTen) if (o.Parent == dongCu) dongCu.Controls.Remove(o);
            foreach (var o in giayTo) if (o.Parent == dongCu) dongCu.Controls.Remove(o);
        }
        while (hanhKhach.Controls.Count > 0) hanhKhach.Controls[0].Dispose();
        for (int i = 0; i < n; i++)
        {
            hoTen[i].AccessibleName = "Họ tên hành khách " + (i + 1); giayTo[i].AccessibleName = "Giấy tờ hành khách " + (i + 1);
            var dong = new FlowLayoutPanel { AutoSize = true, WrapContents = true, Width = 470 };
            dong.Controls.AddRange([GiaoDien.TaoNhan("Hành khách " + (i + 1)), hoTen[i], giayTo[i]]);
            hanhKhach.Controls.Add(dong);
        }
    }

    private void BoGia() { if (!dangKhoiTao) { giaDaXem = null; gheDi.Clear(); gheVe.Clear(); while(khungGhe.Controls.Count>0)khungGhe.Controls[0].Dispose(); gia.Text = "Lựa chọn đã đổi. Tính lại giá trước khi đặt."; } }

    private YeuCauDatVe DocYeuCau()
    {
        if (khach.SelectedItem is not KhachHangDTO k || nhanVien.SelectedItem is not NhanVienDTO || luotDi.SelectedItem is not LuaChonChuyenBayDTO di)
            throw new LoiDatVe("Chọn khách hàng, nhân viên và chuyến đi.");
        var ve = khuHoi.Checked ? luotVe.SelectedItem as LuaChonChuyenBayDTO : null;
        if (khuHoi.Checked && ve is null) throw new LoiDatVe("Chưa có chuyến về phù hợp.");
        var yc = new YeuCauDatVe { FlightId = di.ChuyenBay.MaChuyenBay, FareId = di.HangVe.MaHangVe, Quantity = (int)soNguoi.Value,
            TripType = khuHoi.Checked ? "RoundTrip" : "OneWay", ReturnFlightId = ve?.ChuyenBay.MaChuyenBay, ReturnFareId = ve?.HangVe.MaHangVe, ReturnDate = ve?.ChuyenBay.NgayKhoiHanh,
            DiscountCode = maGiam.Text, ExpectedTotal = giaDaXem ?? 0,
            Customer = new ThongTinKhachDat { FullName = k.HoTen, Phone = k.DienThoai, Email = k.Email, DateOfBirth = DateOnly.FromDateTime(k.NgaySinh), Gender = k.GioiTinh, IdPassportNumber = k.SoGiayTo } };
        for (int i = 0; i < yc.Quantity; i++) yc.Passengers.Add(new() { FullName = hoTen[i].Text, IdCard = giayTo[i].Text });
        if(gheDi.Count==yc.Quantity)yc.Seats=gheDi.Select(x=>x.Text).ToList();
        if(khuHoi.Checked && gheVe.Count==yc.Quantity)yc.ReturnSeats=gheVe.Select(x=>x.Text).ToList();
        return yc;
    }

    private void DatVe()
    {
        if (!giaDaXem.HasValue || gheDi.Count!=(int)soNguoi.Value || (khuHoi.Checked && gheVe.Count!=(int)soNguoi.Value)) { MessageBox.Show(this, "Tính lại giá trước khi xác nhận."); return; }
        if (MessageBox.Show(this, $"Tạo đơn {giaDaXem:N0} đ, chưa thanh toán?", "Xác nhận đặt tại quầy", MessageBoxButtons.YesNo) != DialogResult.Yes) return;
        xacNhan.Enabled = false;
        try
        {
            var yc = DocYeuCau();
            int id = bus.TaoDatVeTaiQuay(yc, ((KhachHangDTO)khach.SelectedItem!).MaKhachHang, ((NhanVienDTO)nhanVien.SelectedItem!).MaNhanVien, khoa);
            daDat = true; MessageBox.Show(this, $"Đã tạo đơn #{id}. Mở Hóa đơn → Tải lại để thanh toán. Khứ hồi có hóa đơn riêng cho từng chặng.", "Đặt thành công");
            DialogResult = DialogResult.OK; Close();
        }
        catch (Exception loi) { MessageBox.Show(this, loi.Message, "Chưa xác nhận được đơn"); }
        finally { if (!IsDisposed) xacNhan.Enabled = true; }
    }

    private static ComboBox TaoChon() => new() { DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, FormattingEnabled = true, DropDownWidth = 800 };
}
