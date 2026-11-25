using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using FlightBooking.DAO;
using FlightBooking.DTO;

namespace FlightBooking.BUS;

public sealed class QuanLyBUS
{
    private readonly QuanLyDAO dao;

    public QuanLyBUS(string chuoiKetNoi)
    {
        dao = new QuanLyDAO(chuoiKetNoi);
    }
    public ArrayList DocDSSanBay()
    {
        return new ArrayList(dao.DocDSSanBay());
    }

    public ArrayList DocDSChuyenBay()
    {
        return new ArrayList(dao.DocDSChuyenBay());
    }

    public ArrayList DocDSHangVe(int? maChuyenBay = null)
    {
        return new ArrayList(dao.DocDSHangVe(maChuyenBay));
    }

    public static SanBayDTO KiemTraSanBay(SanBayDTO dauVao)
    {
        string tenSanBay = (dauVao.TenSanBay ?? "").Trim();
        if (tenSanBay.Length == 0 || tenSanBay.Length > 150)
        {
            throw new LoiQuanLy("Tên sân bay bắt buộc, tối đa 150 ký tự.");
        }
        string thanhPho = (dauVao.ThanhPho ?? "").Trim();
        if (thanhPho.Length == 0 || thanhPho.Length > 100)
        {
            throw new LoiQuanLy("Thành phố bắt buộc, tối đa 100 ký tự.");
        }
        return new SanBayDTO(dauVao.MaSanBay, tenSanBay, thanhPho);
    }

    public int LuuSanBay(SanBayDTO dauVao, SanBayDTO? duLieuGoc = null)
    {
        // Giữ API cũ cho các nơi đang gọi Save; mỗi nghiệp vụ có hàm riêng bên dưới.
        if (dauVao.MaSanBay == 0) return ThemSanBay(dauVao);
        return SuaSanBay(dauVao, duLieuGoc);
    }

    public int ThemSanBay(SanBayDTO dauVao)
    {
        // 1. Chuẩn hóa và kiểm tra DTO trước khi truy cập SQL Server.
        SanBayDTO sanBay = KiemTraSanBay(dauVao);
        if (sanBay.MaSanBay != 0)
        {
            throw new LoiQuanLy("Mã sân bay không hợp lệ.");
        }
        return dao.TrongGiaoDich(duLieu =>
        {
            // 2. Kiểm tra trùng và thêm trong cùng transaction để tránh hai người thêm trùng.
            if (duLieu.SanBayDaTonTai(sanBay))
            {
                throw new LoiQuanLy("Đã có sân bay cùng tên và thành phố.");
            }
            // 3. DAO INSERT và trả ID mới; DAO chỉ trả kết quả sau khi commit thành công.
            return duLieu.ThemSanBay(sanBay);
        });
    }

    public int SuaSanBay(SanBayDTO dauVao, SanBayDTO? duLieuGoc)
    {
        SanBayDTO sanBay = KiemTraSanBay(dauVao);
        if (sanBay.MaSanBay <= 0)
        {
            throw new LoiQuanLy("Mã sân bay không hợp lệ.");
        }
        return dao.TrongGiaoDich(duLieu =>
        {
            // Khóa và so sánh với dòng đã chọn, không ghi đè thay đổi của người khác.
            if (duLieuGoc == null || duLieu.DocSanBay(sanBay.MaSanBay, true) != duLieuGoc)
            {
                throw new LoiQuanLy("Sân bay đã thay đổi. Hãy tải lại rồi chọn lại dòng.");
            }
            if (duLieu.SanBayDaTonTai(sanBay))
            {
                throw new LoiQuanLy("Đã có sân bay cùng tên và thành phố.");
            }
            return duLieu.SuaSanBay(sanBay);
        });
    }

    public void XoaSanBay(SanBayDTO duLieuGoc)
    {
        dao.TrongGiaoDich(duLieu =>
        {
            if (duLieu.DocSanBay(duLieuGoc.MaSanBay, true) != duLieuGoc)
            {
                throw new LoiQuanLy("Sân bay đã thay đổi. Hãy tải lại.");
            }
            if (duLieu.SanBayDaDuocDung(duLieuGoc.MaSanBay))
            {
                throw new LoiQuanLy("Không xóa sân bay đang được chuyến bay sử dụng.");
            }
            duLieu.XoaSanBay(duLieuGoc.MaSanBay);
        });
    }

