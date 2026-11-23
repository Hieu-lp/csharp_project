# Rà soát BookingFlight — 23/11/2026

**Tiếp nối khi đã nhận tài liệu gốc:** xem [đối chiếu W7/QLSV3Lop và kiểm tra GUI
bổ sung](DoiChieu-W7-QLSV3Lop.md). Các ghi nhận thiếu PDF/mẫu bên dưới thuộc thời
điểm rà soát ban đầu.

Đã sửa ba lỗi trong phần quản lý, build thành công và chạy lại kiểm tra.
Chưa nghiệm thu toàn bộ đồ án hoặc luồng đặt/hủy SQL: máy này không có database
kiểm thử riêng và database chính thiếu BOOKING_WEB_REQUEST. Không áp dụng script 08.

## A. Trạng thái repository và tài liệu

| Nội dung | Kết quả thực tế |
|---|---|
| Repository | `E:\csharp_project`, remote `https://github.com/Hieu-lp/csharp_project.git` |
| Trạng thái ban đầu | Nhánh `feature/database-schema`, HEAD `f63aa5d`, working tree sạch |
| Nhánh làm việc sau khảo sát | `feature/flight-management`, tracking `origin/feature/flight-management` |
| HEAD khi rà soát | `9b20abcdbe7bab5f88ca2ee9561613d173fa5fc2` |
| Bảo toàn dữ liệu | Ban đầu không có thay đổi chưa commit; không reset/clean/stash hoặc ghi đè công việc người dùng |
| Git | Đã fetch và kiểm tra nhánh trước khi chuyển; không commit/push/PR/merge |
| Runtime | Windows 10 x64, .NET SDK 10.0.401; thư viện/Web/test net10.0, WinForms net10.0-windows |
| Hướng dẫn repository | Không tìm thấy AGENTS.md ở repository hoặc thư mục cha được kiểm tra |

Đã đọc [README](../README.md), [bàn giao Hiếu](Hieu-FlightManagement.md),
[hướng dẫn chạy](HuongDan-Chay-Va-Test.md), [bàn giao Thịnh](Thinh-Booking.md),
[bộ lọc Booking](Booking-Filters-Deals.md), [lịch sử dữ liệu khứ hồi](RoundTrip-Demo-2026-2027.md),
[solution](../FlightBookingSystem.slnx), các project, source BUS/DAO/DTO/GUI,
[schema](../Database/02_CreateTables.sql), [index](../Database/03_Constraints_Indexes.sql)
và [ERD](flight_booking_erd_source.drawio).

Không có bản gốc `C-w4-YeuCauDoAn.pdf`, `C-w7-YeuCauDoAn.pdf`, `c-3-layer-.pdf`,
`QLSV3Lop.rar` hoặc file phân công nhóm trong repository. Đường dẫn `D:\bookingflight`
được nhắc trong bàn giao cũ cũng không tồn tại trên máy này. Đã hỏi đường dẫn tài liệu;
chưa nhận được tài liệu bổ sung khi lập báo cáo. Vì vậy chưa giải nén/đọc trực tiếp
QLSV3Lop và chưa xác nhận đầy đủ yêu cầu W7 nguyên bản. Bảng dưới đối chiếu yêu cầu
trong attachment với source; phân công Phú/Quân dựa trên attachment và bàn giao hiện có.

## B. Đối chiếu yêu cầu

“Đạt” dưới đây chỉ áp dụng cho bằng chứng ghi ở cột cuối; kiểm tra C# hoặc đọc source
không thay thế kiểm thử ghi SQL và toàn bộ thao tác GUI.

