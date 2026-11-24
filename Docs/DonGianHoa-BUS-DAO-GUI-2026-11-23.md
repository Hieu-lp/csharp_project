# Đơn giản hóa BUS/DAO/GUI theo QLSV3Lop — 23–24/11/2026

Đợt phản hồi bắt đầu ngày 23/11 và bàn giao ngày 24/11/2026 (Asia/Bangkok).

Tiếp tục trên `feature/integration-w7-final`, HEAD `be67664`, giữ working tree
đã có từ đợt trước. Không checkout/reset/stash, không commit/push/merge, không
lấy code Phú. Báo cáo này là kết quả đợt sửa theo phản hồi về Require,
try/catch và tổ chức các hàm; [báo cáo A–J trước](TichHop-W7-Final-2026-11-23.md)
vẫn giữ lịch sử Git, đối chiếu 16 mục W7 và kế hoạch tích hợp của nhóm.

Đã đọc trực tiếp đủ năm file mẫu trong bản giải nén từ QLSV3Lop.rar:
[SinhVienBUS](../tmp/teacher-reference-20261123/QLSV3Lop/BUS/SinhVienBUS.cs),
[SinhVienDAO](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SinhVienDAO.cs),
[SinhVienFRM](../tmp/teacher-reference-20261123/QLSV3Lop/GUI/SinhVienFRM.cs),
[SinhVienDTO](../tmp/teacher-reference-20261123/QLSV3Lop/DTO/SinhVienDTO.cs),
[SqlServerConnect](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SqlServerConnect.cs).
Không sửa/chạy database của mẫu.

| Hàm/lớp mẫu | BookingFlight tương ứng | Quyết định sau đối chiếu |
|---|---|---|
| SinhVienBUS.Them / SinhVienDAO.Them | AddAirport → InsertAirport, AddFlight → InsertFlight; SaveFare với ID=0 | Tách rõ thêm sân bay/chuyến, giữ validation và kiểm tra dữ liệu trước INSERT. |
| SinhVienBUS.Sua / SinhVienDAO.Sua | UpdateAirport, UpdateFlight; SaveFare với ID>0 | Hàm sửa độc lập; giữ snapshot và khóa mà mẫu đơn ứng dụng không có. |
| SinhVienBUS.Xoa / SinhVienDAO.Xoa | DeleteAirport/DeleteFlight/DeleteFare | Mỗi hàm đọc thấy kiểm tra tham chiếu rồi gọi DELETE, không còn kết quả 0 giả ở BUS. |
| DocDSSV | BUS Airports/Flights/Fares → DAO GetAirports/GetFlights/GetFares | BUS nhận danh sách DTO và bọc ArrayList; callback đọc được đưa vào DAO. |
| MaDaTonTai | AirportDuplicate/FareDuplicate trong transaction do BUS điều phối | BUS quyết định từ chối; DAO truy vấn tham số có khóa, không dựa vào cache để kết luận không trùng. |
| TimTheoMa/TimTheoHo/TimKiemNangCao | ManagementSearch.Filter/Match | Giữ nguyên toàn bộ tìm ArrayList tại BUS đúng W7, không đưa tìm nâng cao sang SQL như mẫu. |
| btnThem_Click/btnSua_Click/btnXoa_Click | ManagementPage.Save/Delete | Control → DTO → hàm BUS cụ thể → tải lại/render; không SQL trong GUI. |
| SinhVienDTO | AirportDTO/FlightEdit/FlightManagementDTO/FareClassDTO | Giữ record để so sánh snapshot theo giá trị; không đổi sang class chỉ để giống mẫu. |
| SqlServerConnect.TaoKetNoi | MyConnection.Open, ManagementDAO.OpenConnection | Chỉ DAO mở kết nối; giữ SQL Server và tham số hóa. |

## 1. Các hàm đã bỏ Require

[ManagementBUS](../src/FlightBooking.BUS/ManagementBUS.cs) không còn định nghĩa
hoặc lời gọi Require; cũng bỏ helper Text để đọc được giới hạn chuỗi ngay
trong ValidateAirport/ValidateFare.

