# Bộ lọc khách hàng và chuyến bay giá tốt — 21/11/2026

> Tài liệu nghiệp vụ của Thịnh được giữ theo quyền sở hữu. Hướng dẫn chạy hiện tại
> nằm trong [README](../README.md); số test/ID/ngày và các mô tả thay đổi bên dưới là lịch sử.

Phạm vi: website của Thịnh trên `feature/razor-booking`. Không thay đổi Database,
ERD, dữ liệu, WinForms hoặc tự merge PR. Các thay đổi có trước được giữ nguyên.

## Thay đổi chính

- `Pages/Index.cshtml` và `.cshtml.cs`: khoảng giá thấp nhất/cao nhất, hạng vé
  thực tế, Áp dụng/Xóa; giữ hành trình và số khách, kể cả khi nhập giá sai.
- `BookingModels.cs`, `DatVeBUS.cs`: kiểm tra VND, lọc ArrayList theo khoảng
  giá và tên hạng khớp hoàn toàn; giữ API phép so sánh, AND/OR/NOT cũ cho nơi khác.
- `DatVeDAO.cs`: đọc danh sách hạng vé và tối đa 4 chuyến giá thấp bằng SQL.
- `wwwroot/css/site.css`, `wwwroot/js/search.js`: giao diện xanh–trắng, 4/2/1
  cột theo kích thước; đổi số khách tự gửi lại để server tính tiền/kiểm tra ghế.

## Dữ liệu và cách chọn chuyến

DAO dùng một truy vấn `TOP (4)` và `CROSS APPLY TOP (1)` để lấy hạng rẻ nhất
của mỗi FLIGHT; không truy vấn riêng từng thẻ. Điều kiện: SCHEDULED, ghế còn > 0,
khởi hành sau thời điểm Việt Nam do BUS cung cấp. Thứ tự giá, ngày/giờ khởi hành,
flight_id rồi fare_id giữ ổn định khi bằng giá. Dưới 4 chuyến thì hiện đúng số có;
không có thì hiện thông báo trống. Không tự tạo giá gốc hay phần trăm giảm.

Bấm thẻ truyền flight_id/fare_id và số khách; BUS đọc lại chuyến/hạng/giá từ SQL,
PageModel đặt lại hành trình Một chiều theo dữ liệu này, xóa trạng thái lượt về.
Màn hình hiển thị đúng một lựa chọn và tổng tiền. Đổi số khách giữ nguyên hai ID;
Tiếp tục đặt vé chuyển sang Checkout với đúng số biểu mẫu. Khi tắt JavaScript,
khách vẫn có thể dùng Cập nhật số người hoặc Tiếp tục; server luôn kiểm tra lại.
Thiếu ghế hiển thị cảnh báo và vô hiệu hóa Tiếp tục. Chuyến hết hạn/hạng không
thuộc chuyến báo lỗi kèm đường chọn chuyến khác. Không tạo đơn hoặc giữ ghế ở đây.

## Kết quả kiểm tra và review

- Build toàn solution trong `tmp/deals-build`: **0 lỗi, 5 cảnh báo NU1900** do
  không truy cập được nguồn kiểm tra lỗ hổng NuGet. Chưa xác minh lỗ hổng gói.
- **66 kiểm tra C# + 21 kiểm tra SQL chỉ đọc = 87 đạt**. SQL thực thi trên
  `RNR-LAPTOP/FlightBookingDB`: chuyến hợp lệ/không trùng/sắp xếp/hạng rẻ nhất,
  đọc lại lựa chọn với 1/2/3 người và từ chối hạng của chuyến khác.
- **20 kiểm tra HTTP đạt**: chọn thẻ từ trạng thái khứ hồi, 1/2/3 biểu mẫu,
  số khách bị sửa, hạng không hợp lệ, lọc giá + hạng, giá đảo ngược, giữ sân bay
  khi lỗi, xóa lọc, chọn hai lượt và sang Checkout.
- JavaScript thực tế đã kiểm tra: đổi sân bay, chuyển loại hành trình/ngày về,
  tự gửi lại khi đổi số khách ở chuyến đã chọn. Trình duyệt xác nhận 3 → 2 người
  giữ chuyến #40/hạng Economy, tổng 2.550.000 → 1.700.000 VND.
- Đã xem giao diện ở 1440px và 390px: bốn cột/một cột, không tràn ngang;
  placeholder tải thành công. Ảnh kiểm tra desktop: `tmp/deals-desktop.jpg`.
- Review đã sửa lỗi form có thể mất lựa chọn sân bay khi giá không hợp lệ;
  không dùng cách tìm lại toàn tuyến khi khách bấm thẻ, không tin giá trên URL.
- Thiếu ghế kiểm tra bằng dữ liệu mô phỏng ở C#; chưa ép database thật hết ghế
  hoặc mô phỏng tranh chấp đồng thời. Chưa ép database về trạng thái 0/<4 chuyến.
- Chỉ kiểm tra đến Checkout, không xác nhận ghi Booking. Truy vấn chỉ đọc xác nhận
  `BOOKING_WEB_REQUEST` vẫn thiếu; cần thống nhất script 08 trước khi nghiệm thu
  đặt vé đầy đủ. Không áp dụng script 08 trong yêu cầu này.

Tệp kiểm tra: `tests/FlightBooking.Checks/Program.cs`, `BrowseChecks.cs`,
`deals.http.ps1`, `search-form.checks.cjs`. Chế độ `--browse` cần biến môi trường
`FLIGHTBOOKING_BROWSE_CONNECTION` trỏ đúng database; chỉ dùng SELECT, không seed.

## Bổ sung ảnh thật

Placeholder hiện có: `src/FlightBooking.Web/wwwroot/images/destinations/placeholder.svg`.
Đặt ảnh thật vào cùng thư mục và đổi `src` của ảnh trong khối `.destination-art`
tại `src/FlightBooking.Web/Pages/Index.cshtml`. Có thể ánh xạ ảnh theo mã sân bay;
giữ placeholder làm dự phòng cho điểm đến chưa có ảnh. Không trỏ tới ảnh chưa tồn tại.
