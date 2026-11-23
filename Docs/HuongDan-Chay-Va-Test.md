# Hướng dẫn chạy và thao tác thử website cùng WinForms

**Tiếp nối sau khi nhận tài liệu gốc:** xem [đối chiếu đủ 16 mục W7, so sánh
QLSV3Lop, kiểm tra GUI và phương án database test](DoiChieu-W7-QLSV3Lop.md).

**Kiểm tra trên máy hiện tại ngày 23/11/2026:** xem [báo cáo rà soát, lỗi đã sửa,
kết quả test và lệnh chạy an toàn](RaSoat-BookingFlight-2026-11-23.md).
Các đường dẫn, server, ID và trạng thái script 08 trong hướng dẫn cũ bên dưới là
thông tin bàn giao của máy trước. Máy hiện tại thiếu BOOKING_WEB_REQUEST; không
chạy seed/script 08 hoặc thử ghi dữ liệu vào database chính để làm theo ví dụ cũ.

Cập nhật ngày 22/11/2026. Dành cho bản tích hợp trên nhánh
`feature/flight-management` tại commit `debfa06`: có cả Booking của Thịnh và
WinForms quản lý sân bay, chuyến bay, hạng vé của Hiếu.

**Bắt đầu nhanh:** mở hai cửa sổ PowerShell, chạy mục 2 và 3; sau đó thử đặt khứ
hồi TP.HCM → Hà Nội ngày 24/11/2026, về 27/11/2026, chọn 2 người theo mục 5.

## 1. Chuẩn bị trên máy hiện tại

- Windows, .NET SDK 10 và SQL Server đang chạy.
- Source: `C:\Users\ACER\OneDrive - GalaxyOne Company Limited\Documents\ChatGPT\bookingflight\csharp_project`.
- SQL Server hiện dùng: `RNR-LAPTOP`; database: `FlightBookingDB`.
- Database đã có dữ liệu mẫu và bảng `BOOKING_WEB_REQUEST` của script 08.
  **Không chạy lại script 01/02/04/07 trên database đang có dữ liệu.**
- Nếu dùng máy khác, thay đường dẫn source và tên server trong các lệnh bên dưới.
  `RNR-LAPTOP` là tên server của máy đang kiểm tra, không phải tên dùng cho mọi máy.

Đây là lệnh **PowerShell**, không dán nguyên vào Git Bash hoặc ô Query của SSMS.
Chạy từng dòng; không thêm dấu `\` vào cuối dòng.

## 2. Cửa sổ PowerShell thứ nhất: chạy website

```powershell
cd 'C:\Users\ACER\OneDrive - GalaxyOne Company Limited\Documents\ChatGPT\bookingflight\csharp_project'
$env:ConnectionStrings__FlightBookingDB = 'Server=RNR-LAPTOP;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FlightBooking.Web --launch-profile http
```

Đợi có dòng `Now listening on: http://localhost:5191`, rồi mở:

- Trang chủ: <http://localhost:5191/>
- Tra cứu đơn: <http://localhost:5191/Booking/Lookup>

Giữ cửa sổ PowerShell này mở trong lúc sử dụng. Nhấn **Ctrl+C** để dừng website.
Nếu cổng 5191 đã có website đang chạy, dùng bản đó hoặc dừng nó trước khi chạy lại.
Sau khi đổi nhánh/cập nhật code, khởi động lại để dùng đúng bản mới.

Nếu cần chạy bản riêng ở cổng khác:

```powershell
dotnet run --project src/FlightBooking.Web --no-launch-profile -- --urls http://localhost:5080 --environment Development
```

Khi đó mở `http://localhost:5080/` thay vì 5191.

## 3. Cửa sổ PowerShell thứ hai: chạy WinForms

```powershell
cd 'C:\Users\ACER\OneDrive - GalaxyOne Company Limited\Documents\ChatGPT\bookingflight\csharp_project'
$env:ConnectionStrings__FlightBookingDB = 'Server=RNR-LAPTOP;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FlightBooking.WinForms
```

