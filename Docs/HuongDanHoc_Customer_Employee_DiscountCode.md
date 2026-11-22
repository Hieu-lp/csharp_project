# Ghi chú học đồ án Customer, Employee và Discount Code

Tài liệu này ghi lại yêu cầu, kiến thức và các bước đã làm cho phần Phú phụ trách. Mục tiêu là giúp bạn hiểu luồng dữ liệu và tự giải thích được code, không chỉ có code để nộp. Ghi chú sẽ được cập nhật sau mỗi phần hoàn thành.

## 1. Nhiệm vụ được giao

Phụ trách ba bảng `CUSTOMER`, `EMPLOYEE`, `DISCOUNT_CODE`:

- Tạo DTO, DAO, BUS cho mỗi thực thể.
- Tạo WinForms quản lý khách hàng, nhân viên và mã giảm giá.
- Hỗ trợ xem, thêm, sửa, xóa và tìm kiếm.
- Kiểm tra dữ liệu trùng, giá trị giảm giá và thời hạn khuyến mãi.
- Hỗ trợ `MainForm`, menu và giao diện tổng thể.

Nhánh cá nhân: `feature/customer-management`.

## 2. Quy tắc làm việc của nhóm

- Làm trên nhánh cá nhân tạo từ `develop`; không push trực tiếp lên `main` hoặc `develop`.
- Không tự đổi cấu trúc database. Nếu cần đổi cột, kiểu dữ liệu hay constraint, trao đổi với người phụ trách database và chuẩn bị SQL script.
- Tìm kiếm nâng cao phải dùng `ArrayList` trong BUS theo yêu cầu giảng viên.
- Khi hoàn thành chức năng: chạy `dotnet build`, chạy thử SQL Server, commit code, push nhánh cá nhân, tạo PR vào `develop` và nhờ ít nhất một thành viên review.
- Không commit `bin`, `obj`, thông tin đăng nhập/connection string bí mật, `.mdf` hay `.bak`.
- Thao tác ghi nhiều bảng phải dùng transaction. CRUD một bảng thường không cần transaction nhiều bảng.

## 3. Kiến trúc 3 lớp cần nhớ

Luồng chính:

```text
WinForms (GUI) -> BUS -> DAO -> SQL Server
                    DTO mang dữ liệu giữa các lớp
```

- **DTO**: mang dữ liệu của một thực thể. Ví dụ một dòng `CUSTOMER` thành một `CustomerDTO`.
- **DAO**: chạy SQL, đọc kết quả và chuyển từng dòng thành DTO. DAO không quyết định quy tắc giao diện.
- **BUS**: kiểm tra dữ liệu, luật nghiệp vụ và tìm kiếm; sau đó gọi DAO.
- **GUI / WinForms**: nhận thao tác người dùng, gọi BUS và hiển thị kết quả.
- **MyConnection**: nơi tập trung tạo kết nối DB dùng chung.

Mẹo nhớ: **Form nhận thao tác, BUS kiểm tra, DAO truy cập DB, DTO mang dữ liệu.**

### Ghi chú về tài liệu 3 lớp

Slide minh họa 3 lớp bằng ví dụ MySQL và có đoạn SQL nối chuỗi. Đồ án này đang dùng SQL Server và package `Microsoft.Data.SqlClient`; vì vậy phải theo project. Khi làm truy vấn có dữ liệu người dùng, dùng SQL parameters, không nối giá trị nhập vào SQL.

## 4. Schema và luật dữ liệu

Schema đang dùng nằm trong `Database/02_CreateTables.sql`. Không tự sửa schema trong lúc làm chức năng.

### CUSTOMER

| Cột DB | Kiểu / luật | Property DTO dự kiến |
|---|---|---|
| `customer_id` | `INT IDENTITY`, khóa chính | `int CustomerId` |
| `full_name` | `NVARCHAR(100) NOT NULL` | `string FullName` |
| `phone` | `VARCHAR(20) NOT NULL` | `string Phone` |
| `email` | `VARCHAR(100) NOT NULL` | `string Email` |
| `date_of_birth` | `DATE NOT NULL` | `DateTime DateOfBirth` |
| `gender` | `NVARCHAR(10) NOT NULL` | `string Gender` |
| `id_passport_number` | `VARCHAR(50) NULL` | `string? IdPassportNumber` |

