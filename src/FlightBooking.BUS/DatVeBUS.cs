using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using FlightBooking.DAO;
using FlightBooking.DTO;

namespace FlightBooking.BUS;

public sealed class DatVeBUS
{
    private readonly DatVeDAO dao;
    private readonly TimeProvider dongHo;

    public DatVeBUS(string chuoiKetNoi, TimeProvider? dongHo = null)
    {
        dao = new DatVeDAO(chuoiKetNoi);
        this.dongHo = dongHo ?? TimeProvider.System;
    }

    private DateTime ThoiDiemHienTai
    {
        get
        {
            DateTimeOffset gioUTC = dongHo.GetUtcNow();
            TimeZoneInfo muiGioVietNam = TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time");
            return TimeZoneInfo.ConvertTime(gioUTC, muiGioVietNam).DateTime;
        }
    }

    public DateOnly HomNay
    {
        get { return DateOnly.FromDateTime(ThoiDiemHienTai); }
    }

    public static DateOnly NgaySinhKhachDatToiDa(DateOnly homNay)
    {
        var ngayGioiHan = homNay.AddYears(-18);
        return ngayGioiHan.AddYears(18) < homNay ? ngayGioiHan : ngayGioiHan.AddDays(-1);
    }

    public static string? LoiNgaySinhKhachDat(DateOnly? ngaySinh, DateOnly homNay)
    {
        if (ngaySinh is not DateOnly dob || dob > homNay || dob < homNay.AddYears(-120))
            return "Nhập ngày sinh người đặt hợp lệ, không ở tương lai.";
        return dob.AddYears(18) >= homNay ? "Người đặt phải đã qua ngày sinh nhật lần thứ 18." : null;
    }

    public ChuyenBayDTO DocChuyenBay(int maChuyenBay)
    {
        return dao.DocChuyenBay(maChuyenBay);
    }

    public IReadOnlyList<SanBayDTO> DocDSSanBay()
    {
        return dao.DocDSSanBay();
    }

    public IReadOnlyList<string> DocTenHangVe()
    {
        return dao.DocTenHangVe();
    }
    public IReadOnlyList<LuaChonChuyenBayDTO> DocChuyenGiaThap()
    {
        // Giữ thời điểm lấy Now sau khi mở kết nối, giống luồng đọc ban đầu.
        return dao.DocDuLieu(duLieu => duLieu.DocChuyenGiaThap(ThoiDiemHienTai));
    }

    public ChuyenBayDaChonDTO ChonChuyenBay(int maChuyenBay, int maHangVe, int soLuong)
    {
        return dao.DocDuLieu(duLieu =>
        {
            // Màn hình chọn vẫn được xem chuyến thiếu ghế; Quote/Create mới từ chối xác nhận.
            BaoGiaDatVeDTO baoGia = LapBaoGia(duLieu, maChuyenBay, maHangVe, soLuong, null, ThoiDiemHienTai, false, canDuGhe: false);
            return new ChuyenBayDaChonDTO(baoGia);
        });
    }

    public static decimal? DocTienViet(string? chuoi)
    {
        if (string.IsNullOrWhiteSpace(chuoi)) return null;
        chuoi = chuoi.Trim();
        if (chuoi.Length > 32 || !Regex.IsMatch(chuoi, @"^(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]{1,2})?$"))
            throw new LoiDatVe("Nhập giá VND không âm, ví dụ 1.000.000 hoặc 1000000.");
        if (!decimal.TryParse(chuoi.Replace(".", "").Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var soTien) || soTien > 9999999999999999.99m)
            throw new LoiDatVe("Giá vượt giới hạn cho phép.");
        return soTien;
    }

    public static void KiemTraKhoangGia(DieuKienTimChuyenBay dieuKienTimKiem)
    {
        if (dieuKienTimKiem.MinPrice < 0 || dieuKienTimKiem.MaxPrice < 0 || dieuKienTimKiem.MinPrice > dieuKienTimKiem.MaxPrice)
            throw new LoiDatVe("Giá phải không âm và giá thấp nhất không được lớn hơn giá cao nhất.");
        if (dieuKienTimKiem.FareClassName?.Length > 50) throw new LoiDatVe("Hạng vé không hợp lệ.");
    }

