# Đối chiếu W7 và QLSV3Lop — tiếp nối rà soát 23/11/2026

Báo cáo này tiếp tục từ source đang sửa tại `feature/flight-management`, HEAD
`9b20abc`, không bắt đầu lại dự án. [Báo cáo trước](RaSoat-BookingFlight-2026-11-23.md)
là kết quả tại thời điểm chưa nhận được tài liệu gốc; phần đối chiếu tài liệu dưới
đây thay thế các nhận xét “chưa có PDF/mẫu” của báo cáo đó.

## 1. Tài liệu đã đọc và yêu cầu đã đáp ứng

Đã đọc trực tiếp [W7](../../Downloads/C-w7-YeuCauDoAn.pdf) **7 trang**, cả hình
minh họa main-sub/menu/form con; [tài liệu 3 lớp](../../c-3-layer-.pdf) **32 trang**,
gồm các trang ảnh code DAO/BUS/GUI. Đã giải nén source từ
[QLSV3Lop.rar](../../QLSV3Lop.rar) vào [thư mục đối chiếu tạm](../tmp/teacher-reference-20261123/QLSV3Lop/)
và đọc GUI, Designer, BUS, DAO, kết nối, DTO, Program và hướng dẫn ôn từng hàm.
Không chạy chương trình mẫu hoặc script database SVDB. Bản giải nén và công cụ
đọc PDF nằm trong tmp đã ignore; không thêm dependency vào BookingFlight.

Các khái niệm trạng thái trong bảng:

- **Có code:** đã tìm được đường đi trong source, chưa mặc nhiên đạt thao tác SQL.
- **Đã thử:** có kết quả chạy mới; ghi rõ C#, GUI, HTTP hoặc SQL chỉ đọc.
- **Chưa thử ghi:** chưa chạy CRUD/đặt/hủy trên database test được phép.
- **Chưa triển khai/một phần:** chưa thấy chức năng đầy đủ trên nhánh này.

