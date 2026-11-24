# Chuẩn hóa Booking và đối chiếu Hiếu–Thịnh — 24/11/2026

Đợt này tiếp tục trên source đã có, không viết lại Booking. Các thay đổi phần
Hiếu ở [bàn giao trước](DonGianHoa-BUS-DAO-GUI-2026-11-23.md) được giữ; GUI Hiếu
chỉ sửa cách đọc/chọn tên sân bay dài. Chưa nghiệm thu đặt/hủy SQL thực tế.

## A. Git và tài liệu gốc

- Nhánh `feature/integration-w7-final`, HEAD ban đầu `81bc540` (message `.`),
  tracking origin cùng nhánh. Working tree ban đầu sạch; người dùng đã commit
  các thay đổi Hiếu từ đợt trước. Không tìm thấy AGENTS.md áp dụng.
- Giữ nguyên ManagementBUS/ManagementDAO/MyConnection, các sửa tìm trạng thái
  Việt/số phân nhóm/ShowFares, snapshot/ghế, editor và các tests có sẵn.
- Không checkout/reset/clean/stash, commit/push/merge/tạo PR; không fetch/lấy
  code hoặc xử lý conflict Phú. Tình trạng PR conflict tại MyConnection do
  người dùng cung cấp; không xác minh lại trạng thái PR bằng GitHub trong đợt này.
- Đã đọc BookingBUS, BookingDAO, DTO, PageModel/BookingDraft thực tế, README,
  [bàn giao Thịnh](Thinh-Booking.md), [bộ lọc/ưu đãi](Booking-Filters-Deals.md),
  [bàn giao Hiếu](Hieu-FlightManagement.md) và các báo cáo gần nhất.
- Kiểm tra lại archive/PDF có hash trùng bản đã đọc, đọc lại source mẫu và nội
  dung W7/3 lớp được trích từ tài liệu gốc. Không chạy chương trình/database mẫu.

| Tài liệu gốc | SHA-256 |
|---|---|
| [QLSV3Lop.rar](../../QLSV3Lop.rar) | E360275C89D00F00FF9750A49644B194CEE3141CB075C4769C9D1F7096C21540 |
| [W7, 7 trang](../../Downloads/C-w7-YeuCauDoAn.pdf) | 560775B0612E50D67C681455E3D161CA684E6EACA2B9A6A5FD441B2D3C403F06 |
| [3 lớp, 32 trang](../../c-3-layer-.pdf) | 04A3CE1E7BDE5184FA8BB139E35BF50A32466840B18B8E23AB56F38B05EA0CDC |

## B. So sánh với QLSV3Lop

| Mẫu đã đọc trực tiếp | BookingFlight sau sửa | Khác biệt cần giữ |
|---|---|---|
| [SinhVienBUS.DocDSSV/TimTheoMa/TimTheoHo](../tmp/teacher-reference-20261123/QLSV3Lop/BUS/SinhVienBUS.cs) | GetAirports/GetFlight/GetFareClassNames, Search/Filter | Danh sách tìm là ArrayList tại BUS; DAO chỉ đọc dữ liệu bằng tham số. |
| SinhVienBUS.Them/Sua/Xoa | Create/CreateLeg, Get/GetJourney, Cancel/CancelJourney | Booking không sửa/xóa tùy ý vé và hóa đơn lịch sử; dùng nghiệp vụ hủy. |
| [SinhVienDAO](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SinhVienDAO.cs): command, reader, DTO | BookingData có hàm INSERT/SELECT/Cancel rõ ràng, mapping tách biến | Một nghiệp vụ Booking dùng chung connection/transaction cho nhiều bảng/hai lượt. |
| [SinhVienFRM](../tmp/teacher-reference-20261123/QLSV3Lop/GUI/SinhVienFRM.cs): nhận control → DTO → BUS → render | Index/Checkout/Lookup/Details nhận HTTP/model, gọi BUS, Page/Redirect | Razor có session, antiforgery, ModelState và access key; giữ công nghệ/bố cục Web. |
| [SinhVienDTO](../tmp/teacher-reference-20261123/QLSV3Lop/DTO/SinhVienDTO.cs) | BookingRequest/CustomerInput/PassengerInput và record DTO | Giữ DataAnnotations và tên/thứ tự property cho binding, snapshot và hash yêu cầu. |
| [SqlServerConnect](../tmp/teacher-reference-20261123/QLSV3Lop/DAO/SqlServerConnect.cs) | MyConnection nằm trong DAO, không sửa | Dùng SQL Server; không chép hậu tố kết nối MySQL hoặc SQL nối chuỗi dữ liệu đầu vào. |

Các bản bàn giao cũ ghi giả định/chức năng theo thời điểm cũ. Ví dụ ghi “không
khứ hồi” không còn mô tả source hiện tại; source đã có hành trình hai lượt.
Đối chiếu đủ 16 mục W7 và trách nhiệm nhóm vẫn ở
[báo cáo A–J trước](TichHop-W7-Final-2026-11-23.md); đợt này không triển khai
thay Quân các phần thống kê, Excel, PDF hoặc hóa đơn ngoài luồng Booking đã có.

## C. File/hàm sửa và ảnh hưởng

