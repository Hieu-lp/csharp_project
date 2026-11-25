# BookingFlight — Quản lý bán vé máy bay

Đồ án C# .NET 10 dùng WinForms, ASP.NET Core Razor Pages và SQL Server.
Kiến trúc **GUI → BUS → DAO**, DTO truyền dữ liệu giữa các lớp; DAO dùng
SqlClient và truy vấn tham số hóa. Identifier nghiệp vụ dùng tiếng Việt không dấu.

Hiếu quản lý sân bay, chuyến bay, hạng vé và tìm kiếm ArrayList AND/OR/NOT.
Phú quản lý khách hàng, nhân viên và mã giảm giá. MainForm dùng một vùng nội dung
chung cho bảy mục: Tổng quan, Sân bay, Chuyến bay, Hạng vé, Khách hàng, Nhân viên,
Mã giảm giá. Ba tab của Hiếu và ListView cha–con được giữ; chuyển chức năng
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

Với **database mới được nhóm duyệt**, người phụ trách SQL review các script
trong [Database/](Database/), thực hiện 01 → 02 → 03 → 04; 07 là seed mở rộng
tùy chọn. Các script gốc có `USE FlightBookingDB` hardcode: đổi tham số `-d`
không đổi được đích bên trong. Không chạy chúng trên database đang có dữ liệu.

Booking cần bảng `BOOKING_WEB_REQUEST` cùng constraint/index của
[08_BookingWebRequest.sql](Database/08_BookingWebRequest.sql). Chỉ người phụ trách
triển khai vào đúng database sau khi nhóm phê duyệt; ứng dụng không tự sửa schema.
Không chạy lại seed 04/07 hoặc test ghi vào database chính.

## Chạy WinForms và website

Trong **mỗi terminal**, thay `YOUR_SQL_SERVER` bằng instance thực tế và chỉ chọn
database đã được cho phép. Tài khoản Windows hiện tại cần quyền SQL phù hợp.
Không cần account BookingFlightTest để chạy ứng dụng thông thường.

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=YOUR_SQL_SERVER;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
```

Terminal thứ nhất:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Terminal thứ hai, sau khi đặt cùng biến kết nối:

```powershell
dotnet run --no-build --no-launch-profile --project src/FlightBooking.Web -- --urls http://127.0.0.1:5087
```

Mở http://127.0.0.1:5087/. WinForms ưu tiên biến môi trường; có thể dùng
`connection.local.txt` cạnh executable làm fallback (file bị Git ignore).
Web đọc key `ConnectionStrings:FlightBookingDB`. Ví dụ trên dành cho phát triển
với chứng chỉ tự ký; môi trường triển khai cần chứng chỉ được tin cậy.
Các DAO quản lý từ develop cũng nhận biến này; `FLIGHTBOOKING_DB_CONNECTION`
được giữ làm fallback tương thích. WinForms khởi động qua MainForm hiện có.

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
$env:FLIGHTBOOKING_BROWSE_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.Checks -- --connection-settings
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout
& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl http://127.0.0.1:5087
```

Các lệnh này cần kết nối test và Web tương ứng; HTTP readonly chỉ quote/back/lookup,
không đặt/hủy. Các script HTTP demo cũ có ID/ngày cố định, cần đối chiếu dữ liệu
trước khi dùng. GUI automation không thay nghiệm thu CRUD bằng chuột, Esc vật lý
hoặc DPI Windows 125%/150%; không tự đổi thiết lập màn hình để chạy test.

## Cấu trúc

| Thư mục | Nội dung |
|---|---|
| [FlightBooking.DTO](src/FlightBooking.DTO/) | DTO/record, dữ liệu đầu vào và lỗi nghiệp vụ dùng chung. |
| [FlightBooking.DAO](src/FlightBooking.DAO/) | MyConnection, SQL tham số, đọc/ghi và transaction. |
| [FlightBooking.BUS](src/FlightBooking.BUS/) | QuanLyBUS, TimKiemQuanLy, DatVeBUS; validation và nghiệp vụ. |
| [FlightBooking.WinForms](src/FlightBooking.WinForms/) | Ba tab quản lý, ListView cha–con, editor và hộp thoại chọn. |
| [FlightBooking.Web](src/FlightBooking.Web/) | Razor Pages tìm/đặt/tra cứu/hủy, session và antiforgery. |
| [Database](Database/) | 8 script chính thức của nhóm và TestPlan có guard. |
| [tests](tests/) | C#, WinForms, HTTP, JavaScript và preflight/policy dùng chung. |
| [Docs](Docs/) | [ERD chung](Docs/flight_booking_erd_source.drawio) và tài liệu nghiệp vụ của các thành viên được giữ theo quyền sở hữu. |

MyConnection/Open, namespace và tên project giữ nguyên. Public API nghiệp vụ
đã đổi tên Việt; khi tích hợp nhánh thành viên cần cập nhật type/member tương ứng,
giữ hợp đồng HTTP/JSON và schema. Các module quản lý đã có trên develop được bảo toàn;
chưa bổ sung thanh toán/thống kê/xls/PDF của Quân. Bảng tham số, báo cáo cuối kỳ W7 và
các kiểm thử thủ công còn thiếu vẫn cần nhóm hoàn tất.