    public ArrayList TimChuyenBay(DieuKienTimChuyenBay dieuKienTimKiem)
    {
        KiemTraNgayTimKiem(dieuKienTimKiem);
        KiemTraKhoangGia(dieuKienTimKiem);
        if (dieuKienTimKiem.DepartureAirportId < 1 || dieuKienTimKiem.ArrivalAirportId < 1 || dieuKienTimKiem.DepartureAirportId == dieuKienTimKiem.ArrivalAirportId)
            throw new LoiDatVe("Chọn hai sân bay đi và đến khác nhau.");
        if (dieuKienTimKiem.DepartureDate < DateOnly.FromDateTime(ThoiDiemHienTai) || dieuKienTimKiem.Passengers is < 1 or > 9)
            throw new LoiDatVe("Chọn ngày từ hôm nay và từ 1 đến 9 hành khách.");
        var hienTai = ThoiDiemHienTai;
        var danhSachLuaChon = dao.DocDuLieu(duLieu =>
        {
            var ketQua = new ArrayList();
            foreach (ChuyenBayDTO chuyenBay in duLieu.DocDSChuyenBay(dieuKienTimKiem))
            {
                if (chuyenBay.ThoiDiemKhoiHanh <= hienTai) continue;
                foreach (HangVeDTO hangVe in duLieu.DocDSHangVe(chuyenBay.MaChuyenBay))
                {
                    ketQua.Add(new LuaChonChuyenBayDTO(chuyenBay, hangVe));
                }
            }
            return ketQua;
        });
        return LocChuyenBay(danhSachLuaChon, dieuKienTimKiem);
    }

    // Lọc nâng cao trên ArrayList tại BUS; không ghép điều kiện người dùng vào SQL.
    public static ArrayList LocChuyenBay(ArrayList danhSachLuaChon, DieuKienTimChuyenBay dieuKienTimKiem)
    {
        KiemTraNgayTimKiem(dieuKienTimKiem);
        KiemTraKhoangGia(dieuKienTimKiem);
        if (dieuKienTimKiem.Join is not ("AND" or "OR") || dieuKienTimKiem.Operator is not ("=" or "<>" or ">" or ">=" or "<" or "<="))
            throw new LoiDatVe("Phép lọc không hợp lệ.");
        var tenHangVe = dieuKienTimKiem.ClassName?.Trim() ?? "";
        if (dieuKienTimKiem.Price < 0 || tenHangVe.Length > 50) throw new LoiDatVe("Điều kiện lọc không hợp lệ.");
        var ketQua = new ArrayList();
        foreach (LuaChonChuyenBayDTO luaChon in danhSachLuaChon)
        {
            if (dieuKienTimKiem.ArrivalDate is DateOnly gioDen && luaChon.ChuyenBay.NgayDen != gioDen) continue;
            if (dieuKienTimKiem.MinPrice is decimal toiThieu && luaChon.HangVe.GiaVe < toiThieu || dieuKienTimKiem.MaxPrice is decimal toiDa && luaChon.HangVe.GiaVe > toiDa) continue;
            if (!string.IsNullOrEmpty(dieuKienTimKiem.FareClassName) && !string.Equals(luaChon.HangVe.TenHangVe, dieuKienTimKiem.FareClassName, StringComparison.OrdinalIgnoreCase)) continue;
            var dieuKienLoc = new List<bool>();
            if (dieuKienTimKiem.Price is decimal giaVe)
            {
                var soSanh = luaChon.HangVe.GiaVe.CompareTo(giaVe);
                bool khopGia;
                switch (dieuKienTimKiem.Operator)
                {
                    case "=": khopGia = soSanh == 0; break;
                    case "<>": khopGia = soSanh != 0; break;
                    case ">": khopGia = soSanh > 0; break;
                    case ">=": khopGia = soSanh >= 0; break;
                    case "<": khopGia = soSanh < 0; break;
                    default: khopGia = soSanh <= 0; break;
                }
                dieuKienLoc.Add(khopGia);
            }
            if (tenHangVe.Length > 0)
                dieuKienLoc.Add(luaChon.HangVe.TenHangVe.Contains(tenHangVe, StringComparison.OrdinalIgnoreCase));
            bool khop = dieuKienTimKiem.Join == "AND";
            foreach (bool dieuKien in dieuKienLoc)
            {
                if (dieuKienTimKiem.Join == "AND" && !dieuKien) khop = false;
                if (dieuKienTimKiem.Join == "OR" && dieuKien) khop = true;
            }
            if (dieuKienLoc.Count == 0) khop = true;
            if (dieuKienLoc.Count > 0 && dieuKienTimKiem.Not) khop = !khop;
            if (khop) ketQua.Add(luaChon);
        }
        return ketQua;
    }