| Mục W7, trang | Yêu cầu gốc | File/hàm thực hiện | Hiện trạng và bằng chứng |
|---|---|---|---|
| 1, tr.1 | 3-Layer ver2; class MyConnection | [MyConnection.Open](../src/FlightBooking.DAO/MyConnection.cs), [ManagementDAO.Read/Write](../src/FlightBooking.DAO/ManagementDAO.cs), [BookingDAO.Read/Write](../src/FlightBooking.DAO/BookingDAO.cs) | Có code, build và đọc SQL qua GUI đã thử. GUI không mở SqlConnection; BUS gọi DAO; DTO truyền dữ liệu. |
| 2, tr.1 | Tìm trên ArrayList trong BUS | [ManagementSearch.Filter/Match](../src/FlightBooking.BUS/ManagementBUS.cs), [BookingBUS.Filter](../src/FlightBooking.BUS/BookingBUS.cs) | Đã thử C#. DAO đọc dữ liệu, BUS duyệt ArrayList, GUI render kết quả. Không chuyển tìm nâng cao sang WHERE chỉ để giống mẫu. |
| 3, tr.1–2 | Main-sub quan hệ 1-n, hai ListView liên kết | [SelectionChanged/ResetView/UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs) | Đã thử GUI chỉ đọc qua 21 chuyến: chỉ hiện hạng cùng FlightId. Đây là FLIGHT–FARE_CLASS; Invoice–InvoiceDetail riêng của Quân chưa xác nhận. |
| 4, tr.3 | Nút chọn tìm dữ liệu, lấy kết quả vào form qua static | [SelectionDialog.SelectedValue](../src/FlightBooking.WinForms/SelectionDialog.cs), [PickAirport/PickFlight](../src/FlightBooking.WinForms/ManagementPage.cs) | Đã thử chọn/Hủy/X; có tìm từ khóa, Enter/CancelButton. Kiểm tra đường xử lý phím Esc được bổ sung, kết quả tại mục 5. Static chỉ dùng picker WinForms, không chia sẻ khách hàng giữa các phiên Web. |
| 5, tr.4 | Tính duy nhất dữ liệu | [ManagementBUS.SaveAirport/SaveFare](../src/FlightBooking.BUS/ManagementBUS.cs), [schema/index](../Database/02_CreateTables.sql) | Có code; PK IDENTITY, cặp tên/thành phố sân bay, UNIQUE tên hạng trong chuyến. FK/unique index đã xác minh chỉ đọc. Chưa thử hai giao dịch ghi trùng trong đợt này. Không tự thêm quy tắc “trùng chuyến” chưa được nhóm quy định. |
| 6, tr.4 | Field số lượng biến động theo nghiệp vụ | [ReserveSeats/Cancel](../src/FlightBooking.DAO/BookingDAO.cs), [ValidateFlightChange](../src/FlightBooking.BUS/ManagementBUS.cs) | Có transaction/khóa, trừ/hoàn ghế và kiểm tra ghế hiệu lực. C# đạt, SELECT hiện trạng khớp; chưa thử đặt/hủy SQL hoặc đồng thời với WinForms. |
| 7, tr.4 | Có field không được nhập | [BuildEditor/UpdateButtons](../src/FlightBooking.WinForms/ManagementPage.cs) | ID, vé đã bán, ghế còn, doanh thu ReadOnly, đã thử GUI. Tổng tiền Web được tính/đối chiếu ở server, không tin hidden ExpectedTotal. |
| 8, tr.4 | AND/OR/NOT, >, >=, <, <=, <> trên một/nhiều field | [ManagementSearch.Filter/Match](../src/FlightBooking.BUS/ManagementBUS.cs) | Đã thử trong 53 ManagementChecks: phép so sánh, cùng/khác trường, NOT trên nhóm OR, ngày, số Việt và trạng thái Việt. Có thêm phép bằng/chứa. |
| 9, tr.4 | Table tham số | [schema hiện tại](../Database/02_CreateTables.sql) | Chưa triển khai bảng tham số nghiệp vụ. Biến cấu hình kết nối không thay thế tiêu chí này. Nhóm cần phân công/chốt với cô; không tự thêm schema. |
| 10, tr.4–5 | Thống kê tiền theo thời gian, tháng/quý/năm; thu/chi/lợi nhuận; bảng theo nhân viên/khách/sản phẩm | [phân công bàn giao](Thinh-Booking.md) | Chưa thấy module đầy đủ trên nhánh này. Quân phụ trách theo bàn giao/attachment. Field total_revenue chỉ đọc không thay thế các bảng thống kê. Chưa có dữ liệu phiếu nhập/chi phí để suy ra tổng chi/lợi nhuận; nhóm cần hỏi cách áp dụng cho đề tài vé bay. |
| 11, tr.5 | Export/import file xls (1 điểm) | [phân công bàn giao](Thinh-Booking.md) | Chưa thấy module đầy đủ; thuộc Quân, không sửa thay. PDF ghi xls; không tự coi chức năng CSV hoặc nhãn “Excel” là đã đạt. |
| 12, tr.5 | Report ra PDF (1 điểm) | [phân công bàn giao](Thinh-Booking.md) | Chưa thấy chức năng xuất hóa đơn/report PDF đầy đủ; thuộc Quân. Báo cáo Markdown này không thay chức năng Report PDF. |
| 13, tr.5–6 | Desktop chính/menu trái/form con | [Form1](../src/FlightBooking.WinForms/Form1.cs) | Một phần: desktop thật có ba tab/UserControl. Chưa có shell menu trái như hình W7 và chưa xác nhận MainForm chung. Phú/nhóm tích hợp; không thay giao diện chung trong đợt này. |
| 14, tr.6–7 | Báo cáo cuối kỳ | [Docs](./) | Chưa xác nhận báo cáo toàn nhóm: bìa, đề tài/lớp/nhóm/GV, dòng Tháng 12/2026; nội dung I–V, ảnh CSDL/source/form, công thức, vào/ra từng hàm, cài đặt, link sản phẩm; <=20 trang, Times New Roman 13, không mục lục. Nhóm cùng làm; tài liệu bàn giao chưa thay thế báo cáo cuối kỳ. |
| 15, tr.7 | Lưu tiếng Việt, ví dụ nối chuỗi UTF-8 | [MyConnection](../src/FlightBooking.DAO/MyConnection.cs), [schema NVARCHAR](../Database/02_CreateTables.sql) | SQL Server dùng NVARCHAR và tham số SqlClient. Không nối `?useUnicode=yes&characterEncoding=UTF-8`: đây là cú pháp MySQL trong ví dụ, không hợp lệ với SqlClient. Giữ theo ràng buộc SQL Server của nhiệm vụ. |
| 16, tr.7 | Timeline kiểm tra/chấm | Tài liệu học ở mục 8 bên dưới | Tuần 9–10 kiểm tra giấy 10 phút, viết 1–2 hàm; tuần 11–12 chấm đồ án/bốc thăm. Đây là yêu cầu tổ chức học phần, không phải chức năng phải thêm vào app. |

