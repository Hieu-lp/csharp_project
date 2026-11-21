namespace FlightBooking.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Text="FlightBooking · Quản lý sân bay và chuyến bay";
        Size=new(1440,900);MinimumSize=new(1150,740);StartPosition=FormStartPosition.CenterScreen;
        Font=new("Segoe UI",10);BackColor=Color.White;
        var connection=Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        var path=Path.Combine(AppContext.BaseDirectory,"connection.local.txt");
        if(string.IsNullOrWhiteSpace(connection)&&File.Exists(path))connection=File.ReadAllText(path).Trim();
        var bus=new FlightBooking.BUS.ManagementBUS(connection??"");
        var tabs=new TabControl{Dock=DockStyle.Fill,Padding=new(22,12)};
        var airport=new ManagementPage(bus,CatalogKind.Airport);var flight=new ManagementPage(bus,CatalogKind.Flight);var fare=new ManagementPage(bus,CatalogKind.Fare);
        foreach(var (name,page) in new[]{("Sân bay",airport),("Chuyến bay",flight),("Hạng vé",fare)}){var tab=new TabPage(name);tab.Controls.Add(page);tabs.TabPages.Add(tab);}
        flight.ManageFares+=async id=>{tabs.SelectedIndex=2;await fare.ShowFares(id);};
        Controls.Add(tabs);Shown+=async(_,_)=>await airport.Reload();
    }
}
