namespace FlightBooking.WinForms;

/// <summary>MainForm chứa menu tổng và mở các màn hình quản lý trong vùng nội dung.</summary>
public sealed class MainForm : Form
{
    private readonly Panel _contentPanel = new() { Dock = DockStyle.Fill, Padding = new Padding(12) };
    private Form? _activeModule;

    public MainForm()
    {
        Text = "Flight Booking System - Quản trị";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(1150, 760);
        Size = new Size(1450, 920);
        Font = new Font("Segoe UI", 9F);

        BuildLayout();
        ShowHome();
    }

    private void BuildLayout()
    {
        var menu = BuildMenu();
        var body = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 1,
            Margin = new Padding(0)
        };
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
        body.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        body.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        body.Controls.Add(BuildNavigation(), 0, 0);
        body.Controls.Add(_contentPanel, 1, 0);

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 2,
            Margin = new Padding(0)
        };
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.Controls.Add(menu, 0, 0);
        root.Controls.Add(body, 0, 1);
        Controls.Add(root);
    }

    private MenuStrip BuildMenu()
    {
        var menu = new MenuStrip { Dock = DockStyle.Fill };
        var fileMenu = new ToolStripMenuItem("Tệp");
        fileMenu.DropDownItems.Add("Trang chủ", null, (_, _) => ShowHome());
        fileMenu.DropDownItems.Add(new ToolStripSeparator());
        fileMenu.DropDownItems.Add("Thoát", null, (_, _) => Close());

        var managementMenu = new ToolStripMenuItem("Quản lý");
        managementMenu.DropDownItems.Add("Khách hàng", null, (_, _) => OpenModule(new CustomerManagementForm()));
        managementMenu.DropDownItems.Add("Nhân viên", null, (_, _) => OpenModule(new EmployeeManagementForm()));
        managementMenu.DropDownItems.Add("Mã giảm giá", null, (_, _) => OpenModule(new DiscountCodeManagementForm()));
        managementMenu.DropDownItems.Add("Sân bay / chuyến bay / hạng vé", null, (_, _) => OpenFlightManagement());

        var helpMenu = new ToolStripMenuItem("Trợ giúp");
        helpMenu.DropDownItems.Add("Giới thiệu", null, (_, _) =>
            MessageBox.Show("Ứng dụng quản lý hệ thống đặt vé máy bay.", "Giới thiệu",
                MessageBoxButtons.OK, MessageBoxIcon.Information));

        menu.Items.AddRange([fileMenu, managementMenu, helpMenu]);
        MainMenuStrip = menu;
        return menu;
    }

    private Control BuildNavigation()
    {
        var sidebar = new Panel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(12),
            BackColor = Color.FromArgb(34, 45, 65)
        };
        var stack = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            FlowDirection = FlowDirection.TopDown,
            WrapContents = false,
            AutoSize = true
        };
        stack.Controls.Add(new Label
        {
            AutoSize = true,
            Text = "FLIGHT BOOKING",
            ForeColor = Color.White,
            Font = new Font("Segoe UI", 12F, FontStyle.Bold),
            Margin = new Padding(4, 4, 4, 22)
        });
        stack.Controls.Add(CreateNavigationButton("Trang chủ", (_, _) => ShowHome()));
        stack.Controls.Add(CreateNavigationButton("Quản lý khách hàng", (_, _) => OpenModule(new CustomerManagementForm())));
        stack.Controls.Add(CreateNavigationButton("Quản lý nhân viên", (_, _) => OpenModule(new EmployeeManagementForm())));
        stack.Controls.Add(CreateNavigationButton("Quản lý mã giảm giá", (_, _) => OpenModule(new DiscountCodeManagementForm())));
        stack.Controls.Add(CreateNavigationButton("Quản lý chuyến bay", (_, _) => OpenFlightManagement()));
        sidebar.Controls.Add(stack);
        return sidebar;
    }

    private void OpenFlightManagement()
    {
        using var flightManagement = new Form1();
        flightManagement.ShowDialog(this);
    }

    private static Button CreateNavigationButton(string text, EventHandler click)
    {
        var button = new Button
        {
            Text = text,
            Width = 194,
            Height = 42,
            TextAlign = ContentAlignment.MiddleLeft,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.FromArgb(48, 62, 86),
            ForeColor = Color.White,
            Margin = new Padding(0, 0, 0, 8),
            Padding = new Padding(10, 0, 4, 0)
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += click;
        return button;
    }

    private void ShowHome()
    {
        CloseActiveModule();
        var welcome = new Label
        {
            Dock = DockStyle.Fill,
            Text = "Chào mừng đến với hệ thống quản lý đặt vé máy bay.\n\nChọn một mục trong menu để bắt đầu.",
            TextAlign = ContentAlignment.MiddleCenter,
            Font = new Font("Segoe UI", 16F),
            ForeColor = Color.FromArgb(48, 62, 86)
        };
        _contentPanel.Controls.Add(welcome);
    }

    private void OpenModule(Form module)
    {
        CloseActiveModule();
        _activeModule = module;
        module.TopLevel = false;
        module.FormBorderStyle = FormBorderStyle.None;
        module.Dock = DockStyle.Fill;
        _contentPanel.Controls.Add(module);
        module.Show();
    }

    private void CloseActiveModule()
    {
        foreach (Control control in _contentPanel.Controls.Cast<Control>().ToArray())
        {
            _contentPanel.Controls.Remove(control);
            control.Dispose();
        }

        _activeModule = null;
    }
}
