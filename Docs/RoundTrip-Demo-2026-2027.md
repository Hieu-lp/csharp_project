# Dữ liệu thử khứ hồi và form hành khách — 21/11/2026

> Tài liệu nghiệp vụ của Thịnh được giữ theo quyền sở hữu. Hướng dẫn chạy hiện tại
> nằm trong [README](../README.md); số test/ID/ngày và các mô tả thay đổi bên dưới là lịch sử.

## Database và script

Hiện dùng `Database/04_SeedDemoData.sql` để khởi tạo các chuyến khứ hồi cùng dữ liệu gốc.
Các số liệu kiểm tra và ID bên dưới ghi nhận từ bản bổ sung cũ trên `RNR-LAPTOP / FlightBookingDB`;
không phải ID cố định của seed hiện tại. File bổ sung cũ đã được xóa.
Thời điểm SQL xác nhận theo giờ Việt Nam: 21/11/2026 01:14:20 (UTC+7).
Script dùng transaction, khóa seed và tra ID sân bay từ AIRPORT; không đổi schema/ERD,
không sửa seed cũ hoặc dữ liệu đã có. Không tạo sẵn Booking, khách, vé hay hóa đơn.

- Trước: 21 FLIGHT, 41 FARE_CLASS.
- Sau: 43 FLIGHT, 85 FARE_CLASS: thêm 22 chuyến và 44 hạng vé.
- Chạy lại: thêm 0 FLIGHT, 0 FARE_CLASS.
- CUSTOMER/BOOKING/TICKET/INVOICE giữ nguyên: 25 / 81 / 141 / 81.
- Các chuyến mới có 120 ghế, tickets_sold=0, seats_available=120, total_revenue=0.
- Economy 7 kg, Business 20 kg. Giá lấy theo tuyến/ngày trong script, lượt về hơn lượt đi 50.000đ.
- Trạng thái khi INSERT tính theo thời gian Việt Nam; chạy lại không cập nhật trạng thái
  hoặc giá/counters của dữ liệu đã tồn tại. Chặn đặt chuyến đã khởi hành vẫn nằm trong BUS.

## Các cặp để thử

SGN = Tân Sơn Nhất; HAN = Nội Bài; DAD = Đà Nẵng. Lượt về đảo chiều tuyến.
Mọi lượt đi: 07:15–09:25; mọi lượt về: 13:40–15:50, hạ cánh cùng ngày khởi hành.

| Tuyến lượt đi | Ngày đi | Ngày về | ID đi / về | Trạng thái hai lượt |
|---|---|---|---|---|
| SGN → HAN | 10/11/2026 | 12/11/2026 | 38 / 39 | COMPLETED / COMPLETED |
| SGN → HAN | 24/11/2026 | 27/11/2026 | 42 / 43 | SCHEDULED / SCHEDULED |
| DAD → HAN | 25/11/2026 | 28/11/2026 | 36 / 37 | SCHEDULED / SCHEDULED |
| SGN → DAD | 26/11/2026 | 29/11/2026 | 40 / 41 | SCHEDULED / SCHEDULED |
| SGN → HAN | 05/12/2026 | 08/12/2026 | 28 / 29 | SCHEDULED / SCHEDULED |
| DAD → HAN | 12/12/2026 | 15/12/2026 | 22 / 23 | SCHEDULED / SCHEDULED |
| SGN → DAD | 20/12/2026 | 23/12/2026 | 26 / 27 | SCHEDULED / SCHEDULED |
| SGN → HAN | 29/12/2026 | 03/01/2027 | 24 / 25 | SCHEDULED / SCHEDULED |
| SGN → HAN | 10/01/2027 | 13/01/2027 | 34 / 35 | SCHEDULED / SCHEDULED |
| DAD → HAN | 17/01/2027 | 20/01/2027 | 30 / 31 | SCHEDULED / SCHEDULED |
| SGN → DAD | 24/01/2027 | 27/01/2027 | 32 / 33 | SCHEDULED / SCHEDULED |