- ValidateAirport: trim, tên 1–150 ký tự, thành phố 1–100 ký tự.
- SaveAirport: trở thành điểm phân luồng tương thích; AddAirport/UpdateAirport
  kiểm tra DTO/ID, trùng và snapshot bằng if trực tiếp.
- DeleteAirport: snapshot và sân bay đã được chuyến tham chiếu.
- ValidateFlight: ID/tuyến, tổng ghế, ngày giờ đến sau đi, trạng thái hợp lệ.
- ValidateFlightChange: bộ đếm, tuyến/lịch/trạng thái khi đã có Booking,
  tổng ghế so với vé hiệu lực, vị trí ghế đã cấp khi giảm ghế.
- SaveFlight: phân luồng; AddFlight/UpdateFlight có kiểm tra tồn tại sân bay,
  snapshot/Booking/ghế trong phần nghiệp vụ tương ứng.
- DeleteFlight: snapshot, hạng vé, Booking và vé liên quan.
- ValidateFare: ID/chuyến, giá không âm/trong decimal(18,2), hành lý, tên hạng.
- SaveFare: hai chuyến tồn tại, snapshot hạng, cấm chuyển hạng có vé, trùng tên.
- DeleteFare: chuyến/snapshot và hạng đang được vé tham chiếu.

Giữ thứ tự điều kiện và đoản mạch: ví dụ `A != null && B != null` bị từ chối
bằng `A == null || B == null`, không đổi sang hai truy vấn luôn chạy. Giữ các
thông báo validation cũ; hai hàm chuyến mới có thêm thông báo riêng khi gọi sai
API Add với ID đã có hoặc Update với ID=0. SaveFlight cũ vẫn giữ validation ID
âm/thông báo cũ, được kiểm tra riêng.

## 2. Những try/catch đã tập trung lại

Bỏ **chín khối try/catch, mỗi khối hai nhánh catch** khỏi BUS: Airports,
Flights, Fares, SaveAirport, DeleteAirport, SaveFlight, DeleteFlight, SaveFare,
DeleteFare. Bỏ DatabaseError, ConnectionFailureMessage và using SqlClient
khỏi BUS. Các hàm nghiệp vụ hiện không có try/catch.

[ManagementDAO](../src/FlightBooking.DAO/ManagementDAO.cs) xử lý lỗi truy cập
dữ liệu tại Read và InTransaction; DatabaseError ánh xạ đúng các mã đang dùng:

| Lỗi | Xử lý |
|---|---|
| SQL 547 | Thông báo dữ liệu được tham chiếu hoặc sân bay/chuyến không còn tồn tại. |
| SQL 2601/2627 | Thông báo dữ liệu trùng. |
| SQL 1205 | Thông báo dữ liệu đang được xử lý, tải lại rồi thử. Không tự retry thao tác ghi. |
| SQL khác | Giữ thông báo kiểm tra kết nối/quyền/cấu trúc SQL Server. |
| InvalidOperationException | Giữ thông báo thiếu/chưa kết nối FlightBookingDB. |
| ArgumentException khi mở kết nối | Bắt riêng tại OpenConnection để báo cấu hình sai cú pháp; không bắt rộng mọi ArgumentException của nghiệp vụ. |

Mọi lỗi được chuyển từ DAO đều giữ exception gốc bằng constructor
`ManagementException(message, innerException)` tại
[lớp DTO dùng chung](../src/FlightBooking.DTO/ManagementModels.cs). Không có
tham chiếu DAO → BUS; không thêm một hệ thống exception/phân cấp lớp mới.
Stack trace gốc nằm trong InnerException. ManagementException do BUS chủ động
ném không bị DAO bắt và bọc lại. Không có `throw error`, catch rỗng hoặc trả
thành công khi câu SQL/commit thất bại trong các hàm vừa sửa.

## 3. Những try/catch vẫn giữ

