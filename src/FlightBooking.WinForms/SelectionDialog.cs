namespace FlightBooking.WinForms;

public sealed class SelectionDialog : Form
{
    public static object? SelectedValue {get;private set;}
    public SelectionDialog(string title,List<object> source,Func<object,string> label)
    {
        SelectedValue=null;Text=title;Size=new(760,480);MinimumSize=new(550,350);StartPosition=FormStartPosition.CenterParent;Font=new("Segoe UI",11);
        var list=Ui.List();list.Columns.Add("Chọn dữ liệu",680);
        var search=new TextBox{Dock=DockStyle.Top,PlaceholderText="Nhập từ khóa để tìm…"};
        void Render(){list.Items.Clear();foreach(var item in source.Where(o=>label(o).Contains(search.Text.Trim(),StringComparison.OrdinalIgnoreCase)))list.Items.Add(new ListViewItem(label(item)){Tag=item});}
        void Choose(){if(list.SelectedItems.Count==0)return;SelectedValue=list.SelectedItems[0].Tag;DialogResult=DialogResult.OK;Close();}
        var choose=Ui.Button("Chọn",()=>{Choose();return Task.CompletedTask;});var cancel=new Button{Text="Hủy",AutoSize=true,DialogResult=DialogResult.Cancel};
        var actions=Ui.Row();actions.Dock=DockStyle.Bottom;actions.Controls.AddRange([choose,cancel]);Controls.Add(list);Controls.Add(search);Controls.Add(actions);CancelButton=cancel;AcceptButton=choose;
        list.DoubleClick+=(_,_)=>Choose();search.TextChanged+=(_,_)=>Render();FormClosed+=(_,_)=>{if(DialogResult!=DialogResult.OK)SelectedValue=null;};Render();
    }
}
