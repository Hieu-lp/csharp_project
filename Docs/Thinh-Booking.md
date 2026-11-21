# Phần Thịnh — Booking và Razor Pages

## Hiện trạng trước thay đổi

`develop` tại `893e751` đã merge PR #2: 11 bảng SQL, ERD, seed, các kiểm tra
database và solution .NET 10 gồm 5 project. DTO/DAO/BUS chỉ có Class1 trống;
Web và WinForms là mẫu mặc định. Không có luồng nghiệp vụ Booking.

Không tìm thấy AGENTS.md trong workspace/repository. Đã đối chiếu README, script
01–07, ERD trong Docs, tài liệu phân công và các tài liệu tại `D:\bookingflight`:
`C-w4-YeuCauDoAn.pdf`, `C-w7-YeuCauDoAn.pdf`, `c-3-layer-.pdf`,
`flight_booking_erd.drawio`, `văn bản.txt`. Không chép các PDF của giảng viên vào source.

## Phần triển khai

- DTO cho Booking, BookingDetail, đầu vào khách đặt/hành khách và dữ liệu tích hợp.
- `BookingBUS`: kiểm tra dữ liệu, tính lại giá/giảm giá, tìm kiếm nâng cao trên
  ArrayList, cấp ghế, điều phối transaction đặt/hủy và kiểm tra quyền tra cứu.
- `BookingDAO`/`BookingData`: SQL có tham số, quản lý transaction và truy cập
  Booking/BookingDetail cùng dữ liệu cần thiết. Chi tiết Booking được quản lý
  trong cùng aggregate/transaction, không có màn hình sửa độc lập từng chi tiết.
- `MyConnection`: dùng chung, không lưu mật khẩu trong source. Được tách thành
  commit hạ tầng riêng để Hiếu tái sử dụng chọn lọc.
- Razor Pages tiếng Việt: tìm chuyến/hạng vé, nhập hành khách, áp mã, xác nhận,
  kết quả có vé/ghế, tra cứu có khóa riêng và hủy có xác nhận.
- PageModel chỉ gọi BUS. Composition root đăng ký BUS, không cho PageModel chứa SQL.
- CSRF của Razor Pages; khóa ngẫu nhiên 256 bit; chỉ lưu SHA-256 trong SQL;
  khóa không đi trong URL; giới hạn 30 request/phút/IP cho Razor Pages.
  Trang chứa thông tin đặt vé dùng no-store và no-referrer.

## Transaction và tính nhất quán

Đặt vé chạy trong một SqlTransaction Serializable: khóa yêu cầu bằng sp_getapplock,
kiểm tra yêu cầu cũ, khóa bản ghi chuyến bay, kiểm tra hạng vé cùng chuyến, đọc và
khóa mã giảm giá, kiểm tra ghế, cập nhật số chỗ/lượt mã, tạo khách liên hệ, Booking,
Ticket, BookingDetail, Invoice, InvoiceDetail và bản ghi chống lặp rồi commit.
Lỗi bất kỳ sẽ dispose transaction chưa commit và rollback toàn bộ. Khóa yêu cầu
và bảng bền vững giúp hai lần gửi cùng khóa trả về cùng Booking cả sau khởi động lại.
Một khóa dùng cho nội dung khác bị từ chối. Biểu mẫu mới có khóa mới là đơn mới.

Chỉ cấp vé ISSUED khi còn chỗ. Kiểm tra số vé hiệu lực khớp counters trước khi ghi;
giữ index UX_TICKET_ActiveSeat làm hàng rào cuối cho trùng ghế. Cùng chuyến được
tuần tự hóa bởi khóa cập nhật FLIGHT. Những phần mềm khác trong nhóm cũng phải
duy trì counters và trạng thái trong cùng transaction, cùng thứ tự khóa.

