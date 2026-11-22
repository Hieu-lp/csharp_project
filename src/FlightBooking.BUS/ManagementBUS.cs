using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.BUS;

public sealed class ManagementBUS(string connectionString)
{
    private readonly ManagementDAO dao=new(connectionString);
    private static T Database<T>(Func<T> action)
    {
        try{return action();}
        catch(SqlException e) when(e.Number==547){throw new ManagementException("Dữ liệu đang được tham chiếu hoặc sân bay/chuyến bay không còn tồn tại. Hãy tải lại.");}
        catch(SqlException e) when(e.Number is 2601 or 2627){throw new ManagementException("Thông tin bị trùng. Hãy tải lại và kiểm tra tên hạng vé.");}
        catch(SqlException e) when(e.Number==1205){throw new ManagementException("Dữ liệu đang được người khác xử lý. Hãy tải lại rồi thử lại.");}
        catch(SqlException){throw new ManagementException("Không thể xử lý SQL Server. Kiểm tra kết nối, quyền truy cập và cấu trúc database.");}
        catch(InvalidOperationException){throw new ManagementException("Chưa cấu hình hoặc chưa kết nối được FlightBookingDB.");}
    }
    public ArrayList Airports()=>Database(()=>dao.Read(d=>new ArrayList(d.Airports())));
    public ArrayList Flights()=>Database(()=>dao.Read(d=>new ArrayList(d.Flights())));
    public ArrayList Fares(int? flightId=null)=>Database(()=>dao.Read(d=>new ArrayList(d.Fares(flightId))));
    private static void Require(bool value,string message){if(!value)throw new ManagementException(message);}
    private static string Text(string? value,int max,string name)
    {var result=(value??"").Trim();Require(result.Length>0&&result.Length<=max,$"{name} bắt buộc, tối đa {max} ký tự.");return result;}
    public static AirportDTO ValidateAirport(AirportDTO a)=>a with{AirportName=Text(a.AirportName,150,"Tên sân bay"),City=Text(a.City,100,"Thành phố")};
    public int SaveAirport(AirportDTO input,AirportDTO? original=null)
    {
        var a=ValidateAirport(input);Require(a.AirportId>=0,"Mã sân bay không hợp lệ.");
        return Database(()=>dao.Write(d=>{
            if(a.AirportId>0)Require(original is not null&&d.Airport(a.AirportId,true)==original,"Sân bay đã thay đổi. Hãy tải lại rồi chọn lại dòng.");
            Require(!d.AirportDuplicate(a),"Đã có sân bay cùng tên và thành phố.");return d.SaveAirport(a);
        }));
    }
    public void DeleteAirport(AirportDTO original)=>Database(()=>dao.Write(d=>{
        Require(d.Airport(original.AirportId,true)==original,"Sân bay đã thay đổi. Hãy tải lại.");
        Require(!d.AirportUsed(original.AirportId),"Không xóa sân bay đang được chuyến bay sử dụng.");d.DeleteAirport(original.AirportId);return 0;
    }));
    public static void ValidateFlight(FlightEdit f)
    {
        Require(f.FlightId>=0&&f.DepartureAirportId>0&&f.ArrivalAirportId>0&&f.DepartureAirportId!=f.ArrivalAirportId,"Chọn hai sân bay đi/đến khác nhau.");
        Require(f.TotalSeats>0,"Tổng số ghế phải lớn hơn 0.");
        Require(f.ArrivalDate.ToDateTime(f.ArrivalTime)>f.DepartureDate.ToDateTime(f.DepartureTime),"Ngày giờ đến phải sau ngày giờ đi.");
        Require(f.Status is "SCHEDULED" or "COMPLETED" or "CANCELLED","Trạng thái chuyến bay không hợp lệ.");
    }
    public static void ValidateFlightChange(FlightEdit f,FlightManagementDTO old,bool hasBookings,IReadOnlyList<string> activeSeats)
    {
        ValidateFlight(f);var o=old.Flight;
        Require(old.TicketsSold>=0&&o.SeatsAvailable>=0&&o.TotalSeats==old.TicketsSold+o.SeatsAvailable&&old.TicketsSold==activeSeats.Count,"Số ghế và vé hiệu lực không khớp. Cần kiểm tra dữ liệu trước khi sửa.");
        if(hasBookings)Require(f.DepartureAirportId==o.DepartureAirportId&&f.ArrivalAirportId==o.ArrivalAirportId&&f.DepartureDate==o.DepartureDate&&f.DepartureTime==o.DepartureTime&&f.ArrivalDate==o.ArrivalDate&&f.ArrivalTime==o.ArrivalTime&&f.Status==o.Status,
            "Chuyến đã có Booking/vé: không đổi tuyến, lịch hoặc trạng thái. Cần quy trình xử lý vé riêng.");
        Require(f.TotalSeats>=old.TicketsSold,"Tổng ghế không được nhỏ hơn số vé hiệu lực.");
        if(f.TotalSeats<o.TotalSeats)
            foreach(var seat in activeSeats)
            {
                var match=Regex.Match(seat,@"^([1-9][0-9]*)([A-F])$",RegexOptions.IgnoreCase);
                Require(match.Success&&int.TryParse(match.Groups[1].Value,out var row)&&row<=(int.MaxValue-6)/6,"Không xác định được vị trí ghế đã cấp; chưa thể giảm tổng ghế.");
                var index=((long)int.Parse(match.Groups[1].Value)-1)*6+char.ToUpperInvariant(match.Groups[2].Value[0])-'A'+1;
                Require(index<=f.TotalSeats,$"Không thể giảm tổng ghế vì ghế {seat} đã được cấp.");
            }
    }
    public int SaveFlight(FlightEdit f,FlightManagementDTO? original=null)
    {
        ValidateFlight(f);
        return Database(()=>dao.Write(d=>{
            var sold=0;
            if(f.FlightId>0)
            {
                var current=d.Flight(f.FlightId,true);
                Require(current is not null&&original is not null&&current==original,"Chuyến bay/số ghế đã thay đổi. Hãy tải lại và chọn lại dòng.");
                ValidateFlightChange(f,current!,d.FlightHasBookings(f.FlightId),d.ActiveSeats(f.FlightId));sold=current!.TicketsSold;
            }
            Require(d.Airport(f.DepartureAirportId) is not null&&d.Airport(f.ArrivalAirportId) is not null,"Sân bay không tồn tại.");
            return d.SaveFlight(f,sold);
        }));
    }
    public void DeleteFlight(FlightManagementDTO original)=>Database(()=>dao.Write(d=>{
        Require(d.Flight(original.Flight.FlightId,true)==original,"Chuyến bay đã thay đổi. Hãy tải lại.");
        Require(!d.FlightHasBookings(original.Flight.FlightId)&&!d.FlightHasFares(original.Flight.FlightId),"Không xóa chuyến đã có hạng vé, Booking hoặc vé. Không tự xóa dữ liệu liên quan.");
        d.DeleteFlight(original.Flight.FlightId);return 0;
    }));
    public static FareClassDTO ValidateFare(FareClassDTO f)
    {
        Require(f.FareId>=0&&f.FlightId>0,"Chọn chuyến bay hợp lệ.");
        Require(f.Price>=0&&f.Price<=9999999999999999.99m&&decimal.Round(f.Price,2)==f.Price,"Giá vé phải không âm, tối đa 2 chữ số thập phân và trong giới hạn SQL.");
        Require(f.BaseBaggageKg>=0,"Hành lý phải không âm.");return f with{ClassName=Text(f.ClassName,50,"Tên hạng vé")};
    }
    public int SaveFare(FareClassDTO input,FareClassDTO? original=null)
    {
        var f=ValidateFare(input);
        return Database(()=>dao.Write(d=>{
            foreach(var id in new[]{original?.FlightId??f.FlightId,f.FlightId}.Distinct().Order())
                Require(d.Flight(id,true) is not null,"Chuyến bay không tồn tại.");
            if(f.FareId>0)
            {
                Require(original is not null&&d.Fare(f.FareId)==original,"Hạng vé đã thay đổi. Hãy tải lại.");
                Require(original!.FlightId==f.FlightId||!d.FareUsed(f.FareId),"Không chuyển hạng đã có vé sang chuyến khác.");
            }
            Require(!d.FareDuplicate(f),"Tên hạng vé đã tồn tại trên chuyến này.");return d.SaveFare(f);
        }));
    }
    public void DeleteFare(FareClassDTO original)=>Database(()=>dao.Write(d=>{
        Require(d.Flight(original.FlightId,true) is not null&&d.Fare(original.FareId)==original,"Hạng vé đã thay đổi. Hãy tải lại.");
        Require(!d.FareUsed(original.FareId),"Không xóa hạng vé đã được vé tham chiếu.");d.DeleteFare(original.FareId);return 0;
    }));
}