- DAO Read/InTransaction: chuyển lỗi SQL và trạng thái truy cập dữ liệu ở hai
  ranh giới thực sự dùng kết nối. OpenConnection bắt riêng lỗi cú pháp cấu hình.
- [MyConnection.Open](../src/FlightBooking.DAO/MyConnection.cs): Dispose kết
  nối khi mở thất bại và `throw;` để giữ trace. Không sửa hàm này trong đợt mới.
- [ManagementPage.Run](../src/FlightBooking.WinForms/ManagementPage.cs): GUI
  hiện thông báo/ErrorProvider và finally khôi phục trạng thái bận/control.
- Catch sau lần lưu đã commit nhưng ReadAll lỗi: báo “đã lưu nhưng chưa tải
  lại được”, giữ editor về chế độ xem, không tạo điều kiện gửi lại INSERT.
- Catch ở lọc GUI: báo điều kiện lọc sai mà không làm mất vùng nhập liệu.
- Đã rà soát [BookingBUS](../src/FlightBooking.BUS/BookingBUS.cs),
  [BookingDAO](../src/FlightBooking.DAO/BookingDAO.cs) và catch ở Razor Pages.
  Booking có một wrapper lỗi riêng dùng chung và cơ chế xử lý quote/lookup
  hiện có; không sửa đường xử lý này hoặc transaction của Thịnh. Cải thiện
  InnerException ở trên áp dụng cho đường Management, không tuyên bố đã thay
  đổi cơ chế exception của Booking.

## 4. CRUD và lambda tại transaction

Các hàm mới có nghiệp vụ thật, không chỉ gọi vòng lại Save:

- BUS AddAirport kiểm tra DTO/ID/trùng → DAO InsertAirport.
- BUS UpdateAirport kiểm tra DTO/ID/snapshot/trùng → DAO UpdateAirport.
- BUS AddFlight kiểm tra DTO/ID/sân bay → DAO InsertFlight với số vé bán 0.
- BUS UpdateFlight kiểm tra DTO/ID/snapshot/Booking/ghế/sân bay → DAO UpdateFlight.

SaveAirport/SaveFlight ở BUS và ManagementData vẫn giữ API/chữ ký/kết quả cũ
để tương thích. Thêm trả ID sinh bởi SQL; sửa trả số dòng cập nhật. GUI dùng
ID gốc khi sửa, không nhầm số dòng cập nhật thành ID.

SaveFare được giữ một hàm: phần khóa chuyến cũ/mới theo ID tăng dần và kiểm
tra chuyển hạng phải dùng chung. Tách hai bản sẽ lặp đoạn quan trọng này hoặc
thêm lớp/helper khó theo dõi. Đặc biệt API cũ nhận cả original trong lần gọi
ID=0 vẫn được giữ cách khóa theo ngữ cảnh original, không âm thầm bỏ nó.
Nhánh INSERT/UPDATE của ManagementData.SaveFare đã tách bằng if rõ ràng.

Giữ **tám callback nghiệp vụ** trong InTransaction: Add/Update/Delete Airport,
Add/Update/Delete Flight, SaveFare và DeleteFare. `data` là ManagementData
ngắn hạn, dùng chung kết nối và transaction của lần gọi đó. DAO thực thi
callback ngay, rồi commit, rồi mới trả kết quả. Khi có lỗi, transaction chưa
commit được Dispose để rollback và lỗi truyền lên. Kiểm tra trùng, snapshot,
tham chiếu, Booking và ghế đều nằm trong callback nếu cần dữ liệu được khóa.

Không đưa các quyết định nghiệp vụ này xuống DAO để xóa callback về hình thức.
Bỏ callback bằng cách tự mở transaction từ BUS sẽ lộ kết nối SQL hoặc cần thêm
cơ chế session/unit of work; cách đó phức tạp hơn cho phạm vi hiện tại.

