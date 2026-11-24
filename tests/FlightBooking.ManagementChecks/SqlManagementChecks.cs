using System.Data;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

internal static class SqlManagementChecks
{
    public static void Run(Action<bool,string> check,Action<Action,string> reject)
    {
        var cs=Environment.GetEnvironmentVariable("FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION")??throw new Exception("Set a dedicated test connection.");
        FlightBooking.Testing.SqlTestPreflight.Verify(cs);
        using var connection=new MyConnection(cs).Open();
        var bus=new QuanLyBUS(cs);var dao=new QuanLyDAO(cs);var tag="MGMT-"+Guid.NewGuid().ToString("N")[..10];
        var airports=new List<int>();var flights=new List<int>();int customer=0;
        QuanLyChuyenBayDTO Read(int id)=>bus.DocDSChuyenBay().Cast<QuanLyChuyenBayDTO>().Single(x=>x.ChuyenBay.MaChuyenBay==id);
        HangVeDTO Fare(int id)=>bus.DocDSHangVe().Cast<HangVeDTO>().Single(x=>x.MaHangVe==id);
        try
        {
            var a=bus.LuuSanBay(new(0,tag+" A","Test A"));airports.Add(a);
            var b=bus.LuuSanBay(new(0,tag+" B","Test B"));airports.Add(b);
            var original=bus.DocDSSanBay().Cast<SanBayDTO>().Single(x=>x.MaSanBay==a);
            bus.LuuSanBay(original with{ThanhPho="Updated city"},original);check(bus.DocDSSanBay().Cast<SanBayDTO>().Single(x=>x.MaSanBay==a).ThanhPho=="Updated city","SQL airport add/read/update");
            reject(()=>bus.LuuSanBay(original with{ThanhPho="stale"},original),"SQL airport stale edit rejected");
            reject(()=>bus.LuuSanBay(new(0,tag+" B","Test B")),"SQL duplicate airport pair rejected");
            var edit=new ThongTinSuaChuyenBay(0,a,b,120,new(2026,12,31),new(23,0),new(2027,1,1),new(1,0),"SCHEDULED");
            var id=bus.LuuChuyenBay(edit);flights.Add(id);edit=edit with{MaChuyenBay=id};var initial=Read(id);
            reject(()=>bus.LuuChuyenBay(edit with{MaChuyenBay=0,MaSanBayDen=int.MaxValue}),"SQL nonexistent airport rejected");
            reject(()=>bus.LuuHangVe(new(0,int.MaxValue,"Test",100,7)),"SQL nonexistent flight rejected");
            check(initial.SoVeDaBan==0&&initial.TongDoanhThu==0&&initial.ChuyenBay.SoGheCon==120,"SQL new flight counters");
            bus.LuuChuyenBay(edit with{TongSoGhe=130},initial);var current=Read(id);
            check(current.ChuyenBay.SoGheCon==130,"SQL seat change consistent");
            reject(()=>bus.LuuChuyenBay(edit,initial),"SQL stale flight snapshot rejected");
            reject(()=>bus.XoaSanBay(original with{ThanhPho="Updated city"}),"SQL airport referenced by flight cannot delete");
            var fareId=bus.LuuHangVe(new(0,id,"Test",250,7));var oldFare=Fare(fareId);
            check(bus.DocDSHangVe(id).Cast<HangVeDTO>().All(f=>f.MaChuyenBay==id),"SQL child fares belong to selected flight");
            reject(()=>bus.LuuHangVe(new(0,id,"Test",300,10)),"SQL duplicate fare rejected");
            bus.LuuHangVe(oldFare with{GiaVe=275},oldFare);check(Fare(fareId).GiaVe==275,"SQL fare add/read/update");
            reject(()=>bus.LuuHangVe(oldFare with{GiaVe=280},oldFare),"SQL stale fare rejected");
            bus.XoaHangVe(Fare(fareId));check(!bus.DocDSHangVe().Cast<HangVeDTO>().Any(f=>f.MaHangVe==fareId),"SQL unreferenced fare deletes");
            fareId=bus.LuuHangVe(new(0,id,"Test",250,7));
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
                UPDATE dbo.FLIGHT SET tickets_sold=1,seats_available=total_seats-1,total_revenue=0 WHERE flight_id=@flight;
                COMMIT; SELECT @customer;
                """,connection))
            {fixture.Parameters.AddWithValue("@flight",id);fixture.Parameters.AddWithValue("@fare",fareId);customer=Convert.ToInt32(fixture.ExecuteScalar());}
            current=Read(id);
            check(current.TongDoanhThu==0,"SQL unpaid management fixture does not record collected revenue");
            reject(()=>bus.LuuChuyenBay(edit with{TongSoGhe=100},current),"SQL shrink cannot remove issued 20F");
            reject(()=>bus.LuuChuyenBay(edit with{TrangThai="CANCELLED",TongSoGhe=130},current),"SQL cancellation of booked flight blocked");
            reject(()=>bus.LuuChuyenBay(edit with{GioKhoiHanh=new(22,0),TongSoGhe=130},current),"SQL reschedule booked flight blocked");
            reject(()=>bus.XoaChuyenBay(current),"SQL booked flight delete blocked");
            reject(()=>bus.XoaHangVe(Fare(fareId)),"SQL referenced fare delete blocked");
            bus.LuuChuyenBay(edit with{TongSoGhe=140},current);
            check(Read(id).SoVeDaBan==1&&Read(id).ChuyenBay.SoGheCon==139,"SQL resize preserves booked counters");
            bus.LuuHangVe(Fare(fareId) with{GiaVe=350},Fare(fareId));
            using(var history=new SqlCommand("SELECT COUNT(*) FROM dbo.BOOKING_DETAIL d JOIN dbo.INVOICE i ON i.booking_id=d.booking_id JOIN dbo.INVOICE_DETAIL x ON x.invoice_id=i.invoice_id WHERE d.booking_id IN(SELECT booking_id FROM dbo.BOOKING WHERE flight_id=@id) AND d.price=250 AND i.total_amount=250 AND x.unit_price=250",connection))
            {history.Parameters.AddWithValue("@id",id);check((int)history.ExecuteScalar()! == 1,"SQL fare price does not rewrite booking or invoice history");}
            var second=bus.LuuChuyenBay(edit with{MaChuyenBay=0,TongSoGhe=120});flights.Add(second);
            reject(()=>bus.LuuHangVe(Fare(fareId) with{MaChuyenBay=second},Fare(fareId)),"SQL used fare cannot move to different flight");
            var snapshot=Read(second);int won=0,lost=0;
            using(var start=new Barrier(2))
                Task.WaitAll(new[]{130,140}.Select(n=>Task.Run(()=>{if(!start.SignalAndWait(TimeSpan.FromSeconds(15)))throw new TimeoutException("Concurrent editors did not both start.");try{bus.LuuChuyenBay(edit with{MaChuyenBay=second,TongSoGhe=n},snapshot);Interlocked.Increment(ref won);}catch(LoiQuanLy error)when(error.Message=="Chuyến bay/số ghế đã thay đổi. Hãy tải lại và chọn lại dòng."){Interlocked.Increment(ref lost);}})).ToArray());
            check(won==1&&lost==1,"SQL concurrent edits have one winner and one stale rejection");
            bus.XoaChuyenBay(Read(second));flights.Remove(second);check(!bus.DocDSChuyenBay().Cast<QuanLyChuyenBayDTO>().Any(f=>f.ChuyenBay.MaChuyenBay==second),"SQL unreferenced flight deletes");
            var disposable=bus.LuuSanBay(new(0,tag+" delete","Test"));bus.XoaSanBay(new(disposable,tag+" delete","Test"));check(!bus.DocDSSanBay().Cast<SanBayDTO>().Any(x=>x.MaSanBay==disposable),"SQL unreferenced airport deletes");
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
