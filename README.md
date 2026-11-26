# BookingFlight — Quản lý bán vé máy bay

Đồ án C# .NET 10 dùng WinForms, ASP.NET Core Razor Pages và SQL Server.
Kiến trúc **GUI → BUS → DAO**, DTO truyền dữ liệu giữa các lớp; DAO dùng
SqlClient và truy vấn tham số hóa. Identifier nghiệp vụ dùng tiếng Việt không dấu.

Hiếu quản lý sân bay, chuyến bay, hạng vé và tìm kiếm ArrayList AND/OR/NOT.
Phú quản lý khách hàng, nhân viên và mã giảm giá. MainForm dùng một vùng nội dung
chung cho Tổng quan, Sân bay, Chuyến bay, Hạng vé, Khách hàng, Nhân viên,
Mã giảm giá, Vé, Hóa đơn và Thống kê. Ba tab của Hiếu và ListView cha–con được giữ; chuyển chức năng
phải xác nhận nếu còn thay đổi chưa lưu. Các module Phú dùng ListView và tên Việt hóa.
Thịnh triển khai tìm/chọn chuyến, báo giá server, đặt vé một chiều/khứ hồi,
tra cứu và hủy. Đặt/hủy chạy trong transaction, bảo vệ ghế, giá lịch sử và
yêu cầu gửi lặp; hóa đơn mới UNPAID, chưa ghi doanh thu đã thu.

## Môi trường

- Windows để chạy WinForms; .NET 10 SDK để restore/build.
- SQL Server và quyền truy cập database được nhóm cho phép.
- Microsoft.Data.SqlClient được restore theo project DAO, không cần cài gói thủ công.
- Tùy chọn: SSMS hoặc sqlcmd để quản trị SQL; Python 3, Node.js để chạy policy/JavaScript checks.

## Clone và build

```powershell
git clone https://github.com/Hieu-lp/csharp_project.git
Set-Location csharp_project
git switch feature/final-integration-fix
dotnet restore FlightBookingSystem.slnx
dotnet build FlightBookingSystem.slnx
```

Dừng đúng tiến trình Web/WinForms đang giữ DLL trước khi build nếu gặp lỗi file lock.
Không commit mật khẩu, access key, database backup hoặc cấu hình riêng của máy.

## Chuẩn bị database

Ứng dụng demo dùng **FlightBooking_Final**, độc lập với `FlightBookingDB` và
`FlightBooking_Hieu_ManagementTests`. Bộ script [Database/Final/](Database/Final/)
chỉ dùng 12 bảng hiện có, gồm `BOOKING_WEB_REQUEST`; chưa thêm chi phí/tham số.
Khởi tạo ứng dụng thống nhất dùng `Database/Final/01_CreateAndSchema.sql`,
`02_SeedDemo.sql` và `03_Verify_ReadOnly.sql` theo thứ tự bên dưới, chỉ trên DB mới
được duyệt. Database đã dùng thì đối chiếu hiện trạng, không tạo lại hoặc seed lại.

Ba file ở gốc Database được giữ làm **nguồn schema**, không phải bộ cài ứng dụng:
`02_CreateTables.sql`, `03_Constraints_Indexes.sql`, `08_BookingWebRequest.sql`.
`Database/TestPlan/prepare_plan.py` và `source-manifest.json` phụ thuộc nội dung/hash
của chúng. Không chạy trực tiếp ba file này vào Final vì còn `USE FlightBookingDB`.
TestPlan là môi trường nghiệm thu riêng có guard, không thay thế bộ khởi tạo Final.

DBA kiểm tra instance bằng `SELECT @@SERVERNAME`, rồi thay `YOUR_VERIFIED_SERVER_NAME`
và `-S .` theo máy mình. Script tạo database từ chối nếu đích đã tồn tại; khi lỗi
phải dừng và khảo sát, không DROP hoặc chạy lại mù quáng.

```powershell
sqlcmd -S . -E -C -d master -b -Q "SELECT @@SERVERNAME AS ServerName"
sqlcmd -S . -E -C -d master -b -l 15 -t 120 -i Database/Final/01_CreateAndSchema.sql -v EXPECTED_SERVER="YOUR_VERIFIED_SERVER_NAME" CREATE_APPROVAL="APPROVE_CREATE_FLIGHTBOOKING_FINAL"
```

