using FlightBooking.DTO;
using Microsoft.Data.SqlClient;
using System.Data;

namespace FlightBooking.DAO;

/// <summary>
/// DAO xử lý truy vấn và cập nhật dữ liệu bảng TICKET trong SQL Server.
/// </summary>
public class VeMayBayDAO
{
    private const string TruyVanVe = """
        SELECT t.ticket_id, t.flight_id, t.fare_id, t.seat_number, t.issued_at, t.status,
               fc.class_name, COALESCE(id.unit_price, bd.price) AS price,
               f.departure_date, f.departure_time,
               dep.airport_name AS dep_airport, arr.airport_name AS arr_airport,
               bd.passenger_full_name, bd.passenger_id_card, bd.booking_id,inv.status AS invoice_status
        FROM dbo.TICKET t
        JOIN dbo.FLIGHT f ON t.flight_id = f.flight_id
        JOIN dbo.FARE_CLASS fc ON t.fare_id = fc.fare_id AND t.flight_id = fc.flight_id
        LEFT JOIN dbo.AIRPORT dep ON f.departure_airport_id = dep.airport_id
        LEFT JOIN dbo.AIRPORT arr ON f.arrival_airport_id = arr.airport_id
        LEFT JOIN dbo.BOOKING_DETAIL bd ON t.ticket_id = bd.ticket_id
        LEFT JOIN dbo.INVOICE_DETAIL id ON id.ticket_id=t.ticket_id
        LEFT JOIN dbo.INVOICE inv ON inv.booking_id=bd.booking_id
        """;

    public List<VeMayBayDTO> DocDSVe()
    {
        var ketQua = new List<VeMayBayDTO>();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand($"{TruyVanVe} ORDER BY t.ticket_id DESC;", ketNoi);
        ketNoi.Open();
        using SqlDataReader doc = lenhSQL.ExecuteReader();
        while (doc.Read())
        {
            ketQua.Add(ChuyenDoiVe(doc));
        }
        return ketQua;
    }

    public VeMayBayDTO? DocVeTheoMa(int maVe)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand($"{TruyVanVe} WHERE t.ticket_id = @id;", ketNoi);
        lenhSQL.Parameters.Add("@id", SqlDbType.Int).Value = maVe;
        ketNoi.Open();
        using SqlDataReader doc = lenhSQL.ExecuteReader();
        return doc.Read() ? ChuyenDoiVe(doc) : null;
    }

    public List<VeMayBayDTO> DocVeTheoChuyenBay(int maChuyenBay)
    {
        var ketQua = new List<VeMayBayDTO>();
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand($"{TruyVanVe} WHERE t.flight_id = @flightId ORDER BY t.seat_number;", ketNoi);
        lenhSQL.Parameters.Add("@flightId", SqlDbType.Int).Value = maChuyenBay;
        ketNoi.Open();
        using SqlDataReader doc = lenhSQL.ExecuteReader();
        while (doc.Read())
        {
            ketQua.Add(ChuyenDoiVe(doc));
        }
        return ketQua;
    }

    public bool HuyDonChuaVe(int maVe)
    {
        // Không hủy rời một vé đã nằm trong Booking/Invoice: phải hủy cả đơn UNPAID.
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        using var lenhSQL = new SqlCommand("""
            SELECT i.invoice_id FROM dbo.BOOKING_DETAIL bd
            JOIN dbo.INVOICE i ON i.booking_id=bd.booking_id WHERE bd.ticket_id=@ticket
            """, ketNoi);
        lenhSQL.Parameters.Add("@ticket", SqlDbType.Int).Value = maVe;
        ketNoi.Open();
        object? maHoaDon = lenhSQL.ExecuteScalar();
        if (maHoaDon is null || maHoaDon is DBNull) throw new LoiDatVe("Vé chưa có chứng từ liên kết hợp lệ. Không hủy hoặc hoàn ghế thủ công.");
        return new HoaDonDAO().HuyHoaDon(Convert.ToInt32(maHoaDon));
    }

    private static VeMayBayDTO ChuyenDoiVe(SqlDataReader doc)
    {
        return new VeMayBayDTO
        {
            MaVe = doc.GetInt32(doc.GetOrdinal("ticket_id")),
            MaChuyenBay = doc.GetInt32(doc.GetOrdinal("flight_id")),
            MaHangVe = doc.GetInt32(doc.GetOrdinal("fare_id")),
            SoGhe = doc.GetString(doc.GetOrdinal("seat_number")).Trim(),
            NgayPhatHanh = doc.GetDateTime(doc.GetOrdinal("issued_at")),
            TrangThai = doc.GetString(doc.GetOrdinal("status")).Trim(),
            TenHangVe = doc.IsDBNull(doc.GetOrdinal("class_name")) ? "" : doc.GetString(doc.GetOrdinal("class_name")),
            GiaVe = doc.IsDBNull(doc.GetOrdinal("price")) ? 0 : doc.GetDecimal(doc.GetOrdinal("price")),
            CoGiaLichSu = !doc.IsDBNull(doc.GetOrdinal("price")),
            SanBayDi = doc.IsDBNull(doc.GetOrdinal("dep_airport")) ? "" : doc.GetString(doc.GetOrdinal("dep_airport")),
            SanBayDen = doc.IsDBNull(doc.GetOrdinal("arr_airport")) ? "" : doc.GetString(doc.GetOrdinal("arr_airport")),
            NgayKhoiHanh = doc.IsDBNull(doc.GetOrdinal("departure_date")) ? null : DateOnly.FromDateTime(doc.GetDateTime(doc.GetOrdinal("departure_date"))),
            GioKhoiHanh = doc.IsDBNull(doc.GetOrdinal("departure_time")) ? null : TimeOnly.FromTimeSpan(doc.GetTimeSpan(doc.GetOrdinal("departure_time"))),
            TenHanhKhach = doc.IsDBNull(doc.GetOrdinal("passenger_full_name")) ? "" : doc.GetString(doc.GetOrdinal("passenger_full_name")),
            SoGiayTo = doc.IsDBNull(doc.GetOrdinal("passenger_id_card")) ? "" : doc.GetString(doc.GetOrdinal("passenger_id_card")),
            MaDatVe = doc.IsDBNull(doc.GetOrdinal("booking_id")) ? null : doc.GetInt32(doc.GetOrdinal("booking_id")),
            TrangThaiHoaDon = doc.IsDBNull(doc.GetOrdinal("invoice_status")) ? "" : doc.GetString(doc.GetOrdinal("invoice_status"))
        };
    }
}