    public static void KiemTraNgayTimKiem(DieuKienTimChuyenBay dieuKienTimKiem)
    {
        if (dieuKienTimKiem.TripType is not ("OneWay" or "RoundTrip")) throw new LoiDatVe("Loại hành trình không hợp lệ.");
        if (dieuKienTimKiem.TripType == "OneWay") dieuKienTimKiem.ReturnDate = null;
        else if (dieuKienTimKiem.ReturnDate is not DateOnly ngay || ngay < dieuKienTimKiem.DepartureDate)
            throw new LoiDatVe("Chọn ngày về từ ngày đi trở đi.");
        if (dieuKienTimKiem.ArrivalDate is DateOnly gioDen && gioDen < dieuKienTimKiem.DepartureDate)
            throw new LoiDatVe("Ngày đến không được trước ngày đi.");
    }

    public BaoGiaDatVeDTO TinhGiaVe(int maChuyenBay, int maHangVe, int soLuong, string? maGiamGia)
    {
        return dao.DocDuLieu(duLieu => LapBaoGia(duLieu, maChuyenBay, maHangVe, soLuong, maGiamGia, ThoiDiemHienTai, false));
    }

    private static BaoGiaDatVeDTO LapBaoGia(DuLieuDatVe duLieu, int maChuyenBay, int maHangVe, int soLuong,
        string? maGiamGia, DateTime hienTai, bool canKhoa, bool canDuGhe = true)
    {
        if (soLuong is < 1 or > 9) throw new LoiDatVe("Mỗi đơn đặt từ 1 đến 9 hành khách.");
        var chuyenBay = duLieu.DocChuyenBay(maChuyenBay, canKhoa);
        if (chuyenBay.TrangThai != "SCHEDULED" || chuyenBay.ThoiDiemKhoiHanh <= hienTai) throw new LoiDatVe("Chuyến bay không còn nhận đặt vé.");
        if (canDuGhe && chuyenBay.SoGheCon < soLuong) throw new LoiDatVe("Chuyến bay không còn đủ chỗ.");
        var hangVe = duLieu.DocDSHangVe(maChuyenBay, canKhoa).SingleOrDefault(chuyenBay => chuyenBay.MaHangVe == maHangVe)
            ?? throw new LoiDatVe("Hạng vé không thuộc chuyến bay này.");
        var tamTinh = hangVe.GiaVe * soLuong;
        MaGiamGiaDTO? giamGia = null;
        if (!string.IsNullOrWhiteSpace(maGiamGia))
            giamGia = duLieu.DocMaGiamGia(ChuanHoaMa(maGiamGia), canKhoa) ?? throw new LoiDatVe("Mã giảm giá không tồn tại.");
        var soTien = TinhGiamGia(giamGia, tamTinh, hienTai);
        return new(chuyenBay, hangVe, soLuong, tamTinh, soTien, tamTinh - soTien, giamGia?.MaGiamGia);
    }

