using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class DatVeDAO
{
    private readonly MyConnection ketNoi;
    private const string ThongBaoLoiKetNoi = "Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB.";

    public DatVeDAO(string chuoiKetNoi)
    {
        ketNoi = new MyConnection(chuoiKetNoi);
    }

    public DatVeDAO(MyConnection ketNoi)
    {
        this.ketNoi = ketNoi;
    }

    private static LoiDatVe ChuyenLoiSQL(SqlException loi)
    {
        if (loi.Number == 208)
            return new LoiDatVe("Database chưa đủ bảng. Kiểm tra đúng kết nối và đối chiếu bộ Database/Final/ theo README; không tạo lại hoặc seed lại database đang dùng.", loi);
        if (loi.Number == 1205 || loi.Number == 2601 || loi.Number == 2627)
            return new LoiDatVe("Dữ liệu vừa thay đổi hoặc yêu cầu đang xử lý. Vui lòng gửi lại cùng biểu mẫu.", loi);
        return new LoiDatVe("Không thể xử lý với SQL Server. Vui lòng thử lại hoặc liên hệ nhóm quản lý.", loi);
    }

    private SqlConnection MoKetNoi()
    {
        try { return ketNoi.Open(); }
        catch (ArgumentException loi)
        {
            throw new LoiDatVe(ThongBaoLoiKetNoi, loi);
        }
    }

    public T DocDuLieu<T>(Func<DuLieuDatVe, T> thaoTac)
    {
        try
        {
            using var ketNoi = MoKetNoi();
            return thaoTac(new DuLieuDatVe(ketNoi, null));
        }
        catch (SqlException loi) { throw ChuyenLoiSQL(loi); }
        catch (InvalidOperationException loi) { throw new LoiDatVe(ThongBaoLoiKetNoi, loi); }
    }

    public T GhiDuLieu<T>(Func<DuLieuDatVe, T> thaoTac)
    {
        // API cũ vẫn dùng cùng ranh giới giao dịch.
        return TrongGiaoDich(thaoTac);
    }

    public void TrongGiaoDich(Action<DuLieuDatVe> thaoTac)
    {
        TrongGiaoDich<int>(duLieu => { thaoTac(duLieu); return 0; });
    }

    public T TrongGiaoDich<T>(Func<DuLieuDatVe, T> thaoTac)
    {
        try
        {
            using var ketNoi = MoKetNoi();
            using var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
            var ketQua = thaoTac(new DuLieuDatVe(ketNoi, giaoDich));
            giaoDich.Commit();
            return ketQua;
        }
        // Nếu action/commit lỗi, using giải phóng transaction chưa commit để rollback.
        catch (SqlException loi) { throw ChuyenLoiSQL(loi); }
        catch (InvalidOperationException loi) { throw new LoiDatVe(ThongBaoLoiKetNoi, loi); }
    }

    public ChuyenBayDTO DocChuyenBay(int ma)
    {
        return DocDuLieu(duLieu => duLieu.DocChuyenBay(ma));
    }

    public List<SanBayDTO> DocDSSanBay()
    {
        return DocDuLieu(duLieu => duLieu.DocDSSanBay());
    }

    public List<string> DocTenHangVe()
    {
        return DocDuLieu(duLieu => duLieu.DocTenHangVe());
    }

    public DatVeDTO? DocDatVe(int ma, string maBam)
    {
        return DocDuLieu(duLieu => duLieu.DocDatVe(ma, maBam));
    }
}

// Context dùng chung kết nối/transaction, chỉ tồn tại trong lần gọi Read/InTransaction.
public sealed class DuLieuDatVe
{
    private readonly SqlConnection ketNoi;
    private readonly SqlTransaction? giaoDich;

