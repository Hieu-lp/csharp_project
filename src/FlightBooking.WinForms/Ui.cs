namespace FlightBooking.WinForms;

internal sealed record UiChoice(string Label, string Value);
internal sealed record RecordChoice(int Id, string Label);

internal static class Ui
{
    public static readonly Color Ink = Color.FromArgb(27, 48, 69);
    public static readonly Color Blue = Color.FromArgb(13, 102, 177);
    public static readonly Color Canvas = Color.FromArgb(243, 247, 251);
    public static readonly Color Muted = Color.FromArgb(78, 95, 111);
    public static readonly Color Line = Color.FromArgb(211, 222, 232);
    public static readonly Color Danger = Color.FromArgb(166, 35, 43);

    public static Label Label(string text, float size = 11, bool bold = false) => new()
    {
        Text = text, AutoSize = true, ForeColor = Ink,
        Font = new Font("Segoe UI", size, bold ? FontStyle.Bold : FontStyle.Regular),
        Margin = new Padding(0, 3, 0, 5), Anchor = AnchorStyles.Left | AnchorStyles.Right
    };
    public static Button Button(string text, Func<Task> action, bool primary = false, bool danger = false)
    {
        var button = new Button
        {
            Text = text, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
            MinimumSize = new Size(92, 36), Padding = new Padding(12, 4, 12, 4),
            FlatStyle = FlatStyle.Flat, BackColor = primary ? Blue : Color.White,
            ForeColor = primary ? Color.White : danger ? Danger : Ink,
            Margin = new Padding(0, 0, 8, 0), UseVisualStyleBackColor = false
        };
        button.FlatAppearance.BorderColor = primary ? Blue : danger ? Danger : Line;
        button.FlatAppearance.MouseOverBackColor = primary ? Color.FromArgb(9, 81, 143) : Color.FromArgb(232, 242, 251);
        button.Click += async (_, _) => await action();
        return button;
    }
    public static TableLayoutPanel Table(int columns = 1)
    {
        var table = new TableLayoutPanel { ColumnCount = columns, AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink, Dock = DockStyle.Top,
            Margin = Padding.Empty, Padding = Padding.Empty };
        if (columns == 1) table.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        return table;
    }
    public static FlowLayoutPanel Actions() => new()
    {
        Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false,
        Margin = Padding.Empty, Padding = new Padding(0, 4, 0, 4)
    };
    public static void Row(TableLayoutPanel table, Control control)
    {
        var row = table.RowCount++;
        table.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        table.Controls.Add(control, 0, row); table.SetColumnSpan(control, table.ColumnCount);
    }
    public static ComboBox Combo() => new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList, Dock = DockStyle.Fill, BindingContext = new BindingContext(),
        IntegralHeight = false, MaxDropDownItems = 12, Margin = new Padding(0, 0, 0, 8)
    };
    public static void Bind<T>(ComboBox combo, List<T> source, string display = "Label", string value = "Id")
    { combo.DataSource = null; combo.DisplayMember = display; combo.ValueMember = value; combo.DataSource = source; }
    public static void Choices(ComboBox combo, params UiChoice[] choices) => Bind(combo, choices.ToList(), "Label", "Value");
    public static ListView List() => new()
    {
        Dock = DockStyle.Fill, View = View.Details, FullRowSelect = true, MultiSelect = false,
        HideSelection = false, GridLines = false, BorderStyle = BorderStyle.FixedSingle,
        BackColor = Color.White, ForeColor = Ink, Margin = Padding.Empty
    };
    public static NumericUpDown Number(decimal max, int decimals = 0) => new()
    { Minimum = 0, Maximum = max, DecimalPlaces = decimals, ThousandsSeparator = true, Dock = DockStyle.Fill };
    public static DateTimePicker Date() => new()
    { Format = DateTimePickerFormat.Custom, CustomFormat = "dd/MM/yyyy", Dock = DockStyle.Fill };
    public static DateTimePicker Time() => new()
    { Format = DateTimePickerFormat.Custom, CustomFormat = "HH:mm:ss", ShowUpDown = true, Dock = DockStyle.Fill };
    public static string Status(string value) => value switch
    { "SCHEDULED" => "Mở bán", "COMPLETED" => "Đã hoàn thành", "CANCELLED" => "Đã hủy", _ => value };
    public static Rectangle WindowBounds(Rectangle work, Size preferred) => new(
        work.Left + Math.Max(0, (work.Width - Math.Min(preferred.Width, work.Width - 24)) / 2),
        work.Top + Math.Max(0, (work.Height - Math.Min(preferred.Height, work.Height - 24)) / 2),
        Math.Min(preferred.Width, work.Width - 24), Math.Min(preferred.Height, work.Height - 24));
}
