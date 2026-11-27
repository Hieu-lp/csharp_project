using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using FlightBooking.WinForms;
using Microsoft.Data.SqlClient;

internal static class CounterReadChecks
{
    private static T Doc<T>(object o,string name)=>(T)o.GetType().GetField(name,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    public static void Run(Form host,Action<bool,string> check)
    {
        // This mode only loads/quotes. It never clicks book/save/cancel/payment.
        string cs=MyConnection.DocChuoiKetNoi()??throw new Exception("Missing read configuration");
        var config=new SqlConnectionStringBuilder(cs);
        if(config.InitialCatalog!="FlightBooking_Final")throw new Exception("Dedicated Final read-only mode requires exact demo target");
        using var sql=new MyConnection(cs).Open();
        using var marker=new SqlCommand("SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name='BookingFlight.FinalDemo'",sql);
        if(Convert.ToInt32(marker.ExecuteScalar())!=1)throw new Exception("Final marker missing");
        using var query=new SqlCommand("SELECT TOP(1) f.flight_id,r.flight_id,f.departure_airport_id,f.arrival_airport_id,f.departure_date,r.departure_date FROM dbo.FLIGHT f JOIN dbo.FLIGHT r ON r.departure_airport_id=f.arrival_airport_id AND r.arrival_airport_id=f.departure_airport_id AND r.departure_date>f.arrival_date WHERE f.status='SCHEDULED' AND r.status='SCHEDULED' AND f.seats_available>=3 AND r.seats_available>=3 AND f.departure_date>CONVERT(date,SYSUTCDATETIME() AT TIME ZONE 'UTC' AT TIME ZONE 'SE Asia Standard Time') ORDER BY f.departure_date,r.departure_date",sql);
        using var rd=query.ExecuteReader();if(!rd.Read())throw new Exception("No fixture pair for three passengers");
        int outbound=rd.GetInt32(0),ret=rd.GetInt32(1),a=rd.GetInt32(2),b=rd.GetInt32(3);var day=rd.GetDateTime(4);var back=rd.GetDateTime(5);rd.Close();
        using var form=new DatVeTaiQuayForm();form.Show(host);Application.DoEvents();
        var from=Doc<ComboBox>(form,"noiDi");var to=Doc<ComboBox>(form,"noiDen");
        foreach(SanBayDTO x in from.Items)if(x.MaSanBay==a){from.SelectedItem=x;break;}
        foreach(SanBayDTO x in to.Items)if(x.MaSanBay==b){to.SelectedItem=x;break;}
        Doc<DateTimePicker>(form,"ngayDi").Value=day;Doc<NumericUpDown>(form,"soNguoi").Value=3;
        var di=Doc<ComboBox>(form,"chuyenDi");var fare=Doc<ComboBox>(form,"luotDi");
        check(di.Items.Cast<ChuyenBayDTO>().Any(x=>x.MaChuyenBay==outbound),"live GUI outbound dropdown matches independent SQL");
        di.SelectedItem=di.Items.Cast<ChuyenBayDTO>().Single(x=>x.MaChuyenBay==outbound);
        check(fare.Items.Count>0 && fare.Items.Cast<LuaChonChuyenBayDTO>().All(x=>x.HangVe.MaChuyenBay==outbound),"outbound fares belong to chosen flight");fare.SelectedIndex=0;
        Doc<CheckBox>(form,"khuHoi").Checked=true;Doc<DateTimePicker>(form,"ngayVe").Value=back;
        var ve=Doc<ComboBox>(form,"chuyenVe");var fareVe=Doc<ComboBox>(form,"luotVe");
        check(ve.Items.Cast<ChuyenBayDTO>().Any(x=>x.MaChuyenBay==ret),"live GUI return dropdown matches independent reversed-route SQL");
        ve.SelectedItem=ve.Items.Cast<ChuyenBayDTO>().Single(x=>x.MaChuyenBay==ret);
        check(fareVe.Items.Count>0 && fareVe.Items.Cast<LuaChonChuyenBayDTO>().All(x=>x.HangVe.MaChuyenBay==ret),"return fares use return ID instead of outbound ID");fareVe.SelectedIndex=0;
        Doc<ComboBox>(form,"khach").SelectedIndex=0;Doc<ComboBox>(form,"nhanVien").SelectedIndex=0;
        IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
        ((Button)All(form).Single(x=>x is Button && x.Text=="Tính lại giá / áp mã")).PerformClick();
        check(Doc<List<ComboBox>>(form,"gheDi").Count==3 && Doc<List<ComboBox>>(form,"gheVe").Count==3,"GUI quote previews three seats per leg without booking");
        Doc<DateTimePicker>(form,"ngayVe").Value=day.AddDays(-1);
        check(ve.Items.Count==0 && fareVe.Items.Count==0,"invalid return date clears both dependent choices");
        Doc<CheckBox>(form,"khuHoi").Checked=false;check(!ve.Enabled && !fareVe.Enabled,"one-way disables return flight and fare");
        form.Close();
        using var tickets=new QuanLyVeForm();tickets.Show(host);Application.DoEvents();
        var list=Doc<ListView>(tickets,"danhSach");
        var ids=list.Items.Cast<ListViewItem>().Select(x=>((VeMayBayDTO)x.Tag!).MaVe).ToArray();
        using(var count=new SqlCommand("SELECT COUNT(*) FROM dbo.TICKET",sql))
            check(ids.Length==Convert.ToInt32(count.ExecuteScalar()) && ids.Distinct().Count()==ids.Length,"GUI ticket list contains one row per SQL ticket, no join duplicates");
        var target=(VeMayBayDTO)list.Items[0].Tag!;
        Doc<TextBox>(tickets,"oTimKiem").Text=target.MaVe.ToString();Doc<Button>(tickets,"nutTimKiem").PerformClick();
        var item=list.Items.Cast<ListViewItem>().Single(x=>((VeMayBayDTO)x.Tag!).MaVe==target.MaVe);item.Selected=true;Application.DoEvents();
        check(Doc<TextBox>(tickets,"oMaVe").Text==target.MaVe.ToString(),"GUI quick search selects and displays the correct ticket");
        Doc<TextBox>(tickets,"oTimKiem").Clear();
        ((CheckBox)All(tickets).Single(x=>x is CheckBox && x.Text=="Tìm kiếm nâng cao")).Checked=true;
        var advanced=Doc<BoLocNangCao>(tickets,"locNangCao");Doc<CheckBox[]>(advanced,"dung")[0].Checked=true;
        var fields=Doc<ComboBox[]>(advanced,"truong")[0];
        fields.SelectedItem=fields.Items.Cast<string>().Single(x=>x.EndsWith("|MaDatVe"));
        Doc<TextBox[]>(advanced,"giaTri")[0].Text=target.MaDatVe!.Value.ToString();
        Doc<Button>(tickets,"nutTimKiem").PerformClick();
        using(var expected=new SqlCommand("SELECT COUNT(*) FROM dbo.BOOKING_DETAIL WHERE booking_id=@id",sql))
        {
            expected.Parameters.AddWithValue("@id",target.MaDatVe.Value);
            check(list.Items.Count==Convert.ToInt32(expected.ExecuteScalar()) && list.Items.Cast<ListViewItem>().All(x=>((VeMayBayDTO)x.Tag!).MaDatVe==target.MaDatVe),"GUI advanced Booking filter matches independent SQL");
        }
        tickets.Close();

        var stats=new ThongKeBUS().ThongKeTongHop(new());
        if(Environment.GetEnvironmentVariable("FLIGHTBOOKING_CHART_EVIDENCE") is string folder && folder.Length>0)
        {
            Directory.CreateDirectory(folder);
            foreach(bool pie in new[]{false,true})
            {
                using var chart=new BieuDoThongKe{HinhTron=pie,Size=new Size(960,480),Font=new Font("Segoe UI",10)};
                chart.GanDuLieu(pie?ThongKeBUS.TronTheoHangVe(stats.TheoHangVe):ThongKeBUS.CotTheoThang(stats,new()));
                using var bitmap=new Bitmap(960,480);chart.CreateControl();chart.DrawToBitmap(bitmap,new Rectangle(0,0,960,480));
                bitmap.Save(Path.Combine(folder,pie?"pie.png":"bars.png"));
            }
        }
        check(ThongKeBUS.CotTheoThang(stats,new()).Sum(x=>x.GiaTri)==stats.DoanhThuDaThu,"live chart columns reconcile with report total");
        check(ThongKeBUS.TronTheoHangVe(stats.TheoHangVe).Sum(x=>x.GiaTri)==stats.DoanhThuDaThu,"live pie groups reconcile with report total");
        using var sum=new SqlCommand("SELECT COALESCE(SUM(total_amount),0) FROM dbo.INVOICE WHERE status='PAID'",sql);
        check((decimal)sum.ExecuteScalar()! == stats.DoanhThuDaThu,"independent invoice SQL matches loaded report/charts");
    }
}