    public DuLieuDatVe(SqlConnection ketNoi, SqlTransaction? giaoDich)
    {
        this.ketNoi = ketNoi;
        this.giaoDich = giaoDich;
    }
    private SqlCommand TaoLenhSQL(string cauSQL, params (string Name, object? Value)[] thamSo)
    {
        var lenhSQL = new SqlCommand(cauSQL, ketNoi, giaoDich);
        foreach (var (ten, giaTri) in thamSo)
        {
            var phanTu = lenhSQL.Parameters.AddWithValue(ten, giaTri ?? DBNull.Value);
            if (giaTri is decimal)
            {
                phanTu.Precision = 18;
                phanTu.Scale = 2;
            }
            if (giaTri is DateOnly ngay)
            {
                phanTu.SqlDbType = SqlDbType.Date;
                phanTu.Value = ngay.ToDateTime(TimeOnly.MinValue);
            }
        }
        return lenhSQL;
    }

    private int ThucThi(string cauSQL, params (string Name, object? Value)[] thamSo)
    {
        using var lenhSQL = TaoLenhSQL(cauSQL, thamSo);
        return lenhSQL.ExecuteNonQuery();
    }

    private int LayMaMoi(string cauSQL, params (string Name, object? Value)[] thamSo)
    {
        using var lenhSQL = TaoLenhSQL(cauSQL, thamSo);
        return Convert.ToInt32(lenhSQL.ExecuteScalar());
    }

