using FlightBooking.BUS;
using FlightBooking.DTO;
namespace FlightBooking.WinForms;
public sealed partial class DatVeTaiQuayForm
{
    private readonly ComboBox noiDi=TaoChon(),noiDen=TaoChon(),chuyenDi=TaoChon(),chuyenVe=TaoChon();
    private readonly DateTimePicker ngayDi=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy"},ngayVe=new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy"};
    private readonly Label trangThaiChuyen=GiaoDien.TaoNhan("Chọn sân bay, ngày và số người để tìm chuyến.",9);
    private readonly CheckBox tuDong=new(){Text="Tự động chọn ghế",Checked=true,AutoSize=true};
    private readonly FlowLayoutPanel khungGhe=new(){AutoSize=true,Dock=DockStyle.Fill,FlowDirection=FlowDirection.TopDown,WrapContents=false};
    private readonly List<ComboBox> gheDi=new(),gheVe=new();
    private List<LuaChonChuyenBayDTO> luaChonDi=new(),luaChonVe=new();
    private bool dangNap;

    private void KhoiTaoTimChuyen(Action<string,Control> them)
    {
        ngayDi.Value=bus.HomNay.ToDateTime(TimeOnly.MinValue);ngayVe.Value=ngayDi.Value.AddDays(3);
        them("Sân bay đi",noiDi);them("Sân bay đến",noiDen);them("Ngày đi",ngayDi);them("Ngày về",ngayVe);
        them("Chuyến đi",chuyenDi);them("Chuyến về",chuyenVe);them("Kết quả tìm chuyến",trangThaiChuyen);
        foreach(var o in new[]{noiDi,noiDen})o.Format+=(_,e)=>{if(e.ListItem is SanBayDTO a)e.Value=$"{a.ThanhPho} · {a.TenSanBay}";};
        foreach(var o in new[]{chuyenDi,chuyenVe})o.Format+=(_,e)=>{if(e.ListItem is ChuyenBayDTO c)e.Value=$"#{c.MaChuyenBay} · {c.NgayKhoiHanh:dd/MM/yyyy} {c.GioKhoiHanh:HH:mm} · còn {c.SoGheCon} chỗ";};
        noiDi.SelectedIndexChanged+=(_,_)=>TimLuotDi();noiDen.SelectedIndexChanged+=(_,_)=>TimLuotDi();
        ngayDi.ValueChanged+=(_,_)=>TimLuotDi();ngayVe.ValueChanged+=(_,_)=>TimLuotVe();
        chuyenDi.SelectedIndexChanged+=(_,_)=>{NapHang(luotDi,luaChonDi,chuyenDi);TimLuotVe();};
        chuyenVe.SelectedIndexChanged+=(_,_)=>NapHang(luotVe,luaChonVe,chuyenVe);
        soNguoi.ValueChanged+=(_,_)=>TimLuotDi();
        khuHoi.CheckedChanged+=(_,_)=>{ngayVe.Enabled=chuyenVe.Enabled=luotVe.Enabled=khuHoi.Checked;TimLuotVe();};
    }
    private void KhoiTaoGhe(Action<string,Control> them)
    {
        them("Chế độ cấp ghế",tuDong);them("Ghế từng hành khách",khungGhe);
        tuDong.CheckedChanged+=(_,_)=>{foreach(var c in gheDi.Concat(gheVe))c.Enabled=!tuDong.Checked;};
    }
    private void TaiDanhMuc()
    {
        dangNap=true;UseWaitCursor=true;trangThaiChuyen.Text="Đang tải danh mục…";
        try
        {
            foreach(KhachHangDTO k in new KhachHangBUS().DocDSKhachHang())khach.Items.Add(k);
            foreach(NhanVienDTO n in new NhanVienBUS().DocDSNhanVien())if(n.TrangThai=="ACTIVE")nhanVien.Items.Add(n);
            foreach(var a in bus.DocDSSanBay()){noiDi.Items.Add(a);noiDen.Items.Add(a);}
            ngayVe.Enabled=chuyenVe.Enabled=luotVe.Enabled=false;
            trangThaiChuyen.Text="Chọn sân bay đi/đến và ngày để tìm chuyến; chưa chọn chuyến/hạng.";
            dangKhoiTao=false;
        }
        catch(Exception e){trangThaiChuyen.Text="Lỗi đọc danh mục: "+e.Message;xacNhan.Enabled=false;}
        finally{dangNap=false;UseWaitCursor=false;}
    }
    private void TimLuotDi()
    {
        if(dangNap || dangKhoiTao)return;
        chuyenDi.Items.Clear();luotDi.Items.Clear();chuyenVe.Items.Clear();luotVe.Items.Clear();BoGia();
        if(noiDi.SelectedItem is not SanBayDTO a || noiDen.SelectedItem is not SanBayDTO b){trangThaiChuyen.Text="Chưa đủ sân bay đi/đến.";return;}
        UseWaitCursor=true;trangThaiChuyen.Text="Đang tìm chuyến đi…";
        try
        {
            luaChonDi=bus.TimChuyenBay(new(){DepartureAirportId=a.MaSanBay,ArrivalAirportId=b.MaSanBay,DepartureDate=DateOnly.FromDateTime(ngayDi.Value),Passengers=(int)soNguoi.Value}).Cast<LuaChonChuyenBayDTO>().ToList();
            NapChuyen(chuyenDi,luaChonDi);trangThaiChuyen.Text=chuyenDi.Items.Count==0?"Không có chuyến đi đủ điều kiện/ghế trong ngày đã chọn.":$"Có {chuyenDi.Items.Count} chuyến đi. Chọn chuyến rồi chọn hạng vé.";
        }
        catch(Exception e){trangThaiChuyen.Text="Không tìm được chuyến: "+e.Message;}
        finally{UseWaitCursor=false;}
    }
    private void TimLuotVe()
    {
        chuyenVe.Items.Clear();luotVe.Items.Clear();BoGia();
        if(!khuHoi.Checked)return;
        if(chuyenDi.SelectedItem is not ChuyenBayDTO di){trangThaiChuyen.Text="Chọn chuyến đi trước khi tìm lượt về.";return;}
        if(ngayVe.Value.Date<ngayDi.Value.Date){trangThaiChuyen.Text="Ngày về không được trước ngày đi.";return;}
        try
        {
            luaChonVe=bus.TimChuyenBay(new(){DepartureAirportId=di.MaSanBayDen,ArrivalAirportId=di.MaSanBayDi,DepartureDate=DateOnly.FromDateTime(ngayVe.Value),Passengers=(int)soNguoi.Value}).Cast<LuaChonChuyenBayDTO>().Where(x=>x.ChuyenBay.ThoiDiemKhoiHanh>di.ThoiDiemDen).ToList();
            NapChuyen(chuyenVe,luaChonVe);trangThaiChuyen.Text=chuyenVe.Items.Count==0?"Không có chuyến ngược tuyến phù hợp ngày về/giờ hạ cánh/số ghế.":$"Có {chuyenVe.Items.Count} chuyến về. Chọn chuyến và hạng lượt về.";
        }
        catch(Exception e){trangThaiChuyen.Text="Lỗi tìm lượt về: "+e.Message;}
    }
    private static void NapChuyen(ComboBox o,List<LuaChonChuyenBayDTO> ds)
    {
        var ma=new HashSet<int>();foreach(var c in ds)if(ma.Add(c.ChuyenBay.MaChuyenBay))o.Items.Add(c.ChuyenBay);
        o.SelectedIndex=-1;
    }
    private void NapHang(ComboBox hang,List<LuaChonChuyenBayDTO> ds,ComboBox chuyen)
    {
        hang.Items.Clear();BoGia();
        if(chuyen.SelectedItem is ChuyenBayDTO c)foreach(var x in ds)if(x.ChuyenBay.MaChuyenBay==c.MaChuyenBay)hang.Items.Add(x);
        hang.SelectedIndex=-1;
    }
    private void XemGhe(YeuCauDatVe yc)
    {
        var di=bus.DocGheTrong(yc.FlightId,yc.Quantity);
        var ve=yc.ReturnFlightId.HasValue?bus.DocGheTrong(yc.ReturnFlightId.Value,yc.Quantity):null;
        gheDi.Clear();gheVe.Clear();while(khungGhe.Controls.Count>0)khungGhe.Controls[0].Dispose();
        for(int i=0;i<yc.Quantity;i++)
        {
            var dong=new FlowLayoutPanel{AutoSize=true};dong.Controls.Add(GiaoDien.TaoNhan("Hành khách "+(i+1)+" · Đi"));
            var g=TaoChon();g.Dock=DockStyle.None;g.Width=85;g.Items.AddRange(di.ToArray());g.SelectedIndex=i;g.Enabled=!tuDong.Checked;dong.Controls.Add(g);gheDi.Add(g);
            if(ve!=null){dong.Controls.Add(GiaoDien.TaoNhan("Về"));var v=TaoChon();v.Dock=DockStyle.None;v.Width=85;v.Items.AddRange(ve.ToArray());v.SelectedIndex=i;v.Enabled=!tuDong.Checked;dong.Controls.Add(v);gheVe.Add(v);}
            khungGhe.Controls.Add(dong);
        }
    }
}
