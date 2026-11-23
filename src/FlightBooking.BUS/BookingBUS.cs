using System.Collections;
using System.ComponentModel.DataAnnotations;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Globalization;
using FlightBooking.DAO;
using FlightBooking.DTO;
using Microsoft.Data.SqlClient;

namespace FlightBooking.BUS;

public sealed class BookingBUS(string connectionString, TimeProvider? clock = null)
{
    private readonly BookingDAO dao = new(new MyConnection(connectionString));
    private DateTime Now => TimeZoneInfo.ConvertTime((clock ?? TimeProvider.System).GetUtcNow(),
        TimeZoneInfo.FindSystemTimeZoneById("SE Asia Standard Time")).DateTime;
    public DateOnly Today => DateOnly.FromDateTime(Now);

    public static DateOnly LatestContactBirthDate(DateOnly today)
    {
        var boundary = today.AddYears(-18);
        return boundary.AddYears(18) < today ? boundary : boundary.AddDays(-1);
    }

    public static string? ContactBirthDateError(DateOnly? birthDate, DateOnly today)
    {
        if (birthDate is not DateOnly dob || dob > today || dob < today.AddYears(-120))
            return "Nhập ngày sinh người đặt hợp lệ, không ở tương lai.";
        return dob.AddYears(18) >= today ? "Người đặt phải đã qua ngày sinh nhật lần thứ 18." : null;
    }

    private static T Database<T>(Func<T> action)
    {
        try { return action(); }
        catch (SqlException error) when (error.Number == 208)
        { throw new BookingException("Database chưa đủ bảng. Cần người phụ trách Database kiểm tra script 01–03 và duyệt script 08."); }
        catch (SqlException error) when (error.Number is 1205 or 2601 or 2627)
        { throw new BookingException("Dữ liệu vừa thay đổi hoặc yêu cầu đang xử lý. Vui lòng gửi lại cùng biểu mẫu."); }
        catch (SqlException)
        { throw new BookingException("Không thể xử lý với SQL Server. Vui lòng thử lại hoặc liên hệ nhóm quản lý."); }
        catch (InvalidOperationException)
        { throw new BookingException("Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB."); }
    }

    public FlightDTO GetFlight(int flightId) => Database(() => dao.Read(d => d.Flight(flightId)));

    public IReadOnlyList<AirportDTO> GetAirports() => Database(() => dao.Read(d => d.Airports()));

    public IReadOnlyList<string> GetFareClassNames() => Database(() => dao.Read(d => d.FareClassNames()));
    public IReadOnlyList<FlightOfferDTO> GetCheapestFlights() => Database(() => dao.Read(d => d.CheapestFlights(Now)));

    public FlightSelectionDTO SelectFlight(int flightId, int fareId, int quantity) => Database(() => dao.Read(d =>
    {
        var quote = MakeQuote(d, flightId, fareId, quantity, null, Now, false, requireSeats: false);
        return new FlightSelectionDTO(quote);
    }));

    public static decimal? ParseVnd(string? text)
    {
        if (string.IsNullOrWhiteSpace(text)) return null;
        text = text.Trim();
        if (text.Length > 32 || !Regex.IsMatch(text, @"^(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]{1,2})?$"))
            throw new BookingException("Nhập giá VND không âm, ví dụ 1.000.000 hoặc 1000000.");
        if (!decimal.TryParse(text.Replace(".", "").Replace(',', '.'), NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture, out var amount) || amount > 9999999999999999.99m)
            throw new BookingException("Giá vượt giới hạn cho phép.");
        return amount;
    }

    public static void ValidatePriceRange(FlightSearch search)
    {
        if (search.MinPrice < 0 || search.MaxPrice < 0 || search.MinPrice > search.MaxPrice)
            throw new BookingException("Giá phải không âm và giá thấp nhất không được lớn hơn giá cao nhất.");
        if (search.FareClassName?.Length > 50) throw new BookingException("Hạng vé không hợp lệ.");
    }

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

