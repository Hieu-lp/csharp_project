using System.Collections;
using System.Text.Json;
using FlightBooking.BUS;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

public enum CatalogKind { Airport, Flight, Fare }

public sealed class ManagementPage : UserControl
{
    private readonly ManagementBUS bus;
    private readonly CatalogKind kind;
    private readonly ListView list = Ui.List(), children = Ui.List();
    private readonly Label notice = Ui.Label("Chưa tải dữ liệu"), count = Ui.Label(""), detailTitle = Ui.Label("Chưa chọn bản ghi", 14, true);
    private readonly Label context = Ui.Label(""), childTitle = Ui.Label("Hạng vé · Chọn một chuyến", 11, true);
    private readonly Label empty = new() { Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleCenter, ForeColor = Ui.Muted };
    private readonly TextBox quick = new() { Dock = DockStyle.Fill, PlaceholderText = "Tên, thành phố hoặc mã…" };
    private readonly TextBox value1 = new() { Dock = DockStyle.Fill }, value2 = new() { Dock = DockStyle.Fill };
    private readonly ComboBox field1 = Ui.Combo(), field2 = Ui.Combo(), op1 = Ui.Combo(), op2 = Ui.Combo(), join = Ui.Combo(), fareScope = Ui.Combo();
    private readonly CheckBox not = new() { Text = "Đảo kết quả lọc nâng cao (NOT), trong phạm vi chuyến đang xem", AutoSize = true };
    private readonly Dictionary<string, Control> fields = new();
    private readonly List<Button> pickers = [];
    private readonly ErrorProvider errors = new() { BlinkStyle = ErrorBlinkStyle.NeverBlink };
    private readonly EditorSession editor = new();
    private readonly Button save, cancel, edit, delete, manage;
    private readonly TableLayoutPanel advanced = Ui.Table(), formFields = Ui.Table();
    private readonly SplitContainer split = new() { Dock = DockStyle.Fill, FixedPanel = FixedPanel.Panel2, SplitterWidth = 8 };
    private ArrayList rows = [], airports = [], flights = [], fares = [];
    private object? original, lastViewed;
    private bool binding, busy, loaded;
    private int scopeFlight, pendingSavedId;
    private Task? loading;
    public event Func<int, Task>? ManageFares;

    public ManagementPage(ManagementBUS bus, CatalogKind kind)
    {
        this.bus = bus; this.kind = kind;
        Font = new Font("Segoe UI", 11); AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi;
        Dock = DockStyle.Fill; Padding = new Padding(16); BackColor = Ui.Canvas;
        errors.ContainerControl = this;
        save = Ui.Button("Lưu", Save, primary: true);
        cancel = Ui.Button("Hủy thay đổi", () => { CancelEdit(); return Task.CompletedTask; });
        edit = Ui.Button("Chỉnh sửa", () => { BeginEdit(); return Task.CompletedTask; });
        delete = Ui.Button("Xóa bản ghi…", Delete, danger: true);
        manage = Ui.Button("Quản lý hạng vé", async () =>
        {
            if (original is FlightManagementDTO m && CanLeave() && ManageFares is { } handler)
                await handler(m.Flight.FlightId);
        });
        var root = Ui.Table(); root.Dock = DockStyle.Fill; root.AutoSize = false;
        root.RowCount = 3; root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.Controls.Add(BuildHeader(), 0, 0);
        notice.Padding = new Padding(8); notice.Margin = new Padding(0, 4, 0, 10);
        root.Controls.Add(notice, 0, 1); root.Controls.Add(split, 0, 2);
        split.Size = new Size(1200, 600); split.SplitterDistance = 760;
        // Parent tabs briefly have a tiny size during construction; apply layout after mounting.
        split.Panel1.Controls.Add(BuildLists()); split.Panel2.Controls.Add(BuildEditor());
        Controls.Add(root);
        split.SizeChanged += (_, _) =>
        {
            // Tab có thể co rất nhỏ khi khởi tạo; khôi phục vùng chi tiết khi đã có đủ chỗ.
            if (split.Width > 760 && split.Panel2.Width < 360)
                split.SplitterDistance = Math.Max(360, split.Width - 430);
        };
        list.SelectedIndexChanged += (_, _) => SelectionChanged();
        foreach (var c in fields.Values)
        {
            switch (c)
            {
                case TextBox t: t.TextChanged += (_, _) => Changed(); break;
                case ComboBox combo: combo.SelectedIndexChanged += (_, _) => Changed(); break;
                case NumericUpDown n: n.ValueChanged += (_, _) => Changed(); break;
                case DateTimePicker d: d.ValueChanged += (_, _) => Changed(); break;
            }
        }
        foreach (var c in new[] { quick, value1, value2 }) c.KeyDown += SearchKey;
        foreach (var c in new[] { field1, field2, op1, op2, join, fareScope }) c.KeyDown += SearchKey;
        fareScope.SelectedIndexChanged += (_, _) => ScopeChanged();
        VisibleChanged += async (_, _) => { if (Visible && !loaded) await EnsureLoaded(); };
        SizeChanged += (_, _) =>
        {
            notice.MaximumSize = new Size(Math.Max(200, Width - 48), 0);
            context.MaximumSize = new Size(Math.Max(200, split.Panel2.ClientSize.Width - 40), 0);
        };
        Load += (_, _) =>
        {
            if (split.Width > 760) split.SplitterDistance = Math.Max(360, split.Width - 430);
        };
        ResetView(null);
    }

