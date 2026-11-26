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
               fc.class_name, fc.price,
               f.departure_date, f.departure_time,
               dep.airport_name AS dep_airport, arr.airport_name AS arr_airport,
               bd.passenger_full_name, bd.passenger_id_card, bd.booking_id
        FROM dbo.TICKET t
        JOIN dbo.FLIGHT f ON t.flight_id = f.flight_id
        JOIN dbo.FARE_CLASS fc ON t.fare_id = fc.fare_id AND t.flight_id = fc.flight_id
        LEFT JOIN dbo.AIRPORT dep ON f.departure_airport_id = dep.airport_id
        LEFT JOIN dbo.AIRPORT arr ON f.arrival_airport_id = arr.airport_id
        LEFT JOIN dbo.BOOKING_DETAIL bd ON t.ticket_id = bd.ticket_id
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

    public bool HuyVe(int maVe)
    {
        using SqlConnection ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        using SqlTransaction giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);

        try
        {
            // 1. Kiểm tra vé và lấy mã chuyến bay
            using var cmdCheck = new SqlCommand("""
                SELECT flight_id, status FROM dbo.TICKET WITH (UPDLOCK, HOLDLOCK) WHERE ticket_id = @ticketId;
                """, ketNoi, giaoDich);
            cmdCheck.Parameters.Add("@ticketId", SqlDbType.Int).Value = maVe;

            int maChuyenBay = 0;
            string trangThai = "";
            using (var reader = cmdCheck.ExecuteReader())
            {
                if (!reader.Read()) return false;
                maChuyenBay = reader.GetInt32(0);
                trangThai = reader.GetString(1).Trim();
            }

            if (trangThai == "CANCELLED")
                return true; // Đã hủy trước đó

            // 2. Cập nhật trạng thái vé sang CANCELLED
            using var cmdUpdateTicket = new SqlCommand("""
                UPDATE dbo.TICKET SET status = 'CANCELLED' WHERE ticket_id = @ticketId AND status = 'ISSUED';
                """, ketNoi, giaoDich);
            cmdUpdateTicket.Parameters.Add("@ticketId", SqlDbType.Int).Value = maVe;
            int rowsTicket = cmdUpdateTicket.ExecuteNonQuery();
            if (rowsTicket == 0) return false;

            // 3. Hoàn lại số ghế cho chuyến bay
            using var cmdUpdateFlight = new SqlCommand("""
                UPDATE dbo.FLIGHT
                SET seats_available = seats_available + 1,
                    tickets_sold = CASE WHEN tickets_sold > 0 THEN tickets_sold - 1 ELSE 0 END
                WHERE flight_id = @flightId;
                """, ketNoi, giaoDich);
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
            SanBayDi = doc.IsDBNull(doc.GetOrdinal("dep_airport")) ? "" : doc.GetString(doc.GetOrdinal("dep_airport")),
            SanBayDen = doc.IsDBNull(doc.GetOrdinal("arr_airport")) ? "" : doc.GetString(doc.GetOrdinal("arr_airport")),
            NgayKhoiHanh = doc.IsDBNull(doc.GetOrdinal("departure_date")) ? null : DateOnly.FromDateTime(doc.GetDateTime(doc.GetOrdinal("departure_date"))),
            GioKhoiHanh = doc.IsDBNull(doc.GetOrdinal("departure_time")) ? null : TimeOnly.FromTimeSpan(doc.GetTimeSpan(doc.GetOrdinal("departure_time"))),
            TenHanhKhach = doc.IsDBNull(doc.GetOrdinal("passenger_full_name")) ? "" : doc.GetString(doc.GetOrdinal("passenger_full_name")),
            SoGiayTo = doc.IsDBNull(doc.GetOrdinal("passenger_id_card")) ? "" : doc.GetString(doc.GetOrdinal("passenger_id_card")),
            MaDatVe = doc.IsDBNull(doc.GetOrdinal("booking_id")) ? null : doc.GetInt32(doc.GetOrdinal("booking_id"))
        };
    }
}