    public static void KiemTraChuyenBay(ThongTinSuaChuyenBay chuyenBay)
    {
        if (chuyenBay.MaChuyenBay < 0 || chuyenBay.MaSanBayDi <= 0 || chuyenBay.MaSanBayDen <= 0 ||
            chuyenBay.MaSanBayDi == chuyenBay.MaSanBayDen)
        {
            throw new LoiQuanLy("Chọn hai sân bay đi/đến khác nhau.");
        }
        if (chuyenBay.TongSoGhe <= 0)
        {
            throw new LoiQuanLy("Tổng số ghế phải lớn hơn 0.");
        }
        DateTime gioKhoiHanh = chuyenBay.NgayKhoiHanh.ToDateTime(chuyenBay.GioKhoiHanh);
        DateTime gioDen = chuyenBay.NgayDen.ToDateTime(chuyenBay.GioDen);
        if (gioDen <= gioKhoiHanh)
        {
            throw new LoiQuanLy("Ngày giờ đến phải sau ngày giờ đi.");
        }
        if (chuyenBay.TrangThai != "SCHEDULED" && chuyenBay.TrangThai != "COMPLETED" && chuyenBay.TrangThai != "CANCELLED")
        {
            throw new LoiQuanLy("Trạng thái chuyến bay không hợp lệ.");
        }
    }

    public static void KiemTraThayDoiChuyenBay(ThongTinSuaChuyenBay chuyenBay, QuanLyChuyenBayDTO duLieuCu,
        bool daCoDatVe, IReadOnlyList<string> gheHieuLuc)
    {
        KiemTraChuyenBay(chuyenBay);
        ChuyenBayDTO duLieuGoc = duLieuCu.ChuyenBay;
        bool boDemKhop = duLieuCu.SoVeDaBan >= 0 && duLieuGoc.SoGheCon >= 0 &&
            duLieuGoc.TongSoGhe == duLieuCu.SoVeDaBan + duLieuGoc.SoGheCon && duLieuCu.SoVeDaBan == gheHieuLuc.Count;
        if (!boDemKhop)
        {
            throw new LoiQuanLy("Số ghế và vé hiệu lực không khớp. Cần kiểm tra dữ liệu trước khi sửa.");
        }

        if (daCoDatVe)
        {
            bool tuyenKhongDoi = chuyenBay.MaSanBayDi == duLieuGoc.MaSanBayDi &&
                chuyenBay.MaSanBayDen == duLieuGoc.MaSanBayDen;
            bool lichKhongDoi = chuyenBay.NgayKhoiHanh == duLieuGoc.NgayKhoiHanh &&
                chuyenBay.GioKhoiHanh == duLieuGoc.GioKhoiHanh && chuyenBay.NgayDen == duLieuGoc.NgayDen &&
                chuyenBay.GioDen == duLieuGoc.GioDen;
            if (!tuyenKhongDoi || !lichKhongDoi || chuyenBay.TrangThai != duLieuGoc.TrangThai)
            {
                throw new LoiQuanLy("Chuyến đã có Booking/vé: không đổi tuyến, lịch hoặc trạng thái. Cần quy trình xử lý vé riêng.");
            }
        }
        if (chuyenBay.TongSoGhe < duLieuCu.SoVeDaBan)
        {
            throw new LoiQuanLy("Tổng ghế không được nhỏ hơn số vé hiệu lực.");
        }

        if (chuyenBay.TongSoGhe < duLieuGoc.TongSoGhe)
        {
            foreach (string ghe in gheHieuLuc)
            {
                Match ketQuaKhop = Regex.Match(ghe, @"^([1-9][0-9]*)([A-F])$", RegexOptions.IgnoreCase);
                if (!ketQuaKhop.Success || !int.TryParse(ketQuaKhop.Groups[1].Value, out int dong) || dong > (int.MaxValue - 6) / 6)
                    throw new LoiQuanLy("Không xác định được vị trí ghế đã cấp; chưa thể giảm tổng ghế.");

                // Sáu ghế A–F mỗi hàng, cùng quy ước cấp ghế của Booking.
                long viTriGhe = ((long)dong - 1) * 6 + char.ToUpperInvariant(ketQuaKhop.Groups[2].Value[0]) - 'A' + 1;
                if (viTriGhe > chuyenBay.TongSoGhe)
                {
                    throw new LoiQuanLy($"Không thể giảm tổng ghế vì ghế {ghe} đã được cấp.");
                }
            }
        }
    }

