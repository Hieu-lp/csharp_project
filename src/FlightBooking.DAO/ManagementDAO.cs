using System.Data;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.DAO;

public sealed class ManagementDAO(string connectionString)
{
    private readonly MyConnection connection = new(connectionString);
    public T Read<T>(Func<ManagementData,T> action)
    {
        using var sql = connection.Open();
        return action(new(sql,null));
    }
    public T Write<T>(Func<ManagementData,T> action)
    {
        using var sql = connection.Open();
        using var transaction = sql.BeginTransaction(IsolationLevel.Serializable);
        var result = action(new(sql,transaction));
        transaction.Commit();
        return result;
    }
}

public sealed class ManagementData(SqlConnection connection, SqlTransaction? transaction)
{
    private SqlCommand Command(string sql, params (string Name, object Value)[] args)
    {
        var c = new SqlCommand(sql,connection,transaction);
        foreach(var (name,value) in args)
        {
            var p = c.Parameters.AddWithValue(name,value);
            if(value is DateOnly d) { p.SqlDbType=SqlDbType.Date; p.Value=d.ToDateTime(TimeOnly.MinValue); }
            if(value is TimeOnly t) { p.SqlDbType=SqlDbType.Time; p.Value=t.ToTimeSpan(); }
            if(value is decimal) { p.Precision=18; p.Scale=2; }
        }
        return c;
    }
    private int Execute(string sql, params (string Name,object Value)[] args)
    { using var c=Command(sql,args); return c.ExecuteNonQuery(); }
    private int Scalar(string sql, params (string Name,object Value)[] args)
    { using var c=Command(sql,args); return Convert.ToInt32(c.ExecuteScalar()); }
    public List<AirportDTO> Airports()
    {
        using var c=Command("SELECT airport_id,airport_name,city FROM dbo.AIRPORT ORDER BY airport_id");
        using var r=c.ExecuteReader(); var list=new List<AirportDTO>();
        while(r.Read()) list.Add(new(r.GetInt32(0),r.GetString(1),r.GetString(2)));
        return list;
    }
    public AirportDTO? Airport(int id, bool locked=false)
    {
        using var c=Command("SELECT airport_id,airport_name,city FROM dbo.AIRPORT "+(locked?"WITH(UPDLOCK,HOLDLOCK) ":"")+"WHERE airport_id=@id",("@id",id));
        using var r=c.ExecuteReader(); return r.Read()?new(r.GetInt32(0),r.GetString(1),r.GetString(2)):null;
    }
    public bool AirportDuplicate(AirportDTO a) => Scalar("SELECT COUNT(*) FROM dbo.AIRPORT WITH(UPDLOCK,HOLDLOCK) WHERE airport_name=@name AND city=@city AND airport_id<>@id",("@name",a.AirportName),("@city",a.City),("@id",a.AirportId))>0;
    public bool AirportUsed(int id) => Scalar("SELECT COUNT(*) FROM dbo.FLIGHT WHERE departure_airport_id=@id OR arrival_airport_id=@id",("@id",id))>0;
    public int SaveAirport(AirportDTO a) => a.AirportId==0
        ?Scalar("INSERT dbo.AIRPORT(airport_name,city) OUTPUT INSERTED.airport_id VALUES(@name,@city)",("@name",a.AirportName),("@city",a.City))
        :Execute("UPDATE dbo.AIRPORT SET airport_name=@name,city=@city WHERE airport_id=@id",("@name",a.AirportName),("@city",a.City),("@id",a.AirportId));
    public void DeleteAirport(int id)=>Execute("DELETE dbo.AIRPORT WHERE airport_id=@id",("@id",id));