| File | Thay đổi |
|---|---|
| [BookingBUS](../src/FlightBooking.BUS/BookingBUS.cs) | Constructor thường, clock rõ; Search/Filter dùng foreach/if/switch thường; CalculateDiscount có nhánh PERCENT/FIXED; CreateLeg tách khỏi hàm lồng; LockFlightsInOrder sắp xếp/bỏ ID lặp; CancelJourney đọc/khóa đủ đơn, kiểm tra đủ rồi mới hủy. Bỏ wrapper Database lồng quanh 14 lời gọi, giữ API công khai cũ. |
| [BookingDAO](../src/FlightBooking.DAO/BookingDAO.cs) | Constructor thường và overload cũ MyConnection; Read/InTransaction xử lý lỗi, Write tương thích, overload void cho hủy; Get có tên rõ. BookingData dùng trường/constructor thường, hàm ghi có thân đầy đủ, reader/map DTO dễ đọc hơn. Không đổi SQL nghiệp vụ. |
| [BookingModels](../src/FlightBooking.DTO/BookingModels.cs) | BookingException constructor thường và overload giữ InnerException. Các record/input/binding attributes còn nguyên. |
| [Index](../src/FlightBooking.Web/Pages/Index.cshtml.cs) | Constructor thường; OnGet gọi RedirectDeal/ShowDealSelection/PrepareSearchFilters/SearchReturningFlights/RedirectSelectedOffer. Giữ thứ tự nhánh, điều kiện, tham số route và ModelState. |
| [Checkout](../src/FlightBooking.Web/Pages/Booking/Checkout.cshtml.cs) | Constructor thường; BackToSearch/PreviewQuote/ConfirmBooking là private. OnPost vẫn kiểm tra action/session/draft; chỉ book mới đi qua ModelState hợp lệ tới Create. |
| [Lookup](../src/FlightBooking.Web/Pages/Booking/Lookup.cshtml.cs), [Details](../src/FlightBooking.Web/Pages/Booking/Details.cshtml.cs) | Constructor thường; giữ quyền tra cứu/hủy và giải thích việc đọc lại chỉ để bổ sung màn hình khi đã có lỗi gốc. |
| [BookingDraft](../src/FlightBooking.Web/BookingDraft.cs) | Load/Save có thân hàm rõ; giữ khóa session, schema JSON và key rotation. |
| [ManagementPage](../src/FlightBooking.WinForms/ManagementPage.cs) | Chỉ hai ComboBox sân bay: đo chữ lúc mở, tăng DropDownWidth trong giới hạn màn hình; tooltip/AccessibleDescription chứa tên đầy đủ trên combo và nhãn còn bật ở chế độ xem; dispose tooltip. Không đổi chiều rộng control/layout hoặc nghiệp vụ Hiếu. |
| [BookingStyleChecks](../tests/FlightBooking.Checks/BookingStyleChecks.cs), [BookingDataAccessChecks](../tests/FlightBooking.Checks/BookingDataAccessChecks.cs), [Program](../tests/FlightBooking.Checks/Program.cs) | Thêm 11 ca lọc/giảm giá/thứ tự validation và 13 ca lỗi cấu hình/giữ nguyên cause/callback không chạy. Không ghi SQL. |
| [IntegrationReadChecks](../tests/FlightBooking.Checks/IntegrationReadChecks.cs), [BrowseChecks](../tests/FlightBooking.Checks/BrowseChecks.cs) | Chuẩn bị đối chiếu airport/flight/fare/quote/search giữa hai BUS bằng SELECT. Chỉ chạy khi bật --browse và có cấu hình đọc rõ ràng. |
| [HTTP chỉ đọc](../tests/FlightBooking.Checks/readonly.http.ps1) | Giữ 17 ca cũ; bổ sung round-trip sai/thiếu, sai fare, tra cứu lỗi không cấp quyền, back/resume draft. Chỉ quote/back/lookup; không book/cancel. |
| [HTTP cấu hình](../tests/FlightBooking.Checks/configuration.http.ps1) | Sáu ca Web thực tế với chuỗi kết nối toàn khoảng trắng: lỗi, form/khóa nút đặt, ModelState, quyền truy cập và antiforgery. UTF-8 BOM để chạy đúng trên Windows PowerShell 5.1. |
| [AirportDropDownChecks](../tests/FlightBooking.WinFormsChecks/AirportDropDownChecks.cs), [GUI Program](../tests/FlightBooking.WinFormsChecks/Program.cs) | Thêm 8 ca chọn/đọc tên dài bằng dữ liệu bộ nhớ, có chế độ --dropdown-only không cần SQL. Bộ kiểm tra GUI cũ vẫn được giữ. |

Lỗi truy cập được chuyển ở DAO thành BookingException dùng chung tại DTO, giữ
exception/stack trace gốc trong InnerException. SQL 208 giữ hướng dẫn thiếu
bảng; 1205/2601/2627 giữ thông báo gửi lại cùng biểu mẫu; SQL khác (gồm 547)
giữ thông báo cũ. InvalidOperation giữ thông báo cấu hình/kết nối. Chuỗi kết
nối sai cú pháp nay cũng được đổi thành BookingException có cause tại bước
mở kết nối, thay vì lỗi ArgumentException thoát khỏi PageModel.

Không tạo lớp trừu tượng/repository/unit of work mới. Không thêm dependency.
Một số lambda đọc và ba callback transaction vẫn được giữ để các quyết định
nghiệp vụ ở BUS dùng đúng context SQL đang mở. Hàm GetCheapestFlights còn
callback để lấy Now sau khi kết nối mở, đúng thời điểm như trước.