| Yêu cầu | File thực hiện | Trạng thái | Bằng chứng |
|---|---|---|---|
| GUI → BUS → DAO, MyConnection | [ManagementBUS](../src/FlightBooking.BUS/ManagementBUS.cs), [ManagementDAO](../src/FlightBooking.DAO/ManagementDAO.cs), [MyConnection](../src/FlightBooking.DAO/MyConnection.cs) | Đã có, xác minh phân lớp | GUI không tạo SqlConnection/truy vấn SQL; đọc GUI thực tế qua BUS/DAO |
| DTO truyền dữ liệu | [BookingModels](../src/FlightBooking.DTO/BookingModels.cs), [ManagementModels](../src/FlightBooking.DTO/ManagementModels.cs) | Đã có, xác minh tĩnh | AirportDTO, FlightDTO, FareClassDTO, FlightEdit và BookingRequest dùng chung |
| Phong cách QLSV3Lop, 3 lớp ver2 theo bản gốc | [BUS](../src/FlightBooking.BUS/ManagementBUS.cs), [DAO](../src/FlightBooking.DAO/ManagementDAO.cs) | Chưa đối chiếu đầy đủ | Thiếu archive/PDF gốc; giữ kiến trúc hiện có, thêm chú thích Việt tại chỗ sửa |
| CRUD AIRPORT/FLIGHT/FARE_CLASS | [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs), [ManagementBUS](../src/FlightBooking.BUS/ManagementBUS.cs), [ManagementDAO](../src/FlightBooking.DAO/ManagementDAO.cs) | Đã có, chưa thử ghi SQL trong đợt này | Có Save/Delete, kiểm tra trùng/tham chiếu, transaction; không CRUD database chính |
| Tìm ArrayList tại BUS | [ManagementSearch.Filter](../src/FlightBooking.BUS/ManagementBUS.cs) | Đạt kiểm tra C# | AND/OR/NOT, =, <>, >, >=, <, <=, chứa; hai điều kiện cùng/khác trường |
| Ánh xạ tên trường/toán tử/trạng thái Việt | [ManagementPage.BuildHeader](../src/FlightBooking.WinForms/ManagementPage.cs), [ManagementSearch.Match](../src/FlightBooking.BUS/ManagementBUS.cs) | Đã sửa, đạt C# | Nhãn so sánh về khóa nội bộ; Mở bán/Đã hoàn thành/Đã hủy và mã SQL đều tìm được |
| Form main-sub, hai ListView FLIGHT → FARE_CLASS | [SelectionChanged/UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs) | Đạt đọc GUI thực tế | Chọn qua 21 chuyến; danh sách con khớp fare_id của từng chuyến |
| Chuyến không có hạng | [UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs) | Có code, chưa thử fixture SQL riêng | Xóa danh sách con trước khi điền; hiện nhãn “chưa có hạng vé” khi rỗng |
| Tải lại, mở hạng đúng chuyến/ngày/tuyến | [ReadAll/ShowFares](../src/FlightBooking.WinForms/ManagementPage.cs) | Đã sửa, đạt GUI chỉ đọc | Cố ý làm cache cũ, mở hạng lại: danh sách chuyến/hạng và hai ComboBox khớp |
| Cửa sổ chọn và biến static | [SelectionDialog](../src/FlightBooking.WinForms/SelectionDialog.cs) | Đạt chọn/Hủy/đóng GUI | Trả đúng DTO; lần mở sau xóa static; Hủy/X giữ null. Esc gắn CancelButton, chưa gửi phím Esc thực tế trong runner |
| Duy nhất và tham chiếu | [ManagementBUS](../src/FlightBooking.BUS/ManagementBUS.cs), [schema/index](../Database/02_CreateTables.sql) | Có code, chưa nghiệm thu ghi SQL | Cặp tên/thành phố sân bay; tên hạng trong chuyến; FK/unique ghế; không tự đặt quy tắc trùng chuyến mới |
| Kiểm tra lịch/tuyến/ghế/trạng thái | [ValidateFlight/ValidateFlightChange](../src/FlightBooking.BUS/ManagementBUS.cs) | Đạt C#, SQL chưa chạy | Chuyến qua đêm/qua năm, cùng sân bay, ghế 20F, đổi lịch/trạng thái khi đã có Booking |
| Trường biến động và chỉ đọc | [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs), [BookingDAO](../src/FlightBooking.DAO/BookingDAO.cs) | Đạt readonly GUI; cập nhật SQL chưa chạy | ID/vé bán/ghế còn/doanh thu ReadOnly; reserve/cancel trong transaction; tạo đơn chưa cộng doanh thu |
| Giá/hành lý/hạn mức hạng | [ValidateFare](../src/FlightBooking.BUS/ManagementBUS.cs), [FARE_CLASS](../Database/02_CreateTables.sql) | Đạt validation C#; ghi SQL chưa chạy | Giá không âm, 2 số lẻ, độ dài tên, hành lý; schema chỉ có hạn mức ghế ở FLIGHT, không tự thêm cột ghế FARE_CLASS |
| Editor Xem/Thêm/Sửa; lỗi không mất nội dung | [EditorSession](../src/FlightBooking.WinForms/EditorSession.cs), [Save/CanLeave](../src/FlightBooking.WinForms/ManagementPage.cs) | C# đạt; GUI đầy đủ chưa nghiệm thu | 6 kiểm tra editor, có ErrorProvider và giữ nội dung khi BUS báo lỗi; chưa thử CRUD GUI trên DB riêng |
| Tìm/chọn một chiều, khứ hồi, khách/hành khách, giảm giá | [BookingBUS](../src/FlightBooking.BUS/BookingBUS.cs), [Web Pages](../src/FlightBooking.Web/Pages/) | 82 C# đạt; web được kiểm tra từng phần | Xem mục E về phạm vi trình duyệt; không dùng kết quả lịch sử làm kết quả mới |
| Đặt/hủy, rollback, gửi lặp, tranh chấp ghế | [Create/CancelJourney](../src/FlightBooking.BUS/BookingBUS.cs), [BookingDAO.Write](../src/FlightBooking.DAO/BookingDAO.cs) | Có code, bị chặn nghiệm thu SQL | SERIALIZABLE, khóa FLIGHT theo thứ tự, sp_getapplock, snapshot giá; thiếu DB test và BOOKING_WEB_REQUEST trên DB chính |
| Không sửa giá lịch sử khi đổi giá danh mục | [ManagementData.SaveFare](../src/FlightBooking.DAO/ManagementDAO.cs) | Xác minh source; chưa thử SQL mới | Chỉ UPDATE FARE_CLASS; BOOKING_DETAIL/INVOICE_DETAIL không bị cập nhật ở hàm này |
| Table tham số | [schema hiện có](../Database/02_CreateTables.sql) | Thiếu/chưa chốt trách nhiệm | Không thấy bảng tham số nghiệp vụ; không tự thêm schema; cần đối chiếu W7 gốc và phân công |
| Thống kê, hóa đơn, thanh toán, Excel, PDF | [phân công hiện có](Thinh-Booking.md) | Ngoài phạm vi Hiếu–Thịnh | Quân phụ trách theo attachment/bàn giao; chưa thấy module đầy đủ trên nhánh này, không tự viết thay |
| Desktop/menu/form con | [Form1](../src/FlightBooking.WinForms/Form1.cs) | Có ba tab; menu chung chưa xác minh | Form1 chứa ba ManagementPage; không tự thay MainForm của thành viên khác |
| Ma trận độ phân giải/DPI | [Form1/ManagementPage](../src/FlightBooking.WinForms/) | Chưa kiểm tra đầy đủ | Đã chạy cửa sổ GUI thật; chưa đủ 1366×768/1920×1080 × 100/125/150% |
| Báo cáo cuối kỳ | [Docs](./) | Chưa xác nhận có báo cáo toàn nhóm | Tài liệu bàn giao hiện có không thay báo cáo cuối kỳ hoặc xác nhận tiêu chí PDF gốc |