    // Advanced predicates are applied in BUS to an ArrayList, never interpolated into SQL.
    public static ArrayList Filter(ArrayList offers, FlightSearch search)
    {
        ValidateSearchDates(search);
        ValidatePriceRange(search);
        if (search.Join is not ("AND" or "OR") || search.Operator is not ("=" or "<>" or ">" or ">=" or "<" or "<="))
            throw new BookingException("Phép lọc không hợp lệ.");
        var className = search.ClassName?.Trim() ?? "";
        if (search.Price < 0 || className.Length > 50) throw new BookingException("Điều kiện lọc không hợp lệ.");
        var result = new ArrayList();
        foreach (FlightOfferDTO offer in offers)
        {
            if (search.ArrivalDate is DateOnly arrival && offer.Flight.ArrivalDate != arrival) continue;
            if (search.MinPrice is decimal min && offer.Fare.Price < min || search.MaxPrice is decimal max && offer.Fare.Price > max) continue;
            if (!string.IsNullOrEmpty(search.FareClassName) && !string.Equals(offer.Fare.ClassName, search.FareClassName, StringComparison.OrdinalIgnoreCase)) continue;
            var conditions = new List<bool>();
            if (search.Price is decimal price)
            {
                var comparison = offer.Fare.Price.CompareTo(price);
                conditions.Add(search.Operator switch
                {
                    "=" => comparison == 0,
                    "<>" => comparison != 0,
                    ">" => comparison > 0,
                    ">=" => comparison >= 0,
                    "<" => comparison < 0,
                    _ => comparison <= 0
                });
            }
            if (className.Length > 0)
                conditions.Add(offer.Fare.ClassName.Contains(className, StringComparison.OrdinalIgnoreCase));
            var matches = conditions.Count == 0 || (search.Join == "AND" ? conditions.All(v => v) : conditions.Any(v => v));
            if (conditions.Count > 0 && search.Not) matches = !matches;
            if (matches) result.Add(offer);
        }
        return result;
    }

    public static void ValidateSearchDates(FlightSearch search)
    {
        if (search.TripType is not ("OneWay" or "RoundTrip")) throw new BookingException("Loại hành trình không hợp lệ.");
        if (search.TripType == "OneWay") search.ReturnDate = null;
        else if (search.ReturnDate is not DateOnly date || date < search.DepartureDate)
            throw new BookingException("Chọn ngày về từ ngày đi trở đi.");
        if (search.ArrivalDate is DateOnly arrival && arrival < search.DepartureDate)
            throw new BookingException("Ngày đến không được trước ngày đi.");
    }

    public BookingQuoteDTO Quote(int flightId, int fareId, int quantity, string? code) => Database(() => dao.Read(d =>
        MakeQuote(d, flightId, fareId, quantity, code, Now, false)));

    private static BookingQuoteDTO MakeQuote(BookingData data, int flightId, int fareId, int quantity,
        string? code, DateTime now, bool locked, bool requireSeats = true)
    {
        if (quantity is < 1 or > 9) throw new BookingException("Mỗi đơn đặt từ 1 đến 9 hành khách.");
        var flight = data.Flight(flightId, locked);
        if (flight.Status != "SCHEDULED" || flight.DepartsAt <= now) throw new BookingException("Chuyến bay không còn nhận đặt vé.");
        if (requireSeats && flight.SeatsAvailable < quantity) throw new BookingException("Chuyến bay không còn đủ chỗ.");
        var fare = data.Fares(flightId, locked).SingleOrDefault(f => f.FareId == fareId)
            ?? throw new BookingException("Hạng vé không thuộc chuyến bay này.");
        var subtotal = fare.Price * quantity;
        DiscountCodeDTO? discount = null;
        if (!string.IsNullOrWhiteSpace(code))
            discount = data.Discount(NormalizeCode(code), locked) ?? throw new BookingException("Mã giảm giá không tồn tại.");
        var amount = CalculateDiscount(discount, subtotal, now);
        return new(flight, fare, quantity, subtotal, amount, subtotal - amount, discount?.DiscountId);
    }

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

    public JourneyQuoteDTO QuoteJourney(BookingRequest request) => Database(() => dao.Read(d => MakeJourneyQuote(d, request, Now, false)));

    private static JourneyQuoteDTO MakeJourneyQuote(BookingData data, BookingRequest request, DateTime now, bool locked)
    {
        NormalizeJourney(request);
        var outbound = MakeQuote(data, request.FlightId, request.FareId, request.Quantity, request.DiscountCode, now, locked);
        BookingQuoteDTO? returning = null;
        if (request.TripType == "RoundTrip")
        {
            returning = MakeQuote(data, request.ReturnFlightId!.Value, request.ReturnFareId!.Value, request.Quantity, null, now, locked);
            ValidateReturn(outbound.Flight, returning.Flight, request.ReturnDate!.Value);
        }
        return new(outbound, returning);
    }

