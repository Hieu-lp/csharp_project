using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class BookingDAO
{
    private readonly MyConnection connection;
    private const string ConnectionFailureMessage = "Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB.";

    public BookingDAO(string connectionString)
    {
        connection = new MyConnection(connectionString);
    }

    public BookingDAO(MyConnection connection)
    {
        this.connection = connection;
    }

    private static BookingException DatabaseError(SqlException error)
    {
        if (error.Number == 208)
            return new BookingException("Database chưa đủ bảng. Cần người phụ trách Database kiểm tra script 01–03 và duyệt script 08.", error);
        if (error.Number == 1205 || error.Number == 2601 || error.Number == 2627)
            return new BookingException("Dữ liệu vừa thay đổi hoặc yêu cầu đang xử lý. Vui lòng gửi lại cùng biểu mẫu.", error);
        return new BookingException("Không thể xử lý với SQL Server. Vui lòng thử lại hoặc liên hệ nhóm quản lý.", error);
    }

    private SqlConnection OpenConnection()
    {
        try { return connection.Open(); }
        catch (ArgumentException error)
        {
            throw new BookingException(ConnectionFailureMessage, error);
        }
    }

    public T Read<T>(Func<BookingData, T> action)
    {
        try
        {
            using var sql = OpenConnection();
            return action(new BookingData(sql, null));
        }
        catch (SqlException error) { throw DatabaseError(error); }
        catch (InvalidOperationException error) { throw new BookingException(ConnectionFailureMessage, error); }
    }

    public T Write<T>(Func<BookingData, T> action)
    {
        // API cũ vẫn dùng cùng ranh giới giao dịch.
        return InTransaction(action);
    }

    public void InTransaction(Action<BookingData> action)
    {
        InTransaction<int>(data => { action(data); return 0; });
    }

    public T InTransaction<T>(Func<BookingData, T> action)
    {
        try
        {
            using var sql = OpenConnection();
            using var transaction = sql.BeginTransaction(IsolationLevel.Serializable);
            var result = action(new BookingData(sql, transaction));
            transaction.Commit();
            return result;
        }
        // Nếu action/commit lỗi, using giải phóng transaction chưa commit để rollback.
        catch (SqlException error) { throw DatabaseError(error); }
        catch (InvalidOperationException error) { throw new BookingException(ConnectionFailureMessage, error); }
    }

    public FlightDTO GetFlight(int id)
    {
        return Read(data => data.Flight(id));
    }

    public List<AirportDTO> GetAirports()
    {
        return Read(data => data.Airports());
    }

    public List<string> GetFareClassNames()
    {
        return Read(data => data.FareClassNames());
    }

    public BookingDTO? GetBooking(int id, string hash)
    {
        return Read(data => data.Booking(id, hash));
    }
}

// Context dùng chung kết nối/transaction, chỉ tồn tại trong lần gọi Read/InTransaction.
public sealed class BookingData
{
    private readonly SqlConnection connection;
    private readonly SqlTransaction? transaction;

    public BookingData(SqlConnection connection, SqlTransaction? transaction)
    {
        this.connection = connection;
        this.transaction = transaction;
    }
    private SqlCommand Command(string sql, params (string Name, object? Value)[] args)
    {
        var command = new SqlCommand(sql, connection, transaction);
        foreach (var (name, value) in args)
        {
            var p = command.Parameters.AddWithValue(name, value ?? DBNull.Value);
            if (value is decimal)
            {
                p.Precision = 18;
                p.Scale = 2;
            }
            if (value is DateOnly date)
            {
                p.SqlDbType = SqlDbType.Date;
                p.Value = date.ToDateTime(TimeOnly.MinValue);
            }
        }
        return command;
    }

    private int Execute(string sql, params (string Name, object? Value)[] args)
    {
        using var command = Command(sql, args);
        return command.ExecuteNonQuery();
    }