DAO có overload InTransaction(Action) cho thao tác không trả kết quả. Chuyển
Action sang bộ quản lý transaction có kết quả chỉ xảy ra bên trong DAO; các hàm
xóa ở BUS không phải viết `return 0`. Write cũ chuyển tiếp vào cùng cơ chế,
không mở transaction lồng nhau. Callback đọc chỉ còn trong các hàm Get của DAO.

Giữ SERIALIZABLE, UPDLOCK/HOLDLOCK, thứ tự khóa chuyến, so snapshot theo giá trị,
trừ số vé đã bán để tính ghế còn và bảo vệ giá lịch sử. Đã đối chiếu tự động:
toàn bộ chuỗi/mảnh SQL giữ nguyên thứ tự; hai hàm InsertFlight/UpdateFlight
giữ đúng danh sách tên tham số và biểu thức giá trị của SaveFlight trước sửa.

## 5. GUI sau khi tổ chức lại

[ManagementPage.Save](../src/FlightBooking.WinForms/ManagementPage.cs) đọc
control → tạo DTO → gọi Add hoặc Update rõ ràng cho sân bay/chuyến → đọc lại
và render. Hạng vẫn gọi SaveFare theo quyết định ở trên. Delete có switch nhiều
dòng, mỗi nhánh gọi đúng hàm BUS, rồi mới tải lại/hiển thị thành công.

Giữ Task.Run để SQL chạy ngoài luồng giao diện, Run để kiểm soát bận/lỗi và
finally, original snapshot, pendingSavedId, ErrorProvider và các trạng thái
editor. Giữ nguyên ShowFares tải mới/chờ lần tải đầu, cảnh báo chưa lưu và bố
cục nút ngoài vùng cuộn. Các lambda sự kiện/async của GUI phục vụ cơ chế
WinForms, khác với callback giữ phạm vi transaction trong DAO.

## 6. Code trước–sau của ba nghiệp vụ

Các đoạn dưới được lấy trực tiếp từ bản sao đầu đợt và source sau sửa, không
phải hàm giả để minh họa.

### Thêm sân bay

Trước: cùng SaveAirport chứa cả thêm/sửa, Require và xử lý lỗi SQL.

```csharp
public int SaveAirport(AirportDTO input, AirportDTO? original = null)
{
    AirportDTO airport = ValidateAirport(input);
    Require(airport.AirportId >= 0, "Mã sân bay không hợp lệ.");
    try
    {
        // DAO.Write giữ chung connection/transaction cho kiểm tra và ghi.
        return dao.Write(data =>
        {
            if (airport.AirportId > 0)
            {
                Require(original != null && data.Airport(airport.AirportId, true) == original,
                    "Sân bay đã thay đổi. Hãy tải lại rồi chọn lại dòng.");
            }
            Require(!data.AirportDuplicate(airport), "Đã có sân bay cùng tên và thành phố.");
            return data.SaveAirport(airport);
        });
    }
    catch (SqlException error) { throw DatabaseError(error); }
    catch (InvalidOperationException) { throw new ManagementException(ConnectionFailureMessage); }
}
```

Sau: hàm AddAirport riêng, đọc được DTO → kiểm tra → INSERT → ID. Hàm sửa
riêng và SaveAirport phân luồng vẫn còn trong source.

```csharp
public int AddAirport(AirportDTO input)
{
    // 1. Chuẩn hóa và kiểm tra DTO trước khi truy cập SQL Server.
    AirportDTO airport = ValidateAirport(input);
    if (airport.AirportId != 0)
    {
        throw new ManagementException("Mã sân bay không hợp lệ.");
    }
    return dao.InTransaction(data =>
    {
        // 2. Kiểm tra trùng và thêm trong cùng transaction để tránh hai người thêm trùng.
        if (data.AirportDuplicate(airport))
        {
            throw new ManagementException("Đã có sân bay cùng tên và thành phố.");
        }
        // 3. DAO INSERT và trả ID mới; DAO chỉ trả kết quả sau khi commit thành công.
        return data.InsertAirport(airport);
    });
}
```

### Sửa chuyến bay

