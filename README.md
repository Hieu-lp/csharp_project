# FlightBookingSystem

Đồ án Quản lý bán vé máy bay.

## Công nghệ
- C# .NET 10
- WinForms
- ASP.NET Core Razor Pages
- SQL Server
- Mô hình GUI - BUS - DAO

## Nhóm
4 thành viên.

## Database
Các script nằm trong `Database/`. Với database mới: chạy lần lượt 01, 02, 03,
04; có thể thêm dữ liệu mẫu bằng 07. Không chạy lại seed 04/07 trên dữ liệu đã có.

## Website Booking — phần Thịnh

Hướng dẫn cài đặt, quy tắc nghiệp vụ, đối chiếu yêu cầu và giới hạn kiểm chứng:
[Docs/Thinh-Booking.md](Docs/Thinh-Booking.md).

Script `Database/08_BookingWebRequest.sql` là **đề xuất cần Hiếu review** trước khi
áp dụng database chung. Website cần bảng này để chống đặt trùng sau khi gửi lại
và xác minh khóa tra cứu. Không thay đổi 11 bảng gốc.

```powershell
dotnet build FlightBookingSystem.slnx
dotnet run --project tests/FlightBooking.Checks
```

Thiết lập `ConnectionStrings__FlightBookingDB` trong môi trường cục bộ rồi chạy:

```powershell
dotnet run --project src/FlightBooking.Web --launch-profile https
```