    public static decimal TinhGiamGia(MaGiamGiaDTO? giamGia, decimal tamTinh, DateTime hienTai)
    {
        if (tamTinh < 0) throw new LoiDatVe("Tổng tiền không hợp lệ.");
        if (giamGia is null) return 0;
        if (giamGia.TrangThai != "ACTIVE" || hienTai < giamGia.NgayBatDau || hienTai > giamGia.NgayKetThuc)
            throw new LoiDatVe("Mã giảm giá không hoạt động hoặc ngoài thời hạn.");
        if (tamTinh < giamGia.GiaTriDonToiThieu) throw new LoiDatVe("Đơn chưa đạt giá trị tối thiểu của mã giảm giá.");
        if (giamGia.GioiHanSuDung is int gioiHan && giamGia.SoLanDaDung >= gioiHan) throw new LoiDatVe("Mã giảm giá đã hết lượt.");
        decimal soTien;
        if (giamGia.Loai == "PERCENT")
        {
            soTien = decimal.Round(tamTinh * giamGia.GiaTri / 100m, 2, MidpointRounding.AwayFromZero);
        }
        else if (giamGia.Loai == "FIXED")
        {
            soTien = giamGia.GiaTri;
        }
        else
        {
            throw new LoiDatVe("Loại giảm giá không hợp lệ.");
        }
        if (giamGia.GiamToiDa is decimal mucTran) soTien = Math.Min(soTien, mucTran);
        return Math.Min(soTien, tamTinh);
    }

    public BaoGiaHanhTrinhDTO TinhGiaHanhTrinh(YeuCauDatVe yeuCau)
    {
        return dao.DocDuLieu(duLieu => LapBaoGiaHanhTrinh(duLieu, yeuCau, ThoiDiemHienTai, false));
    }

    private static BaoGiaHanhTrinhDTO LapBaoGiaHanhTrinh(DuLieuDatVe duLieu, YeuCauDatVe yeuCau, DateTime hienTai, bool canKhoa)
    {
        ChuanHoaHanhTrinh(yeuCau);
        var luotDi = LapBaoGia(duLieu, yeuCau.FlightId, yeuCau.FareId, yeuCau.Quantity, yeuCau.DiscountCode, hienTai, canKhoa);
        BaoGiaDatVeDTO? luotVe = null;
        if (yeuCau.TripType == "RoundTrip")
        {
            luotVe = LapBaoGia(duLieu, yeuCau.ReturnFlightId!.Value, yeuCau.ReturnFareId!.Value, yeuCau.Quantity, null, hienTai, canKhoa);
            KiemTraChuyenVe(luotDi.ChuyenBay, luotVe.ChuyenBay, yeuCau.ReturnDate!.Value);
        }
        return new(luotDi, luotVe);
    }

    public static void ChuanHoaHanhTrinh(YeuCauDatVe yeuCau)
    {
        if (yeuCau.Quantity is < 1 or > 9) throw new LoiDatVe("Chọn từ 1 đến 9 hành khách.");
        if (yeuCau.TripType == "OneWay") { yeuCau.ReturnDate = null; yeuCau.ReturnFlightId = null; yeuCau.ReturnFareId = null; }
        else if (yeuCau.TripType != "RoundTrip" || yeuCau.ReturnDate is null || yeuCau.ReturnFlightId is not > 0 || yeuCau.ReturnFareId is not > 0)
            throw new LoiDatVe("Chọn đầy đủ chuyến đi, chuyến về và ngày về.");
    }

    public static void KiemTraChuyenVe(ChuyenBayDTO luotDi, ChuyenBayDTO luotVe, DateOnly ngay)
    {
        if (luotDi.MaChuyenBay == luotVe.MaChuyenBay || luotVe.MaSanBayDi != luotDi.MaSanBayDen ||
            luotVe.MaSanBayDen != luotDi.MaSanBayDi || luotVe.NgayKhoiHanh != ngay || luotVe.ThoiDiemKhoiHanh <= luotDi.ThoiDiemDen)
            throw new LoiDatVe("Lượt về phải đảo chiều hành trình, đúng ngày về và khởi hành sau khi lượt đi hạ cánh.");
    }

