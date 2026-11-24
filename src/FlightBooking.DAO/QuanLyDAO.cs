using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class QuanLyDAO
{
    private readonly MyConnection ketNoi;

    public QuanLyDAO(string chuoiKetNoi)
    {
        ketNoi = new MyConnection(chuoiKetNoi);
    }

    private const string ThongBaoLoiKetNoi = "Chưa cấu hình hoặc chưa kết nối được FlightBookingDB.";

    // Exception dùng chung ở DTO; DAO không tham chiếu ngược tới BUS hoặc GUI.
    private static LoiQuanLy ChuyenLoiSQL(SqlException loi)
    {
        if (loi.Number == 547)
            return new LoiQuanLy("Dữ liệu đang được tham chiếu hoặc sân bay/chuyến bay không còn tồn tại. Hãy tải lại.", loi);
        if (loi.Number == 2601 || loi.Number == 2627)
            return new LoiQuanLy("Thông tin bị trùng. Hãy tải lại và kiểm tra tên hạng vé.", loi);
        if (loi.Number == 1205)
            return new LoiQuanLy("Dữ liệu đang được người khác xử lý. Hãy tải lại rồi thử lại.", loi);
        return new LoiQuanLy("Không thể xử lý SQL Server. Kiểm tra kết nối, quyền truy cập và cấu trúc database.", loi);
    }

    private SqlConnection MoKetNoi()
    {
        try
        {
            return ketNoi.Open();
        }
        catch (ArgumentException loi)
        {
            // Chuỗi kết nối sai cú pháp; chỉ bắt tại bước mở kết nối, không bắt lỗi tham số nghiệp vụ.
            throw new LoiQuanLy(ThongBaoLoiKetNoi, loi);
        }
    }

    public T DocDuLieu<T>(Func<DuLieuQuanLy, T> thaoTac)
    {
        try
        {
            using var ketNoi = MoKetNoi();
            var duLieu = new DuLieuQuanLy(ketNoi, null);
            return thaoTac(duLieu);
        }
        catch (SqlException loi)
        {
            throw ChuyenLoiSQL(loi);
        }
        catch (InvalidOperationException loi)
        {
            throw new LoiQuanLy(ThongBaoLoiKetNoi, loi);
        }
    }

    public T GhiDuLieu<T>(Func<DuLieuQuanLy, T> thaoTac)
    {
        // Tương thích với API trước; mọi thao tác ghi dùng chung một cơ chế transaction.
        return TrongGiaoDich(thaoTac);
    }

    public void TrongGiaoDich(Action<DuLieuQuanLy> thaoTac)
    {
        TrongGiaoDich<int>(duLieu =>
        {
            thaoTac(duLieu);
            return 0;
        });
    }

    public T TrongGiaoDich<T>(Func<DuLieuQuanLy, T> thaoTac)
    {
        // Kiểm tra và ghi trong cùng kết nối, cùng transaction để giữ khóa đến khi commit.
        // Nếu action ném lỗi, Dispose transaction chưa commit sẽ rollback.
        try
        {
            using var ketNoi = MoKetNoi();
            using var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
            var duLieu = new DuLieuQuanLy(ketNoi, giaoDich);
            T ketQua = thaoTac(duLieu);
            giaoDich.Commit();
            return ketQua;
        }
        catch (SqlException loi)
        {
            throw ChuyenLoiSQL(loi);
        }
        catch (InvalidOperationException loi)
        {
            throw new LoiQuanLy(ThongBaoLoiKetNoi, loi);
        }
    }

    public List<SanBayDTO> DocDSSanBay()
    {
        return DocDuLieu(duLieu => duLieu.DocDSSanBay());
    }

    public List<QuanLyChuyenBayDTO> DocDSChuyenBay()
    {
        return DocDuLieu(duLieu => duLieu.DocDSChuyenBay());
    }

    public List<HangVeDTO> DocDSHangVe(int? maChuyenBay = null)
    {
        return DocDuLieu(duLieu => duLieu.DocDSHangVe(maChuyenBay));
    }
}

public sealed class DuLieuQuanLy
{
    private readonly SqlConnection ketNoi;
    private readonly SqlTransaction? giaoDich;

    public DuLieuQuanLy(SqlConnection ketNoi, SqlTransaction? giaoDich)
    {
        this.ketNoi = ketNoi;
        this.giaoDich = giaoDich;
    }

