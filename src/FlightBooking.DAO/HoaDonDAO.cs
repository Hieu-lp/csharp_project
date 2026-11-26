using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

/// <summary>
/// DAO xử lý truy vấn và cập nhật dữ liệu bảng INVOICE và INVOICE_DETAIL.
/// </summary>
public class HoaDonDAO
{
    private const string TruyVanHoaDon = """
        SELECT i.invoice_id, i.booking_id, i.invoice_date, i.total, i.discount_amount, i.tax_amount, i.total_amount, i.status,
               b.flight_id, b.ticket_quantity,b.employee_id,e.full_name AS employee_name,
               c.full_name AS customer_name, c.phone AS customer_phone, c.email AS customer_email
        FROM dbo.INVOICE i
        JOIN dbo.BOOKING b ON i.booking_id = b.booking_id
        JOIN dbo.CUSTOMER c ON b.customer_id = c.customer_id
        LEFT JOIN dbo.EMPLOYEE e ON e.employee_id=b.employee_id
        """;

    public List<HoaDonDTO> DocDSHoaDon()
    {
        var ketQua = new List<HoaDonDTO>();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand($"{TruyVanHoaDon} ORDER BY i.invoice_id DESC;", ketNoi);
        ketNoi.Open();
        using SqlDataReader doc = lenhSQL.ExecuteReader();
        while (doc.Read())
        {
            ketQua.Add(ChuyenDoiHoaDon(doc));
        }
        return ketQua;
    }

    public HoaDonDTO? DocHoaDonTheoMa(int maHoaDon)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand($"{TruyVanHoaDon} WHERE i.invoice_id = @id;", ketNoi);
        lenhSQL.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
        ketNoi.Open();
        using SqlDataReader doc = lenhSQL.ExecuteReader();
        if (!doc.Read())
            return null;
        var hoaDon = ChuyenDoiHoaDon(doc);
        doc.Close();