    public int TaoDatVe(YeuCauDatVe yeuCau, string khoaTraCuu)
    {
        // 1. Kiểm tra/chuẩn hóa DTO và giữ nguyên cấu trúc nội dung dùng để chống gửi lặp.
        KiemTraYeuCauDatVe(yeuCau, DateOnly.FromDateTime(ThoiDiemHienTai));
        var maBam = BamKhoa(khoaTraCuu);
        var maBamNoiDung = BamChuoi(JsonSerializer.Serialize(new
        {
            FlightId = yeuCau.FlightId,
            FareId = yeuCau.FareId,
            Quantity = yeuCau.Quantity,
            TripType = yeuCau.TripType,
            ReturnDate = yeuCau.ReturnDate,
            ReturnFlightId = yeuCau.ReturnFlightId,
            ReturnFareId = yeuCau.ReturnFareId,
            Customer = yeuCau.Customer,
            Passengers = yeuCau.Passengers,
            DiscountCode = yeuCau.DiscountCode
        }));
        return dao.TrongGiaoDich(duLieu =>
        {
            duLieu.KhoaYeuCau(maBam);
            if (duLieu.TimYeuCau(maBam) is { } truocDo)
            {
                if (truocDo.PayloadHash != maBamNoiDung) throw new LoiDatVe("Biểu mẫu đã được đặt với nội dung khác. Hãy bắt đầu đơn mới.");
                return truocDo.BookingId;
            }
            // 2. Cả hai lượt dùng chung transaction; khóa chuyến theo ID tăng dần.
            KhoaChuyenTheoThuTu(duLieu, new[] { yeuCau.FlightId, yeuCau.ReturnFlightId ?? yeuCau.FlightId });
            var hienTai = ThoiDiemHienTai;
            var hanhTrinh = LapBaoGiaHanhTrinh(duLieu, yeuCau, hienTai, true);
            if (yeuCau.ExpectedTotal != hanhTrinh.TongTien)
                throw new LoiDatVe("Giá hoặc giảm giá vừa thay đổi. Hãy tính lại giá trước khi xác nhận.");
            // 3. Chỉ ghi sau khi giá được tính lại và đối chiếu với giá đã xác nhận.
            int maKhachHang = duLieu.ThemKhachDat(yeuCau.Customer);
            int ma = TaoDatVeMotChang(duLieu, yeuCau, maKhachHang, maBamNoiDung, hienTai, hanhTrinh.LuotDi, maBam);
            if (hanhTrinh.LuotVe is not null)
            {
                TaoDatVeMotChang(duLieu, yeuCau, maKhachHang, maBamNoiDung, hienTai, hanhTrinh.LuotVe, BamKhoa(KhoaLuotVe(khoaTraCuu)));
            }
            return ma;
        });
    }

    private static int TaoDatVeMotChang(DuLieuDatVe duLieu, YeuCauDatVe yeuCau, int maKhachHang,
        string maBamNoiDung, DateTime hienTai, BaoGiaDatVeDTO baoGia, string maBamYeuCau)
    {
        // Hàm nhận context đang có transaction, tuyệt đối không tự mở kết nối/commit lượt riêng.
        HashSet<string> gheHieuLuc = duLieu.DocGheHieuLuc(baoGia.ChuyenBay.MaChuyenBay);
        if (gheHieuLuc.Count != baoGia.ChuyenBay.TongSoGhe - baoGia.ChuyenBay.SoGheCon)
            throw new LoiDatVe("Số liệu ghế không khớp số vé hiệu lực. Liên hệ nhóm quản lý.");
        IReadOnlyList<string> danhSachGhe = CapGhe(baoGia.ChuyenBay.TongSoGhe, gheHieuLuc, yeuCau.Quantity);
        duLieu.GiuGhe(baoGia.ChuyenBay.MaChuyenBay, yeuCau.Quantity);
        if (baoGia.MaGiamGia is int maGiamGia) duLieu.DungMaGiamGia(maGiamGia);
        int maDatVe = duLieu.ThemDatVe(maKhachHang, baoGia, hienTai);
        int maHoaDon = duLieu.ThemHoaDon(maDatVe, baoGia, hienTai);
        for (int i = 0; i < yeuCau.Quantity; i++)
        {
            duLieu.ThemHanhKhach(maDatVe, maHoaDon, baoGia, yeuCau.Passengers[i], danhSachGhe[i], hienTai);
        }
        duLieu.LuuYeuCau(maBamYeuCau, maBamNoiDung, maDatVe);
        return maDatVe;
    }