Review số lượng demo trước khi seed: 15 sân bay, 50 khách, 15 nhân viên, 15 mã giảm giá,
43 chuyến, 129 hạng vé, 81 booking/hóa đơn, 141 vé/chi tiết và 41 yêu cầu WEB.
Chuyến tương lai dùng ngày tương đối; dữ liệu lịch sử có đủ bốn quý năm trước,
các trạng thái PAID/UNPAID/CANCELLED và hai kênh WEB/COUNTER. Đây là dữ liệu giả
để trình diễn, không phải bằng chứng chức năng thanh toán đã được kiểm thử.

Chỉ seed **một lần**, sau khi số lượng được duyệt, trên database hoàn toàn rỗng
và chưa từng sử dụng. Đóng Web/WinForms đang kết nối Final. Toàn bộ seed dùng
một transaction; file đầu ra có khóa tra cứu demo riêng, chỉ giữ local trong `tmp/`.

```powershell
New-Item -ItemType Directory -Path tmp -Force | Out-Null
sqlcmd -S . -E -C -d FlightBooking_Final -b -l 15 -t 120 -i Database/Final/02_SeedDemo.sql -v EXPECTED_SERVER="YOUR_VERIFIED_SERVER_NAME" SEED_APPROVAL="APPROVE_SEED_FLIGHTBOOKING_FINAL_DEMO" SEED_PROFILE="DEMO_15_50_15_15_43_129_81_141" -o tmp/final-demo-lookup.private.txt
sqlcmd -S . -E -C -d FlightBooking_Final -b -i Database/Final/03_Verify_ReadOnly.sql -v EXPECTED_SERVER="YOUR_VERIFIED_SERVER_NAME"
```

Không seed lại, không sửa hai database cũ. Script writer
[04_ProposedWriter_ApprovalRequired.sql](Database/Final/04_ProposedWriter_ApprovalRequired.sql)
là đề xuất cần duyệt riêng; chưa cấp quyền chỉ vì đã tạo/seed database.

## Chạy WinForms và website

Tạo **một file local tại gốc repository** sau khi kiểm tra instance của mình:

```powershell
Set-Content -Encoding utf8 connection.local.txt 'Server=YOUR_SQL_SERVER;Database=FlightBooking_Final;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
dotnet build FlightBookingSystem.slnx
```

File này bị Git ignore, được copy cạnh executable của cả hai ứng dụng khi build
và không tự đóng gói publish. Máy đã được cấu hình thì chỉ cần chạy ứng dụng;
không phải đặt lại biến kết nối trong mỗi terminal. Windows account vẫn cần quyền SQL.

Terminal thứ nhất:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Terminal thứ hai:

```powershell
dotnet run --no-build --no-launch-profile --project src/FlightBooking.Web -- --urls http://127.0.0.1:5087
```

Mở http://127.0.0.1:5087/. Nếu chạy Web bằng profile mặc định thì URL là
http://localhost:5191 (xem launchSettings). WinForms khởi động qua MainForm hiện có.

Thứ tự ưu tiên: WinForms/DAO dùng `ConnectionStrings__FlightBookingDB`, rồi
`FLIGHTBOOKING_DB_CONNECTION`, rồi file local cạnh executable. Web giữ cấu hình
ASP.NET cho key `ConnectionStrings:FlightBookingDB` (command line, biến môi trường,
appsettings theo thứ tự framework); nếu key chưa có mới dùng cấu hình MyConnection
chung. Giá trị key rỗng tường minh vẫn báo lỗi, không tự chuyển sang database khác.
Kiểm tra/xóa override cũ trong terminal trước khi chạy nếu còn trỏ database test/main.
Không đổi key, không ghi connection string vào DAO hoặc commit cấu hình cá nhân.

Để đo kết nối thật, bật `$env:FLIGHTBOOKING_CONNECTION_DIAGNOSTICS='1'`, chạy ứng dụng
và mở chức năng đọc. MyConnection thực hiện `SELECT @@SERVERNAME,DB_NAME(),ORIGINAL_LOGIN()`
trên kết nối vừa mở và in `SQL_IDENTITY` (không in password/access key). Cả hai ứng dụng
phải báo `Database=FlightBooking_Final`. Sau khi kiểm tra có thể bỏ biến chẩn đoán.
Ví dụ trên dùng chứng chỉ tự ký cho local; triển khai cần chứng chỉ được tin cậy.

Thử: tạo chuyến/hạng → Web tìm/chọn → nhập người đặt và từng hành khách →
tính lại giá → xác nhận → lưu mã/khóa riêng → tra cứu/hủy → WinForms **Tải lại**.
Tải lại là đọc dữ liệu mới, không phải đồng bộ realtime. Chỉ dùng dữ liệu giả
trong môi trường kiểm thử được duyệt khi thực hiện các thao tác nghiệm thu ghi.

## Kiểm thử

