using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class ManagementDAO
{
    private readonly MyConnection connection;

    public ManagementDAO(string connectionString)
    {
        connection = new MyConnection(connectionString);
    }

    private const string ConnectionFailureMessage = "Chưa cấu hình hoặc chưa kết nối được FlightBookingDB.";

    // Exception dùng chung ở DTO; DAO không tham chiếu ngược tới BUS hoặc GUI.
    private static ManagementException DatabaseError(SqlException error)
    {
        if (error.Number == 547)
            return new ManagementException("Dữ liệu đang được tham chiếu hoặc sân bay/chuyến bay không còn tồn tại. Hãy tải lại.", error);
        if (error.Number == 2601 || error.Number == 2627)
            return new ManagementException("Thông tin bị trùng. Hãy tải lại và kiểm tra tên hạng vé.", error);
        if (error.Number == 1205)
            return new ManagementException("Dữ liệu đang được người khác xử lý. Hãy tải lại rồi thử lại.", error);
        return new ManagementException("Không thể xử lý SQL Server. Kiểm tra kết nối, quyền truy cập và cấu trúc database.", error);
    }

    private SqlConnection OpenConnection()
    {
        try
        {
            return connection.Open();
        }
        catch (ArgumentException error)
        {
            // Chuỗi kết nối sai cú pháp; chỉ bắt tại bước mở kết nối, không bắt lỗi tham số nghiệp vụ.
            throw new ManagementException(ConnectionFailureMessage, error);
        }
    }

    public T Read<T>(Func<ManagementData, T> action)
    {
        try
        {
            using var sql = OpenConnection();
            var data = new ManagementData(sql, null);
            return action(data);
        }
        catch (SqlException error)
        {
            throw DatabaseError(error);
        }
        catch (InvalidOperationException error)
        {
            throw new ManagementException(ConnectionFailureMessage, error);
        }
    }

    public T Write<T>(Func<ManagementData, T> action)
    {
        // Tương thích với API trước; mọi thao tác ghi dùng chung một cơ chế transaction.
        return InTransaction(action);
    }

    public void InTransaction(Action<ManagementData> action)
    {
        InTransaction<int>(data =>
        {
            action(data);
            return 0;
        });
    }

    public T InTransaction<T>(Func<ManagementData, T> action)
    {
        // Kiểm tra và ghi trong cùng kết nối, cùng transaction để giữ khóa đến khi commit.
        // Nếu action ném lỗi, Dispose transaction chưa commit sẽ rollback.
        try
        {
            using var sql = OpenConnection();
            using var transaction = sql.BeginTransaction(IsolationLevel.Serializable);
            var data = new ManagementData(sql, transaction);
            T result = action(data);
            transaction.Commit();
            return result;
        }
        catch (SqlException error)
        {
            throw DatabaseError(error);
        }
        catch (InvalidOperationException error)
        {
            throw new ManagementException(ConnectionFailureMessage, error);
        }
    }

    public List<AirportDTO> GetAirports()
    {
        return Read(data => data.Airports());
    }

    public List<FlightManagementDTO> GetFlights()
    {
        return Read(data => data.Flights());
    }

    public List<FareClassDTO> GetFares(int? flightId = null)
    {
        return Read(data => data.Fares(flightId));
    }
}

public sealed class ManagementData
{
    private readonly SqlConnection connection;
    private readonly SqlTransaction? transaction;

    public ManagementData(SqlConnection connection, SqlTransaction? transaction)
    {
        this.connection = connection;
        this.transaction = transaction;
    }

    private SqlCommand Command(string sql, params (string Name, object Value)[] args)
    {
        var command = new SqlCommand(sql, connection, transaction);
        foreach (var (name, value) in args)
        {
            var parameter = command.Parameters.AddWithValue(name, value);
            if (value is DateOnly date)
            {
                parameter.SqlDbType = SqlDbType.Date;
                parameter.Value = date.ToDateTime(TimeOnly.MinValue);
            }
            if (value is TimeOnly time)
            {
                parameter.SqlDbType = SqlDbType.Time;
                parameter.Value = time.ToTimeSpan();
            }
            if (value is decimal)
            {
                parameter.Precision = 18;
                parameter.Scale = 2;
            }
        }
        return command;
    }

    private int Execute(string sql, params (string Name, object Value)[] args)
    {
        using var command = Command(sql, args);
        return command.ExecuteNonQuery();
    }

    private int Scalar(string sql, params (string Name, object Value)[] args)
    {
        using var command = Command(sql, args);
        return Convert.ToInt32(command.ExecuteScalar());
    }