    public int LuuChuyenBay(ThongTinSuaChuyenBay chuyenBay, QuanLyChuyenBayDTO? duLieuGoc = null)
    {
        if (chuyenBay.MaChuyenBay == 0) return ThemChuyenBay(chuyenBay);
        return SuaChuyenBay(chuyenBay, duLieuGoc);
    }

    public int ThemChuyenBay(ThongTinSuaChuyenBay chuyenBay)
    {
        KiemTraChuyenBay(chuyenBay);
        if (chuyenBay.MaChuyenBay != 0)
        {
            throw new LoiQuanLy("Mã chuyến bay phải bằng 0 khi thêm mới.");
        }
        return dao.TrongGiaoDich(duLieu =>
        {
            if (duLieu.DocSanBay(chuyenBay.MaSanBayDi) == null || duLieu.DocSanBay(chuyenBay.MaSanBayDen) == null)
            {
                throw new LoiQuanLy("Sân bay không tồn tại.");
            }
            // Chuyến mới chưa bán vé; DAO khởi tạo ghế còn và doanh thu đúng nghiệp vụ.
            return duLieu.ThemChuyenBay(chuyenBay, 0);
        });
    }

    public int SuaChuyenBay(ThongTinSuaChuyenBay chuyenBay, QuanLyChuyenBayDTO? duLieuGoc)
    {
        // 1. Kiểm tra tuyến, lịch, tổng ghế và trạng thái của DTO.
        KiemTraChuyenBay(chuyenBay);
        if (chuyenBay.MaChuyenBay == 0)
        {
            throw new LoiQuanLy("Mã chuyến bay phải lớn hơn 0 khi chỉnh sửa.");
        }
        return dao.TrongGiaoDich(duLieu =>
        {
            // 2. Khóa chuyến và kiểm tra snapshot trước khi đọc vé/ghế liên quan.
            QuanLyChuyenBayDTO? hienTai = duLieu.DocChuyenBay(chuyenBay.MaChuyenBay, true);
            if (hienTai == null || duLieuGoc == null || hienTai != duLieuGoc)
            {
                throw new LoiQuanLy("Chuyến bay/số ghế đã thay đổi. Hãy tải lại và chọn lại dòng.");
            }
            // 3. Đối chiếu Booking, bộ đếm và vị trí ghế đã cấp trong chính transaction này.
            bool daCoDatVe = duLieu.ChuyenBayDaCoDatVe(chuyenBay.MaChuyenBay);
            List<string> gheHieuLuc = duLieu.DocGheHieuLuc(chuyenBay.MaChuyenBay);
            KiemTraThayDoiChuyenBay(chuyenBay, hienTai, daCoDatVe, gheHieuLuc);
            if (duLieu.DocSanBay(chuyenBay.MaSanBayDi) == null || duLieu.DocSanBay(chuyenBay.MaSanBayDen) == null)
            {
                throw new LoiQuanLy("Sân bay không tồn tại.");
            }
            // 4. Chỉ cập nhật sau khi tất cả điều kiện hợp lệ; không sửa giá lịch sử/doanh thu.
            return duLieu.SuaChuyenBay(chuyenBay, hienTai.SoVeDaBan);
        });
    }

