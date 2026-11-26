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
git switch develop
dotnet restore FlightBookingSystem.slnx
dotnet build FlightBookingSystem.slnx
```

Dừng đúng tiến trình Web/WinForms đang giữ DLL trước khi build nếu gặp lỗi file lock.
Không commit mật khẩu, access key, database backup hoặc cấu hình riêng của máy.

## Chuẩn bị database

Ứng dụng demo dùng **FlightBooking_Final**, độc lập với `FlightBookingDB` và
`FlightBooking_Hieu_ManagementTests`. Bộ script [Database/Final/](Database/Final/)
chỉ dùng 12 bảng hiện có, gồm `BOOKING_WEB_REQUEST`; chưa thêm chi phí/tham số.
Không chạy trực tiếp script gốc 01–08: chúng có `USE FlightBookingDB` hardcode.

DBA kiểm tra instance bằng `SELECT @@SERVERNAME`, rồi thay `YOUR_VERIFIED_SERVER_NAME`
và `-S .` theo máy mình. Script tạo database từ chối nếu đích đã tồn tại; khi lỗi
phải dừng và khảo sát, không DROP hoặc chạy lại mù quáng.

```powershell
sqlcmd -S . -E -C -d master -b -Q "SELECT @@SERVERNAME AS ServerName"
sqlcmd -S . -E -C -d master -b -l 15 -t 120 -i Database/Final/01_CreateAndSchema.sql -v EXPECTED_SERVER="YOUR_VERIFIED_SERVER_NAME" CREATE_APPROVAL="APPROVE_CREATE_FLIGHTBOOKING_FINAL"
```

Review số lượng demo trước khi seed: 8 sân bay, 25 khách, 7 nhân viên, 6 mã giảm giá,
43 chuyến, 85 hạng vé, 81 booking/hóa đơn, 141 vé/chi tiết và 41 yêu cầu WEB.
Chuyến tương lai dùng ngày tương đối; dữ liệu lịch sử có đủ bốn quý năm trước,
các trạng thái PAID/UNPAID/CANCELLED và hai kênh WEB/COUNTER. Đây là dữ liệu giả
để trình diễn, không phải bằng chứng chức năng thanh toán đã được kiểm thử.

Chỉ seed **một lần**, sau khi số lượng được duyệt, trên database hoàn toàn rỗng
và chưa từng sử dụng. Đóng Web/WinForms đang kết nối Final. Toàn bộ seed dùng
một transaction; file đầu ra có khóa tra cứu demo riêng, chỉ giữ local trong `tmp/`.

```powershell
New-Item -ItemType Directory -Path tmp -Force | Out-Null
sqlcmd -S . -E -C -d FlightBooking_Final -b -l 15 -t 120 -i Database/Final/02_SeedDemo.sql -v EXPECTED_SERVER="YOUR_VERIFIED_SERVER_NAME" SEED_APPROVAL="APPROVE_SEED_FLIGHTBOOKING_FINAL_DEMO" SEED_PROFILE="DEMO_8_25_7_6_43_85_81_141" -o tmp/final-demo-lookup.private.txt
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
| [Docs](Docs/) | [ERD chung](Docs/flight_booking_erd_source.drawio) và tài liệu nghiệp vụ của các thành viên được giữ theo quyền sở hữu. |

MyConnection/Open, namespace và tên project giữ nguyên. Public API nghiệp vụ
đã đổi tên Việt; khi tích hợp nhánh thành viên cần cập nhật type/member tương ứng,
giữ hợp đồng HTTP/JSON và schema. Các module quản lý đã có trên develop được bảo toàn;
chưa bổ sung thanh toán/thống kê/xls/PDF của Quân. Bảng tham số, báo cáo cuối kỳ W7 và
các kiểm thử thủ công còn thiếu vẫn cần nhóm hoàn tất.