## D. Bốn ví dụ code trước–sau thực tế

Các đoạn lấy trực tiếp từ bản sao source tại HEAD 81bc540 và source sau sửa.
Chúng minh họa tổ chức code; kết quả SQL ghi vẫn phải nghiệm thu riêng.

### 1. Tìm chuyến — Search

Trước:

```csharp
public ArrayList Search(FlightSearch search)
{
    ValidateSearchDates(search);
    ValidatePriceRange(search);
    if (search.DepartureAirportId < 1 || search.ArrivalAirportId < 1 || search.DepartureAirportId == search.ArrivalAirportId)
        throw new BookingException("Chọn hai sân bay đi và đến khác nhau.");
    if (search.DepartureDate < DateOnly.FromDateTime(Now) || search.Passengers is < 1 or > 9)
        throw new BookingException("Chọn ngày từ hôm nay và từ 1 đến 9 hành khách.");
    var now = Now;
    var offers = Database(() => dao.Read(d =>
    {
        var result = new ArrayList();
        foreach (var flight in d.Flights(search).Where(f => f.DepartsAt > now))
            foreach (var fare in d.Fares(flight.FlightId)) result.Add(new FlightOfferDTO(flight, fare));
        return result;
    }));
    return Filter(offers, search);
}
```

Sau: kiểm tra đầu vào → đọc chuyến/hạng → lọc ArrayList; không còn wrapper lỗi
lồng bên ngoài, foreach nhìn rõ việc bỏ chuyến đã khởi hành.

```csharp
public ArrayList Search(FlightSearch search)
{
    ValidateSearchDates(search);
    ValidatePriceRange(search);
    if (search.DepartureAirportId < 1 || search.ArrivalAirportId < 1 || search.DepartureAirportId == search.ArrivalAirportId)
        throw new BookingException("Chọn hai sân bay đi và đến khác nhau.");
    if (search.DepartureDate < DateOnly.FromDateTime(Now) || search.Passengers is < 1 or > 9)
        throw new BookingException("Chọn ngày từ hôm nay và từ 1 đến 9 hành khách.");
    var now = Now;
    var offers = dao.Read(d =>
    {
        var result = new ArrayList();
        foreach (FlightDTO flight in d.Flights(search))
        {
            if (flight.DepartsAt <= now) continue;
            foreach (FareClassDTO fare in d.Fares(flight.FlightId))
            {
                result.Add(new FlightOfferDTO(flight, fare));
            }
        }
        return result;
    });
    return Filter(offers, search);
}
```

### 2. Tính tiền giảm giá — CalculateDiscount

Trước:

```csharp
public static decimal CalculateDiscount(DiscountCodeDTO? discount, decimal subtotal, DateTime now)
{
    if (subtotal < 0) throw new BookingException("Tổng tiền không hợp lệ.");
    if (discount is null) return 0;
    if (discount.Status != "ACTIVE" || now < discount.StartDate || now > discount.EndDate)
        throw new BookingException("Mã giảm giá không hoạt động hoặc ngoài thời hạn.");
    if (subtotal < discount.MinOrderAmount) throw new BookingException("Đơn chưa đạt giá trị tối thiểu của mã giảm giá.");
    if (discount.UsageLimit is int limit && discount.UsedCount >= limit) throw new BookingException("Mã giảm giá đã hết lượt.");
    var amount = discount.Type switch
    {
        "PERCENT" => decimal.Round(subtotal * discount.Value / 100m, 2, MidpointRounding.AwayFromZero),
        "FIXED" => discount.Value,
        _ => throw new BookingException("Loại giảm giá không hợp lệ.")
    };
    if (discount.MaxDiscountAmount is decimal cap) amount = Math.Min(amount, cap);
    return Math.Min(amount, subtotal);
}
```

Sau: các nhánh và biến tiền dễ giải thích hơn; thời hạn, điều kiện tối thiểu,
lượt dùng, cách làm tròn và mức chặn vẫn giữ nguyên. Giá vé gốc vẫn do MakeQuote
đọc từ FARE_CLASS, không lấy từ browser.

```csharp
public static decimal CalculateDiscount(DiscountCodeDTO? discount, decimal subtotal, DateTime now)
{
    if (subtotal < 0) throw new BookingException("Tổng tiền không hợp lệ.");
    if (discount is null) return 0;
    if (discount.Status != "ACTIVE" || now < discount.StartDate || now > discount.EndDate)
        throw new BookingException("Mã giảm giá không hoạt động hoặc ngoài thời hạn.");
    if (subtotal < discount.MinOrderAmount) throw new BookingException("Đơn chưa đạt giá trị tối thiểu của mã giảm giá.");
    if (discount.UsageLimit is int limit && discount.UsedCount >= limit) throw new BookingException("Mã giảm giá đã hết lượt.");
    decimal amount;
    if (discount.Type == "PERCENT")
    {
        amount = decimal.Round(subtotal * discount.Value / 100m, 2, MidpointRounding.AwayFromZero);
    }
    else if (discount.Type == "FIXED")
    {
        amount = discount.Value;
    }
    else
    {
        throw new BookingException("Loại giảm giá không hợp lệ.");
    }
    if (discount.MaxDiscountAmount is decimal cap) amount = Math.Min(amount, cap);
    return Math.Min(amount, subtotal);
}
```

