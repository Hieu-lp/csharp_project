using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public enum CatalogKind { Airport,Flight,Fare }

public sealed class ManagementPage : UserControl
{
    private readonly ManagementBUS bus;
    private readonly CatalogKind kind;
    private readonly ListView list=Ui.List();
    private readonly ListView children=Ui.List();
    private readonly Label notice=new(){AutoSize=true,MaximumSize=new(1000,0),Padding=new(8),ForeColor=Color.DarkSlateGray};
    private readonly Dictionary<string,Control> fields=new();
    private readonly ComboBox field1=Ui.Combo(),field2=Ui.Combo(),op1=Ui.Combo(),op2=Ui.Combo(),join=Ui.Combo();
    private readonly TextBox value1=new(){Width=130},value2=new(){Width=130};
    private readonly CheckBox not=new(){Text="NOT",AutoSize=true};
    private readonly ComboBox fareFlightFilter=Ui.Combo();
    private ArrayList rows=[],airports=[],flights=[],fares=[];
    private object? original;
    private bool loaded;
    public event Action<int>? ManageFares;
    private Task? loading;

    public ManagementPage(ManagementBUS bus,CatalogKind kind)
    {
        this.bus=bus;this.kind=kind;Dock=DockStyle.Fill;Padding=new(18);BackColor=Color.FromArgb(244,249,253);
        var title=new Label{Text=kind switch{CatalogKind.Airport=>"Quản lý sân bay",CatalogKind.Flight=>"Quản lý chuyến bay",_=>"Quản lý hạng vé"},Font=new("Segoe UI",19,FontStyle.Bold),AutoSize=true,Padding=new(0,0,0,12)};
        var top=new FlowLayoutPanel{Dock=DockStyle.Top,AutoSize=true,FlowDirection=FlowDirection.TopDown,WrapContents=false};
        top.Controls.Add(title);
        var toolbar=Ui.Row();
        toolbar.Controls.Add(Ui.Button("Tải lại",async()=>await Reload()));
        toolbar.Controls.Add(Ui.Button("Thêm mới / Bỏ chọn",()=>{NewRecord();return Task.CompletedTask;}));
        toolbar.Controls.Add(Ui.Button("Lưu",Save));
        toolbar.Controls.Add(Ui.Button("Xóa",Delete));
        top.Controls.Add(toolbar);
        var names=kind switch{CatalogKind.Airport=>ManagementSearch.AirportFields,CatalogKind.Flight=>ManagementSearch.FlightFields,_=>ManagementSearch.FareFields};
        field1.Items.AddRange(names);field2.Items.AddRange(names);field1.SelectedIndex=kind==CatalogKind.Fare?2:1;field2.SelectedIndex=0;
        foreach(var c in new[]{op1,op2}){c.Items.AddRange(["chứa","=","<>",">",">=","<","<="]);c.SelectedIndex=1;}
        op1.SelectedIndex=kind==CatalogKind.Flight?1:0;join.Items.AddRange(["AND","OR"]);join.SelectedIndex=0;
        var filters=Ui.Row();filters.Controls.AddRange([new Label{Text="Lọc 1",AutoSize=true,Padding=new(0,8,0,0)},field1,op1,value1,join,new Label{Text="Lọc 2",AutoSize=true,Padding=new(0,8,0,0)},field2,op2,value2,not]);
        top.Controls.Add(filters);
        var filterActions=Ui.Row();filterActions.Controls.Add(Ui.Button("Áp dụng lọc",()=>{Filter();return Task.CompletedTask;}));
        filterActions.Controls.Add(Ui.Button("Xóa lọc",()=>{value1.Clear();value2.Clear();not.Checked=false;join.SelectedIndex=0;if(kind==CatalogKind.Fare&&fareFlightFilter.Items.Count>0)fareFlightFilter.SelectedIndex=0;Filter();return Task.CompletedTask;}));
        if(kind==CatalogKind.Fare)
        {
            filterActions.Controls.Add(new Label{Text="Theo chuyến",AutoSize=true,Padding=new(12,8,0,0)});
            fareFlightFilter.Width=220;filterActions.Controls.Add(fareFlightFilter);
            fareFlightFilter.SelectedIndexChanged+=(_,_)=>{if(loaded)Filter();};
        }
        top.Controls.Add(filterActions);top.Controls.Add(new Label{Text="Bỏ trống để không lọc • Số: 1.000.000 • Ngày: dd/MM/yyyy • Tìm kiếm ArrayList",AutoSize=true,ForeColor=Color.DimGray});top.Controls.Add(notice);
        var split=new SplitContainer{Dock=DockStyle.Fill,FixedPanel=FixedPanel.Panel2,SplitterWidth=8,Panel1MinSize=320};
        split.Size=new(1100,600);split.SplitterDistance=710;
        split.Panel1.Controls.Add(list);
        var edit=new FlowLayoutPanel{Dock=DockStyle.Fill,AutoScroll=true,FlowDirection=FlowDirection.TopDown,WrapContents=false,BackColor=Color.White,Padding=new(12)};
        split.Panel2.Controls.Add(edit);
        AddField(edit,"id","Mã (tự sinh)",new TextBox{ReadOnly=true});
        switch(kind)
        {
            case CatalogKind.Airport:
                list.Columns.Add("Mã",65);list.Columns.Add("Tên sân bay",260);list.Columns.Add("Thành phố",180);
                AddField(edit,"name","Tên sân bay",new TextBox{MaxLength=150});AddField(edit,"city","Thành phố",new TextBox{MaxLength=100});break;
            case CatalogKind.Flight:
                foreach(var (text,width) in new[]{("Mã",55),("Nơi đi",130),("Nơi đến",130),("Khởi hành",150),("Hạ cánh",150),("Tổng ghế",80),("Đã bán",70),("Còn ghế",80),("Doanh thu",110),("Trạng thái",110)})list.Columns.Add(text,width);
                AddAirport(edit,"from","Sân bay đi");AddAirport(edit,"to","Sân bay đến");
                AddField(edit,"total","Tổng số ghế",Ui.Number(int.MaxValue));
                AddField(edit,"dd","Ngày đi",Ui.Date());AddField(edit,"dt","Giờ đi",Ui.Time());
                AddField(edit,"ad","Ngày đến",Ui.Date());AddField(edit,"at","Giờ đến",Ui.Time());
                var status=Ui.Combo();status.Items.AddRange(["SCHEDULED","COMPLETED","CANCELLED"]);status.SelectedIndex=0;AddField(edit,"status","Trạng thái",status);
                AddField(edit,"system","Đã bán / còn ghế / doanh thu",new TextBox{ReadOnly=true});
                var sub=new Panel{Dock=DockStyle.Bottom,Height=185,Padding=new(0,12,0,0)};
                children.Columns.Add("Mã hạng",75);children.Columns.Add("Tên hạng",180);children.Columns.Add("Giá VND",130);children.Columns.Add("Hành lý kg",100);
                var subTitle=new Label{Text="Hạng vé của chuyến được chọn",Dock=DockStyle.Top,Height=28,Font=new("Segoe UI",11,FontStyle.Bold)};
                var manage=Ui.Button("Mở quản lý hạng vé",()=>{if(original is FlightManagementDTO f)ManageFares?.Invoke(f.Flight.FlightId);else Message("Chọn chuyến trước.",true);return Task.CompletedTask;});manage.Dock=DockStyle.Bottom;
                sub.Controls.Add(children);sub.Controls.Add(subTitle);sub.Controls.Add(manage);split.Panel1.Controls.Add(sub);break;
            case CatalogKind.Fare:
                foreach(var (text,width) in new[]{("Mã",60),("Chuyến",70),("Hạng vé",180),("Giá VND",150),("Hành lý kg",100)})list.Columns.Add(text,width);
                AddField(edit,"flight","Chuyến bay",Ui.Combo());edit.Controls.Add(Ui.Button("Chọn chuyến…",()=>PickFlight()));
                AddField(edit,"name","Tên hạng vé",new TextBox{MaxLength=50});AddField(edit,"price","Giá VND / người",Ui.Number(9999999999999999.99m,2));AddField(edit,"kg","Hành lý kg",Ui.Number(int.MaxValue));break;
        }
        edit.Controls.Add(new Label{Text="Chọn dòng để sửa. Thêm mới để nhập bản ghi mới. Dữ liệu đang được đặt vé sử dụng sẽ được bảo vệ.",AutoSize=true,MaximumSize=new(320,0),ForeColor=Color.DimGray,Padding=new(0,12,0,0)});
        list.SelectedIndexChanged+=(_,_)=>SelectRecord();
        Controls.Add(split);Controls.Add(top);
        SizeChanged+=(_,_)=>{foreach(var row in top.Controls.OfType<FlowLayoutPanel>())row.MaximumSize=new(Math.Max(500,ClientSize.Width-40),0);};
        VisibleChanged+=async(_,_)=>{if(Visible&&!loaded)await Reload();};
    }
    private void AddField(FlowLayoutPanel parent,string key,string label,Control control)
    {
        parent.Controls.Add(new Label{Text=label,AutoSize=true,Margin=new(0,10,0,4)});control.Width=320;control.AccessibleName=label;parent.Controls.Add(control);fields[key]=control;
    }
    private void AddAirport(FlowLayoutPanel edit,string key,string label)
    {AddField(edit,key,label,Ui.Combo());edit.Controls.Add(Ui.Button("Chọn sân bay…",()=>PickAirport(key)));}
    private void Message(string text,bool error=false){notice.Text=text;notice.ForeColor=error?Color.Firebrick:Color.DarkSlateGray;}
    private async Task Run(Func<Task> action)
    {
        Enabled=false;UseWaitCursor=true;
        try{await action();}
        catch(Exception e) when(e is ManagementException or ArgumentException or FormatException){if(!IsDisposed)Message(e.Message,true);}
        finally{if(!IsDisposed){Enabled=true;UseWaitCursor=false;}}
    }
    public Task Reload()=>loading is {IsCompleted:false}?loading:loading=Run(async()=>{await ReadAll();if(!IsDisposed){Filter();NewRecord();Message($"Đã tải {rows.Count} bản ghi. Chọn dòng để xem chi tiết.");}});
    private async Task ReadAll()
    {
        var data=await Task.Run(()=>(bus.Airports(),bus.Flights(),bus.Fares()));
        if(IsDisposed)return;(airports,flights,fares)=data;rows=kind switch{CatalogKind.Airport=>airports,CatalogKind.Flight=>flights,_=>fares};
        if(kind==CatalogKind.Flight){Bind((ComboBox)fields["from"],airports.Cast<AirportDTO>().ToList(),"AirportName","AirportId");Bind((ComboBox)fields["to"],airports.Cast<AirportDTO>().ToList(),"AirportName","AirportId");}
        if(kind==CatalogKind.Fare)
        {
            var previous=fareFlightFilter.SelectedValue is int id?id:0;
            Bind((ComboBox)fields["flight"],FlightChoices(),"Label","Id");
            var choices=FlightChoices();choices.Insert(0,new(0,"Tất cả chuyến"));loaded=false;Bind(fareFlightFilter,choices,"Label","Id");fareFlightFilter.SelectedValue=previous;
            if(fareFlightFilter.SelectedIndex<0)fareFlightFilter.SelectedIndex=0;
        }
        loaded=true;
    }
    private sealed record FlightChoice(int Id,string Label);
    private List<FlightChoice> FlightChoices()=>flights.Cast<FlightManagementDTO>().Select(m=>new FlightChoice(m.Flight.FlightId,$"#{m.Flight.FlightId} · {m.Flight.DepartureCity} → {m.Flight.ArrivalCity} · {m.Flight.DepartureDate:dd/MM/yyyy}")).ToList();
    private static void Bind<T>(ComboBox combo,List<T> source,string display,string value){combo.DataSource=null;combo.DisplayMember=display;combo.ValueMember=value;combo.DataSource=source;}
    private void Filter()
    {
        try
        {
            var source=rows;
            if(kind==CatalogKind.Fare&&fareFlightFilter.SelectedValue is int flight&&flight>0)source=new ArrayList(fares.Cast<FareClassDTO>().Where(f=>f.FlightId==flight).ToList());
            var filtered=ManagementSearch.Filter(source,[new(field1.Text,op1.Text,value1.Text),new(field2.Text,op2.Text,value2.Text)],join.Text,not.Checked);
            list.BeginUpdate();list.Items.Clear();foreach(var item in filtered){var row=new ListViewItem(Cells(item!)){Tag=item};list.Items.Add(row);}list.EndUpdate();
            Message($"{filtered.Count} / {source.Count} bản ghi.");
        }
        catch(ManagementException e){Message(e.Message,true);}
    }
    private static string[] Cells(object item)=>item switch
    {
        AirportDTO a=>[a.AirportId.ToString(),a.AirportName,a.City],
        FlightManagementDTO m=>[m.Flight.FlightId.ToString(),m.Flight.DepartureCity,m.Flight.ArrivalCity,m.Flight.DepartsAt.ToString("dd/MM/yyyy HH:mm"),m.Flight.ArrivesAt.ToString("dd/MM/yyyy HH:mm"),m.Flight.TotalSeats.ToString(),m.TicketsSold.ToString(),m.Flight.SeatsAvailable.ToString(),m.TotalRevenue.ToString("N0"),m.Flight.Status],
        FareClassDTO f=>[f.FareId.ToString(),f.FlightId.ToString(),f.ClassName,f.Price.ToString("N2"),f.BaseBaggageKg.ToString()],_=>[]
    };
    private void SelectRecord()
    {
        if(list.SelectedItems.Count==0)return;original=list.SelectedItems[0].Tag;
        switch(original)
        {
            case AirportDTO a:fields["id"].Text=a.AirportId.ToString();fields["name"].Text=a.AirportName;fields["city"].Text=a.City;break;
            case FlightManagementDTO m:
                var f=m.Flight;fields["id"].Text=f.FlightId.ToString();((ComboBox)fields["from"]).SelectedValue=f.DepartureAirportId;((ComboBox)fields["to"]).SelectedValue=f.ArrivalAirportId;
                ((NumericUpDown)fields["total"]).Value=f.TotalSeats;((DateTimePicker)fields["dd"]).Value=f.DepartureDate.ToDateTime(TimeOnly.MinValue);((DateTimePicker)fields["dt"]).Value=DateTime.Today.Add(f.DepartureTime.ToTimeSpan());((DateTimePicker)fields["ad"]).Value=f.ArrivalDate.ToDateTime(TimeOnly.MinValue);((DateTimePicker)fields["at"]).Value=DateTime.Today.Add(f.ArrivalTime.ToTimeSpan());fields["status"].Text=f.Status;fields["system"].Text=$"{m.TicketsSold} / {f.SeatsAvailable} / {m.TotalRevenue:N0} VND";
                children.Items.Clear();foreach(var fare in fares.Cast<FareClassDTO>().Where(x=>x.FlightId==f.FlightId))children.Items.Add(new ListViewItem([fare.FareId.ToString(),fare.ClassName,fare.Price.ToString("N2"),fare.BaseBaggageKg.ToString()]));break;
            case FareClassDTO fare:
                fields["id"].Text=fare.FareId.ToString();((ComboBox)fields["flight"]).SelectedValue=fare.FlightId;fields["name"].Text=fare.ClassName;((NumericUpDown)fields["price"]).Value=fare.Price;((NumericUpDown)fields["kg"]).Value=fare.BaseBaggageKg;break;
        }
        Message("Đang sửa bản ghi đã chọn. Lưu sẽ kiểm tra lại dữ liệu trên server.");
    }
    private void NewRecord()
    {
        original=null;foreach(ListViewItem item in list.SelectedItems)item.Selected=false;
        foreach(var control in fields.Values)
            switch(control){case TextBox text:text.Clear();break;case NumericUpDown n:n.Value=0;break;case DateTimePicker date:date.Value=date.ShowUpDown?DateTime.Today.AddHours(8):DateTime.Today;break;case ComboBox c:if(c.Items.Count>0)c.SelectedIndex=0;break;}
        if(kind==CatalogKind.Flight){((NumericUpDown)fields["total"]).Value=120;((DateTimePicker)fields["at"]).Value=DateTime.Today.AddHours(10);fields["system"].Text="0 / 120 / 0 VND (chuyến mới)";children.Items.Clear();}
        if(kind==CatalogKind.Fare&&fareFlightFilter.SelectedValue is int id&&id>0)((ComboBox)fields["flight"]).SelectedValue=id;
        Message("Thêm bản ghi mới. ID sẽ được SQL Server tạo khi lưu.");
    }
    private int Number(string key)=>(int)((NumericUpDown)fields[key]).Value;
    private int SelectedId(string key)=>((ComboBox)fields[key]).SelectedValue is int id?id:0;
    private Task Save()=>Run(async()=>{
        switch(kind)
        {
            case CatalogKind.Airport:
                var a=new AirportDTO((original as AirportDTO)?.AirportId??0,fields["name"].Text,fields["city"].Text);await Task.Run(()=>bus.SaveAirport(a,original as AirportDTO));break;
            case CatalogKind.Flight:
                var f=new FlightEdit((original as FlightManagementDTO)?.Flight.FlightId??0,SelectedId("from"),SelectedId("to"),Number("total"),DateOnly.FromDateTime(((DateTimePicker)fields["dd"]).Value),TimeOnly.FromDateTime(((DateTimePicker)fields["dt"]).Value),DateOnly.FromDateTime(((DateTimePicker)fields["ad"]).Value),TimeOnly.FromDateTime(((DateTimePicker)fields["at"]).Value),fields["status"].Text);await Task.Run(()=>bus.SaveFlight(f,original as FlightManagementDTO));break;
            case CatalogKind.Fare:
                var fare=new FareClassDTO((original as FareClassDTO)?.FareId??0,SelectedId("flight"),fields["name"].Text,((NumericUpDown)fields["price"]).Value,Number("kg"));await Task.Run(()=>bus.SaveFare(fare,original as FareClassDTO));break;
        }
        await ReadAll();if(!IsDisposed){Filter();NewRecord();Message("Đã lưu. Dữ liệu danh sách đã được tải lại.");}
    });
    private Task Delete()
    {
        if(original is null){Message("Chọn dòng cần xóa trước.",true);return Task.CompletedTask;}
        if(MessageBox.Show(this,"Xóa bản ghi đang chọn? Dữ liệu đã được sử dụng sẽ không được xóa.","Xác nhận xóa",MessageBoxButtons.YesNo,MessageBoxIcon.Warning)!=DialogResult.Yes)return Task.CompletedTask;
        return Run(async()=>{var selected=original;await Task.Run(()=>{switch(selected){case AirportDTO a:bus.DeleteAirport(a);break;case FlightManagementDTO f:bus.DeleteFlight(f);break;case FareClassDTO fare:bus.DeleteFare(fare);break;}});await ReadAll();if(!IsDisposed){Filter();NewRecord();Message("Đã xóa bản ghi.");}});
    }
    private Task PickAirport(string key)
    {
        using var picker=new SelectionDialog("Chọn sân bay",airports.Cast<object>().ToList(),o=>{var a=(AirportDTO)o;return $"#{a.AirportId} · {a.AirportName} · {a.City}";});
        if(picker.ShowDialog(this)==DialogResult.OK&&SelectionDialog.SelectedValue is AirportDTO a)((ComboBox)fields[key]).SelectedValue=a.AirportId;return Task.CompletedTask;
    }
    private Task PickFlight()
    {
        using var picker=new SelectionDialog("Chọn chuyến bay",flights.Cast<object>().ToList(),o=>{var f=((FlightManagementDTO)o).Flight;return $"#{f.FlightId} · {f.DepartureCity} → {f.ArrivalCity} · {f.DepartsAt:dd/MM/yyyy HH:mm}";});
        if(picker.ShowDialog(this)==DialogResult.OK&&SelectionDialog.SelectedValue is FlightManagementDTO m)((ComboBox)fields["flight"]).SelectedValue=m.Flight.FlightId;return Task.CompletedTask;
    }
    public async Task ShowFares(int flightId){await Reload();if(!IsDisposed&&kind==CatalogKind.Fare){fareFlightFilter.SelectedValue=flightId;((ComboBox)fields["flight"]).SelectedValue=flightId;}}
}