Biến kết nối phải đặt trong **cả hai cửa sổ**, vì mỗi PowerShell có môi trường riêng.
Web và WinForms dùng cùng database khi hai chuỗi kết nối giống nhau.

Ứng dụng có ba tab: **Sân bay**, **Chuyến bay**, **Hạng vé**. Phóng lớn cửa sổ
nếu cần. Đóng cửa sổ ứng dụng để dừng WinForms.

## 4. Chuyến và mã giảm giá để thử ngay

Đã đọc từ database local ngày 22/11/2026. Các chuyến sau còn đủ chỗ cho 3 người
tại thời điểm kiểm tra. ID chỉ đúng với database hiện tại, có thể khác trên máy khác.

| Nơi đi → nơi đến | Ngày giờ đi | Ngày giờ về | ID đi / về | Giá rẻ nhất đi / về mỗi người |
|---|---|---|---|---|
| Tân Sơn Nhất → Nội Bài | 24/11/2026 07:15 | 27/11/2026 13:40 | 42 / 43 | 1.200.000 / 1.250.000đ |
| Đà Nẵng → Nội Bài | 25/11/2026 07:15 | 28/11/2026 13:40 | 36 / 37 | 950.000 / 1.000.000đ |
| Tân Sơn Nhất → Đà Nẵng | 26/11/2026 07:15 | 29/11/2026 13:40 | 40 / 41 | 850.000 / 900.000đ |

Chọn đúng ngày ở cả hai lượt và để trống bộ lọc khi thử lần đầu. Danh sách sân
bay có thể hiển thị Đà Nẵng là `Da Nang`. Nếu đã qua các ngày trên, chọn cặp
tháng 12/2026 hoặc 01/2027 trong danh sách chuyến; web chặn chuyến đã khởi hành.
Sau 01/2027 cần tạo lịch bay tương lai mới để thử đặt.

| Mã | Điều kiện hiện tại |
|---|---|
| `EXPAND5` | Giảm 5%, tối đa 200.000đ, còn lượt tại lúc kiểm tra |
| `SAVE100` | Giảm 100.000đ khi tiền vé lượt đi từ 500.000đ, còn lượt tại lúc kiểm tra |

Mã hiện chỉ áp dụng cho **lượt đi**, tính một lần dùng cho cả hành trình.
Giá và điều kiện được kiểm tra lại trên server nên có thể thay đổi sau khi quản lý cập nhật.

## 5. Thao tác thử website

### 5.1 Tìm và chọn khứ hồi 2 người

1. Chọn **Khứ hồi** và **2 hành khách**.
2. Từ: Tân Sơn Nhất; Đến: Nội Bài.
3. Ngày đi 24/11/2026, ngày về 27/11/2026; để trống bộ lọc.
4. Bấm **Tìm chuyến bay**, chọn Economy của chuyến #42.
5. Trang phải chuyển sang **Chọn chuyến về**; chọn Economy của chuyến #43.
6. Màn nhập thông tin có **1 khối người đặt/liên hệ riêng và đúng 2 khối hành khách**.

Với giá trong bảng mục 4, tổng trước giảm là:
`2 × 1.200.000 + 2 × 1.250.000 = 4.900.000đ`.

### 5.2 Nhập thông tin mẫu

Chỉ dùng dữ liệu giả để thao tác thử. Những chuỗi TEST bên dưới phù hợp quy tắc
nhập của đồ án, không phải CCCD/hộ chiếu thật.

| Khối | Trường | Giá trị thử |
|---|---|---|
| Người đặt | Họ tên | Nguyễn Văn Test |
| Người đặt | Email | booking.test@example.test |
| Người đặt | Điện thoại | 0900000000 |
| Người đặt | Ngày sinh | 01/01/2000 |
| Người đặt | Giới tính | Nam |
| Người đặt | CCCD/hộ chiếu | TEST-LH-001 |
| Hành khách 1 | Họ tên / giấy tờ | Hành Khách Một / TEST-HK-001 |
| Hành khách 2 | Họ tên / giấy tờ | Hành Khách Hai / TEST-HK-002 |
| Hành khách 3, nếu chọn 3 người | Họ tên / giấy tờ | Hành Khách Ba / TEST-HK-003 |