Các lệnh sau không cần database và không ghi SQL:

```powershell
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks
python Database/TestPlan/prepare_plan.py --check
python tests/Shared/sql_test_plan_checks.py
node tests/FlightBooking.Checks/search-form.checks.cjs
node tests/FlightBooking.Checks/checkout.checks.cjs
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --dropdown-only
```

Kiểm thử SQL ghi, rollback, concurrency và GUI–Web sync chỉ dùng database test
riêng, Windows writer thật và đủ 16 gate preflight. Xem
[TestPlan](Database/TestPlan/README.md) để cấu hình đúng các biến môi trường và
chạy runner. Không dùng sysadmin thay writer, không bỏ guard, không seed lại DB test.

Kiểm tra chỉ đọc trên database test đã xác minh:

```powershell
$env:FLIGHTBOOKING_BROWSE_CONNECTION = 'Server=YOUR_SQL_SERVER;Database=FlightBooking_Hieu_ManagementTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True;'
dotnet run --no-build --project tests/FlightBooking.Checks -- --connection-settings
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:FLIGHTBOOKING_BROWSE_CONNECTION
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout
& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl http://127.0.0.1:5087
```

Các lệnh này cần kết nối test và Web tương ứng; HTTP readonly chỉ quote/back/lookup,
không đặt/hủy. Các script HTTP demo cũ có ID/ngày cố định, cần đối chiếu dữ liệu
trước khi dùng. GUI automation không thay nghiệm thu CRUD bằng chuột, Esc vật lý
hoặc DPI Windows 125%/150%; không tự đổi thiết lập màn hình để chạy test.
Các runner SQL hiện vẫn chỉ chấp nhận database test cũ; không trỏ chúng vào Final,
không đổi marker/whitelist để chạy được và không dùng sysadmin thay writer.


## Cập nhật luồng vé, dropdown và biểu đồ

- **TICKET được tạo thông qua luồng đặt vé tại quầy hoặc website** sau xác nhận,
  cùng BOOKING, BOOKING_DETAIL, INVOICE và INVOICE_DETAIL trong một transaction.
  Không có kho vé tạo trước, không thêm luồng Thêm vé khác hoặc INSERT vé độc lập.
- Tại quầy chọn sân bay đi/đến, ngày đi, số người → chuyến đi → hạng vé.
  Khứ hồi thêm ngày về → chuyến đảo tuyến sau giờ hạ cánh → hạng thuộc đúng lượt về.
  Chọn lại tuyến/ngày/số người xóa lựa chọn phụ thuộc, giá và ghế dự kiến.
  Dropdown không tự chọn chuyến/hạng để che trạng thái chưa có lựa chọn.
- Cấp ghế theo thứ tự hàng A–F, không Random. Bấm tính lại giá để xem từng ghế;
  giữ chế độ **Tự động chọn ghế** hoặc bỏ chọn để chọn ghế còn trống cho mỗi người/chặng.
  Xem ghế không giữ chỗ, bán vé hay ghi doanh thu. BUS tại quầy yêu cầu đủ ghế đã
  xác nhận; transaction từ chối ghế bị tranh, không tự đổi sang ghế khác.
- Web hiển thị ghế tự động dự kiến cho mỗi hành khách/chặng, lưu trong draft server.
  Khi báo ghế đã bị đặt, khách chủ động tính lại giá để chấp thuận ghế mới.
  DTO có Seats/ReturnSeats tùy chọn; payload idempotency bao gồm ghế nếu có.
  Yêu cầu cũ không có trường ghế giữ nguyên nội dung/hash, không thay hash đã lưu.
- **Đổi ghế** là thao tác riêng với **Sửa hành khách**: chỉ cùng chuyến, đơn UNPAID,
  vé ISSUED trước khởi hành. Kiểm tra ghế cũ để chặn phiên lưu đè; không đổi giá,
  hạng, số lượng hoặc trạng thái hóa đơn. CRUD CUSTOMER vẫn ở Quản lý khách hàng.
- Tìm vé hỗ trợ mã Booking, thêm Xóa tìm kiếm; Enter trong cả vùng lọc nâng cao
  chỉ lọc. Giá tìm kiếm là giá chứng từ lịch sử. Hủy đơn chứa vé tác động toàn
  BOOKING của chặng đó; Web hủy hành trình tác động cả hai chặng. Không DELETE
  vật lý vé đã liên kết chứng từ; hủy logic không được nhận là Delete vật lý.