    private SqlCommand TaoLenhSQL(string cauSQL, params (string Name, object Value)[] thamSo)
    {
        var lenhSQL = new SqlCommand(cauSQL, ketNoi, giaoDich);
        foreach (var (ten, giaTri) in thamSo)
        {
            var parameter = lenhSQL.Parameters.AddWithValue(ten, giaTri);
            if (giaTri is DateOnly ngay)
            {
                parameter.SqlDbType = SqlDbType.Date;
                parameter.Value = ngay.ToDateTime(TimeOnly.MinValue);
            }
            if (giaTri is TimeOnly time)
            {
                parameter.SqlDbType = SqlDbType.Time;
                parameter.Value = time.ToTimeSpan();
            }
            if (giaTri is decimal)
            {
                parameter.Precision = 18;
                parameter.Scale = 2;
            }
        }
        return lenhSQL;
    }

    private int ThucThi(string cauSQL, params (string Name, object Value)[] thamSo)
    {
        using var lenhSQL = TaoLenhSQL(cauSQL, thamSo);
        return lenhSQL.ExecuteNonQuery();
    }

    private int DocGiaTri(string cauSQL, params (string Name, object Value)[] thamSo)
    {
        using var lenhSQL = TaoLenhSQL(cauSQL, thamSo);
        return Convert.ToInt32(lenhSQL.ExecuteScalar());
    }

    public List<SanBayDTO> DocDSSanBay()
    {
        using var lenhSQL = TaoLenhSQL("SELECT airport_id,airport_name,city FROM dbo.AIRPORT ORDER BY airport_id");
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSach = new List<SanBayDTO>();
        while (docDuLieu.Read())
        {
            var sanBay = new SanBayDTO(docDuLieu.GetInt32(0), docDuLieu.GetString(1), docDuLieu.GetString(2));
            danhSach.Add(sanBay);
        }
        return danhSach;
    }

