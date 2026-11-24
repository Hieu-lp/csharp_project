using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

int checks=0;
void Check(bool yes,string label){if(!yes)throw new Exception("FAIL: "+label);checks++;Console.WriteLine("PASS: "+label);}
void Reject(Action action,string label){try{action();}catch(LoiQuanLy){Check(true,label);return;}throw new Exception("FAIL: "+label);}
Check(QuanLyBUS.KiemTraSanBay(new(0," Airport "," City ")).TenSanBay=="Airport","trim airport");
Reject(()=>QuanLyBUS.KiemTraSanBay(new(0," ","City")),"empty airport");
Reject(()=>QuanLyBUS.KiemTraSanBay(new(0,new string('x',151),"City")),"airport maximum length");
var edit=new ThongTinSuaChuyenBay(0,1,2,120,new(2026,12,31),new(23,0),new(2027,1,1),new(1,0),"SCHEDULED");
QuanLyBUS.KiemTraChuyenBay(edit);Check(true,"overnight across year");
QuanLyBUS.KiemTraChuyenBay(edit with{NgayKhoiHanh=new(2020,1,1),NgayDen=new(2020,1,2)});Check(true,"historical flight allowed");
Reject(()=>QuanLyBUS.KiemTraChuyenBay(edit with{MaSanBayDen=1}),"same airport");
Reject(()=>QuanLyBUS.KiemTraChuyenBay(edit with{TongSoGhe=0}),"nonpositive seats");
Reject(()=>QuanLyBUS.KiemTraChuyenBay(edit with{NgayDen=edit.NgayKhoiHanh,GioDen=edit.GioKhoiHanh}),"arrival equal departure");
Reject(()=>QuanLyBUS.KiemTraChuyenBay(edit with{TrangThai="PENDING"}),"unknown status");
var flight=new ChuyenBayDTO(1,1,2,"A","B","Airport A","Airport B",edit.NgayKhoiHanh,edit.GioKhoiHanh,edit.NgayDen,edit.GioDen,120,119,"SCHEDULED");
var old=new QuanLyChuyenBayDTO(flight,1,100);
var modifying=edit with{MaChuyenBay=1};
QuanLyBUS.KiemTraThayDoiChuyenBay(modifying with{TongSoGhe=130},old,true,["20F"]);Check(true,"increase seats preserves allocated positions");
Reject(()=>QuanLyBUS.KiemTraThayDoiChuyenBay(modifying with{TongSoGhe=100},old,true,["20F"]),"cannot remove occupied high seat even with only one ticket");
Reject(()=>QuanLyBUS.KiemTraThayDoiChuyenBay(modifying with{TongSoGhe=100},old,true,["CUSTOM-SEAT"]),"unknown seat mapping prevents shrink");
Reject(()=>QuanLyBUS.KiemTraThayDoiChuyenBay(modifying with{TrangThai="CANCELLED"},old,true,["1A"]),"cannot cancel booked flight");
Reject(()=>QuanLyBUS.KiemTraThayDoiChuyenBay(modifying with{NgayKhoiHanh=edit.NgayKhoiHanh.AddDays(1)},old,true,["1A"]),"cannot reschedule booked flight");
Reject(()=>QuanLyBUS.KiemTraThayDoiChuyenBay(modifying,old,true,[]),"sold count must equal active tickets");
Check(QuanLyBUS.KiemTraHangVe(new(0,1," Economy ",1000,7)).TenHangVe=="Economy","trim fare");
Reject(()=>QuanLyBUS.KiemTraHangVe(new(0,1,"Economy",-1,7)),"negative price");
Reject(()=>QuanLyBUS.KiemTraHangVe(new(0,1,"Economy",1.001m,7)),"price precision");
Reject(()=>QuanLyBUS.KiemTraHangVe(new(0,1,"Economy",1,-1)),"negative baggage");
var rows=new ArrayList{new SanBayDTO(1,"Noi Bai","Ha Noi"),new SanBayDTO(2,"Da Nang","Da Nang")};
Check(TimKiemQuanLy.LocDanhSach(rows,[new("Tên","chứa","noi"),new("Mã",">=","1")]).Count==1,"ArrayList AND");
Check(TimKiemQuanLy.LocDanhSach(rows,[new("Tên","chứa","noi"),new("Mã","=","2")],"OR").Count==2,"ArrayList OR");
Check(TimKiemQuanLy.LocDanhSach(rows,[new("Mã","=","1")],daoKetQua:true).Count==1,"ArrayList NOT");
foreach(var (op,count) in new[]{("=",1),("<>",1),(">",1),(">=",2),("<",0),("<=",1)})Check(TimKiemQuanLy.LocDanhSach(rows,[new("Mã",op,"1")]).Count==count,"comparison "+op);
Check(TimKiemQuanLy.LocDanhSach(new(){new QuanLyChuyenBayDTO(flight,1,100)},[new("Ngày đến","=","01/01/2027")]).Count==1,"date predicate");
Check(TimKiemQuanLy.LocDanhSach(new(){new HangVeDTO(1,1,"Eco",1000000,7)},[new("Giá",">=","1.000.000")]).Count==1,"Vietnamese money predicate");
Reject(()=>TimKiemQuanLy.LocDanhSach(rows,[new("Mã","=","not a number")]),"invalid filter number");
var statusRows = new ArrayList
{
    new QuanLyChuyenBayDTO(flight, 1, 100),
    new QuanLyChuyenBayDTO(flight with { MaChuyenBay = 2, TrangThai = "COMPLETED" }, 1, 100),
    new QuanLyChuyenBayDTO(flight with { MaChuyenBay = 3, TrangThai = "CANCELLED" }, 1, 100)
};
foreach (var (label, id) in new[] { ("Mở bán", 1), ("Đã hoàn thành", 2), ("Đã hủy", 3), ("SCHEDULED", 1) })
{
    var result = TimKiemQuanLy.LocDanhSach(statusRows, [new("Trạng thái", "=", label)]);
    Check(result.Count == 1 && ((QuanLyChuyenBayDTO)result[0]!).ChuyenBay.MaChuyenBay == id, "Vietnamese/SQL status " + label);
}
Check(TimKiemQuanLy.LocDanhSach(statusRows, [new("Trạng thái", "chứa", "hoàn thành")]).Count == 1, "status contains displayed text");
Check(TimKiemQuanLy.LocDanhSach(statusRows, [new("Trạng thái", "<>", "Đã hủy"), new("Mã", ">=", "2")]).Count == 1, "status and numeric AND");
Check(TimKiemQuanLy.LocDanhSach(statusRows, [new("Trạng thái", "=", "Mở bán"), new("Mã", "=", "3")], "OR", true).Count == 1, "NOT over OR status conditions");
foreach (var invalid in new[] { "1.5", "1.00.000", "1..000", "1,2,3", "1e3" })
    Reject(() => TimKiemQuanLy.LocDanhSach(rows, [new("Mã", "=", invalid)]), "reject malformed Vietnamese number " + invalid);