    private static void KhoaChuyenTheoThuTu(DuLieuDatVe duLieu, IEnumerable<int> danhSachMaChuyenBay)
    {
        List<int> maDaSapXep = new List<int>(danhSachMaChuyenBay);
        maDaSapXep.Sort();
        int? maTruocDo = null;
        foreach (int maChuyenBay in maDaSapXep)
        {
            if (maChuyenBay == maTruocDo) continue;
            duLieu.DocChuyenBay(maChuyenBay, true);
            maTruocDo = maChuyenBay;
        }
    }

    private static string KhoaLuotVe(string khoaTraCuu) => BamChuoi("return:" + khoaTraCuu.ToUpperInvariant());

    public IReadOnlyList<DatVeDTO> TraCuuHanhTrinh(int maDatVe, string khoaTraCuu)
    {
        var maBam = BamKhoa(khoaTraCuu);
        return dao.DocDuLieu(duLieu =>
        {
            var luotDau = duLieu.DocDatVe(maDatVe, maBam);
            var ketQua = new List<DatVeDTO>();
            if (luotDau is null) return ketQua;
            ketQua.Add(luotDau);
            var maBamLuotVe = BamKhoa(KhoaLuotVe(khoaTraCuu));
            if (duLieu.TimYeuCau(maBamLuotVe) is { } luotSau)
                ketQua.Add(duLieu.DocDatVe(luotSau.BookingId, maBamLuotVe) ?? throw new LoiDatVe("Không đọc được lượt về."));
            return ketQua;
        });
    }

    public void HuyHanhTrinh(int maDatVe, string khoaTraCuu)
    {
        var maBam = BamKhoa(khoaTraCuu);
        var duLieuBanDau = TraCuuHanhTrinh(maDatVe, khoaTraCuu);
        if (duLieuBanDau.Count == 0) throw new LoiDatVe("Không tìm thấy hành trình hoặc khóa tra cứu không đúng.");
        dao.TrongGiaoDich(duLieu =>
        {
            duLieu.KhoaYeuCau(maBam);
            List<int> danhSachMaChuyenBay = new List<int>();
            foreach (DatVeDTO datVe in duLieuBanDau) danhSachMaChuyenBay.Add(datVe.MaChuyenBay);
            KhoaChuyenTheoThuTu(duLieu, danhSachMaChuyenBay);

            // Đọc/khóa đủ các đơn trước, kiểm tra đủ các lượt rồi mới hủy bất kỳ lượt nào.
            List<DatVeDTO> danhSachDatVe = new List<DatVeDTO>();
            for (int i = 0; i < duLieuBanDau.Count; i++)
            {
                string maBamDatVe = i == 0 ? maBam : BamKhoa(KhoaLuotVe(khoaTraCuu));
                DatVeDTO? datVe = duLieu.DocDatVe(duLieuBanDau[i].MaDatVe, maBamDatVe, true);
                if (datVe is null) throw new LoiDatVe("Hành trình đã thay đổi.");
                danhSachDatVe.Add(datVe);
            }
            for (var i = 0; i < danhSachDatVe.Count; i++)
            {
                if (danhSachDatVe[i].MaChuyenBay != duLieuBanDau[i].MaChuyenBay) throw new LoiDatVe("Chuyến bay đã thay đổi. Hãy tải lại.");
                if (danhSachDatVe[i].TrangThai != "CANCELLED") KiemTraHuyVe(danhSachDatVe[i], ThoiDiemHienTai);
            }
            foreach (DatVeDTO datVe in danhSachDatVe)
            {
                if (datVe.TrangThai != "CANCELLED") duLieu.HuyDatVe(datVe);
            }
        });
    }

