# FlightBookingSystem — Báo cáo tiến độ phần Thịnh

**Đề tài:** Quản lý bán vé máy bay

**Người phụ trách:** Thịnh — BOOKING, BOOKING_DETAIL và website Razor Pages

**Ngày cập nhật:** 20/11/2026

**Trạng thái:** Đã triển khai code và kiểm tra C#; chưa nghiệm thu luồng SQL Server thực tế.

## 1. Công nghệ và kiến trúc

- C# .NET 10, ASP.NET Core Razor Pages, WinForms và SQL Server.
- Mô hình GUI → BUS → DAO; DTO truyền dữ liệu giữa các lớp.
- PageModel chỉ gọi BUS; BUS kiểm tra nghiệp vụ; DAO thực hiện SQL có tham số.

```text
FlightBookingSystem.slnx
├── Database/                 Script SQL, dữ liệu mẫu và kiểm tra
├── Docs/                     ERD và tài liệu bàn giao
├── src/
│   ├── FlightBooking.DTO/    Dữ liệu Booking, hành khách và thông tin tích hợp
│   ├── FlightBooking.DAO/    MyConnection, truy cập dữ liệu và transaction
│   ├── FlightBooking.BUS/    Nghiệp vụ, tính tiền và lọc ArrayList
│   ├── FlightBooking.Web/    Website Razor Pages tiếng Việt
│   └── FlightBooking.WinForms/  Khung ứng dụng quản lý của nhóm
└── tests/FlightBooking.Checks/  Kiểm tra C# và kiểm tra SQL tùy chọn
```

## 2. Hiện trạng trước khi thực hiện