Người đặt có thể không đi cùng. Tuổi chỉ kiểm tra cho người đặt: phải **đã qua**
sinh nhật lần thứ 18; đúng ngày sinh nhật thứ 18 chưa được chấp nhận.

### 5.3 Thử giảm giá và phím Enter

1. Nhập ` expand5 `, bấm **Tính lại giá / áp mã**: mã được chuẩn hóa và tiền giảm
   là 120.000đ; tổng ví dụ 2 người ở trên còn **4.780.000đ**.
2. Bấm áp mã lần nữa: hiện **“Mã giảm giá này đã được áp dụng”**, tổng không giảm thêm.
3. Đổi thành `SAVE100`, áp lại: tổng ví dụ còn **4.800.000đ**, không giữ thông báo
   mã cũ đã áp dụng.
4. Xóa mã rồi áp lại: tổng trở về **4.900.000đ**.
5. Enter trong ô tên/người liên hệ/hành khách: vẫn ở form, xem lại giá;
   không quay về chọn chuyến và không tạo Booking.
6. Enter trong ô mã: thực hiện áp mã. Enter khi đang **focus nút Xác nhận đặt vé**
   vẫn kích hoạt xác nhận, nên chỉ thử nút đó khi muốn tạo đơn.

### 5.4 Xác nhận, tra cứu và hủy

**Bấm Xác nhận đặt vé sẽ ghi đơn thật vào database local**, giảm ghế và ghi lượt
mã nếu có. Muốn dừng ở bước xem trước thì chưa bấm nút này.

1. Điền đủ dữ liệu rồi bấm **Xác nhận đặt vé**.
2. Trang kết quả khứ hồi phải có **2 mã Booking, mỗi lượt 2 vé**, tổng 4 vé.
   Danh sách hai hành khách phải giống nhau ở hai lượt.
3. Ghi lại **mã đơn lượt đi** và mở **Hiện khóa tra cứu riêng** để lưu khóa 64 ký tự.
   Khóa có quyền tra cứu/hủy; giữ riêng, không đưa vào báo cáo nhóm.
4. Mở trang **Tra cứu đơn đặt**, nhập mã lượt đi và khóa để xem cả hành trình.
5. Để dọn đơn thử hợp lệ, mở **Yêu cầu hủy hành trình** và bấm xác nhận hủy toàn bộ.
   Chỉ hủy được đơn chưa thanh toán, trước giờ khởi hành.
6. Sau hủy, Booking/vé/hóa đơn chuyển sang trạng thái hủy; ghế và lượt mã được hoàn.
   Các bản ghi vẫn được giữ làm lịch sử, không xóa bằng tay trong SQL.

**Thanh toán hiện chưa hoàn thiện:** đặt thành công tạo vé ISSUED và hóa đơn UNPAID.
Không có nút trả tiền online trên web hoặc màn quản lý thanh toán trong phần Hiếu.
Mã vé hiển thị không có nghĩa là khách đã trả tiền.

### 5.5 Các trường hợp nên thử thêm

| Thao tác | Kết quả mong đợi |
|---|---|
| Chọn một chiều | Chỉ yêu cầu ngày đi, không xử lý ngày về |
| Khứ hồi với ngày về trước ngày đi | Báo lỗi, không cho chọn hành trình sai |
| Chọn 1/2/3 người | Hiện đúng 1/2/3 khối hành khách, ngoài khối người đặt |
| Lưu thông tin và chọn lại chuyến, đổi 1 → 2 người | Giữ người còn lại, thêm khối thứ hai và tính lại tiền |
| Người đặt thiếu giấy tờ, ngày sinh tương lai hoặc chưa qua tuổi 18 | Báo lỗi; thông tin khác không mất |
| Hai hành khách trùng giấy tờ | Không cho xác nhận |
| Mã không tồn tại, hết hạn hoặc hết lượt | Báo lỗi, không giảm tiền và vẫn giữ form/hành trình |
| Bấm xác nhận lại cùng biểu mẫu/cùng nội dung | Không tạo thêm đơn/vé hoặc trừ ghế lần nữa |
| Bấm thẻ Chuyến bay giá tốt từ trang chủ | Mở đúng chuyến/hạng, đặt Một chiều; đổi số khách vẫn giữ lựa chọn |

