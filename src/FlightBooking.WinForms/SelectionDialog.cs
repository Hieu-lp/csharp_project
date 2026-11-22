namespace FlightBooking.WinForms;

public sealed class SelectionDialog : Form
{
    public static object? SelectedValue { get; private set; }
    public SelectionDialog(string title, List<object> source, Func<object, string> label)
    {
        SelectedValue = null; Text = title; Font = new Font("Segoe UI", 11);
        AutoScaleDimensions = new SizeF(96, 96); AutoScaleMode = AutoScaleMode.Dpi; BackColor = Ui.Canvas;
        StartPosition = FormStartPosition.CenterParent;
        var work = Screen.FromPoint(Cursor.Position).WorkingArea;
        Size = Ui.WindowBounds(work, new Size(900, 580)).Size;
        MinimumSize = new Size(Math.Min(600, work.Width - 24), Math.Min(360, work.Height - 24));
        Padding = new Padding(20);
        var root = Ui.Table(); root.Dock = DockStyle.Fill; root.AutoSize = false;
        root.RowCount = 4; root.RowStyles.Add(new RowStyle(SizeType.AutoSize)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100)); root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        var header = Ui.Label(title, 17, true); root.Controls.Add(header, 0, 0);
        var search = new TextBox { Dock = DockStyle.Fill, AccessibleName = "Từ khóa chọn dữ liệu", PlaceholderText = "Nhập tên, thành phố, tuyến hoặc mã…", Margin = new Padding(0, 4, 0, 12) };
        var searchPanel = Ui.Table(); Ui.Row(searchPanel, Ui.Label("Tìm dữ liệu")); Ui.Row(searchPanel, search); root.Controls.Add(searchPanel, 0, 1);
        var list = Ui.List(); list.Columns.Add("Mã · Thông tin lựa chọn", 800); root.Controls.Add(list, 0, 2);
        var count = Ui.Label("");
        var footer = Ui.Table(2); footer.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100)); footer.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        footer.RowCount = 1; footer.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        footer.Controls.Add(count, 0, 0);
        void Choose()
        {
            if (list.SelectedItems.Count == 0) { count.Text = "Chọn một dòng trước khi xác nhận."; return; }
            SelectedValue = list.SelectedItems[0].Tag; DialogResult = DialogResult.OK; Close();
        }
        var choose = Ui.Button("Chọn", () => { Choose(); return Task.CompletedTask; }, primary: true);
        var cancel = Ui.Button("Hủy", () => { DialogResult = DialogResult.Cancel; Close(); return Task.CompletedTask; });
        cancel.DialogResult = DialogResult.Cancel;
        var actions = Ui.Actions(); actions.Padding = new Padding(0, 12, 0, 0); actions.Controls.Add(choose); actions.Controls.Add(cancel); footer.Controls.Add(actions, 1, 0);
        root.Controls.Add(footer, 0, 3); Controls.Add(root); CancelButton = cancel; AcceptButton = choose;
        void Render()
        {
            list.BeginUpdate(); list.Items.Clear();
            foreach (var item in source.Where(o => label(o).Contains(search.Text.Trim(), StringComparison.OrdinalIgnoreCase)))
                list.Items.Add(new ListViewItem(label(item)) { Tag = item });
            list.EndUpdate(); count.Text = list.Items.Count == 0 ? "Không tìm thấy kết quả." : $"{list.Items.Count} kết quả · Enter chọn · Esc hủy";
        }
        list.DoubleClick += (_, _) => Choose(); search.TextChanged += (_, _) => Render();
        FormClosed += (_, _) => { if (DialogResult != DialogResult.OK) SelectedValue = null; };
        Render();
    }
}
