using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

// Báo cáo PAID theo NGÀY LẬP hóa đơn. Schema chưa có ngày nhận tiền riêng.
public sealed class ThongKeDAO
{
    private const string DieuKien = """
        (@from IS NULL OR i.invoice_date>=@from) AND (@to IS NULL OR i.invoice_date<@to)
        AND (@year IS NULL OR YEAR(i.invoice_date)=@year)
        AND (@month IS NULL OR MONTH(i.invoice_date)=@month)
        AND (@quarter IS NULL OR DATEPART(QUARTER,i.invoice_date)=@quarter)
        """;

    private static SqlCommand TaoLenh(SqlConnection ketNoi, SqlTransaction giaoDich, string cauSQL, BoLocThongKeDTO boLoc)
    {
        var lenh = new SqlCommand(cauSQL, ketNoi, giaoDich);
        lenh.Parameters.Add("@from", SqlDbType.DateTime2).Value = (object?)boLoc.TuNgay?.Date ?? DBNull.Value;
        lenh.Parameters.Add("@to", SqlDbType.DateTime2).Value = (object?)boLoc.DenNgay?.Date.AddDays(1) ?? DBNull.Value;
        lenh.Parameters.Add("@year", SqlDbType.Int).Value = (object?)boLoc.Nam ?? DBNull.Value;
        lenh.Parameters.Add("@month", SqlDbType.Int).Value = (object?)boLoc.Thang ?? DBNull.Value;
        lenh.Parameters.Add("@quarter", SqlDbType.Int).Value = (object?)boLoc.Quy ?? DBNull.Value;
        return lenh;
    }

