# TestPlan — môi trường SQL ghi riêng

Đích allowlist hiện tại: `FlightBooking_Hieu_ManagementTests`. Đây là môi trường
nghiệm thu đã được duyệt, không phải điều kiện để chạy ứng dụng thông thường.
Database hiện tại đã có schema/fixture: **không tạo lại, seed lại, repair hoặc provisioning**.

## File được giữ và dependency

| File | Được dùng ở đâu / mục đích |
|---|---|
| [01_CreateAndSchema.sql](01_CreateAndSchema.sql) | Output `prepare_plan.py:189`; policy Python kiểm SET/guard tại dòng38. Chỉ tái lập DB mới sau phê duyệt tạo riêng. |
| [02_FakeFixtures.sql](02_FakeFixtures.sql) | Output generator dòng283; policy dòng38,43 kiểm SET và gate. Chỉ seed một lần trên DB rỗng được duyệt. |
| [03_Preflight.sql](03_Preflight.sql) | Output generator dòng141; policy dòng43; kiểm 16 gate trước runner bằng sqlcmd, không ghi SQL. |
| [07_VerifyTestWriter_ReadOnly.sql](07_VerifyTestWriter_ReadOnly.sql) | Audit cần cho nghiệm thu hiện tại/lần sau: đo danh tính, token/group/role/quyền hiệu lực của writer thật. Không dùng EXECUTE AS. |
| [prepare_plan.py](prepare_plan.py) | Policy parse biểu thức tại `tests/Shared/sql_test_plan_checks.py:12`, gọi --check ở dòng49; không kết nối SQL. |
| [source-manifest.json](source-manifest.json) | Output generator dòng284, --check đối chiếu fingerprint/định nghĩa nguồn whitelist 02/03/08. |
| [History/01_CreateAndSchema-6EE11AF9.sql](History/01_CreateAndSchema-6EE11AF9.sql) | Policy dòng48 kiểm SHA-256 `6EE11AF95A66B9603CFCA1C963D0D66F0234BF89DA1422B6FBB29011A09A220A`. Giữ nguyên byte để xác minh provenance; không thực thi bản lịch sử. |

README này là chỉ dẫn vận hành, không phải dependency runtime. Verifier schema
trước seed, snapshot thủ công và README lịch sử không được C#/generator/policy
load; đã lưu ngoài repo. Không bỏ kiểm hash SQL lịch sử để xóa file đó.

Cả ba test project copy [SqlTestPreflight.sql](../../tests/Shared/SqlTestPreflight.sql)
vào output và compile [SqlTestPreflight.cs](../../tests/Shared/SqlTestPreflight.cs).
`Verify` đọc SQL từ `AppContext.BaseDirectory`, yêu cầu đủ 16 kết quả PASS.
Thiếu file copy sẽ làm runner thất bại; không thay bằng SQL từ tài liệu lưu trữ.

## Chạy nghiệm thu trên môi trường được duyệt

1. Mở terminal **bằng chính Windows writer đã cấu hình**, kiểm `whoami`.
   Không dùng DELL/sysadmin hoặc EXECUTE AS thay thế. Không lưu mật khẩu.
2. Đóng app/Web/SSMS đang dùng test DB; giữ `exclusive_use`.
3. Chạy audit 07 bằng Windows Authentication, kiểm exit code0 và đúng login/
   server/database. Script 07 hiện gắn với principal/máy nghiệm thu đã được duyệt;
   máy khác cần nhóm review môi trường, không tự bỏ guard hoặc tạo principal.
4. Đặt các biến dưới đây bằng server đã xác minh; chạy preflight 03 rồi runner.

```powershell
$testServer = 'YOUR_VERIFIED_TEST_SERVER'
$testConnection = "Server=$testServer;Database=FlightBooking_Hieu_ManagementTests;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
sqlcmd -S $testServer -E -C -I -d FlightBooking_Hieu_ManagementTests -b -i Database/TestPlan/07_VerifyTestWriter_ReadOnly.sql
if ($LASTEXITCODE -ne 0) { throw 'Identity/permission audit failed' }
sqlcmd -S $testServer -E -C -I -d FlightBooking_Hieu_ManagementTests -b -i Database/TestPlan/03_Preflight.sql -v EXPECTED_SERVER="$testServer" WRITE_APPROVAL="APPROVE_WRITES_TO_FLIGHTBOOKING_HIEU_MANAGEMENTTESTS"
if ($LASTEXITCODE -ne 0) { throw 'Preflight failed' }
$env:FLIGHTBOOKING_EXPECTED_TEST_SERVER = $testServer
$env:FLIGHTBOOKING_SQL_WRITE_APPROVAL = 'APPROVE_WRITES_TO_FLIGHTBOOKING_HIEU_MANAGEMENTTESTS'
$env:FLIGHTBOOKING_MANAGEMENT_TEST_CONNECTION = $testConnection
$env:FLIGHTBOOKING_TEST_CONNECTION = $testConnection
$env:ConnectionStrings__FlightBookingDB = $testConnection
dotnet build FlightBookingSystem.slnx
if ($LASTEXITCODE -ne 0) { throw 'Build failed' }
dotnet run --no-build --project tests/FlightBooking.ManagementChecks -- --sql
if ($LASTEXITCODE -ne 0) { throw 'Management SQL failed' }
dotnet run --no-build --project tests/FlightBooking.Checks -- --sql
if ($LASTEXITCODE -ne 0) { throw 'Booking SQL failed' }
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --sql-sync
if ($LASTEXITCODE -ne 0) { throw 'GUI/Web SQL sync failed' }
```

Runner tự preflight lại trước ghi. Management dùng nhãn MGMT-/ID mới và chỉ dọn
ID của lượt chạy; Booking giữ fixture CHECK- mới; Sync giữ SYNC- mới. Sync dùng
hai sân bay giả đã được seed để tạo chuyến/hạng qua GUI, không nghiệm thu việc
GUI thêm sân bay. Không xóa fixture lịch sử; không mở app khác khi runner bắt đầu.

16 gate giữ server/database/approval, writer profile, main write isolation,
online writable, marker/fingerprint, bảng/cột/constraint, FK/CHECK trusted,
index/unique-seat, DML permissions và exclusive use. SQL>=16 cần VIEW SERVER
PERFORMANCE STATE; bản cũ VIEW SERVER STATE; quyền/version chưa rõ bị chặn.
MyConnection.Open giữ bảy SET options kể cả pooled reopen. Không nới guard.

Kiểm offline sau thay đổi file:

```powershell
python Database/TestPlan/prepare_plan.py --check
python tests/Shared/sql_test_plan_checks.py
```

--check không chạy SQL. Schema nguồn 02/03/08 trong Database hardcode USE
FlightBookingDB; không chạy trực tiếp lên main hoặc nghĩ `-d` đổi được context.
Không tự triển khai BOOKING_WEB_REQUEST vào database chính.