DTO không phải mục độc lập trong W7 nhưng được mô tả rõ tại tài liệu 3 lớp tr.5–8.
[BookingModels](../src/FlightBooking.DTO/BookingModels.cs) và [ManagementModels](../src/FlightBooking.DTO/ManagementModels.cs)
đáp ứng truyền dữ liệu giữa các lớp, không chứa truy vấn hoặc control WinForms.

## 2. Những phần còn thiếu hoặc chưa nghiệm thu

- Chưa có database test riêng được xác minh/cho phép. Server cục bộ chỉ thấy
  FlightBookingDB; database chính thiếu BOOKING_WEB_REQUEST. Không chạy SQL ghi,
  không tạo database/schema, không chạy script 08 hoặc seed.
- CRUD GUI AIRPORT/FLIGHT/FARE_CLASS thành công, xóa thật, rollback, gửi lặp,
  tranh chấp ghế và đồng bộ sau đặt/hủy Web vẫn **chưa nghiệm thu**.
- Độ phân giải/DPI phải ghi theo thiết bị thực tế; việc đặt kích thước cửa sổ
  không tương đương đổi độ phân giải màn hình hoặc DPI của Windows. Các tổ hợp
  100%/125%/150% chưa chạy được sẽ giữ trong checklist thủ công.
- Table tham số, thống kê, xls, report PDF, menu desktop chung và báo cáo cuối kỳ
  chưa đủ bằng chứng. Chỉ ghi nhận trách nhiệm; không sửa phần Phú/Quân.
- Chưa có file phân công nhóm gốc hoặc W4 trong tài liệu mới. Trách nhiệm ở đây
  dựa trên yêu cầu người dùng và bàn giao hiện tại, không coi W7 là file phân công.

## 3. So sánh trực tiếp với mẫu QLSV3Lop

| Khía cạnh | Mẫu đã đọc | BookingFlight và quyết định giữ nguyên |
|---|---|---|
| GUI/BUS/DAO/DTO | Một project, thư mục GUI/BUS/DAO/DTO; [SinhVienFRM](../tmp/teacher-reference-20261123/QLSV3Lop/GUI/SinhVienFRM.cs) gọi SinhVienBUS rồi DAO | Nhiều project cùng solution; Web là GUI HTTP. Cùng hướng phụ thuộc, không gộp lớp hoặc di chuyển SQL vào GUI. |
| Thêm/sửa/xóa | [SinhVienBUS.Them/Sua/Xoa](../tmp/teacher-reference-20261123/QLSV3Lop/BUS/SinhVienBUS.cs) và DAO có hàm riêng; GUI tự đổ lại ListView trong từng Click | SaveAirport/SaveFlight/SaveFare phân nhánh theo ID=0 hoặc ID>0, Delete riêng. Giữ API vì đang hoạt động và có snapshot chống sửa đè; không tách lại chỉ để giống cách đặt tên mẫu. |
| Đọc danh sách | [SinhVienDAO.DocDSSV](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SinhVienDAO.cs): Open → SqlCommand → ExecuteReader → while → DTO → ArrayList | ManagementData đọc List<DTO>, BUS bọc ArrayList trước khi lọc. Kết quả tìm thực sự là ArrayList trong BUS, đáp ứng W7; không cần đổi mọi collection thành ArrayList. |
| ArrayList ở BUS | MaDaTonTai/TimTheoMa/TimTheoHo dùng foreach trên dao.DocDSSV() | ManagementSearch.Filter dùng foreach; BookingBUS.Filter cũng dùng ArrayList. Không dùng static cache cho ghế/giá vì các app cùng thay đổi SQL. |
| Tìm nâng cao | SinhVienBUS.TimKiemNangCao kiểm tra điều kiện rồi gọi SQL WHERE tại DAO | **Giữ khác biệt theo W7 mục 2/8:** lọc nâng cao danh mục trong BUS. Không sao chép phần SQL-filter của mẫu. |
| ListView | GUI dùng Items.Add/SubItems.Add và Tag=DTO; SelectedIndexChanged điền control | Render/Cells tạo ListViewItem, Tag giữ DTO; SelectionChanged/Fill điền control. Thêm ListView con theo FlightId. Cùng cơ chế dữ liệu, có helper vì ba danh mục. |
| Kết nối/command | [SqlServerConnect.TaoKetNoi](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SqlServerConnect.cs), mỗi DAO tự mở connection/SqlCommand và tham số có kiểu | MyConnection đúng tên W7; ManagementDAO/BookingDAO giữ một connection/transaction cho cả nghiệp vụ. Không mở lại connection riêng mỗi bước đặt vé, vì sẽ làm mất tính nguyên tử. |
| Trùng/hợp lệ | BUS trim/if, MaDaTonTai duyệt ArrayList; DAO trả số dòng bị tác động | BUS validate/ra quyết định; DAO kiểm tra tồn tại/trùng bằng tham số trong SERIALIZABLE. UNIQUE/FK/khóa chống hai người đồng thời thêm trùng. Không thay bằng kiểm tra cache đơn thuần. |
| Sự kiện/nút | Designer gắn btnThem_Click/btnSua_Click; hàm lặp đoạn render để dễ viết trên giấy | Ui.Button gắn delegate gọi Save/Delete/Reload; Run xử lý bận/lỗi, EditorSession theo dõi thay đổi. Giữ giao diện phản hồi khi SQL chạy nền và không mất nội dung khi lỗi. |
| DTO | Class mutable với property get/set | Record DTO có so sánh theo giá trị phục vụ snapshot chống sửa đè; BookingRequest/input là class. Không đổi record thành class chỉ để giống mẫu vì sẽ thay đổi ý nghĩa so sánh. |

