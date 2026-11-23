using FlightBooking.BUS;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.WinForms;

/// <summary>
/// Màn hình quản lý khách hàng.
/// Form chỉ đọc/hiển thị dữ liệu và gọi BUS; câu lệnh SQL nằm trong DAO.
/// </summary>
public sealed class CustomerManagementForm : Form
{
    private readonly CustomerBUS _customerBUS = new();

    // Các ô nhập liệu tương ứng với thông tin trong CustomerDTO.
    private readonly TextBox _customerIdTextBox = CreateTextBox(readOnly: true);
    private readonly TextBox _fullNameTextBox = CreateTextBox();
    private readonly TextBox _phoneTextBox = CreateTextBox();
    private readonly TextBox _emailTextBox = CreateTextBox();
    private readonly TextBox _passportTextBox = CreateTextBox();
    private readonly DateTimePicker _dateOfBirthPicker = new()
    {
        Format = DateTimePickerFormat.Custom,
        CustomFormat = "dd/MM/yyyy",
        MaxDate = DateTime.Today,
        Dock = DockStyle.Fill
    };
    private readonly ComboBox _genderComboBox = new()
    {
        DropDownStyle = ComboBoxStyle.DropDownList,
        Dock = DockStyle.Fill
    };

    private readonly TextBox _searchTextBox = CreateTextBox();
    private readonly DataGridView _customerGrid = new();
    private readonly Label _statusLabel = new()
    {
        AutoSize = true,
        Text = "Sẵn sàng."
    };

    private readonly Button _searchButton = CreateButton("Tìm kiếm");
    private readonly Button _reloadButton = CreateButton("Tải lại");
    private readonly Button _addButton = CreateButton("Thêm");
    private readonly Button _updateButton = CreateButton("Cập nhật");
    private readonly Button _deleteButton = CreateButton("Xóa");
    private readonly Button _clearButton = CreateButton("Làm trống form");

    public CustomerManagementForm()
    {
        Text = "Quản lý khách hàng";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1000, 680);
        Size = new Size(1200, 800);
        Font = new Font("Segoe UI", 9F);

        // Chỉ hiện một lựa chọn Nữ; dữ liệu cũ "Nu" được quy đổi khi nạp lên form.
        _genderComboBox.Items.AddRange(["Nam", "Nữ", "Khác"]);
        _genderComboBox.Text = "Nam";

        ConfigureGrid();
        BuildLayout();
        WireEvents();