Hủy xác minh khóa, khóa chuyến và Booking/Invoice, chỉ chuyển CONFIRMED sang
CANCELLED một lần. Đồng bộ Ticket/Invoice, hoàn ghế và lượt mã trong một transaction.
Hủy lại trả về thành công mà không hoàn thêm. Hóa đơn PAID bị từ chối hủy online.
Tạo Booking không ghi `FLIGHT.total_revenue`; mới tạo Invoice UNPAID.

Giá gửi từ browser chỉ là mức khách đã xem để phát hiện thay đổi; giá chính thức
và mức giảm luôn lấy/tính lại trên server. Nếu khác, yêu cầu khách tính lại giá.

## Đề xuất SQL cần người phụ trách Database duyệt

`08_BookingWebRequest.sql` thêm duy nhất bảng kỹ thuật BOOKING_WEB_REQUEST:
request_hash, payload_hash, booking_id (FK và UNIQUE). Không sửa/xóa dữ liệu hoặc
cột của 11 bảng gốc. Khóa chống lặp cần lưu bền vững, không thể thay bằng bộ nhớ
web hoặc nhét vào tên/giấy tờ khách hàng. Đây là thay đổi đề xuất trong PR,
**chưa áp dụng lên database chung**; cần Hiếu đồng ý trước khi triển khai.
Không chạy migration tự động khi web khởi động.

Booking tạo trước khi có bảng này không có khóa tra cứu riêng; cần nhân viên
xử lý trong phần quản lý của nhóm. Không mở tra cứu công khai chỉ bằng ID.

## Đối chiếu ERD và SQL

| Điểm | SQL hiện có và cách xử lý |
|---|---|
| TICKET.ticket_id trong ERD để trống/sai kiểu | SQL dùng INT IDENTITY, DTO dùng int |
| Tên sân bay/khách và mô tả mã | SQL NVARCHAR hỗ trợ tiếng Việt; ERD một số ô còn VARCHAR |
| BOOKING.total_amount | SQL DECIMAL(18,2); bản ERD mới cũng ghi (18,2) |
| Ngày giờ | SQL DATETIME2(0) thay DATETIME; FLIGHT vẫn giữ 4 cột DATE/TIME riêng |
| DISCOUNT_CODE.discount_type | SQL dùng PERCENT/FIXED, không dùng ký hiệu % trong code |
| Giới hạn giảm giá/lượt | SQL cho NULL = không giới hạn; không coi NULL là 0 |
| Ghế | TICKET.seat_number; BOOKING_DETAIL không có seat_number |
| Chỗ theo hạng vé | Không có quota FARE_CLASS; hiển thị chỗ còn của toàn chuyến |
| Fare–Flight | Dùng FK ghép TICKET(fare_id,flight_id); BUS kiểm tra trước ghi |
| BookingDetail–Flight | Schema chưa có FK ghép kiểm tra Booking và Ticket cùng chuyến; luồng ghi luôn lấy cùng flight từ quote, script 05 kiểm tra dữ liệu |
| Mã sân bay/số hiệu chuyến | Schema không có; giao diện dùng tên/thành phố và flight_id |

Không chỉnh lại ERD hoặc thêm cột nghiệp vụ để thuận tiện lập trình.

## Giả định nghiệp vụ cần nhóm duyệt

- Tối đa 9 khách/đơn, một chuyến và một hạng vé/đơn. Không làm vé khứ hồi.
- Giờ chuyến, hạn mã và ngày đặt được hiểu theo giờ Việt Nam (UTC+7).
- Tự cấp ghế theo hàng A–F vì chưa có sơ đồ máy bay; không cho khách chọn ghế.
  Cần Hiếu xác nhận quy ước trước khi dùng cho dữ liệu thực tế.
- Giá FARE_CLASS là giá cơ sở được dùng; thuế bổ sung = 0 do chưa có chính sách thuế.
- Chỉ hủy online đơn chưa thanh toán trước giờ khởi hành. Không tự hoàn tiền
  hoặc trừ doanh thu của hóa đơn PAID; giao Quân tích hợp quy trình thanh toán/hoàn tiền.
