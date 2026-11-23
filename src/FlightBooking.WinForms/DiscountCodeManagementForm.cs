using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>Màn hình CRUD mã giảm giá; BUS kiểm tra trùng mã, giá trị và thời hạn.</summary>
public sealed class DiscountCodeManagementForm : Form
{
    private const decimal MaxDecimal18Scale2 = 9999999999999999.99m;
    private readonly DiscountCodeBUS _discountBUS = new();

    private readonly TextBox _idTextBox = CreateTextBox(readOnly: true);
    private readonly TextBox _codeTextBox = CreateTextBox();
    private readonly TextBox _descriptionTextBox = new() { Multiline = true, Height = 54, ScrollBars = ScrollBars.Vertical };
    private readonly ComboBox _typeComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly ComboBox _statusComboBox = new() { DropDownStyle = ComboBoxStyle.DropDownList };
    private readonly NumericUpDown _valueInput = CreateMoneyInput();
    private readonly NumericUpDown _minimumOrderInput = CreateMoneyInput();
    private readonly NumericUpDown _maximumDiscountInput = CreateMoneyInput();
    private readonly NumericUpDown _usageLimitInput = new()
    {
        Minimum = 0,
        Maximum = int.MaxValue,
        ThousandsSeparator = true,
        Width = 150
    };
    private readonly CheckBox _hasMaximumDiscountCheckBox = new() { Text = "Có mức giảm tối đa", AutoSize = true };
    private readonly CheckBox _hasUsageLimitCheckBox = new() { Text = "Có giới hạn lượt dùng", AutoSize = true };
    private readonly DateTimePicker _startDatePicker = CreateDateTimePicker();
    private readonly DateTimePicker _endDatePicker = CreateDateTimePicker();
    private readonly TextBox _usedCountTextBox = CreateTextBox(readOnly: true);
    private readonly TextBox _searchTextBox = CreateTextBox();
    private readonly DataGridView _grid = new();
    private readonly Label _messageLabel = new() { AutoSize = true, Text = "Sẵn sàng." };

    public DiscountCodeManagementForm()
    {
        Text = "Quản lý mã giảm giá";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1100, 760);
        Size = new Size(1380, 900);
        Font = new Font("Segoe UI", 9F);

        _typeComboBox.Items.AddRange(["PERCENT", "FIXED"]);
        _typeComboBox.SelectedIndex = 0;
        _statusComboBox.Items.AddRange(["ACTIVE", "INACTIVE"]);
        _statusComboBox.SelectedIndex = 0;
        _startDatePicker.Value = DateTime.Now;
        _endDatePicker.Value = DateTime.Now.AddDays(7);
        _maximumDiscountInput.Enabled = false;
        _usageLimitInput.Enabled = false;