    public void KhoaYeuCau(string maBam)
    {
        using var lenhSQL = TaoLenhSQL("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=@key, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            SELECT @result;
            """, ("@key", "booking:" + maBam));
        if (Convert.ToInt32(lenhSQL.ExecuteScalar()) < 0)
            throw new LoiDatVe("Yêu cầu đang được xử lý. Vui lòng thử lại với cùng biểu mẫu.");
    }

    public (int BookingId, string PayloadHash)? TimYeuCau(string maBam)
    {
        using var lenhSQL = TaoLenhSQL("SELECT booking_id,payload_hash FROM dbo.BOOKING_WEB_REQUEST WITH (UPDLOCK,HOLDLOCK) WHERE request_hash=@hash", ("@hash", maBam));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) return null;
        return (docDuLieu.GetInt32(0), docDuLieu.GetString(1));
    }

    public void LuuYeuCau(string maBam, string maBamNoiDung, int maDatVe)
    {
        ThucThi("""
        INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id) VALUES (@hash,@payload,@booking)
        """, ("@hash", maBam), ("@payload", maBamNoiDung), ("@booking", maDatVe));
    }

    public List<SanBayDTO> DocDSSanBay()
    {
        using var lenhSQL = TaoLenhSQL("SELECT airport_id,airport_name,city FROM dbo.AIRPORT ORDER BY city,airport_id");
        using var docDuLieu = lenhSQL.ExecuteReader();
        var ketQua = new List<SanBayDTO>();
        while (docDuLieu.Read())
        {
            var sanBay = new SanBayDTO(docDuLieu.GetInt32(0), docDuLieu.GetString(1), docDuLieu.GetString(2));
            ketQua.Add(sanBay);
        }
        return ketQua;
    }

    private const string CacTruongChuyenBay = """
        f.flight_id,f.departure_airport_id,f.arrival_airport_id,
        a.city,z.city,a.airport_name,z.airport_name,
        f.departure_date,f.departure_time,f.arrival_date,f.arrival_time,
        f.total_seats,f.seats_available,f.status
        """;
    private const string CauDocChuyenBay = "SELECT " + CacTruongChuyenBay + " FROM dbo.FLIGHT f ";
    private const string NoiBangSanBay = """
        JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id
        JOIN dbo.AIRPORT z ON z.airport_id=f.arrival_airport_id
        """;
    private static ChuyenBayDTO DocDongChuyenBay(SqlDataReader docDuLieu)
    {
        return new ChuyenBayDTO(docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetInt32(2),
            docDuLieu.GetString(3), docDuLieu.GetString(4), docDuLieu.GetString(5), docDuLieu.GetString(6),
            DateOnly.FromDateTime(docDuLieu.GetDateTime(7)), TimeOnly.FromTimeSpan(docDuLieu.GetTimeSpan(8)),
            DateOnly.FromDateTime(docDuLieu.GetDateTime(9)), TimeOnly.FromTimeSpan(docDuLieu.GetTimeSpan(10)),
            docDuLieu.GetInt32(11), docDuLieu.GetInt32(12), docDuLieu.GetString(13));
    }

    public List<ChuyenBayDTO> DocDSChuyenBay(DieuKienTimChuyenBay dieuKienTimKiem)
    {
        using var lenhSQL = TaoLenhSQL(CauDocChuyenBay + " " + NoiBangSanBay + """
             WHERE f.departure_airport_id=@departure AND f.arrival_airport_id=@arrival
                AND f.departure_date=@date AND f.status='SCHEDULED' AND f.seats_available>=@quantity
             ORDER BY f.departure_time,f.flight_id
            """, ("@departure", dieuKienTimKiem.DepartureAirportId), ("@arrival", dieuKienTimKiem.ArrivalAirportId),
            ("@date", dieuKienTimKiem.DepartureDate), ("@quantity", dieuKienTimKiem.Passengers));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var ketQua = new List<ChuyenBayDTO>();
        while (docDuLieu.Read()) ketQua.Add(DocDongChuyenBay(docDuLieu));
        return ketQua;
    }

    public List<string> DocTenHangVe()
    {
        using var lenhSQL = TaoLenhSQL("SELECT DISTINCT class_name FROM dbo.FARE_CLASS ORDER BY class_name");
        using var docDuLieu = lenhSQL.ExecuteReader();
        var names = new List<string>();
        while (docDuLieu.Read()) names.Add(docDuLieu.GetString(0));
        return names;
    }

    public List<LuaChonChuyenBayDTO> DocChuyenGiaThap(DateTime hienTai)
    {
        using var lenhSQL = TaoLenhSQL("SELECT TOP (4) " + CacTruongChuyenBay + """
            , cheapest.fare_id,cheapest.class_name,cheapest.price,cheapest.base_baggage_kg
            FROM dbo.FLIGHT f
            """ + " " + NoiBangSanBay + """
             CROSS APPLY (
                SELECT TOP (1) fc.fare_id,fc.class_name,fc.price,fc.base_baggage_kg
                FROM dbo.FARE_CLASS fc WHERE fc.flight_id=f.flight_id
                ORDER BY fc.price,fc.fare_id
             ) cheapest
             WHERE f.status='SCHEDULED' AND f.seats_available>0
               AND (f.departure_date>@date OR (f.departure_date=@date AND f.departure_time>@time))
             ORDER BY cheapest.price,f.departure_date,f.departure_time,f.flight_id,cheapest.fare_id
            """, ("@date", DateOnly.FromDateTime(hienTai)), ("@time", hienTai.TimeOfDay));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSachLuaChon = new List<LuaChonChuyenBayDTO>();
        while (docDuLieu.Read())
        {
            ChuyenBayDTO chuyenBay = DocDongChuyenBay(docDuLieu);
            var hangVe = new HangVeDTO(docDuLieu.GetInt32(14), docDuLieu.GetInt32(0), docDuLieu.GetString(15), docDuLieu.GetDecimal(16), docDuLieu.GetInt32(17));
            danhSachLuaChon.Add(new LuaChonChuyenBayDTO(chuyenBay, hangVe));
        }
        return danhSachLuaChon;
    }

    public ChuyenBayDTO DocChuyenBay(int ma, bool canKhoa = false)
    {
        using var lenhSQL = TaoLenhSQL(CauDocChuyenBay + (canKhoa ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + NoiBangSanBay +
            " WHERE f.flight_id=@id", ("@id", ma));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) throw new LoiDatVe("Không tìm thấy chuyến bay.");
        return DocDongChuyenBay(docDuLieu);
    }

    public List<HangVeDTO> DocDSHangVe(int maChuyenBay, bool canKhoa = false)
    {
        using var lenhSQL = TaoLenhSQL("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS " +
            (canKhoa ? "WITH (UPDLOCK,HOLDLOCK) " : "") + "WHERE flight_id=@id ORDER BY price,fare_id", ("@id", maChuyenBay));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var ketQua = new List<HangVeDTO>();
        while (docDuLieu.Read())
        {
            var hangVe = new HangVeDTO(docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetString(2), docDuLieu.GetDecimal(3), docDuLieu.GetInt32(4));
            ketQua.Add(hangVe);
        }
        return ketQua;
    }

    public MaGiamGiaDTO? DocMaGiamGia(string maGiamGia, bool canKhoa = false)
    {
        using var lenhSQL = TaoLenhSQL("""
            SELECT discount_id,code,discount_type,discount_value,min_order_amount,max_discount_amount,
                start_date,end_date,usage_limit,used_count,status FROM dbo.DISCOUNT_CODE
            """ + (canKhoa ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + "WHERE code=@code", ("@code", maGiamGia));
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read()) return null;
        return new MaGiamGiaDTO(docDuLieu.GetInt32(0), docDuLieu.GetString(1), docDuLieu.GetString(2), docDuLieu.GetDecimal(3), docDuLieu.GetDecimal(4),
            docDuLieu.IsDBNull(5) ? null : docDuLieu.GetDecimal(5), docDuLieu.GetDateTime(6), docDuLieu.GetDateTime(7),
            docDuLieu.IsDBNull(8) ? null : docDuLieu.GetInt32(8), docDuLieu.GetInt32(9), docDuLieu.GetString(10));
    }

    public HashSet<string> DocGheHieuLuc(int maChuyenBay)
    {
        using var lenhSQL = TaoLenhSQL("SELECT seat_number FROM dbo.TICKET WITH (UPDLOCK,HOLDLOCK) WHERE flight_id=@id AND status='ISSUED'", ("@id", maChuyenBay));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var danhSachGhe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (docDuLieu.Read()) danhSachGhe.Add(docDuLieu.GetString(0).Trim());
        return danhSachGhe;
    }

    public int ThemKhachDat(ThongTinKhachDat dauVao)
    {
        return LayMaMoi("""
        INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
        OUTPUT INSERTED.customer_id VALUES (@name,@phone,@email,@dob,@gender,@passport)
        """, ("@name", dauVao.FullName), ("@phone", dauVao.Phone), ("@email", dauVao.Email),
        ("@dob", dauVao.DateOfBirth), ("@gender", dauVao.Gender), ("@passport", dauVao.IdPassportNumber));
    }

    public List<(int Booking, int Flight, string? Hash)> DocLienKetSuaVe(int maVe)
    {
        using var lenh = TaoLenhSQL("""
            SELECT b.booking_id,b.flight_id,w.request_hash
            FROM dbo.BOOKING b LEFT JOIN dbo.BOOKING_WEB_REQUEST w ON w.booking_id=b.booking_id
            WHERE b.booking_id=(SELECT booking_id FROM dbo.BOOKING_DETAIL WHERE ticket_id=@ticket)
               OR w.payload_hash=(SELECT w0.payload_hash FROM dbo.BOOKING_WEB_REQUEST w0
                   JOIN dbo.BOOKING_DETAIL bd ON bd.booking_id=w0.booking_id WHERE bd.ticket_id=@ticket)
            ORDER BY b.flight_id,b.booking_id
            """, ("@ticket", maVe));
        using var doc = lenh.ExecuteReader();
        var ketQua = new List<(int Booking, int Flight, string? Hash)>();
        while (doc.Read()) ketQua.Add((doc.GetInt32(0), doc.GetInt32(1), doc.IsDBNull(2) ? null : doc.GetString(2)));
        return ketQua;
    }

    public void DoiGheVe(int maVe, string cu, string moi)
    {
        if(ThucThi("UPDATE dbo.TICKET SET seat_number=@new WHERE ticket_id=@id AND seat_number=@old AND status='ISSUED'",("@new",moi),("@id",maVe),("@old",cu))!=1)
            throw new LoiDatVe("Vé vừa thay đổi. Chưa đổi ghế.");
    }

    public void SuaHanhKhach(int maDatVe, int maVe, ThongTinHanhKhach thongTin)
    {
        if (ThucThi("UPDATE dbo.BOOKING_DETAIL SET passenger_full_name=@name,passenger_id_card=@paper WHERE booking_id=@booking AND ticket_id=@ticket",
            ("@name", thongTin.FullName), ("@paper", thongTin.IdCard), ("@booking", maDatVe), ("@ticket", maVe)) != 1)
            throw new LoiDatVe("Thông tin vé vừa thay đổi. Giao dịch sửa đã được hoàn tác.");
    }

    public ThongTinKhachDat DocKhachTaiQuay(int maKhachHang, bool canKhoa = false)
    {
        using var lenh = TaoLenhSQL("SELECT full_name,phone,email,date_of_birth,gender,id_passport_number FROM dbo.CUSTOMER " +
            (canKhoa ? "WITH (UPDLOCK,HOLDLOCK) " : "") + "WHERE customer_id=@id", ("@id", maKhachHang));
        using var doc = lenh.ExecuteReader();
        if (!doc.Read()) throw new LoiDatVe("Khách hàng không còn tồn tại.");
        return new ThongTinKhachDat { FullName = doc.GetString(0), Phone = doc.GetString(1), Email = doc.GetString(2),
            DateOfBirth = DateOnly.FromDateTime(doc.GetDateTime(3)), Gender = doc.GetString(4),
            IdPassportNumber = doc.IsDBNull(5) ? null : doc.GetString(5) };
    }

    public bool NhanVienConHoatDong(int maNhanVien)
    {
        using var lenh = TaoLenhSQL("SELECT COUNT(*) FROM dbo.EMPLOYEE WITH (UPDLOCK,HOLDLOCK) WHERE employee_id=@id AND status='ACTIVE'", ("@id", maNhanVien));
        return Convert.ToInt32(lenh.ExecuteScalar()) == 1;
    }

    public int ThemDatVe(int maKhachHang, BaoGiaDatVeDTO baoGia, DateTime hienTai, int? maNhanVien = null)
    {
        return LayMaMoi("""
        INSERT dbo.BOOKING(customer_id,employee_id,flight_id,discount_id,booking_date,booking_source,
            ticket_quantity,discount_amount,total_amount,status)
        OUTPUT INSERTED.booking_id VALUES (@customer,@employee,@flight,@discount,@now,@source,@qty,@amount,@total,'CONFIRMED')
        """, ("@customer", maKhachHang), ("@employee", maNhanVien), ("@source", maNhanVien.HasValue ? "COUNTER" : "WEB"), ("@flight", baoGia.ChuyenBay.MaChuyenBay), ("@discount", baoGia.MaGiamGia),
        ("@now", hienTai), ("@qty", baoGia.SoLuong), ("@amount", baoGia.TienGiam), ("@total", baoGia.TongTien));
    }

    public int ThemHoaDon(int maDatVe, BaoGiaDatVeDTO baoGia, DateTime hienTai)
    {
        return LayMaMoi("""
        INSERT dbo.INVOICE(booking_id,invoice_date,total,discount_amount,tax_amount,total_amount,status)
        OUTPUT INSERTED.invoice_id VALUES (@booking,@now,@subtotal,@discount,0,@total,'UNPAID')
        """, ("@booking", maDatVe), ("@now", hienTai), ("@subtotal", baoGia.TamTinh),
        ("@discount", baoGia.TienGiam), ("@total", baoGia.TongTien));
    }

    public void ThemHanhKhach(int maDatVe, int maHoaDon, BaoGiaDatVeDTO baoGia, ThongTinHanhKhach hanhKhach, string ghe, DateTime hienTai)
    {
        var maVe = LayMaMoi("""
            INSERT dbo.TICKET(flight_id,fare_id,seat_number,issued_at,status)
            OUTPUT INSERTED.ticket_id VALUES (@flight,@fare,@seat,@now,'ISSUED')
            """, ("@flight", baoGia.ChuyenBay.MaChuyenBay), ("@fare", baoGia.HangVe.MaHangVe), ("@seat", ghe), ("@now", hienTai));
        ThucThi("""
            INSERT dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
            VALUES (@booking,@ticket,@name,@identity,@price);
            INSERT dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal)
            VALUES (@invoice,@ticket,@price,1,@price);
            """, ("@booking", maDatVe), ("@ticket", maVe), ("@name", hanhKhach.FullName),
            ("@identity", hanhKhach.IdCard), ("@price", baoGia.HangVe.GiaVe), ("@invoice", maHoaDon));
    }

    public void GiuGhe(int maChuyenBay, int soLuong)
    {
        if (ThucThi("""
            UPDATE dbo.FLIGHT SET tickets_sold=tickets_sold+@count,seats_available=seats_available-@count
            WHERE flight_id=@id AND status='SCHEDULED' AND seats_available>=@count
            """, ("@id", maChuyenBay), ("@count", soLuong)) != 1)
            throw new LoiDatVe("Chuyến bay không còn đủ chỗ.");
    }

    public void DungMaGiamGia(int maGiamGia)
    {
        if (ThucThi("""
            UPDATE dbo.DISCOUNT_CODE SET used_count=used_count+1 WHERE discount_id=@id
                AND (usage_limit IS NULL OR used_count<usage_limit) AND status='ACTIVE'
            """, ("@id", maGiamGia)) != 1)
            throw new LoiDatVe("Mã giảm giá không còn lượt sử dụng.");
    }

    public DatVeDTO? DocDatVe(int ma, string maBam, bool canKhoa = false)
    {
        // Luồng web luôn bắt buộc khóa tra cứu; không cho null chuyển sang quyền quản trị.
        ArgumentNullException.ThrowIfNull(maBam);
        return DocDatVeChung(ma, maBam, canKhoa);
    }

    // Chỉ dùng trong DAO quản trị sau khi đã kiểm tra quyền kết nối SQL của ứng dụng desktop.
    public DatVeDTO? DocDatVeQuanTri(int ma, bool canKhoa = false)
    {
        return DocDatVeChung(ma, null, canKhoa);
    }

    private DatVeDTO? DocDatVeChung(int ma, string? maBam, bool canKhoa)
    {
        string khoa = canKhoa ? " WITH (UPDLOCK,HOLDLOCK) " : " ";
        string noiYeuCau = maBam is null ? "" : " JOIN dbo.BOOKING_WEB_REQUEST w ON w.booking_id=b.booking_id ";
        string locYeuCau = maBam is null ? "" : " AND w.request_hash=@hash";
        using var lenhSQL = TaoLenhSQL("""
            SELECT b.customer_id,b.flight_id,b.discount_id,b.booking_date,b.booking_source,b.ticket_quantity,
                b.discount_amount,b.total_amount,b.status,c.full_name,c.email,c.phone,i.status
            FROM dbo.BOOKING b
            """ + khoa + noiYeuCau + """
            JOIN dbo.CUSTOMER c ON c.customer_id=b.customer_id
            JOIN dbo.INVOICE i
            """ + khoa + " ON i.booking_id=b.booking_id WHERE b.booking_id=@id" + locYeuCau,
            ("@id", ma), ("@hash", maBam));
        int customer, chuyenBay, soLuong; int? giamGia;
        DateTime ngay; decimal tienGiam, tongTien; string nguon, trangThai, ten, email, soDienThoai, trangThaiHoaDon;
        using (var docDuLieu = lenhSQL.ExecuteReader())
        {
            if (!docDuLieu.Read()) return null;
            customer = docDuLieu.GetInt32(0);
            chuyenBay = docDuLieu.GetInt32(1);
            giamGia = docDuLieu.IsDBNull(2) ? null : docDuLieu.GetInt32(2);
            ngay = docDuLieu.GetDateTime(3);
            nguon = docDuLieu.GetString(4);
            soLuong = docDuLieu.GetInt32(5);
            tienGiam = docDuLieu.GetDecimal(6);
            tongTien = docDuLieu.GetDecimal(7);
            trangThai = docDuLieu.GetString(8);
            ten = docDuLieu.GetString(9);
            email = docDuLieu.GetString(10);
            soDienThoai = docDuLieu.GetString(11);
            trangThaiHoaDon = docDuLieu.GetString(12);
        }
        // Reader đã đóng trước khi đọc thêm chuyến và chi tiết trên cùng kết nối.
        return new(ma, customer, chuyenBay, giamGia, ngay, nguon, soLuong, tienGiam, tongTien, trangThai,
            ten, email, soDienThoai, trangThaiHoaDon, DocChuyenBay(chuyenBay), DocChiTietDatVe(ma));
    }

    public List<ChiTietDatVeDTO> DocChiTietDatVe(int maDatVe)
    {
        using var lenhSQL = TaoLenhSQL("""
            SELECT d.booking_detail_id,d.ticket_id,d.passenger_full_name,d.passenger_id_card,d.price,
                t.seat_number,f.class_name,t.status FROM dbo.BOOKING_DETAIL d
            JOIN dbo.TICKET t ON t.ticket_id=d.ticket_id
            JOIN dbo.FARE_CLASS f ON f.fare_id=t.fare_id AND f.flight_id=t.flight_id
            WHERE d.booking_id=@id ORDER BY d.booking_detail_id
            """, ("@id", maDatVe));
        using var docDuLieu = lenhSQL.ExecuteReader();
        var ketQua = new List<ChiTietDatVeDTO>();
        while (docDuLieu.Read())
        {
            var detail = new ChiTietDatVeDTO(docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.GetString(2), docDuLieu.GetString(3),
                docDuLieu.GetDecimal(4), docDuLieu.GetString(5), docDuLieu.GetString(6), docDuLieu.GetString(7));
            ketQua.Add(detail);
        }
        return ketQua;
    }

    public void HuyDatVe(DatVeDTO datVe)
    {
        if (ThucThi("UPDATE dbo.BOOKING SET status='CANCELLED' WHERE booking_id=@id AND status='CONFIRMED'", ("@id", datVe.MaDatVe)) != 1)
            throw new LoiDatVe("Trạng thái đơn vừa thay đổi. Vui lòng tải lại.");
        var soLuong = ThucThi("""
            UPDATE t SET status='CANCELLED' FROM dbo.TICKET t
            JOIN dbo.BOOKING_DETAIL d ON d.ticket_id=t.ticket_id
            WHERE d.booking_id=@id AND t.status='ISSUED' AND t.flight_id=@flight
            """, ("@id", datVe.MaDatVe), ("@flight", datVe.MaChuyenBay));
        if (soLuong != datVe.SoLuongVe) throw new LoiDatVe("Số vé không khớp. Liên hệ nhóm quản lý.");
        if (ThucThi("UPDATE dbo.INVOICE SET status='CANCELLED' WHERE booking_id=@id AND status='UNPAID'", ("@id", datVe.MaDatVe)) != 1)
            throw new LoiDatVe("Hóa đơn đã thay đổi. Không thể hủy trực tuyến.");
        if (ThucThi("""
            UPDATE dbo.FLIGHT SET tickets_sold=tickets_sold-@count,seats_available=seats_available+@count
            WHERE flight_id=@id AND tickets_sold>=@count
            """, ("@id", datVe.MaChuyenBay), ("@count", soLuong)) != 1)
            throw new LoiDatVe("Số liệu ghế không khớp. Liên hệ nhóm quản lý.");
        if (datVe.MaGiamGia is int maGiamGia && ThucThi("""
            UPDATE dbo.DISCOUNT_CODE SET used_count=used_count-1 WHERE discount_id=@id AND used_count>0
            """, ("@id", maGiamGia)) != 1)
            throw new LoiDatVe("Số lượt mã giảm giá không khớp. Liên hệ nhóm quản lý.");
    }
}
