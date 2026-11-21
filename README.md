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
Xem các script trong thư mục Database; không chạy lại script tạo bảng/seed trên database đã có dữ liệu.

## Phần Hiếu — quản lý sân bay, chuyến bay và hạng vé

Nhánh `feature/flight-management` có ba màn hình WinForms, CRUD và tìm kiếm
ArrayList trong BUS. Chọn chuyến liên kết danh sách hạng vé; các thao tác sửa/xóa
được kiểm tra để bảo vệ Booking và vé đã có.

```powershell
$env:ConnectionStrings__FlightBookingDB = 'Server=YOUR_SERVER;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet run --project src/FlightBooking.WinForms
```

Xem [báo cáo phần Hiếu, quy tắc dữ liệu và hướng dẫn kiểm tra](Docs/Hieu-FlightManagement.md).