Archive mới là bản ôn .NET 10 + SQL Server và **đã có SQL tham số hóa**. Tài liệu
3 lớp tr.9–17 và tr.30–32 có ví dụ MySQL/SQL nối chuỗi/MessageBox trong DAO;
đó là minh họa cũ, không sao chép những chi tiết này. BookingFlight tiếp tục
Microsoft.Data.SqlClient, `using`, tham số hóa và chỉ hiển thị lỗi tại GUI.

Hướng dẫn mẫu chủ động lặp code để học viết hàm độc lập. BookingFlight lớn hơn;
những helper đang bảo vệ transaction, validation và editor không bị bỏ đi để đạt
sự giống nhau về hình thức. Phần giải thích từng hàm ở mục 8 giúp học đúng luồng.

## 4. File và phương thức sửa trong lần tiếp nối

Ba sửa lỗi trước được giữ: [ManagementSearch.Match](../src/FlightBooking.BUS/ManagementBUS.cs)
vẫn ánh xạ trạng thái Việt và kiểm tra số phân nhóm; [ShowFares](../src/FlightBooking.WinForms/ManagementPage.cs)
vẫn chờ tải ban đầu, tải mới qua BUS và chọn đúng chuyến. Không checkout/reset/
commit/push/merge. Toàn bộ thay đổi chưa commit trước lượt này được bảo toàn.

Thay đổi giao diện mới chỉ ở [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs):

1. Lỗi “Đã có sân bay cùng tên và thành phố” nay gắn cả name/city. Trước đó thông
   báo rơi vào nhánh sân bay đi/đến, vốn không tồn tại ở tab AIRPORT. Test GUI mới
   đã FAIL ở `duplicate airport error marks name and city` trước sửa. Để không
   INSERT lên DB chính, test đưa đúng thông báo từ BUS vào ErrorProvider trên control thật.
2. FARE_CLASS chưa chọn chuyến trả “Chọn chuyến bay hợp lệ”; nay gắn lỗi vào ComboBox
   flight, thay vì chỉ nhận đúng câu “Chuyến bay không tồn tại”. Test dùng BUS với
   chuỗi kết nối rỗng, nên validation chạy trước SQL và không thể ghi dữ liệu chính.
3. Vùng chi tiết có thể bị co hẹp trong lúc tab được dựng ở kích thước nhỏ rồi giữ
   chiều rộng sai. Bố cục cửa sổ 1366×768 tại DPI 96 đã tái hiện nút Lưu ở x=1376,
   Hủy ở x=1476, ngoài trang có cạnh phải x=1404. Đã bổ sung xử lý SizeChanged của
   SplitContainer: khi có đủ chỗ và panel chi tiết bị co, khôi phục vùng khoảng
   430 px theo bố cục sẵn có. Giữ màu/font, editor, BUS/DAO và thao tác chia pane.
   Xem [ảnh trước sửa](../tmp/teacher-reference-20261123/evidence/before-Airport-1366x768-dpi96.png)
   và [ảnh sau sửa](../tmp/teacher-reference-20261123/evidence/Airport-1366x768-dpi96-advancedFalse.png).

[WinFormsChecks/Program](../tests/FlightBooking.WinFormsChecks/Program.cs) giữ 35
kiểm tra cũ, thêm chế độ tùy chọn `--interaction` và `--layout`; chỉ in tổng PASS
khi không có lỗi. [InteractionChecks](../tests/FlightBooking.WinFormsChecks/InteractionChecks.cs)
kiểm tra ErrorProvider, giữ nội dung, cảnh báo và phím Esc; [LayoutChecks](../tests/FlightBooking.WinFormsChecks/LayoutChecks.cs)
kiểm tra bố cục và lưu ảnh local. Reflection/PInvoke nằm trong test, không thêm vào
code ứng dụng. [Preflight SQL](../tests/FlightBooking.Checks/preflight-readonly.sql)
chỉ SELECT metadata/tính nhất quán, không tự chuẩn bị schema hoặc fixture.