ERD còn chênh lệch với SQL: airport_name/full_name của CUSTOMER là varchar trong
bản vẽ nhưng SQL là nvarchar; gender CUSTOMER cũng khác; BOOKING.total_amount thiếu
precision trong bản vẽ; ticket_id ghi thiếu/sai kiểu. Không sửa ERD hoặc schema.

## C. Lỗi và điểm chặn đã tìm được

1. **Bộ lọc trạng thái tiếng Việt không tìm được bản ghi — mức vừa.**
   Trên Chuyến bay, chọn `Trạng thái`, `=`, nhập `Mở bán`. ListView hiển thị nhãn này
   nhưng `ManagementSearch.Value` trả `SCHEDULED`; `Match` trước đây so thẳng hai chuỗi.
   Test mới đã thất bại tại `Vietnamese/SQL status Mở bán` trước khi sửa.
   Hiện chấp nhận cả nhãn Việt/mã SQL; phép chứa tìm theo cả hai, vẫn xử lý trong BUS.

2. **Số phân nhóm sai bị hiểu thành số khác — mức vừa.**
   `decimal.TryParse` với NumberStyles.Number/vi-VN chấp nhận `1.5` như 15;
   bộ lọc mã/giá có thể trả kết quả sai thay vì báo lỗi. `ManagementSearch.Match`
   nay kiểm tra nhóm ba chữ số trước khi parse. Test từ chối `1.5`, `1.00.000`,
   `1..000`, `1,2,3`, `1e3`; vẫn chấp nhận `1.000.000`, `1,5` và ngưỡng âm.