    public DatVeDTO? TraCuuDatVe(int maDatVe, string khoaTraCuu)
    {
        var maBam = BamKhoa(khoaTraCuu);
        return dao.DocDatVe(maDatVe, maBam);
    }

    public void HuyDatVe(int maDatVe, string khoaTraCuu)
    {
        var maBam = BamKhoa(khoaTraCuu);
        var duLieuBanDau = dao.DocDatVe(maDatVe, maBam)
            ?? throw new LoiDatVe("Không tìm thấy đơn hoặc khóa tra cứu không đúng.");
        dao.TrongGiaoDich(duLieu =>
        {
            duLieu.KhoaYeuCau(maBam);
            duLieu.DocChuyenBay(duLieuBanDau.MaChuyenBay, true);
            var datVe = duLieu.DocDatVe(maDatVe, maBam, true)
                ?? throw new LoiDatVe("Không tìm thấy đơn hoặc khóa tra cứu không đúng.");
            if (datVe.MaChuyenBay != duLieuBanDau.MaChuyenBay) throw new LoiDatVe("Chuyến bay của đơn đã thay đổi. Vui lòng tải lại.");
            if (datVe.TrangThai == "CANCELLED") return;
            KiemTraHuyVe(datVe, ThoiDiemHienTai);
            duLieu.HuyDatVe(datVe);
        });
    }

    public static void KiemTraHuyVe(DatVeDTO datVe, DateTime hienTai)
    {
        if (datVe.TrangThai != "CONFIRMED" || datVe.TrangThaiHoaDon != "UNPAID")
            throw new LoiDatVe("Chỉ hủy trực tuyến đơn chưa thanh toán. Đơn đã thanh toán cần liên hệ nhân viên xử lý hoàn tiền.");
        if (datVe.ChuyenBay.ThoiDiemKhoiHanh <= hienTai) throw new LoiDatVe("Không thể hủy sau giờ khởi hành.");
    }

    public static IReadOnlyList<string> CapGhe(int tongSoGhe, HashSet<string> gheHieuLuc, int soLuong)
    {
        // No aircraft layout exists in the schema. Auto-assign six seats per row (A..F).
        var ketQua = new List<string>();
        for (var n = 0; n < tongSoGhe && ketQua.Count < soLuong; n++)
        {
            var ghe = $"{n / 6 + 1}{(char)('A' + n % 6)}";
            if (!gheHieuLuc.Contains(ghe)) ketQua.Add(ghe);
        }
        if (ketQua.Count != soLuong) throw new LoiDatVe("Không đủ ghế để cấp vé. Vui lòng liên hệ nhân viên.");
        return ketQua;
    }

    public static void DieuChinhSoHanhKhach(YeuCauDatVe yeuCau, int soLuong)
    {
        if (soLuong is < 1 or > 9) throw new LoiDatVe("Chọn từ 1 đến 9 hành khách.");
        while (yeuCau.Passengers.Count < soLuong) yeuCau.Passengers.Add(new ThongTinHanhKhach());
        if (yeuCau.Passengers.Count > soLuong) yeuCau.Passengers.RemoveRange(soLuong, yeuCau.Passengers.Count - soLuong);
        yeuCau.Quantity = soLuong;
    }

