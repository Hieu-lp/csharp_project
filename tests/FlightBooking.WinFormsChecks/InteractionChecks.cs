using System.Diagnostics;
using System.Reflection;
using System.Runtime.InteropServices;
using FlightBooking.BUS;
using FlightBooking.WinForms;

internal static class InteractionChecks
{
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static IEnumerable<Control> Controls(Control parent) => parent.Controls.Cast<Control>()
        .SelectMany(child => new[] { child }.Concat(Controls(child)));
    private static void Click(Control parent, string text) => Controls(parent).OfType<Button>().Single(b => b.Text == text).PerformClick();
    private static async Task Idle(ManagementPage page)
    {
        var timeout = Stopwatch.StartNew();
        while (Field<bool>(page, "busy"))
        {
            if (timeout.Elapsed > TimeSpan.FromSeconds(10)) throw new TimeoutException("GUI action did not finish.");
            await Task.Delay(20);
        }
    }

    public static async Task Run(Form host, string readConnection, Action<bool, string> check)
    {
        // Validation dùng BUS không có connection: không thể ghi lên database chính.
        using (var airport = new ManagementPage(new ManagementBUS(""), CatalogKind.Airport))
        {
            host.Controls.Add(airport); await airport.EnsureLoaded();
            var fields = Field<Dictionary<string, Control>>(airport, "fields");
            var errors = Field<ErrorProvider>(airport, "errors");
            Click(airport, "Thêm mới"); Click(airport, "Lưu"); await Idle(airport);
            check(errors.GetError(fields["name"]).Length > 0, "empty airport name shows ErrorProvider on name");
            fields["name"].Text = "GUI validation only"; fields["city"].Text = new string('X', 101);
            Click(airport, "Lưu"); await Idle(airport);
            check(errors.GetError(fields["city"]).Length > 0, "invalid city shows ErrorProvider on city");
            check(fields["name"].Text == "GUI validation only" && fields["city"].Text.Length == 101,
                "failed validation preserves all entered text");
            // Cùng thông báo do ManagementBUS.SaveAirport trả khi trùng; không INSERT fixture SQL.
            typeof(ManagementPage).GetMethod("MarkErrors", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(airport, ["Đã có sân bay cùng tên và thành phố."]);
            check(errors.GetError(fields["name"]).Length > 0 && errors.GetError(fields["city"]).Length > 0,
                "duplicate airport error marks name and city");
            Click(airport, "Hủy thay đổi");
            check(fields["name"].Text.Length == 0 && errors.GetError(fields["name"]).Length == 0,
                "cancel clears validation errors and restores view");
            Click(airport, "Thêm mới");
            var cleanPrompt = Respond(host, () => airport.CanLeave(), DialogResult.No);
            check(!cleanPrompt, "opening a new editor without typing does not prompt");
            host.Controls.Remove(airport);
        }
        using (var flight = new ManagementPage(new ManagementBUS(""), CatalogKind.Flight))
        {
            host.Controls.Add(flight); await flight.EnsureLoaded();
            Click(flight, "Thêm mới");
            Click(flight, "Lưu"); await Idle(flight);
            var fields = Field<Dictionary<string, Control>>(flight, "fields");
            var errors = Field<ErrorProvider>(flight, "errors");
            check(errors.GetError(fields["from"]).Length > 0 && errors.GetError(fields["to"]).Length > 0,
                "AddFlight GUI path marks both missing airports without an SQL connection");
            Click(flight, "Hủy thay đổi"); host.Controls.Remove(flight);
        }
        using (var fare = new ManagementPage(new ManagementBUS(""), CatalogKind.Fare))
        {
            host.Controls.Add(fare); await fare.EnsureLoaded();
            Click(fare, "Thêm mới");
            Field<Dictionary<string, Control>>(fare, "fields")["name"].Text = "GUI validation only";
            Click(fare, "Lưu"); await Idle(fare);
            check(Field<ErrorProvider>(fare, "errors").GetError(Field<Dictionary<string, Control>>(fare, "fields")["flight"]).Length > 0,
                "missing fare flight shows ErrorProvider on flight");
            Click(fare, "Hủy thay đổi"); host.Controls.Remove(fare);
        }

        var previous = Environment.GetEnvironmentVariable("ConnectionStrings__FlightBookingDB");
        Form1 main;
        try
        {
            Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB", readConnection);
            main = new Form1();
        }
        finally { Environment.SetEnvironmentVariable("ConnectionStrings__FlightBookingDB", previous); }
        using (main)
        {
            main.Show();
            await Task.Yield();
            var tabs = Controls(main).OfType<TabControl>().Single();
            var page = tabs.TabPages[0].Controls.OfType<ManagementPage>().Single();
            await page.EnsureLoaded();
            await Idle(page);
            check(tabs.TabCount == 3, "main form has three management tabs");
            Click(page, "Thêm mới");
            var fields = Field<Dictionary<string, Control>>(page, "fields");
            check(!((TextBox)fields["name"]).ReadOnly, "new airport editor is editable before discard checks");
            fields["name"].Text = "UNSAVED GUI CHECK";
            check(Field<Label>(page, "detailTitle").Text.Contains("chưa lưu"), "typed airport data is marked unsaved");
            var tabPrompt = Respond(main, () => SelectTab(tabs, 1), DialogResult.No);
            Console.WriteLine($"TAB CHECK: prompt={tabPrompt}, selected={tabs.SelectedIndex}, busy={Field<bool>(page, "busy")}");
            check(tabPrompt && tabs.SelectedIndex == 0,
                "No on tab change keeps current tab");
            check(fields["name"].Text == "UNSAVED GUI CHECK", "tab discard rejection preserves typed text");
            var list = Field<ListView>(page, "list");
            check(list.Items.Count > 0, "existing airport can be selected for discard check");
            check(Respond(main, () => list.Items[0].Selected = true, DialogResult.No) && fields["name"].Text == "UNSAVED GUI CHECK",
                "No on row change preserves editor contents");
            check(Respond(main, () => Click(page, "Tải lại"), DialogResult.No) && fields["name"].Text == "UNSAVED GUI CHECK",
                "No on reload preserves editor contents");
            check(Respond(main, main.Close, DialogResult.No) && !main.IsDisposed,
                "No on closing keeps the main form open");
            check(Respond(main, () => SelectTab(tabs, 1), DialogResult.Yes) && tabs.SelectedIndex == 1,
                "Yes discards changes and switches tab");
            var flightPage = tabs.TabPages[1].Controls.OfType<ManagementPage>().Single();
            await flightPage.EnsureLoaded();
            check(Screen.FromControl(main).WorkingArea.Contains(main.Bounds), "main form fits actual screen working area");
            var pageBounds = flightPage.RectangleToScreen(flightPage.ClientRectangle);
            var saveButton = Field<Button>(flightPage, "save"); var cancelButton = Field<Button>(flightPage, "cancel");
            check(pageBounds.Contains(saveButton.RectangleToScreen(saveButton.ClientRectangle)) &&
                  pageBounds.Contains(cancelButton.RectangleToScreen(cancelButton.ClientRectangle)),
                "main form save and cancel fit visible page");
            var evidence = Path.Combine("tmp", "teacher-reference-20261123", "evidence");
            Directory.CreateDirectory(evidence);
            using (var bitmap = new Bitmap(main.Width, main.Height))
            {
                main.DrawToBitmap(bitmap, new Rectangle(Point.Empty, main.Size));
                bitmap.Save(Path.Combine(evidence, $"main-actual-display-dpi{main.DeviceDpi}.png"));
            }
            check(!Respond(main, main.Close, DialogResult.No) && main.IsDisposed,
                "closing a clean main form does not prompt");
        }

        using var picker = new SelectionDialog("GUI Escape check", ["fixture"], value => value.ToString()!);
        picker.Shown += (_, _) => picker.BeginInvoke(new Action(() =>
        {
            // Gửi phím tới chính Form đang kiểm tra, không gửi bàn phím toàn desktop.
            typeof(Form).GetMethod("ProcessDialogKey", BindingFlags.Instance | BindingFlags.NonPublic)!
                .Invoke(picker, [Keys.Escape]);
        }));
        check(picker.ShowDialog(host) == DialogResult.Cancel && SelectionDialog.SelectedValue is null,
            "Escape dialog routing cancels picker and clears static selection");
    }

    private static void SelectTab(TabControl tabs, int index)
    {
        var bounds = tabs.GetTabRect(index);
        var point = new IntPtr(((bounds.Top + bounds.Height / 2) << 16) | (bounds.Left + bounds.Width / 2));
        SendMessage(tabs.Handle, 0x0201, new IntPtr(1), point); // WM_LBUTTONDOWN
        SendMessage(tabs.Handle, 0x0202, IntPtr.Zero, point); // WM_LBUTTONUP
    }

    private static bool Respond(Form owner, Action action, DialogResult answer)
    {
        var seen = false;
        using var timer = new System.Windows.Forms.Timer { Interval = 50 };
        timer.Tick += (_, _) =>
        {
            var dialog = GetLastActivePopup(owner.Handle);
            if (dialog == owner.Handle || dialog == IntPtr.Zero) return;
            GetWindowThreadProcessId(dialog, out var process);
            if (process != Environment.ProcessId) return;
            var button = GetDlgItem(dialog, (int)answer);
            if (button == IntPtr.Zero) return;
            seen = true; SendMessage(button, 0x00F5, IntPtr.Zero, IntPtr.Zero); // BM_CLICK
        };
        timer.Start();
        try { action(); }
        finally { timer.Stop(); }
        return seen;
    }

    [DllImport("user32.dll")] private static extern IntPtr GetLastActivePopup(IntPtr window);
    [DllImport("user32.dll")] private static extern IntPtr GetDlgItem(IntPtr dialog, int id);
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(IntPtr window, out int process);
    [DllImport("user32.dll")] private static extern IntPtr SendMessage(IntPtr window, uint message, IntPtr wParam, IntPtr lParam);
}
