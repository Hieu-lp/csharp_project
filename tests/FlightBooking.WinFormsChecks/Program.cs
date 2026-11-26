using System.Collections;
using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.DTO;
using FlightBooking.WinForms;

// Default modes only read; --sql-sync is separately guarded and uses only the approved test DB.
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
        bool sqlSync = args.Contains("--sql-sync");
        bool pdfOnly = args.Contains("--pdf-only");
        if (!sqlSync && !dropdownOnly && !pdfOnly && string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set FLIGHTBOOKING_UI_READ_CONNECTION; checks only read existing data.");
        ApplicationConfiguration.Initialize();
        System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.GetCultureInfo("vi-VN");
        using var host = new Form { Text = sqlSync ? "BookingFlight · Kiểm thử GUI/Web · DATABASE TEST" : "BookingFlight · Kiểm tra GUI chỉ đọc", Size = new(1320, 740) };
        host.Shown += async (_, _) =>
        {
            try
            {
                if (sqlSync) { await SqlSyncChecks.Run(host, Check); return; }
                if (pdfOnly) { PdfExportChecks.Run(Check); return; }
                if (args.Contains("--quan-readonly")) { await QuanReadOnlyChecks.Run(host, connection!, Check); return; }
                if (!dropdownOnly)
                {
                    await Run(host, new QuanLyBUS(connection!));
                    if (args.Contains("--interaction")) await InteractionChecks.Run(host, connection!, Check);
                    if (args.Contains("--layout")) await LayoutChecks.Run(host, new QuanLyBUS(connection!), Check);
                }
                await AirportDropDownChecks.Run(host, Check);
            }
            catch (Exception error) { Console.Error.WriteLine(error); Environment.ExitCode = 1; }
            finally { host.Close(); }
        };
        Application.Run(host);
        if (Environment.ExitCode == 0) Console.WriteLine($"{checks} GUI checks passed.");
    }

    private static async Task Run(Form host, QuanLyBUS bus)
    {
        using var flightPage = new TrangQuanLy(bus, LoaiDanhMuc.ChuyenBay);
        host.Controls.Add(flightPage);
        await flightPage.DamBaoDaTai();
        Check(Field<bool>(flightPage, "loaded"), "flight page loads through BUS/DAO");
        var flights = Field<ArrayList>(flightPage, "flights").Cast<QuanLyChuyenBayDTO>().ToList();
        var fares = Field<ArrayList>(flightPage, "fares").Cast<HangVeDTO>().ToList();
        Check(flights.Count > 0, "database has existing flights to inspect");
        var list = Field<ListView>(flightPage, "list");
        var children = Field<ListView>(flightPage, "children");
        foreach (ListViewItem item in list.Items)
        {
            item.Selected = true;
            var flight = (QuanLyChuyenBayDTO)item.Tag!;
            var expected = fares.Where(f => f.MaChuyenBay == flight.ChuyenBay.MaChuyenBay).Select(f => f.MaHangVe.ToString()).ToArray();
            Check(children.Items.Cast<ListViewItem>().Select(i => i.Text).SequenceEqual(expected),
                "linked ListViews only show fares of flight " + flight.ChuyenBay.MaChuyenBay);
        }
        var systemFields = Field<Dictionary<string, Control>>(flightPage, "fields");
        Check(new[] { "id", "sold", "available", "revenue" }.All(k => ((TextBox)systemFields[k]).ReadOnly),
            "system fields are read-only");
        host.Controls.Remove(flightPage);

        using var farePage = new TrangQuanLy(bus, LoaiDanhMuc.HangVe);
        host.Controls.Add(farePage);
        await farePage.DamBaoDaTai();
        Check(Field<bool>(farePage, "loaded"), "fare page initially loads");
        // Mô phỏng tab đã tải trước khi chuyến/hạng xuất hiện ở tab khác.
        Field<ArrayList>(farePage, "flights").Clear();
        Field<ArrayList>(farePage, "fares").Clear();
        Field<ComboBox>(farePage, "fareScope").DataSource = null;
        var fields = Field<Dictionary<string, Control>>(farePage, "fields");
        ((ComboBox)fields["flight"]).DataSource = null;
        var selected = flights.First(f => fares.Any(fare => fare.MaChuyenBay == f.ChuyenBay.MaChuyenBay)).ChuyenBay.MaChuyenBay;
        await farePage.HienThiHangVe(selected);
        Check(Field<ArrayList>(farePage, "flights").Count == flights.Count, "opening fares refreshes stale flight cache");
        Check((int?)Field<ComboBox>(farePage, "fareScope").SelectedValue == selected, "fare scope matches requested flight");
        Check((int?)((ComboBox)fields["flight"]).SelectedValue == selected, "new fare defaults to requested flight");
        Check(Field<ListView>(farePage, "list").Items.Count == fares.Count(f => f.MaChuyenBay == selected), "refreshed fare list matches selected flight");
        host.Controls.Remove(farePage);

        using (var firstOpen = new TrangQuanLy(bus, LoaiDanhMuc.HangVe))
        {
            // VisibleChanged bắt đầu tải bất đồng bộ ngay khi gắn tab lần đầu.
            host.Controls.Add(firstOpen);
            var initialLoad = firstOpen.DamBaoDaTai();
            await firstOpen.HienThiHangVe(selected);
            await initialLoad;
            Check((int?)Field<ComboBox>(firstOpen, "fareScope").SelectedValue == selected,
                "first opening waits for initial load and keeps requested flight");
            host.Controls.Remove(firstOpen);
        }

        var airports = bus.DocDSSanBay().Cast<object>().ToList();
        Check(airports.Count > 0, "database has existing airports to select");
        using (var picker = new HopThoaiChon("Kiểm tra chọn sân bay", airports, o => ((SanBayDTO)o).TenSanBay))
        {
            picker.Shown += (_, _) =>
            {
                var pickerList = Descendants(picker).OfType<ListView>().Single();
                pickerList.Items[0].Selected = true;
                ((Button)picker.AcceptButton!).PerformClick();
            };
            Check(picker.ShowDialog(host) == DialogResult.OK && Equals(HopThoaiChon.GiaTriDaChon, airports[0]), "selection returns exact DTO through static value");
        }
        using (var picker = new HopThoaiChon("Kiểm tra hủy", airports, o => ((SanBayDTO)o).TenSanBay))
        {
            Check(HopThoaiChon.GiaTriDaChon is null, "opening picker clears previous static selection");
            picker.Shown += (_, _) => ((Button)picker.CancelButton!).PerformClick();
            Check(picker.ShowDialog(host) == DialogResult.Cancel && HopThoaiChon.GiaTriDaChon is null, "cancel leaves no stale selection");
        }
        using (var picker = new HopThoaiChon("Kiểm tra đóng", airports, o => ((SanBayDTO)o).TenSanBay))
        {
            picker.Shown += (_, _) => picker.Close();
            picker.ShowDialog(host);
            Check(HopThoaiChon.GiaTriDaChon is null, "closing picker leaves no selection");
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