## 6. Thao tác thử WinForms

### 6.1 Sân bay

1. Chọn tab **Sân bay**, bấm **Tải lại**; chọn một dòng để xem thông tin bên phải.
2. Nhập từ khóa ở **Tìm nhanh**, nhấn Enter; hoặc mở **Lọc nâng cao**, chọn Tên sân bay, Chứa, giá trị `Nội`, bấm **Áp dụng**.
3. Bấm **Xóa lọc** trong bộ lọc nâng cao để xem lại toàn bộ.
4. Chỉ trên database kiểm thử riêng: bấm **Thêm mới**, nhập tên `TEST - Sân bay thử`, thành phố `TEST`, bấm Lưu.
5. Chọn dòng vừa tạo, bấm **Chỉnh sửa**, sửa thành phố thành `TEST 2`, bấm Lưu. Chọn **Hủy thay đổi** để khôi phục giá trị cũ.
6. Xóa dòng thử khi chưa có chuyến tham chiếu; phải có hộp xác nhận.
7. Lưu thiếu tên/thành phố phải báo lỗi. Không thử xóa sân bay thật để dọn dữ liệu;
   ứng dụng sẽ chặn nếu sân bay đã có chuyến.

### 6.2 Chuyến bay và hai ListView

1. Mở tab **Chuyến bay**, chọn dòng: vùng nhập phải điền đúng chuyến; ListView
   phía dưới chỉ hiện hạng vé của chuyến đó.
2. Bấm **Chỉnh sửa** rồi thử **Chọn…** cạnh sân bay; chọn một dòng rồi Enter hoặc Chọn để điền vào form. Bấm Esc/Hủy phải
   giữ nguyên giá trị đang có, không lấy lựa chọn của lần mở trước.
3. Bấm **Thêm mới** để thử thêm chuyến chưa có vé trên database kiểm thử riêng:
   chọn hai sân bay khác nhau, tổng 12 ghế, ngày tương lai, giờ đi 17:10, đến 19:20,
   trạng thái **Mở bán** (lưu SCHEDULED); bấm Lưu và ghi lại mã tự sinh.
4. Tìm nhanh theo mã vừa ghi, chọn dòng, bấm **Chỉnh sửa** để sửa tổng ghế từ 12 thành 14.
5. Vé bán vẫn bằng 0, ghế còn bằng 14, doanh thu bằng 0; các trường hệ thống chỉ đọc.
6. Chọn cùng sân bay ở hai đầu, tổng ghế 0 hoặc giờ đến trước giờ đi phải bị chặn.
   Chuyến qua đêm hợp lệ cần ngày đến lớn hơn ngày đi.

Chuyến đã có Booking/vé bị chặn đổi tuyến, lịch hoặc trạng thái. Giảm số ghế không
được làm mất ghế đã cấp, ví dụ có ghế 20F thì không giảm xuống 100 ghế.
Nếu có người vừa đặt vé hoặc sửa cùng dòng, tải lại và chọn lại trước khi lưu.

### 6.3 Hạng vé và xóa dữ liệu thử