Số điện thoại là chuỗi, không phải số nguyên: cần giữ số `0` đầu và có thể có dấu `+`. Hộ chiếu nullable vì DB cho phép `NULL`. Bảng hiện không có unique constraint cho phone, email hay hộ chiếu; nếu kiểm tra trùng trong BUS thì đó mới là kiểm tra ở ứng dụng. Không tự thêm constraint DB nếu chưa trao đổi người phụ trách DB.

### EMPLOYEE

| Cột DB | Kiểu / luật |
|---|---|
| `employee_id` | `INT IDENTITY`, khóa chính |
| `full_name` | `NVARCHAR(100) NOT NULL` |
| `phone` | `VARCHAR(20) NOT NULL` |
| `email` | `VARCHAR(100) NOT NULL` |
| `position` | `NVARCHAR(50) NOT NULL` |
| `hire_date` | `DATE NOT NULL` |
| `status` | `VARCHAR(20)`, mặc định `ACTIVE`, chỉ `ACTIVE` hoặc `INACTIVE` |

Bảng hiện không đặt unique constraint cho phone/email. Booking có thể tham chiếu nhân viên; xóa nhân viên đã được dùng có thể bị khóa ngoại chặn. Cần thông báo rõ hoặc chuyển trạng thái sang `INACTIVE` theo nghiệp vụ.

### DISCOUNT_CODE

| Cột DB | Kiểu / luật |
|---|---|
| `discount_id` | `INT IDENTITY`, khóa chính |
| `code` | `VARCHAR(50) NOT NULL`, unique |
| `description` | `NVARCHAR(255) NOT NULL` |
| `discount_type` | chỉ `PERCENT` hoặc `FIXED` |
| `discount_value` | lớn hơn 0; nếu `PERCENT` thì không vượt 100 |
| `min_order_amount` | không âm, mặc định 0 |
| `max_discount_amount` | nullable; nếu có thì không âm |
| `start_date`, `end_date` | `DATETIME2(0)`; end phải sau start |
| `usage_limit` | nullable; nếu có thì không âm và không nhỏ hơn `used_count` |
| `used_count` | không âm, mặc định 0 |
| `status` | chỉ `ACTIVE` hoặc `INACTIVE` |

DB đã chặn mã giảm giá trùng bằng unique constraint. BUS vẫn nên kiểm tra trước để trả thông báo dễ hiểu. Booking có thể tham chiếu discount; xóa mã đã được dùng có thể bị khóa ngoại chặn, nên cần xử lý lỗi hoặc vô hiệu hóa mã theo nghiệp vụ.

### Khác biệt ERD và SQL

ERD gửi kèm có chỗ ghi `varchar` cho trường tên/mô tả và dùng ký hiệu `%/FIXED`; SQL hiện tại dùng một số cột `NVARCHAR` và giá trị `PERCENT`/`FIXED`. Khi code, schema SQL đang có là căn cứ cho tên cột, kiểu và constraint. Nếu nhóm muốn đổi DB, cần trao đổi riêng với người phụ trách database.

## 5. Tài liệu yêu cầu chung của giảng viên

- Tạo lớp kết nối `MyConnection`.
- Tìm kiếm nâng cao trong BUS bằng `ArrayList`; hỗ trợ kết hợp điều kiện `AND`, `OR`, `NOT`, `>`, `>=`, `<`, `<=`, `<>` theo yêu cầu màn hình.
- Có nút hỗ trợ tìm kiếm để chọn bản ghi rồi điền thông tin lên form. Tài liệu lớp có nhắc dùng biến `static` để chuyển thông tin; chỉ dùng ở chỗ thật sự cần chia sẻ lựa chọn giữa form.
- Giao diện chính desktop có menu trái; các bảng quan hệ một-nhiều có thể dùng form chính/form con và liên kết danh sách cha-con. Phần hóa đơn/chi tiết hóa đơn là yêu cầu chung của project, không phải CRUD ba bảng mình đang bắt đầu.
- Đồ án chung còn có yêu cầu thống kê, Excel, báo cáo PDF và báo cáo cuối kỳ; cần phối hợp phạm vi với nhóm.