    public SanBayDTO? DocSanBay(int ma, bool canKhoa = false)
    {
        using var lenhSQL = TaoLenhSQL("SELECT airport_id,airport_name,city FROM dbo.AIRPORT " + (canKhoa ? "WITH(UPDLOCK,HOLDLOCK) " : "") + "WHERE airport_id=@id", ("@id", ma));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) return null;
        return new SanBayDTO(docDuLieu.GetInt32(0), docDuLieu.GetString(1), docDuLieu.GetString(2));
    }

    public bool SanBayDaTonTai(SanBayDTO sanBay)
    {
        int soLuong = DocGiaTri("SELECT COUNT(*) FROM dbo.AIRPORT WITH(UPDLOCK,HOLDLOCK) WHERE airport_name=@name AND city=@city AND airport_id<>@id",
            ("@name", sanBay.TenSanBay), ("@city", sanBay.ThanhPho), ("@id", sanBay.MaSanBay));
        return soLuong > 0;
    }

    public bool SanBayDaDuocDung(int ma)
    {
        int soLuong = DocGiaTri("SELECT COUNT(*) FROM dbo.FLIGHT WHERE departure_airport_id=@id OR arrival_airport_id=@id", ("@id", ma));
        return soLuong > 0;
    }

    public int LuuSanBay(SanBayDTO sanBay)
    {
        // ID bằng 0 là thêm mới: trả về ID do SQL Server sinh. Sửa trả số dòng cập nhật.
        if (sanBay.MaSanBay == 0)
        {
            return ThemSanBay(sanBay);
        }
        return SuaSanBay(sanBay);
    }

    public int ThemSanBay(SanBayDTO sanBay)
    {
        return DocGiaTri("INSERT dbo.AIRPORT(airport_name,city) OUTPUT INSERTED.airport_id VALUES(@name,@city)",
            ("@name", sanBay.TenSanBay), ("@city", sanBay.ThanhPho));
    }

    public int SuaSanBay(SanBayDTO sanBay)
    {
        return ThucThi("UPDATE dbo.AIRPORT SET airport_name=@name,city=@city WHERE airport_id=@id",
            ("@name", sanBay.TenSanBay), ("@city", sanBay.ThanhPho), ("@id", sanBay.MaSanBay));
    }

    public void XoaSanBay(int ma)
    {
        ThucThi("DELETE dbo.AIRPORT WHERE airport_id=@id", ("@id", ma));
    }

    private const string FlightSelect = """
        SELECT f.flight_id,f.departure_airport_id,f.arrival_airport_id,a.city,z.city,a.airport_name,z.airport_name,
        f.departure_date,f.departure_time,f.arrival_date,f.arrival_time,f.total_seats,f.seats_available,f.status,f.tickets_sold,f.total_revenue
        FROM dbo.FLIGHT f
        """;
    private const string Joins = " JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id JOIN dbo.AIRPORT z ON z.airport_id=f.arrival_airport_id ";
    private static QuanLyChuyenBayDTO DocDongChuyenBay(SqlDataReader docDuLieu)
    {
        var chuyenBay = new ChuyenBayDTO(
            docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetInt32(2),
            docDuLieu.GetString(3), docDuLieu.GetString(4), docDuLieu.GetString(5), docDuLieu.GetString(6),
            DateOnly.FromDateTime(docDuLieu.GetDateTime(7)), TimeOnly.FromTimeSpan(docDuLieu.GetTimeSpan(8)),
            DateOnly.FromDateTime(docDuLieu.GetDateTime(9)), TimeOnly.FromTimeSpan(docDuLieu.GetTimeSpan(10)),
            docDuLieu.GetInt32(11), docDuLieu.GetInt32(12), docDuLieu.GetString(13));
        int soVeDaBan = docDuLieu.GetInt32(14);
        decimal totalRevenue = docDuLieu.GetDecimal(15);
        return new QuanLyChuyenBayDTO(chuyenBay, soVeDaBan, totalRevenue);
    }

    public List<QuanLyChuyenBayDTO> DocDSChuyenBay()
    {
        using var lenhSQL = TaoLenhSQL(FlightSelect + Joins + " ORDER BY f.departure_date DESC,f.flight_id DESC");
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSach = new List<QuanLyChuyenBayDTO>();
        while (docDuLieu.Read())
        {
            danhSach.Add(DocDongChuyenBay(docDuLieu));
        }
        return danhSach;
    }

    public QuanLyChuyenBayDTO? DocChuyenBay(int ma, bool canKhoa = false)
    {
        using var lenhSQL = TaoLenhSQL(FlightSelect + (canKhoa ? " WITH(UPDLOCK,HOLDLOCK) " : " ") + Joins + " WHERE f.flight_id=@id", ("@id", ma));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) return null;
        return DocDongChuyenBay(docDuLieu);
    }

    public bool ChuyenBayDaCoDatVe(int ma)
    {
        int soLuong = DocGiaTri("SELECT (SELECT COUNT(*) FROM dbo.BOOKING WHERE flight_id=@id)+(SELECT COUNT(*) FROM dbo.TICKET WHERE flight_id=@id)", ("@id", ma));
        return soLuong > 0;
    }

    public bool ChuyenBayDaCoHangVe(int ma)
    {
        int soLuong = DocGiaTri("SELECT COUNT(*) FROM dbo.FARE_CLASS WHERE flight_id=@id", ("@id", ma));
        return soLuong > 0;
    }

    public List<string> DocGheHieuLuc(int ma)
    {
        using var lenhSQL = TaoLenhSQL("SELECT seat_number FROM dbo.TICKET WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@id AND status='ISSUED'", ("@id", ma));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSachGhe = new List<string>();
        while (docDuLieu.Read())
        {
            danhSachGhe.Add(docDuLieu.GetString(0));
        }
        return danhSachGhe;
    }

    public int LuuChuyenBay(ThongTinSuaChuyenBay chuyenBay, int sold)
    {
        if (chuyenBay.MaChuyenBay == 0) return ThemChuyenBay(chuyenBay, sold);
        return SuaChuyenBay(chuyenBay, sold);
    }

    public int ThemChuyenBay(ThongTinSuaChuyenBay chuyenBay, int sold)
    {
        // Ghế còn được tính từ tổng ghế và số vé thực tế; không nhận trực tiếp từ GUI.
        var thamSo = new (string, object)[]
        {
            ("@id", chuyenBay.MaChuyenBay), ("@from", chuyenBay.MaSanBayDi), ("@to", chuyenBay.MaSanBayDen),
            ("@total", chuyenBay.TongSoGhe), ("@available", chuyenBay.TongSoGhe - sold),
            ("@dd", chuyenBay.NgayKhoiHanh), ("@dt", chuyenBay.GioKhoiHanh),
            ("@ad", chuyenBay.NgayDen), ("@at", chuyenBay.GioDen), ("@status", chuyenBay.TrangThai)
        };
        return DocGiaTri("INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status) OUTPUT INSERTED.flight_id VALUES(@from,@to,@total,0,@available,0,@dd,@dt,@ad,@at,@status)", thamSo);
    }

    public int SuaChuyenBay(ThongTinSuaChuyenBay chuyenBay, int sold)
    {
        var thamSo = new (string, object)[]
        {
            ("@id", chuyenBay.MaChuyenBay), ("@from", chuyenBay.MaSanBayDi), ("@to", chuyenBay.MaSanBayDen),
            ("@total", chuyenBay.TongSoGhe), ("@available", chuyenBay.TongSoGhe - sold),
            ("@dd", chuyenBay.NgayKhoiHanh), ("@dt", chuyenBay.GioKhoiHanh),
            ("@ad", chuyenBay.NgayDen), ("@at", chuyenBay.GioDen), ("@status", chuyenBay.TrangThai)
        };
        return ThucThi("UPDATE dbo.FLIGHT SET departure_airport_id=@from,arrival_airport_id=@to,total_seats=@total,seats_available=@available,departure_date=@dd,departure_time=@dt,arrival_date=@ad,arrival_time=@at,status=@status WHERE flight_id=@id", thamSo);
    }

    public void XoaChuyenBay(int ma)
    {
        ThucThi("DELETE dbo.FLIGHT WHERE flight_id=@id", ("@id", ma));
    }

    public List<HangVeDTO> DocDSHangVe(int? maChuyenBay = null)
    {
        using var lenhSQL = TaoLenhSQL("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS " + (maChuyenBay is null ? "" : "WHERE flight_id=@id ") + "ORDER BY flight_id,fare_id", ("@id", maChuyenBay ?? 0));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSach = new List<HangVeDTO>();
        while (docDuLieu.Read())
        {
            var hangVe = new HangVeDTO(docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetString(2), docDuLieu.GetDecimal(3), docDuLieu.GetInt32(4));
            danhSach.Add(hangVe);
        }
        return danhSach;
    }

    public HangVeDTO? DocHangVe(int ma)
    {
        using var lenhSQL = TaoLenhSQL("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE fare_id=@id", ("@id", ma));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) return null;
        return new HangVeDTO(docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetString(2), docDuLieu.GetDecimal(3), docDuLieu.GetInt32(4));
    }

    public bool HangVeDaDuocDung(int ma)
    {
        int soLuong = DocGiaTri("SELECT COUNT(*) FROM dbo.TICKET WHERE fare_id=@id", ("@id", ma));
        return soLuong > 0;
    }

    public bool HangVeDaTonTai(HangVeDTO hangVe)
    {
        int soLuong = DocGiaTri("SELECT COUNT(*) FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@flight AND class_name=@name AND fare_id<>@id",
            ("@flight", hangVe.MaChuyenBay), ("@name", hangVe.TenHangVe), ("@id", hangVe.MaHangVe));
        return soLuong > 0;
    }

    public int LuuHangVe(HangVeDTO hangVe)
    {
        if (hangVe.MaHangVe == 0)
        {
            return DocGiaTri("INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) OUTPUT INSERTED.fare_id VALUES(@flight,@name,@price,@kg)",
                ("@flight", hangVe.MaChuyenBay), ("@name", hangVe.TenHangVe), ("@price", hangVe.GiaVe), ("@kg", hangVe.HanhLyCoBanKg));
        }
        return ThucThi("UPDATE dbo.FARE_CLASS SET flight_id=@flight,class_name=@name,price=@price,base_baggage_kg=@kg WHERE fare_id=@id",
            ("@flight", hangVe.MaChuyenBay), ("@name", hangVe.TenHangVe), ("@price", hangVe.GiaVe), ("@kg", hangVe.HanhLyCoBanKg), ("@id", hangVe.MaHangVe));
    }

    public void XoaHangVe(int ma)
    {
        ThucThi("DELETE dbo.FARE_CLASS WHERE fare_id=@id", ("@id", ma));
    }
}