    public ThongKeDoanhThuDTO ThongKeTongHop(BoLocThongKeDTO boLoc)
    {
        using var ketNoi = MyConnection.CreateConnection();
        ketNoi.Open();
        using var giaoDich = ketNoi.BeginTransaction(IsolationLevel.Serializable);
        // Đọc khóa chuyến trước chứng từ như luồng thanh toán, giữ một ảnh dữ liệu cho cả báo cáo.
        using (var khoa = new SqlCommand("SELECT flight_id FROM dbo.FLIGHT WITH(HOLDLOCK) ORDER BY flight_id", ketNoi, giaoDich))
        using (var doc = khoa.ExecuteReader())
        {
            while (doc.Read())
            {
            }
        }
        using (var kiemTra = TaoLenh(ketNoi, giaoDich, """
            SELECT COUNT(*) FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id
            WHERE i.status='PAID' AND (
             i.total<>COALESCE((SELECT SUM(d.subtotal) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id),0)
             OR b.total_amount<>i.total_amount
             OR b.ticket_quantity<>(SELECT COUNT(*) FROM dbo.INVOICE_DETAIL d WHERE d.invoice_id=i.invoice_id)) AND
            """ + DieuKien, boLoc))
            if (Convert.ToInt32(kiemTra.ExecuteScalar()) > 0)
                throw new InvalidOperationException("Hóa đơn PAID và chi tiết không khớp. Chưa tính báo cáo; cần kiểm tra chứng từ gốc, không tự bù số liệu.");
        var ketQua = new ThongKeDoanhThuDTO();
        using (var lenh = TaoLenh(ketNoi, giaoDich, """
            SELECT COALESCE(SUM(CASE WHEN i.status='PAID' THEN i.total_amount ELSE 0 END),0),
             COALESCE(SUM(CASE WHEN i.status='UNPAID' THEN i.total_amount ELSE 0 END),0),COUNT(*),
             COALESCE(SUM(CASE WHEN i.status='PAID' THEN 1 ELSE 0 END),0),
             COALESCE(SUM(CASE WHEN i.status='UNPAID' THEN 1 ELSE 0 END),0),
             COALESCE(SUM(CASE WHEN i.status='CANCELLED' THEN 1 ELSE 0 END),0)
            FROM dbo.INVOICE i WHERE
            """ + DieuKien, boLoc))
        using (var doc = lenh.ExecuteReader())
        {
            if (doc.Read())
            {
                ketQua.DoanhThuDaThu = doc.GetDecimal(0);
                ketQua.TongDoanhThu = doc.GetDecimal(0);
                ketQua.DoanhThuChuaThu = doc.GetDecimal(1);
                ketQua.TongSoHoaDon = doc.GetInt32(2);
                ketQua.SoHoaDonDaThanhToan = doc.GetInt32(3);
                ketQua.SoHoaDonChuaThanhToan = doc.GetInt32(4);
                ketQua.SoHoaDonDaHuy = doc.GetInt32(5);
            }
        }
        using (var lenh = TaoLenh(ketNoi, giaoDich, """
            SELECT COUNT(*) FROM dbo.TICKET t JOIN dbo.BOOKING_DETAIL d ON d.ticket_id=t.ticket_id
            JOIN dbo.INVOICE i ON i.booking_id=d.booking_id WHERE t.status='ISSUED' AND
            """ + DieuKien, boLoc))
            ketQua.TongSoVeBan = Convert.ToInt32(lenh.ExecuteScalar());

        using (var lenh = TaoLenh(ketNoi, giaoDich, """
            SELECT YEAR(i.invoice_date),MONTH(i.invoice_date),DATEPART(QUARTER,i.invoice_date),COUNT(*),SUM(b.ticket_quantity),SUM(i.total_amount)
            FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE i.status='PAID' AND
            """ + DieuKien + " GROUP BY YEAR(i.invoice_date),MONTH(i.invoice_date),DATEPART(QUARTER,i.invoice_date) ORDER BY 1,2", boLoc))
        using (var doc = lenh.ExecuteReader())
        {
            while (doc.Read())
                ketQua.TheoThoiGian.Add(new DoanhThuTheoKyDTO { Nam = doc.GetInt32(0), Thang = doc.GetInt32(1), Quy = doc.GetInt32(2), SoHoaDon = doc.GetInt32(3), SoVe = doc.GetInt32(4), DoanhThu = doc.GetDecimal(5) });
        }
        using (var lenh = TaoLenh(ketNoi, giaoDich, """
            SELECT YEAR(i.invoice_date),DATEPART(QUARTER,i.invoice_date),COUNT(*),SUM(b.ticket_quantity),SUM(i.total_amount)
            FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE i.status='PAID' AND
            """ + DieuKien + " GROUP BY YEAR(i.invoice_date),DATEPART(QUARTER,i.invoice_date) ORDER BY 1,2", boLoc))
        using (var doc = lenh.ExecuteReader())
        {
            while (doc.Read())
                ketQua.DoanhThuTheoQuy.Add(new DoanhThuTheoQuyDTO { Nam = doc.GetInt32(0), Quy = doc.GetInt32(1), SoHoaDon = doc.GetInt32(2), SoVe = doc.GetInt32(3), DoanhThu = doc.GetDecimal(4) });
        }
        using (var lenh = TaoLenh(ketNoi, giaoDich, """
            SELECT f.flight_id,dep.city+N' → '+arr.city,SUM(b.ticket_quantity),SUM(i.total_amount)
            FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id JOIN dbo.FLIGHT f ON f.flight_id=b.flight_id
            JOIN dbo.AIRPORT dep ON dep.airport_id=f.departure_airport_id JOIN dbo.AIRPORT arr ON arr.airport_id=f.arrival_airport_id
            WHERE i.status='PAID' AND
            """ + DieuKien + " GROUP BY f.flight_id,dep.city,arr.city ORDER BY f.flight_id", boLoc))
        using (var doc = lenh.ExecuteReader())
        {
            while (doc.Read())
                ketQua.DoanhThuTheoChuyenBay.Add(new DoanhThuTheoChuyenBayDTO { MaChuyenBay = doc.GetInt32(0), TuyenBay = doc.GetString(1), SoVeDaBan = doc.GetInt32(2), DoanhThu = doc.GetDecimal(3) });
        }
        ketQua.TheoNhanVien = DocMaTran(ketNoi, giaoDich, boLoc, "NhanVien");
        ketQua.TheoKhachHang = DocMaTran(ketNoi, giaoDich, boLoc, "KhachHang");
        ketQua.TheoChuyenBay = DocMaTran(ketNoi, giaoDich, boLoc, "ChuyenBay");
        ketQua.TheoHangVe = DocMaTran(ketNoi, giaoDich, boLoc, "HangVe");
        giaoDich.Commit();
        return ketQua;
    }