Check(TimKiemQuanLy.LocDanhSach(rows, [new("Mã", ">", "1,5")]).Count == 1, "Vietnamese decimal comparison");
Check(TimKiemQuanLy.LocDanhSach(rows, [new("Mã", ">", "-1")]).Count == 2, "negative numeric threshold");
Check(TimKiemQuanLy.LocDanhSach(rows, [new("Mã", ">", "1"), new("Mã", "<", "3")]).Count == 1, "range on same field");
Check(TimKiemQuanLy.LocDanhSach(rows, [], daoKetQua: true).Count == 2, "empty filters keep all rows with NOT");
Reject(() => TimKiemQuanLy.LocDanhSach(rows, [new("Mã", "=", "999"), new("Mã", "=", "invalid")]), "AND still validates later conditions after a false match");
Reject(() => TimKiemQuanLy.LocDanhSach(rows, [new("Mã", "=", "1"), new("Mã", "=", "invalid")], "OR"), "OR still validates later conditions after a true match");
Reject(() => TimKiemQuanLy.LocDanhSach(rows, [new("Unknown", "=", "1")]), "unknown field rejected");
Reject(() => TimKiemQuanLy.LocDanhSach(rows, [], "XOR"), "unsupported logical join rejected");
var unconfigured = new QuanLyBUS("");
Reject(() => unconfigured.DocDSSanBay(), "airport read translates missing connection into management error");
Reject(() => unconfigured.DocDSChuyenBay(), "flight read translates missing connection into management error");
Reject(() => unconfigured.DocDSHangVe(), "fare read translates missing connection into management error");
var editor = new FlightBooking.WinForms.PhienChinhSua();
editor.BatDau(FlightBooking.WinForms.CheDoChinhSua.Xem,"saved");
Check(!editor.CoThayDoi("changed")&&!editor.CoTheLuu("changed"),"view mode never edits or saves");
editor.BatDau(FlightBooking.WinForms.CheDoChinhSua.Them,"defaults");
Check(!editor.CoThayDoi("defaults")&&editor.CoTheLuu("defaults"),"opening new form alone is clean but save validates defaults");
Check(editor.CoThayDoi("typed"),"new typed data requires discard confirmation");
editor.BatDau(FlightBooking.WinForms.CheDoChinhSua.Sua,"saved");
Check(!editor.CoTheLuu("saved")&&editor.CoTheLuu("changed"),"edit save enabled only for actual changes");
Check(!editor.CoThayDoi("saved"),"undoing typing does not prompt to discard");
editor.BatDau(FlightBooking.WinForms.CheDoChinhSua.Xem,"committed");
Check(!editor.CoThayDoi("committed")&&!editor.CoTheLuu("committed"),"successful save or cancel returns clean view");
ExceptionChecks.Run(Check);
CrudChecks.Run(Check);
FlightBooking.Testing.SqlTestPolicyChecks.Run(Check);
if(args.Contains("--sql"))SqlManagementChecks.Run(Check,Reject);
Console.WriteLine($"{checks} checks passed.");