Trước: phần sửa nằm trong SaveFlight cùng phần thêm và try/catch.

```csharp
public int SaveFlight(FlightEdit flight, FlightManagementDTO? original = null)
{
    ValidateFlight(flight);
    try
    {
        return dao.Write(data =>
        {
            int sold = 0;
            if (flight.FlightId > 0)
            {
                FlightManagementDTO? current = data.Flight(flight.FlightId, true);
                if (current == null || original == null || current != original)
                    throw new ManagementException("Chuyến bay/số ghế đã thay đổi. Hãy tải lại và chọn lại dòng.");
                bool hasBookings = data.FlightHasBookings(flight.FlightId);
                List<string> activeSeats = data.ActiveSeats(flight.FlightId);
                ValidateFlightChange(flight, current, hasBookings, activeSeats);
                sold = current.TicketsSold;
            }
            Require(data.Airport(flight.DepartureAirportId) != null && data.Airport(flight.ArrivalAirportId) != null,
                "Sân bay không tồn tại.");
            return data.SaveFlight(flight, sold);
        });
    }
    catch (SqlException error) { throw DatabaseError(error); }
    catch (InvalidOperationException) { throw new ManagementException(ConnectionFailureMessage); }
}
```

Sau: UpdateFlight là một luồng độc lập; các kiểm tra snapshot/Booking/ghế vẫn
ở cùng transaction trước UPDATE.

```csharp
public int UpdateFlight(FlightEdit flight, FlightManagementDTO? original)
{
    // 1. Kiểm tra tuyến, lịch, tổng ghế và trạng thái của DTO.
    ValidateFlight(flight);
    if (flight.FlightId == 0)
    {
        throw new ManagementException("Mã chuyến bay phải lớn hơn 0 khi chỉnh sửa.");
    }
    return dao.InTransaction(data =>
    {
        // 2. Khóa chuyến và kiểm tra snapshot trước khi đọc vé/ghế liên quan.
        FlightManagementDTO? current = data.Flight(flight.FlightId, true);
        if (current == null || original == null || current != original)
        {
            throw new ManagementException("Chuyến bay/số ghế đã thay đổi. Hãy tải lại và chọn lại dòng.");
        }
        // 3. Đối chiếu Booking, bộ đếm và vị trí ghế đã cấp trong chính transaction này.
        bool hasBookings = data.FlightHasBookings(flight.FlightId);
        List<string> activeSeats = data.ActiveSeats(flight.FlightId);
        ValidateFlightChange(flight, current, hasBookings, activeSeats);
        if (data.Airport(flight.DepartureAirportId) == null || data.Airport(flight.ArrivalAirportId) == null)
        {
            throw new ManagementException("Sân bay không tồn tại.");
        }
        // 4. Chỉ cập nhật sau khi tất cả điều kiện hợp lệ; không sửa giá lịch sử/doanh thu.
        return data.UpdateFlight(flight, current.TicketsSold);
    });
}
```

### Xóa hạng vé

Trước: Require, callback có kết quả 0 và try/catch lặp.

```csharp
public void DeleteFare(FareClassDTO original)
{
    try
    {
        dao.Write(data =>
        {
            Require(data.Flight(original.FlightId, true) != null && data.Fare(original.FareId) == original,
                "Hạng vé đã thay đổi. Hãy tải lại.");
            Require(!data.FareUsed(original.FareId), "Không xóa hạng vé đã được vé tham chiếu.");
            data.DeleteFare(original.FareId);
            return 0;
        });
    }
    catch (SqlException error) { throw DatabaseError(error); }
    catch (InvalidOperationException) { throw new ManagementException(ConnectionFailureMessage); }
}
```

Sau: ba bước kiểm tra snapshot → tham chiếu → DELETE; DAO lo lỗi và transaction.