### 3. Đặt Booking — Create

Trước, hàm SaveLeg nằm lồng trong transaction:

```csharp
public int Create(BookingRequest request, string accessKey)
{
    Validate(request, DateOnly.FromDateTime(Now));
    var hash = HashKey(accessKey);
    var payloadHash = Hash(JsonSerializer.Serialize(new { request.FlightId, request.FareId, request.Quantity, request.TripType,
        request.ReturnDate, request.ReturnFlightId, request.ReturnFareId, request.Customer, request.Passengers, request.DiscountCode }));
    return Database(() => dao.Write(d =>
    {
        d.LockRequest(hash);
        if (d.FindRequest(hash) is { } previous)
        {
            if (previous.PayloadHash != payloadHash) throw new BookingException("Biểu mẫu đã được đặt với nội dung khác. Hãy bắt đầu đơn mới.");
            return previous.BookingId;
        }
        // Both legs use the same transaction; lock flights in a stable order.
        foreach (var flightId in new[] { request.FlightId, request.ReturnFlightId ?? request.FlightId }.Distinct().Order())
            d.Flight(flightId, true);
        var now = Now;
        var journey = MakeJourneyQuote(d, request, now, true);
        if (request.ExpectedTotal != journey.TotalAmount)
            throw new BookingException("Giá hoặc giảm giá vừa thay đổi. Hãy tính lại giá trước khi xác nhận.");
        var customerId = d.InsertCustomer(request.Customer);
        int SaveLeg(BookingQuoteDTO quote, string requestHash)
        {
            var activeSeats = d.ActiveSeats(quote.Flight.FlightId);
            if (activeSeats.Count != quote.Flight.TotalSeats - quote.Flight.SeatsAvailable)
                throw new BookingException("Số liệu ghế không khớp số vé hiệu lực. Liên hệ nhóm quản lý.");
            var seats = AllocateSeats(quote.Flight.TotalSeats, activeSeats, request.Quantity);
            d.ReserveSeats(quote.Flight.FlightId, request.Quantity);
            if (quote.DiscountId is int discountId) d.UseDiscount(discountId);
            var bookingId = d.InsertBooking(customerId, quote, now);
            var invoiceId = d.InsertInvoice(bookingId, quote, now);
            for (var i = 0; i < request.Quantity; i++)
                d.InsertPassenger(bookingId, invoiceId, quote, request.Passengers[i], seats[i], now);
            d.SaveRequest(requestHash, payloadHash, bookingId);
            return bookingId;
        }
        var id = SaveLeg(journey.Outbound, hash);
        if (journey.Return is not null) SaveLeg(journey.Return, HashKey(ReturnKey(accessKey)));
        return id;
    }));
}
```

Sau, Create điều phối từng bước; CreateLeg nhận cùng context và không mở thêm
connection/commit. Mã Serialize payload giữ nguyên property/thứ tự để không
làm thay đổi idempotency của biểu mẫu cũ.

```csharp
public int Create(BookingRequest request, string accessKey)
{
    // 1. Kiểm tra/chuẩn hóa DTO và giữ nguyên cấu trúc nội dung dùng để chống gửi lặp.
    Validate(request, DateOnly.FromDateTime(Now));
    var hash = HashKey(accessKey);
    var payloadHash = Hash(JsonSerializer.Serialize(new { request.FlightId, request.FareId, request.Quantity, request.TripType,
        request.ReturnDate, request.ReturnFlightId, request.ReturnFareId, request.Customer, request.Passengers, request.DiscountCode }));
    return dao.InTransaction(d =>
    {
        d.LockRequest(hash);
        if (d.FindRequest(hash) is { } previous)
        {
            if (previous.PayloadHash != payloadHash) throw new BookingException("Biểu mẫu đã được đặt với nội dung khác. Hãy bắt đầu đơn mới.");
            return previous.BookingId;
        }
        // 2. Cả hai lượt dùng chung transaction; khóa chuyến theo ID tăng dần.
        LockFlightsInOrder(d, new[] { request.FlightId, request.ReturnFlightId ?? request.FlightId });
        var now = Now;
        var journey = MakeJourneyQuote(d, request, now, true);
        if (request.ExpectedTotal != journey.TotalAmount)
            throw new BookingException("Giá hoặc giảm giá vừa thay đổi. Hãy tính lại giá trước khi xác nhận.");
        // 3. Chỉ ghi sau khi giá được tính lại và đối chiếu với giá đã xác nhận.
        int customerId = d.InsertCustomer(request.Customer);
        int id = CreateLeg(d, request, customerId, payloadHash, now, journey.Outbound, hash);
        if (journey.Return is not null)
        {
            CreateLeg(d, request, customerId, payloadHash, now, journey.Return, HashKey(ReturnKey(accessKey)));
        }
        return id;
    });
}
```