    public static void NormalizeJourney(BookingRequest request)
    {
        if (request.Quantity is < 1 or > 9) throw new BookingException("Chọn từ 1 đến 9 hành khách.");
        if (request.TripType == "OneWay") { request.ReturnDate = null; request.ReturnFlightId = null; request.ReturnFareId = null; }
        else if (request.TripType != "RoundTrip" || request.ReturnDate is null || request.ReturnFlightId is not > 0 || request.ReturnFareId is not > 0)
            throw new BookingException("Chọn đầy đủ chuyến đi, chuyến về và ngày về.");
    }

    public static void ValidateReturn(FlightDTO outbound, FlightDTO returning, DateOnly date)
    {
        if (outbound.FlightId == returning.FlightId || returning.DepartureAirportId != outbound.ArrivalAirportId ||
            returning.ArrivalAirportId != outbound.DepartureAirportId || returning.DepartureDate != date || returning.DepartsAt <= outbound.ArrivesAt)
            throw new BookingException("Lượt về phải đảo chiều hành trình, đúng ngày về và khởi hành sau khi lượt đi hạ cánh.");
    }

    public int Create(BookingRequest request, string accessKey)
    {
        Validate(request, DateOnly.FromDateTime(Now));
        var hash = HashKey(accessKey);
        var payloadHash = Hash(JsonSerializer.Serialize(new
        {
            request.FlightId,
            request.FareId,
            request.Quantity,
            request.TripType,
            request.ReturnDate,
            request.ReturnFlightId,
            request.ReturnFareId,
            request.Customer,
            request.Passengers,
            request.DiscountCode
        }));
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

    private static string ReturnKey(string accessKey) => Hash("return:" + accessKey.ToUpperInvariant());

    public IReadOnlyList<BookingDTO> GetJourney(int bookingId, string accessKey)
    {
        var hash = HashKey(accessKey);
        return Database(() => dao.Read(d =>
        {
            var first = d.Booking(bookingId, hash);
            var result = new List<BookingDTO>();
            if (first is null) return result;
            result.Add(first);
            var returnHash = HashKey(ReturnKey(accessKey));
            if (d.FindRequest(returnHash) is { } second)
                result.Add(d.Booking(second.BookingId, returnHash) ?? throw new BookingException("Không đọc được lượt về."));
            return result;
        }));
    }

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

    public BookingDTO? Get(int bookingId, string accessKey)
    {
        var hash = HashKey(accessKey);
        return Database(() => dao.Read(d => d.Booking(bookingId, hash)));
    }

    public void Cancel(int bookingId, string accessKey)
    {
        var hash = HashKey(accessKey);
        var initial = Database(() => dao.Read(d => d.Booking(bookingId, hash)))
            ?? throw new BookingException("Không tìm thấy đơn hoặc khóa tra cứu không đúng.");
        Database(() => dao.Write(d =>
        {
            d.LockRequest(hash);
            d.Flight(initial.FlightId, true);
            var booking = d.Booking(bookingId, hash, true)
                ?? throw new BookingException("Không tìm thấy đơn hoặc khóa tra cứu không đúng.");
            if (booking.FlightId != initial.FlightId) throw new BookingException("Chuyến bay của đơn đã thay đổi. Vui lòng tải lại.");
            if (booking.Status == "CANCELLED") return 0;
            CheckCancellation(booking, Now);
            d.Cancel(booking);
            return 0;
        }));
    }

    public static void CheckCancellation(BookingDTO booking, DateTime now)
    {
        if (booking.Status != "CONFIRMED" || booking.InvoiceStatus != "UNPAID")
            throw new BookingException("Chỉ hủy trực tuyến đơn chưa thanh toán. Đơn đã thanh toán cần liên hệ nhân viên xử lý hoàn tiền.");
        if (booking.Flight.DepartsAt <= now) throw new BookingException("Không thể hủy sau giờ khởi hành.");
    }

    public static IReadOnlyList<string> AllocateSeats(int totalSeats, HashSet<string> activeSeats, int count)
    {
        // No aircraft layout exists in the schema. Auto-assign six seats per row (A..F).
        var result = new List<string>();
        for (var n = 0; n < totalSeats && result.Count < count; n++)
        {
            var seat = $"{n / 6 + 1}{(char)('A' + n % 6)}";
            if (!activeSeats.Contains(seat)) result.Add(seat);
        }
        if (result.Count != count) throw new BookingException("Không đủ ghế để cấp vé. Vui lòng liên hệ nhân viên.");
        return result;
    }

    public static void ResizePassengers(BookingRequest request, int quantity)
    {
        if (quantity is < 1 or > 9) throw new BookingException("Chọn từ 1 đến 9 hành khách.");
        while (request.Passengers.Count < quantity) request.Passengers.Add(new PassengerInput());
        if (request.Passengers.Count > quantity) request.Passengers.RemoveRange(quantity, request.Passengers.Count - quantity);
        request.Quantity = quantity;
    }

    public static void Validate(BookingRequest request, DateOnly today)
    {
        if (request.Customer is null || request.Passengers is null || request.Passengers.Any(p => p is null))
            throw new BookingException("Thiếu thông tin khách đặt hoặc hành khách.");
        NormalizeJourney(request);
        if (request.Passengers.Count != request.Quantity) throw new BookingException("Số hành khách không khớp số vé đặt.");
        if (request.Customer.Gender == "Nữ") request.Customer.Gender = "Nu";
        request.Customer.FullName = (request.Customer.FullName ?? "").Trim();
        request.Customer.Phone = (request.Customer.Phone ?? "").Trim();
        request.Customer.Email = (request.Customer.Email ?? "").Trim().ToLowerInvariant();
        request.Customer.IdPassportNumber = request.Customer.IdPassportNumber?.Trim();
        request.DiscountCode = NormalizeCode(request.DiscountCode);
        foreach (var passenger in request.Passengers)
        {
            passenger.FullName = (passenger.FullName ?? "").Trim(); passenger.IdCard = (passenger.IdCard ?? "").Trim().ToUpperInvariant();
        }
        var errors = new List<ValidationResult>();
        foreach (var input in new object[] { request, request.Customer }.Concat(request.Passengers.Cast<object>()))
            Validator.TryValidateObject(input, new ValidationContext(input), errors, true);
        if (errors.Count > 0) throw new BookingException(errors[0].ErrorMessage ?? "Thông tin không hợp lệ.");
        if (request.FlightId < 1 || request.FareId < 1 || request.Passengers.Count is < 1 or > 9)
            throw new BookingException("Chọn chuyến bay, hạng vé và 1–9 hành khách.");
        if (ContactBirthDateError(request.Customer.DateOfBirth, today) is { } birthError)
            throw new BookingException(birthError);
        if (request.Customer.Gender is not ("Nam" or "Nu" or "Khác")) throw new BookingException("Giới tính không hợp lệ.");
        if (request.Customer.Email.Any(c => c > 127)) throw new BookingException("Email cần dùng ký tự Latin theo cấu trúc database hiện có.");
        if (request.Passengers.Select(p => p.IdCard).Distinct(StringComparer.OrdinalIgnoreCase).Count() != request.Passengers.Count)
            throw new BookingException("Giấy tờ các hành khách trong cùng đơn không được trùng.");
        if (request.ExpectedTotal < 0 || request.ExpectedTotal > 9999999999999999.99m)
            throw new BookingException("Tổng tiền không hợp lệ.");
    }

    public static string NormalizeCode(string? code)
    {
        var result = (code ?? "").Trim().ToUpperInvariant();
        if (result.Length > 50 || result.Any(c => !char.IsAsciiLetterOrDigit(c) && c is not ('_' or '-')))
            throw new BookingException("Mã giảm giá không hợp lệ.");
        return result;
    }
    public static string NewAccessKey() => Convert.ToHexString(RandomNumberGenerator.GetBytes(32));
    private static string HashKey(string key)
    {
        if (key is null || key.Length != 64 || key.Any(c => !char.IsAsciiHexDigit(c)))
            throw new BookingException("Khóa tra cứu cần đủ 64 ký tự được cấp khi đặt vé.");
        return Hash(key.ToUpperInvariant());
    }
    private static string Hash(string text) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(text)));
}