3. **Mở quản lý hạng từ chuyến dùng cache cũ — mức vừa.**
   Tải tab Hạng vé, sau đó thêm/sửa chuyến hoặc hạng ở nơi khác, quay lại Chuyến bay
   và bấm Quản lý hạng vé. `ShowFares` trước đây chỉ gọi `EnsureLoaded`, vốn bỏ qua
   tải lại khi tab đã có dữ liệu. ComboBox/danh sách có thể thiếu chuyến/hạng mới.
   Đã đổi sang `Run → ReadAll → kiểm tra chuyến → đặt phạm vi → Render`.
   GUI test làm rỗng cache cục bộ để tái hiện tab cũ, rồi xác nhận dữ liệu được tải
   lại từ SQL và chọn đúng chuyến. Không tạo/xóa chuyến thật để kiểm thử lỗi này.

4. **Thiếu BOOKING_WEB_REQUEST — điểm chặn môi trường, không sửa schema.**
   SQL SELECT trên `./FlightBookingDB` trả OBJECT_ID của bảng bằng NULL,
   trong khi HAS_PERMS_BY_NAME(..., 'DATABASE', 'VIEW DEFINITION') = 1;
   đây là thiếu bảng, không phải do tài khoản không nhìn thấy metadata.
   `BookingData.FindRequest/SaveRequest/Booking` phụ thuộc bảng này với các cột
   request_hash/payload_hash/booking_id. Không thể nghiệm thu xác nhận/tra cứu/hủy
   đơn web mới ở database hiện tại. Người phụ trách database cần xử lý theo quy trình
   riêng; đợt này không chạy hoặc sửa [script 08](../Database/08_BookingWebRequest.sql).

## D. Thay đổi đã thực hiện

| File | Hàm/nội dung | Mục đích |
|---|---|---|
| [ManagementBUS.cs](../src/FlightBooking.BUS/ManagementBUS.cs) | ManagementSearch.Match | Ánh xạ trạng thái Việt và kiểm tra định dạng số; giữ ArrayList, không chuyển lọc sang SQL |
| [ManagementPage.cs](../src/FlightBooking.WinForms/ManagementPage.cs) | ShowFares | Tải dữ liệu mới qua BUS trước khi chọn phạm vi; báo rõ nếu chuyến đã bị xóa; vẫn hỏi bỏ thay đổi trước khi tải |
| [ManagementChecks/Program.cs](../tests/FlightBooking.ManagementChecks/Program.cs) | Thêm 16 kiểm tra | Trạng thái, số sai định dạng, ngưỡng âm/thập phân, khoảng trên một trường, NOT/OR |
| [WinFormsChecks](../tests/FlightBooking.WinFormsChecks/) | Project kiểm tra GUI chỉ đọc | Chạy control WinForms thật, ListView cha–con, cache, mở tab lần đầu và static selection; không gọi Save/Delete |
| [readonly.http.ps1](../tests/FlightBooking.Checks/readonly.http.ps1) | Kiểm tra HTTP chỉ đọc | Lấy ID từ kết quả server; chỉ gửi xem giá, không gửi book/cancel; UTF-8 BOM để chạy đúng tiếng Việt trong Windows PowerShell 5 |
| [FlightBookingSystem.slnx](../FlightBookingSystem.slnx) | Thêm project WinFormsChecks | Build cùng solution; không thêm thư viện NuGet |
| [Hướng dẫn chạy](HuongDan-Chay-Va-Test.md) và báo cáo này | Liên kết kết quả hiện tại | Phân biệt máy hiện tại với các kết quả bàn giao cũ |