## Màn hình mới và ánh xạ

- Tiêu đề "Thông tin hành khách", đúng N khối Hành khách 1..N.
- Bỏ form khách đặt riêng và bỏ bộ chọn số người ở checkout. Muốn đổi số người,
  bấm lưu/quay lại tìm chuyến; chọn số mới ở đó rồi chọn lại chuyến/hạng.
- Hành khách 1 là người liên hệ: tên/giấy tờ chỉ nhập một lần ở
  `Input.Passengers[0]`, BUS ánh xạ sang CUSTOMER.full_name/id_passport_number.
- Email, điện thoại, ngày sinh, giới tính thực của người liên hệ nằm ngay trong
  khối Hành khách 1 và lưu CUSTOMER.email/phone/date_of_birth/gender.
- Giới tính bắt buộc tự chọn, không gán Khác mặc định. Nhãn Nữ dùng giá trị Nu theo seed.
- Tên/giấy tờ của từng người lưu BOOKING_DETAIL.passenger_full_name/passenger_id_card.
  Không tạo CUSTOMER cho từng người. Khứ hồi dùng cùng N người cho cả hai lượt.
- Không thêm ngày sinh hoặc giới tính cho hành khách 2..N vì schema chưa lưu các trường này.
- ModelState giữ thông tin lỗi, session draft giữ dữ liệu qua nút quay lại.
  Phía server đối chiếu hành trình/Quantity và số phần tử với nháp phiên,
  kiểm tra giấy tờ trùng, không tin một hidden Quantity đơn lẻ.

## Kiểm tra đã thực hiện

- Build riêng `tmp/contact-build`, thành công. Có cảnh báo NU1900 do không kết nối
  được dịch vụ audit NuGet; không thay cấu hình/tắt audit của dự án.
- 50 kiểm tra nghiệp vụ C# đạt; kiểm tra JavaScript chuyển một chiều/khứ hồi đạt.
- HTTP thật với Web kết nối FlightBookingDB: tìm được cả lượt đi và lượt về của
  10 cặp tương lai, gồm ba tuyến và cặp xuyên năm.
- HTTP xác nhận 1/2/3 người hiển thị đúng N khối, chỉ một email liên hệ,
  không có form tên khách đặt riêng hoặc bộ chọn số người thứ hai.
- Nhập ba tên/giấy tờ riêng: lỗi email vẫn giữ dữ liệu, nút lưu/quay lại trả về
  tìm chuyến và chọn lại khôi phục đủ thông tin.
- `--demo-rollback`: 9 kiểm tra SQL thật đạt. Với 1/2/3 người, DAO tạo riêng đúng
  N vé và chi tiết tên/giấy tờ/ghế cho từng lượt 42 và 43 trong cùng transaction;
  cố ý rollback và đối chiếu toàn bộ row counts/counters/doanh thu trước/sau.
  50 kiểm tra C# + 9 SQL = 59 kiểm tra đạt. Không giữ dữ liệu đặt thử.
- Đây là kiểm tra DAO lưu/rollback, **không phải xác nhận hoàn tất BUS.Create và
  luồng xác nhận Web đầy đủ**: FlightBookingDB chưa có BOOKING_WEB_REQUEST của script 08.
  Không tự chạy 08 vì yêu cầu giữ nguyên schema; cần nhóm duyệt/ap dụng phụ thuộc
  đã được đề xuất từ trước để dùng chống gửi lặp và tra cứu khi xác nhận thật.

## Khởi tạo dữ liệu và chạy kiểm tra

```powershell
# Chỉ chạy seed 04 trên database mới sau 01..03. Không chạy lại trên DB đã có dữ liệu.
sqlcmd -S RNR-LAPTOP -E -C -d FlightBookingDB -f 65001 -b -i Database/04_SeedDemoData.sql

dotnet build FlightBookingSystem.slnx --artifacts-path tmp/contact-build -m:1 --disable-build-servers -p:UseSharedCompilation=false
dotnet tmp/contact-build/bin/FlightBooking.Checks/debug/FlightBooking.Checks.dll
```