    public void XoaChuyenBay(QuanLyChuyenBayDTO duLieuGoc)
    {
        dao.TrongGiaoDich(duLieu =>
        {
            int maChuyenBay = duLieuGoc.ChuyenBay.MaChuyenBay;
            if (duLieu.DocChuyenBay(maChuyenBay, true) != duLieuGoc)
            {
                throw new LoiQuanLy("Chuyến bay đã thay đổi. Hãy tải lại.");
            }
            if (duLieu.ChuyenBayDaCoDatVe(maChuyenBay) || duLieu.ChuyenBayDaCoHangVe(maChuyenBay))
            {
                throw new LoiQuanLy("Không xóa chuyến đã có hạng vé, Booking hoặc vé. Không tự xóa dữ liệu liên quan.");
            }
            duLieu.XoaChuyenBay(maChuyenBay);
        });
    }

    public static HangVeDTO KiemTraHangVe(HangVeDTO dauVao)
    {
        if (dauVao.MaHangVe < 0 || dauVao.MaChuyenBay <= 0)
        {
            throw new LoiQuanLy("Chọn chuyến bay hợp lệ.");
        }
        if (dauVao.GiaVe < 0 || dauVao.GiaVe > 9999999999999999.99m || decimal.Round(dauVao.GiaVe, 2) != dauVao.GiaVe)
        {
            throw new LoiQuanLy("Giá vé phải không âm, tối đa 2 chữ số thập phân và trong giới hạn SQL.");
        }
        if (dauVao.HanhLyCoBanKg < 0)
        {
            throw new LoiQuanLy("Hành lý phải không âm.");
        }
        string tenHangVe = (dauVao.TenHangVe ?? "").Trim();
        if (tenHangVe.Length == 0 || tenHangVe.Length > 50)
        {
            throw new LoiQuanLy("Tên hạng vé bắt buộc, tối đa 50 ký tự.");
        }
        return new HangVeDTO(dauVao.MaHangVe, dauVao.MaChuyenBay, tenHangVe, dauVao.GiaVe, dauVao.HanhLyCoBanKg);
    }

    public int LuuHangVe(HangVeDTO dauVao, HangVeDTO? duLieuGoc = null)
    {
        HangVeDTO hangVe = KiemTraHangVe(dauVao);
        // Hạng vé có thể chuyển chuyến: giữ chung kiểm tra/khóa hai chuyến cho cả thêm và sửa.
        return dao.TrongGiaoDich(duLieu =>
        {
            int maChuyenDau = hangVe.MaChuyenBay;
            if (duLieuGoc != null) maChuyenDau = duLieuGoc.MaChuyenBay;
            int maChuyenSau = hangVe.MaChuyenBay;
            // Khóa theo ID tăng dần như Booking để hạn chế deadlock; không khóa lặp một chuyến.
            if (maChuyenDau > maChuyenSau)
            {
                int maTam = maChuyenDau;
                maChuyenDau = maChuyenSau;
                maChuyenSau = maTam;
            }
            if (duLieu.DocChuyenBay(maChuyenDau, true) == null)
            {
                throw new LoiQuanLy("Chuyến bay không tồn tại.");
            }
            if (maChuyenSau != maChuyenDau)
            {
                if (duLieu.DocChuyenBay(maChuyenSau, true) == null)
                {
                    throw new LoiQuanLy("Chuyến bay không tồn tại.");
                }
            }

            if (hangVe.MaHangVe > 0)
            {
                if (duLieuGoc == null || duLieu.DocHangVe(hangVe.MaHangVe) != duLieuGoc)
                    throw new LoiQuanLy("Hạng vé đã thay đổi. Hãy tải lại.");
                if (duLieuGoc.MaChuyenBay != hangVe.MaChuyenBay && duLieu.HangVeDaDuocDung(hangVe.MaHangVe))
                {
                    throw new LoiQuanLy("Không chuyển hạng đã có vé sang chuyến khác.");
                }
            }
            if (duLieu.HangVeDaTonTai(hangVe))
            {
                throw new LoiQuanLy("Tên hạng vé đã tồn tại trên chuyến này.");
            }
            return duLieu.LuuHangVe(hangVe);
        });
    }

