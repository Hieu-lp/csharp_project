using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.WinForms;

internal static class AirportDropDownChecks
{
    public static async Task Run(Form host, Action<bool, string> check)
    {
        // Dữ liệu hiển thị trong bộ nhớ; BUS cấu hình rỗng, không thể ghi/kết nối SQL.
        using var page = new TrangQuanLy(new QuanLyBUS(""), LoaiDanhMuc.ChuyenBay);
        host.Controls.Add(page);
        await page.DamBaoDaTai();
        var fields = Field<Dictionary<string, Control>>(page, "fields");
        var tips = Field<ToolTip>(page, "airportTips");
        Controls(page).OfType<Button>().Single(b => b.Text == "Thêm mới").PerformClick();
        string longLabel = "Sân bay quốc tế có tên dài để kiểm tra việc chọn đúng sân bay · Thành phố có tên đầy đủ";
        foreach (string key in new[] { "from", "to" })
        {
            var combo = (ComboBox)fields[key];
            combo.DisplayMember = "Label";
            combo.ValueMember = "Id";
            combo.DataSource = new[] { new Choice(101, longLabel), new Choice(102, "Sân bay ngắn · Thành phố") };
            host.PerformLayout(); page.PerformLayout();
            await Task.Yield();
            int closedWidth = combo.Width;
            int textWidth = TextRenderer.MeasureText(longLabel, combo.Font).Width;
            check(textWidth > closedWidth, "long airport fixture exceeds the closed combo width: " + key);
            combo.Focus();
            combo.DroppedDown = true;
            await Task.Yield();
            check(combo.DropDownWidth >= textWidth && combo.Width == closedWidth,
                "native airport dropdown fits full label without resizing editor: " + key);
            combo.DroppedDown = false;
            var title = Controls(page).OfType<Label>().Single(l => l.Text == combo.AccessibleName);
            check(tips.GetToolTip(combo) == longLabel && tips.GetToolTip(title) == longLabel,
                "full airport text is available on combo and view-mode label: " + key);
            combo.SelectedIndex = 1;
            check((int?)combo.SelectedValue == 102 && tips.GetToolTip(title) == "Sân bay ngắn · Thành phố",
                "dropdown still selects exact airport ID and updates full-text hint: " + key);
        }
        host.Controls.Remove(page);
    }

    private sealed record Choice(int Id, string Label);
    private static T Field<T>(object target, string name) =>
        (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(target)!;
    private static IEnumerable<Control> Controls(Control parent) => parent.Controls.Cast<Control>()
        .SelectMany(child => new[] { child }.Concat(Controls(child)));
}