public static class ManagementSearch
{
    public static readonly string[] AirportFields=["Mã","Tên","Thành phố"];
    public static readonly string[] FlightFields=["Mã","Nơi đi","Nơi đến","Ngày đi","Ngày đến","Tổng ghế","Đã bán","Còn ghế","Doanh thu","Trạng thái"];
    public static readonly string[] FareFields=["Mã","Chuyến","Tên","Giá","Hành lý"];
    private static object Value(object item,string field)=>item switch
    {
        AirportDTO a=>field switch{"Mã"=>a.AirportId,"Tên"=>a.AirportName,"Thành phố"=>a.City,_=>throw new ManagementException("Trường lọc không hợp lệ.")},
        FlightManagementDTO m=>field switch{"Mã"=>m.Flight.FlightId,"Nơi đi"=>m.Flight.DepartureCity,"Nơi đến"=>m.Flight.ArrivalCity,"Ngày đi"=>m.Flight.DepartureDate,"Ngày đến"=>m.Flight.ArrivalDate,"Tổng ghế"=>m.Flight.TotalSeats,"Đã bán"=>m.TicketsSold,"Còn ghế"=>m.Flight.SeatsAvailable,"Doanh thu"=>m.TotalRevenue,"Trạng thái"=>m.Flight.Status,_=>throw new ManagementException("Trường lọc không hợp lệ.")},
        FareClassDTO f=>field switch{"Mã"=>f.FareId,"Chuyến"=>f.FlightId,"Tên"=>f.ClassName,"Giá"=>f.Price,"Hành lý"=>f.BaseBaggageKg,_=>throw new ManagementException("Trường lọc không hợp lệ.")},
        _=>throw new ManagementException("Loại dữ liệu không hợp lệ.")
    };
    public static ArrayList Filter(ArrayList source,IReadOnlyList<ManagementFilter> filters,string join="AND",bool not=false)
    {
        if(join is not("AND" or "OR"))throw new ManagementException("Chọn AND hoặc OR.");
        var predicates=filters.Where(f=>!string.IsNullOrWhiteSpace(f.Value)).ToArray();
        foreach(var f in predicates)if(f.Operator is not("=" or "<>" or ">" or ">=" or "<" or "<=" or "chứa"))throw new ManagementException("Phép so sánh không hợp lệ.");
        var result=new ArrayList();
        foreach(var item in source)
        {
            var values=predicates.Select(f=>Match(Value(item!,f.Field),f)).ToArray();
            var yes=values.Length==0||(join=="AND"?values.All(x=>x):values.Any(x=>x));
            if(values.Length>0&&not)yes=!yes;if(yes)result.Add(item);
        }
        return result;
    }
    private static bool Match(object value,ManagementFilter f)
    {
        var text=f.Value.Trim();int comparison;
        if(value is string s)
        {if(f.Operator=="chứa")return s.Contains(text,StringComparison.OrdinalIgnoreCase);comparison=string.Compare(s,text,StringComparison.OrdinalIgnoreCase);}
        else if(value is DateOnly date)
        {
            if(!DateOnly.TryParseExact(text,["dd/MM/yyyy","yyyy-MM-dd"],CultureInfo.InvariantCulture,DateTimeStyles.None,out var target))throw new ManagementException("Ngày lọc dùng dd/MM/yyyy hoặc yyyy-MM-dd.");
            comparison=date.CompareTo(target);
        }
        else
        {
            if(!decimal.TryParse(text,NumberStyles.Number,CultureInfo.GetCultureInfo("vi-VN"),out var number))throw new ManagementException("Giá trị lọc phải là số, ví dụ 1.000.000.");
            comparison=Convert.ToDecimal(value).CompareTo(number);
        }
        return f.Operator switch{"="=>comparison==0,"<>"=>comparison!=0,">"=>comparison>0,">="=>comparison>=0,"<"=>comparison<0,"<="=>comparison<=0,_=>throw new ManagementException("Phép chứa chỉ dùng cho văn bản.")};
    }
}