## 6. Project hiện trạng trước khi làm

- Repo thật nằm ở `E:/Csharp_project/csharp_project` (thư mục con có `.git`).
- Project dùng .NET 10, WinForms, SQL Server và mô hình GUI-BUS-DAO.
- DTO/DAO/BUS ban đầu chỉ là project khung có `Class1`; WinForms còn `Form1` mặc định.
- `FlightBooking.DAO.csproj` đã tham chiếu DTO và package `Microsoft.Data.SqlClient`.
- Hiện đang làm trên `feature/customer-management`, nhánh này đã được tạo trên GitHub và local đang track `origin/feature/customer-management`.

## 7. Code đã làm và cách đọc

### CustomerDTO

File: `src/FlightBooking.DTO/CustomerDTO.cs`.

Đây là khuôn dữ liệu một khách hàng. `get` đọc property, `set` gán property. Các chuỗi bắt buộc khởi tạo bằng `string.Empty` vì project bật nullable reference types. `IdPassportNumber` có dấu `?` vì cột DB nhận `NULL`. `CustomerId` do DB tự tạo khi insert.

### EmployeeDTO và DiscountCodeDTO

Files: `src/FlightBooking.DTO/EmployeeDTO.cs` và `src/FlightBooking.DTO/DiscountCodeDTO.cs`.

`EmployeeDTO.Status` mặc định `ACTIVE`, khớp default và `CHECK` của bảng. Trong `DiscountCodeDTO`, `MaxDiscountAmount` và `UsageLimit` dùng kiểu nullable vì cột DB cho phép `NULL`; `UsedCount` được đọc để hiển thị nhưng không được nhập/sửa từ form giảm giá.

### MyConnection

File: `src/FlightBooking.DAO/MyConnection.cs`.

`CreateConnection()` đọc biến môi trường `FLIGHTBOOKING_DB_CONNECTION`. Nếu chưa có chuỗi kết nối thì ném `InvalidOperationException` với hướng dẫn. Hàm chỉ tạo `SqlConnection`, không mở; DAO mở và tự giải phóng qua `using`. Không ghi mật khẩu vào code hoặc commit.

Biến môi trường này **chưa được cấu hình**, nên hiện chưa thể chạy truy vấn với SQL Server. Cần cấu hình cho tiến trình chạy WinForms trước bước chạy thử.

### CustomerDAO.GetAll

File: `src/FlightBooking.DAO/CustomerDAO.cs`.

Phương thức dùng `SELECT` lấy các cột rõ ràng từ `dbo.CUSTOMER`. `List<CustomerDTO>` chứa 0 hoặc nhiều kết quả. `SqlDataReader.Read()` đi qua từng dòng; mỗi dòng được ánh xạ thành một DTO rồi `Add()` vào danh sách. `GetOrdinal("column")` lấy chỉ số cột theo tên. `IsDBNull()` phải kiểm tra hộ chiếu trước `GetString()`, vì `GetString()` không đọc được SQL `NULL`. Cuối cùng trả về danh sách cho lớp gọi.

Mẫu nhớ: **một dòng SQL -> một DTO; nhiều dòng -> một danh sách DTO.**

### Ba DAO và CRUD

Files: `CustomerDAO.cs`, `EmployeeDAO.cs`, `DiscountCodeDAO.cs` trong `src/FlightBooking.DAO/`.

Mỗi DAO có `GetAll`, `GetById`, `Insert`, `Update`, `Delete`; hàm tra trùng kiểm tra các trường cần thiết. `GetById` trả `null` nếu không thấy. `Insert` không tự gán ID identity. DAO dùng `ExecuteReader()` để đọc nhiều cột, `ExecuteScalar()` để đếm kết quả kiểm tra trùng, và `ExecuteNonQuery()` cho thêm/sửa/xóa. Các truy vấn nhận dữ liệu đều dùng parameter có tên và kiểu SQL rõ ràng.