- Hai tab biểu đồ ở Thống kê: cột doanh thu PAID chọn tháng/quý (bù kỳ trống), tròn
  cơ cấu **tiền** theo hạng vé (6 nhóm lớn + Khác). BUS tổng hợp từ đúng kết quả,
  bộ lọc đã tải; GUI chỉ vẽ bằng GDI+ có sẵn, không thêm dependency. Tooltip,
  nhãn tiền/phần trăm và bảng đối chiếu được giữ. Tổng 0 hiển thị trạng thái rỗng.
  Chưa có nguồn chi được duyệt nên chưa vẽ Thu–Chi hoặc nhận lợi nhuận hoàn chỉnh.

Kiểm tra GUI/quote chỉ đọc có guard riêng cho Final; không thay whitelist writer cũ:

```powershell
dotnet run --project tests/FlightBooking.WinFormsChecks -- --counter-readonly
```

Lượt kiểm tra này: Debug 0 lỗi/1 cảnh báo license; Release FAIL do ImageSharp;
ManagementChecks 152, BookingChecks 144, CounterFormChecks 16, Final GUI chỉ đọc 13,
hai bộ JavaScript PASS. GUI đã đối chiếu chuyến/hạng hai lượt và ghế dự kiến với
SQL riêng; tổng biểu đồ khớp tổng PAID từ INVOICE. Không ghi SQL hoặc dùng #82 làm
fixture. Ca tranh ghế, đổi ghế đồng thời, thanh toán, rollback/import SQL còn BLOCKED
vì chưa có writer/môi trường ghi được duyệt. Chưa kiểm thử DPI 125%/150%.

**Dữ liệu cần duyệt sửa riêng:** vé 122/chuyến 31 và vé 124/chuyến 32 có ghế `2A`
nhưng sức chứa chỉ 2/3 chỗ, vượt sơ đồ A–F hiện hành. Giữ nguyên dữ liệu Final;
BUS chặn đặt tiếp trên sơ đồ sai. Script seed mới đã đổi mapping sang A–F,
không chạy lại trên Final. Hiện vẫn giữ 82 BOOKING/142 TICKET và đơn #82 CONFIRMED.
Migration chi phí/tham số vẫn chỉ là đề xuất; module chi phí/tham số, sử dụng tham số
thực tế, biểu đồ Thu–Chi và báo cáo cuối kỳ chưa hoàn tất. Không tự coi đã đạt W7.

## Tiếp tục bản tích hợp

Nhánh làm việc: `feature/final-integration-fix`; giữ giao diện được bàn giao từ Hiếu.
Các thay đổi giao diện Noel cũ được giữ riêng trong stash local, không ghép vào nhánh này.

- **Vé → Đặt vé tại quầy**: chọn khách hàng đã có, nhân viên ACTIVE, chuyến/hạng
  và số người; nhập riêng từng hành khách. Khứ hồi chọn lượt về đảo chiều sau khi
  lượt đi hạ cánh. Tính lại giá rồi xác nhận; tạo BOOKING/BOOKING_DETAIL/TICKET/
  INVOICE/INVOICE_DETAIL trong transaction Booking dùng chung. Nguồn COUNTER,
  nhân viên được lưu; không tạo CUSTOMER mới. Hóa đơn UNPAID, chưa ghi doanh thu thu tiền.
- **Vé → Sửa hành khách**: chỉ tên/giấy tờ; yêu cầu đơn CONFIRMED, hóa đơn UNPAID,
  vé ISSUED và chuyến SCHEDULED chưa khởi hành. Hành trình hai lượt được sửa cùng
  transaction. Nếu liên kết theo dấu vân tay bị mơ hồ, từ chối sửa để kiểm tra lại;
  không tự suy đoán hành trình. Giữ nguyên khóa/hash yêu cầu gốc, giá, ghế và chuyến.
- **Vé / Hóa đơn → Tìm kiếm nâng cao**: bật từng điều kiện, chọn trường, phép so sánh
  và AND/OR/NOT; BUS lọc ArrayList. NOT nghĩa là AND NOT điều kiện tiếp theo.
  Bộ lọc từ khóa/trạng thái vẫn là điều kiện bắt buộc trước bộ lọc nâng cao.

Kiểm thử form tại quầy không mở SQL:

```powershell
dotnet run --project tests/FlightBooking.WinFormsChecks -- --counter-only
```

Script Final mới tạo 20 hành trình WEB khứ hồi và một hành trình một chiều;
payload hash khớp `DatVeBUS.NoiDungChongGuiLap`, khóa lượt về dùng đúng dẫn xuất
`return:`. Giảm giá chỉ áp dụng lượt đi. Giá theo chứng từ thấp hơn giá danh mục
để kiểm tra lịch sử; ngày lập chứng từ tương lai được chặn ở ngày seed.

