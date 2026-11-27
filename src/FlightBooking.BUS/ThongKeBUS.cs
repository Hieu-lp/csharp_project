using FlightBooking.DAO;
using FlightBooking.DTO;

namespace FlightBooking.BUS;

public sealed class ThongKeBUS
{
    private readonly ThongKeDAO dao = new();

    public static List<DiemBieuDoDTO> CotTheoThang(ThongKeDoanhThuDTO data, BoLocThongKeDTO loc)
    {
        KiemTraBoLoc(loc);
        var tong=new SortedDictionary<DateTime,decimal>();
        foreach(var m in data.TheoThoiGian){var k=new DateTime(m.Nam,m.Thang,1);tong[k]=tong.GetValueOrDefault(k)+m.DoanhThu;}
        DateTime? tu=loc.TuNgay ?? (loc.Nam.HasValue?new DateTime(loc.Nam.Value,1,1):tong.Count>0?tong.Keys.First():null);
        DateTime? den=loc.DenNgay ?? (loc.Nam.HasValue?new DateTime(loc.Nam.Value,12,1):tong.Count>0?tong.Keys.Last():tu);
        var kq=new List<DiemBieuDoDTO>();if(tu==null || den==null)return kq;
        for(var k=new DateTime(tu.Value.Year,tu.Value.Month,1);k<=den.Value;k=k.AddMonths(1))
        {
            if(kq.Count>=1200)throw new ArgumentException("Thu hẹp biểu đồ dưới 100 năm.");
            if(loc.Nam.HasValue&&k.Year!=loc.Nam || loc.Thang.HasValue&&k.Month!=loc.Thang || loc.Quy.HasValue&&(k.Month-1)/3+1!=loc.Quy)continue;
            kq.Add(new(k.ToString("MM/yyyy"),tong.GetValueOrDefault(k),0));
            if(k.Year==9999&&k.Month==12)break;
        }
        return kq;
    }
    public static List<DiemBieuDoDTO> CotTheoQuy(ThongKeDoanhThuDTO data, BoLocThongKeDTO loc)
    {
        var nhom=new Dictionary<string,decimal>();
        foreach(var x in CotTheoThang(data,loc))
        {
            var ngay=DateTime.ParseExact(x.Nhan,"MM/yyyy",System.Globalization.CultureInfo.InvariantCulture);
            string key=$"Q{(ngay.Month-1)/3+1}/{ngay.Year}";
            nhom[key]=nhom.GetValueOrDefault(key)+x.GiaTri;
        }
        var kq=new List<DiemBieuDoDTO>();foreach(var x in nhom)kq.Add(new(x.Key,x.Value,0));return kq;
    }

    public static List<DiemBieuDoDTO> TronTheoHangVe(IEnumerable<MaTranDoanhThuDTO> rows)
    {
        var nhom=new Dictionary<string,decimal>();
        foreach(var r in rows){if(r.Tong<0)throw new ArgumentException("Không dùng biểu đồ tròn cho doanh thu âm.");nhom[r.Ten]=nhom.GetValueOrDefault(r.Ten)+r.Tong;}
        decimal tong=nhom.Values.Sum();var kq=new List<DiemBieuDoDTO>();if(tong==0)return kq;
        decimal khac=0;int n=0;
        foreach(var x in nhom.OrderByDescending(x=>x.Value).ThenBy(x=>x.Key,StringComparer.Ordinal))
        { if(x.Value==0)continue;if(n++<6)kq.Add(new(x.Key,x.Value,x.Value/tong*100));else khac+=x.Value; }
        if(khac>0)kq.Add(new("Khác",khac,khac/tong*100));return kq;
    }

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
