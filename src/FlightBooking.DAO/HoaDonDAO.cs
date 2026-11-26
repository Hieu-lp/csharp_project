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
               b.flight_id, b.ticket_quantity,
               c.full_name AS customer_name, c.phone AS customer_phone, c.email AS customer_email
        FROM dbo.INVOICE i
        JOIN dbo.BOOKING b ON i.booking_id = b.booking_id
        JOIN dbo.CUSTOMER c ON b.customer_id = c.customer_id
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
        if (!doc.Read()) return null;
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
            if (taoMoiKetNoi) ketNoi.Open();
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
            if (taoMoiKetNoi) ketNoi.Dispose();
        }

        return ketQua;
    }

    public bool ThanhToanHoaDon(int maHoaDon)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        using SqlTransaction giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // 1. Kiểm tra hóa đơn hiện tại và lấy thông tin
            using var cmdCheck = new SqlCommand("""
                SELECT i.status, i.total_amount, b.flight_id
                FROM dbo.INVOICE i WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.BOOKING b ON i.booking_id = b.booking_id
                WHERE i.invoice_id = @id;
                """, ketNoi, giaoDich);
            cmdCheck.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;

            string trangThai = "";
            decimal tongTien = 0;
            int maChuyenBay = 0;

            using (var reader = cmdCheck.ExecuteReader())
            {
                if (!reader.Read()) return false;
                trangThai = reader.GetString(0).Trim();
                tongTien = reader.GetDecimal(1);
                maChuyenBay = reader.GetInt32(2);
            }

            if (trangThai != "UNPAID")
                return false; // Chỉ hóa đơn UNPAID mới được thanh toán

            // 2. Chuyển trạng thái sang PAID
            using var cmdUpdateInvoice = new SqlCommand("""
                UPDATE dbo.INVOICE SET status = 'PAID' WHERE invoice_id = @id AND status = 'UNPAID';
                """, ketNoi, giaoDich);
            cmdUpdateInvoice.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
            if (cmdUpdateInvoice.ExecuteNonQuery() != 1) return false;

            // 3. Cập nhật doanh thu chuyến bay FLIGHT.total_revenue
            using var cmdUpdateFlight = new SqlCommand("""
                UPDATE dbo.FLIGHT
                SET total_revenue = total_revenue + @amount
                WHERE flight_id = @flightId;
                """, ketNoi, giaoDich);
            cmdUpdateFlight.Parameters.Add("@amount", SqlDbType.Decimal).Value = tongTien;
            cmdUpdateFlight.Parameters.Add("@flightId", SqlDbType.Int).Value = maChuyenBay;
            cmdUpdateFlight.ExecuteNonQuery();

            giaoDich.Commit();
            return true;
        }
        catch
        {
            giaoDich.Rollback();
            throw;
        }
    }

    public bool HuyHoaDon(int maHoaDon)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        using SqlTransaction giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // 1. Lấy thông tin hóa đơn và booking
            using var cmdCheck = new SqlCommand("""
                SELECT i.status, i.booking_id, b.flight_id, b.ticket_quantity, b.discount_id
                FROM dbo.INVOICE i WITH (UPDLOCK, HOLDLOCK)
                JOIN dbo.BOOKING b WITH (UPDLOCK, HOLDLOCK) ON i.booking_id = b.booking_id
                WHERE i.invoice_id = @id;
                """, ketNoi, giaoDich);
            cmdCheck.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;

            string trangThai = "";
            int maDatVe = 0, maChuyenBay = 0, soLuongVe = 0;
            int? maGiamGia = null;

            using (var reader = cmdCheck.ExecuteReader())
            {
                if (!reader.Read()) return false;
                trangThai = reader.GetString(0).Trim();
                maDatVe = reader.GetInt32(1);
                maChuyenBay = reader.GetInt32(2);
                soLuongVe = reader.GetInt32(3);
                if (!reader.IsDBNull(4)) maGiamGia = reader.GetInt32(4);
            }

            if (trangThai == "CANCELLED") return true;
            if (trangThai == "PAID")
            {
                // Nếu hóa đơn đã PAID mà hủy, cần trừ lại total_revenue của chuyến bay
                using var cmdSubRevenue = new SqlCommand("""
                    UPDATE dbo.FLIGHT SET total_revenue = CASE WHEN total_revenue >= i.total_amount THEN total_revenue - i.total_amount ELSE 0 END
                    FROM dbo.FLIGHT f
                    JOIN dbo.BOOKING b ON f.flight_id = b.flight_id
                    JOIN dbo.INVOICE i ON b.booking_id = i.booking_id
                    WHERE i.invoice_id = @id;
                    """, ketNoi, giaoDich);
                cmdSubRevenue.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
                cmdSubRevenue.ExecuteNonQuery();
            }

            // 2. Chuyển trạng thái Invoice sang CANCELLED
            using var cmdUpdateInvoice = new SqlCommand("""
                UPDATE dbo.INVOICE SET status = 'CANCELLED' WHERE invoice_id = @id;
                """, ketNoi, giaoDich);
            cmdUpdateInvoice.Parameters.Add("@id", SqlDbType.Int).Value = maHoaDon;
            cmdUpdateInvoice.ExecuteNonQuery();

            // 3. Chuyển trạng thái Booking sang CANCELLED
            using var cmdUpdateBooking = new SqlCommand("""
                UPDATE dbo.BOOKING SET status = 'CANCELLED' WHERE booking_id = @bookingId;
                """, ketNoi, giaoDich);
            cmdUpdateBooking.Parameters.Add("@bookingId", SqlDbType.Int).Value = maDatVe;
            cmdUpdateBooking.ExecuteNonQuery();

            // 4. Hủy các vé TICKET thuộc booking này
            using var cmdUpdateTickets = new SqlCommand("""
                UPDATE t
                SET t.status = 'CANCELLED'
                FROM dbo.TICKET t
                JOIN dbo.BOOKING_DETAIL bd ON t.ticket_id = bd.ticket_id
                WHERE bd.booking_id = @bookingId AND t.status = 'ISSUED';
                """, ketNoi, giaoDich);
            cmdUpdateTickets.Parameters.Add("@bookingId", SqlDbType.Int).Value = maDatVe;
            cmdUpdateTickets.ExecuteNonQuery();

            // 5. Hoàn số ghế cho chuyến bay
            using var cmdRestoreSeats = new SqlCommand("""
                UPDATE dbo.FLIGHT
                SET seats_available = seats_available + @qty,
                    tickets_sold = CASE WHEN tickets_sold >= @qty THEN tickets_sold - @qty ELSE 0 END
                WHERE flight_id = @flightId;
                """, ketNoi, giaoDich);
            cmdRestoreSeats.Parameters.Add("@qty", SqlDbType.Int).Value = soLuongVe;
            cmdRestoreSeats.Parameters.Add("@flightId", SqlDbType.Int).Value = maChuyenBay;
            cmdRestoreSeats.ExecuteNonQuery();

            // 6. Hoàn mã giảm giá nếu có
            if (maGiamGia.HasValue)
            {
                using var cmdRestoreDiscount = new SqlCommand("""
                    UPDATE dbo.DISCOUNT_CODE
                    SET used_count = CASE WHEN used_count > 0 THEN used_count - 1 ELSE 0 END
                    WHERE discount_id = @discountId;
                    """, ketNoi, giaoDich);
                cmdRestoreDiscount.Parameters.Add("@discountId", SqlDbType.Int).Value = maGiamGia.Value;
                cmdRestoreDiscount.ExecuteNonQuery();
            }

            giaoDich.Commit();
            return true;
        }
        catch
        {
            giaoDich.Rollback();
            throw;
        }
    }

    public ThongKeDoanhThuDTO ThongKeTongHop()
    {
        var ketQua = new ThongKeDoanhThuDTO();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();

        // 1. Thống kê chung theo hóa đơn
        const string sqlTongHop = """
            SELECT 
                ISNULL(SUM(CASE WHEN status = 'PAID' THEN total_amount ELSE 0 END), 0) AS PaidRevenue,
                ISNULL(SUM(CASE WHEN status = 'UNPAID' THEN total_amount ELSE 0 END), 0) AS UnpaidRevenue,
                ISNULL(SUM(CASE WHEN status != 'CANCELLED' THEN total_amount ELSE 0 END), 0) AS TotalRevenue,
                COUNT(*) AS TotalInvoices,
                ISNULL(SUM(CASE WHEN status = 'PAID' THEN 1 ELSE 0 END), 0) AS PaidCount,
                ISNULL(SUM(CASE WHEN status = 'UNPAID' THEN 1 ELSE 0 END), 0) AS UnpaidCount,
                ISNULL(SUM(CASE WHEN status = 'CANCELLED' THEN 1 ELSE 0 END), 0) AS CancelledCount
            FROM dbo.INVOICE;
            """;

        using (var cmd = new SqlCommand(sqlTongHop, ketNoi))
        using (var reader = cmd.ExecuteReader())
        {
            if (reader.Read())
            {
                ketQua.DoanhThuDaThu = reader.GetDecimal(0);
                ketQua.DoanhThuChuaThu = reader.GetDecimal(1);
                ketQua.TongDoanhThu = reader.GetDecimal(2);
                ketQua.TongSoHoaDon = reader.GetInt32(3);
                ketQua.SoHoaDonDaThanhToan = reader.GetInt32(4);
                ketQua.SoHoaDonChuaThanhToan = reader.GetInt32(5);
                ketQua.SoHoaDonDaHuy = reader.GetInt32(6);
            }
        }

        // 2. Tổng số vé đã bán
        const string sqlVe = "SELECT COUNT(*) FROM dbo.TICKET WHERE status = 'ISSUED';";
        using (var cmd = new SqlCommand(sqlVe, ketNoi))
        {
            ketQua.TongSoVeBan = Convert.ToInt32(cmd.ExecuteScalar() ?? 0);
        }

        // 3. Doanh thu theo quý
        const string sqlQuy = """
            SELECT 
                YEAR(invoice_date) AS [Year],
                DATEPART(QUARTER, invoice_date) AS [Quarter],
                COUNT(*) AS InvoiceCount,
                ISNULL(SUM(b.ticket_quantity), 0) AS TicketCount,
                ISNULL(SUM(i.total_amount), 0) AS Revenue
            FROM dbo.INVOICE i
            JOIN dbo.BOOKING b ON i.booking_id = b.booking_id
            WHERE i.status = 'PAID'
            GROUP BY YEAR(invoice_date), DATEPART(QUARTER, invoice_date)
            ORDER BY [Year] DESC, [Quarter] DESC;
            """;

        using (var cmd = new SqlCommand(sqlQuy, ketNoi))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                ketQua.DoanhThuTheoQuy.Add(new DoanhThuTheoQuyDTO
                {
                    Nam = reader.GetInt32(0),
                    Quy = reader.GetInt32(1),
                    SoHoaDon = reader.GetInt32(2),
                    SoVe = reader.GetInt32(3),
                    DoanhThu = reader.GetDecimal(4)
                });
            }
        }

        // 4. Doanh thu theo chuyến bay
        const string sqlChuyenBay = """
            SELECT 
                f.flight_id,
                dep.city + ' - ' + arr.city AS Route,
                f.tickets_sold,
                f.total_revenue
            FROM dbo.FLIGHT f
            JOIN dbo.AIRPORT dep ON f.departure_airport_id = dep.airport_id
            JOIN dbo.AIRPORT arr ON f.arrival_airport_id = arr.airport_id
            ORDER BY f.total_revenue DESC, f.flight_id;
            """;

        using (var cmd = new SqlCommand(sqlChuyenBay, ketNoi))
        using (var reader = cmd.ExecuteReader())
        {
            while (reader.Read())
            {
                ketQua.DoanhThuTheoChuyenBay.Add(new DoanhThuTheoChuyenBayDTO
                {
                    MaChuyenBay = reader.GetInt32(0),
                    TuyenBay = reader.GetString(1),
                    SoVeDaBan = reader.GetInt32(2),
                    DoanhThu = reader.GetDecimal(3)
                });
            }
        }

        return ketQua;
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
            TenKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_name")) ? "" : doc.GetString(doc.GetOrdinal("customer_name")),
            DienThoaiKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_phone")) ? "" : doc.GetString(doc.GetOrdinal("customer_phone")),
            EmailKhachHang = doc.IsDBNull(doc.GetOrdinal("customer_email")) ? "" : doc.GetString(doc.GetOrdinal("customer_email"))
        };
    }
}