1. Chọn chuyến thử chưa đặt vé ở trên, bấm **Quản lý hạng vé**.
2. Màn Hạng vé phải lọc và điền đúng mã chuyến; cũng có thể chọn bằng **Chọn chuyến…**.
3. Bấm **Thêm mới**, nhập `TEST-Economy`, giá 1.234.000đ, hành lý 7 kg; bấm Lưu.
4. Thêm lại cùng tên trên cùng chuyến phải bị chặn. Giá/hành lý không được âm.
5. Chọn hạng vừa tạo, bấm **Chỉnh sửa**, sửa giá thành 1.250.000đ; kiểm tra danh sách cập nhật. Quay lại tab Chuyến bay, bấm **Tải lại** để cập nhật hạng trong ListView phụ.
6. Nếu chưa đặt vé cho chuyến thử: xóa hạng trước, rồi xóa chuyến. Nếu đã có
   Booking/vé thì giữ lịch sử; hủy Booking không làm mất các khóa ngoại để có thể xóa chuyến.

Đổi giá danh mục không được thay giá chi tiết Booking/hóa đơn cũ. Hạng có vé không
được xóa hoặc chuyển sang chuyến khác. Chuyến có hạng vé cũng chưa được xóa.

## 7. Thử web và app cùng dùng một database

Dùng một chuyến mẫu trong mục 4, không dùng chuyến đang muốn xóa ở bài CRUD.

1. Trên app, chọn chuyến đi, ghi lại Tổng ghế / Đã bán / Còn ghế.
2. Trên web, đặt thành công 2 người cho chuyến đó (có thể khứ hồi).
3. Trở lại app, bấm **Tải lại** và chọn lại chuyến:
   Đã bán tăng 2, Còn ghế giảm 2. Khứ hồi thì kiểm tra cả hai chuyến.
4. Hóa đơn chưa thanh toán nên doanh thu đã thu không tự tăng.
5. Hủy đơn thử trên web, tải lại app: số ghế được hoàn đúng.

App không tự cập nhật liên tục khi người khác đặt vé; dùng Tải lại để thấy số mới.
Nếu hai bên không thấy cùng dữ liệu, kiểm tra lại Server và Database trong hai cửa sổ PowerShell.

## 8. Đối chiếu bằng SSMS khi cần

Kết nối server `RNR-LAPTOP`, mở New Query. Các câu lệnh sau chỉ đọc dữ liệu.

```sql
USE FlightBookingDB;
GO
SELECT @@SERVERNAME AS ServerDangDung, DB_NAME() AS DatabaseDangDung;
SELECT OBJECT_ID(N'dbo.BOOKING_WEB_REQUEST', N'U') AS BangChongDatTrung;
```

Nhập mã đơn **do bạn vừa tạo** vào biến bên dưới; không dùng mã ví dụ của người khác:

```sql
DECLARE @BookingId int = 0; -- Thay 0 bằng mã đơn vừa ghi lại
SELECT booking_id, customer_id, flight_id, ticket_quantity,
       discount_amount, total_amount, status
FROM dbo.BOOKING WHERE booking_id = @BookingId;

SELECT d.booking_id, d.ticket_id, d.passenger_full_name,
       t.flight_id, t.seat_number, t.status, d.price
FROM dbo.BOOKING_DETAIL d
JOIN dbo.TICKET t ON t.ticket_id = d.ticket_id
WHERE d.booking_id = @BookingId;

SELECT invoice_id, booking_id, total, discount_amount, total_amount, status
FROM dbo.INVOICE WHERE booking_id = @BookingId;

SELECT x.invoice_id, x.ticket_id, x.unit_price, x.quantity, x.subtotal
FROM dbo.INVOICE_DETAIL x
JOIN dbo.INVOICE i ON i.invoice_id = x.invoice_id
WHERE i.booking_id = @BookingId;
```

Khứ hồi có hai Booking: xem lần lượt mã đi và mã về. Giá trị request_hash trong
bảng 08 là bản băm, không phải khóa gốc để nhập vào trang tra cứu.

## 9. Build và chạy kiểm tra tự động tùy chọn

Đây là phần bổ sung cho kiểm tra bằng giao diện. Chạy từ thư mục chứa solution.
Build vào thư mục riêng để tránh khóa DLL của web/app đang mở:

```powershell
dotnet build FlightBookingSystem.slnx --artifacts-path tmp/manual-build -m:1 --disable-build-servers -p:UseSharedCompilation=false
dotnet tmp/manual-build/bin/FlightBooking.Checks/debug/FlightBooking.Checks.dll
dotnet tmp/manual-build/bin/FlightBooking.ManagementChecks/debug/FlightBooking.ManagementChecks.dll
```

Hai lệnh kiểm tra mặc định kiểm tra logic C#, không ghi database.
Để thử CRUD bằng SQL, dùng database kiểm thử riêng đã có trên máy:

```powershell
$env:FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION = 'Server=RNR-LAPTOP;Database=FlightBooking_Hieu_ManagementTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet tmp/manual-build/bin/FlightBooking.ManagementChecks/debug/FlightBooking.ManagementChecks.dll --sql
```

Runner chỉ chấp nhận database có tên kết thúc `_ManagementTests`; không đổi nó
thành FlightBookingDB. Máy khác cần tạo database kiểm thử riêng trước. Runner tạo
fixture và dọn fixture của lần chạy trong database test, không reset database chính.

## 10. Lỗi thường gặp

| Hiện tượng | Cách xử lý |
|---|---|
| PowerShell không nhận dotnet | Kiểm tra `dotnet --version`, cài/đảm bảo có SDK .NET 10 |
| Không tìm thấy project | Chạy đúng lệnh cd tới thư mục csharp_project chứa solution |
| App báo chưa cấu hình kết nối | Đặt biến ConnectionStrings__FlightBookingDB trong đúng cửa sổ đang chạy app |
| Không kết nối được SQL / database không tồn tại | Kiểm tra tên server ở SSMS và database; không mặc định máy nào cũng dùng SQLEXPRESS |
| Báo thiếu bảng / script 08 | Kiểm tra đúng server/database và OBJECT_ID ở mục 8; bảng 08 trên máy hiện tại đã có |
| Build báo file bị khóa | Dừng web bằng Ctrl+C, đóng app hoặc dùng thư mục build riêng ở mục 9 |
| Cổng 5191 đang dùng | Dùng web đang chạy, dừng bản cũ hoặc chạy cổng 5080 theo mục 2 |
| Không có chuyến về | Kiểm tra đúng ngày, tuyến đảo chiều, số ghế; xóa bộ lọc giá/hạng trước khi thử |
| App chưa hiện vé/ghế vừa đổi | Bấm Tải lại rồi chọn lại dòng |
| Báo dữ liệu vừa thay đổi | Tải lại và chọn lại; đây là bảo vệ tránh ghi đè dữ liệu mới |
| NU1900 khi build | Không lấy được dữ liệu audit NuGet; xem build có 0 lỗi hay không. Chưa xác minh lỗ hổng gói khi nguồn audit không truy cập được |
| HTTP 429 khi chạy nhiều kiểm tra | Đợi ít nhất 1 phút rồi thử lại; không tắt giới hạn tần suất của website |

## 11. Checklist trước khi báo nhóm đã thử xong

- [ ] Web và app mở được, cùng đúng database.
- [ ] Tìm/chọn được một chiều và khứ hồi.
- [ ] Số hành khách và thông tin từng người đúng qua các bước.
- [ ] Người đặt đủ tuổi, có giấy tờ; lỗi không làm mất dữ liệu.
- [ ] Áp lại mã không giảm chồng; đổi/xóa mã cập nhật đúng giá.
- [ ] Enter trong ô thông tin không quay về chọn chuyến hoặc tự tạo đơn.
- [ ] Đặt thành công có đủ Booking, vé, chi tiết và hóa đơn; lưu được khóa tra cứu.
- [ ] Tra cứu/hủy đơn thử đúng điều kiện; ghế được hoàn và app thấy số mới sau Tải lại.
- [ ] CRUD sân bay/chuyến/hạng thử đúng; không xóa hoặc sửa sai dữ liệu đã có vé.
- [ ] Chọn chuyến liên kết đúng hạng vé, bộ lọc AND/OR/NOT hoạt động.
- [ ] Ghi riêng các mục chưa thử; không coi build thành công là đã thử đủ nghiệp vụ.