Khi xóa khách hàng/nhân viên/mã giảm giá đã được booking tham chiếu, database có thể trả lỗi khóa ngoại. DAO không nuốt lỗi đó; GUI sau này cần bắt lỗi và báo cho người dùng. Mình không chạy các lệnh CRUD lên DB trong lúc viết code.

`DiscountCodeDAO` không chèn `used_count` khi tạo (DB mặc định 0) và không cập nhật trường này khi sửa. Đếm lượt dùng là nghiệp vụ của đặt vé. Tham số `DECIMAL(18,2)` khai báo `Precision = 18`, `Scale = 2`; thời gian dùng `DATETIME2(0)`.

### Ba BUS và tìm kiếm bằng ArrayList

Files: `CustomerBUS.cs`, `EmployeeBUS.cs`, `DiscountCodeBUS.cs` và `ArrayListSearch.cs` trong `src/FlightBooking.BUS/`.

BUS chuẩn hóa chuỗi, kiểm tra bắt buộc/độ dài/email/ngày/trạng thái/giá trị giảm, gọi DAO kiểm tra trùng rồi mới thêm hoặc sửa. Các phương thức `Add`, `Update`, `Delete` trả `null` khi thành công hoặc chuỗi tiếng Việt khi nghiệp vụ không hợp lệ/không tìm thấy. Lỗi SQL như khóa ngoại vẫn được để lớp gọi xử lý.

`Search(keyword)` lấy danh sách từ DAO rồi lọc trong BUS, trả kết quả dạng `ArrayList`. `SearchAdvanced(ArrayList conditions)` dùng `SearchConditionDTO` gồm `Field`, `Operator`, `Value`, `LogicalOperator`; hỗ trợ `=`, `<>`, `>`, `>=`, `<`, `<=`, `CONTAINS` và nối điều kiện `AND`, `OR`, `NOT`. Điều kiện được tính lần lượt từ trái sang phải; `NOT` có nghĩa `AND NOT` với điều kiện hiện tại. Trường cần lọc phải là tên property hoặc tên cột tương ứng như `DateOfBirth`/`date_of_birth`.

Giới hạn hiện tại: tìm kiếm nạp các dòng qua DAO rồi lọc trong bộ nhớ để đáp ứng yêu cầu BUS/`ArrayList`; phù hợp dữ liệu đồ án, nhưng có thể tốn tài nguyên nếu bảng rất lớn.

## 8. Tiến độ hiện tại

| Phần | Trạng thái |
|---|---|
| Tạo nhánh cá nhân | Hoàn tất, nhánh remote đã tồn tại |
| DTO cho CUSTOMER, EMPLOYEE, DISCOUNT_CODE | Hoàn tất, có chú thích |
| MyConnection | Hoàn tất; biến môi trường hiện chưa được cấu hình |
| DAO cho ba bảng: đọc, theo ID, trùng, thêm, sửa, xóa | Hoàn tất trong code |
| BUS cho ba bảng: kiểm tra, chuẩn hóa, CRUD, tìm kiếm ArrayList | Hoàn tất trong code |
| WinForms khách hàng | Chưa làm |
| WinForms quản lý nhân viên | Chưa làm |
| WinForms quản lý mã giảm giá | Chưa làm |
| MainForm/menu tích hợp | Chưa làm |
| `dotnet build FlightBookingSystem.slnx` | Thành công, 0 warning, 0 error |
| Chạy thử kết nối/CRUD với SQL Server | Chưa làm; không thực hiện theo yêu cầu không đụng DB |
| Commit code và push | Chưa làm; code và ghi chú hiện chỉ là thay đổi local |
| PR vào develop và review | Chưa làm |

## 9. Cách học và quy trình tiếp theo

Trước mỗi thay đổi code, trình bày mục tiêu, luồng xử lý và bài tập/điểm cần hiểu. Sau khi trao đổi xong mới cập nhật file; khi sửa xong giải thích các dòng/khối mới và cập nhật mục tiến độ trong ghi chú này.