Giữ nguyên Booking của Thịnh, DAO, DTO, database, seed, ERD và phần ngoài phạm vi.
Reflection chỉ dùng trong chương trình kiểm tra GUI để mô phỏng cache cũ/đọc control
private; không đưa reflection hoặc cơ chế giả lập vào ứng dụng nghiệp vụ.

## E. Kết quả kiểm thử mới

| Lệnh/hạng mục | Kết quả |
|---|---|
| `dotnet build FlightBookingSystem.slnx` | PASS, 8 project, 0 lỗi, 0 cảnh báo sau sửa |
| `dotnet run --no-build --project tests/FlightBooking.ManagementChecks` | 53/53 đạt, tăng từ 37 lên 53 |
| `dotnet run --no-build --project tests/FlightBooking.Checks` | 82/82 đạt |
| `node tests/FlightBooking.Checks/checkout.checks.cjs` | PASS |
| `node tests/FlightBooking.Checks/search-form.checks.cjs` | PASS |
| `dotnet run --no-build --project tests/FlightBooking.WinFormsChecks` | 35/35 GUI chỉ đọc đạt với dữ liệu hiện tại (21 chuyến); số kiểm tra thay đổi theo số chuyến |
| `& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl http://127.0.0.1:5087` | 17/17 HTTP chỉ đọc đạt: tìm/chọn chuyến, hai hành khách, tính giá server, giữ thông tin/mã sai, chống sửa Quantity, tra cứu chưa xác minh, antiforgery |
| SQL integration `--sql`, đặt/hủy/rollback/tranh chấp | Chưa chạy; không có database test riêng được xác minh |
| Ma trận DPI, CRUD GUI, ErrorProvider trên thao tác ghi | Chưa nghiệm thu đầy đủ |

Lần build đầu trong sandbox thất bại NU1301 do socket tới api.nuget.org bị chặn.
Đã chạy lại với quyền mạng, restore thành công. Các lượt build thành công trong
đợt này không có NU1900; những cảnh báo NU1900 trong bàn giao cũ không phải kết quả mới.

