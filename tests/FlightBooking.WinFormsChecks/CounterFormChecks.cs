using System.Collections;
using System.Reflection;
using FlightBooking.BUS;
using FlightBooking.DTO;
using FlightBooking.WinForms;

internal static class CounterFormChecks
{
    private static T Doc<T>(object o, string ten) => (T)o.GetType().GetField(ten, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(o)!;
    public static void Run(Action<bool,string> check)
    {
        // Do not show/load this form: these interactions must never open SQL.
        using var form = new DatVeTaiQuayForm();
        var soNguoi = Doc<NumericUpDown>(form, "soNguoi");
        var vung = Doc<FlowLayoutPanel>(form, "hanhKhach");
        var ten = Doc<List<TextBox>>(form, "hoTen");
        var giay = Doc<List<TextBox>>(form, "giayTo");
        ten[0].Text = "Hành khách riêng 1"; giay[0].Text = "DOC-1";
        foreach (int n in new[] { 1, 2, 3, 1, 3 })
        {
            soNguoi.Value = n;
            check(vung.Controls.Count == n, "counter shows exactly " + n + " passenger blocks");
            check(ten[0].Text == "Hành khách riêng 1" && giay[0].Text == "DOC-1", "counter retains first passenger after count change");
        }
        ten[1].Text = "Hành khách 2"; giay[1].Text = "DOC-2";
        check(ten[0].Text != ten[1].Text && giay[0].Text != giay[1].Text, "counter passenger identities are independent");
        using var loc = new BoLocNangCao(["Giá|GiaVe", "Trạng thái|TrangThai"]);
        var bat = Doc<CheckBox[]>(loc,"dung"); var truong = Doc<ComboBox[]>(loc,"truong");
        var giaTri = Doc<TextBox[]>(loc,"giaTri"); var phep = Doc<ComboBox[]>(loc,"phep"); var noi = Doc<ComboBox>(loc,"noi");
        check(loc.DocDieuKien().Count == 0, "disabled advanced conditions do not filter");
        bat[0].Checked = true; giaTri[0].Text = "1.000.000"; phep[0].SelectedItem = ">=";
        bat[1].Checked = true; truong[1].SelectedIndex = 1; giaTri[1].Text = "ISSUED";
        var rows = new ArrayList { new VeMayBayDTO { GiaVe=2000000,TrangThai="ISSUED" },new VeMayBayDTO{GiaVe=500000,TrangThai="ISSUED"},new VeMayBayDTO{GiaVe=2000000,TrangThai="CANCELLED"} };
        foreach (var pair in new[] { ("AND",1), ("OR",3), ("NOT",1) })
        {
            noi.SelectedItem = pair.Item1;
            check(new VeMayBayBUS().LocDanhSach(rows,loc.DocDieuKien()).Count == pair.Item2,"advanced UI delegates " + pair.Item1 + " to BUS correctly");
        }
        giaTri[0].Text = " "; bool rejected = false;
        try { loc.DocDieuKien(); } catch (ArgumentException) { rejected = true; }
        check(rejected,"enabled advanced condition requires a value");
    }
}