## 5. Kết quả kiểm thử thực tế

Build và bộ C# sau sửa ErrorProvider: **0 lỗi, 0 cảnh báo; 53 ManagementChecks,
82 BookingChecks đạt**. Kết quả này cũng đã được chạy lại sau sửa bố cục.
Hai script JavaScript đạt; **17/17 HTTP checks chỉ đọc đạt**, script cũ không bị sửa.

Đã mở trình duyệt thật, chọn số khách 2 và bấm chuyến giá tốt lấy từ dữ liệu hiện
tại; Checkout có một người đặt riêng và hai hành khách, tổng 1.800.000đ cho lựa
chọn được đọc lúc thử. Nhập tên giả và mã không tồn tại, bấm tính lại giá: lỗi Việt
hiện rõ, giữ ba tên và hai biểu mẫu. Đo DOM tại viewport 1366 và 390 px không tràn
ngang. Không bấm Xác nhận đặt vé; không nghiệm thu đặt/hủy SQL hoặc đầy đủ khứ hồi.

Thiết bị GUI hiện tại: màn hình **1366×768, DeviceDpi=96 (100%)**. Các yêu cầu
1920×1080 bị Windows giới hạn kích thước cửa sổ; runner kiểm tra kích thước thực
và ghi SKIP. **Chưa thử 1920×1080 hoặc DPI 125%/150%**; không đổi thiết lập Windows.
Lượt xác nhận cuối `--interaction --layout`: **74/74 GUI checks đạt**, gồm 35
kiểm tra cũ + 21 kiểm tra tương tác/validation/main form + 18 kiểm tra bố cục tại
1366×768/96 DPI. Ba tab đều thử khi bộ lọc nâng cao đóng/mở; Lưu/Hủy nằm ngoài
scroll, trong trang và mọi vùng chứa, chữ nút vừa chiều rộng. Các cảnh báo đổi
dòng/tab/tải lại/đóng, No giữ dữ liệu, Yes bỏ sửa, form sạch không hỏi, và đường xử
lý Esc của Form đều chạy trên control thật. Esc được đưa tới ProcessDialogKey của
chính picker, không gửi bàn phím toàn desktop. MainForm thực tế cũng nằm gọn trong
working area và hiển thị được Lưu/Hủy; xem [ảnh main form](../tmp/teacher-reference-20261123/evidence/main-actual-display-dpi96.png)
và [bố cục có bộ lọc](../tmp/teacher-reference-20261123/evidence/Flight-1366x768-dpi96-advancedTrue.png).

| Lệnh đã chạy | Kết quả mới |
|---|---|
| `dotnet build FlightBookingSystem.slnx` | 8 project, 0 lỗi, 0 cảnh báo |
| `dotnet run --no-build --project tests/FlightBooking.ManagementChecks` | 53/53 đạt |
| `dotnet run --no-build --project tests/FlightBooking.Checks` | 82/82 đạt; SQL integration được bỏ qua |
| `dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout` | 74/74 đạt; yêu cầu 1920×1080 ghi SKIP theo kích thước thực |
| `& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl http://127.0.0.1:5087` | 17/17 đạt, không gửi book/cancel |
| `node tests/FlightBooking.Checks/checkout.checks.cjs` và `search-form.checks.cjs` | Cả hai script đạt |
| `sqlcmd ... -i tests/FlightBooking.Checks/preflight-readonly.sql` | Chạy thành công, chỉ đọc; xác định DB hiện tại chưa đủ điều kiện test ghi |

Các lượt thất bại khi thêm test đã được dùng để chẩn đoán: ErrorProvider và vùng
chi tiết được sửa ở app; kiểm tra tab đổi từ gán SelectedIndex sang click native
trên TabControl, phù hợp đường tương tác người dùng. Không sửa logic CanLeave
hoặc Form1 để ép test qua. Ảnh/layout chỉ được ghi sau khi control đã dựng và
layout ổn định. Không có cảnh báo NuGet trong lượt build cuối; không lấy NU1900
trong bàn giao cũ làm kết quả mới.

Preflight chỉ đọc tại `./FlightBookingDB` xác nhận:

- Có 11 bảng gốc; không có BOOKING_WEB_REQUEST, cả ba cột cần dùng trong bảng này
  đều chưa có. Tài khoản có quyền VIEW DEFINITION nên không phải bị che metadata.