```csharp
private static int CreateLeg(BookingData data, BookingRequest request, int customerId,
    string payloadHash, DateTime now, BookingQuoteDTO quote, string requestHash)
{
    // Hàm nhận context đang có transaction, tuyệt đối không tự mở kết nối/commit lượt riêng.
    HashSet<string> activeSeats = data.ActiveSeats(quote.Flight.FlightId);
    if (activeSeats.Count != quote.Flight.TotalSeats - quote.Flight.SeatsAvailable)
        throw new BookingException("Số liệu ghế không khớp số vé hiệu lực. Liên hệ nhóm quản lý.");
    IReadOnlyList<string> seats = AllocateSeats(quote.Flight.TotalSeats, activeSeats, request.Quantity);
    data.ReserveSeats(quote.Flight.FlightId, request.Quantity);
    if (quote.DiscountId is int discountId) data.UseDiscount(discountId);
    int bookingId = data.InsertBooking(customerId, quote, now);
    int invoiceId = data.InsertInvoice(bookingId, quote, now);
    for (int i = 0; i < request.Quantity; i++)
    {
        data.InsertPassenger(bookingId, invoiceId, quote, request.Passengers[i], seats[i], now);
    }
    data.SaveRequest(requestHash, payloadHash, bookingId);
    return bookingId;
}
```

### 4. Hủy hành trình — CancelJourney

Trước:

```csharp
public void CancelJourney(int bookingId, string accessKey)
{
    var hash = HashKey(accessKey);
    var initial = GetJourney(bookingId, accessKey);
    if (initial.Count == 0) throw new BookingException("Không tìm thấy hành trình hoặc khóa tra cứu không đúng.");
    Database(() => dao.Write(d =>
    {
        d.LockRequest(hash);
        foreach (var flightId in initial.Select(b => b.FlightId).Distinct().Order()) d.Flight(flightId, true);
        var bookings = initial.Select((b, i) => d.Booking(b.BookingId, i == 0 ? hash : HashKey(ReturnKey(accessKey)), true)
            ?? throw new BookingException("Hành trình đã thay đổi.")).ToList();
        for (var i = 0; i < bookings.Count; i++)
        {
            if (bookings[i].FlightId != initial[i].FlightId) throw new BookingException("Chuyến bay đã thay đổi. Hãy tải lại.");
            if (bookings[i].Status != "CANCELLED") CheckCancellation(bookings[i], Now);
        }
        foreach (var booking in bookings.Where(b => b.Status != "CANCELLED")) d.Cancel(booking);
        return 0;
    }));
}
```

Sau: khóa ID tăng dần, đọc/khóa đủ các đơn, kiểm tra đủ các lượt trước khi hủy.
Callback void không cần kết quả 0 giả ở BUS.

```csharp
public void CancelJourney(int bookingId, string accessKey)
{
    var hash = HashKey(accessKey);
    var initial = GetJourney(bookingId, accessKey);
    if (initial.Count == 0) throw new BookingException("Không tìm thấy hành trình hoặc khóa tra cứu không đúng.");
    dao.InTransaction(d =>
    {
        d.LockRequest(hash);
        List<int> flightIds = new List<int>();
        foreach (BookingDTO booking in initial) flightIds.Add(booking.FlightId);
        LockFlightsInOrder(d, flightIds);

        // Đọc/khóa đủ các đơn trước, kiểm tra đủ các lượt rồi mới hủy bất kỳ lượt nào.
        List<BookingDTO> bookings = new List<BookingDTO>();
        for (int i = 0; i < initial.Count; i++)
        {
            string bookingHash = i == 0 ? hash : HashKey(ReturnKey(accessKey));
            BookingDTO? booking = d.Booking(initial[i].BookingId, bookingHash, true);
            if (booking is null) throw new BookingException("Hành trình đã thay đổi.");
            bookings.Add(booking);
        }
        for (var i = 0; i < bookings.Count; i++)
        {
            if (bookings[i].FlightId != initial[i].FlightId) throw new BookingException("Chuyến bay đã thay đổi. Hãy tải lại.");
            if (bookings[i].Status != "CANCELLED") CheckCancellation(bookings[i], Now);
        }
        foreach (BookingDTO booking in bookings)
        {
            if (booking.Status != "CANCELLED") d.Cancel(booking);
        }
    });
}
```

## E. Cơ chế bắt buộc giữ nguyên

| Cơ chế | Đối chiếu source sau sửa |
|---|---|
| Transaction | InTransaction mở một MyConnection và một SERIALIZABLE; chỉ commit sau callback. CreateLeg dùng context được truyền vào, cả hai lượt chung giao dịch. Dispose transaction chưa commit khi lỗi. |
| Thứ tự khóa | LockRequest/sp_getapplock trước, rồi flight ID tăng dần/bỏ lặp. LockFlightsInOrder dùng List.Sort + foreach thay Distinct.Order, không mở kết nối mới. |
| Gửi lặp/quyền | HashKey/ReturnKey, payload SHA-256 và property/thứ tự JSON giữ nguyên. FindRequest kiểm tra nội dung; cùng request trả đơn cũ; khác nội dung bị từ chối. Lookup vẫn cần khóa riêng và chỉ cấp session khi xác minh thành công. |
| Ghế | So activeSeats với total−available, AllocateSeats A–F, ReserveSeats UPDATE có điều kiện đủ ghế/status. Còn unique index ISSUED và FK fare–flight trong schema. |
| Giá | MakeQuote lấy Fare từ DB; Create tính lại sau khóa và so ExpectedTotal. BOOKING_DETAIL.price/INVOICE_DETAIL.unit_price lưu giá tại lúc đặt. SaveFare của Hiếu không sửa các bảng lịch sử. |
| Hủy | Khóa request/chuyến/Booking/Invoice; chỉ đơn CONFIRMED + UNPAID trước khởi hành. Hủy lặp không hoàn thêm, cả hai lượt được kiểm tra trước lần ghi đầu tiên. |
| Doanh thu | INSERT Invoice UNPAID; ReserveSeats không cộng total_revenue, không biến đơn chưa trả tiền thành tiền đã thu. Quy trình thu tiền/thống kê thuộc Quân. |
| GUI/HTTP | Giữ antiforgery, session, model binding, tên handler/route, ModelState, redirect, số khách/khứ hồi và các tài sản HTML/CSS/JS. Các helper PageModel mới là private. |
| Exception | DAO giữ InnerException khi đổi lỗi; lỗi nghiệp vụ không bị bọc lại. Các catch fallback chỉ giữ lỗi chính/ModelState khi dữ liệu phụ không tải được, không báo giao dịch ghi thành công giả. |