    public List<AirportDTO> Airports()
    {
        using var command = Command("SELECT airport_id,airport_name,city FROM dbo.AIRPORT ORDER BY airport_id");
        using var reader = command.ExecuteReader();
        var list = new List<AirportDTO>();
        while (reader.Read())
        {
            var airport = new AirportDTO(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
            list.Add(airport);
        }
        return list;
    }

    public AirportDTO? Airport(int id, bool locked = false)
    {
        using var command = Command("SELECT airport_id,airport_name,city FROM dbo.AIRPORT " + (locked ? "WITH(UPDLOCK,HOLDLOCK) " : "") + "WHERE airport_id=@id", ("@id", id));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new AirportDTO(reader.GetInt32(0), reader.GetString(1), reader.GetString(2));
    }

    public bool AirportDuplicate(AirportDTO airport)
    {
        int count = Scalar("SELECT COUNT(*) FROM dbo.AIRPORT WITH(UPDLOCK,HOLDLOCK) WHERE airport_name=@name AND city=@city AND airport_id<>@id",
            ("@name", airport.AirportName), ("@city", airport.City), ("@id", airport.AirportId));
        return count > 0;
    }

    public bool AirportUsed(int id)
    {
        int count = Scalar("SELECT COUNT(*) FROM dbo.FLIGHT WHERE departure_airport_id=@id OR arrival_airport_id=@id", ("@id", id));
        return count > 0;
    }

    public int SaveAirport(AirportDTO airport)
    {
        // ID bằng 0 là thêm mới: trả về ID do SQL Server sinh. Sửa trả số dòng cập nhật.
        if (airport.AirportId == 0)
        {
            return InsertAirport(airport);
        }
        return UpdateAirport(airport);
    }

    public int InsertAirport(AirportDTO airport)
    {
        return Scalar("INSERT dbo.AIRPORT(airport_name,city) OUTPUT INSERTED.airport_id VALUES(@name,@city)",
            ("@name", airport.AirportName), ("@city", airport.City));
    }

    public int UpdateAirport(AirportDTO airport)
    {
        return Execute("UPDATE dbo.AIRPORT SET airport_name=@name,city=@city WHERE airport_id=@id",
            ("@name", airport.AirportName), ("@city", airport.City), ("@id", airport.AirportId));
    }

    public void DeleteAirport(int id)
    {
        Execute("DELETE dbo.AIRPORT WHERE airport_id=@id", ("@id", id));
    }

    private const string FlightSelect="""
        SELECT f.flight_id,f.departure_airport_id,f.arrival_airport_id,a.city,z.city,a.airport_name,z.airport_name,
        f.departure_date,f.departure_time,f.arrival_date,f.arrival_time,f.total_seats,f.seats_available,f.status,f.tickets_sold,f.total_revenue
        FROM dbo.FLIGHT f
        """;
    private const string Joins=" JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id JOIN dbo.AIRPORT z ON z.airport_id=f.arrival_airport_id ";
    private static FlightManagementDTO FlightRow(SqlDataReader reader)
    {
        var flight = new FlightDTO(
            reader.GetInt32(0), reader.GetInt32(1), reader.GetInt32(2),
            reader.GetString(3), reader.GetString(4), reader.GetString(5), reader.GetString(6),
            DateOnly.FromDateTime(reader.GetDateTime(7)), TimeOnly.FromTimeSpan(reader.GetTimeSpan(8)),
            DateOnly.FromDateTime(reader.GetDateTime(9)), TimeOnly.FromTimeSpan(reader.GetTimeSpan(10)),
            reader.GetInt32(11), reader.GetInt32(12), reader.GetString(13));
        int ticketsSold = reader.GetInt32(14);
        decimal totalRevenue = reader.GetDecimal(15);
        return new FlightManagementDTO(flight, ticketsSold, totalRevenue);
    }

    public List<FlightManagementDTO> Flights()
    {
        using var command = Command(FlightSelect + Joins + " ORDER BY f.departure_date DESC,f.flight_id DESC");
        using var reader = command.ExecuteReader();
        var list = new List<FlightManagementDTO>();
        while (reader.Read())
        {
            list.Add(FlightRow(reader));
        }
        return list;
    }

    public FlightManagementDTO? Flight(int id, bool locked = false)
    {
        using var command = Command(FlightSelect + (locked ? " WITH(UPDLOCK,HOLDLOCK) " : " ") + Joins + " WHERE f.flight_id=@id", ("@id", id));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return FlightRow(reader);
    }

    public bool FlightHasBookings(int id)
    {
        int count = Scalar("SELECT (SELECT COUNT(*) FROM dbo.BOOKING WHERE flight_id=@id)+(SELECT COUNT(*) FROM dbo.TICKET WHERE flight_id=@id)", ("@id", id));
        return count > 0;
    }

    public bool FlightHasFares(int id)
    {
        int count = Scalar("SELECT COUNT(*) FROM dbo.FARE_CLASS WHERE flight_id=@id", ("@id", id));
        return count > 0;
    }

    public List<string> ActiveSeats(int id)
    {
        using var command = Command("SELECT seat_number FROM dbo.TICKET WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@id AND status='ISSUED'", ("@id", id));
        using var reader = command.ExecuteReader();
        var seats = new List<string>();
        while (reader.Read())
        {
            seats.Add(reader.GetString(0));
        }
        return seats;
    }

    public int SaveFlight(FlightEdit flight, int sold)
    {
        if (flight.FlightId == 0) return InsertFlight(flight, sold);
        return UpdateFlight(flight, sold);
    }

    public int InsertFlight(FlightEdit flight, int sold)
    {
        // Ghế còn được tính từ tổng ghế và số vé thực tế; không nhận trực tiếp từ GUI.
        var args = new (string, object)[]
        {
            ("@id", flight.FlightId), ("@from", flight.DepartureAirportId), ("@to", flight.ArrivalAirportId),
            ("@total", flight.TotalSeats), ("@available", flight.TotalSeats - sold),
            ("@dd", flight.DepartureDate), ("@dt", flight.DepartureTime),
            ("@ad", flight.ArrivalDate), ("@at", flight.ArrivalTime), ("@status", flight.Status)
        };
        return Scalar("INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status) OUTPUT INSERTED.flight_id VALUES(@from,@to,@total,0,@available,0,@dd,@dt,@ad,@at,@status)", args);
    }

    public int UpdateFlight(FlightEdit flight, int sold)
    {
        var args = new (string, object)[]
        {
            ("@id", flight.FlightId), ("@from", flight.DepartureAirportId), ("@to", flight.ArrivalAirportId),
            ("@total", flight.TotalSeats), ("@available", flight.TotalSeats - sold),
            ("@dd", flight.DepartureDate), ("@dt", flight.DepartureTime),
            ("@ad", flight.ArrivalDate), ("@at", flight.ArrivalTime), ("@status", flight.Status)
        };
        return Execute("UPDATE dbo.FLIGHT SET departure_airport_id=@from,arrival_airport_id=@to,total_seats=@total,seats_available=@available,departure_date=@dd,departure_time=@dt,arrival_date=@ad,arrival_time=@at,status=@status WHERE flight_id=@id", args);
    }

    public void DeleteFlight(int id)
    {
        Execute("DELETE dbo.FLIGHT WHERE flight_id=@id", ("@id", id));
    }

    public List<FareClassDTO> Fares(int? flightId = null)
    {
        using var command = Command("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS " + (flightId is null ? "" : "WHERE flight_id=@id ") + "ORDER BY flight_id,fare_id", ("@id", flightId ?? 0));
        using var reader = command.ExecuteReader();
        var list = new List<FareClassDTO>();
        while (reader.Read())
        {
            var fare = new FareClassDTO(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4));
            list.Add(fare);
        }
        return list;
    }