internal static class Ui
{
    public static ComboBox Combo()=>new(){DropDownStyle=ComboBoxStyle.DropDownList,Width=105};
    public static FlowLayoutPanel Row()=>new(){AutoSize=true,WrapContents=true,MaximumSize=new(1300,0),Padding=new(0,2,0,4)};
    public static Button Button(string label,Func<Task> action)
    {var b=new Button{Text=label,AutoSize=true,MinimumSize=new(95,36),FlatStyle=FlatStyle.Flat,BackColor=Color.FromArgb(232,245,255),ForeColor=Color.FromArgb(12,73,118),Margin=new(0,3,8,3)};b.Click+=async(_,_)=>await action();return b;}
    public static ListView List()=>new(){Dock=DockStyle.Fill,View=View.Details,FullRowSelect=true,MultiSelect=false,HideSelection=false,GridLines=true,BorderStyle=BorderStyle.FixedSingle};
    public static NumericUpDown Number(decimal max,int decimals=0)=>new(){Minimum=0,Maximum=max,DecimalPlaces=decimals,ThousandsSeparator=true};
    public static DateTimePicker Date()=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="dd/MM/yyyy",MinDate=new DateTime(1753,1,1),MaxDate=new DateTime(9998,12,31)};
    public static DateTimePicker Time()=>new(){Format=DateTimePickerFormat.Custom,CustomFormat="HH:mm:ss",ShowUpDown=true};
}