    private Control BuildHeader()
    {
        var header = Ui.Table();
        var titleRow = Ui.Table(2); titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        titleRow.RowCount = 1; titleRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        titleRow.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        titleRow.Controls.Add(Ui.Label(kind switch { CatalogKind.Airport => "Sân bay", CatalogKind.Flight => "Chuyến bay", _ => "Hạng vé" }, 20, true), 0, 0);
        var actions = Ui.Actions();
        actions.Controls.Add(Ui.Button("Thêm mới", () => { if (CanLeave()) NewRecord(); return Task.CompletedTask; }, primary: true));
        actions.Controls.Add(Ui.Button("Tải lại", Reload)); titleRow.Controls.Add(actions, 1, 0); Ui.Row(header, titleRow);
        var search = Ui.Table(4);
        search.RowCount = 1; search.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        search.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); search.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        search.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); search.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        var label = Ui.Label("Tìm nhanh"); label.Margin = new Padding(0, 8, 12, 0);
        search.Controls.Add(label, 0, 0); quick.AccessibleName = "Từ khóa tìm nhanh"; quick.Margin = new Padding(0, 4, 12, 4); search.Controls.Add(quick, 1, 0);
        search.Controls.Add(Ui.Button("Tìm kiếm", () => { ApplyFilter(); return Task.CompletedTask; }), 2, 0);
        Button? toggle = null;
        toggle = Ui.Button("Lọc nâng cao", () => { advanced.Visible = !advanced.Visible; toggle!.Text = advanced.Visible ? "Thu gọn bộ lọc" : "Lọc nâng cao"; return Task.CompletedTask; });
        search.Controls.Add(toggle, 3, 0); Ui.Row(header, search);
        if (kind == CatalogKind.Fare)
        {
            var scope = Ui.Table(2); scope.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize)); scope.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            scope.RowCount = 1; scope.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            scope.Controls.Add(Ui.Label("Phạm vi chuyến"), 0, 0); fareScope.AccessibleName = "Phạm vi chuyến"; scope.Controls.Add(fareScope, 1, 0); Ui.Row(header, scope);
        }
        var names = kind switch { CatalogKind.Airport => ManagementSearch.AirportFields, CatalogKind.Flight => ManagementSearch.FlightFields, _ => ManagementSearch.FareFields };
        foreach (var c in new[] { field1, field2 }) Ui.Choices(c, names.Select(n => new UiChoice(n == "Mã" ? "Mã bản ghi" : n == "Tên" ? "Tên " + (kind == CatalogKind.Airport ? "sân bay" : "hạng vé") : n, n)).ToArray());
        field1.SelectedValue = kind == CatalogKind.Flight ? "Nơi đi" : "Tên";
        foreach (var c in new[] { op1, op2 }) Ui.Choices(c,
            new("Chứa", "chứa"), new("Bằng (=)", "="), new("Khác (<>)", "<>"), new("Lớn hơn (>)", ">"),
            new("Từ (>=)", ">="), new("Nhỏ hơn (<)", "<"), new("Đến (<=)", "<="));
        op2.SelectedValue = "=";
        Ui.Choices(join, new("Thỏa tất cả điều kiện (AND)", "AND"), new("Thỏa một trong các điều kiện (OR)", "OR"));
        var predicates = Ui.Table(3);
        predicates.RowCount = 3;
        for (var i = 0; i < 3; i++) predicates.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        foreach (var width in new[] { 36f, 28f, 36f }) predicates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, width));
        predicates.Controls.Add(Ui.Label("Trường dữ liệu"), 0, 0); predicates.Controls.Add(Ui.Label("So sánh"), 1, 0); predicates.Controls.Add(Ui.Label("Giá trị"), 2, 0);
        foreach (var c in new Control[] { field1, op1, value1, field2, op2, value2 }) c.Margin = new Padding(0, 0, 8, 8);
        field1.AccessibleName = "Trường điều kiện 1"; field2.AccessibleName = "Trường điều kiện 2";
        op1.AccessibleName = "So sánh điều kiện 1"; op2.AccessibleName = "So sánh điều kiện 2";
        value1.AccessibleName = "Giá trị điều kiện 1"; value2.AccessibleName = "Giá trị điều kiện 2";
        predicates.Controls.Add(field1, 0, 1); predicates.Controls.Add(op1, 1, 1); predicates.Controls.Add(value1, 2, 1);
        predicates.Controls.Add(field2, 0, 2); predicates.Controls.Add(op2, 1, 2); predicates.Controls.Add(value2, 2, 2);
        advanced.Padding = new Padding(12); advanced.BackColor = Color.White;
        Ui.Row(advanced, predicates); join.AccessibleName = "Kết hợp điều kiện"; Ui.Row(advanced, join); Ui.Row(advanced, not);
        Ui.Row(advanced, Ui.Label("Ngày: dd/MM/yyyy · Giá VND: 1.000.000 · Bỏ trống để không thêm điều kiện", 10));
        var filterActions = Ui.Actions(); filterActions.Controls.Add(Ui.Button("Áp dụng", () => { ApplyFilter(); return Task.CompletedTask; }));
        filterActions.Controls.Add(Ui.Button("Xóa lọc", () =>
        {
            if (!CanLeave()) return Task.CompletedTask;
            quick.Clear(); value1.Clear(); value2.Clear(); not.Checked = false; join.SelectedValue = "AND";
            Render(); Message("Đã xóa bộ lọc. Phạm vi chuyến được giữ nguyên."); return Task.CompletedTask;
        }));
        Ui.Row(advanced, filterActions); advanced.Visible = false; Ui.Row(header, advanced);
        return header;
    }

    private Control BuildLists()
    {
        var main = new Panel { Dock = DockStyle.Fill, BackColor = Color.White, Padding = new Padding(12) };
        var listPanel = new Panel { Dock = DockStyle.Fill };
        var top = Ui.Table(2); top.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); top.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        top.RowCount = 1; top.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        top.Controls.Add(Ui.Label("Danh sách", 12, true), 0, 0); top.Controls.Add(count, 1, 0);
        listPanel.Controls.Add(list); listPanel.Controls.Add(empty); listPanel.Controls.Add(top);
        switch (kind)
        {
            case CatalogKind.Airport: Columns(list, ("Mã", 65), ("Tên sân bay", 250), ("Thành phố", 180)); break;
            case CatalogKind.Flight: Columns(list, ("Mã", 60), ("Chặng bay", 235), ("Khởi hành", 155), ("Hạ cánh", 155), ("Còn / tổng", 95), ("Trạng thái", 130)); break;
            case CatalogKind.Fare: Columns(list, ("Mã", 60), ("Chuyến", 70), ("Tên hạng", 170), ("Giá VND/người", 150), ("Hành lý kg", 95)); break;
        }
        if (kind != CatalogKind.Flight) { main.Controls.Add(listPanel); return main; }
        var linked = new SplitContainer { Dock = DockStyle.Fill, Orientation = Orientation.Horizontal, SplitterWidth = 8 };
        linked.Size = new Size(700, 520); linked.SplitterDistance = 300;
        linked.Panel1.Controls.Add(listPanel);
        var sub = Ui.Table(); sub.Dock = DockStyle.Fill; sub.AutoSize = false;
        sub.RowCount = 3; sub.RowStyles.Add(new RowStyle(SizeType.AutoSize)); sub.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); sub.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        sub.Controls.Add(childTitle, 0, 0); sub.Controls.Add(children, 0, 1); sub.Controls.Add(manage, 0, 2);
        Columns(children, ("Mã", 60), ("Tên hạng", 160), ("Giá VND/người", 140), ("Hành lý kg", 100));
        linked.Panel2.Controls.Add(sub); main.Controls.Add(linked); return main;
    }
    private static void Columns(ListView view, params (string Text, int Width)[] columns)
    { foreach (var (text, width) in columns) view.Columns.Add(text, width); }

    private Control BuildEditor()
    {
        var shell = Ui.Table(); shell.Dock = DockStyle.Fill; shell.AutoSize = false; shell.BackColor = Color.White; shell.Padding = new Padding(16);
        shell.RowCount = 3; shell.RowStyles.Add(new RowStyle(SizeType.AutoSize)); shell.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); shell.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var head = Ui.Table(); Ui.Row(head, detailTitle); context.ForeColor = Ui.Muted; Ui.Row(head, context);
        var actions = Ui.Actions(); actions.Controls.Add(edit); actions.Controls.Add(delete); Ui.Row(head, actions); shell.Controls.Add(head, 0, 0);
        var scroll = new Panel { Dock = DockStyle.Fill, AutoScroll = true, Padding = new Padding(0, 4, 20, 0) };
        scroll.Controls.Add(formFields); shell.Controls.Add(scroll, 0, 1);
        AddField(formFields, "id", "Mã tự sinh · có thể sao chép", new TextBox { ReadOnly = true });
        switch (kind)
        {
            case CatalogKind.Airport:
                AddField(formFields, "name", "Tên sân bay", new TextBox { MaxLength = 150 });
                AddField(formFields, "city", "Thành phố", new TextBox { MaxLength = 100 }); break;
            case CatalogKind.Flight:
                Section("Chặng bay"); AddAirport("from", "Sân bay đi"); AddAirport("to", "Sân bay đến");
                Section("Lịch bay"); var dates = Ui.Table(2); dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 55)); dates.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 45));
                dates.RowCount = 2; dates.RowStyles.Add(new RowStyle(SizeType.AutoSize)); dates.RowStyles.Add(new RowStyle(SizeType.AutoSize));
                dates.Controls.Add(Field("dd", "Ngày đi", Ui.Date()), 0, 0); dates.Controls.Add(Field("dt", "Giờ đi", Ui.Time()), 1, 0);
                dates.Controls.Add(Field("ad", "Ngày đến", Ui.Date()), 0, 1); dates.Controls.Add(Field("at", "Giờ đến", Ui.Time()), 1, 1); Ui.Row(formFields, dates);
                Section("Số ghế và trạng thái"); AddField(formFields, "total", "Tổng số ghế", Ui.Number(int.MaxValue));
                var status = Ui.Combo(); Ui.Choices(status, new("Mở bán", "SCHEDULED"), new("Đã hoàn thành", "COMPLETED"), new("Đã hủy", "CANCELLED"));
                AddField(formFields, "status", "Trạng thái", status);
                Section("Thông tin hệ thống · chỉ đọc");
                AddField(formFields, "sold", "Vé đã bán", new TextBox { ReadOnly = true });
                AddField(formFields, "available", "Ghế còn", new TextBox { ReadOnly = true });
                AddField(formFields, "revenue", "Doanh thu đã thu (VND)", new TextBox { ReadOnly = true }); break;
            case CatalogKind.Fare:
                AddField(formFields, "flight", "Chuyến bay", Ui.Combo());
                var pick = Ui.Button("Chọn chuyến…", PickFlight); pickers.Add(pick); Ui.Row(formFields, pick);
                AddField(formFields, "name", "Tên hạng vé", new TextBox { MaxLength = 50 });
                AddField(formFields, "price", "Giá vé (VND/người)", Ui.Number(9999999999999999.99m, 2));
                AddField(formFields, "kg", "Hành lý (kg/người)", Ui.Number(int.MaxValue)); break;
        }
        var footer = Ui.Actions(); footer.Padding = new Padding(0, 12, 0, 0); footer.Controls.Add(save); footer.Controls.Add(cancel); shell.Controls.Add(footer, 0, 2);
        return shell;
    }
    private void Section(string title) { var label = Ui.Label(title, 11, true); label.Margin = new Padding(0, 16, 0, 6); Ui.Row(formFields, label); }
    private Control Field(string key, string label, Control control)
    {
        var panel = Ui.Table(); panel.Margin = new Padding(0, 0, 8, 10);
        control.Dock = DockStyle.Fill; control.AccessibleName = label;
        fields[key] = control; Ui.Row(panel, Ui.Label(label)); Ui.Row(panel, control); return panel;
    }
    private void AddField(TableLayoutPanel parent, string key, string label, Control control) => Ui.Row(parent, Field(key, label, control));
    private void AddAirport(string key, string label)
    {
        var holder = Ui.Table(2); holder.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); holder.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        holder.RowCount = 1; holder.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var combo = Ui.Combo(); fields[key] = combo; combo.AccessibleName = label; holder.Controls.Add(combo, 0, 0);
        var pick = Ui.Button("Chọn…", () => PickAirport(key)); pick.MinimumSize = new Size(60, 32); pickers.Add(pick); holder.Controls.Add(pick, 1, 0);
        var wrapper = Ui.Table(); Ui.Row(wrapper, Ui.Label(label)); Ui.Row(wrapper, holder); Ui.Row(formFields, wrapper);
    }
    private void SearchKey(object? sender, KeyEventArgs e)
    { if (e.KeyCode == Keys.Enter) { e.SuppressKeyPress = true; ApplyFilter(); } }
    private void Message(string text, bool error = false)
    { notice.Text = (error ? "Lỗi: " : "") + text; notice.ForeColor = error ? Ui.Danger : Ui.Ink; notice.BackColor = error ? Color.MistyRose : Color.FromArgb(230, 240, 249); }
    private static int Id(object? row) => row switch { AirportDTO a => a.AirportId, FlightManagementDTO f => f.Flight.FlightId, FareClassDTO f => f.FareId, _ => 0 };
    private object? Find(int id) => rows.Cast<object>().FirstOrDefault(row => Id(row) == id);
    private string Snapshot() => JsonSerializer.Serialize(fields.Where(p => !IsSystem(p.Key)).ToDictionary(p => p.Key, p => p.Value switch
    { ComboBox c => Convert.ToString(c.SelectedValue) ?? "", DateTimePicker d => d.Value.ToString("O"), NumericUpDown n => n.Value.ToString(System.Globalization.CultureInfo.InvariantCulture), _ => p.Value.Text }));
    private static bool IsSystem(string key) => key is "id" or "sold" or "available" or "revenue";
    private void Changed() { if (!binding) { UpdateButtons(); if (kind == CatalogKind.Fare) UpdateContext(); } }
    private void UpdateButtons()
    {
        var view = editor.Mode == EditorMode.View;
        foreach (var (key, control) in fields)
        {
            if (control is TextBox text) { text.ReadOnly = view || IsSystem(key); text.BackColor = text.ReadOnly ? Ui.Canvas : Color.White; }
            else control.Enabled = !view && !IsSystem(key);
        }
        foreach (var picker in pickers) picker.Enabled = !view;
        save.Enabled = !busy && editor.CanSave(Snapshot()); cancel.Enabled = !busy && !view;
        edit.Enabled = !busy && view && original is not null; delete.Enabled = !busy && view && original is not null;
        manage.Enabled = !busy && original is FlightManagementDTO;
        detailTitle.Text = editor.Mode switch
        { EditorMode.New => "Thêm mới", EditorMode.Edit => $"Chỉnh sửa #{Id(original)}", _ => original is null ? "Chọn bản ghi để xem" : $"Chi tiết #{Id(original)}" };
        if (editor.IsDirty(Snapshot())) detailTitle.Text += " · chưa lưu";
    }
    public bool CanLeave()
    {
        if (busy) { Message("Đang xử lý, vui lòng đợi hoàn tất."); return false; }
        if (editor.IsDirty(Snapshot()) && MessageBox.Show(this, "Bỏ các thay đổi chưa lưu? Nội dung đang nhập sẽ không được lưu.", "Thay đổi chưa lưu", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return false;
        if (editor.Mode != EditorMode.View) CancelEdit();
        return true;
    }
    private async Task Run(Func<Task> action, string progress)
    {
        if (busy) return; busy = true; Enabled = false; UseWaitCursor = true; Message(progress);
        try { await action(); }
        catch (Exception e) when (e is ManagementException or ArgumentException or FormatException)
        { if (!IsDisposed) { Message(e.Message, true); MarkErrors(e.Message); } }
        finally { if (!IsDisposed) { busy = false; Enabled = true; UseWaitCursor = false; UpdateButtons(); } }
    }
    private void MarkErrors(string message)
    {
        errors.Clear();
        string[] keys = message switch
        {
            var s when s.Contains("Tên sân bay") => ["name"], var s when s.Contains("Thành phố") => ["city"],
            var s when kind == CatalogKind.Airport && s.Contains("cùng tên và thành phố", StringComparison.OrdinalIgnoreCase) => ["name", "city"],
            var s when s.Contains("tên hạng", StringComparison.OrdinalIgnoreCase) || s.Contains("hạng vé đã tồn tại") => ["name"],
            var s when s.Contains("Giá vé") => ["price"], var s when s.Contains("Hành lý") => ["kg"],
            var s when s.Contains("ghế", StringComparison.OrdinalIgnoreCase) => ["total"], var s when s.Contains("Ngày giờ") => ["ad", "at"],
            var s when s.Contains("sân bay", StringComparison.OrdinalIgnoreCase) => ["from", "to"],
            var s when kind == CatalogKind.Fare && s.Contains("chuyến bay", StringComparison.OrdinalIgnoreCase) => ["flight"], _ => []
        };
        foreach (var key in keys) if (fields.TryGetValue(key, out var control)) errors.SetError(control, message);
    }
    public Task EnsureLoaded() => loaded ? Task.CompletedTask : LoadCore();
    public Task Reload() => !CanLeave() ? Task.CompletedTask : LoadCore();
    private Task LoadCore() => loading is { IsCompleted: false } ? loading : loading = Run(async () =>
    {
        var keep = pendingSavedId > 0 ? pendingSavedId : Id(original); await ReadAll(); if (IsDisposed) return;
        Render(); ResetView(Find(keep)); Message($"Đã tải {rows.Count} bản ghi.");
    }, "Đang tải dữ liệu…");
    private async Task ReadAll()
    {
        var data = await Task.Run(() => (bus.Airports(), bus.Flights(), bus.Fares()));
        if (IsDisposed) return;
        binding = true;
        try
        {
            (airports, flights, fares) = data;
            rows = kind switch { CatalogKind.Airport => airports, CatalogKind.Flight => flights, _ => fares };
            if (kind == CatalogKind.Flight)
            {
                var choices = airports.Cast<AirportDTO>().Select(a => new RecordChoice(a.AirportId, $"{a.AirportName} · {a.City}")).ToList();
                Ui.Bind((ComboBox)fields["from"], choices); Ui.Bind((ComboBox)fields["to"], choices.ToList());
            }
            if (kind == CatalogKind.Fare)
            {
                var choices = FlightChoices(); Ui.Bind((ComboBox)fields["flight"], choices.ToList());
                choices.Insert(0, new(0, "Tất cả chuyến")); Ui.Bind(fareScope, choices);
                fareScope.SelectedValue = scopeFlight; if (fareScope.SelectedIndex < 0) { fareScope.SelectedValue = 0; scopeFlight = 0; }
            }
            loaded = true;
        }
        finally { binding = false; }
    }
    private List<RecordChoice> FlightChoices() => flights.Cast<FlightManagementDTO>().Select(m => new RecordChoice(m.Flight.FlightId,
        $"#{m.Flight.FlightId} · {m.Flight.DepartureCity} → {m.Flight.ArrivalCity} · {m.Flight.DepartsAt:dd/MM/yyyy HH:mm}")).ToList();
    private void ScopeChanged()
    {
        if (binding || !loaded) return;
        var requested = fareScope.SelectedValue is int id ? id : 0;
        if (!CanLeave()) { binding = true; fareScope.SelectedValue = scopeFlight; binding = false; return; }
        scopeFlight = requested; ResetView(null); Render();
    }
    private static string Choice(ComboBox combo) => Convert.ToString(combo.SelectedValue) ?? "";
    private void ApplyFilter() { if (CanLeave() && Render()) ResetView(null); }
    private bool Render()
    {
        try
        {
            var source = rows;
            if (kind == CatalogKind.Fare && scopeFlight > 0) source = ManagementSearch.Filter(source, [new("Chuyến", "=", scopeFlight.ToString())]);
            var filtered = ManagementSearch.Filter(source, [new(Choice(field1), Choice(op1), value1.Text), new(Choice(field2), Choice(op2), value2.Text)], Choice(join), not.Checked);
            var text = quick.Text.Trim();
            if (text.Length > 0)
            {
                var terms = kind switch
                {
                    CatalogKind.Airport => new List<ManagementFilter> { new("Tên", "chứa", text), new("Thành phố", "chứa", text) },
                    CatalogKind.Flight => new List<ManagementFilter> { new("Nơi đi", "chứa", text), new("Nơi đến", "chứa", text) },
                    _ => new List<ManagementFilter> { new("Tên", "chứa", text) }
                };
                if (int.TryParse(text, out _)) terms.Add(new("Mã", "=", text));
                filtered = ManagementSearch.Filter(filtered, terms, "OR");
            }
            binding = true; list.BeginUpdate();
            try
            {
                list.Items.Clear(); foreach (var row in filtered)
                {
                    var item = new ListViewItem(Cells(row!)) { Tag = row };
                    if (list.Items.Count % 2 == 1) item.BackColor = Color.FromArgb(248, 251, 254);
                    list.Items.Add(item);
                }
            }
            finally { list.EndUpdate(); binding = false; }
            count.Text = $"{filtered.Count} / {source.Count} bản ghi";
            empty.Text = !loaded ? "Đang tải dữ liệu…" : source.Count == 0 ? "Chưa có dữ liệu trong phạm vi này. Chọn Thêm mới để bắt đầu." : "Không tìm thấy kết quả. Thử Xóa lọc hoặc đổi từ khóa.";
            empty.Visible = filtered.Count == 0; list.Visible = filtered.Count > 0;
            Message(filtered.Count == 0 ? empty.Text : $"Tìm thấy {filtered.Count} bản ghi."); return true;
        }
        catch (ManagementException e) { Message(e.Message, true); return false; }
    }
    private static string[] Cells(object row) => row switch
    {
        AirportDTO a => [a.AirportId.ToString(), a.AirportName, a.City],
        FlightManagementDTO m => [m.Flight.FlightId.ToString(), $"{m.Flight.DepartureCity} → {m.Flight.ArrivalCity}", m.Flight.DepartsAt.ToString("dd/MM/yyyy HH:mm"), m.Flight.ArrivesAt.ToString("dd/MM/yyyy HH:mm"), $"{m.Flight.SeatsAvailable} / {m.Flight.TotalSeats}", Ui.Status(m.Flight.Status)],
        FareClassDTO f => [f.FareId.ToString(), f.FlightId.ToString(), f.ClassName, f.Price.ToString("N2"), f.BaseBaggageKg.ToString()], _ => []
    };
    private void SelectionChanged()
    {
        if (binding || list.SelectedItems.Count == 0) return;
        var requested = list.SelectedItems[0].Tag;
        if (Id(requested) == Id(original) && editor.Mode == EditorMode.View) return;
        if (!CanLeave()) { SelectList(Id(original)); return; }
        ResetView(requested);
    }
    private void SelectList(int id)
    {
        binding = true;
        try { foreach (ListViewItem item in list.Items) item.Selected = Id(item.Tag) == id && id > 0; }
        finally { binding = false; }
    }
    private void ResetView(object? row)
    {
        binding = true; original = row; lastViewed = row; pendingSavedId = 0; errors.Clear();
        try
        {
            ClearFields(); if (row is not null) Fill(row);
            if (row is null && kind == CatalogKind.Flight) foreach (var key in new[] { "sold", "available", "revenue" }) fields[key].Text = "";
            editor.Begin(EditorMode.View, Snapshot());
        }
        finally { binding = false; }
        SelectList(Id(row)); UpdateContext(); UpdateChildren(); UpdateButtons();
    }
    private void ClearFields()
    {
        foreach (var (key, control) in fields)
            switch (control)
            {
                case TextBox t: t.Clear(); break; case NumericUpDown n: n.Value = 0; break;
                case DateTimePicker d: d.Value = key == "dt" ? DateTime.Today.AddHours(8) : key == "at" ? DateTime.Today.AddHours(10) : DateTime.Today; break;
                case ComboBox c: if (c.Items.Count > 0) c.SelectedIndex = 0; break;
            }
        if (kind == CatalogKind.Flight) { ((NumericUpDown)fields["total"]).Value = 120; fields["sold"].Text = "0"; fields["available"].Text = "120"; fields["revenue"].Text = "0"; }
        if (kind == CatalogKind.Fare && scopeFlight > 0) ((ComboBox)fields["flight"]).SelectedValue = scopeFlight;
    }
    private void Fill(object row)
    {
        fields["id"].Text = Id(row).ToString();
        switch (row)
        {
            case AirportDTO a: fields["name"].Text = a.AirportName; fields["city"].Text = a.City; break;
            case FlightManagementDTO m:
                var f = m.Flight;
                ((ComboBox)fields["from"]).SelectedValue = f.DepartureAirportId; ((ComboBox)fields["to"]).SelectedValue = f.ArrivalAirportId;
                ((NumericUpDown)fields["total"]).Value = f.TotalSeats;
                ((DateTimePicker)fields["dd"]).Value = f.DepartureDate.ToDateTime(TimeOnly.MinValue); ((DateTimePicker)fields["dt"]).Value = DateTime.Today.Add(f.DepartureTime.ToTimeSpan());
                ((DateTimePicker)fields["ad"]).Value = f.ArrivalDate.ToDateTime(TimeOnly.MinValue); ((DateTimePicker)fields["at"]).Value = DateTime.Today.Add(f.ArrivalTime.ToTimeSpan());
                ((ComboBox)fields["status"]).SelectedValue = f.Status;
                fields["sold"].Text = m.TicketsSold.ToString(); fields["available"].Text = f.SeatsAvailable.ToString(); fields["revenue"].Text = m.TotalRevenue.ToString("N2"); break;
            case FareClassDTO fare:
                ((ComboBox)fields["flight"]).SelectedValue = fare.FlightId; fields["name"].Text = fare.ClassName;
                ((NumericUpDown)fields["price"]).Value = fare.Price; ((NumericUpDown)fields["kg"]).Value = fare.BaseBaggageKg; break;
        }
    }
    private void NewRecord()
    {
        lastViewed = original; original = null; binding = true;
        try { ClearFields(); errors.Clear(); editor.Begin(EditorMode.New, Snapshot()); }
        finally { binding = false; }
        SelectList(0); UpdateChildren(); UpdateContext(); UpdateButtons(); fields[kind == CatalogKind.Flight ? "from" : "name"].Focus();
        Message("Đang thêm mới. Chỉ ghi dữ liệu khi bấm Lưu.");
    }
    private void BeginEdit()
    {
        if (original is null || busy) return;
        editor.Begin(EditorMode.Edit, Snapshot()); errors.Clear(); UpdateButtons();
        fields[kind == CatalogKind.Flight ? "from" : "name"].Focus(); Message("Đang chỉnh sửa. Lưu để áp dụng hoặc Hủy thay đổi để trở lại.");
    }
    private void CancelEdit() { ResetView(original ?? lastViewed); Message("Đã trở lại chế độ xem, không ghi dữ liệu."); }
    private void UpdateContext()
    {
        if (kind != CatalogKind.Fare) { context.Text = editor.Mode == EditorMode.View ? "Chọn dòng để xem; bấm Chỉnh sửa để thay đổi." : "Các trường hệ thống chỉ đọc."; return; }
        if (editor.Mode == EditorMode.View && original is null && scopeFlight == 0) { context.Text = "Tất cả chuyến · Chọn dòng để xem hoặc Thêm mới."; return; }
        var id = ((ComboBox)fields["flight"]).SelectedValue is int selected ? selected : scopeFlight;
        context.Text = FlightChoices().FirstOrDefault(x => x.Id == id)?.Label ?? "Chọn chuyến trước khi thêm hạng vé.";
    }
    private void UpdateChildren()
    {
        children.Items.Clear();
        if (original is not FlightManagementDTO selectedFlight)
        {
            childTitle.Text = "Hạng vé · Chưa chọn chuyến";
            return;
        }
        int flightId = selectedFlight.Flight.FlightId;
        childTitle.Text = $"Hạng vé · Chuyến #{flightId}";
        // ListView con chỉ hiển thị các hạng thuộc chuyến đang chọn ở ListView cha.
        foreach (FareClassDTO fare in fares)
        {
            if (fare.FlightId != flightId) continue;
            string[] cells = { fare.FareId.ToString(), fare.ClassName, fare.Price.ToString("N2"), fare.BaseBaggageKg.ToString() };
            children.Items.Add(new ListViewItem(cells));
        }
        if (children.Items.Count == 0) childTitle.Text += " · chưa có hạng vé";
    }
    private int Number(string key)
    {
        return (int)((NumericUpDown)fields[key]).Value;
    }
    private int SelectedId(string key)
    {
        if (((ComboBox)fields[key]).SelectedValue is int id) return id;
        return 0;
    }
    private Task Save()
    {
        if (editor.Mode == EditorMode.View || busy) return Task.CompletedTask;
        return Run(async () =>
        {
            errors.Clear();
            int previousId = Id(original);
            int saved;
            // GUI đọc control và tạo DTO; việc kiểm tra/ghi dữ liệu thuộc BUS và DAO.
            switch (kind)
            {
                case CatalogKind.Airport:
                    var airport = new AirportDTO(previousId, fields["name"].Text, fields["city"].Text);
                    if (previousId == 0)
                        saved = await Task.Run(() => bus.AddAirport(airport));
                    else
                        saved = await Task.Run(() => bus.UpdateAirport(airport, original as AirportDTO));
                    break;
                case CatalogKind.Flight:
                    DateOnly departureDate = DateOnly.FromDateTime(((DateTimePicker)fields["dd"]).Value);
                    TimeOnly departureTime = TimeOnly.FromDateTime(((DateTimePicker)fields["dt"]).Value);
                    DateOnly arrivalDate = DateOnly.FromDateTime(((DateTimePicker)fields["ad"]).Value);
                    TimeOnly arrivalTime = TimeOnly.FromDateTime(((DateTimePicker)fields["at"]).Value);
                    var flight = new FlightEdit(previousId, SelectedId("from"), SelectedId("to"), Number("total"),
                        departureDate, departureTime, arrivalDate, arrivalTime, Choice((ComboBox)fields["status"]));
                    if (previousId == 0)
                        saved = await Task.Run(() => bus.AddFlight(flight));
                    else
                        saved = await Task.Run(() => bus.UpdateFlight(flight, original as FlightManagementDTO));
                    break;
                default:
                    var fare = new FareClassDTO(previousId, SelectedId("flight"), fields["name"].Text,
                        ((NumericUpDown)fields["price"]).Value, Number("kg"));
                    saved = await Task.Run(() => bus.SaveFare(fare, original as FareClassDTO));
                    break;
            }
            var id = previousId > 0 ? previousId : saved;
            // A committed insert must never be retried if only the subsequent reload fails.
            editor.Begin(EditorMode.View, Snapshot()); pendingSavedId = id; fields["id"].Text = id.ToString(); UpdateButtons();
            try
            {
                await ReadAll(); if (IsDisposed) return;
                Render(); ResetView(Find(id));
                Message($"Đã lưu #{id}." + (list.Items.Cast<ListViewItem>().Any(i => Id(i.Tag) == id) ? " Đang hiển thị bản ghi vừa lưu." : " Bản ghi nằm ngoài bộ lọc; chi tiết vẫn được giữ."));
            }
            catch (ManagementException e) { Message($"Đã lưu #{id}, nhưng chưa tải lại được: {e.Message}", true); }
        }, "Đang lưu dữ liệu…");
    }
    private Task Delete()
    {
        if (original is null || busy || editor.Mode != EditorMode.View) return Task.CompletedTask;
        var selected = original;
        var label = selected switch { AirportDTO a => a.AirportName, FareClassDTO f => f.ClassName, FlightManagementDTO f => $"{f.Flight.DepartureCity} → {f.Flight.ArrivalCity}", _ => "" };
        if (MessageBox.Show(this, $"Xóa #{Id(selected)} — {label}?\nDữ liệu đã được tham chiếu sẽ không được xóa.", "Xác nhận xóa bản ghi", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2) != DialogResult.Yes) return Task.CompletedTask;
        return Run(async () =>
        {
            // GUI gọi đúng nghiệp vụ theo DTO đang chọn; BUS/DAO quyết định có được xóa hay không.
            switch (selected)
            {
                case AirportDTO airport:
                    await Task.Run(() => bus.DeleteAirport(airport));
                    break;
                case FlightManagementDTO flight:
                    await Task.Run(() => bus.DeleteFlight(flight));
                    break;
                case FareClassDTO fare:
                    await Task.Run(() => bus.DeleteFare(fare));
                    break;
            }
            ResetView(null);
            await ReadAll();
            if (!IsDisposed)
            {
                Render();
                Message($"Đã xóa bản ghi #{Id(selected)}.");
            }
        }, "Đang xóa bản ghi…");
    }
    private Task PickAirport(string key)
    {
        using var picker = new SelectionDialog("Chọn sân bay", airports.Cast<object>().ToList(), o => { var a = (AirportDTO)o; return $"#{a.AirportId} · {a.AirportName} · {a.City}"; });
        if (picker.ShowDialog(this) == DialogResult.OK && SelectionDialog.SelectedValue is AirportDTO a) ((ComboBox)fields[key]).SelectedValue = a.AirportId;
        return Task.CompletedTask;
    }
    private Task PickFlight()
    {
        using var picker = new SelectionDialog("Chọn chuyến bay", flights.Cast<object>().ToList(), o => { var f = ((FlightManagementDTO)o).Flight; return $"#{f.FlightId} · {f.DepartureCity} → {f.ArrivalCity} · {f.DepartsAt:dd/MM/yyyy HH:mm}"; });
        if (picker.ShowDialog(this) == DialogResult.OK && SelectionDialog.SelectedValue is FlightManagementDTO m) ((ComboBox)fields["flight"]).SelectedValue = m.Flight.FlightId;
        return Task.CompletedTask;
    }
    public async Task ShowFares(int flightId)
    {
        // Lần mở tab đầu tiên có thể đã bắt đầu tải từ VisibleChanged.
        if (loading is { IsCompleted: false }) await loading;
        if (IsDisposed || !CanLeave()) return;
        await Run(async () =>
        {
            // Tab có thể đã tải trước khi chuyến/hạng được thêm hoặc sửa ở nơi khác.
            await ReadAll(); if (IsDisposed) return;
            if (!flights.Cast<FlightManagementDTO>().Any(m => m.Flight.FlightId == flightId))
                throw new ManagementException("Chuyến bay không còn tồn tại. Hãy tải lại và chọn chuyến khác.");
            scopeFlight = flightId; binding = true;
            try { fareScope.SelectedValue = flightId; }
            finally { binding = false; }
            ResetView(null); Render();
        }, "Đang tải hạng vé của chuyến…");
    }
    protected override void Dispose(bool disposing) { if (disposing) errors.Dispose(); base.Dispose(disposing); }
}