    public static void KiemTraYeuCauDatVe(YeuCauDatVe yeuCau, DateOnly homNay)
    {
        if (yeuCau.Customer is null || yeuCau.Passengers is null)
            throw new LoiDatVe("Thiếu thông tin khách đặt hoặc hành khách.");
        foreach (ThongTinHanhKhach hanhKhach in yeuCau.Passengers)
        {
            if (hanhKhach is null) throw new LoiDatVe("Thiếu thông tin khách đặt hoặc hành khách.");
        }
        ChuanHoaHanhTrinh(yeuCau);
        if (yeuCau.Passengers.Count != yeuCau.Quantity) throw new LoiDatVe("Số hành khách không khớp số vé đặt.");
        if (yeuCau.Customer.Gender == "Nữ") yeuCau.Customer.Gender = "Nu";
        yeuCau.Customer.FullName = (yeuCau.Customer.FullName ?? "").Trim();
        yeuCau.Customer.Phone = (yeuCau.Customer.Phone ?? "").Trim();
        yeuCau.Customer.Email = (yeuCau.Customer.Email ?? "").Trim().ToLowerInvariant();
        yeuCau.Customer.IdPassportNumber = yeuCau.Customer.IdPassportNumber?.Trim();
        yeuCau.DiscountCode = ChuanHoaMa(yeuCau.DiscountCode);
        foreach (var hanhKhach in yeuCau.Passengers)
        {
            hanhKhach.FullName = (hanhKhach.FullName ?? "").Trim(); hanhKhach.IdCard = (hanhKhach.IdCard ?? "").Trim().ToUpperInvariant();
        }
        var loiKiemTra = new List<ValidationResult>();
        Validator.TryValidateObject(yeuCau, new ValidationContext(yeuCau), loiKiemTra, true);
        Validator.TryValidateObject(yeuCau.Customer, new ValidationContext(yeuCau.Customer), loiKiemTra, true);
        foreach (ThongTinHanhKhach hanhKhach in yeuCau.Passengers)
        {
            Validator.TryValidateObject(hanhKhach, new ValidationContext(hanhKhach), loiKiemTra, true);
        }
        if (loiKiemTra.Count > 0) throw new LoiDatVe(loiKiemTra[0].ErrorMessage ?? "Thông tin không hợp lệ.");
        if (yeuCau.FlightId < 1 || yeuCau.FareId < 1 || yeuCau.Passengers.Count is < 1 or > 9)
            throw new LoiDatVe("Chọn chuyến bay, hạng vé và 1–9 hành khách.");
        if (LoiNgaySinhKhachDat(yeuCau.Customer.DateOfBirth, homNay) is { } loiNgaySinh)
            throw new LoiDatVe(loiNgaySinh);
        if (yeuCau.Customer.Gender is not ("Nam" or "Nu" or "Khác")) throw new LoiDatVe("Giới tính không hợp lệ.");
        foreach (char character in yeuCau.Customer.Email)
        {
            if (character > 127) throw new LoiDatVe("Email cần dùng ký tự Latin theo cấu trúc database hiện có.");
        }
        HashSet<string> documents = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (ThongTinHanhKhach hanhKhach in yeuCau.Passengers)
        {
            if (!documents.Add(hanhKhach.IdCard))
                throw new LoiDatVe("Giấy tờ các hành khách trong cùng đơn không được trùng.");
        }
        if (yeuCau.ExpectedTotal < 0 || yeuCau.ExpectedTotal > 9999999999999999.99m)
            throw new LoiDatVe("Tổng tiền không hợp lệ.");
    }

    public static string ChuanHoaMa(string? maGiamGia)
    {
        var ketQua = (maGiamGia ?? "").Trim().ToUpperInvariant();
        if (ketQua.Length > 50 || ketQua.Any(lenhSQL => !char.IsAsciiLetterOrDigit(lenhSQL) && lenhSQL is not ('_' or '-')))
            throw new LoiDatVe("Mã giảm giá không hợp lệ.");
        return ketQua;
    }
    public static string TaoKhoaTraCuu() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static string BamKhoa(string khoa)
    {
        if (khoa is null || khoa.Length != 64 || khoa.Any(lenhSQL => !char.IsAsciiHexDigit(lenhSQL)))
            throw new LoiDatVe("Khóa tra cứu cần đủ 64 ký tự được cấp khi đặt vé.");
        return BamChuoi(khoa.ToUpperInvariant());
    }
    private static string BamChuoi(string chuoi) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(chuoi)));
}