```csharp
public void DeleteFare(FareClassDTO original)
{
    dao.InTransaction(data =>
    {
        // 1. Khóa chuyến, xác minh hạng vẫn giống snapshot đã chọn.
        if (data.Flight(original.FlightId, true) == null || data.Fare(original.FareId) != original)
        {
            throw new ManagementException("Hạng vé đã thay đổi. Hãy tải lại.");
        }
        // 2. Hạng đã có vé tham chiếu không được xóa.
        if (data.FareUsed(original.FareId))
        {
            throw new ManagementException("Không xóa hạng vé đã được vé tham chiếu.");
        }
        // 3. DAO xóa và commit; lỗi ở bất kỳ bước nào sẽ không trả thành công.
        data.DeleteFare(original.FareId);
    });
}
```

Code gần mẫu hơn vì mỗi nghiệp vụ có tên riêng và các if từ chối nằm ngay trong
luồng xử lý, BUS không còn đọc mã lỗi SqlException, GUI gọi đúng Add/Update/Delete.
Những khác biệt giữ lại phục vụ dữ liệu dùng chung giữa Web/WinForms và W7:
transaction/khóa, record snapshot, ArrayList search tại BUS và chạy SQL nền.

## 7. Kết quả mới sau từng phần

| Mốc source được chạy | Build solution | ManagementChecks | BookingChecks |
|---|---|---|---|
| A — bỏ Require/validation trực tiếp | 0 lỗi, 0 cảnh báo | 60 đạt | 82 đạt |
| B — lỗi truy cập dữ liệu ở DAO | 0 lỗi, 0 cảnh báo | 66 đạt | 82 đạt |
| C — tách CRUD, gọi DAO/transaction | 0 lỗi, 0 cảnh báo | 74 đạt | 82 đạt |
| D — GUI gọi nghiệp vụ rõ ràng | 0 lỗi, 0 cảnh báo | 74 đạt | 82 đạt |

14 ca C# mới gồm 8 ca trong
[ExceptionChecks](../tests/FlightBooking.ManagementChecks/ExceptionChecks.cs)
và 6 ca trong [CrudChecks](../tests/FlightBooking.ManagementChecks/CrudChecks.cs):
cấu hình rỗng/sai bị chặn trước kết nối, giữ lỗi gốc/trace, callback ghi/void
không chạy khi kết nối lỗi, thông báo validation không bị bọc lại, kiểm tra ID
của API mới và giữ thông báo API Save cũ. Các ca này **không ghi SQL**.

Vòng cuối trên bản D:

- **75 WinFormsChecks đạt**, giữ 74 ca cũ và thêm đường GUI AddFlight thiếu hai
  sân bay với BUS cấu hình rỗng; ErrorProvider đánh dấu đúng hai ô.
- **17 HTTP chỉ đọc đạt** với website thật; không gửi book/cancel.
- **Hai bộ JavaScript đạt**: search-form.checks.cjs và checkout.checks.cjs.
- git diff --check không báo lỗi; không có diff ở BookingBUS, BookingDAO,
  BookingModels hoặc toàn bộ Web so với origin/develop.
- So sánh cả class ManagementSearch với bản đầu đợt không có thay đổi: giữ
  AND/OR/NOT, nhãn trạng thái Việt và định dạng số Việt đã sửa trước đó.

GUI thực tế vẫn là màn hình 1366×768, DPI 96 (100%). Ba ca yêu cầu cửa sổ
1920×1080 báo SKIP do Windows giới hạn 1386×788. DPI 125%/150% chưa chạy.
Không coi kết quả kiểm tra nút/bố cục là nghiệm thu mọi chuỗi dài của ComboBox.

Lần đầu đã chạy `dotnet build FlightBookingSystem.slnx` kèm restore. Các phần
sau dùng cache package đã restore, không thêm dependency:

```powershell
dotnet build FlightBookingSystem.slnx --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks
node tests/FlightBooking.Checks/search-form.checks.cjs
node tests/FlightBooking.Checks/checkout.checks.cjs
```

Build đầu chờ lâu nhưng hoàn tất đạt; các vòng sau tắt build server/dùng một
node. Không đánh dấu test đạt dựa vào kết quả đợt trước.