    private static List<MaTranDoanhThuDTO> DocMaTran(SqlConnection ketNoi, SqlTransaction giaoDich, BoLocThongKeDTO boLoc, string loai)
    {
        string nguon;
        // Tên cột/nguồn chỉ lấy từ bốn nhánh cố định, không ghép identifier người dùng vào SQL.
        if (loai == "NhanVien")
            nguon = "SELECT COALESCE(CONVERT(nvarchar(20),b.employee_id),N'WEB') Ma,COALESCE(e.full_name,N'WEB · không nhân viên') Ten,i.invoice_date Ngay,i.total_amount Tien FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id LEFT JOIN dbo.EMPLOYEE e ON e.employee_id=b.employee_id WHERE i.status='PAID' AND " + DieuKien;
        else if (loai == "KhachHang")
            nguon = "SELECT CONVERT(nvarchar(20),b.customer_id) Ma,c.full_name Ten,i.invoice_date Ngay,i.total_amount Tien FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id JOIN dbo.CUSTOMER c ON c.customer_id=b.customer_id WHERE i.status='PAID' AND " + DieuKien;
        else if (loai == "ChuyenBay")
            nguon = "SELECT CONVERT(nvarchar(20),b.flight_id) Ma,N'Chuyến #'+CONVERT(nvarchar(20),b.flight_id) Ten,i.invoice_date Ngay,i.total_amount Tien FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE i.status='PAID' AND " + DieuKien;
        else if (loai == "HangVe")
        {
            // Phân bổ tiền PAID theo tỷ trọng thành tiền lịch sử. Gốc 0 chia đều;
            // dòng cuối nhận phần lẻ để tổng hạng vé đúng bằng tổng hóa đơn tới từng xu.
            nguon = """
                SELECT Ma,Ten,Ngay,CAST(CASE WHEN ThuTu=SoDong THEN TongTien-COALESCE(SUM(TienLamTron) OVER(PARTITION BY invoice_id ORDER BY ThuTu ROWS BETWEEN UNBOUNDED PRECEDING AND 1 PRECEDING),0) ELSE TienLamTron END AS decimal(18,2)) Tien
                FROM (SELECT i.invoice_id,i.invoice_date Ngay,i.total_amount TongTien,
                 COALESCE(fc.class_name,N'Chưa phân loại') Ma,COALESCE(fc.class_name,N'Chưa phân loại') Ten,
                 ROW_NUMBER() OVER(PARTITION BY i.invoice_id ORDER BY d.invoice_detail_id) ThuTu,COUNT(*) OVER(PARTITION BY i.invoice_id) SoDong,
                 CAST(ROUND(CASE WHEN i.total>0 THEN i.total_amount*(CONVERT(decimal(28,10),COALESCE(d.subtotal,0))/i.total)
                  ELSE i.total_amount/COUNT(*) OVER(PARTITION BY i.invoice_id) END,2) AS decimal(18,2)) TienLamTron
                 FROM dbo.INVOICE i LEFT JOIN dbo.INVOICE_DETAIL d ON d.invoice_id=i.invoice_id
                 LEFT JOIN dbo.TICKET t ON t.ticket_id=d.ticket_id LEFT JOIN dbo.FARE_CLASS fc ON fc.fare_id=t.fare_id AND fc.flight_id=t.flight_id
                 WHERE i.status='PAID' AND
                """ + DieuKien + ") PhanBo";
        }
        else
            throw new ArgumentException("Đối tượng thống kê không hợp lệ.");
        string cauSQL = "WITH Nguon AS (" + nguon + ") SELECT Ma,Ten,YEAR(Ngay)," +
            "SUM(CASE WHEN DATEPART(QUARTER,Ngay)=1 THEN Tien ELSE 0 END)," +
            "SUM(CASE WHEN DATEPART(QUARTER,Ngay)=2 THEN Tien ELSE 0 END)," +
            "SUM(CASE WHEN DATEPART(QUARTER,Ngay)=3 THEN Tien ELSE 0 END)," +
            "SUM(CASE WHEN DATEPART(QUARTER,Ngay)=4 THEN Tien ELSE 0 END),SUM(Tien) FROM Nguon GROUP BY Ma,Ten,YEAR(Ngay) ORDER BY 3,2,1";
        using var lenh = TaoLenh(ketNoi, giaoDich, cauSQL, boLoc);
        using var doc = lenh.ExecuteReader();
        var ketQua = new List<MaTranDoanhThuDTO>();
        while (doc.Read())
            ketQua.Add(new MaTranDoanhThuDTO { Ma = doc.GetString(0), Ten = doc.GetString(1), Nam = doc.GetInt32(2), Quy1 = doc.GetDecimal(3), Quy2 = doc.GetDecimal(4), Quy3 = doc.GetDecimal(5), Quy4 = doc.GetDecimal(6), Tong = doc.GetDecimal(7) });
        return ketQua;
    }
}