SQL Server cục bộ có service MSSQLSERVER đang chạy; truy vấn sys.databases chỉ thấy
`FlightBookingDB` trong các tên Flight/*_ManagementTests. Không có biến môi trường
FLIGHTBOOKING_* hoặc ConnectionStrings__FlightBookingDB cấu hình sẵn lúc khảo sát.
Kết nối được xác minh bằng Windows Authentication, không đưa mật khẩu vào source.
Truy vấn chỉ đọc trước và sau kiểm tra ghi nhận cùng số bản ghi: 8 AIRPORT,
21 FLIGHT, 41 FARE_CLASS, 81 BOOKING, 141 TICKET. Không seed, không chạy
demo-rollback trên database chính. Đây là đối chiếu số dòng năm bảng, không phải
nghiệm thu thay đổi ghế/doanh thu hoặc so sánh toàn bộ nội dung database.
Website kiểm tra chạy tại http://127.0.0.1:5087 và đã dừng sau kiểm tra. Công cụ
mở trình duyệt tích hợp thất bại; chỉ xác minh HTTP/HTML và JavaScript tự động,
chưa quan sát trực quan desktop/mobile của Web trong đợt này. Bộ HTTP chỉ thử một
chiều và xem giá, chưa kiểm tra đủ khứ hồi/giảm giá hợp lệ/đặt/tra cứu/hủy SQL.

## F. Hướng dẫn chạy trên Windows

### 1. Mở và build

Mở repository trong VS Code/Visual Studio có hỗ trợ .NET 10. Dùng PowerShell:

```powershell
Set-Location 'E:\csharp_project'
git status --short --branch
dotnet --info
dotnet build FlightBookingSystem.slnx
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks
```

Nếu NU1301: kiểm tra mạng/proxy/quyền truy cập NuGet rồi chạy lại build. NU1900 là
cảnh báo không lấy được dữ liệu audit, không tự coi là lỗi biên dịch. Nếu DLL bị
khóa MSB3021/MSB3027, đóng đúng tiến trình app do mình mở trước khi build lại.

### 2. Xác nhận SQL Server và kết nối

```powershell
Get-Service -Name 'MSSQL*'
sqlcmd -S . -E -C -d master -Q "SELECT name,state_desc FROM sys.databases;"
sqlcmd -S . -E -C -d FlightBookingDB -Q "SELECT DB_NAME() AS database_name,OBJECT_ID('dbo.BOOKING_WEB_REQUEST') AS web_request_object;"
```

`.` chỉ phù hợp với default instance trên máy đã kiểm tra. Máy khác phải thay đúng
server/instance có thật; không dùng mặc định RNR-LAPTOP. Nếu lỗi kết nối: kiểm tra
service, instance, Windows login/quyền vào database và cấu hình giao thức. -C/
TrustServerCertificate dành cho môi trường phát triển có chứng chỉ tự ký.

Trong **mỗi** cửa sổ PowerShell chạy app, đặt cùng chuỗi kết nối. Ví dụ xem dữ liệu
hiện có trên máy này (không thực hiện CRUD/đặt/hủy ở database chính):

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=.;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Không lưu mật khẩu vào Git. WinForms cũng đọc connection.local.txt cạnh file chạy
nếu không có biến môi trường; Web dùng cấu hình ASP.NET Core. Để tránh khác nguồn,
ưu tiên cùng biến môi trường ở cả hai terminal.

### 3. Mở hai giao diện

Terminal WinForms:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Terminal Web đã đặt cùng biến môi trường:

```powershell
Set-Location 'E:\csharp_project'
dotnet run --no-build --project src/FlightBooking.Web --launch-profile http
```

Mở URL thực tế từ terminal; profile http hiện dùng http://localhost:5191.
Nhấn Ctrl+C để dừng Web; đóng form để dừng WinForms.

### 4. Kiểm tra giao diện chỉ đọc

```powershell
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks
```

Runner mở cửa sổ rồi tự đóng. Chỉ tải/chọn/lọc danh mục đã tồn tại; không tạo fixture
SQL, không lưu/xóa dữ liệu. Cần database đã có ít nhất một sân bay và chuyến có hạng.

Với Web đang chạy, có thể kiểm tra HTTP chỉ đọc (đổi BaseUrl theo terminal):

```powershell
& 'tests/FlightBooking.Checks/readonly.http.ps1' -BaseUrl 'http://localhost:5191'
node tests/FlightBooking.Checks/checkout.checks.cjs
node tests/FlightBooking.Checks/search-form.checks.cjs
```

HTTP runner cần chuyến đang nhận đặt, còn ít nhất hai ghế và giá dương trong danh
sách giá tốt hiện có. Chỉ tạo nháp trong session, không gửi xác nhận đặt/hủy.

### 5. CRUD và tích hợp A/B/C chỉ khi có database kiểm thử riêng

Không thực hiện các bước ghi sau trên FlightBookingDB chính của máy này. Người phụ
trách cần cung cấp database kiểm thử đã chuẩn bị/duyệt schema phù hợp, gồm phụ thuộc
BOOKING_WEB_REQUEST khi kiểm tra Web. Không tự chạy seed hoặc script 08.

1. Xác minh DB_NAME() và trỏ **cả hai app** tới đúng database kiểm thử.
2. Trên WinForms, dùng sân bay/chuyến/hạng test hiện có hoặc tạo bản ghi test mới.
   Ghi lại ID được sinh; chọn lịch tương lai và hạng giá hợp lệ, đủ ít nhất hai ghế.
3. Thử AIRPORT: thiếu tên/thành phố, vượt độ dài, trùng cặp tên/thành phố, sửa,
   xóa bản ghi tự tạo chưa tham chiếu; xóa sân bay có chuyến phải bị chặn.
4. Thử FLIGHT: cùng sân bay, giờ đến không sau giờ đi, 0 ghế, trạng thái sai.
   Thử FARE_CLASS: tên trùng trong chuyến, giá/hành lý âm; đổi giá không sửa giá cũ.
5. Chọn chuyến, ghi tổng ghế/vé bán/ghế còn/doanh thu. Trên Web tìm đúng tuyến/ngày
   theo dữ liệu thật, chọn hạng, nhập người đặt đủ tuổi/giấy tờ và hai hành khách riêng.
   Nếu khứ hồi, chọn chuyến về đảo tuyến, khởi hành sau khi lượt đi hạ cánh.
6. Xác nhận Booking test, lưu mã/khóa. Tải lại WinForms: đã bán +2, còn -2 cho
   mỗi lượt; invoice UNPAID không làm tăng doanh thu đã thu.
7. Tra cứu/hủy đơn chưa thanh toán trước giờ bay. Tải lại: ghế và lượt dùng mã
   trở về đúng mức; hủy lại không hoàn ghế/lượt mã lần hai.
8. Khi đã có vé/Booking, thử giảm tổng ghế dưới mức sử dụng hoặc bỏ vị trí ghế
   đã cấp; sửa tuyến/lịch/trạng thái hay xóa dữ liệu có tham chiếu phải bị chặn.
9. Thử cảnh báo khi đổi dòng/tab/tải lại/đóng form; không đổi dữ liệu thì không
   hỏi bỏ thay đổi; lưu lỗi giữ nội dung; Lưu/Hủy luôn ngoài vùng cuộn.
10. Chỉ dọn đúng bản ghi do lần test tạo và không còn tham chiếu. Không xóa dữ liệu
    test có sẵn của người khác. Chạy thêm tranh chấp/rollback khi môi trường được phép.

Chạy bộ SQL quản lý cũ chỉ sau khi đã xác nhận database riêng có hậu tố chính xác:

```powershell
# Thay SERVER và tên database bằng database test có thật đã được xác nhận.
$env:FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION = 'Server=SERVER;Database=FlightBooking_ManagementTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --no-build --project tests/FlightBooking.ManagementChecks -- --sql
```

[SqlManagementChecks](../tests/FlightBooking.ManagementChecks/SqlManagementChecks.cs)
có thể áp schema 02/03 khi database test rỗng; cần đọc trước và tuân thủ quyền của
người phụ trách database. Bộ SQL Booking hiện tự áp schema/script 08 lên database
test; vì hạn chế trong nhiệm vụ này, không chạy bộ đó hoặc --demo-rollback vào DB chính.

## G. Học luồng code theo GUI → BUS → DAO → SQL Server

Chưa có mẫu QLSV3Lop để so sánh trực tiếp; các luồng dưới là tên hàm thật trong
source hiện tại. DTO là dữ liệu đầu vào/đầu ra, BUS kiểm tra nghiệp vụ, DAO chạy
SQL tham số hóa qua MyConnection. Dùng các luồng này để đọc/giải thích trước khi
học thêm helper hoặc transaction.

| Tác vụ | Luồng thực tế, đầu vào/đầu ra và điều kiện lỗi |
|---|---|
| Đọc sân bay | [ManagementPage.ReadAll](../src/FlightBooking.WinForms/ManagementPage.cs) → [ManagementBUS.Airports](../src/FlightBooking.BUS/ManagementBUS.cs) → [ManagementDAO.Read/ManagementData.Airports](../src/FlightBooking.DAO/ManagementDAO.cs) → SELECT airport_id,airport_name,city. SqlDataReader tạo AirportDTO, BUS trả ArrayList, Render tạo các dòng ListView. Thiếu cấu hình/kết nối được đổi thành ManagementException. |
| Thêm sân bay | [ManagementPage.Save](../src/FlightBooking.WinForms/ManagementPage.cs) lấy TextBox, tạo AirportDTO ID=0 → [SaveAirport/ValidateAirport](../src/FlightBooking.BUS/ManagementBUS.cs) trim/kiểm tra độ dài và trùng → [Write/AirportDuplicate/SaveAirport](../src/FlightBooking.DAO/ManagementDAO.cs) → INSERT OUTPUT INSERTED.airport_id. Commit rồi GUI tải lại và chọn mã vừa lưu. Lỗi dữ liệu giữ nội dung và gắn ErrorProvider. |
| Sửa chuyến | [ManagementPage.Save](../src/FlightBooking.WinForms/ManagementPage.cs) tạo FlightEdit cùng snapshot gốc → [ManagementBUS.SaveFlight](../src/FlightBooking.BUS/ManagementBUS.cs) → [Flight(locked), FlightHasBookings, ActiveSeats, SaveFlight](../src/FlightBooking.DAO/ManagementDAO.cs). BUS từ chối snapshot cũ, lịch sai, đổi tuyến/lịch khi có Booking, ghế không đủ hoặc mất vị trí đã cấp. DAO UPDATE tổng ghế/còn ghế trong SERIALIZABLE; không sửa doanh thu/vé lịch sử. |
| Xóa hạng | [ManagementPage.Delete](../src/FlightBooking.WinForms/ManagementPage.cs) xác nhận mã/tên → [ManagementBUS.DeleteFare](../src/FlightBooking.BUS/ManagementBUS.cs) → [Flight/Fare/FareUsed/DeleteFare](../src/FlightBooking.DAO/ManagementDAO.cs). Đọc lại/khóa chuyến, so snapshot, từ chối khi có TICKET; nếu hợp lệ DELETE theo @id rồi commit. |
| Tìm ArrayList | [ManagementPage.Render](../src/FlightBooking.WinForms/ManagementPage.cs) tạo ManagementFilter từ nhãn/giá trị ComboBox → [ManagementSearch.Filter/Value/Match](../src/FlightBooking.BUS/ManagementBUS.cs). foreach kiểm tra điều kiện, AND/OR rồi đảo NOT, trả ArrayList mới cho ListView. Không phát sinh SQL tìm nâng cao. Giá dùng dấu chấm nhóm ba chữ số, dấu phẩy thập phân; ngày dd/MM/yyyy hoặc yyyy-MM-dd. |
| Hai ListView | [SelectionChanged → ResetView → UpdateChildren](../src/FlightBooking.WinForms/ManagementPage.cs): lấy DTO ở Tag của dòng FLIGHT, xóa ListView con, chỉ thêm fare có cùng FlightId. Reload đọc lại qua BUS; ShowFares tải mới rồi chọn phạm vi của chuyến trước khi Render. |
| Đặt vé | [CheckoutModel.OnPost](../src/FlightBooking.Web/Pages/Booking/Checkout.cshtml.cs) kiểm tra phiên/ModelState → [BookingBUS.Create](../src/FlightBooking.BUS/BookingBUS.cs) Validate, hash nội dung/khóa, kiểm tra gửi lặp, MakeJourneyQuote tính tiền từ server → [BookingDAO.Write/BookingData](../src/FlightBooking.DAO/BookingDAO.cs). Khóa yêu cầu/chuyến, AllocateSeats, ReserveSeats, InsertCustomer/Booking/Invoice/Passenger, SaveRequest; khứ hồi trong cùng transaction. Dispose transaction chưa commit sẽ rollback. Cần BOOKING_WEB_REQUEST; hiện chưa thử ghi thực tế. |
| Hủy và hoàn ghế | [DetailsModel.OnPostCancel](../src/FlightBooking.Web/Pages/Booking/Details.cshtml.cs) → [CancelJourney/CheckCancellation](../src/FlightBooking.BUS/BookingBUS.cs) xác minh khóa, CONFIRMED+UNPAID và trước giờ đi → [BookingData.Cancel](../src/FlightBooking.DAO/BookingDAO.cs). UPDATE Booking/Ticket/Invoice sang CANCELLED, giảm tickets_sold/tăng seats_available, trả lượt mã trong cùng transaction. Đã CANCELLED thì không hoàn lần nữa; có lỗi một lượt thì toàn giao dịch không commit. Chưa thử ghi SQL trong đợt này. |

Việc còn lại: nhận PDF/mẫu/phân công gốc để đối chiếu trực tiếp; cung cấp database
test được phép và đủ schema để nghiệm thu A/B/C, rollback, đồng thời; hoàn thành
ma trận DPI và CRUD GUI; nhóm xác nhận phần tham số/thống kê/Excel/PDF/báo cáo cuối kỳ.
