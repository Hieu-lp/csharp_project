using System.Collections;
using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.DTO;
using FlightBooking.WinForms;

// Chỉ tải/chọn/lọc dữ liệu. Không gọi Save/Delete hoặc tạo dữ liệu kiểm thử trên SQL.
internal static class Program
{
    private static int checks;
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static void Check(bool value, string label)
    {
        if (!value) throw new Exception("FAIL: " + label);
        checks++; Console.WriteLine("PASS: " + label);
    }

    [STAThread]
    private static void Main(string[] args)
    {
        var connection = Environment.GetEnvironmentVariable("FLIGHTBOOKING_UI_READ_CONNECTION");
        bool dropdownOnly = args.Contains("--dropdown-only");
        if (!dropdownOnly && string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set FLIGHTBOOKING_UI_READ_CONNECTION; checks only read existing data.");
        ApplicationConfiguration.Initialize();
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
        using var host = new Form { Text = "BookingFlight · Kiểm tra GUI chỉ đọc", Size = new(1320, 740) };
        host.Shown += async (_, _) =>
        {
            try
            {
                if (!dropdownOnly)
                {
                    await Run(host, new ManagementBUS(connection!));
                    if (args.Contains("--interaction")) await InteractionChecks.Run(host, connection!, Check);
                    if (args.Contains("--layout")) await LayoutChecks.Run(host, new ManagementBUS(connection!), Check);
                }
                await AirportDropDownChecks.Run(host, Check);
            }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { host.Close(); }
        };
        Application.Run(host);
        if (Environment.ExitCode == 0) Console.WriteLine($"{checks} GUI checks passed.");
    }

    private static async Task Run(Form host, ManagementBUS bus)
    {
        using var flightPage = new ManagementPage(bus, CatalogKind.Flight);
        host.Controls.Add(flightPage);
        await flightPage.EnsureLoaded();
        Check(Field<bool>(flightPage, "loaded"), "flight page loads through BUS/DAO");
        var flights = Field<ArrayList>(flightPage, "flights").Cast<FlightManagementDTO>().ToList();
        var fares = Field<ArrayList>(flightPage, "fares").Cast<FareClassDTO>().ToList();
        Check(flights.Count > 0, "database has existing flights to inspect");
        var list = Field<ListView>(flightPage, "list");
        var children = Field<ListView>(flightPage, "children");
        foreach (ListViewItem item in list.Items)
        {
            item.Selected = true;
            var flight = (FlightManagementDTO)item.Tag!;
            var expected = fares.Where(f => f.FlightId == flight.Flight.FlightId).Select(f => f.FareId.ToString()).ToArray();
            Check(children.Items.Cast<ListViewItem>().Select(i => i.Text).SequenceEqual(expected),
                "linked ListViews only show fares of flight " + flight.Flight.FlightId);
        }
        var systemFields = Field<Dictionary<string, Control>>(flightPage, "fields");
        Check(new[] { "id", "sold", "available", "revenue" }.All(k => ((TextBox)systemFields[k]).ReadOnly),
            "system fields are read-only");
        host.Controls.Remove(flightPage);

        using var farePage = new ManagementPage(bus, CatalogKind.Fare);
        host.Controls.Add(farePage);
        await farePage.EnsureLoaded();
        Check(Field<bool>(farePage, "loaded"), "fare page initially loads");
        // Mô phỏng tab đã tải trước khi chuyến/hạng xuất hiện ở tab khác.
        Field<ArrayList>(farePage, "flights").Clear();
        Field<ArrayList>(farePage, "fares").Clear();
        Field<ComboBox>(farePage, "fareScope").DataSource = null;
        var fields = Field<Dictionary<string, Control>>(farePage, "fields");
        ((ComboBox)fields["flight"]).DataSource = null;
        var selected = flights.First(f => fares.Any(fare => fare.FlightId == f.Flight.FlightId)).Flight.FlightId;
        await farePage.ShowFares(selected);
        Check(Field<ArrayList>(farePage, "flights").Count == flights.Count, "opening fares refreshes stale flight cache");
        Check((int?)Field<ComboBox>(farePage, "fareScope").SelectedValue == selected, "fare scope matches requested flight");
        Check((int?)((ComboBox)fields["flight"]).SelectedValue == selected, "new fare defaults to requested flight");
        Check(Field<ListView>(farePage, "list").Items.Count == fares.Count(f => f.FlightId == selected), "refreshed fare list matches selected flight");
        host.Controls.Remove(farePage);

        using (var firstOpen = new ManagementPage(bus, CatalogKind.Fare))
        {
            // VisibleChanged bắt đầu tải bất đồng bộ ngay khi gắn tab lần đầu.
            host.Controls.Add(firstOpen);
            var initialLoad = firstOpen.EnsureLoaded();
            await firstOpen.ShowFares(selected);
            await initialLoad;
            Check((int?)Field<ComboBox>(firstOpen, "fareScope").SelectedValue == selected,
                "first opening waits for initial load and keeps requested flight");
            host.Controls.Remove(firstOpen);
        }

        var airports = bus.Airports().Cast<object>().ToList();
        Check(airports.Count > 0, "database has existing airports to select");
        using (var picker = new SelectionDialog("Kiểm tra chọn sân bay", airports, o => ((AirportDTO)o).AirportName))
        {
            picker.Shown += (_, _) =>
            {
                var pickerList = Descendants(picker).OfType<ListView>().Single();
                pickerList.Items[0].Selected = true;
                ((Button)picker.AcceptButton!).PerformClick();
            };
            Check(picker.ShowDialog(host) == DialogResult.OK && Equals(SelectionDialog.SelectedValue, airports[0]), "selection returns exact DTO through static value");
        }
        using (var picker = new SelectionDialog("Kiểm tra hủy", airports, o => ((AirportDTO)o).AirportName))
        {
            Check(SelectionDialog.SelectedValue is null, "opening picker clears previous static selection");
            picker.Shown += (_, _) => ((Button)picker.CancelButton!).PerformClick();
            Check(picker.ShowDialog(host) == DialogResult.Cancel && SelectionDialog.SelectedValue is null, "cancel leaves no stale selection");
        }
        using (var picker = new SelectionDialog("Kiểm tra đóng", airports, o => ((AirportDTO)o).AirportName))
        {
            picker.Shown += (_, _) => picker.Close();
            picker.ShowDialog(host);
            Check(SelectionDialog.SelectedValue is null, "closing picker leaves no selection");
        }
    }

    private static IEnumerable<Control> Descendants(Control parent)
    {
        foreach (Control child in parent.Controls)
        {
            yield return child;
            foreach (var nested in Descendants(child)) yield return nested;
        }
    }
}