        // Chỉ truy cập database khi form chạy; không sửa cấu trúc database.
        Load += (_, _) => LoadCustomers();
    }

    /// <summary>
    /// Tạo bố cục từ trên xuống: tiêu đề, tìm kiếm, form nhập, nút, trạng thái, bảng.
    /// </summary>
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
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        var title = new Label
        {
            AutoSize = true,
            Text = "QUẢN LÝ KHÁCH HÀNG",
            Font = new Font(Font, FontStyle.Bold),
            Margin = new Padding(3, 0, 3, 14)
        };
        root.Controls.Add(title, 0, 0);
        root.Controls.Add(BuildSearchPanel(), 0, 1);
        root.Controls.Add(BuildCustomerFields(), 0, 2);
        root.Controls.Add(BuildActionPanel(), 0, 3);
        root.Controls.Add(_statusLabel, 0, 4);
        root.Controls.Add(_customerGrid, 0, 5);

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

        panel.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "Từ khóa:",
            Margin = new Padding(3, 9, 8, 3)
        });

        _searchTextBox.Width = 340;
        _searchTextBox.PlaceholderText = "Tên, điện thoại, email, mã khách hàng...";
        panel.Controls.Add(_searchTextBox);
        panel.Controls.Add(_searchButton);
        panel.Controls.Add(_reloadButton);
        return panel;
    }

    private Control BuildCustomerFields()
    {
        var fields = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 4,
            RowCount = 4,
            Padding = new Padding(0, 0, 0, 8),
            Margin = new Padding(0)
        };

        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 13));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 37));

        for (int row = 0; row < 4; row++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        }

        AddField(fields, 0, 0, "Mã khách hàng:", _customerIdTextBox);
        AddField(fields, 0, 2, "Họ và tên:", _fullNameTextBox);
        AddField(fields, 1, 0, "Điện thoại:", _phoneTextBox);
        AddField(fields, 1, 2, "Email:", _emailTextBox);
        AddField(fields, 2, 0, "Ngày sinh:", _dateOfBirthPicker);
        AddField(fields, 2, 2, "Giới tính:", _genderComboBox);
        AddField(fields, 3, 0, "Số hộ chiếu/CCCD:", _passportTextBox, columnSpan: 3);

        return fields;
    }

    private Control BuildActionPanel()
    {
        var panel = new FlowLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            WrapContents = false,
            Margin = new Padding(0, 0, 0, 8)
        };

        panel.Controls.Add(_addButton);
        panel.Controls.Add(_updateButton);
        panel.Controls.Add(_deleteButton);
        panel.Controls.Add(_clearButton);
        return panel;
    }

    private void ConfigureGrid()
    {
        _customerGrid.Dock = DockStyle.Fill;
        _customerGrid.ReadOnly = true;
        _customerGrid.AllowUserToAddRows = false;
        _customerGrid.AllowUserToDeleteRows = false;
        _customerGrid.MultiSelect = false;
        _customerGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        _customerGrid.AutoGenerateColumns = false;
        _customerGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        _customerGrid.RowHeadersVisible = false;

        AddGridColumn(nameof(CustomerDTO.CustomerId), "Mã KH", 8);
        AddGridColumn(nameof(CustomerDTO.FullName), "Họ và tên", 18);
        AddGridColumn(nameof(CustomerDTO.Phone), "Điện thoại", 12);
        AddGridColumn(nameof(CustomerDTO.Email), "Email", 19);
        AddGridColumn(nameof(CustomerDTO.DateOfBirth), "Ngày sinh", 11, "dd/MM/yyyy");
        AddGridColumn(nameof(CustomerDTO.Gender), "Giới tính", 9);
        AddGridColumn(nameof(CustomerDTO.IdPassportNumber), "Hộ chiếu/CCCD", 15);
    }

    private void AddGridColumn(string propertyName, string header, float fillWeight, string? format = null)
    {
        var column = new DataGridViewTextBoxColumn
        {
            Name = propertyName,
            HeaderText = header,
            DataPropertyName = propertyName,
            FillWeight = fillWeight
        };

        if (format is not null)
        {
            column.DefaultCellStyle = new DataGridViewCellStyle { Format = format };
        }

        _customerGrid.Columns.Add(column);
    }

    private void WireEvents()
    {
        _searchButton.Click += (_, _) => SearchCustomers();
        _reloadButton.Click += (_, _) => LoadCustomers();
        _addButton.Click += (_, _) => AddCustomer();
        _updateButton.Click += (_, _) => UpdateCustomer();
        _deleteButton.Click += (_, _) => DeleteCustomer();
        _clearButton.Click += (_, _) => ClearForm();
        _customerGrid.SelectionChanged += (_, _) => CopySelectedCustomerToForm();
        _searchTextBox.KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Enter)
            {
                SearchCustomers();
                e.SuppressKeyPress = true;
            }
        };
    }

    private void LoadCustomers()
    {
        try
        {
            _customerGrid.DataSource = _customerBUS.GetAll();
            _statusLabel.Text = $"Đang hiển thị {_customerGrid.Rows.Count} khách hàng.";
        }
        catch (Exception exception)
        {
            ShowDataError(exception);
        }
    }

    private void SearchCustomers()
    {
        try
        {
            // BUS trả ArrayList theo yêu cầu môn học; form chuyển kết quả về danh sách DTO để bind.
            var results = _customerBUS.Search(_searchTextBox.Text)
                .Cast<CustomerDTO>()
                .ToList();

            _customerGrid.DataSource = results;
            _statusLabel.Text = $"Tìm thấy {results.Count} khách hàng.";
        }
        catch (Exception exception)
        {
            ShowDataError(exception);
        }
    }

    private void AddCustomer()
    {
        try
        {
            string? error = _customerBUS.Add(ReadCustomerFromForm());
            if (ShowBusinessError(error)) return;

            LoadCustomers();
            ClearForm();
            MessageBox.Show("Đã thêm khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ShowDataError(exception);
        }
    }

    private void UpdateCustomer()
    {
        if (!TryGetSelectedCustomerId(out int customerId))
        {
            MessageBox.Show("Hãy chọn khách hàng cần cập nhật trong bảng.", "Chưa chọn khách hàng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            CustomerDTO customer = ReadCustomerFromForm();
            customer.CustomerId = customerId;
            string? error = _customerBUS.Update(customer);
            if (ShowBusinessError(error)) return;

            LoadCustomers();
            ClearForm();
            MessageBox.Show("Đã cập nhật khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ShowDataError(exception);
        }
    }

    private void DeleteCustomer()
    {
        if (!TryGetSelectedCustomerId(out int customerId))
        {
            MessageBox.Show("Hãy chọn khách hàng cần xóa trong bảng.", "Chưa chọn khách hàng",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DialogResult answer = MessageBox.Show(
            $"Bạn có chắc muốn xóa khách hàng mã {customerId} không?",
            "Xác nhận xóa",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Question);

        if (answer != DialogResult.Yes) return;

        try
        {
            string? error = _customerBUS.Delete(customerId);
            if (ShowBusinessError(error)) return;

            LoadCustomers();
            ClearForm();
            MessageBox.Show("Đã xóa khách hàng.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception exception)
        {
            ShowDataError(exception);
        }
    }

    private CustomerDTO ReadCustomerFromForm() => new()
    {
        FullName = _fullNameTextBox.Text,
        Phone = _phoneTextBox.Text,
        Email = _emailTextBox.Text,
        DateOfBirth = _dateOfBirthPicker.Value.Date,
        Gender = _genderComboBox.Text,
        IdPassportNumber = _passportTextBox.Text
    };

    private void CopySelectedCustomerToForm()
    {
        if (_customerGrid.CurrentRow?.DataBoundItem is not CustomerDTO customer) return;

        _customerIdTextBox.Text = customer.CustomerId.ToString();
        _fullNameTextBox.Text = customer.FullName;
        _phoneTextBox.Text = customer.Phone;
        _emailTextBox.Text = customer.Email;
        _dateOfBirthPicker.Value = customer.DateOfBirth;
        _genderComboBox.Text = customer.Gender == "Nu" ? "Nữ" : customer.Gender;
        _passportTextBox.Text = customer.IdPassportNumber ?? string.Empty;
    }

    private bool TryGetSelectedCustomerId(out int customerId) =>
        int.TryParse(_customerIdTextBox.Text, out customerId) && customerId > 0;

    private void ClearForm()
    {
        _customerGrid.ClearSelection();
        _customerIdTextBox.Clear();
        _fullNameTextBox.Clear();
        _phoneTextBox.Clear();
        _emailTextBox.Clear();
        _passportTextBox.Clear();
        _dateOfBirthPicker.Value = DateTime.Today;
        _genderComboBox.Text = "Nam";
        _fullNameTextBox.Focus();
    }

    private static bool ShowBusinessError(string? error)
    {
        if (error is null) return false;

        MessageBox.Show(error, "Dữ liệu chưa hợp lệ", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        return true;
    }

    private void ShowDataError(Exception exception)
    {
        _statusLabel.Text = "Không tải/thao tác được dữ liệu khách hàng.";
        string detail = exception is SqlException { Number: 547 }
            ? "Database từ chối thao tác vì khách hàng đang được dữ liệu khác tham chiếu hoặc vi phạm ràng buộc."
            : exception.Message;
        MessageBox.Show(
            $"Không thể hoàn thành thao tác với dữ liệu khách hàng.\nChi tiết: {detail}",
            "Lỗi",
            MessageBoxButtons.OK,
            MessageBoxIcon.Error);
    }

    private static void AddField(
        TableLayoutPanel table,
        int row,
        int labelColumn,
        string caption,
        Control input,
        int columnSpan = 1)
    {
        var label = new Label
        {
            AutoSize = true,
            Text = caption,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 10, 8, 3)
        };

        input.Dock = DockStyle.Fill;
        input.Margin = new Padding(3, 3, 12, 8);
        table.Controls.Add(label, labelColumn, row);
        table.Controls.Add(input, labelColumn + 1, row);

        if (columnSpan > 1)
        {
            table.SetColumnSpan(input, columnSpan);
        }
    }

    private static TextBox CreateTextBox(bool readOnly = false) => new()
    {
        ReadOnly = readOnly,
        Dock = DockStyle.Fill,
        Height = 30
    };

    private static Button CreateButton(string text) => new()
    {
        Text = text,
        AutoSize = true,
        Height = 34,
        Margin = new Padding(3, 3, 8, 3),
        Padding = new Padding(8, 2, 8, 2)
    };
}