Cấu hình phát triển cục bộ đã được đặt trong appsettings.Development.json (được
.gitignore loại trừ), dùng Windows Authentication và đúng RNR-LAPTOP/FlightBookingDB.
Không có mật khẩu trong source. Khởi động lại website để nhận form mới.

Chỉ khi chủ động muốn kiểm tra SQL transaction rồi rollback:

```powershell
$env:FLIGHTBOOKING_DEMO_CONNECTION = 'Server=RNR-LAPTOP;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet tmp/contact-build/bin/FlightBooking.Checks/debug/FlightBooking.Checks.dll --demo-rollback
```

Không thay đổi phần Hiếu, không merge PR. Thay đổi có sẵn của người dùng trong
Database/02_CreateTables.sql được giữ nguyên.


## Điều chỉnh mới nhất: tách người đặt và hành khách

Theo yêu cầu mới, người đặt/liên hệ đã được tách thành khối riêng, không tự lấy
thông tin Hành khách 1. CUSTOMER lưu người đặt; BOOKING_DETAIL lưu từng người đi vé.
Chọn N người vẫn có đúng N khối hành khách; khối người đặt không tính vào số vé.
Thông tin người đặt và danh sách hành khách được giữ độc lập trong nháp và khi validation lỗi.
Không thay đổi database hoặc số lượng vé hai lượt. Phần mô tả lấy Hành khách 1 làm
người liên hệ ở báo cáo trước là lịch sử, không còn là hành vi hiện tại.

## Sửa lỗi đổi số người sau khi quay lại (21/11/2026)

Nút chọn chuyến trước đây là liên kết được render bằng số người của lần tìm cũ.
Đổi dropdown rồi bấm chọn ngay chưa gửi lại giá trị mới, nên checkout nhận quantity cũ.
Đã đổi nút chọn hạng/lượt về thành submit của form tìm chuyến hiện tại. Index xử lý
SelectedOffer, tìm/kiểm tra lại lựa chọn theo số người và chỗ hiện tại trước khi chuyển
bước; giữ thông tin lượt đi khi chọn lượt về và giữ DraftId qua các bước.
Không dựa vào JavaScript để cập nhật số người trong liên kết.

Build thành công (0 lỗi, 5 cảnh báo NU1900 do truy cập NuGet). Kiểm tra HTTP thật với
website kết nối SQL đạt 14 xác nhận: một chiều/khứ hồi 1→2→3, không cần bấm Tìm lại
trước khi chọn chuyến, đúng số khối và Quantity, tính lại giá, giữ hành khách/người đặt.
Không tạo Booking hoặc thay đổi dữ liệu SQL. Chương trình tái hiện:
`tests/FlightBooking.Checks/passenger-count.http.ps1` (các chuyến hiện có trong seed 04, ngày 24–27/11/2026).

## Cập nhật: dữ liệu đã có trong seed gốc (21/11/2026)

11 cặp chuyến ở bảng trên đã được đưa vào `Database/04_SeedDemoData.sql`.
Database mới chỉ cần chạy 01 → 02 → 03 → 04 để có các cặp khứ hồi; 07 là dữ liệu
mở rộng tùy chọn và không thêm trùng sân bay Đà Nẵng đã có từ 04.

Số lượng dự kiến sau 04: 3 AIRPORT, 23 FLIGHT, 45 FARE_CLASS; sau 04 + 07:
8 AIRPORT, 43 FLIGHT, 85 FARE_CLASS. Booking/vé/hóa đơn mẫu gốc vẫn giữ nguyên.
File bổ sung 09 đã được xóa; dữ liệu khứ hồi nằm trong seed gốc 04.
Không chạy lại 04/07 trên DB đã có dữ liệu. Các ID và kết quả chạy SQL ở phần trên
là lịch sử của lần chạy bản bổ sung cũ, không phải ID cố định của bản seed gốc mới.
