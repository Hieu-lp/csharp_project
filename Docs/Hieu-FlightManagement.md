# Phần Hiếu — AIRPORT, FLIGHT, FARE_CLASS

Nhánh `feature/flight-management`. Dựa trên phân công `D:\bookingflight\văn bản.txt`,
yêu cầu w4/w7 và tài liệu GUI–BUS–DAO. Chưa triển khai phần Phú, Quân hoặc thanh toán.

## Chức năng và kiến trúc

- Ba màn hình WinForms: Sân bay, Chuyến bay, Hạng vé; xem/thêm/sửa/xóa/tải lại.
- Lọc hai điều kiện AND/OR/NOT và =, <>, >, >=, <, <=, chứa; lọc ArrayList thật
  trong `ManagementSearch` của BUS. Giá dùng định dạng Việt Nam, ngày dd/MM/yyyy.
- Hai ListView FLIGHT → FARE_CLASS liên kết; chọn chuyến hiện các hạng tương ứng.
  Nút mở quản lý hạng vé chuyển sang màn hình hạng, lọc và điền đúng chuyến.
- Hộp thoại chọn sân bay/chuyến, tìm từ khóa, truyền lựa chọn bằng biến static;
  xóa lựa chọn cũ khi mở và khi đóng bằng Hủy/X.
- SQL chỉ nằm trong ManagementDAO; WinForms chỉ gọi ManagementBUS. Các thao tác
  dữ liệu chạy nền, có trạng thái bận, không thực hiện SQL trên luồng giao diện.
- ID tự sinh; vé đã bán, ghế còn và doanh thu chỉ đọc. Lỗi giữ vùng nhập liệu.

File chính:

| Lớp | File |
|---|---|
| DTO | `src/FlightBooking.DTO/BookingModels.cs`, `ManagementModels.cs` |
| DAO | `src/FlightBooking.DAO/MyConnection.cs`, `ManagementDAO.cs` |
| BUS | `src/FlightBooking.BUS/ManagementBUS.cs` |
| GUI | `src/FlightBooking.WinForms/Form1.cs`, `ManagementPage.cs`, `SelectionDialog.cs` |
| Kiểm tra | `tests/FlightBooking.ManagementChecks` |

## Bảo vệ dữ liệu đã đặt vé

- Sân bay bắt buộc tên/thành phố đúng độ dài. Không thêm trùng cặp tên/thành phố;
  đây là kiểm tra BUS/DAO, không thêm UNIQUE vào schema. Chặn xóa sân bay có chuyến.
- Ngày/giờ đến sau đi, hỗ trợ qua đêm/qua năm; không cấm quản lý chuyến lịch sử.
- Chuyến mới: đã bán/doanh thu bằng 0, ghế còn bằng tổng ghế.
- Sửa tổng ghế trong transaction SERIALIZABLE, khóa dòng FLIGHT bằng UPDLOCK/HOLDLOCK
  tương thích cách Booking khóa chuyến. So sánh bản ghi đã chọn với hiện trạng để
  từ chối sửa đè khi Booking hoặc người quản lý khác vừa thay đổi dữ liệu.
- Kiểm tra tổng = đã bán + còn; số vé ISSUED bằng đã bán. Giảm ghế không được làm
  mất vị trí đã cấp: ví dụ chỉ có vé 20F vẫn không giảm xuống 100 ghế. Quy ước sáu
  ghế A–F/hàng theo Booking; ghế không nhận diện được thì chặn giảm, không tự đổi ghế.
- Chuyến có bất kỳ Booking/vé: chặn đổi tuyến, lịch và trạng thái, kể cả CANCELLED.
  Không tự triển khai hủy hàng loạt, hoàn tiền hoặc quy trình đổi lịch.
- Chặn xóa chuyến có hạng/Booking/vé, không xóa dây chuyền.
- Hạng không trùng tên trên cùng chuyến, giá không âm và tối đa hai chữ số lẻ,
  hành lý không âm. Chặn xóa hạng có vé và chuyển hạng đó sang chuyến khác.
- Đổi giá danh mục không sửa BOOKING_DETAIL/INVOICE/INVOICE_DETAIL đã lưu.

## Tái sử dụng và tích hợp