- Đơn UNPAID vẫn giữ chỗ cho đến khi hủy; chưa có cơ chế tự hết hạn giữ chỗ.
- CUSTOMER được tạo như bản chụp liên hệ của đơn, không sửa khách đang tồn tại
  dựa trên email/điện thoại do chưa có đăng nhập hoặc quy tắc định danh khách.
  Cần phối hợp Phú nếu bổ sung tài khoản hoặc quy tắc dùng lại hồ sơ khách.
- Người giữ khóa tra cứu có quyền xem/hủy đơn hợp lệ. Chưa có tài khoản/OTP,
  gửi email khôi phục khóa hay cổng thanh toán. Cần HTTPS khi chạy ngoài localhost.
- Session hiện lưu bộ nhớ một máy; khi restart, dùng mã đơn + khóa để mở lại.

## Đối chiếu yêu cầu đồ án / phân công

| Yêu cầu | Phần Thịnh | Phối hợp / còn lại |
|---|---|---|
| Database đúng, 3-layer bắt buộc (w4) | Bám schema, GUI→BUS→DAO, DTO; đề xuất 08 tách riêng | Hiếu review SQL/ERD toàn nhóm |
| MyConnection (w7 mục 1) | Đã có lớp dùng chung trong DAO | Hiếu tái sử dụng commit hạ tầng |
| Xem/thêm/sửa/xóa/tìm (w4) | Tìm, tạo, xem, hủy Booking; không xóa lịch sử hoặc sửa hành khách sau cấp vé | CRUD danh mục do Hiếu/Phú; sửa Booking cần quy tắc được duyệt |
| Giao diện hợp lý (w4) | Web Việt, responsive, nhãn, thông báo lỗi và xác nhận hủy | MainForm/menu WinForms do nhóm phối hợp |
| ArrayList + AND/OR/NOT/so sánh (w7 mục 2,8) | BUS lọc giá và hạng vé sau điều kiện hành trình bắt buộc | Các BUS danh mục thuộc người phụ trách |
| Form cha–con, hai ListView (w7 mục 3) | Không áp WinForms vào Razor | Quân: Invoice/InvoiceDetail, Hiếu: form danh mục |
| Nút chọn dữ liệu dùng static (w7 mục 4) | Web dùng request/session, không dùng static chia sẻ dữ liệu khách | Hiếu/Phú/Quân áp dụng cho WinForms |
| Duy nhất, số liệu biến động, không nhập số liệu tự tính (w7 mục 5–7) | FK đúng chuyến; ghế, giá, counters và giảm giá tính server; transaction | Các luồng ngoài web phải theo cùng quy tắc |
| Table tham số (w7 mục 9) | Chưa thêm bảng nghiệp vụ vào schema chung | Hiếu/nhóm cần chốt phạm vi theo đề tài |
| Ba thống kê / tiền theo thời gian, quý (w4, w7 mục 10) | Chỉ tích hợp Invoice UNPAID, không ghi doanh thu sớm | Quân; các mẫu tổng chi/lợi nhuận cần hỏi GV vì schema bán vé chưa có phiếu nhập/chi phí |
| Import/export xls (w7 mục 11) | Ngoài phạm vi Booking | Quân |
| Report PDF (w7 mục 12) | Ngoài phạm vi Booking | Quân |
| Báo cáo cuối kỳ (w7 mục 14) | Tài liệu này là bàn giao phần Thịnh, không phải báo cáo toàn nhóm | Nhóm viết bản ≤20 trang, TNR 13, không mục lục, có hướng dẫn cài đặt và ảnh minh họa |
| Unicode connection string (w7 mục 15) | Dùng NVARCHAR và SqlClient Unicode parameters | Không nối cú pháp MySQL `?useUnicode=...` vào SQL Server |

Không khẳng định toàn dự án đã đáp ứng mọi yêu cầu hoặc đạt toàn bộ điểm đồ án.

## Cấu hình và chạy

1. Cài .NET SDK 10 và SQL Server/LocalDB. Tạo database bằng 01, schema bằng 02,
   index bằng 03; dữ liệu mẫu 04 rồi tùy chọn 07. Seed không dùng để chạy lặp.
