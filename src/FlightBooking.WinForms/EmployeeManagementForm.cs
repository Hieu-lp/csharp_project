using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>Màn hình CRUD nhân viên; mọi luật dữ liệu được kiểm tra ở EmployeeBUS.</summary>
public sealed class EmployeeManagementForm : Form
{
    private readonly EmployeeBUS _employeeBUS = new();
    private readonly TextBox _idTextBox = CreateTextBox(readOnly: true);
    private readonly TextBox _nameTextBox = CreateTextBox();
    private readonly TextBox _phoneTextBox = CreateTextBox();
    private readonly TextBox _emailTextBox = CreateTextBox();
    private readonly TextBox _positionTextBox = CreateTextBox();
    private readonly TextBox _searchTextBox = CreateTextBox();
    private readonly DateTimePicker _hireDatePicker = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        MaxDate = DateTime.Today,
        Dock = DockStyle.Fill
    };
    private readonly ComboBox _statusComboBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };
    private readonly DataGridView _grid = new();
    private readonly Label _messageLabel = new() { AutoSize = true, Text = "Sẵn sàng." };

    public EmployeeManagementForm()
    {
        Text = "Quản lý nhân viên";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 650);
        Size = new Size(1180, 780);
        Font = new Font("Segoe UI", 9F);
        _statusComboBox.Items.AddRange(["ACTIVE", "INACTIVE"]);
        _statusComboBox.SelectedIndex = 0;

        ConfigureGrid();
        BuildLayout();
        WireEvents();
        Load += (_, _) => LoadEmployees();
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
        for (int row = 0; row < 5; row++)
            root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        root.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ NHÂN VIÊN",
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
        _searchTextBox.Width = 360;
        _searchTextBox.PlaceholderText = "Tên, điện thoại, email, chức vụ, trạng thái...";
        panel.Controls.Add(_searchTextBox);
        panel.Controls.Add(MakeButton("Tìm kiếm", (_, _) => SearchEmployees()));
        panel.Controls.Add(MakeButton("Tải lại", (_, _) => LoadEmployees()));
        return panel;
    }

    private Control BuildFields()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 4,
            Margin = new Padding(0),
            Padding = new Padding(0, 0, 0, 8)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        for (int row = 0; row < 4; row++) fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        AddField(fields, 0, 0, "Mã nhân viên:", _idTextBox);
        AddField(fields, 0, 2, "Họ và tên:", _nameTextBox);
        AddField(fields, 1, 0, "Điện thoại:", _phoneTextBox);
        AddField(fields, 1, 2, "Email:", _emailTextBox);
        AddField(fields, 2, 0, "Chức vụ:", _positionTextBox);
        AddField(fields, 2, 2, "Ngày vào làm:", _hireDatePicker);
        AddField(fields, 3, 0, "Trạng thái:", _statusComboBox);
        return fields;
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
        panel.Controls.Add(MakeButton("Thêm", (_, _) => AddEmployee()));
        panel.Controls.Add(MakeButton("Cập nhật", (_, _) => UpdateEmployee()));
        panel.Controls.Add(MakeButton("Xóa", (_, _) => DeleteEmployee()));
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
        AddColumn(nameof(EmployeeDTO.EmployeeId), "Mã NV", 9);
        AddColumn(nameof(EmployeeDTO.FullName), "Họ và tên", 20);
        AddColumn(nameof(EmployeeDTO.Phone), "Điện thoại", 13);
        AddColumn(nameof(EmployeeDTO.Email), "Email", 22);
        AddColumn(nameof(EmployeeDTO.Position), "Chức vụ", 15);
        AddColumn(nameof(EmployeeDTO.HireDate), "Ngày vào làm", 11, "dd/MM/yyyy");
        AddColumn(nameof(EmployeeDTO.Status), "Trạng thái", 10);
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
            SearchEmployees();
            e.SuppressKeyPress = true;
        };
    }

    private void LoadEmployees()
    {
        try
        {
            _grid.DataSource = _employeeBUS.GetAll();
            _messageLabel.Text = $"Đang hiển thị {_grid.Rows.Count} nhân viên.";
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void SearchEmployees()
    {
        try
        {
            var results = _employeeBUS.Search(_searchTextBox.Text).Cast<EmployeeDTO>().ToList();
            _grid.DataSource = results;
            _messageLabel.Text = $"Tìm thấy {results.Count} nhân viên.";
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void AddEmployee()
    {
        try
        {
            string? error = _employeeBUS.Add(ReadEmployeeFromForm());
            if (ShowBusinessError(error)) return;
            LoadEmployees();
            ClearForm();
            MessageBox.Show("Đã thêm nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void UpdateEmployee()
    {
        if (!TryGetSelectedId(out int id))
        {
            MessageBox.Show("Hãy chọn nhân viên cần cập nhật trong bảng.", "Chưa chọn nhân viên",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        try
        {
            EmployeeDTO employee = ReadEmployeeFromForm();
            employee.EmployeeId = id;
            string? error = _employeeBUS.Update(employee);
            if (ShowBusinessError(error)) return;
            LoadEmployees();
            ClearForm();
            MessageBox.Show("Đã cập nhật nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private void DeleteEmployee()
    {
        if (!TryGetSelectedId(out int id))
        {
            MessageBox.Show("Hãy chọn nhân viên cần xóa trong bảng.", "Chưa chọn nhân viên",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show($"Bạn có chắc muốn xóa nhân viên mã {id} không?", "Xác nhận xóa",
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;
        try
        {
            string? error = _employeeBUS.Delete(id);
            if (ShowBusinessError(error)) return;
            LoadEmployees();
            ClearForm();
            MessageBox.Show("Đã xóa nhân viên.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception) { ShowDataError(exception); }
    }

    private EmployeeDTO ReadEmployeeFromForm() => new()
    {
        FullName = _nameTextBox.Text,
        Phone = _phoneTextBox.Text,
        Email = _emailTextBox.Text,
        Position = _positionTextBox.Text,
        HireDate = _hireDatePicker.Value.Date,
        Status = _statusComboBox.Text
    };

    private void CopySelectedToForm()
    {
        if (_grid.CurrentRow?.DataBoundItem is not EmployeeDTO employee) return;
        _idTextBox.Text = employee.EmployeeId.ToString();
        _nameTextBox.Text = employee.FullName;
        _phoneTextBox.Text = employee.Phone;
        _emailTextBox.Text = employee.Email;
        _positionTextBox.Text = employee.Position;
        _hireDatePicker.Value = employee.HireDate;
        _statusComboBox.Text = employee.Status;
    }

    private bool TryGetSelectedId(out int id) => int.TryParse(_idTextBox.Text, out id) && id > 0;

    private void ClearForm()
    {
        _grid.ClearSelection();
        _idTextBox.Clear();
        _nameTextBox.Clear();
        _phoneTextBox.Clear();
        _emailTextBox.Clear();
        _positionTextBox.Clear();
        _hireDatePicker.Value = DateTime.Today;
        _statusComboBox.SelectedIndex = 0;
        _nameTextBox.Focus();
    }

    private static bool ShowBusinessError(string? error)
    {
        if (error is null) return false;
        MessageBox.Show(error, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private void ShowDataError(Exception exception)
    {
        _messageLabel.Text = "Không tải/thao tác được dữ liệu nhân viên.";
        string detail = exception is SqlException { Number: 547 }
            ? "Database từ chối thao tác vì nhân viên đang được dữ liệu khác tham chiếu hoặc vi phạm ràng buộc."
            : exception.Message;
        MessageBox.Show($"Không thể hoàn thành thao tác nhân viên.\nChi tiết: {detail}",
            "Lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
    }

    private static void AddField(TableLayoutPanel table, int row, int labelColumn, string caption, Control input)
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
    }

    private static TextBox CreateTextBox(bool readOnly = false) => new()
    {
        ReadOnly = readOnly,
        Dock = DockStyle.Fill,
        Height = 30
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
