using FlightBooking.DAO;
using FlightBooking.DTO;

namespace FlightBooking.BUS;

public sealed class ThongKeBUS
{
    private readonly ThongKeDAO dao = new();

    public static void KiemTraBoLoc(BoLocThongKeDTO boLoc)
    {
        if (boLoc.TuNgay.HasValue && boLoc.DenNgay.HasValue && boLoc.TuNgay.Value.Date > boLoc.DenNgay.Value.Date)
            throw new ArgumentException("Từ ngày không được sau đến ngày.");
        if (boLoc.DenNgay?.Date == DateTime.MaxValue.Date)
            throw new ArgumentException("Đến ngày phải trước 31/12/9999.");
        if (boLoc.Nam.HasValue && (boLoc.Nam < 1 || boLoc.Nam > 9998))
            throw new ArgumentException("Năm phải từ 1 đến 9998.");
        if (boLoc.Thang.HasValue && (boLoc.Thang < 1 || boLoc.Thang > 12))
            throw new ArgumentException("Tháng phải từ 1 đến 12.");
        if (boLoc.Quy.HasValue && (boLoc.Quy < 1 || boLoc.Quy > 4))
            throw new ArgumentException("Quý phải từ 1 đến 4.");
        if (boLoc.Thang.HasValue && boLoc.Quy.HasValue && (boLoc.Thang.Value - 1) / 3 + 1 != boLoc.Quy.Value)
            throw new ArgumentException("Tháng không thuộc quý đã chọn.");
    }

    public ThongKeDoanhThuDTO ThongKeTongHop(BoLocThongKeDTO? boLoc = null)
    {
        boLoc ??= new BoLocThongKeDTO();
        KiemTraBoLoc(boLoc);
        var ketQua = dao.ThongKeTongHop(boLoc);
        // Tổng thu là PAID. Chưa có nguồn chi phí, không tự đặt chi phí bằng 0.
        ketQua.TongDoanhThu = ketQua.DoanhThuDaThu;
        ketQua.LoiNhuan = ketQua.TongChi.HasValue ? ketQua.DoanhThuDaThu - ketQua.TongChi.Value : null;
        ketQua.TheoNam = TongHopTheoNam(ketQua.TheoThoiGian);
        return ketQua;
    }

    public static List<DoanhThuTheoKyDTO> TongHopTheoNam(IEnumerable<DoanhThuTheoKyDTO> cacThang)
    {
        var cacNam = new SortedDictionary<int, DoanhThuTheoKyDTO>();
        foreach (var thang in cacThang)
        {
            if (!cacNam.TryGetValue(thang.Nam, out var nam))
            {
                nam = new DoanhThuTheoKyDTO { Nam = thang.Nam };
                cacNam.Add(thang.Nam, nam);
            }
            nam.SoHoaDon += thang.SoHoaDon;
            nam.SoVe += thang.SoVe;
            nam.DoanhThu += thang.DoanhThu;
        }
        return new List<DoanhThuTheoKyDTO>(cacNam.Values);
    }

    public static MaTranDoanhThuDTO TinhDongTong(IEnumerable<MaTranDoanhThuDTO> cacDong)
    {
        var tong = new MaTranDoanhThuDTO { Ten = "TỔNG" };
        foreach (var dong in cacDong)
        {
            tong.Quy1 += dong.Quy1;
            tong.Quy2 += dong.Quy2;
            tong.Quy3 += dong.Quy3;
            tong.Quy4 += dong.Quy4;
            tong.Tong += dong.Tong;
        }
        return tong;
    }
}
