using System.Globalization;
using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using FlightBooking.WinForms;
using Microsoft.Data.SqlClient;

internal static class StatsLayoutChecks
{
    private static T Doc<T>(object o,string ten)=>(T)o.GetType().GetField(ten,BindingFlags.Instance|BindingFlags.NonPublic)!.GetValue(o)!;
    public static async Task Run(Form host,Action<bool,string> check)
    {
        string cs=MyConnection.DocChuoiKetNoi()??throw new Exception("Missing read connection");
        if(new SqlConnectionStringBuilder(cs).InitialCatalog!="FlightBooking_Final")throw new Exception("This mode reads only the named Final demo");
        using var sql=new MyConnection(cs).Open();
        using var marker=new SqlCommand("SELECT COUNT(*) FROM sys.extended_properties WHERE class=0 AND name='BookingFlight.FinalDemo'",sql);
        if(Convert.ToInt32(marker.ExecuteScalar())!=1)throw new Exception("Missing Final marker");
        // Approximate the content area at 1366x768 after the MainForm sidebar.
        host.ClientSize=new Size(1140,690);
        using var form=new ThongKeDoanhThuForm{TopLevel=false,FormBorderStyle=FormBorderStyle.None,Dock=DockStyle.Fill};
        host.Controls.Add(form);form.Show();
        async Task Wait()
        {
            var deadline=DateTime.UtcNow.AddSeconds(20);
            while(Doc<bool>(form,"dangTai") && DateTime.UtcNow<deadline)await Task.Delay(40);
            if(Doc<bool>(form,"dangTai") || Doc<ThongKeDoanhThuDTO?>(form,"duLieu")==null)throw new Exception("Report failed to load: "+Doc<Label>(form,"thongBao").Text);
        }
        await Wait();
        var bar=Doc<BieuDoThongKe>(form,"cot");var pie=Doc<BieuDoThongKe>(form,"tron");
        var tables=Doc<Dictionary<string,ListView>>(form,"cacBang");
        IEnumerable<Control> All(Control c){foreach(Control x in c.Controls){yield return x;foreach(var y in All(x))yield return y;}}
        bool InTab(Control c){for(Control? p=c.Parent;p!=null;p=p.Parent)if(p is TabPage)return true;return false;}
        check(!InTab(bar)&&!InTab(pie)&&bar.Visible&&pie.Visible,"both existing charts appear immediately outside TabPages");
        check(bar.RectangleToScreen(bar.ClientRectangle).Right<=pie.RectangleToScreen(pie.ClientRectangle).Left,"charts occupy separate side-by-side columns");
        var scroll=All(form).OfType<Panel>().Single(x=>x.Name=="CuonThongKe");
        var view=scroll.RectangleToScreen(scroll.ClientRectangle);
        check(view.Contains(bar.RectangleToScreen(bar.ClientRectangle))&&view.Contains(pie.RectangleToScreen(pie.ClientRectangle)),"both charts fit the initial content viewport without covering filters");
        check(All(form).OfType<Button>().Count(x=>x.Text=="Tải số liệu")==1,"one common reload action for every view");
        var tabs=All(form).OfType<TabControl>().Single();
        check(tabs.TabPages.Count==7&&!tabs.TabPages.Cast<TabPage>().Any(x=>x.Text.StartsWith("Biểu đồ")),"only the seven data-table tabs remain");
        foreach(string key in new[]{"Nhân viên","Khách hàng","Chuyến bay","Hạng vé"})
        {
            check(tables[key].Parent!.Text=="Doanh thu đã thu theo "+key.ToLowerInvariant(),"explicit paid-revenue label for "+key);
            check(tables[key].Columns[2].Text=="Năm"&&tables[key].Columns.Cast<ColumnHeader>().Skip(3).All(x=>x.Text.Contains("VND")),"matrix year and VND headings for "+key);
        }
        void Totals()
        {
            var data=Doc<ThongKeDoanhThuDTO>(form,"duLieu");
            check(Doc<List<DiemBieuDoDTO>>(bar,"duLieu").Sum(x=>x.GiaTri)==data.DoanhThuDaThu && Doc<List<DiemBieuDoDTO>>(pie,"duLieu").Sum(x=>x.GiaTri)==data.DoanhThuDaThu,"loaded column/pie values reconcile with the same KPI");
            foreach(string key in new[]{"Nhân viên","Khách hàng","Chuyến bay","Hạng vé"})
            {
                var rows=tables[key].Items.Cast<ListViewItem>().ToArray();
                decimal Read(ListViewItem row,int col)=>decimal.Parse(row.SubItems[col].Text,CultureInfo.CurrentCulture);
                check(rows.Take(rows.Length-1).Sum(x=>Read(x,7))==data.DoanhThuDaThu && Read(rows[^1],7)==data.DoanhThuDaThu,"matrix group/footer total reconciles for "+key);
            }
        }
        Totals();
        Doc<ComboBox>(form,"kyCot").SelectedIndex=1;Totals();
        Doc<CheckBox>(form,"locNam").Checked=true;Doc<NumericUpDown>(form,"nam").Value=2025;
        ((Button)All(form).Single(x=>x is Button&&x.Text=="Tải số liệu")).PerformClick();await Wait();Totals();
        check(Doc<BoLocThongKeDTO>(form,"boLocDaTai").Nam==2025 && tables["Theo tháng"].Items.Cast<ListViewItem>().Where(x=>x.Text!="TỔNG").All(x=>x.Text=="2025"),"common reload applies selected year to tables and charts");
        using(var sum=new SqlCommand("SELECT COALESCE(SUM(total_amount),0) FROM dbo.INVOICE WHERE status='PAID' AND YEAR(invoice_date)=2025",sql))
            check((decimal)sum.ExecuteScalar()! == Doc<ThongKeDoanhThuDTO>(form,"duLieu").DoanhThuDaThu,"filtered KPI matches independent invoice SQL");
        scroll.ScrollControlIntoView(tabs);Application.DoEvents();
        check(tabs.Height>=280 && tables["Theo tháng"].ClientSize.Height>=180,"scrolling preserves a usable table/list area");
        if(Environment.GetEnvironmentVariable("FLIGHTBOOKING_STATS_EVIDENCE") is string folder && folder.Length>0)
        {
            Directory.CreateDirectory(folder);scroll.AutoScrollPosition=Point.Empty;Application.DoEvents();
            using var image=new Bitmap(form.Width,form.Height);form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(folder,"statistics-layout.png"));
            scroll.ScrollControlIntoView(tabs);Application.DoEvents();form.DrawToBitmap(image,new Rectangle(0,0,form.Width,form.Height));image.Save(Path.Combine(folder,"statistics-tables.png"));
        }
        host.Controls.Remove(form);
    }
}