    public void XoaHangVe(HangVeDTO duLieuGoc)
    {
        dao.TrongGiaoDich(duLieu =>
        {
            // 1. Khóa chuyến, xác minh hạng vẫn giống snapshot đã chọn.
            if (duLieu.DocChuyenBay(duLieuGoc.MaChuyenBay, true) == null || duLieu.DocHangVe(duLieuGoc.MaHangVe) != duLieuGoc)
            {
                throw new LoiQuanLy("Hạng vé đã thay đổi. Hãy tải lại.");
            }
            // 2. Hạng đã có vé tham chiếu không được xóa.
            if (duLieu.HangVeDaDuocDung(duLieuGoc.MaHangVe))
            {
                throw new LoiQuanLy("Không xóa hạng vé đã được vé tham chiếu.");
            }
            // 3. DAO xóa và commit; lỗi ở bất kỳ bước nào sẽ không trả thành công.
            duLieu.XoaHangVe(duLieuGoc.MaHangVe);
        });
    }
}

public static class TimKiemQuanLy
{
    public static readonly string[] TruongSanBay = { "Mã", "Tên", "Thành phố" };
    public static readonly string[] TruongChuyenBay =
    {
        "Mã", "Nơi đi", "Nơi đến", "Ngày đi", "Ngày đến", "Tổng ghế",
        "Đã bán", "Còn ghế", "Doanh thu", "Trạng thái"
    };
    public static readonly string[] TruongHangVe = { "Mã", "Chuyến", "Tên", "Giá", "Hành lý" };

    private static object LayGiaTri(object? phanTu, string truong)
    {
        if (phanTu is SanBayDTO sanBay)
        {
            switch (truong)
            {
                case "Mã": return sanBay.MaSanBay;
                case "Tên": return sanBay.TenSanBay;
                case "Thành phố": return sanBay.ThanhPho;
                default: throw new LoiQuanLy("Trường lọc không hợp lệ.");
            }
        }
        if (phanTu is QuanLyChuyenBayDTO chuyenBayQuanLy)
        {
            ChuyenBayDTO chuyenBay = chuyenBayQuanLy.ChuyenBay;
            switch (truong)
            {
                case "Mã": return chuyenBay.MaChuyenBay;
                case "Nơi đi": return chuyenBay.ThanhPhoDi;
                case "Nơi đến": return chuyenBay.ThanhPhoDen;
                case "Ngày đi": return chuyenBay.NgayKhoiHanh;
                case "Ngày đến": return chuyenBay.NgayDen;
                case "Tổng ghế": return chuyenBay.TongSoGhe;
                case "Đã bán": return chuyenBayQuanLy.SoVeDaBan;
                case "Còn ghế": return chuyenBay.SoGheCon;
                case "Doanh thu": return chuyenBayQuanLy.TongDoanhThu;
                case "Trạng thái": return chuyenBay.TrangThai;
                default: throw new LoiQuanLy("Trường lọc không hợp lệ.");
            }
        }
        if (phanTu is HangVeDTO hangVe)
        {
            switch (truong)
            {
                case "Mã": return hangVe.MaHangVe;
                case "Chuyến": return hangVe.MaChuyenBay;
                case "Tên": return hangVe.TenHangVe;
                case "Giá": return hangVe.GiaVe;
                case "Hành lý": return hangVe.HanhLyCoBanKg;
                default: throw new LoiQuanLy("Trường lọc không hợp lệ.");
            }
        }
        throw new LoiQuanLy("Loại dữ liệu không hợp lệ.");
    }