    public FareClassDTO? Fare(int id)
    {
        using var command = Command("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE fare_id=@id", ("@id", id));
        using var reader = command.ExecuteReader();
        if (!reader.Read()) return null;
        return new FareClassDTO(reader.GetInt32(0), reader.GetInt32(1), reader.GetString(2), reader.GetDecimal(3), reader.GetInt32(4));
    }

    public bool FareUsed(int id)
    {
        int count = Scalar("SELECT COUNT(*) FROM dbo.TICKET WHERE fare_id=@id", ("@id", id));
        return count > 0;
    }

    public bool FareDuplicate(FareClassDTO fare)
    {
        int count = Scalar("SELECT COUNT(*) FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@flight AND class_name=@name AND fare_id<>@id",
            ("@flight", fare.FlightId), ("@name", fare.ClassName), ("@id", fare.FareId));
        return count > 0;
    }

    public int SaveFare(FareClassDTO fare)
    {
        if (fare.FareId == 0)
        {
            return Scalar("INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) OUTPUT INSERTED.fare_id VALUES(@flight,@name,@price,@kg)",
                ("@flight", fare.FlightId), ("@name", fare.ClassName), ("@price", fare.Price), ("@kg", fare.BaseBaggageKg));
        }
        return Execute("UPDATE dbo.FARE_CLASS SET flight_id=@flight,class_name=@name,price=@price,base_baggage_kg=@kg WHERE fare_id=@id",
            ("@flight", fare.FlightId), ("@name", fare.ClassName), ("@price", fare.Price), ("@kg", fare.BaseBaggageKg), ("@id", fare.FareId));
    }

    public void DeleteFare(int id)
    {
        Execute("DELETE dbo.FARE_CLASS WHERE fare_id=@id", ("@id", id));
    }
}