- FK FLIGHT/AIRPORT, FARE_CLASS/FLIGHT, TICKET/FLIGHT/FARE_CLASS,
  BOOKING_DETAIL/BOOKING/TICKET đều bật và trusted; UX_TICKET_ActiveSeat là index
  unique có lọc ISSUED và không bị vô hiệu hóa.
- 0 chuyến lệch bộ đếm ghế/vé ISSUED; 0 Booking lệch số chi tiết; 0 chi tiết có vé
  thuộc chuyến khác Booking. Đây là kiểm tra **hiện trạng**, không phải thử đặt/hủy.
- Trước/sau kiểm tra: cùng 8 AIRPORT, 21 FLIGHT, 41 FARE_CLASS, 81 BOOKING,
  141 TICKET. Đây là đối chiếu số dòng và các bất biến đã nêu, không phải thử ghi.
- Không chạy `--sql` hoặc `--demo-rollback`, không xác nhận đặt/hủy trên database chính.
- Website tại cổng 5087 đã dừng sau kiểm tra. Git vẫn ở `feature/flight-management`,
  HEAD `9b20abc`; không commit/push/merge. Hash của ManagementBUS, hai bộ C# 53/82
  và HTTP runner 17 kiểm tra không đổi so với đầu lượt tiếp nối.

## 6. Phần chưa thử và phương án môi trường SQL an toàn

Có quyền kỹ thuật tạo database không đồng nghĩa được phép tạo trong nhiệm vụ.
Đã hỏi database test/tên server/phạm vi cho phép; khi chưa được cung cấp/xác nhận,
chỉ kiểm tra đọc và không tự provisioning.

Phương án đề xuất để nhóm/người phụ trách database chuẩn bị:

1. Chuẩn bị database độc lập, dữ liệu giả, login/quyền giới hạn. Bộ quản lý hiện
   yêu cầu tên kết thúc `_ManagementTests`; bộ Booking yêu cầu `_BookingTests`.
   Có thể dùng hai database riêng; tích hợp Web–WinForms cùng trỏ database Booking test.
2. Người phụ trách duyệt và chuẩn bị schema/index/phụ thuộc BOOKING_WEB_REQUEST
   **trong database test**, không áp lên FlightBookingDB chính. Script 01/02/03 có
   tên database cố định trong nội dung, nên không chạy nguyên xi với ý nghĩ `-d`
   sẽ ngăn chúng chuyển sang database khác. Không dùng seed trên DB đang có dữ liệu.
3. Chạy [preflight-readonly.sql](../tests/FlightBooking.Checks/preflight-readonly.sql):
   DB_NAME đúng đích, hậu tố test, bảng/cột/constraint/index đầy đủ, bộ đếm nhất quán.
   Preflight không thay thế xác nhận quyền sử dụng và không tự sửa khi thiếu thành phần.
4. Hai SQL runner cũ có bước tự áp schema: [SqlManagementChecks](../tests/FlightBooking.ManagementChecks/SqlManagementChecks.cs)
   có thể chạy 02/03 khi DB rỗng; [SqlChecks](../tests/FlightBooking.Checks/SqlChecks.cs)
   có thể chạy 02/03/08. Chúng được giữ nguyên; **không chạy khi chưa cho phép bước
   chuẩn bị schema**. Nếu quy định vẫn cấm tự sửa schema, cần tách/duyệt bước này
   trước khi nghiệm thu SQL, không bỏ qua một cách ngầm định.
5. Ghi nhận baseline của riêng fixture: ID thực tế được sinh, bộ đếm, giá chi tiết,
   doanh thu, lượt mã. Chạy CRUD GUI, đặt hai khách, tải lại WinForms, hủy, hủy lặp,
   gửi lặp, tranh chấp ghế/lượt mã và rollback. Không hardcode ID bàn giao cũ.
6. Xác nhận UNPAID không cộng doanh thu, đổi giá danh mục không đổi giá lịch sử,
   một lỗi giữa transaction không để dữ liệu một phần; chỉ dọn ID do lần test mới tạo.

Chưa có kết quả PASS cho những thao tác ghi/rollback/đồng thời ở trên.

## 7. Hướng dẫn mở app và checklist thủ công

Trong mỗi terminal PowerShell chạy app:

