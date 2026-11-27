using System.Collections;
using FlightBooking.DTO;

namespace FlightBooking.WinForms;

// GUI chỉ thu thập điều kiện; BUS thực hiện toàn bộ so sánh trên ArrayList.
public sealed class BoLocNangCao : FlowLayoutPanel
{
    public event EventHandler? YeuCauTimKiem;
    private readonly CheckBox[] dung = new CheckBox[2];
    private readonly ComboBox[] truong = new ComboBox[2];
    private readonly ComboBox[] phep = new ComboBox[2];
    private readonly TextBox[] giaTri = new TextBox[2];
    private readonly ComboBox noi = new() { DropDownStyle = ComboBoxStyle.DropDownList, Width = 90 };

    public BoLocNangCao(string[] cacTruong)
    {
        Dock = DockStyle.Fill; AutoSize = true; WrapContents = true;
        Margin = Padding.Empty;
        for (int i = 0; i < 2; i++)
        {
            dung[i] = new CheckBox { Text = "Điều kiện " + (i + 1), AutoSize = true, Margin = new Padding(3, 8, 4, 3) };
            truong[i] = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 155, AccessibleName = "Trường điều kiện " + (i + 1) };
            truong[i].FormattingEnabled = true;
            truong[i].Format += (_, e) => e.Value = ((string)e.ListItem!).Split('|')[0];
            truong[i].Items.AddRange(cacTruong); truong[i].SelectedIndex = 0;
            phep[i] = new ComboBox { DropDownStyle = ComboBoxStyle.DropDownList, Width = 100, AccessibleName = "Phép so sánh " + (i + 1) };
            phep[i].Items.AddRange(["=", "<>", ">", ">=", "<", "<=", "chứa"]); phep[i].SelectedIndex = 0;
            giaTri[i] = new TextBox { Width = 170, AccessibleName = "Giá trị điều kiện " + (i + 1), PlaceholderText = "Ngày dd/MM/yyyy; tiền VN" };
            giaTri[i].KeyDown += (_,e) => { if(e.KeyCode==Keys.Enter){ e.SuppressKeyPress=true;YeuCauTimKiem?.Invoke(this,EventArgs.Empty); } };
            Controls.AddRange([dung[i], truong[i], phep[i], giaTri[i]]);
            if (i == 0)
            {
                noi.Items.AddRange(["AND", "OR", "NOT"]); noi.SelectedIndex = 0; noi.AccessibleName = "Phép nối hai điều kiện";
                Controls.Add(noi); SetFlowBreak(noi, true);
            }
        }
        var ghiChu = GiaoDien.TaoNhan("Bật điều kiện rồi bấm Tìm kiếm. NOT = điều kiện 1 AND NOT điều kiện 2; bộ lọc nhanh vẫn áp dụng.", 9);
        SetFlowBreak(giaTri[1], true); Controls.Add(ghiChu);
    }

    public void XoaDieuKien() { for(int i=0;i<2;i++){ dung[i].Checked=false;giaTri[i].Clear(); } }

    public ArrayList DocDieuKien()
    {
        var ketQua = new ArrayList();
        for (int i = 0; i < 2; i++)
        {
            if (!dung[i].Checked) continue;
            if (string.IsNullOrWhiteSpace(giaTri[i].Text))
                throw new ArgumentException("Nhập giá trị điều kiện " + (i + 1) + ".");
            ketQua.Add(new DieuKienTimKiemDTO
            {
                Truong = ((string)truong[i].SelectedItem!).Split('|')[1], PhepSoSanh = phep[i].Text == "chứa" ? "CONTAINS" : phep[i].Text,
                GiaTri = giaTri[i].Text.Trim(), PhepNoi = i == 0 ? "AND" : noi.Text
            });
        }
        return ketQua;
    }
}