    public static ArrayList LocDanhSach(ArrayList nguon, IReadOnlyList<DieuKienLoc> danhSachDieuKien,
        string phepKetHop = "AND", bool daoKetQua = false)
    {
        if (phepKetHop != "AND" && phepKetHop != "OR")
            throw new LoiQuanLy("Chọn AND hoặc OR.");

        // Chỉ dùng điều kiện có nhập giá trị; không sửa danh sách dữ liệu gốc.
        List<DieuKienLoc> dieuKienLoc = new List<DieuKienLoc>();
        foreach (DieuKienLoc dieuKien in danhSachDieuKien)
        {
            if (string.IsNullOrWhiteSpace(dieuKien.GiaTri)) continue;
            switch (dieuKien.PhepSoSanh)
            {
                case "=":
                case "<>":
                case ">":
                case ">=":
                case "<":
                case "<=":
                case "chứa":
                    dieuKienLoc.Add(dieuKien);
                    break;
                default:
                    throw new LoiQuanLy("Phép so sánh không hợp lệ.");
            }
        }

        ArrayList ketQua = new ArrayList();
        foreach (object? phanTu in nguon)
        {
            bool khop = phepKetHop == "AND";
            foreach (DieuKienLoc dieuKien in dieuKienLoc)
            {
                object giaTriThuc = LayGiaTri(phanTu, dieuKien.Truong);
                bool khopDieuKien = KhopDieuKien(giaTriThuc, dieuKien);

                // Vẫn kiểm tra mọi điều kiện để không bỏ qua giá trị nhập sai ở điều kiện sau.
                if (phepKetHop == "AND")
                {
                    if (!khopDieuKien) khop = false;
                }
                else
                {
                    if (khopDieuKien) khop = true;
                }
            }
            if (dieuKienLoc.Count == 0) khop = true;
            if (dieuKienLoc.Count > 0 && daoKetQua) khop = !khop;
            if (khop) ketQua.Add(phanTu);
        }
        return ketQua;
    }

    private static bool KhopDieuKien(object giaTri, DieuKienLoc dieuKien)
    {
        string chuoi = dieuKien.GiaTri.Trim();
        int soSanh;
        if (giaTri is string chuoiThuc)
        {
            if (dieuKien.Truong == "Trạng thái")
            {
                // Tìm theo nhãn Việt đang thấy trên ListView hoặc theo mã lưu trong SQL.
                string nhan = chuoiThuc;
                if (chuoiThuc == "SCHEDULED") nhan = "Mở bán";
                else if (chuoiThuc == "COMPLETED") nhan = "Đã hoàn thành";
                else if (chuoiThuc == "CANCELLED") nhan = "Đã hủy";

                if (dieuKien.PhepSoSanh == "chứa")
                    return nhan.Contains(chuoi, StringComparison.OrdinalIgnoreCase) ||
                        chuoiThuc.Contains(chuoi, StringComparison.OrdinalIgnoreCase);

                string chuoiVietHoa = chuoi.ToUpperInvariant();
                if (chuoiVietHoa == "MỞ BÁN") chuoi = "SCHEDULED";
                else if (chuoiVietHoa == "ĐÃ HOÀN THÀNH") chuoi = "COMPLETED";
                else if (chuoiVietHoa == "ĐÃ HỦY") chuoi = "CANCELLED";
            }
            if (dieuKien.PhepSoSanh == "chứa")
                return chuoiThuc.Contains(chuoi, StringComparison.OrdinalIgnoreCase);
            soSanh = string.Compare(chuoiThuc, chuoi, StringComparison.OrdinalIgnoreCase);
        }
        else if (giaTri is DateOnly ngay)
        {
            string[] dinhDangNgay = { "dd/MM/yyyy", "yyyy-MM-dd" };
            if (!DateOnly.TryParseExact(chuoi, dinhDangNgay, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly giaTriCanSoSanh))
                throw new LoiQuanLy("Ngày lọc dùng dd/MM/yyyy hoặc yyyy-MM-dd.");
            soSanh = ngay.CompareTo(giaTriCanSoSanh);
        }
        else
        {
            // Không để TryParse bỏ dấu phân nhóm sai, ví dụ đọc 1.5 thành 15.
            bool dungDinhDang = Regex.IsMatch(chuoi, @"^[+-]?(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]+)?$");
            if (!dungDinhDang || !decimal.TryParse(chuoi, NumberStyles.Number,
                    CultureInfo.GetCultureInfo("vi-VN"), out decimal so))
                throw new LoiQuanLy("Giá trị lọc phải là số, ví dụ 1.000.000 hoặc 1,5.");
            soSanh = Convert.ToDecimal(giaTri).CompareTo(so);
        }

        switch (dieuKien.PhepSoSanh)
        {
            case "=": return soSanh == 0;
            case "<>": return soSanh != 0;
            case ">": return soSanh > 0;
            case ">=": return soSanh >= 0;
            case "<": return soSanh < 0;
            case "<=": return soSanh <= 0;
            default: throw new LoiQuanLy("Phép chứa chỉ dùng cho văn bản.");
        }
    }
}