    private int Identity(string sql, params (string Name, object? Value)[] args)
    {
        using var command = Command(sql, args);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public void LockRequest(string hash)
    {
        using var command = Command("""
            DECLARE @result int;
            EXEC @result = sys.sp_getapplock @Resource=@key, @LockMode='Exclusive',
                @LockOwner='Transaction', @LockTimeout=10000;
            SELECT @result;
            """, ("@key", "booking:" + hash));
        if (Convert.ToInt32(command.ExecuteScalar()) < 0)
            throw new BookingException("Yêu cầu đang được xử lý. Vui lòng thử lại với cùng biểu mẫu.");
    }

    public (int BookingId, string PayloadHash)? FindRequest(string hash)
    {
        using var command = Command("SELECT booking_id,payload_hash FROM dbo.BOOKING_WEB_REQUEST WITH (UPDLOCK,HOLDLOCK) WHERE request_hash=@hash", ("@hash", hash));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return (reader.GetInt32(0), reader.GetString(1));
    }

    public void SaveRequest(string hash, string payloadHash, int bookingId)
    {
        Execute("""
        INSERT dbo.BOOKING_WEB_REQUEST(request_hash,payload_hash,booking_id) VALUES (@hash,@payload,@booking)
        """, ("@hash", hash), ("@payload", payloadHash), ("@booking", bookingId));
    }

    public List<AirportDTO> Airports()
    {
        using var command = Command("SELECT airport_id,airport_name,city FROM dbo.AIRPORT ORDER BY city,airport_id");
        using var reader = command.ExecuteReader();
        var result = new List<AirportDTO>();
        while (reader.Read())
        {
            var airport = new AirportDTO(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
            result.Add(airport);
        }
        return result;
    }

    private const string FlightFields = """
        f.flight_id,f.departure_airport_id,f.arrival_airport_id,
        a.city,z.city,a.airport_name,z.airport_name,
        f.departure_date,f.departure_time,f.arrival_date,f.arrival_time,
        f.total_seats,f.seats_available,f.status
        """;
    private const string FlightColumns = "SELECT " + FlightFields + " FROM dbo.FLIGHT f ";
    private const string AirportJoins = """
        JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id
        JOIN dbo.AIRPORT z ON z.airport_id=f.arrival_airport_id
        """;
    private static FlightDTO MapFlight(SqlDataReader r)
    {
        return new FlightDTO(r.GetInt32(0), r.GetInt32(1), r.GetInt32(2),
            r.GetString(3), r.GetString(4), r.GetString(5), r.GetString(6),
            DateOnly.FromDateTime(r.GetDateTime(7)), TimeOnly.FromTimeSpan(r.GetTimeSpan(8)),
            DateOnly.FromDateTime(r.GetDateTime(9)), TimeOnly.FromTimeSpan(r.GetTimeSpan(10)),
            r.GetInt32(11), r.GetInt32(12), r.GetString(13));
    }

    public List<FlightDTO> Flights(FlightSearch search)
    {
        using var command = Command(FlightColumns + " " + AirportJoins + """
             WHERE f.departure_airport_id=@departure AND f.arrival_airport_id=@arrival
                AND f.departure_date=@date AND f.status='SCHEDULED' AND f.seats_available>=@quantity
             ORDER BY f.departure_time,f.flight_id
            """, ("@departure", search.DepartureAirportId), ("@arrival", search.ArrivalAirportId),
            ("@date", search.DepartureDate), ("@quantity", search.Passengers));
        using var reader = command.ExecuteReader();
        var result = new List<FlightDTO>();
        while (reader.Read()) result.Add(MapFlight(reader));
        return result;
    }

    public List<string> FareClassNames()
    {
        using var command = Command("SELECT DISTINCT class_name FROM dbo.FARE_CLASS ORDER BY class_name");
        using var reader = command.ExecuteReader();
        var names = new List<string>();
        while (reader.Read()) names.Add(reader.GetString(0));
        return names;
    }

    public List<FlightOfferDTO> CheapestFlights(DateTime now)
    {
        using var command = Command("SELECT TOP (4) " + FlightFields + """
            , cheapest.fare_id,cheapest.class_name,cheapest.price,cheapest.base_baggage_kg
            FROM dbo.FLIGHT f
            """ + " " + AirportJoins + """
             CROSS APPLY (
                SELECT TOP (1) fc.fare_id,fc.class_name,fc.price,fc.base_baggage_kg
                FROM dbo.FARE_CLASS fc WHERE fc.flight_id=f.flight_id
                ORDER BY fc.price,fc.fare_id
             ) cheapest
             WHERE f.status='SCHEDULED' AND f.seats_available>0
               AND (f.departure_date>@date OR (f.departure_date=@date AND f.departure_time>@time))
             ORDER BY cheapest.price,f.departure_date,f.departure_time,f.flight_id,cheapest.fare_id
            """, ("@date", DateOnly.FromDateTime(now)), ("@time", now.TimeOfDay));
        using var r = command.ExecuteReader();
        var offers = new List<FlightOfferDTO>();
        while (r.Read())
        {
            FlightDTO flight = MapFlight(r);
            var fare = new FareClassDTO(r.GetInt32(14), r.GetInt32(0), r.GetString(15), r.GetDecimal(16), r.GetInt32(17));
            offers.Add(new FlightOfferDTO(flight, fare));
        }
        return offers;
    }

    public FlightDTO Flight(int id, bool locked = false)
    {
        using var command = Command(FlightColumns + (locked ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + AirportJoins +
            " WHERE f.flight_id=@id", ("@id", id));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) throw new BookingException("Không tìm thấy chuyến bay.");
        return MapFlight(reader);
    }

    public List<FareClassDTO> Fares(int flightId, bool locked = false)
    {
        using var command = Command("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS " +
            (locked ? "WITH (UPDLOCK,HOLDLOCK) " : "") + "WHERE flight_id=@id ORDER BY price,fare_id", ("@id", flightId));
        using var reader = command.ExecuteReader();
        var result = new List<FareClassDTO>();
        while (reader.Read())
        {
            var fare = new FareClassDTO(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4));
            result.Add(fare);
        }
        return result;
    }

    public DiscountCodeDTO? Discount(string code, bool locked = false)
    {
        using var command = Command("""
            SELECT discount_id,code,discount_type,discount_value,min_order_amount,max_discount_amount,
                start_date,end_date,usage_limit,used_count,status FROM dbo.DISCOUNT_CODE
            """ + (locked ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + "WHERE code=@code", ("@code", code));
        using var r = command.ExecuteReader();
        if (!r.Read()) return null;
        return new DiscountCodeDTO(r.GetInt32(0), r.GetString(1), r.GetString(2), r.GetDecimal(3), r.GetDecimal(4),
            r.IsDBNull(5) ? null : r.GetDecimal(5), r.GetDateTime(6), r.GetDateTime(7),
            r.IsDBNull(8) ? null : r.GetInt32(8), r.GetInt32(9), r.GetString(10));
    }

    public HashSet<string> ActiveSeats(int flightId)
    {
        using var command = Command("SELECT seat_number FROM dbo.TICKET WITH (UPDLOCK,HOLDLOCK) WHERE flight_id=@id AND status='ISSUED'", ("@id", flightId));
        using var reader = command.ExecuteReader();
        var seats = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (reader.Read()) seats.Add(reader.GetString(0).Trim());
        return seats;
    }

    public int InsertCustomer(CustomerInput input)
    {
        return Identity("""
        INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number)
        OUTPUT INSERTED.customer_id VALUES (@name,@phone,@email,@dob,@gender,@passport)
        """, ("@name", input.FullName), ("@phone", input.Phone), ("@email", input.Email),
        ("@dob", input.DateOfBirth), ("@gender", input.Gender), ("@passport", input.IdPassportNumber));
    }

    public int InsertBooking(int customerId, BookingQuoteDTO quote, DateTime now)
    {
        return Identity("""
        INSERT dbo.BOOKING(customer_id,employee_id,flight_id,discount_id,booking_date,booking_source,
            ticket_quantity,discount_amount,total_amount,status)
        OUTPUT INSERTED.booking_id VALUES (@customer,NULL,@flight,@discount,@now,'WEB',@qty,@amount,@total,'CONFIRMED')
        """, ("@customer", customerId), ("@flight", quote.Flight.FlightId), ("@discount", quote.DiscountId),
        ("@now", now), ("@qty", quote.Quantity), ("@amount", quote.DiscountAmount), ("@total", quote.TotalAmount));
    }

    public int InsertInvoice(int bookingId, BookingQuoteDTO quote, DateTime now)
    {
        return Identity("""
        INSERT dbo.INVOICE(booking_id,invoice_date,total,discount_amount,tax_amount,total_amount,status)
        OUTPUT INSERTED.invoice_id VALUES (@booking,@now,@subtotal,@discount,0,@total,'UNPAID')
        """, ("@booking", bookingId), ("@now", now), ("@subtotal", quote.Subtotal),
        ("@discount", quote.DiscountAmount), ("@total", quote.TotalAmount));
    }

    public void InsertPassenger(int bookingId, int invoiceId, BookingQuoteDTO quote, PassengerInput passenger, string seat, DateTime now)
    {
        var ticketId = Identity("""
            INSERT dbo.TICKET(flight_id,fare_id,seat_number,issued_at,status)
            OUTPUT INSERTED.ticket_id VALUES (@flight,@fare,@seat,@now,'ISSUED')
            """, ("@flight", quote.Flight.FlightId), ("@fare", quote.Fare.FareId), ("@seat", seat), ("@now", now));
        Execute("""
            INSERT dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price)
            VALUES (@booking,@ticket,@name,@identity,@price);
            INSERT dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal)
            VALUES (@invoice,@ticket,@price,1,@price);
            """, ("@booking", bookingId), ("@ticket", ticketId), ("@name", passenger.FullName),
            ("@identity", passenger.IdCard), ("@price", quote.Fare.Price), ("@invoice", invoiceId));
    }

    public void ReserveSeats(int flightId, int count)
    {
        if (Execute("""
            UPDATE dbo.FLIGHT SET tickets_sold=tickets_sold+@count,seats_available=seats_available-@count
            WHERE flight_id=@id AND status='SCHEDULED' AND seats_available>=@count
            """, ("@id", flightId), ("@count", count)) != 1)
            throw new BookingException("Chuyến bay không còn đủ chỗ.");
    }

    public void UseDiscount(int discountId)
    {
        if (Execute("""
            UPDATE dbo.DISCOUNT_CODE SET used_count=used_count+1 WHERE discount_id=@id
                AND (usage_limit IS NULL OR used_count<usage_limit) AND status='ACTIVE'
            """, ("@id", discountId)) != 1)
            throw new BookingException("Mã giảm giá không còn lượt sử dụng.");
    }

    public BookingDTO? Booking(int id, string hash, bool locked = false)
    {
        using var command = Command("""
            SELECT b.customer_id,b.flight_id,b.discount_id,b.booking_date,b.booking_source,b.ticket_quantity,
                b.discount_amount,b.total_amount,b.status,c.full_name,c.email,c.phone,i.status
            FROM dbo.BOOKING b
            """ + (locked ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + """
            JOIN dbo.BOOKING_WEB_REQUEST w ON w.booking_id=b.booking_id
            JOIN dbo.CUSTOMER c ON c.customer_id=b.customer_id
            JOIN dbo.INVOICE i
            """ + (locked ? " WITH (UPDLOCK,HOLDLOCK) " : " ") + """
             ON i.booking_id=b.booking_id WHERE b.booking_id=@id AND w.request_hash=@hash
            """, ("@id", id), ("@hash", hash));
        int customer, flight, quantity; int? discount;
        DateTime date; decimal discountAmount, total; string source, status, name, email, phone, invoiceStatus;
        using (var r = command.ExecuteReader())
        {
            if (!r.Read()) return null;
            customer = r.GetInt32(0);
            flight = r.GetInt32(1);
            discount = r.IsDBNull(2) ? null : r.GetInt32(2);
            date = r.GetDateTime(3);
            source = r.GetString(4);
            quantity = r.GetInt32(5);
            discountAmount = r.GetDecimal(6);
            total = r.GetDecimal(7);
            status = r.GetString(8);
            name = r.GetString(9);
            email = r.GetString(10);
            phone = r.GetString(11);
            invoiceStatus = r.GetString(12);
        }
        // Reader đã đóng trước khi đọc thêm chuyến và chi tiết trên cùng kết nối.
        return new(id, customer, flight, discount, date, source, quantity, discountAmount, total, status,
            name, email, phone, invoiceStatus, Flight(flight), Details(id));
    }

    public List<BookingDetailDTO> Details(int bookingId)
    {
        using var command = Command("""
            SELECT d.booking_detail_id,d.ticket_id,d.passenger_full_name,d.passenger_id_card,d.price,
                t.seat_number,f.class_name,t.status FROM dbo.BOOKING_DETAIL d
            JOIN dbo.TICKET t ON t.ticket_id=d.ticket_id
            JOIN dbo.FARE_CLASS f ON f.fare_id=t.fare_id AND f.flight_id=t.flight_id
            WHERE d.booking_id=@id ORDER BY d.booking_detail_id
            """, ("@id", bookingId));
        using var r = command.ExecuteReader();
        var result = new List<BookingDetailDTO>();
        while (r.Read())
        {
            var detail = new BookingDetailDTO(r.GetInt32(0), r.GetInt32(1), r.GetString(2), r.GetString(3),
                r.GetDecimal(4), r.GetString(5), r.GetString(6), r.GetString(7));
            result.Add(detail);
        }
        return result;
    }

    public void Cancel(BookingDTO booking)
    {
        if (Execute("UPDATE dbo.BOOKING SET status='CANCELLED' WHERE booking_id=@id AND status='CONFIRMED'", ("@id", booking.BookingId)) != 1)
            throw new BookingException("Trạng thái đơn vừa thay đổi. Vui lòng tải lại.");
        var count = Execute("""
            UPDATE t SET status='CANCELLED' FROM dbo.TICKET t
            JOIN dbo.BOOKING_DETAIL d ON d.ticket_id=t.ticket_id
            WHERE d.booking_id=@id AND t.status='ISSUED' AND t.flight_id=@flight
            """, ("@id", booking.BookingId), ("@flight", booking.FlightId));
        if (count != booking.TicketQuantity) throw new BookingException("Số vé không khớp. Liên hệ nhóm quản lý.");
        if (Execute("UPDATE dbo.INVOICE SET status='CANCELLED' WHERE booking_id=@id AND status='UNPAID'", ("@id", booking.BookingId)) != 1)
            throw new BookingException("Hóa đơn đã thay đổi. Không thể hủy trực tuyến.");
        if (Execute("""
            UPDATE dbo.FLIGHT SET tickets_sold=tickets_sold-@count,seats_available=seats_available+@count
            WHERE flight_id=@id AND tickets_sold>=@count
            """, ("@id", booking.FlightId), ("@count", count)) != 1)
            throw new BookingException("Số liệu ghế không khớp. Liên hệ nhóm quản lý.");
        if (booking.DiscountId is int discountId && Execute("""
            UPDATE dbo.DISCOUNT_CODE SET used_count=used_count-1 WHERE discount_id=@id AND used_count>0
            """, ("@id", discountId)) != 1)
            throw new BookingException("Số lượt mã giảm giá không khớp. Liên hệ nhóm quản lý.");
    }
}