## 8. Những phần chưa nghiệm thu SQL

Đã chạy lại preflight **chỉ SELECT** với database context FlightBookingDB trên
server `.`, Windows authentication: 11 bảng gốc tồn tại, thiếu
BOOKING_WEB_REQUEST; các FK kiểm tra được bật/trusted, unique index ghế ISSUED
được bật. Ba kiểm tra lệch bộ đếm ghế, detail/quantity và Booking/vé/chuyến đều
trả 0 lỗi. Số dòng hiện tại: 8 AIRPORT, 21 FLIGHT, 41 FARE_CLASS, 81 BOOKING,
141 TICKET. Chỉ thấy FlightBookingDB ONLINE trong danh sách DB liên quan;
chưa có database test riêng được cho phép.

Chưa chạy CRUD thành công/xóa thật, lỗi FK/trùng/deadlock thực từ SQL,
rollback giữa nhiều câu lệnh, tranh chấp ghế, sửa snapshot đồng thời,
đặt/hủy/hoàn ghế Web, giá lịch sử sau ghi hoặc WinForms cập nhật sau Web ghi.
Ánh xạ 547/2601/2627/1205 được giữ và rà soát ở source, không coi sáu/tám ca
exception cấu hình là bằng chứng đã tái hiện những lỗi SQL này.

Không chạy script 08, seed, sửa schema hoặc tạo DB. Chỉ chạy integration sau
khi có DB test tách biệt, kiểm tra đúng server/DB/quyền/schema và được cho phép
ghi. Hướng dẫn mở Web/WinForms và checklist nghiệm thu còn lại nằm ở
[mục H/J của báo cáo A–J](TichHop-W7-Final-2026-11-23.md).

## 9. File sửa trong đợt phản hồi này

| File | Nội dung |
|---|---|
| [ManagementBUS.cs](../src/FlightBooking.BUS/ManagementBUS.cs) | Bỏ Require/Text/9 try-catch, tách Add/Update sân bay/chuyến, dùng DAO Get/InTransaction. |
| [ManagementDAO.cs](../src/FlightBooking.DAO/ManagementDAO.cs) | Xử lý lỗi tập trung, Get có tên, InTransaction generic/void, Write tương thích, Insert/Update sân bay/chuyến riêng. |
| [ManagementModels.cs](../src/FlightBooking.DTO/ManagementModels.cs) | Thêm constructor ManagementException giữ InnerException. |
| [ManagementPage.cs](../src/FlightBooking.WinForms/ManagementPage.cs) | Sự kiện Save gọi Add/Update, Delete trình bày từng bước; giữ editor/Run/ShowFares. |
| [Program.cs của ManagementChecks](../tests/FlightBooking.ManagementChecks/Program.cs) | Gọi hai nhóm kiểm tra mới, giữ các checks trước. |
| [ExceptionChecks.cs](../tests/FlightBooking.ManagementChecks/ExceptionChecks.cs) | 8 ca exception/callback không kết nối SQL. |
| [CrudChecks.cs](../tests/FlightBooking.ManagementChecks/CrudChecks.cs) | 6 ca API CRUD/validation không kết nối SQL. |
| [InteractionChecks.cs](../tests/FlightBooking.WinFormsChecks/InteractionChecks.cs) | Thêm một ca GUI AddFlight/ErrorProvider. |
| [Báo cáo này](DonGianHoa-BUS-DAO-GUI-2026-11-23.md) | Kết quả 9 mục và code trước–sau thực tế. |
| [Báo cáo A–J](TichHop-W7-Final-2026-11-23.md) | Thêm liên kết tới đợt phản hồi mới; giữ nội dung lịch sử. |

Danh sách trên là thay đổi mới của đợt phản hồi, khác với toàn bộ git diff đã
có sẵn từ đợt trước. Không sửa module Phú/Quân, nguồn Booking của Thịnh,
database script hoặc cấu hình kết nối dùng chung trong đợt này.