    private const string FlightSelect="""
        SELECT f.flight_id,f.departure_airport_id,f.arrival_airport_id,a.city,z.city,a.airport_name,z.airport_name,
        f.departure_date,f.departure_time,f.arrival_date,f.arrival_time,f.total_seats,f.seats_available,f.status,f.tickets_sold,f.total_revenue
        FROM dbo.FLIGHT f
        """;
    private const string Joins=" JOIN dbo.AIRPORT a ON a.airport_id=f.departure_airport_id JOIN dbo.AIRPORT z ON z.airport_id=f.arrival_airport_id ";
    private static FlightManagementDTO FlightRow(SqlDataReader r)=>new(new(r.GetInt32(0),r.GetInt32(1),r.GetInt32(2),r.GetString(3),r.GetString(4),r.GetString(5),r.GetString(6),DateOnly.FromDateTime(r.GetDateTime(7)),TimeOnly.FromTimeSpan(r.GetTimeSpan(8)),DateOnly.FromDateTime(r.GetDateTime(9)),TimeOnly.FromTimeSpan(r.GetTimeSpan(10)),r.GetInt32(11),r.GetInt32(12),r.GetString(13)),r.GetInt32(14),r.GetDecimal(15));
    public List<FlightManagementDTO> Flights()
    {
        using var c=Command(FlightSelect+Joins+" ORDER BY f.departure_date DESC,f.flight_id DESC");
        using var r=c.ExecuteReader();var list=new List<FlightManagementDTO>();
        while(r.Read())list.Add(FlightRow(r));return list;
    }
    public FlightManagementDTO? Flight(int id,bool locked=false)
    {
        using var c=Command(FlightSelect+(locked?" WITH(UPDLOCK,HOLDLOCK) ":" ")+Joins+" WHERE f.flight_id=@id",("@id",id));
        using var r=c.ExecuteReader();return r.Read()?FlightRow(r):null;
    }
    public bool FlightHasBookings(int id)=>Scalar("SELECT (SELECT COUNT(*) FROM dbo.BOOKING WHERE flight_id=@id)+(SELECT COUNT(*) FROM dbo.TICKET WHERE flight_id=@id)",("@id",id))>0;
    public bool FlightHasFares(int id)=>Scalar("SELECT COUNT(*) FROM dbo.FARE_CLASS WHERE flight_id=@id",("@id",id))>0;
    public List<string> ActiveSeats(int id)
    {
        using var c=Command("SELECT seat_number FROM dbo.TICKET WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@id AND status='ISSUED'",("@id",id));
        using var r=c.ExecuteReader();var seats=new List<string>();while(r.Read())seats.Add(r.GetString(0));return seats;
    }
    public int SaveFlight(FlightEdit f,int sold)
    {
        var args=new (string,object)[]{("@id",f.FlightId),("@from",f.DepartureAirportId),("@to",f.ArrivalAirportId),("@total",f.TotalSeats),("@available",f.TotalSeats-sold),("@dd",f.DepartureDate),("@dt",f.DepartureTime),("@ad",f.ArrivalDate),("@at",f.ArrivalTime),("@status",f.Status)};
        return f.FlightId==0?Scalar("INSERT dbo.FLIGHT(departure_airport_id,arrival_airport_id,total_seats,tickets_sold,seats_available,total_revenue,departure_date,departure_time,arrival_date,arrival_time,status) OUTPUT INSERTED.flight_id VALUES(@from,@to,@total,0,@available,0,@dd,@dt,@ad,@at,@status)",args)
            :Execute("UPDATE dbo.FLIGHT SET departure_airport_id=@from,arrival_airport_id=@to,total_seats=@total,seats_available=@available,departure_date=@dd,departure_time=@dt,arrival_date=@ad,arrival_time=@at,status=@status WHERE flight_id=@id",args);
    }
    public void DeleteFlight(int id)=>Execute("DELETE dbo.FLIGHT WHERE flight_id=@id",("@id",id));
    public List<FareClassDTO> Fares(int? flightId=null)
    {
        using var c=Command("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS "+(flightId is null?"":"WHERE flight_id=@id ")+"ORDER BY flight_id,fare_id",("@id",flightId??0));
        using var r=c.ExecuteReader();var list=new List<FareClassDTO>();while(r.Read())list.Add(new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetDecimal(3),r.GetInt32(4)));return list;
    }
    public FareClassDTO? Fare(int id)
    {
        using var c=Command("SELECT fare_id,flight_id,class_name,price,base_baggage_kg FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE fare_id=@id",("@id",id));
        using var r=c.ExecuteReader();return r.Read()?new(r.GetInt32(0),r.GetInt32(1),r.GetString(2),r.GetDecimal(3),r.GetInt32(4)):null;
    }
    public bool FareUsed(int id)=>Scalar("SELECT COUNT(*) FROM dbo.TICKET WHERE fare_id=@id",("@id",id))>0;
    public bool FareDuplicate(FareClassDTO f)=>Scalar("SELECT COUNT(*) FROM dbo.FARE_CLASS WITH(UPDLOCK,HOLDLOCK) WHERE flight_id=@flight AND class_name=@name AND fare_id<>@id",("@flight",f.FlightId),("@name",f.ClassName),("@id",f.FareId))>0;
    public int SaveFare(FareClassDTO f)=>f.FareId==0
        ?Scalar("INSERT dbo.FARE_CLASS(flight_id,class_name,price,base_baggage_kg) OUTPUT INSERTED.fare_id VALUES(@flight,@name,@price,@kg)",("@flight",f.FlightId),("@name",f.ClassName),("@price",f.Price),("@kg",f.BaseBaggageKg))
        :Execute("UPDATE dbo.FARE_CLASS SET flight_id=@flight,class_name=@name,price=@price,base_baggage_kg=@kg WHERE fare_id=@id",("@flight",f.FlightId),("@name",f.ClassName),("@price",f.Price),("@kg",f.BaseBaggageKg),("@id",f.FareId));
    public void DeleteFare(int id)=>Execute("DELETE dbo.FARE_CLASS WHERE fare_id=@id",("@id",id));
}