**Đang chờ / chưa nghiệm thu:**

- Release đang bị ImageSharp 3.2.1 chặn vì thiếu license. Nâng NPOI 2.8.1 chỉ sau
  khi người dùng duyệt [EULA](https://www.nuget.org/packages/NPOI/2.8.1/License);
  không tắt license check hoặc hạ xuống gói có lỗ hổng chỉ để build.
- [Migration 05 đề xuất](Database/Final/05_ProposedFinanceParameters_ApprovalRequired.sql)
  gồm EXPENSE, EXPENSE_DETAIL, SYSTEM_PARAMETER và INVOICE.paid_at. Chưa áp dụng;
  chưa có module chi phí/tham số hay tổng chi/lợi nhuận. Cần nhóm duyệt mô hình
  và giảng viên xác nhận cách quy đổi phiếu nhập hàng sang chi phí vận hành.
- [Script hiệu chỉnh seed ban đầu](Database/Final/06_RepairInitialSeedDiscount_ApprovalRequired.sql)
  đã hiệu chỉnh riêng 20 lượt về mẫu sau khi review snapshot 82 đơn/142 vé/42 request;
  giữ nguyên đơn tại quầy mới #82, khóa/hash và trạng thái. Kiểm tra toàn vẹn sau
  hiệu chỉnh pass. Script chặn chạy lại và chặn nếu snapshot/trạng thái tài chính đổi.
  Bộ seed mới đã sửa trực tiếp quy tắc giảm giá cho những máy chưa tạo Final.
- Chưa nghiệm thu ghi SQL, sửa đồng thời hai lượt, rollback/concurrency hoặc import
  vào SQL: cần database test và writer riêng có quyền tối thiểu. Không dùng tài khoản
  sysadmin thay writer, không sửa whitelist/preflight của runner cũ để trỏ vào Final.
  [Script 07 đề xuất quyền](Database/Final/07_ProposedTestWriter_ApprovalRequired.sql)
  chỉ cấp SELECT/INSERT/UPDATE/DELETE trên 12 bảng ở FlightBooking_Final_Tests,
  CONNECT và VIEW DEFINITION cho tài khoản Windows riêng đã tồn tại; không tạo
  login/tài khoản Windows hoặc cấp quyền server/demo DB. Chưa thực thi script này.
- Chưa chốt báo cáo cuối kỳ DOC/PDF và chưa đưa PR sang Ready for review.

Kết quả offline đã kiểm tra: ManagementChecks 152, BookingChecks 132,
CounterFormChecks 16, hai bộ JavaScript. Đã xuất PDF bằng DTO giả cho ba trạng thái,
gồm mẫu CANCELLED 60 dòng/5 trang; đây không phải bằng chứng nghiệp vụ thanh toán SQL.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| [FlightBooking.DTO](src/FlightBooking.DTO/) | DTO/record, dữ liệu đầu vào và lỗi nghiệp vụ dùng chung. |
| [FlightBooking.DAO](src/FlightBooking.DAO/) | MyConnection, SQL tham số, đọc/ghi và transaction. |
| [FlightBooking.BUS](src/FlightBooking.BUS/) | QuanLyBUS, TimKiemQuanLy, DatVeBUS; validation và nghiệp vụ. |
| [FlightBooking.WinForms](src/FlightBooking.WinForms/) | Ba tab quản lý, ListView cha–con, editor và hộp thoại chọn. |
| [FlightBooking.Web](src/FlightBooking.Web/) | Razor Pages tìm/đặt/tra cứu/hủy, session và antiforgery. |
| [Database](Database/) | Script gốc của nhóm, bộ triển khai Final độc lập và TestPlan có guard riêng. |
| [tests](tests/) | C#, WinForms, HTTP, JavaScript và preflight/policy dùng chung. |
| [Docs](Docs/) | [ERD chung](Docs/flight_booking_erd_source.drawio). Hướng dẫn cài đặt/chạy hiện hành nằm trong README; tài liệu lịch sử đã dọn. |

MyConnection/Open, namespace và tên project giữ nguyên. Public API nghiệp vụ
đã đổi tên Việt; khi tích hợp nhánh thành viên cần cập nhật type/member tương ứng,
giữ hợp đồng HTTP/JSON và schema. Các module quản lý đã có trên develop được bảo toàn;
đã tích hợp thanh toán/thống kê/XLS/PDF của Quân trên nhánh final-integration-fix. Bảng tham số, báo cáo cuối kỳ W7 và
các kiểm thử thủ công còn thiếu vẫn cần nhóm hoàn tất.
