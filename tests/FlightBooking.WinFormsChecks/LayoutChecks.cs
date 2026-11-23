using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.WinForms;

internal static class LayoutChecks
{
    public static async Task Run(Form host, ManagementBUS bus, Action<bool, string> check)
    {
        var evidence = Path.Combine("tmp", "teacher-reference-20261123", "evidence");
        Directory.CreateDirectory(evidence);
        Console.WriteLine($"DISPLAY: {Screen.FromControl(host).Bounds}; actual DeviceDpi={host.DeviceDpi}. Windows display settings are unchanged.");
        foreach (var size in new[] { new Size(1366, 768), new Size(1920, 1080) })
        foreach (var kind in Enum.GetValues<CatalogKind>())
        {
            host.Location = Point.Empty;
            host.Size = size;
            if (host.Size != size)
            {
                Console.WriteLine($"SKIP: {kind} requested {size.Width}x{size.Height}; Windows limited the actual window to {host.Width}x{host.Height}.");
                continue;
            }
            using var page = new ManagementPage(bus, kind);
            host.Controls.Add(page); await page.EnsureLoaded(); page.CreateControl(); host.PerformLayout();
            foreach (var expanded in new[] { false, true })
            {
                var advanced = Field<Control>(page, "advanced"); advanced.Visible = expanded;
                page.PerformLayout(); host.PerformLayout();
                await Task.Yield(); host.Refresh();
                var save = Field<Button>(page, "save"); var cancel = Field<Button>(page, "cancel");
                var fields = Field<Control>(page, "formFields");
                var scroll = (ScrollableControl)fields.Parent!;
                var suffix = $"{kind}, {size.Width}x{size.Height}, DPI {host.DeviceDpi}, advanced={expanded}";
                Console.WriteLine($"BOUNDS: {suffix}; page={page.RectangleToScreen(page.ClientRectangle)}, save={save.RectangleToScreen(save.ClientRectangle)}, cancel={cancel.RectangleToScreen(cancel.ClientRectangle)}");
                check(!Within(save, scroll) && !Within(cancel, scroll), "save/cancel outside input scroll: " + suffix);
                var visible = page.RectangleToScreen(page.ClientRectangle);
                check(visible.Contains(save.RectangleToScreen(save.ClientRectangle)) && visible.Contains(cancel.RectangleToScreen(cancel.ClientRectangle)) &&
                      FitsAncestors(save, page) && FitsAncestors(cancel, page),
                    "save/cancel within page bounds: " + suffix);
                check(save.Width >= save.GetPreferredSize(Size.Empty).Width && cancel.Width >= cancel.GetPreferredSize(Size.Empty).Width,
                    "save/cancel text fits button width: " + suffix);
                using var bitmap = new Bitmap(host.Width, host.Height);
                host.DrawToBitmap(bitmap, new Rectangle(Point.Empty, host.Size));
                bitmap.Save(Path.Combine(evidence, $"{kind}-{size.Width}x{size.Height}-dpi{host.DeviceDpi}-advanced{expanded}.png"));
            }
            host.Controls.Remove(page);
        }
    }
    private static T Field<T>(object value, string name) => (T)value.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(value)!;
    private static bool Within(Control child, Control parent)
    {
        for (var current = child.Parent; current is not null; current = current.Parent)
            if (current == parent) return true;
        return false;
    }
    private static bool FitsAncestors(Control child, Control page)
    {
        var bounds = child.RectangleToScreen(child.ClientRectangle);
        for (var parent = child.Parent; parent is not null; parent = parent.Parent)
        {
            if (!parent.RectangleToScreen(parent.ClientRectangle).Contains(bounds)) return false;
            if (parent == page) return true;
        }
        return false;
    }
}