        ConfigureGrid();
        BuildLayout();
        WireEvents();
        Load += (_, _) => LoadDiscountCodes();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(18),
            ColumnCount = 1,
            RowCount = 6
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int row = 0; row < 5; row++) root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ MÃ GIẢM GIÁ",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 14)
        }, 0, 0);
        root.Controls.Add(BuildSearchPanel(), 0, 1);
        root.Controls.Add(BuildFields(), 0, 2);
        root.Controls.Add(BuildActions(), 0, 3);
        root.Controls.Add(_messageLabel, 0, 4);
        root.Controls.Add(_grid, 0, 5);
        Controls.Add(root);
    }

    private Control BuildSearchPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 12)
        };
        panel.Controls.Add(new Label { AutoSize = true, Text = "Từ khóa:", Margin = new Padding(3, 9, 8, 3) });
        _searchTextBox.Width = 380;
        _searchTextBox.PlaceholderText = "Mã, mô tả, loại giảm, trạng thái...";
        panel.Controls.Add(_searchTextBox);
        panel.Controls.Add(MakeButton("Tìm kiếm", (_, _) => SearchDiscountCodes()));
        panel.Controls.Add(MakeButton("Tải lại", (_, _) => LoadDiscountCodes()));
        return panel;
    }

    private Control BuildFields()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 7,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 8)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 15));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
        for (int row = 0; row < 7; row++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddField(fields, 0, 0, "Mã bản ghi:", _idTextBox);
        AddField(fields, 0, 2, "Mã giảm giá:", _codeTextBox);
        AddField(fields, 1, 0, "Mô tả:", _descriptionTextBox, columnSpan: 3);
        AddField(fields, 2, 0, "Loại giảm:", _typeComboBox);
        AddField(fields, 2, 2, "Giá trị giảm (%/đồng):", _valueInput);
        AddField(fields, 3, 0, "Đơn tối thiểu (đồng):", _minimumOrderInput);
        AddField(fields, 3, 2, "Giảm tối đa (đồng):", BuildOptionalInput(_hasMaximumDiscountCheckBox, _maximumDiscountInput));
        AddField(fields, 4, 0, "Bắt đầu:", _startDatePicker);
        AddField(fields, 4, 2, "Kết thúc:", _endDatePicker);
        AddField(fields, 5, 0, "Giới hạn lượt:", BuildOptionalInput(_hasUsageLimitCheckBox, _usageLimitInput));
        AddField(fields, 5, 2, "Đã sử dụng:", _usedCountTextBox);
        AddField(fields, 6, 0, "Trạng thái:", _statusComboBox);
        return fields;
    }

    private Control BuildOptionalInput(CheckBox checkBox, Control input)
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0)
        };
        checkBox.CheckedChanged += (_, _) => input.Enabled = checkBox.Checked;
        panel.Controls.Add(checkBox);
        panel.Controls.Add(input);
        return panel;
    }

    private Control BuildActions()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };
        panel.Controls.Add(MakeButton("Thêm", (_, _) => AddDiscountCode()));
        panel.Controls.Add(MakeButton("Cập nhật", (_, _) => UpdateDiscountCode()));
        panel.Controls.Add(MakeButton("Xóa", (_, _) => DeleteDiscountCode()));
        panel.Controls.Add(MakeButton("Làm trống form", (_, _) => ClearForm()));
        return panel;
    }

    private void ConfigureGrid()
    {
        _grid.Dock = DockStyle.Fill;
        _grid.ReadOnly = true;
        _grid.AllowUserToAddRows = false;
        _grid.AllowUserToDeleteRows = false;
        _grid.MultiSelect = false;
        _grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _grid.AutoGenerateColumns = false;
        _grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _grid.RowHeadersVisible = false;
        AddColumn(nameof(DiscountCodeDTO.DiscountId), "Mã", 6);
        AddColumn(nameof(DiscountCodeDTO.Code), "Code", 10);
        AddColumn(nameof(DiscountCodeDTO.Description), "Mô tả", 15);
        AddColumn(nameof(DiscountCodeDTO.DiscountType), "Loại", 7);
        AddColumn(nameof(DiscountCodeDTO.DiscountValue), "Giá trị", 8, "N2");
        AddColumn(nameof(DiscountCodeDTO.MinOrderAmount), "Đơn tối thiểu", 9, "N2");
        AddColumn(nameof(DiscountCodeDTO.MaxDiscountAmount), "Giảm tối đa", 9, "N2");
        AddColumn(nameof(DiscountCodeDTO.StartDate), "Bắt đầu", 11, "dd/MM/yyyy HH:mm");
        AddColumn(nameof(DiscountCodeDTO.EndDate), "Kết thúc", 11, "dd/MM/yyyy HH:mm");
        AddColumn(nameof(DiscountCodeDTO.UsageLimit), "Giới hạn", 6);
        AddColumn(nameof(DiscountCodeDTO.UsedCount), "Đã dùng", 5);
        AddColumn(nameof(DiscountCodeDTO.Status), "Trạng thái", 7);
    }

    private void AddColumn(string property, string header, float weight, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = property,
            HeaderText = header,
            DataPropertyName = property,
            FillWeight = weight
        };
        if (format is not null) column.DefaultCellStyle = new DataGridViewCellStyle { Format = format };
        _grid.Columns.Add(column);
    }

    private void WireEvents()
    {
        _grid.SelectionChanged += (_, _) => CopySelectedToForm();
        _searchTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode != Keys.Enter) return;
            SearchDiscountCodes();
            e.SuppressKeyPress = true;
        };
    }

    private void LoadDiscountCodes()
    {
        try
        {
            _grid.DataSource = _discountBUS.GetAll();
            _messageLabel.Text = $"Đang hiển thị {_grid.Rows.Count} mã giảm giá.";
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void SearchDiscountCodes()
    {
        try
        {
            var results = _discountBUS.Search(_searchTextBox.Text).Cast<DiscountCodeDTO>().ToList();
            _grid.DataSource = results;
            _messageLabel.Text = $"Tìm thấy {results.Count} mã giảm giá.";
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void AddDiscountCode()
    {
        try
        {
            string? error = _discountBUS.Add(ReadDiscountFromForm());
            if (ShowBusinessError(error)) return;
            LoadDiscountCodes();
            ClearForm();
            MessageBox.Show("Đã thêm mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void UpdateDiscountCode()
    {
        if (!TryGetSelectedId(out int id))
        {
            MessageBox.Show("Hãy chọn mã giảm giá cần cập nhật trong bảng.", "Chưa chọn mã",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            DiscountCodeDTO discount = ReadDiscountFromForm();
            discount.DiscountId = id;
            string? error = _discountBUS.Update(discount);
            if (ShowBusinessError(error)) return;
            LoadDiscountCodes();
            ClearForm();
            MessageBox.Show("Đã cập nhật mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void DeleteDiscountCode()
    {
        if (!TryGetSelectedId(out int id))
        {
            MessageBox.Show("Hãy chọn mã giảm giá cần xóa trong bảng.", "Chưa chọn mã",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show($"Bạn có chắc muốn xóa mã giảm giá mã {id} không?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            string? error = _discountBUS.Delete(id);
            if (ShowBusinessError(error)) return;
            LoadDiscountCodes();
            ClearForm();
            MessageBox.Show("Đã xóa mã giảm giá.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private DiscountCodeDTO ReadDiscountFromForm() => new()
    {
        Code = _codeTextBox.Text,
        Description = _descriptionTextBox.Text,
        DiscountType = _typeComboBox.Text,
        DiscountValue = _valueInput.Value,
        MinOrderAmount = _minimumOrderInput.Value,
        MaxDiscountAmount = _hasMaximumDiscountCheckBox.Checked ? _maximumDiscountInput.Value : null,
        StartDate = _startDatePicker.Value,
        EndDate = _endDatePicker.Value,
        UsageLimit = _hasUsageLimitCheckBox.Checked ? decimal.ToInt32(_usageLimitInput.Value) : null,
        Status = _statusComboBox.Text
        // UsedCount is intentionally not read from any editable input; BUS loads the real DB count on update.
    };

    private void CopySelectedToForm()
    {
        if (_grid.CurrentRow?.DataBoundItem is not DiscountCodeDTO discount) return;
        _idTextBox.Text = discount.DiscountId.ToString();
        _codeTextBox.Text = discount.Code;
        _descriptionTextBox.Text = discount.Description;
        _typeComboBox.Text = discount.DiscountType;
        _valueInput.Value = Math.Clamp(discount.DiscountValue, _valueInput.Minimum, _valueInput.Maximum);
        _minimumOrderInput.Value = Math.Clamp(discount.MinOrderAmount, _minimumOrderInput.Minimum, _minimumOrderInput.Maximum);
        _hasMaximumDiscountCheckBox.Checked = discount.MaxDiscountAmount.HasValue;
        if (discount.MaxDiscountAmount.HasValue)
            _maximumDiscountInput.Value = Math.Clamp(discount.MaxDiscountAmount.Value, _maximumDiscountInput.Minimum, _maximumDiscountInput.Maximum);
        _startDatePicker.Value = discount.StartDate;
        _endDatePicker.Value = discount.EndDate;
        _hasUsageLimitCheckBox.Checked = discount.UsageLimit.HasValue;
        if (discount.UsageLimit.HasValue) _usageLimitInput.Value = discount.UsageLimit.Value;
        _usedCountTextBox.Text = discount.UsedCount.ToString();
        _statusComboBox.Text = discount.Status;
    }

    private bool TryGetSelectedId(out int id) => int.TryParse(_idTextBox.Text, out id) && id > 0;

    private void ClearForm()
    {
        _grid.ClearSelection();
        _idTextBox.Clear();
        _codeTextBox.Clear();
        _descriptionTextBox.Clear();
        _typeComboBox.SelectedIndex = 0;
        _valueInput.Value = 0;
        _minimumOrderInput.Value = 0;
        _hasMaximumDiscountCheckBox.Checked = false;
        _maximumDiscountInput.Value = 0;
        _startDatePicker.Value = DateTime.Now;
        _endDatePicker.Value = DateTime.Now.AddDays(7);
        _hasUsageLimitCheckBox.Checked = false;
        _usageLimitInput.Value = 0;
        _usedCountTextBox.Text = "0";
        _statusComboBox.SelectedIndex = 0;
        _codeTextBox.Focus();
    }

    private static bool ShowBusinessError(string? error)
    {
        if (error is null) return false;
        MessageBox.Show(error, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private void ShowDataError(Exception exception)
    {
        _messageLabel.Text = "Không tải/thao tác được mã giảm giá.";
        string detail = exception is SqlException { Number: 547 }
            ? "Database từ chối thao tác vì mã giảm giá đang được booking tham chiếu hoặc vi phạm ràng buộc."
            : exception.Message;
        MessageBox.Show($"Không thể hoàn thành thao tác mã giảm giá.\nChi tiết: {detail}",
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void AddField(TableLayoutPanel table, int row, int labelColumn, string caption, Control input, int columnSpan = 1)
    {
        table.Controls.Add(new Label
        {
            AutoSize = true,
            Text = caption,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 3)
        }, labelColumn, row);
        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 3, 12, 8);
        table.Controls.Add(input, labelColumn + 1, row);
        if (columnSpan > 1) table.SetColumnSpan(input, columnSpan);
    }

    private static TextBox CreateTextBox(bool readOnly = false) => new()
    {
        ReadOnly = readOnly,
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static NumericUpDown CreateMoneyInput() => new()
    {
        Minimum = 0,
        Maximum = MaxDecimal18Scale2,
        DecimalPlaces = 2,
        Increment = 0.01m,
        ThousandsSeparator = true,
        Dock = DockStyle.Fill
    };

    private static DateTimePicker CreateDateTimePicker() => new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy HH:mm",
        ShowUpDown = true,
        Dock = DockStyle.Fill
    };

    private static Button MakeButton(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 34,
            Margin = new Padding(3, 3, 8, 3),
            Padding = new Padding(8, 2, 8, 2)
        };
        button.Click += click;
        return button;
    }
}
