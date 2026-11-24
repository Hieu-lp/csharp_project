namespace FlightBooking.WinForms;

public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        Text = "FlightBooking · Quản lý chuyến bay";
        Font = new Font("Segoe UI", 11); BackColor = Ui.Canvas;
        StartPosition = FormStartPosition.Manual;
        var work = Screen.FromPoint(Cursor.Position).WorkingArea;
        Bounds = Ui.WindowBounds(work, new Size(1340, 840));
        MinimumSize = new Size(Math.Min(1040, work.Width - 24), Math.Min(640, work.Height - 24));
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        if (string.IsNullOrWhiteSpace(connection))
            connection = Environment.GetEnvironmentVariable("FLIGHTBOOKING_DB_CONNECTION");
        var path = Path.Combine(AppContext.BaseDirectory, "connection.local.txt");
        if (string.IsNullOrWhiteSpace(connection) && File.Exists(path)) connection = File.ReadAllText(path).Trim();
        var bus = new FlightBooking.BUS.ManagementBUS(connection ?? "");
        var tabs = new TabControl { Dock = DockStyle.Fill, Padding = new Point(22, 10), Font = Font };
        var airport = new ManagementPage(bus, CatalogKind.Airport);
        var flight = new ManagementPage(bus, CatalogKind.Flight);
        var fare = new ManagementPage(bus, CatalogKind.Fare);
        ManagementPage[] pages = [airport, flight, fare];
        foreach (var (name, page) in new[] { ("Sân bay", airport), ("Chuyến bay", flight), ("Hạng vé", fare) })
        {
            var tab = new TabPage(name) { BackColor = Ui.Canvas };
            tab.Controls.Add(page); tabs.TabPages.Add(tab);
        }
        tabs.Deselecting += (_, e) =>
        {
            if (e.TabPageIndex >= 0 && !pages[e.TabPageIndex].CanLeave())
                e.Cancel = true;
        };
        flight.ManageFares += async id => { tabs.SelectedIndex = 2; if (tabs.SelectedIndex == 2) await fare.ShowFares(id); };
        FormClosing += (_, e) => { foreach (var page in pages) if (!page.CanLeave()) { e.Cancel = true; break; } };
        Controls.Add(tabs);
        Shown += async (_, _) =>
        {
            Bounds = Ui.WindowBounds(Screen.FromControl(this).WorkingArea, new Size(1340, 840));
            await airport.EnsureLoaded();
        };
    }
}
