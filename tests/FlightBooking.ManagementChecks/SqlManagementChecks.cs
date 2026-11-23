using System.Data;
using System.Text.RegularExpressions;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

internal static class SqlManagementChecks
{
    public static void Run(Action<bool,string> check,Action<Action,string> reject)
    {
        var cs=Environment.GetEnvironmentVariable("FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION")??throw new Exception("Set a dedicated test connection.");
        if(!new SqlConnectionStringBuilder(cs).InitialCatalog.EndsWith("_ManagementTests",StringComparison.Ordinal))throw new Exception("Only a dedicated *_ManagementTests database is allowed.");
        using var connection=new MyConnection(cs).Open();
        using(var exists=new SqlCommand("SELECT COUNT(*) FROM sys.tables WHERE name='AIRPORT'",connection))
            if((int)exists.ExecuteScalar()! == 0)
                foreach(var file in new[]{"02_CreateTables.sql","03_Constraints_Indexes.sql"})
                {
                    var sql=Regex.Replace(File.ReadAllText(Path.Combine("Database",file)),@"^USE FlightBookingDB;\s*$","",RegexOptions.Multiline);
                    foreach(var batch in Regex.Split(sql,@"^GO\s*$",RegexOptions.Multiline|RegexOptions.IgnoreCase))
                        if(!string.IsNullOrWhiteSpace(batch)){using var command=new SqlCommand(batch,connection);command.ExecuteNonQuery();}
                }
        var bus=new ManagementBUS(cs);var dao=new ManagementDAO(cs);var tag="MGMT-"+Guid.NewGuid().ToString("N")[..10];
        var airports=new List<int>();var flights=new List<int>();int customer=0;
        FlightManagementDTO Read(int id)=>bus.Flights().Cast<FlightManagementDTO>().Single(x=>x.Flight.FlightId==id);
        FareClassDTO Fare(int id)=>bus.Fares().Cast<FareClassDTO>().Single(x=>x.FareId==id);
        try
        {
            var a=bus.SaveAirport(new(0,tag+" A","Test A"));airports.Add(a);
            var b=bus.SaveAirport(new(0,tag+" B","Test B"));airports.Add(b);
            var original=bus.Airports().Cast<AirportDTO>().Single(x=>x.AirportId==a);
            bus.SaveAirport(original with{City="Updated city"},original);check(bus.Airports().Cast<AirportDTO>().Single(x=>x.AirportId==a).City=="Updated city","SQL airport add/read/update");
            reject(()=>bus.SaveAirport(original with{City="stale"},original),"SQL airport stale edit rejected");
            reject(()=>bus.SaveAirport(new(0,tag+" B","Test B")),"SQL duplicate airport pair rejected");
            var edit=new FlightEdit(0,a,b,120,new(2026,12,31),new(23,0),new(2027,1,1),new(1,0),"SCHEDULED");
            var id=bus.SaveFlight(edit);flights.Add(id);edit=edit with{FlightId=id};var initial=Read(id);
            reject(()=>bus.SaveFlight(edit with{FlightId=0,ArrivalAirportId=int.MaxValue}),"SQL nonexistent airport rejected");
            reject(()=>bus.SaveFare(new(0,int.MaxValue,"Test",100,7)),"SQL nonexistent flight rejected");
            check(initial.TicketsSold==0&&initial.TotalRevenue==0&&initial.Flight.SeatsAvailable==120,"SQL new flight counters");
            bus.SaveFlight(edit with{TotalSeats=130},initial);var current=Read(id);
            check(current.Flight.SeatsAvailable==130,"SQL seat change consistent");
            reject(()=>bus.SaveFlight(edit,initial),"SQL stale flight snapshot rejected");
            reject(()=>bus.DeleteAirport(original with{City="Updated city"}),"SQL airport referenced by flight cannot delete");
            var fareId=bus.SaveFare(new(0,id,"Test",250,7));var oldFare=Fare(fareId);
            check(bus.Fares(id).Cast<FareClassDTO>().All(f=>f.FlightId==id),"SQL child fares belong to selected flight");
            reject(()=>bus.SaveFare(new(0,id,"Test",300,10)),"SQL duplicate fare rejected");
            bus.SaveFare(oldFare with{Price=275},oldFare);check(Fare(fareId).Price==275,"SQL fare add/read/update");
            reject(()=>bus.SaveFare(oldFare with{Price=280},oldFare),"SQL stale fare rejected");
            bus.DeleteFare(Fare(fareId));check(!bus.Fares().Cast<FareClassDTO>().Any(f=>f.FareId==fareId),"SQL unreferenced fare deletes");
            fareId=bus.SaveFare(new(0,id,"Test",250,7));
            using(var fixture=new SqlCommand("""
                SET XACT_ABORT ON; BEGIN TRAN;
                INSERT dbo.CUSTOMER(full_name,phone,email,date_of_birth,gender,id_passport_number) VALUES(N'Management Test','0900000000','management@example.test','20000101',N'Nam','TEST-ONLY');
                DECLARE @customer int=SCOPE_IDENTITY();
                INSERT dbo.BOOKING(customer_id,flight_id,booking_source,ticket_quantity,discount_amount,total_amount,status) VALUES(@customer,@flight,'WEB',1,0,250,'CONFIRMED');
                DECLARE @booking int=SCOPE_IDENTITY();
                INSERT dbo.TICKET(flight_id,fare_id,seat_number,status) VALUES(@flight,@fare,'20F','ISSUED');
                DECLARE @ticket int=SCOPE_IDENTITY();
                INSERT dbo.BOOKING_DETAIL(booking_id,ticket_id,passenger_full_name,passenger_id_card,price) VALUES(@booking,@ticket,N'Passenger Test','TEST-PAX',250);
                INSERT dbo.INVOICE(booking_id,total,discount_amount,tax_amount,total_amount,status) VALUES(@booking,250,0,0,250,'UNPAID');
                INSERT dbo.INVOICE_DETAIL(invoice_id,ticket_id,unit_price,quantity,subtotal) VALUES(SCOPE_IDENTITY(),@ticket,250,1,250);
                UPDATE dbo.FLIGHT SET tickets_sold=1,seats_available=total_seats-1,total_revenue=250 WHERE flight_id=@flight;
                COMMIT; SELECT @customer;
                """,connection))
            {fixture.Parameters.AddWithValue("@flight",id);fixture.Parameters.AddWithValue("@fare",fareId);customer=Convert.ToInt32(fixture.ExecuteScalar());}
            current=Read(id);
            reject(()=>bus.SaveFlight(edit with{TotalSeats=100},current),"SQL shrink cannot remove issued 20F");
            reject(()=>bus.SaveFlight(edit with{Status="CANCELLED",TotalSeats=130},current),"SQL cancellation of booked flight blocked");
            reject(()=>bus.SaveFlight(edit with{DepartureTime=new(22,0),TotalSeats=130},current),"SQL reschedule booked flight blocked");
            reject(()=>bus.DeleteFlight(current),"SQL booked flight delete blocked");
            reject(()=>bus.DeleteFare(Fare(fareId)),"SQL referenced fare delete blocked");
            bus.SaveFlight(edit with{TotalSeats=140},current);
            check(Read(id).TicketsSold==1&&Read(id).Flight.SeatsAvailable==139,"SQL resize preserves booked counters");
            bus.SaveFare(Fare(fareId) with{Price=350},Fare(fareId));
            using(var history=new SqlCommand("SELECT COUNT(*) FROM dbo.BOOKING_DETAIL d JOIN dbo.INVOICE i ON i.booking_id=d.booking_id JOIN dbo.INVOICE_DETAIL x ON x.invoice_id=i.invoice_id WHERE d.booking_id IN(SELECT booking_id FROM dbo.BOOKING WHERE flight_id=@id) AND d.price=250 AND i.total_amount=250 AND x.unit_price=250",connection))
            {history.Parameters.AddWithValue("@id",id);check((int)history.ExecuteScalar()! == 1,"SQL fare price does not rewrite booking or invoice history");}
            var second=bus.SaveFlight(edit with{FlightId=0,TotalSeats=120});flights.Add(second);
            reject(()=>bus.SaveFare(Fare(fareId) with{FlightId=second},Fare(fareId)),"SQL used fare cannot move to different flight");
            var snapshot=Read(second);int won=0,lost=0;
            Task.WaitAll(new[]{130,140}.Select(n=>Task.Run(()=>{try{bus.SaveFlight(edit with{FlightId=second,TotalSeats=n},snapshot);Interlocked.Increment(ref won);}catch(ManagementException){Interlocked.Increment(ref lost);}})).ToArray());
            check(won==1&&lost==1,"SQL concurrent edits have one winner and one stale rejection");
            bus.DeleteFlight(Read(second));flights.Remove(second);check(!bus.Flights().Cast<FlightManagementDTO>().Any(f=>f.Flight.FlightId==second),"SQL unreferenced flight deletes");
            var disposable=bus.SaveAirport(new(0,tag+" delete","Test"));bus.DeleteAirport(new(disposable,tag+" delete","Test"));check(!bus.Airports().Cast<AirportDTO>().Any(x=>x.AirportId==disposable),"SQL unreferenced airport deletes");
        }
        finally
        {
            // Only remove IDs created by this run, in the dedicated test database.
            foreach(var id in flights)
            {
                using var clean=new SqlCommand("""
                    SET XACT_ABORT ON; BEGIN TRAN;
                    DELETE x FROM dbo.INVOICE_DETAIL x JOIN dbo.INVOICE i ON i.invoice_id=x.invoice_id JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE b.flight_id=@id;
                    DELETE d FROM dbo.BOOKING_DETAIL d JOIN dbo.BOOKING b ON b.booking_id=d.booking_id WHERE b.flight_id=@id;
                    DELETE i FROM dbo.INVOICE i JOIN dbo.BOOKING b ON b.booking_id=i.booking_id WHERE b.flight_id=@id;
                    DELETE dbo.BOOKING WHERE flight_id=@id;DELETE dbo.TICKET WHERE flight_id=@id;DELETE dbo.FARE_CLASS WHERE flight_id=@id;DELETE dbo.FLIGHT WHERE flight_id=@id;COMMIT;
                    """,connection);clean.Parameters.AddWithValue("@id",id);clean.ExecuteNonQuery();
            }
            if(customer>0){using var c=new SqlCommand("DELETE dbo.CUSTOMER WHERE customer_id=@id",connection);c.Parameters.AddWithValue("@id",customer);c.ExecuteNonQuery();}
            foreach(var id in airports){using var c=new SqlCommand("DELETE dbo.AIRPORT WHERE airport_id=@id",connection);c.Parameters.AddWithValue("@id",id);c.ExecuteNonQuery();}
        }
    }
}