Đã so sánh tự động **113 chuỗi/mảnh SQL, tên tham số và thông báo** trong
BookingData: không đổi nội dung hoặc thứ tự. Không sửa MyConnection hoặc
script SQL để giải quyết conflict Phú. Đây là đối chiếu source, không phải
bằng chứng transaction/rollback đã chạy SQL thành công.

## F. Kết quả kiểm tra và gián đoạn môi trường

| Mốc thực sự đã chạy trong đợt này | Build | Management | Booking | Khác |
|---|---|---|---|---|
| Baseline mới tại 81bc540, trước sửa | 0 lỗi/0 cảnh báo | 74 | 82 | 75 GUI, 17 HTTP, 2 bộ JS đạt |
| A — tổ chức BookingBUS | 0 lỗi/0 cảnh báo | 74 | 93 | Giữ 82 ca cũ + 11 ca mới |
| B — BookingDAO/exception | 0 lỗi/0 cảnh báo | 74 | 106 | Thêm 13 ca cấu hình/cause/callback |
| C — PageModel, kiểm tra lại sau gián đoạn | 0 lỗi/0 cảnh báo | 74 | 106 | 2 bộ JS đạt |
| D — bổ sung kiểm tra tích hợp chỉ đọc | 0 lỗi/0 cảnh báo | 74 | 106 | Bộ --browse/HTTP mở rộng cần SQL chạy |
| E — sửa hiển thị tên sân bay, bản cuối | 0 lỗi/0 cảnh báo | 74 | 106 | 8 GUI --dropdown-only, 6 HTTP cấu hình, 2 bộ JS đạt |

Sau lúc tiến trình bị gián đoạn, lượt GUI tải dữ liệu thật thất bại tại
“flight page loads through BUS/DAO”. Chẩn đoán cho thấy **MSSQLSERVER Stopped**;
sqlcmd SELECT cũng không kết nối được. Lệnh Start-Service đã thử nhưng tài
khoản tiến trình không mở/khởi động được dịch vụ. Đã yêu cầu người dùng khởi
động bằng Services/PowerShell administrator. Không thay connection/schema
để né lỗi này và không coi lượt GUI thất bại là PASS.

Vì vậy **75 GUI/17 HTTP trong baseline không được dùng làm kết quả cuối**.
Tám ca GUI mới chạy trên Windows thật bằng dữ liệu hiển thị bộ nhớ và BUS cấu
hình rỗng, không truy cập SQL: đo tên dài vượt ô đóng, mở dropdown native đủ
rộng mà không đổi layout, kiểm tra nội dung tooltip/nhãn, chọn đúng ID và cập
nhật hint cho cả sân bay đi/đến. Chúng không thay thế kiểm tra dữ liệu sống.

Sáu HTTP cấu hình chạy website thật tại cổng 5088 với chuỗi kết nối chỉ có
khoảng trắng, DAO từ chối trước khi mở SQL: Index/Checkout hiển thị lỗi, form
Checkout được giữ nhưng nút đặt bị khóa khi không có quote, Lookup hiển thị
ModelState trước BUS, Details thiếu quyền chuyển về Lookup và POST thiếu
antiforgery bị chặn 400. Website kiểm tra đã tự dừng. Đây là kết quả runtime
của đường lỗi/cấu hình, không phải kết quả 17/24 HTTP với dữ liệu SQL thật.

Các vòng layout baseline trên màn hình 1366×768, DPI 96/100%; 1920×1080 bị
Windows giới hạn còn 1386×788 nên SKIP. DPI 125%/150% chưa chạy. Không đổi
thiết lập độ phân giải/DPI của máy để giả lập kết quả.

## G. Điểm chưa nghiệm thu

- Sau gián đoạn: GUI cần SQL, HTTP mở rộng và bộ đối chiếu --browse đang chờ
  dịch vụ SQL chạy lại. Không báo đã đạt các ca chưa chạy này.
- BOOKING_WEB_REQUEST thiếu theo báo cáo trước; đợt này chưa xác minh lại
  metadata sau gián đoạn vì dịch vụ dừng. Không chạy script 08 hoặc seed.
- Chưa có database test riêng được xác nhận/cho phép ghi. Không thử CRUD tạo
  chuyến/hạng thật, đặt/hủy, rollback, tranh chấp ghế/discount, gửi lặp hoặc
  sửa giá rồi kiểm tra lịch sử trên FlightBookingDB chính.
