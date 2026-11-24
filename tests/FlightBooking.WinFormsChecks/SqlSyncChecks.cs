using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Reflection;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using FlightBooking.BUS;
using FlightBooking.DAO;
using FlightBooking.DTO;
using FlightBooking.WinForms;
using Microsoft.Data.SqlClient;

// Real WinForms button events -> BUS/DAO -> real Razor HTTP posts -> GUI reload.
// Retains only labelled fake data. Never edits SQL schema or a main connection.
internal static class SqlSyncChecks
{
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static IEnumerable<Control> Descendants(Control parent) =>
        parent.Controls.Cast<Control>().SelectMany(c => new[] { c }.Concat(Descendants(c)));
    private static void Click(Control page, string text) =>
        Descendants(page).OfType<Button>().Single(b => b.Text == text).PerformClick();
    private static async Task Wait(Func<bool> ready)
    {
        var timeout = Stopwatch.StartNew();
        while (!ready())
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(40)) throw new TimeoutException("GUI/HTTP operation did not complete.");
            await Task.Delay(50);
        }
    }
    private static Dictionary<string, string> Inputs(string html)
    {
        var fields = new Dictionary<string, string>();
        foreach (Match tag in Regex.Matches(html, "<input\\b[^>]*>", RegexOptions.IgnoreCase))
        {
            string name = Regex.Match(tag.Value, "name=\"([^\"]+)\"").Groups[1].Value;
            string value = Regex.Match(tag.Value, "value=\"([^\"]*)\"").Groups[1].Value;
            if (name.Length > 0) fields[WebUtility.HtmlDecode(name)] = WebUtility.HtmlDecode(value);
        }
        return fields;
    }

    public static async Task Run(Form host, Action<bool, string> check)
    {
        string cs = Environment.GetEnvironmentVariable("FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION")
            ?? throw new InvalidOperationException("Missing dedicated SQL test connection.");
        if (WindowsIdentity.GetCurrent().Name != @"DESKTOP-1MMSBHT\BookingFlightTest")
            throw new InvalidOperationException("Run with the real approved Windows test account.");
        if (Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB") != cs)
            throw new InvalidOperationException("Form1 and the guarded runner must use the same test connection.");
        FlightBooking.Testing.SqlTestPreflight.Verify(cs); // Before creating GUI or starting the Web process.
        var database = new MyConnection(cs);
        string Scalar(string sql, int id = 0)
        {
            using var connection = database.Open();
            using var command = new SqlCommand(sql, connection);
            command.Parameters.AddWithValue("@id", id);
            return Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? "";
        }
        check(Scalar("SELECT ORIGINAL_LOGIN()") == @"DESKTOP-1MMSBHT\BookingFlightTest", "SYNC actual SQL Windows login");
        string evidence = Path.Combine("tmp", "integration-w7-audit", "stage2b-20261124");
        Directory.CreateDirectory(evidence);
        void Photo(Form form, string name)
        {
            using var bitmap = new Bitmap(form.Width, form.Height);
            form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
            bitmap.Save(Path.Combine(evidence, name + ".png"));
        }
        async Task Save(TrangQuanLy page, Dictionary<string, Control> values, string label)
        {
            var save = Field<Button>(page, "save");
            Console.WriteLine($"SYNC {label} before Save: enabled={save.Enabled}, visible={save.Visible}, busy={Field<bool>(page, "busy")}; " +
                string.Join("; ", values.Select(x => x.Key + "=" + (x.Value is ComboBox combo ? Convert.ToString(combo.SelectedValue) : x.Value.Text))));
            check(save.Enabled && save.Visible, $"SYNC {label} Save button is enabled and visible");
            save.PerformClick();
            await Wait(() => !Field<bool>(page, "busy"));
            string notice = Field<Label>(page, "notice").Text;
            Photo(page.FindForm()!, "sync-" + label + "-save-result");
            check(int.TryParse(values["id"].Text, out int id) && id > 0, $"SYNC {label} GUI save result: {notice}");
        }
        using var main = new Form1();
        main.Show(host);
        var tabs = main.Controls.OfType<TabControl>().Single();
        // Form1 starts loading the airport tab in Shown and deliberately blocks
        // tab changes while busy. Wait for that real GUI operation first.
        await tabs.TabPages[0].Controls.OfType<TrangQuanLy>().Single().DamBaoDaTai();
        tabs.SelectedIndex = 1;
        check(tabs.SelectedIndex == 1, "SYNC flight tab can be selected after initial loading");
        var flightPage = tabs.TabPages[1].Controls.OfType<TrangQuanLy>().Single();
        await flightPage.DamBaoDaTai();
        var bus = new QuanLyBUS(cs);
        var airports = bus.DocDSSanBay().Cast<SanBayDTO>().Where(a => a.TenSanBay.StartsWith("TEST ONLY - San bay")).OrderBy(a => a.MaSanBay).ToArray();
        check(airports.Length == 2, "SYNC uses the two approved fake airports");
        string tag = "SYNC-" + Guid.NewGuid().ToString("N")[..8];
        DateTime date = DateTime.Today.AddDays(5);
        Click(flightPage, "Thêm mới");
        var fields = Field<Dictionary<string, Control>>(flightPage, "fields");
        ((ComboBox)fields["from"]).SelectedValue = airports[0].MaSanBay;
        ((ComboBox)fields["to"]).SelectedValue = airports[1].MaSanBay;
        ((NumericUpDown)fields["total"]).Value = 6;
        ((DateTimePicker)fields["dd"]).Value = date;
        ((DateTimePicker)fields["ad"]).Value = date;
        ((DateTimePicker)fields["dt"]).Value = DateTime.Today.AddHours(8);
        ((DateTimePicker)fields["at"]).Value = DateTime.Today.AddHours(10);
        ((ComboBox)fields["status"]).SelectedValue = "SCHEDULED";
        await Save(flightPage, fields, "flight");
        int flightId = int.Parse(fields["id"].Text);
        check(Scalar("SELECT total_seats FROM dbo.FLIGHT WHERE flight_id=@id", flightId) == "6", "SYNC WinForms Save button creates flight through BUS/DAO");
        tabs.SelectedIndex = 2;
        var farePage = tabs.TabPages[2].Controls.OfType<TrangQuanLy>().Single();
        await farePage.HienThiHangVe(flightId);
        Click(farePage, "Thêm mới");
        var fareFields = Field<Dictionary<string, Control>>(farePage, "fields");
        ((ComboBox)fareFields["flight"]).SelectedValue = flightId;
        fareFields["name"].Text = tag;
        ((NumericUpDown)fareFields["price"]).Value = 1234567;
        ((NumericUpDown)fareFields["kg"]).Value = 7;
        await Save(farePage, fareFields, "fare");
        int fareId = int.Parse(fareFields["id"].Text);
        check(Scalar("SELECT price FROM dbo.FARE_CLASS WHERE fare_id=@id", fareId) == "1234567.00", "SYNC WinForms Save button creates fare");
        tabs.SelectedIndex = 1;

        async Task Reload(int available, int sold, string phase)
        {
            Click(flightPage, "Tải lại");
            await Wait(() => !Field<bool>(flightPage, "busy"));
            var list = Field<ListView>(flightPage, "list");
            var item = list.Items.Cast<ListViewItem>().Single(i => i.Text == flightId.ToString());
            item.Selected = true; item.EnsureVisible();
            var model = (QuanLyChuyenBayDTO)item.Tag!;
            check(model.ChuyenBay.SoGheCon == available && model.SoVeDaBan == sold && model.TongDoanhThu == 0,
                $"SYNC {phase}: reloaded DTO seats={available}, sold={sold}, revenue=0");
            check(fields["available"].Text == available.ToString() && fields["sold"].Text == sold.ToString() &&
                decimal.Parse(fields["revenue"].Text, CultureInfo.GetCultureInfo("vi-VN")) == 0,
                $"SYNC {phase}: visible WinForms fields agree");
            string state = Scalar("SELECT CONCAT(tickets_sold,'|',seats_available,'|',CONVERT(decimal(18,2),total_revenue),'|',(SELECT COUNT(*) FROM dbo.TICKET t WHERE t.flight_id=f.flight_id AND t.status='ISSUED')) FROM dbo.FLIGHT f WHERE flight_id=@id", flightId);
            File.AppendAllText(Path.Combine(evidence, "sync-sql-states.log"), $"{tag} flight={flightId} fare={fareId} {phase}: {state}{Environment.NewLine}");
            check(state == $"{sold}|{available}|0.00|{sold}", $"SYNC {phase}: SQL counters/active tickets agree");
            Photo(main, $"sync-{tag}-{phase}");
        }
        await Reload(6, 0, "before-booking");
        var listener = new System.Net.Sockets.TcpListener(IPAddress.Loopback, 5087);
        listener.Start(); listener.Stop(); // Fail without touching an unknown process if port is occupied.
        var start = new ProcessStartInfo("dotnet", "\"" + Path.GetFullPath("src/FlightBooking.Web/bin/Debug/net10.0/FlightBooking.Web.dll") + "\" --urls http://127.0.0.1:5087 --environment Development")
        {
            WorkingDirectory = Path.GetFullPath("src/FlightBooking.Web"), UseShellExecute = false, CreateNoWindow = true,
            RedirectStandardOutput = true, RedirectStandardError = true
        };
        start.Environment["ConnectionStrings__FlightBookingDB"] = cs + ";Application Name=FlightBooking.Stage2B.Web";
        using var web = Process.Start(start) ?? throw new Exception("Web process did not start.");
        var stdout = web.StandardOutput.ReadToEndAsync(); var stderr = web.StandardError.ReadToEndAsync();
        try
        {
            using var client = new HttpClient(new HttpClientHandler { CookieContainer = new CookieContainer() })
                { BaseAddress = new Uri("http://127.0.0.1:5087"), Timeout = TimeSpan.FromSeconds(30) };
            for (int attempt = 0; ; attempt++)
            {
                try { (await client.GetAsync("/")).EnsureSuccessStatusCode(); break; }
                catch (HttpRequestException) when (attempt < 40 && !web.HasExited) { await Task.Delay(250); }
            }
            check(Scalar("SELECT COUNT(*) FROM sys.dm_exec_sessions WHERE login_name=N'DESKTOP-1MMSBHT\\BookingFlightTest' AND program_name=N'FlightBooking.Stage2B.Web'") != "0", "SYNC website SQL session uses the actual test login");
            string search = await client.GetStringAsync($"/?Submitted=true&Search.DepartureAirportId={airports[0].MaSanBay}&Search.ArrivalAirportId={airports[1].MaSanBay}&Search.DepartureDate={date:yyyy-MM-dd}&Search.Passengers=2&Search.TripType=OneWay");
            check(WebUtility.HtmlDecode(search).Contains($"Chuyến #{flightId} · {tag}"), "SYNC website finds the GUI-created flight/fare");
            string checkoutPath = $"/Booking/Checkout?flightId={flightId}&fareId={fareId}&quantity=2";
            string checkout = await client.GetStringAsync(checkoutPath);
            var form = Inputs(checkout);
            form["Input.Customer.FullName"] = "Khach " + tag; form["Input.Customer.Email"] = tag + "@example.test";
            form["Input.Customer.Phone"] = "0900000000"; form["Input.Customer.DateOfBirth"] = "2000-01-01";
            form["Input.Customer.Gender"] = "Nam"; form["Input.Customer.IdPassportNumber"] = tag + "-B";
            for (int i = 0; i < 2; i++) { form[$"Input.Passengers[{i}].FullName"] = "Khach " + i; form[$"Input.Passengers[{i}].IdCard"] = tag + "-" + i; }
            form["action"] = "book";
            using var response = await client.PostAsync("/Booking/Checkout", new FormUrlEncodedContent(form));
            response.EnsureSuccessStatusCode();
            string details = await response.Content.ReadAsStringAsync();
            check(response.RequestMessage!.RequestUri!.AbsolutePath.StartsWith("/Booking/Details/", StringComparison.Ordinal), "SYNC real antiforgery/session checkout confirms two tickets");
            int bookingId = int.Parse(response.RequestMessage.RequestUri.AbsolutePath.Split('/').Last());
            check(Scalar("SELECT ticket_quantity FROM dbo.BOOKING WHERE booking_id=@id", bookingId) == "2" &&
                Scalar("SELECT status FROM dbo.INVOICE WHERE booking_id=@id", bookingId) == "UNPAID", "SYNC booking has two passengers and UNPAID invoice");
            await Reload(4, 2, "after-booking");
            var cancel = Inputs(details);
            using var cancelled = await client.PostAsync($"/Booking/Details/{bookingId}?handler=Cancel", new FormUrlEncodedContent(cancel));
            cancelled.EnsureSuccessStatusCode();
            check(Scalar("SELECT status FROM dbo.BOOKING WHERE booking_id=@id", bookingId) == "CANCELLED", "SYNC website cancellation commits");
            await Reload(6, 0, "after-cancel");
            check(Scalar("SELECT MIN(price) FROM dbo.BOOKING_DETAIL WHERE booking_id=@id", bookingId) == "1234567.00", "SYNC booking price history remains unchanged");
            Console.WriteLine($"SYNC retained fake flight={flightId}, fare={fareId}, booking={bookingId}, label={tag}; screen={Screen.FromControl(main).Bounds}, DPI={main.DeviceDpi}.");
        }
        finally
        {
            if (!web.HasExited) { web.Kill(entireProcessTree: true); await web.WaitForExitAsync(); }
            await File.WriteAllTextAsync(Path.Combine(evidence, "sync-web.stdout.log"), await stdout);
            await File.WriteAllTextAsync(Path.Combine(evidence, "sync-web.stderr.log"), await stderr);
        }
        main.Close();
    }
}