2. Nhờ Hiếu review/duyệt 08, sau đó mới chạy trên database chung. Không bỏ qua
   bước này: thiếu bảng sẽ không đặt/tra cứu được.
3. Đặt cấu hình cục bộ (ví dụ Windows Authentication, thay instance đúng máy):

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=.\SQLEXPRESS;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet build FlightBookingSystem.slnx
dotnet run --project src/FlightBooking.Web --launch-profile https
```

`TrustServerCertificate=True` chỉ dùng máy phát triển với chứng chỉ tự ký. Môi
trường triển khai cần chứng chỉ tin cậy, HTTPS và bỏ tùy chọn này. Không commit
cấu hình chứa mật khẩu; có thể đặt trong appsettings.Development.json đã ignore.
Không tắt bảo vệ chứng chỉ trong source. Tài liệu giảng viên dùng ví dụ MySQL;
tham số UTF-8 trong URL MySQL không hợp lệ với Microsoft.Data.SqlClient.

Dữ liệu mẫu: SGN→HAN ngày 15/12/2026, mã DEMO10; nếu dữ liệu/ngày hiện tại đã
thay đổi thì chọn chuyến tương lai có thật. Giá, hành lý và ghế luôn đọc SQL.

## Kiểm tra

```powershell
dotnet run --project tests/FlightBooking.Checks
```

Kiểm tra discount/trạng thái/thời hạn/min/cap/limit/làm tròn decimal, cấp ghế,
hết ghế, AND/OR/NOT và 6 phép so sánh trên ArrayList, đầu vào lỗi/trùng giấy tờ,
ngày sinh, hủy PAID hoặc sau giờ bay, khóa tra cứu sai.

Kiểm tra SQL chỉ trên database riêng có tên kết thúc `_BookingTests`. Tự tạo
database rỗng trước; chương trình áp schema/index/08 trong database đó, không
tạo/xóa database hoặc dùng FlightBookingDB. Không chạy trên dữ liệu thật.

```powershell
$env:FLIGHTBOOKING_TEST_CONNECTION = 'Server=.\SQLEXPRESS;Database=FlightBooking_BookingTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project tests/FlightBooking.Checks -- --sql
```

Kiểm tra SQL: đặt có giảm giá, chi tiết/vé/ghế/hóa đơn, không ghi doanh thu UNPAID,
retry đồng nhất, payload thay đổi, khóa sai, hết chỗ, sai chuyến/hạng, giá bị sửa,
hủy lặp, rollback sau nhiều bảng, hai request cùng khóa, tranh chấp ghế cuối và
lượt giảm giá cuối trên hai chuyến. Fixture giả lập được giữ trong database test,
mỗi lần có nhãn CHECK riêng để truy vết. Không dùng mock thay cho SQL.

## Giới hạn kiểm chứng hiện tại

Đã build thành công (0 warning/0 error), chạy 30 kiểm tra nghiệp vụ thuần C#.
Đã mở web thật ở localhost: kiểm tra trang chủ và tra cứu trên desktop, 375 px
và 812 px không tràn ngang; thiếu connection string hiển thị thông báo rõ ràng.
GET tra cứu có mã antiforgery; POST thiếu mã bị chặn HTTP 400; chi tiết đơn thiếu
phiên xác minh chuyển về tra cứu. Chưa kiểm tra giao diện checkout/kết quả với
dữ liệu SQL thật vì môi trường chưa được xác nhận.
Kiểm thử SQL chưa chạy: chưa có instance/connection string được xác nhận và lần
xin quyền kiểm tra SQL Server/LocalDB bị từ chối. Không coi các tình huống SQL
hoặc luồng đặt/hủy hoàn chỉnh là đã chạy đạt. Cần hoàn tất trước khi merge.

## Bước tiếp theo cho Hiếu (chưa triển khai)

`feature/flight-management` tạo từ cùng `893e751`, chưa chứa nghiệp vụ Web.
Khi Thịnh xác nhận bắt đầu giai đoạn 2: tái sử dụng chọn lọc MyConnection;
review đề xuất 08, SQL và ERD; DTO/DAO/BUS AIRPORT/FLIGHT/FARE_CLASS, WinForms
CRUD/tìm kiếm ArrayList, validation/FK/ngày giờ/giá/hành lý/chỗ, không sửa tùy ý counters.
Không merge toàn bộ nhánh Web sang và không tự merge PR vào develop.

## Cập nhật hành khách và khứ hồi — 21/11/2026

Phần này thay thế các giới hạn một chuyến/một Booking của phiên bản trước.

### Đã sửa

- `FlightSearch`: loại hành trình và ngày về; một chiều xóa ngày về khỏi xử lý.
- Trang Index tìm lượt đi, rồi đảo sân bay để tìm lượt về. Chuyến về phải khởi hành
  sau khi chuyến đi hạ cánh, kể cả bay qua đêm; không chỉ so sánh ngày.
- Checkout giữ đúng Quantity, bind `Input.Passengers[0..n-1]` riêng từng người.
  Thêm nút lưu thông tin/quay lại chọn chuyến và cập nhật số người 1–9.
- Nháp được lưu server-side trong session, gắn với DraftId riêng cho mỗi luồng.
  Quay lại qua nút lưu hoặc mở lại cùng nháp giữ khách đặt và từng hành khách;
  thay đổi số người giữ các phần tử còn lại. Không lưu giấy tờ vào URL hoặc localStorage.
  Nháp hết hạn sau một giờ không hoạt động hoặc khi tiến trình mất session bộ nhớ.
- BUS đối chiếu Quantity với số bản ghi hành khách, kiểm tra bắt buộc và trùng giấy tờ.
  Đổi hành trình/số người cấp khóa yêu cầu mới; gửi lại cùng yêu cầu trả cùng Booking.
- Trang kết quả hiển thị cả hai Booking, vé/ghế/hành khách và giá từng lượt/tổng tiền.
  Hủy hành trình kiểm tra cả hai lượt rồi hủy trong cùng transaction; hủy lặp không hoàn thêm.

### Ánh xạ biểu mẫu

| Trường | Nơi lưu |
|---|---|
| Họ tên khách đặt | CUSTOMER.full_name |
| Email, điện thoại, ngày sinh, giấy tờ khách đặt | CUSTOMER.email, phone, date_of_birth, id_passport_number |
| Giới tính khách đặt | CUSTOMER.gender; nhãn Nữ gửi giá trị Nu theo seed 07, Nam và Khác giữ theo seed 04/07; chấp nhận Nữ cũ và chuẩn hóa thành Nu |
| Tên mỗi hành khách | BOOKING_DETAIL.passenger_full_name; nhập họ tên đầy đủ, không tách Họ/Tên |
| Giấy tờ mỗi hành khách | BOOKING_DETAIL.passenger_id_card |
| Vé, ghế, hạng và chuyến mỗi lượt | TICKET.ticket_id, seat_number, fare_id, flight_id |
| Số người, chuyến, tổng tiền mỗi lượt | BOOKING.ticket_quantity, flight_id, total_amount |
| Ngày giờ bay | FLIGHT giữ departure_date/time và arrival_date/time riêng |

CUSTOMER chỉ là khách đặt, không tự tạo một CUSTOMER cho mỗi người đi vé.
BOOKING_DETAIL và ERD hiện không có giới tính hành khách. Do đó **không thêm ô
nhập giới tính hành khách rồi bỏ dữ liệu khi lưu** và không sao chép giới tính
khách đặt cho họ. Nếu nhóm cần trường này, phương án cần Hiếu duyệt trước là
thêm `passenger_gender NVARCHAR(10) NULL` vào BOOKING_DETAIL, thống nhất bộ giá trị
và constraint, để lịch sử NULL thay vì tự đoán giới tính, rồi cập nhật DTO/DAO/UI.
Chưa tạo/chạy migration cho phương án đó. CUSTOMER hiện không có CHECK giới tính;
seed 07 dùng Nu trong khi giao diện cũ dùng Nữ; đây là điểm cần thống nhất toàn nhóm.

### Cách lưu khứ hồi

Không gộp hai chuyến vào BOOKING.flight_id. Mỗi hành trình khứ hồi tạo:

- Một CUSTOMER liên hệ, dùng chung customer_id cho hai lượt.
- Hai BOOKING, mỗi bản ghi đúng một flight_id và đúng Quantity.
- Mỗi Booking có Quantity TICKET, BOOKING_DETAIL và một INVOICE UNPAID cùng INVOICE_DETAIL.
- Hai bản ghi trong bảng kỹ thuật BOOKING_WEB_REQUEST đã đề xuất ở script 08.
  Khóa lượt về dẫn xuất SHA-256 từ khóa bí mật lượt đi với tiền tố `return:`;
  mỗi khóa lại được hash trước khi lưu. Không thêm cột/bảng mới để liên kết hai lượt.
  Dùng mã Booking lượt đi + khóa tra cứu gốc để mở lại toàn hành trình sau restart.

BUS dùng đúng một `BookingDAO.Write`/SqlTransaction Serializable cho cả hai lượt,
khóa FLIGHT theo flight_id tăng dần, tính lại giá và kiểm tra ghế cả hai lượt.
Lỗi ở lượt về rollback luôn dữ liệu lượt đi. Không cộng doanh thu khi hóa đơn UNPAID.
Mã giảm giá áp dụng trên giá lượt đi, dùng một lượt mã cho cả hành trình; lượt về
không áp lại mã. Cách áp mã được hiển thị tại checkout, cần nhóm thống nhất chính sách.

Không sửa schema hoặc dữ liệu FlightBookingDB. Script 08 vẫn cần được nhóm duyệt
và áp dụng để lưu khóa chống lặp/tra cứu; đây là phụ thuộc đã có của phiên bản trước.
Những người quản lý hóa đơn/thanh toán tiếp tục cần phối hợp trạng thái từng Booking.

### Kiểm chứng bản cập nhật

- Build dùng `--artifacts-path tmp/journey-build -m:1 --disable-build-servers
  -p:UseSharedCompilation=false`, tránh khóa DLL của website đang chạy.
- Build thành công, 0 lỗi; 5 cảnh báo NU1900 do không truy cập dịch vụ kiểm tra
  lỗ hổng gói NuGet. Không tắt audit trong cấu hình dự án.
- 48 kiểm tra C# đạt, gồm độc lập dữ liệu 1/2/3 người, tăng/giảm giữ người còn lại,
  lệch số vé, một chiều loại giá trị lượt về, ngày/giờ và tuyến khứ hồi, tổng hai lượt.
- 10 kiểm tra HTTP đạt trên localhost (không kết nối SQL): số fieldset 1/2/3,
  tăng/giảm số người, khôi phục nháp bằng GET, giữ dữ liệu khi validation lỗi,
  server chặn ngày về sai và HTML vô hiệu hóa ngày về khi một chiều.
- `node tests/FlightBooking.Checks/search-form.checks.cjs` đạt: chạy script thật
  với DOM kiểm thử, xác minh ẩn/hiện, required/disabled, xóa ngày về khi chuyển một
  chiều, ngày sai và đổi sân bay. Đây không phải kiểm tra trực quan trình duyệt.
- Công cụ trình duyệt không mở được; chưa kiểm tra trực quan bố cục bản này.
- Đã đọc kết nối SQL thật RNR-LAPTOP và xác nhận FlightBookingDB chưa có bảng 08;
  chưa có FlightBooking_BookingTests. Yêu cầu tạo/chạy database test bị từ chối,
  nên **chưa chạy kiểm tra ghi SQL, luồng đặt hai lượt hoặc rollback SQL của bản này**.
  Bộ `--sql` đã bổ sung ca hai Booking/cùng hành khách, retry, hủy hai lượt lặp,
  lỗi lượt về sau khi ghi lượt đi và rollback, nhưng chưa được thực thi.

Các file chính: BookingModels.cs, BookingBUS.cs, BookingDraft.cs, Index.cshtml(.cs),
Checkout.cshtml(.cs), Details.cshtml(.cs), search.js, site.css và tests/FlightBooking.Checks.
Không làm phần Hiếu, không tự merge PR. Thay đổi có sẵn của người dùng trong
Database/02_CreateTables.sql được giữ nguyên.

## Ràng buộc người đặt, áp lại mã và Enter (21/11/2026)

- Người đặt phải đã qua sinh nhật lần 18 theo ngày Việt Nam; đúng ngày sinh nhật
  thứ 18 vẫn bị từ chối. BUS dùng ngày đầy đủ và xử lý ngày 29/2; form có min/max
  tương ứng và lỗi dưới ô ngày sinh. Không áp tuổi này cho hành khách.
- CustomerInput bắt buộc CCCD/hộ chiếu riêng, trim, tối đa 50 ký tự, giữ quy tắc
  chữ Latin/số/dấu gạch ngang. DAO vẫn ghi CUSTOMER.id_passport_number. Không sửa
  schema, dữ liệu cũ hoặc quy tắc đọc các bản ghi cũ.
- BookingDraft lưu dấu trạng thái mã đã áp theo mã chuẩn hóa, lựa chọn hai lượt,
  số người và báo giá. Mỗi lần xem giá đều đọc và tính lại phía server; áp lại
  cùng mã/cùng giá thông báo “Mã giảm giá này đã được áp dụng”. Đổi/xóa/mã lỗi
  không giữ trạng thái cũ; đổi số vé/chuyến/giá làm trạng thái mất hiệu lực.
- Xem trước không tăng used_count. UseDiscount vẫn chỉ gọi trong transaction
  Create; không thay đổi chống gửi lặp, khóa ghế hoặc tính nguyên tử hai lượt.
- Nguyên nhân Enter: nút action=back là submit đầu tiên nên trình duyệt chọn nó
  khi implicit submit. Đã thêm submit mặc định an toàn action=quote, handler không
  có action cũng xem giá; Enter tại nút Xác nhận vẫn là action=book. JavaScript
  chỉ xử lý riêng Enter ô mã, không chặn toàn trang; chặn gửi lặp khi đang áp mã.
- Bỏ ModelState.Clear toàn bộ khi áp mã; chỉ cập nhật trạng thái mã/tổng tiền,
  giữ lỗi, giới tính, danh sách người đi và cả hai lượt. Mã lỗi vẫn hiện hành trình
  với giá chưa giảm, không đưa khách về màn hình chọn chuyến.

File chính: BookingModels.cs, BookingBUS.cs, BookingDraft.cs,
Pages/Booking/Checkout.cshtml và .cshtml.cs, wwwroot/js/checkout.js.

Kiểm tra: build solution ở tmp/contact-build 0 lỗi, 5 cảnh báo NU1900; 82 kiểm tra
C# và 12 kiểm tra HTTP đạt, gồm hai request áp lại mã đồng thời và tổng tiền bị sửa.
checkout.checks.cjs đạt. Đã thử trình duyệt: Enter ở người đặt/hành khách/mã,
áp lại mã bằng chuột, xác nhận bằng Enter khi thiếu giấy tờ bị validation chặn,
lỗi tuổi hiển thị dưới ô và giữ toàn bộ dữ liệu. SQL SELECT xác nhận used_count
EXPAND5/SAVE100 trước và sau đều 18. Không ghi Booking hoặc sửa database.
Chưa thử tạo đơn thành công trên SQL; vẫn còn vướng bảng BOOKING_WEB_REQUEST
của script 08 đã báo trước. Các fixture kiểm thử được bổ sung giấy tờ giả rõ ràng;
không tự tạo giấy tờ cho người dùng hoặc thay dữ liệu CUSTOMER có sẵn.