- Chưa chứng minh bằng SQL chu trình Web ghi → WinForms Tải lại, hoặc việc
  chuyến mới tạo qua WinForms xuất hiện trên Web. Đường đi đã đối chiếu ở source;
  kiểm tra đọc dữ liệu có sẵn không thay thế chu trình ghi đó.
- Màn hình/DPI còn thiếu như mục F. Tên rất dài hơn cả chiều rộng màn hình
  được giới hạn dropdown theo vùng làm việc, có hint đầy đủ; cần xem thêm ở
  các DPI thực tế.
- PR conflict Phú và quy tắc dùng chung CUSTOMER/DISCOUNT_CODE vẫn chờ nhóm.
  Booking đang tạo snapshot khách liên hệ; không tự gộp hoặc đổi quy tắc định
  danh khách khi chưa tích hợp Phú. Các phần Quân không được sửa thay.

## H. Tám luồng hàm để học và bảo vệ

1. **IndexModel.OnGet → BookingBUS.Search/Filter → BookingData.Flights/Fares →
   SQL Server.** HTTP nơi đi/đến/ngày/số người thành FlightSearch; BUS kiểm tra
   hành trình và giờ Việt Nam, DAO đọc chuyến/hạng; BUS lọc ArrayList; Page render
   Offers. SearchReturningFlights là bước chọn kết quả hiển thị lượt về, còn
   ValidateReturn tại BUS tiếp tục bảo vệ tuyến/ngày/giờ khi quote/xác nhận.
2. **RedirectDeal/ShowDealSelection → SelectFlight → MakeQuote → Flight/Fares.**
   Input là ID chuyến/hạng và số khách. Giá/tuyến/ngày được đọc từ server;
   DTO FlightSelectionDTO.HasSeats quyết định có cho tiếp tục. Xem chuyến có
   thể báo thiếu ghế, Quote/Create mới từ chối xác nhận.
3. **CheckoutModel.OnGet/PreviewQuote → QuoteJourney/MakeJourneyQuote →
   MakeQuote/CalculateDiscount → Flight/Fares/Discount.** Input request và mã;
   output JourneyQuoteDTO tổng hai lượt, giảm chỉ lượt đi theo nghiệp vụ cũ.
   BUS kiểm tra hạng đúng chuyến, ngày/ghế, hạn mã/lượt/đơn tối thiểu, làm tròn
   AwayFromZero, chặn mức giảm theo cap/subtotal. GUI chỉ nhận tổng giá.
4. **OnPost/ConfirmBooking → Validate/Create → InTransaction.** ModelState và
   draft/session phải đúng; BUS chuẩn hóa khách/hành khách/giấy tờ, khóa request,
   so payload hash, khóa chuyến và quote lại giá. ExpectedTotal khác thì từ chối.
   Output ID chỉ sau commit. SQL ghi chưa nghiệm thu.
5. **Create → CreateLeg → ActiveSeats/ReserveSeats/InsertBooking/InsertInvoice/
   InsertPassenger/SaveRequest.** Input cùng context, request, quote, customerId,
   hash và giờ đặt. Kiểm tra ghế, cấp ghế, trừ ghế/lượt mã, ghi vé/detail/giá lịch
   sử và Invoice UNPAID; hai lượt không có commit riêng. SQL ghi chưa nghiệm thu.
6. **LookupModel.OnPost → BookingBUS.Get/GetJourney → GetBooking/Booking/Details.**
   Input ID + khóa 64 hex; BUS hash, DAO JOIN BOOKING_WEB_REQUEST/CUSTOMER/
   INVOICE và đọc detail. Page chỉ cấp session sau xác minh; không dùng ID đơn
   một mình để mở dữ liệu. CUSTOMER được đọc qua JOIN, không có hàm SelectCustomer
   độc lập trong luồng hiện tại.
7. **DetailsModel.OnPostCancel → CancelJourney/CheckCancellation → LockRequest/
   LockFlightsInOrder/Booking/Cancel.** Khóa và đọc đủ các đơn trước, kiểm tra
   trạng thái/UNPAID/khởi hành cho mọi lượt; DAO chuyển trạng thái và hoàn ghế/
   lượt mã trong cùng transaction. Hủy lại không hoàn thêm. SQL hủy chưa nghiệm thu.
8. **WinForms Reload/ReadAll → ManagementBUS.Flights/Fares → ManagementDAO,
   rồi Booking Search/Quote đọc cùng FLIGHT/FARE_CLASS.** Cùng chuỗi kết nối
   mới đảm bảo cùng dữ liệu. Tải lại lấy ghế/giá mới; snapshot cũ bị từ chối khi
   sửa. Chưa thử Web ghi trong đợt này; không gọi đây là đồng bộ realtime.

Hàm ở các luồng trên đều có tên thật trong
[BUS](../src/FlightBooking.BUS/BookingBUS.cs), [DAO](../src/FlightBooking.DAO/BookingDAO.cs),
[PageModels](../src/FlightBooking.Web/Pages/Booking/) và
[GUI quản lý](../src/FlightBooking.WinForms/ManagementPage.cs).

## I. Mở và kiểm tra trên Windows

Khởi động SQL Server (MSSQLSERVER) bằng Services nếu cần; tài khoản hiện tại
của tiến trình không đủ quyền start. Không tạo lại DB hoặc chạy schema/seed.
Mở hai PowerShell tại repository, đặt chuỗi kết nối trong từng cửa sổ:

```powershell
Set-Location 'E:\csharp_project'
$env:ConnectionStrings__FlightBookingDB = 'Server=.;Database=FlightBookingDB;Integrated Security=True;Encrypt=True;TrustServerCertificate=True'
dotnet build FlightBookingSystem.slnx --no-restore --disable-build-servers -m:1 -p:UseSharedCompilation=false
```

Cửa sổ WinForms:

```powershell
dotnet run --no-build --project src/FlightBooking.WinForms
```

Xem/lọc/chọn chuyến và hạng, Tải lại; khi Thêm mới/Chỉnh sửa mở danh sách sân
bay để đọc đủ tên, rê vào nhãn Sân bay đi/đến để xem hint khi chế độ xem khóa
ComboBox. Không Lưu hợp lệ/Xóa trên DB chính.

Cửa sổ website:

```powershell
dotnet run --no-build --project src/FlightBooking.Web --no-launch-profile -- --urls http://127.0.0.1:5087 --environment Development
```

Mở [trang chủ](http://127.0.0.1:5087/), chọn dữ liệu hiện có, xem giá/áp mã/
quay lại; không Đặt/Hủy. [Tra cứu](http://127.0.0.1:5087/Booking/Lookup) cần
schema request và khóa hợp lệ để đọc đơn thực. Ctrl+C dừng Web, đóng form dừng GUI.

Kiểm tra không cần SQL:

```powershell
dotnet run --no-build --project tests/FlightBooking.ManagementChecks
dotnet run --no-build --project tests/FlightBooking.Checks
node tests/FlightBooking.Checks/search-form.checks.cjs
node tests/FlightBooking.Checks/checkout.checks.cjs
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --dropdown-only
```

Để kiểm tra riêng đường lỗi cấu hình không kết nối SQL, mở Web ở cửa sổ khác
với `$env:ConnectionStrings__FlightBookingDB = '   '` và cổng 5088, rồi chạy
`& tests/FlightBooking.Checks/configuration.http.ps1 -BaseUrl 'http://127.0.0.1:5088'`.
Khôi phục chuỗi kết nối đúng trong cửa sổ đó trước khi thử dữ liệu thật.

Khi SQL đã chạy, chỉ đọc dữ liệu có sẵn:

```powershell
$env:FLIGHTBOOKING_UI_READ_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.WinFormsChecks -- --interaction --layout
$env:FLIGHTBOOKING_BROWSE_CONNECTION = $env:ConnectionStrings__FlightBookingDB
dotnet run --no-build --project tests/FlightBooking.Checks -- --browse
# Khi Web ở cổng 5087 đang chạy:
& tests/FlightBooking.Checks/readonly.http.ps1 -BaseUrl 'http://127.0.0.1:5087'
```

--browse chỉ SELECT/search/quote, đối chiếu hai BUS; không bật --sql hoặc
--demo-rollback trên DB chính. Script HTTP whitelist quote/back/lookup và không
gửi book/cancel. Nếu thiếu cặp chuyến tương lai đảo chiều, ghi SKIP thay vì tự
tạo chuyến trên DB chính. Kiểm tra số ghế sau Web ghi chỉ thực hiện trên DB test
được phép, sau đó bấm Tải lại trên WinForms cùng DB.

Checklist màn hình: 1366×768 và 1920×1080 ở 100/125/150% thực tế; mở form mới
sau khi đổi DPI; thử tên sân bay dài, mở dropdown/chọn đúng ID, hover nhãn ở chế
độ xem; thu/phóng form/mở bộ lọc/scroll vùng nhập, xác nhận Lưu/Hủy còn nhìn thấy;
thử picker Esc và cảnh báo chưa lưu. Ghi kích thước/DPI thực, ảnh và tổ hợp đạt.

## J. Công việc tiếp theo

1. SQL Server chạy lại: xác minh kết nối/DB_NAME và preflight chỉ đọc, chạy lại
   bộ GUI đầy đủ, --browse và HTTP mở rộng; cập nhật mục F theo kết quả thực.
2. Nhóm xác nhận/cho phép một DB test riêng, tài khoản/quyền giới hạn đúng DB,
   dữ liệu giả, đủ FK/index/bảng request. Duyệt việc áp dụng schema lên DB test;
   không chỉ đổi tham số -d rồi chạy script còn USE FlightBookingDB.
3. Trên DB test mới chạy tạo danh mục → tìm trên Web → quote → đặt/hủy →
   WinForms Tải lại; kiểm tra gửi lặp, tranh chấp ghế/discount, lỗi giữa hai
   lượt/rollback, snapshot cũ, giá lịch sử và doanh thu UNPAID/PAID theo phân công.
4. Nghiệm thu các độ phân giải/DPI còn thiếu trên thiết bị phù hợp.
5. Khi được người dùng cho phép, xử lý PR/conflict Phú theo kế hoạch riêng,
   thống nhất kết nối/CUSTOMER/DISCOUNT_CODE và chạy lại các bộ tests chung.
   Không dùng đợt chuẩn hóa này để tự merge hoặc sửa thay module Phú/Quân.

Các kiểm tra có thể chạy độc lập đã hoàn tất; các giới hạn SQL/GUI dữ liệu
thật ở trên được giữ rõ để bàn giao, không thay bằng kết quả lịch sử.