        hoaDon.DanhSachChiTiet = DocChiTietHoaDon(maHoaDon, ketNoi);
        return hoaDon;
    }

    public List<ChiTietHoaDonDTO> DocChiTietHoaDon(int maHoaDon, SqlConnection? ketNoiNgoai = null)
    {
        const string sqlChiTiet = """
            SELECT id.invoice_detail_id, id.invoice_id, id.ticket_id, id.unit_price, id.quantity, id.subtotal,
                   t.seat_number, fc.class_name,
                   bd.passenger_full_name, bd.passenger_id_card
            FROM dbo.INVOICE_DETAIL id
            JOIN dbo.TICKET t ON id.ticket_id = t.ticket_id
            JOIN dbo.FARE_CLASS fc ON t.fare_id = fc.fare_id AND t.flight_id = fc.flight_id
            LEFT JOIN dbo.BOOKING_DETAIL bd ON t.ticket_id = bd.ticket_id
            WHERE id.invoice_id = @invoiceId
            ORDER BY id.invoice_detail_id;
            """;

        var ketQua = new List<ChiTietHoaDonDTO>();
        bool taoMoiKetNoi = ketNoiNgoai == null;
        SqlConnection ketNoi = ketNoiNgoai ?? MyConnection.CreateConnection();

        try
        {
            if (taoMoiKetNoi)
                ketNoi.Open();
            using var lenhSQL = new SqlCommand(sqlChiTiet, ketNoi);
            lenhSQL.Parameters.Add("@invoiceId", SqlDbType.Int).Value = maHoaDon;
            using SqlDataReader doc = lenhSQL.ExecuteReader();
            while (doc.Read())
            {
                ketQua.Add(new ChiTietHoaDonDTO
                {
                    MaChiTietHoaDon = doc.GetInt32(doc.GetOrdinal("invoice_detail_id")),
                    MaHoaDon = doc.GetInt32(doc.GetOrdinal("invoice_id")),
                    MaVe = doc.GetInt32(doc.GetOrdinal("ticket_id")),
                    DonGia = doc.GetDecimal(doc.GetOrdinal("unit_price")),
                    SoLuong = doc.GetInt32(doc.GetOrdinal("quantity")),
                    ThanhTien = doc.GetDecimal(doc.GetOrdinal("subtotal")),
                    SoGhe = doc.GetString(doc.GetOrdinal("seat_number")).Trim(),
                    TenHangVe = doc.IsDBNull(doc.GetOrdinal("class_name")) ? "" : doc.GetString(doc.GetOrdinal("class_name")),
                    TenHanhKhach = doc.IsDBNull(doc.GetOrdinal("passenger_full_name")) ? "" : doc.GetString(doc.GetOrdinal("passenger_full_name")),
                    SoGiayTo = doc.IsDBNull(doc.GetOrdinal("passenger_id_card")) ? "" : doc.GetString(doc.GetOrdinal("passenger_id_card"))
                });
            }
        }
        finally
        {
            if (taoMoiKetNoi)
                ketNoi.Dispose();
        }

        return ketQua;
    }

    private static (int MaDatVe, int MaChuyenBay, string? MaBam)? DocDanhTinh(SqlConnection ketNoi, int maHoaDon)
    {
        using var lenhSQL = new SqlCommand("""
            SELECT b.booking_id,b.flight_id,w.request_hash FROM dbo.INVOICE i
            JOIN dbo.BOOKING b ON b.booking_id=i.booking_id
            LEFT JOIN dbo.BOOKING_WEB_REQUEST w ON w.booking_id=b.booking_id
            WHERE i.invoice_id=@id
            """, ketNoi);
        lenhSQL.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
        using var docDuLieu = lenhSQL.ExecuteReader();
        if (!docDuLieu.Read())
            return null;
        return (docDuLieu.GetInt32(0), docDuLieu.GetInt32(1), docDuLieu.IsDBNull(2) ? null : docDuLieu.GetString(2));
    }

    public bool ThanhToanHoaDon(int maHoaDon, decimal? soTienDaXacNhan = null)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        var danhTinh = DocDanhTinh(ketNoi, maHoaDon);
        if (danhTinh is null)
            return false;
        using SqlTransaction giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
        var duLieu = new DuLieuDatVe(ketNoi, giaoDich);
        // Cùng thứ tự khóa với website: yêu cầu -> chuyến -> Booking -> Invoice.
        if (danhTinh.Value.MaBam is string maBam)
            duLieu.KhoaYeuCau(maBam);
        duLieu.DocChuyenBay(danhTinh.Value.MaChuyenBay, true);
        DatVeDTO? datVe = duLieu.DocDatVeQuanTri(danhTinh.Value.MaDatVe, true);
        if (datVe is null || datVe.MaChuyenBay != danhTinh.Value.MaChuyenBay ||
            datVe.TrangThai != "CONFIRMED" || datVe.TrangThaiHoaDon != "UNPAID")
            return false;

        // Đối chiếu chứng từ và vé hiệu lực trước khi nhận tiền; không tính lại giá hiện tại.
        using var kiemTra = new SqlCommand("""
            SELECT i.total_amount FROM dbo.INVOICE i WITH(UPDLOCK,HOLDLOCK)
            JOIN dbo.BOOKING b ON b.booking_id=i.booking_id
            WHERE i.invoice_id=@id AND i.booking_id=@booking AND i.status='UNPAID'
              AND b.status='CONFIRMED' AND b.total_amount=i.total_amount
              AND i.total_amount=i.total-i.discount_amount+i.tax_amount
              AND i.total=COALESCE((SELECT SUM(d.subtotal) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id),0)
              AND b.ticket_quantity=(SELECT COUNT(*) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id)
              AND b.ticket_quantity=(SELECT COUNT(*) FROM dbo.BOOKING_DETAIL bd JOIN dbo.TICKET t ON t.ticket_id=bd.ticket_id
                  JOIN dbo.INVOICE_DETAIL d ON d.ticket_id=t.ticket_id AND d.invoice_id=i.invoice_id
                  WHERE bd.booking_id=b.booking_id AND t.flight_id=b.flight_id AND t.status='ISSUED'
                    AND bd.price=d.unit_price AND d.subtotal=d.unit_price AND d.quantity=1)
            """, ketNoi, giaoDich);
        kiemTra.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
        kiemTra.Parameters.Add("@booking", SqlDbType.Int).Value = datVe.MaDatVe;
        object? soTien = kiemTra.ExecuteScalar();
        if (soTien is null || soTien is DBNull)
            throw new LoiDatVe("Chứng từ hoặc số vé không khớp. Chưa ghi nhận thanh toán; hãy tải lại và kiểm tra.");
        if (soTienDaXacNhan.HasValue && soTienDaXacNhan.Value != (decimal)soTien)
            throw new LoiDatVe("Số tiền hóa đơn vừa thay đổi. Chưa ghi nhận thanh toán; hãy tải lại và xác nhận.");
        using var capNhat = new SqlCommand("UPDATE dbo.INVOICE SET status='PAID' WHERE invoice_id=@id AND status='UNPAID'", ketNoi, giaoDich);
        capNhat.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
        if (capNhat.ExecuteNonQuery() != 1)
            return false;
        using var doanhThu = new SqlCommand("UPDATE dbo.FLIGHT SET total_revenue=total_revenue+@amount WHERE flight_id=@flight", ketNoi, giaoDich);
        SqlParameter tien = doanhThu.Parameters.Add("@amount", SqlDbType.Decimal);
        tien.Precision = 18;
        tien.Scale = 2;
        tien.Value = (decimal)soTien;
        doanhThu.Parameters.Add("@flight", SqlDbType.Int).Value = datVe.MaChuyenBay;
        if (doanhThu.ExecuteNonQuery() != 1)
            throw new LoiDatVe("Không cập nhật được doanh thu chuyến bay. Thanh toán đã được hoàn tác.");
        giaoDich.Commit();
        return true;
        // Dispose giao dịch chưa commit sẽ rollback và giữ exception gốc.
    }

    public bool HuyHoaDon(int maHoaDon)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        var danhTinh = DocDanhTinh(ketNoi, maHoaDon);
        if (danhTinh is null)
            return false;
        using SqlTransaction giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
        var duLieu = new DuLieuDatVe(ketNoi, giaoDich);
        if (danhTinh.Value.MaBam is string maBam)
            duLieu.KhoaYeuCau(maBam);
        duLieu.DocChuyenBay(danhTinh.Value.MaChuyenBay, true);
        DatVeDTO? datVe = duLieu.DocDatVeQuanTri(danhTinh.Value.MaDatVe, true);
        if (datVe is null || datVe.MaChuyenBay != danhTinh.Value.MaChuyenBay)
            return false;
        if (datVe.TrangThai == "CANCELLED" && datVe.TrangThaiHoaDon == "CANCELLED")
            return true;
        if (datVe.TrangThai != "CONFIRMED" || datVe.TrangThaiHoaDon != "UNPAID")
            throw new LoiDatVe("Chỉ hủy đơn chưa thanh toán. Hoàn tiền đơn PAID cần quy trình riêng được nhóm phê duyệt.");
        if (datVe.ChuyenBay.ThoiDiemKhoiHanh <= DateTime.Now)
            throw new LoiDatVe("Không hủy sau giờ khởi hành.");
        // Dùng đúng luồng hủy Booking hiện có: hóa đơn, toàn bộ vé, ghế và mã giảm giá cùng giao dịch.
        duLieu.HuyDatVe(datVe);
        giaoDich.Commit();
        return true;
    }

    private static HoaDonDTO ChuyenDoiHoaDon(SqlDataReader doc)
    {
        return new HoaDonDTO
        {
            MaHoaDon = doc.GetInt32(doc.GetOrdinal("invoice_id")),
            MaDatVe = doc.GetInt32(doc.GetOrdinal("booking_id")),
            NgayHoaDon = doc.GetDateTime(doc.GetOrdinal("invoice_date")),
            TongTienGoc = doc.GetDecimal(doc.GetOrdinal("total")),
            TienGiam = doc.GetDecimal(doc.GetOrdinal("discount_amount")),
            TienThue = doc.GetDecimal(doc.GetOrdinal("tax_amount")),
            TongTien = doc.GetDecimal(doc.GetOrdinal("total_amount")),
            TrangThai = doc.GetString(doc.GetOrdinal("status")).Trim(),
            MaChuyenBay = doc.GetInt32(doc.GetOrdinal("flight_id")),
            SoLuongVe = doc.GetInt32(doc.GetOrdinal("ticket_quantity")),
            MaNhanVien = doc.IsDBNull(doc.GetOrdinal("employee_id")) ? null : doc.GetInt32(doc.GetOrdinal("employee_id")),
            TenNhanVien = doc.IsDBNull(doc.GetOrdinal("employee_name")) ? "WEB · không nhân viên" : doc.GetString(doc.GetOrdinal("employee_name")),
            TenKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_name")) ? "" : doc.GetString(doc.GetOrdinal("customer_name")),
            DienThoaiKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_phone")) ? "" : doc.GetString(doc.GetOrdinal("customer_phone")),
            EmailKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_email")) ? "" : doc.GetString(doc.GetOrdinal("customer_email"))
        };
    }
}