Chỉ lấy hai file hạ tầng từ nhánh `feature/razor-booking` tại `e9283a2`:
`MyConnection.cs` và `BookingModels.cs`. Giữ nguyên định nghĩa AirportDTO, FlightDTO,
FareClassDTO và các DTO dùng chung trong cùng file để không tạo tên lớp trùng khi
tích hợp. `FlightManagementDTO` bọc FlightDTO, bổ sung số vé bán/doanh thu cho màn
hình quản lý. Không lấy handler, giao diện web hoặc logic Booking sang đây.
Đợt đồng bộ Database ngày 22/11/2026 lấy riêng các file 04, 07 và 08 từ nhánh web.

Khi tích hợp hai nhánh, giữ một bản MyConnection và BookingModels dùng chung;
nếu Git báo add/add ở hai file đó, đối chiếu và giữ phiên bản web đầy đủ mới nhất.
Sau khi người dùng xác nhận Booking đã merge qua PR #3, đã tích hợp
`origin/develop` tại `9ee83b9` vào nhánh Hiếu theo yêu cầu ngày 22/11/2026.
Không merge phần Hiếu ngược vào develop.

## Chạy WinForms

Từ thư mục chứa solution, mở PowerShell và đặt chuỗi kết nối của máy bạn:

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=YOUR_SERVER;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FlightBooking.WinForms
```

Hoặc đặt `connection.local.txt` cạnh file chạy, nội dung là chuỗi kết nối.
Biến môi trường được ưu tiên; file local này đã được ignore, không commit bí mật.
Không có cấu hình thì chương trình báo lỗi kết nối, không tự tạo/xóa database.

Chọn tab → Tải lại → chọn dòng để sửa; Thêm mới/Bỏ chọn để tạo mới; Lưu để ghi;
Xóa yêu cầu xác nhận và kiểm tra tham chiếu. Sau khi thành viên khác cập nhật,
dùng Tải lại. Hạng vé có bộ lọc theo chuyến; cửa sổ chọn chuyến chỉ điền vùng sửa,
không tự thay đổi danh mục khi chưa bấm Lưu.

## Kiểm tra

```powershell
dotnet build FlightBookingSystem.slnx --artifacts-path tmp/hieu-build -m:1 --disable-build-servers -p:UseSharedCompilation=false
dotnet tmp/hieu-build/bin/FlightBooking.ManagementChecks/debug/FlightBooking.ManagementChecks.dll
# Chỉ dùng database riêng có tên kết thúc bằng _ManagementTests:
$env:FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION = 'YOUR_TEST_CONNECTION'
dotnet tmp/hieu-build/bin/FlightBooking.ManagementChecks/debug/FlightBooking.ManagementChecks.dll --sql
```

Runner SQL từ chối database thường; nếu database test mới trống, chỉ tạo schema
02/03 trong database đó, không chạy seed 04/07. Tạo dữ liệu thử với ID thực tế,
chỉ dọn các ID do lần chạy đó tạo. Không xóa/reset database đang dùng.

Kết quả build/SQL/giao diện cuối cùng được ghi ở mục bàn giao bên dưới.

## Review ERD và SQL

Đã đọc cả ERD tại D: và ERD trong Docs. AIRPORT, FLIGHT, FARE_CLASS có cùng trường
và quan hệ chính với SQL; SQL còn ràng buộc ghế, thời gian, trạng thái và UNIQUE
(flight_id,class_name), FK ghép TICKET(fare_id,flight_id) để tránh vé sai chuyến.

Các chênh lệch có sẵn cần đồng bộ bản vẽ, chưa tự thay đổi:

- AIRPORT.airport_name trong ERD là varchar(150), SQL dùng nvarchar(150).
- CUSTOMER.full_name trong ERD là varchar, SQL dùng nvarchar; gender bản Docs
  còn varchar nhưng bản D: và SQL là nvarchar.
- TICKET.ticket_id ở bản Docs ghi nhầm `in`, bản D: thiếu kiểu; SQL là int.
- BOOKING.total_amount bản Docs thiếu precision; SQL và bản D: là decimal(18,2).
- Một số datetime trong ERD được triển khai bằng datetime2(0) trong SQL.
- SQL cho phép max_discount_amount/usage_limit NULL theo cách triển khai đã có.

Script 08 là phần mở rộng chống đặt trùng/khóa tra cứu, ngoài 11 bảng ERD gốc.
Đã kiểm tra script trên nhánh web: PK request_hash, UNIQUE booking_id, FK BOOKING,
không sửa dữ liệu cũ và có kiểm tra tồn tại. Bảng đã có trong database local,
không áp dụng lại. Bản vẽ ERD chung cần nhóm bổ sung bảng này theo phê duyệt trước.

Nhánh web hiện đã có dữ liệu khứ hồi trong seed 04 và chỉnh seed 07; file 09 không
còn trong cây nhánh đó. Theo yêu cầu đồng bộ ngày 22/11/2026, đã chép riêng
04_SeedDemoData.sql, 07_SeedExpandedDemoData.sql và 08_BookingWebRequest.sql sang
nhánh Hiếu, kiểm tra nội dung khớp nhánh web. Không tạo lại 09, không chạy SQL/seed
vào database đang dùng. File 02 giữ nguyên: khác nhánh web ở chú thích, không khác schema.

## Phối hợp tiếp

- Thịnh: giữ cùng kiểu DTO/kết nối, khóa FLIGHT khi đặt/hủy; khi tích hợp chạy lại
  luồng đặt/hủy để kiểm tra sự phối hợp thực tế giữa hai ứng dụng.
- Phú: thay/ghép điều hướng tối thiểu ba tab vào MainForm chung nếu cần.
- Quân: thanh toán, trạng thái vé, hóa đơn/doanh thu thuộc phần Quân; chưa cung cấp
  thao tác hủy chuyến có đơn hoặc đổi vé để tránh làm sai dữ liệu liên quan.
- Không tự thêm cột/bảng/hạn mức ghế theo hạng; schema và dữ liệu chính giữ nguyên.

## Bàn giao ngày 22/11/2026

- Build toàn solution thành công: 0 lỗi, 5 cảnh báo NU1900 do không lấy được
  dữ liệu kiểm tra lỗ hổng NuGet; chưa xác minh an toàn gói từ nguồn NuGet.
- 31 kiểm tra C# và 25 kiểm tra SQL = 56 đạt trên database riêng
  RNR-LAPTOP/FlightBooking_Hieu_ManagementTests. Có CRUD, dữ liệu không hợp lệ,
  hạng trùng, khóa ngoại, tham chiếu lịch sử, ghế 20F và hai chỉnh sửa đồng thời.
- Dữ liệu tạo cho SQL test đã dọn theo ID của lần chạy. Không ghi hoặc reset
  FlightBookingDB chính; chỉ SELECT để xác nhận script 08 đã có.
- WinForms thực tế đã kiểm tra tải dữ liệu 8 sân bay/43 chuyến, lọc Nội Bài ra
  1 kết quả, validation sân bay, chọn chuyến hiện hai hạng tương ứng, điều hướng
  hạng theo chuyến, cửa sổ chọn chuyến điền form, Hủy chọn sân bay giữ nguyên.
- Chưa bấm toàn bộ thao tác CRUD ghi trên GUI; CRUD được kiểm tra qua BUS/DAO
  trên SQL riêng. Chưa chạy đặt vé website đồng thời với WinForms sau khi merge;
  kiểm tra đồng thời hiện là hai yêu cầu chỉnh sửa qua BUS.
- Ảnh giao diện kiểm tra lưu local tại tmp/hieu-winforms.png, không commit tmp.

## Kiểm tra sau khi tích hợp develop — 22/11/2026

- Đã lưu phần Hiếu tại commit `6c29a9b` trước khi tích hợp `origin/develop`.
- Xử lý ba xung đột .gitignore, solution, README bằng cách giữ nội dung của cả
  hai phần; MyConnection, DTO và Database 04/07/08 đã giống nhau, không bị nhân đôi.
- Solution hiện chứa cả website Booking, WinForms quản lý và hai project kiểm thử.
- Build tại tmp/hieu-integrated: 0 lỗi, 12 cảnh báo NU1900 trong lượt có restore
  (nguồn audit NuGet chưa truy cập được).
- 82 kiểm tra Booking C#, 56 kiểm tra quản lý (gồm SQL trên database test riêng),
  các kiểm tra JavaScript và 32 kiểm tra HTTP đều đạt.
- HTTP xác nhận 1/2/3 khách, đúng form, tìm/chọn khứ hồi, giữ dữ liệu, bộ lọc,
  áp/đổi/xóa mã, xem trước đồng thời và validation người đặt trên bản tích hợp.
- Hai máy chủ HTTP kiểm tra đã dừng. Số Booking, vé và tổng lượt dùng mã trong
  database chính không thay đổi trước/sau thử nghiệm. Không chạy seed hoặc tạo
  Booking thử vào database chính; chưa kiểm thử đặt vé đồng thời với sửa chuyến.
- Diff nhập từ develop có dòng trống cuối file sẵn có ở Database/02_CreateTables.sql,
  deals.http.ps1 và passenger-count.http.ps1; giữ nguyên nội dung các file đó.