Trạng thái triển khai phần WinForms:

1. Đã tạo màn hình khách hàng, nhân viên và mã giảm giá.
2. Đã tạo `MainForm` có menu và thanh điều hướng giữa ba màn hình.
3. Đã nối CRUD/tìm kiếm với BUS; chưa chạy CRUD trên SQL Server vì thao tác ghi làm thay đổi dữ liệu.
4. Còn cần đồng bộ `develop`, kiểm tra với SQL Server test được phép ghi, rồi tạo PR và xin review.
# WinForms quản lý khách hàng

## Trạng thái triển khai

- Đã tạo `CustomerManagementForm`, `EmployeeManagementForm` và `DiscountCodeManagementForm` với xem, tìm, thêm, cập nhật, xóa và chọn dòng.
- Đã tạo `MainForm` có menu trên và thanh điều hướng; `Program.cs` mở `MainForm`.
- Form nhân viên dùng `EmployeeBUS` để kiểm tra trùng phone/email và trạng thái.
- Form mã giảm giá dùng `DiscountCodeBUS` để kiểm tra mã trùng, loại/giá trị giảm, thời hạn và giới hạn lượt; `UsedCount` chỉ đọc.
- Tìm kiếm mã giảm giá bao gồm min order, max discount, usage limit và used count.
- `dotnet build FlightBookingSystem.slnx -c Release` thành công, 0 cảnh báo, 0 lỗi.
- Project `tests/FlightBooking.ManagementChecks` có 12 check offline cho validation Customer/Employee/DiscountCode và lọc `ArrayList`; cả 12 đều đạt.
- Các check offline không kiểm tra CRUD/duplicate thật với SQL Server; phần đó cần database test được phép ghi.
- Chưa chạy CRUD với SQL Server; cần cấu hình kết nối và dùng database thử nghiệm được phép ghi dữ liệu.

## Luồng dữ liệu

`CustomerManagementForm → CustomerBUS → CustomerDAO → SQL Server`

- Form lấy dữ liệu từ các ô nhập, gọi BUS và hiển thị thông báo/kết quả.
- BUS kiểm tra dữ liệu và gọi DAO; form không tự viết SQL.
- DAO thực hiện câu SQL có tham số; không sửa cấu trúc database khi làm giao diện.

## Các phần giao diện

- Ô tìm kiếm gọi `CustomerBUS.Search`; kết quả `ArrayList` được chuyển thành `List<CustomerDTO>` để gán cho `DataGridView`.
- Bảng chỉ đọc; chọn một dòng sẽ chép dữ liệu vào các ô nhập để cập nhật hoặc xóa.
- Thêm, cập nhật và xóa gọi lần lượt `Add`, `Update`, `Delete` trong BUS.
- ID chỉ đọc vì SQL Server tự tạo; form không gửi ID khi thêm.
- Xóa luôn hỏi xác nhận. Nếu database từ chối vì khách hàng đang được booking tham chiếu, lỗi được hiển thị cho người dùng.
- Nếu chưa cấu hình `FLIGHTBOOKING_DB_CONNECTION` hoặc SQL Server không sẵn sàng, form báo lỗi khi nạp/thao tác dữ liệu.

## Bài tập tự viết lại

1. Giải thích vì sao `AddCustomer` gọi BUS thay vì chứa câu `INSERT`.
2. Viết lại luồng chọn một dòng: `DataBoundItem → CustomerDTO → điền TextBox`.
3. Giải thích `ShowBusinessError`: vì sao `null` nghĩa là không có lỗi.
4. Tập viết lại `SearchCustomers`: gọi BUS, chuyển `ArrayList` sang DTO list, gán `DataSource`.
5. So sánh form nhân viên với form khách hàng: các bước CRUD nào giống nhau?
6. Giải thích vì sao form mã giảm giá không cho sửa `UsedCount`.
7. Lần theo một lần nhấn menu: `MainForm → form con → BUS`.