Nhánh `develop` tại commit `893e751` đã có Database gồm 11 bảng, ERD, dữ liệu mẫu,
script kiểm tra và khung 5 project .NET 10 được merge qua
[PR #2](https://github.com/Hieu-lp/csharp_project/pull/2).
DTO/DAO/BUS còn lớp mẫu trống; Web và WinForms chưa có chức năng nghiệp vụ.

## 3. Nội dung đã triển khai cho Thịnh

| Phần | Nội dung |
|---|---|
| DTO, DAO, BUS | Booking và BookingDetail; các lớp dữ liệu tích hợp tối thiểu phục vụ đặt vé |
| Kết nối | MyConnection trong DAO; cấu hình kết nối bên ngoài source |
| Tìm chuyến | Nơi đi, nơi đến, ngày đi, số hành khách; chọn chuyến và hạng vé |
| Tìm nâng cao | Lọc ArrayList trong BUS, hỗ trợ AND/OR/NOT và =, <>, >, >=, <, <= |
| Thông tin vé | Giá, hành lý, ngày/giờ đi và đến riêng biệt, số chỗ còn của toàn chuyến |
| Khách đặt và hành khách | Biểu mẫu tiếng Việt, kiểm tra dữ liệu phía server và giấy tờ trùng trong đơn |
| Giảm giá | Kiểm tra trạng thái, thời hạn, đơn tối thiểu, giới hạn lượt và mức giảm tối đa |
| Đặt vé | Tính lại tiền trên server; tạo Booking, vé, chi tiết và hóa đơn UNPAID trong một transaction |
| Tra cứu | Mã đơn kết hợp khóa tra cứu riêng; hiển thị hành khách, vé, ghế và số tiền |
| Hủy đơn | Xác minh khóa, xác nhận hủy; đồng bộ trạng thái và hoàn ghế/lượt mã trong transaction |
| Bảo vệ dữ liệu | Chống gửi lặp bằng khóa bền vững, khóa dữ liệu khi cấp ghế, giữ index chống trùng ghế; không ghi doanh thu khi mới đặt |
| Giao diện | Bố cục responsive; thông báo lỗi, bảo vệ CSRF và giới hạn tần suất request |

Đây là các chức năng **đã có code**. Những phần ghi/đọc SQL chưa được xác nhận
chạy đạt trong môi trường thực tế; không dùng build thành công để thay thế nghiệm thu nghiệp vụ.

## 4. Kết quả kiểm tra

| Hạng mục | Kết quả đã ghi nhận |
|---|---|
| Build toàn solution | Thành công, 0 lỗi, 0 cảnh báo |
| Kiểm tra nghiệp vụ C# | 30 kiểm tra đạt: giảm giá, làm tròn tiền, cấp ghế, dữ liệu sai, ArrayList và điều kiện hủy |
| Giao diện | Đã xem trang chủ/tra cứu trên desktop và khung 375 px, 812 px; không tràn ngang |
| Biểu mẫu | POST thiếu antiforgery bị chặn HTTP 400; thiếu khóa đặt vé được xử lý không gây lỗi 500 |
| Quyền truy cập | Chi tiết đơn chưa xác minh chuyển về trang tra cứu |
| SQL Server và luồng đặt/hủy đầy đủ | **Chưa chạy** — chưa xác nhận instance và cấu hình kết nối |
| Tranh chấp ghế/lượt mã, gửi lặp, hủy lặp, rollback trên SQL | Đã viết chương trình kiểm tra; **chưa thực thi** |

## 5. Database và các giả định cần duyệt

- Giữ nguyên 11 bảng gốc, script 01–07 và ERD trong repository.
- Có đề xuất riêng [08_BookingWebRequest.sql](Database/08_BookingWebRequest.sql)
  để lưu hash khóa yêu cầu, hash nội dung và Booking tương ứng. Mục đích là chống
  đặt trùng bền vững và xác minh tra cứu; không nhét dữ liệu kỹ thuật vào cột nghiệp vụ.
- **Cần Hiếu review và đồng ý trước khi áp dụng script 08 lên database chung.**
  Website không tự chạy migration; thiếu bảng này thì chưa thể đặt/tra cứu đơn mới.
- Ghế nằm trong TICKET; không giả định BOOKING_DETAIL có seat_number.
  FARE_CLASS chưa có hạn mức chỗ riêng, nên hiển thị số chỗ toàn chuyến.
- Tạm áp dụng 1–9 khách/đơn, một chuyến và một hạng vé; tự cấp ghế theo hàng A–F.
- Chỉ hủy online đơn chưa thanh toán, trước giờ khởi hành. Đơn PAID cần nhân viên
  xử lý hoàn tiền; chưa có quy tắc tự hết hạn giữ chỗ hoặc cổng thanh toán.
- Thuế bổ sung tạm bằng 0; giờ nghiệp vụ là giờ Việt Nam. Các giả định này cần nhóm thống nhất.

## 6. Đối chiếu yêu cầu và phân công

| Yêu cầu | Phần Thịnh / phần cần phối hợp |
|---|---|
| Database đúng, mô hình 3 lớp, MyConnection | Đã bám schema và phân lớp; Hiếu review SQL/ERD và đề xuất 08 |
| CRUD và tìm kiếm | Thịnh có tạo/xem/tìm/hủy Booking; không xóa lịch sử hoặc sửa vé đã cấp tùy ý. CRUD danh mục thuộc Hiếu/Phú |
| Tìm kiếm ArrayList trong BUS | Đã triển khai bộ lọc nâng cao |
| Form cha–con, hai ListView, nút chọn dùng static | Thuộc WinForms của các thành viên; không áp static dùng chung cho dữ liệu khách trên Web |
| Khách hàng, nhân viên, mã giảm giá | Phú quản lý; Web chỉ tích hợp phần cần thiết để đặt vé |
| Vé, hóa đơn, thanh toán, thống kê, PDF và Excel | Quân phụ trách; Thịnh tích hợp tạo vé/hóa đơn chưa thanh toán và đồng bộ khi hủy |
| Table tham số và các yêu cầu thống kê ngoài schema hiện có | Nhóm cần thống nhất phạm vi với giảng viên; chưa tự thêm bảng nghiệp vụ |
| Báo cáo cuối kỳ | README này là báo cáo tiến độ phần Thịnh, chưa thay thế báo cáo toàn nhóm |

Đối chiếu chi tiết tài liệu w4, w7, mô hình 3 lớp và chênh lệch ERD–SQL tại
[Docs/Thinh-Booking.md](Docs/Thinh-Booking.md). Không kết luận toàn đồ án đã hoàn thành.

## 7. Nhánh và commit

Nhánh triển khai: `feature/razor-booking`, xuất phát từ `develop` tại `893e751`.

| Commit | Nội dung |
|---|---|
| `b3c43f7` | Hạ tầng MyConnection dùng chung, tách riêng để Hiếu tái sử dụng |
| `19df671` | DTO/DAO/BUS, transaction đặt/hủy và đề xuất script 08 |
| `d42e6c4` | Website tiếng Việt, đặt vé và tra cứu riêng tư |
| `622f03d` | Kiểm tra trọng tâm, xử lý thiếu khóa và tài liệu bàn giao |

Các commit trên hiện ở máy cục bộ; lần push gần nhất chưa thực hiện được do
quyền chạy bị từ chối. **Chưa có PR cho phần Booking.** PR #2 chỉ là khung solution.
Nhánh `feature/flight-management` đã chuẩn bị nền cục bộ từ cùng phiên bản develop;
**chưa triển khai phần Hiếu**, chờ Thịnh xác nhận bắt đầu giai đoạn 2.

Các file chính:

- [BookingModels.cs](src/FlightBooking.DTO/BookingModels.cs)
- [MyConnection.cs](src/FlightBooking.DAO/MyConnection.cs)
- [BookingDAO.cs](src/FlightBooking.DAO/BookingDAO.cs)
- [BookingBUS.cs](src/FlightBooking.BUS/BookingBUS.cs)
- [Pages](src/FlightBooking.Web/Pages/) và [Program.cs](src/FlightBooking.Web/Program.cs)
- [Kiểm tra nghiệp vụ](tests/FlightBooking.Checks/) và [tài liệu bàn giao](Docs/Thinh-Booking.md)

## 8. Cách chạy và kiểm tra

Yêu cầu: Windows, .NET SDK 10 và SQL Server/LocalDB. Chạy lệnh tại thư mục
`csharp_project` chứa `FlightBookingSystem.slnx`.

### Build và kiểm tra C#

Trước khi build, dừng website đang chạy bằng `Ctrl+C` tại Terminal đã mở web
(hoặc Stop Debugging trong Visual Studio). Nếu gặp MSB3021/MSB3027 kèm thông báo
DLL đang bị .NET Host khóa, dừng đúng tiến trình website rồi build lại.

```powershell
dotnet build FlightBookingSystem.slnx
dotnet run --project tests/FlightBooking.Checks
```

### Cấu hình website

1. Với database mới, chạy script 01 → 02 → 03 → 04 trong `Database/`;
   có thể thêm dữ liệu mẫu bằng 07. Không chạy lại seed 04/07 trên dữ liệu đã có.
2. Nhờ Hiếu duyệt script 08 trước khi áp dụng database chung.
3. Thay tên instance trong ví dụ sau bằng server thực tế trên máy:

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=.\SQLEXPRESS;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FlightBooking.Web --launch-profile https
```

Mở `https://localhost:7268` theo cấu hình launch profile hiện có, hoặc địa chỉ
hiển thị trong Terminal. `TrustServerCertificate=True` chỉ dành cho phát triển
với chứng chỉ tự ký; triển khai cần chứng chỉ tin cậy. Không commit mật khẩu hay
connection string bí mật. Không thêm tham số URL của MySQL vào SqlClient.

Khi database đã sẵn sàng, kiểm tra: **tìm chuyến → chọn hạng → nhập khách/hành khách
→ áp mã → xác nhận → lưu mã đơn và khóa → tra cứu → hủy → hủy lại**.
Dữ liệu mẫu có chuyến SGN → HAN ngày 15/12/2026 và mã DEMO10; kiểm tra lại nếu dữ liệu đã thay đổi.

### Kiểm tra SQL trên database riêng

Tạo database kiểm thử rỗng có tên kết thúc `_BookingTests`, không dùng dữ liệu thật:

```powershell
$env:FLIGHTBOOKING_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=FlightBooking_BookingTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project tests/FlightBooking.Checks -- --sql
```

Chương trình áp schema/index/script 08 vào database kiểm thử, tạo dữ liệu giả và
kiểm tra đặt/hủy, quyền truy cập, giá bị sửa, gửi lặp, rollback, tranh chấp ghế
và lượt giảm giá. Không tạo/xóa database; dữ liệu kiểm thử được giữ để đối chiếu.

## 9. Việc cần hoàn tất trước khi nghiệm thu

1. Xác nhận SQL Server instance và cấu hình kết nối.
2. Hiếu review đề xuất script 08 và quy ước ghế.
3. Chạy kiểm tra SQL cùng luồng Web đầy đủ; sửa lỗi nếu phát hiện.
4. Chốt quy tắc hủy, giữ chỗ, thuế và phối hợp thanh toán với Quân/khách hàng với Phú.
5. Push nhánh cá nhân, tạo PR vào develop và nhờ thành viên review; không tự merge.
6. Chỉ bắt đầu phần Hiếu khi Thịnh xác nhận; chưa làm WinForms quản lý sân bay/chuyến bay/hạng vé.

## Cập nhật hành khách / khứ hồi (21/11/2026)

Đã bổ sung chọn một chiều/khứ hồi, chọn riêng hai lượt, giá từng lượt/tổng hành
trình và các biểu mẫu hành khách độc lập theo đúng số vé; có lưu nháp khi quay
lại/cập nhật số người. Khứ hồi lưu hai Booking trong cùng transaction, tái dùng
bảng khóa yêu cầu của script 08, không thêm bảng/cột mới cho khứ hồi.
Giới tính chỉ nhập cho khách đặt (CUSTOMER); schema chưa lưu giới tính hành khách.

Bản cập nhật build thành công (0 lỗi, 5 cảnh báo NuGet NU1900), 48 kiểm tra C#,
10 kiểm tra HTTP và kiểm tra script chuyển loại hành trình đạt. Chưa chạy các
kiểm tra ghi SQL/đặt hai lượt thực tế; đã xác nhận FlightBookingDB thiếu bảng
BOOKING_WEB_REQUEST của script 08. Xem phần cập nhật cuối
[Docs/Thinh-Booking.md](Docs/Thinh-Booking.md) để biết ánh xạ trường và giới hạn kiểm chứng.


## Chuyến mẫu và form hành khách (21/11/2026)

Đã thêm 11 cặp khứ hồi tháng 11/2026, 12/2026, 01/2027 vào database local
RNR-LAPTOP/FlightBookingDB bằng seed 09, chạy lại không thêm trùng. Checkout chỉ
có N khối hành khách, Hành khách 1 kiêm người liên hệ; đổi số người tại tìm chuyến.
Xem [bảng ID/ngày giờ chuyến và kết quả kiểm tra](Docs/RoundTrip-Demo-2026-2027.md).


## Điều chỉnh mới nhất: tách người đặt và hành khách

Theo yêu cầu mới, người đặt/liên hệ đã được tách thành khối riêng, không tự lấy
thông tin Hành khách 1. CUSTOMER lưu người đặt; BOOKING_DETAIL lưu từng người đi vé.
Chọn N người vẫn có đúng N khối hành khách; khối người đặt không tính vào số vé.
Thông tin người đặt và danh sách hành khách được giữ độc lập trong nháp và khi validation lỗi.
Không thay đổi database hoặc số lượng vé hai lượt. Phần mô tả lấy Hành khách 1 làm
người liên hệ ở báo cáo trước là lịch sử, không còn là hành vi hiện tại.

## Bộ lọc khách hàng và thẻ chuyến bay giá tốt (21/11/2026)

Đã thay bộ lọc kỹ thuật bằng khoảng giá VND và hạng vé thực tế; thêm tối đa bốn
chuyến giá thấp, bấm thẻ mở đúng chuyến/hạng và giữ số khách. Đổi số người tự
kiểm tra ghế/tổng tiền, chuyển Checkout đúng số biểu mẫu. Không thay đổi Database.
Build 0 lỗi; 87 kiểm tra C#/SQL chỉ đọc và 20 kiểm tra HTTP đạt; đã xem desktop/mobile.
Xem [báo cáo review, giới hạn kiểm tra và vị trí thay ảnh](Docs/Booking-Filters-Deals.md).

## Ràng buộc người đặt và sửa Enter (21/11/2026)

Đã bắt buộc người đặt qua sinh nhật thứ 18 và có CCCD/hộ chiếu; áp lại cùng mã
không giảm chồng, đổi/xóa mã cập nhật trạng thái. Enter trong thông tin chỉ xem
lại giá; Enter ở ô mã áp mã, không quay về chọn chuyến hoặc tự đặt. Giữ dữ liệu
và cả hai lượt khi lỗi. Build 0 lỗi; 82 kiểm tra C#, 12 HTTP và JavaScript đạt;
đã kiểm tra Enter trên trình duyệt, lượt dùng mã SQL không tăng khi xem giá.
Chi tiết và giới hạn kiểm tra ở cuối [báo cáo Booking](Docs/Thinh-Booking.md).

## Seed khứ hồi đã gộp vào dữ liệu gốc (21/11/2026)

`Database/04_SeedDemoData.sql` đã bao gồm 11 cặp đi–về (22 chuyến, 44 hạng vé)
trong tháng 11/2026, 12/2026 và 01/2027, cùng sân bay Đà Nẵng cần cho các tuyến này.
Database mới chạy 01 → 02 → 03 → 04 là có đủ các cặp trên; không cần chạy 09.
Sau 04 có 23 chuyến, 45 hạng vé và 3 sân bay. Seed mở rộng 07 chỉ thêm sân bay
còn thiếu; sau 04 + 07 có 43 chuyến, 85 hạng vé và 8 sân bay.

04 và 07 vẫn chỉ chạy một lần trên database mới/đúng bước, không chạy lại trên DB
đã có dữ liệu. `09_SeedRoundTripFlights.sql` được giữ làm bản bổ sung cho DB dùng
seed cũ; file này kiểm tra trùng trước khi thêm. ID chuyến/hạng vé được sinh tự động,
không dùng các ID ghi nhận từ DB cũ để giả định ID trên máy mới.