```powershell
Set-Location 'E:\csharp_project'
dotnet build FlightBookingSystem.slnx
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks

# Chỉ xem dữ liệu hiện có trên máy đã kiểm tra; máy khác thay đúng server/database.
$env:ConnectionStrings__FlightBookingDB = 'Server=.;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Terminal WinForms:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Terminal Web đã đặt cùng biến môi trường:

```powershell
dotnet run --no-build --project src/FlightBooking.Web --launch-profile http
```

Mở URL terminal in ra; profile http hiện là http://localhost:5191. Không thực hiện
CRUD/đặt/hủy trên database chính. Khi có DB test đã được cho phép và chuẩn bị,
đổi chuỗi kết nối **cả hai terminal**, rồi khởi động lại app để dùng cùng nguồn SQL.

Chạy kiểm tra chỉ đọc/validation:

```powershell
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout
& 'tests/FlightBooking.Checks/readonly.http.ps1' -BaseUrl 'http://localhost:5191'
sqlcmd -S . -E -C -d FlightBookingDB -f 65001 -b -i tests/FlightBooking.Checks/preflight-readonly.sql
```

Validation trong interaction mode dùng kết nối rỗng; phần main form chỉ chọn/lọc,
không bấm Lưu/Xóa dữ liệu hợp lệ. Layout mode chụp bitmap ở DPI của tiến trình;
không thay DPI/độ phân giải Windows. HTTP runner chỉ gửi quote, không gửi book/cancel;
cần chuyến giá dương còn ít nhất hai ghế trong danh sách hiện tại.

Checklist còn phải nghiệm thu thủ công trên DB test:

| Nhóm thao tác | Kết quả cần quan sát |
|---|---|
| AIRPORT | Thêm/sửa/đọc/xóa bản ghi test tự tạo; trùng tên/thành phố; không xóa sân bay được FLIGHT tham chiếu; lỗi gắn đúng ô |
| FLIGHT | Hai sân bay khác nhau; đến sau đi; tổng ghế đúng; trạng thái đúng; không đổi lịch/tuyến/trạng thái phá Booking; không giảm ghế đã cấp |
| FARE_CLASS | Đúng chuyến, tên trùng trong chuyến bị chặn, giá/hành lý hợp lệ, không xóa/chuyển hạng có vé; không đổi giá lịch sử |
| Editor | Xem/Thêm/Sửa; No giữ nội dung khi đổi dòng/tab/tải lại/đóng; Yes bỏ sửa; lưu lỗi giữ dữ liệu, lưu thành công giữ đúng mã |
| Picker | Tìm, chọn bằng Enter; Esc/Hủy/X không giữ kết quả static cũ; không đổi nhầm bản ghi |
| Hạng cha–con | Chọn qua chuyến có/không có hạng, mở đúng ngữ cảnh, sửa hạng ở tab khác rồi tải lại |
| Bố cục | Thực hiện hai độ phân giải 1366×768 và 1920×1080, mỗi độ phân giải 100%/125%/150%; mở cả bộ lọc nâng cao; chữ không bị cắt, có thanh cuộn, Lưu/Hủy ngoài vùng cuộn |
| Web–WinForms | Đặt hai khách: còn -2/đã dùng +2, invoice UNPAID chưa cộng doanh thu; hủy đủ điều kiện hoàn đúng một lần; tải lại WinForms khớp |

Đổi DPI trong Settings > System > Display > Scale theo quy trình trên máy test,
đóng/mở lại app ở mỗi mức; chụp ảnh và ghi kích thước/DPI thực tế. Không gọi các
trường hợp chưa thực hiện là PASS. Khi NU1301 kiểm tra NuGet/mạng; NU1900 chỉ là
cảnh báo lấy dữ liệu audit. Nếu DLL bị khóa, dừng đúng app do mình mở trước khi build.

## 8. Giải thích tám hàm/luồng để học theo mẫu

| Luồng | Đầu vào → kiểm tra/xử lý → DAO/SQL → đầu ra/lỗi |
|---|---|
| Đọc sân bay | [ManagementPage.ReadAll](../src/FlightBooking.WinForms/ManagementPage.cs) gọi [ManagementBUS.Airports](../src/FlightBooking.BUS/ManagementBUS.cs) → [ManagementDAO.Read/ManagementData.Airports](../src/FlightBooking.DAO/ManagementDAO.cs). MyConnection.Open mở kết nối, SqlCommand SELECT, SqlDataReader while tạo AirportDTO; BUS bọc ArrayList, Render đưa lên ListView. Lỗi kết nối trả ManagementException để GUI hiện thông báo. Tương ứng DocDSSV của mẫu. |
| Thêm sân bay | [ManagementPage.Save](../src/FlightBooking.WinForms/ManagementPage.cs) đọc name/city, AirportDTO ID=0 → [SaveAirport/ValidateAirport](../src/FlightBooking.BUS/ManagementBUS.cs) trim/độ dài/trùng → [Write/AirportDuplicate/SaveAirport](../src/FlightBooking.DAO/ManagementDAO.cs). INSERT tham số, OUTPUT mã mới; commit rồi tải/chọn mã vừa lưu. Lỗi giữ nội dung, MarkErrors gắn đúng trường. Tương ứng btnThem_Click → Them. |
| Sửa chuyến | GUI Save tạo FlightEdit cùng bản gốc → [ManagementBUS.SaveFlight/ValidateFlightChange](../src/FlightBooking.BUS/ManagementBUS.cs) → [Flight(locked)/FlightHasBookings/ActiveSeats/SaveFlight](../src/FlightBooking.DAO/ManagementDAO.cs). Từ chối snapshot cũ, lịch/tuyến không hợp lệ, sửa phá vé hoặc giảm mất ghế đã cấp; UPDATE tổng/còn ghế trong cùng transaction. Không tự sửa doanh thu/lịch sử. |
| Xóa hạng | [ManagementPage.Delete](../src/FlightBooking.WinForms/ManagementPage.cs) hỏi xác nhận mã/tên → [ManagementBUS.DeleteFare](../src/FlightBooking.BUS/ManagementBUS.cs) → [Flight/Fare/FareUsed/DeleteFare](../src/FlightBooking.DAO/ManagementDAO.cs). Khóa/đọc lại, so snapshot, chặn TICKET tham chiếu; hợp lệ thì DELETE theo @id và commit. Tương ứng btnXoa_Click → Xoa, thêm bảo vệ quan hệ. |
| Tìm ArrayList | [ManagementPage.Render](../src/FlightBooking.WinForms/ManagementPage.cs) tạo ManagementFilter → [ManagementSearch.Filter/Value/Match](../src/FlightBooking.BUS/ManagementBUS.cs). foreach từng DTO, lấy field, so sánh, AND/OR rồi NOT trên nhóm; trả ArrayList mới. Trạng thái Việt được đổi về mã SQL, số Việt kiểm tra nhóm trước parse; GUI đổ lại ListView. Không truy vấn WHERE cho phần lọc này. |
| Hai ListView và mở hạng | [SelectionChanged → ResetView → UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs) lấy FlightManagementDTO từ Tag, xóa danh sách con, chỉ thêm FareClassDTO cùng FlightId. [ShowFares](../src/FlightBooking.WinForms/ManagementPage.cs) chờ tải đầu, ReadAll qua BUS/DAO lấy dữ liệu mới, kiểm tra chuyến tồn tại rồi đặt ComboBox/phạm vi. Không dùng cache cũ để bỏ sót hạng mới. |
| Đặt vé nguyên tử | [CheckoutModel.OnPost](../src/FlightBooking.Web/Pages/Booking/Checkout.cshtml.cs) kiểm tra phiên/ModelState → [BookingBUS.Create](../src/FlightBooking.BUS/BookingBUS.cs). Validate, hash khóa/nội dung, khóa yêu cầu/chuyến theo thứ tự, MakeJourneyQuote đọc lại giá/chỗ/mã; so ExpectedTotal. [BookingDAO.Write/BookingData](../src/FlightBooking.DAO/BookingDAO.cs) reserve ghế, tạo Customer/Booking/Invoice UNPAID/Ticket/Detail và request; hai lượt cùng transaction. Commit mới thành công; lỗi chưa commit thì rollback. Luồng SQL này chưa thực thi trong đợt nghiệm thu. |
| Hủy và hoàn ghế | [DetailsModel.OnPostCancel](../src/FlightBooking.Web/Pages/Booking/Details.cshtml.cs) → [CancelJourney/CheckCancellation](../src/FlightBooking.BUS/BookingBUS.cs) xác minh khóa, chưa thanh toán, trước giờ đi → [BookingData.Cancel](../src/FlightBooking.DAO/BookingDAO.cs). Đổi Booking/Ticket/Invoice sang CANCELLED, giảm tickets_sold/tăng seats_available, trả lượt mã; hủy lặp bỏ qua đơn đã hủy. Cả hành trình trong transaction, lỗi một bước không commit phần còn lại. Chưa có kết quả chạy SQL hủy/rollback mới. |

Nguồn gốc để kiểm tra lại: SHA-256 archive QLSV3Lop là
`E360275C89D00F00FF9750A49644B194CEE3141CB075C4769C9D1F7096C21540`;
W7 là `560775B0612E50D67C681455E3D161CA684E6EACA2B9A6A5FD441B2D3C403F06`;
tài liệu 3 lớp là `04A3CE1E7BDE5184FA8BB139E35BF50A32466840B18B8E23AB56F38B05EA0CDC`.
