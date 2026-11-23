using System.Collections;
using System.Globalization;
using System.Text.RegularExpressions;
using FlightBooking.DAO;
using FlightBooking.DTO;

namespace FlightBooking.BUS;

public sealed class ManagementBUS
{
    private readonly ManagementDAO dao;

    public ManagementBUS(string connectionString)
    {
        dao = new ManagementDAO(connectionString);
    }
    public ArrayList Airports()
    {
        return new ArrayList(dao.GetAirports());
    }

    public ArrayList Flights()
    {
        return new ArrayList(dao.GetFlights());
    }

    public ArrayList Fares(int? flightId = null)
    {
        return new ArrayList(dao.GetFares(flightId));
    }

    public static AirportDTO ValidateAirport(AirportDTO input)
    {
        string airportName = (input.AirportName ?? "").Trim();
        if (airportName.Length == 0 || airportName.Length > 150)
        {
            throw new ManagementException("Tên sân bay bắt buộc, tối đa 150 ký tự.");
        }
        string city = (input.City ?? "").Trim();
        if (city.Length == 0 || city.Length > 100)
        {
            throw new ManagementException("Thành phố bắt buộc, tối đa 100 ký tự.");
        }
        return new AirportDTO(input.AirportId, airportName, city);
    }

    public int SaveAirport(AirportDTO input, AirportDTO? original = null)
    {
        // Giữ API cũ cho các nơi đang gọi Save; mỗi nghiệp vụ có hàm riêng bên dưới.
        if (input.AirportId == 0) return AddAirport(input);
        return UpdateAirport(input, original);
    }

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

    public int UpdateAirport(AirportDTO input, AirportDTO? original)
    {
        AirportDTO airport = ValidateAirport(input);
        if (airport.AirportId <= 0)
        {
            throw new ManagementException("Mã sân bay không hợp lệ.");
        }
        return dao.InTransaction(data =>
        {
            // Khóa và so sánh với dòng đã chọn, không ghi đè thay đổi của người khác.
            if (original == null || data.Airport(airport.AirportId, true) != original)
            {
                throw new ManagementException("Sân bay đã thay đổi. Hãy tải lại rồi chọn lại dòng.");
            }
            if (data.AirportDuplicate(airport))
            {
                throw new ManagementException("Đã có sân bay cùng tên và thành phố.");
            }
            return data.UpdateAirport(airport);
        });
    }

    public void DeleteAirport(AirportDTO original)
    {
        dao.InTransaction(data =>
        {
            if (data.Airport(original.AirportId, true) != original)
            {
                throw new ManagementException("Sân bay đã thay đổi. Hãy tải lại.");
            }
            if (data.AirportUsed(original.AirportId))
            {
                throw new ManagementException("Không xóa sân bay đang được chuyến bay sử dụng.");
            }
            data.DeleteAirport(original.AirportId);
        });
    }

    public static void ValidateFlight(FlightEdit flight)
    {
        if (flight.FlightId < 0 || flight.DepartureAirportId <= 0 || flight.ArrivalAirportId <= 0 ||
            flight.DepartureAirportId == flight.ArrivalAirportId)
        {
            throw new ManagementException("Chọn hai sân bay đi/đến khác nhau.");
        }
        if (flight.TotalSeats <= 0)
        {
            throw new ManagementException("Tổng số ghế phải lớn hơn 0.");
        }
        DateTime departure = flight.DepartureDate.ToDateTime(flight.DepartureTime);
        DateTime arrival = flight.ArrivalDate.ToDateTime(flight.ArrivalTime);
        if (arrival <= departure)
        {
            throw new ManagementException("Ngày giờ đến phải sau ngày giờ đi.");
        }
        if (flight.Status != "SCHEDULED" && flight.Status != "COMPLETED" && flight.Status != "CANCELLED")
        {
            throw new ManagementException("Trạng thái chuyến bay không hợp lệ.");
        }
    }

    public static void ValidateFlightChange(FlightEdit flight, FlightManagementDTO old,
        bool hasBookings, IReadOnlyList<string> activeSeats)
    {
        ValidateFlight(flight);
        FlightDTO original = old.Flight;
        bool countersMatch = old.TicketsSold >= 0 && original.SeatsAvailable >= 0 &&
            original.TotalSeats == old.TicketsSold + original.SeatsAvailable && old.TicketsSold == activeSeats.Count;
        if (!countersMatch)
        {
            throw new ManagementException("Số ghế và vé hiệu lực không khớp. Cần kiểm tra dữ liệu trước khi sửa.");
        }

        if (hasBookings)
        {
            bool routeUnchanged = flight.DepartureAirportId == original.DepartureAirportId &&
                flight.ArrivalAirportId == original.ArrivalAirportId;
            bool scheduleUnchanged = flight.DepartureDate == original.DepartureDate &&
                flight.DepartureTime == original.DepartureTime && flight.ArrivalDate == original.ArrivalDate &&
                flight.ArrivalTime == original.ArrivalTime;
            if (!routeUnchanged || !scheduleUnchanged || flight.Status != original.Status)
            {
                throw new ManagementException("Chuyến đã có Booking/vé: không đổi tuyến, lịch hoặc trạng thái. Cần quy trình xử lý vé riêng.");
            }
        }
        if (flight.TotalSeats < old.TicketsSold)
        {
            throw new ManagementException("Tổng ghế không được nhỏ hơn số vé hiệu lực.");
        }

        if (flight.TotalSeats < original.TotalSeats)
        {
            foreach (string seat in activeSeats)
            {
                Match match = Regex.Match(seat, @"^([1-9][0-9]*)([A-F])$", RegexOptions.IgnoreCase);
                if (!match.Success || !int.TryParse(match.Groups[1].Value, out int row) || row > (int.MaxValue - 6) / 6)
                    throw new ManagementException("Không xác định được vị trí ghế đã cấp; chưa thể giảm tổng ghế.");

                // Sáu ghế A–F mỗi hàng, cùng quy ước cấp ghế của Booking.
                long seatIndex = ((long)row - 1) * 6 + char.ToUpperInvariant(match.Groups[2].Value[0]) - 'A' + 1;
                if (seatIndex > flight.TotalSeats)
                {
                    throw new ManagementException($"Không thể giảm tổng ghế vì ghế {seat} đã được cấp.");
                }
            }
        }
    }

    public int SaveFlight(FlightEdit flight, FlightManagementDTO? original = null)
    {
        if (flight.FlightId == 0) return AddFlight(flight);
        return UpdateFlight(flight, original);
    }

    public int AddFlight(FlightEdit flight)
    {
        ValidateFlight(flight);
        if (flight.FlightId != 0)
        {
            throw new ManagementException("Mã chuyến bay phải bằng 0 khi thêm mới.");
        }
        return dao.InTransaction(data =>
        {
            if (data.Airport(flight.DepartureAirportId) == null || data.Airport(flight.ArrivalAirportId) == null)
            {
                throw new ManagementException("Sân bay không tồn tại.");
            }
            // Chuyến mới chưa bán vé; DAO khởi tạo ghế còn và doanh thu đúng nghiệp vụ.
            return data.InsertFlight(flight, 0);
        });
    }

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

    public void DeleteFlight(FlightManagementDTO original)
    {
        dao.InTransaction(data =>
        {
            int flightId = original.Flight.FlightId;
            if (data.Flight(flightId, true) != original)
            {
                throw new ManagementException("Chuyến bay đã thay đổi. Hãy tải lại.");
            }
            if (data.FlightHasBookings(flightId) || data.FlightHasFares(flightId))
            {
                throw new ManagementException("Không xóa chuyến đã có hạng vé, Booking hoặc vé. Không tự xóa dữ liệu liên quan.");
            }
            data.DeleteFlight(flightId);
        });
    }

    public static FareClassDTO ValidateFare(FareClassDTO input)
    {
        if (input.FareId < 0 || input.FlightId <= 0)
        {
            throw new ManagementException("Chọn chuyến bay hợp lệ.");
        }
        if (input.Price < 0 || input.Price > 9999999999999999.99m || decimal.Round(input.Price, 2) != input.Price)
        {
            throw new ManagementException("Giá vé phải không âm, tối đa 2 chữ số thập phân và trong giới hạn SQL.");
        }
        if (input.BaseBaggageKg < 0)
        {
            throw new ManagementException("Hành lý phải không âm.");
        }
        string className = (input.ClassName ?? "").Trim();
        if (className.Length == 0 || className.Length > 50)
        {
            throw new ManagementException("Tên hạng vé bắt buộc, tối đa 50 ký tự.");
        }
        return new FareClassDTO(input.FareId, input.FlightId, className, input.Price, input.BaseBaggageKg);
    }

    public int SaveFare(FareClassDTO input, FareClassDTO? original = null)
    {
        FareClassDTO fare = ValidateFare(input);
        // Hạng vé có thể chuyển chuyến: giữ chung kiểm tra/khóa hai chuyến cho cả thêm và sửa.
        return dao.InTransaction(data =>
        {
            int firstFlightId = fare.FlightId;
            if (original != null) firstFlightId = original.FlightId;
            int secondFlightId = fare.FlightId;
            // Khóa theo ID tăng dần như Booking để hạn chế deadlock; không khóa lặp một chuyến.
            if (firstFlightId > secondFlightId)
            {
                int temporaryId = firstFlightId;
                firstFlightId = secondFlightId;
                secondFlightId = temporaryId;
            }
            if (data.Flight(firstFlightId, true) == null)
            {
                throw new ManagementException("Chuyến bay không tồn tại.");
            }
            if (secondFlightId != firstFlightId)
            {
                if (data.Flight(secondFlightId, true) == null)
                {
                    throw new ManagementException("Chuyến bay không tồn tại.");
                }
            }

            if (fare.FareId > 0)
            {
                if (original == null || data.Fare(fare.FareId) != original)
                    throw new ManagementException("Hạng vé đã thay đổi. Hãy tải lại.");
                if (original.FlightId != fare.FlightId && data.FareUsed(fare.FareId))
                {
                    throw new ManagementException("Không chuyển hạng đã có vé sang chuyến khác.");
                }
            }
            if (data.FareDuplicate(fare))
            {
                throw new ManagementException("Tên hạng vé đã tồn tại trên chuyến này.");
            }
            return data.SaveFare(fare);
        });
    }

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
}

public static class ManagementSearch
{
    public static readonly string[] AirportFields = { "Mã", "Tên", "Thành phố" };
    public static readonly string[] FlightFields =
    {
        "Mã", "Nơi đi", "Nơi đến", "Ngày đi", "Ngày đến", "Tổng ghế",
        "Đã bán", "Còn ghế", "Doanh thu", "Trạng thái"
    };
    public static readonly string[] FareFields = { "Mã", "Chuyến", "Tên", "Giá", "Hành lý" };

    private static object Value(object? item, string field)
    {
        if (item is AirportDTO airport)
        {
            switch (field)
            {
                case "Mã": return airport.AirportId;
                case "Tên": return airport.AirportName;
                case "Thành phố": return airport.City;
                default: throw new ManagementException("Trường lọc không hợp lệ.");
            }
        }
        if (item is FlightManagementDTO managedFlight)
        {
            FlightDTO flight = managedFlight.Flight;
            switch (field)
            {
                case "Mã": return flight.FlightId;
                case "Nơi đi": return flight.DepartureCity;
                case "Nơi đến": return flight.ArrivalCity;
                case "Ngày đi": return flight.DepartureDate;
                case "Ngày đến": return flight.ArrivalDate;
                case "Tổng ghế": return flight.TotalSeats;
                case "Đã bán": return managedFlight.TicketsSold;
                case "Còn ghế": return flight.SeatsAvailable;
                case "Doanh thu": return managedFlight.TotalRevenue;
                case "Trạng thái": return flight.Status;
                default: throw new ManagementException("Trường lọc không hợp lệ.");
            }
        }
        if (item is FareClassDTO fare)
        {
            switch (field)
            {
                case "Mã": return fare.FareId;
                case "Chuyến": return fare.FlightId;
                case "Tên": return fare.ClassName;
                case "Giá": return fare.Price;
                case "Hành lý": return fare.BaseBaggageKg;
                default: throw new ManagementException("Trường lọc không hợp lệ.");
            }
        }
        throw new ManagementException("Loại dữ liệu không hợp lệ.");
    }

    public static ArrayList Filter(ArrayList source, IReadOnlyList<ManagementFilter> filters,
        string join = "AND", bool not = false)
    {
        if (join != "AND" && join != "OR")
            throw new ManagementException("Chọn AND hoặc OR.");

        // Chỉ dùng điều kiện có nhập giá trị; không sửa danh sách dữ liệu gốc.
        List<ManagementFilter> conditions = new List<ManagementFilter>();
        foreach (ManagementFilter filter in filters)
        {
            if (string.IsNullOrWhiteSpace(filter.Value)) continue;
            switch (filter.Operator)
            {
                case "=": case "<>": case ">": case ">=": case "<": case "<=": case "chứa":
                    conditions.Add(filter);
                    break;
                default:
                    throw new ManagementException("Phép so sánh không hợp lệ.");
            }
        }

        ArrayList result = new ArrayList();
        foreach (object? item in source)
        {
            bool matches = join == "AND";
            foreach (ManagementFilter condition in conditions)
            {
                object actualValue = Value(item, condition.Field);
                bool matchesCondition = Match(actualValue, condition);

                // Vẫn kiểm tra mọi điều kiện để không bỏ qua giá trị nhập sai ở điều kiện sau.
                if (join == "AND")
                {
                    if (!matchesCondition) matches = false;
                }
                else
                {
                    if (matchesCondition) matches = true;
                }
            }
            if (conditions.Count == 0) matches = true;
            if (conditions.Count > 0 && not) matches = !matches;
            if (matches) result.Add(item);
        }
        return result;
    }

    private static bool Match(object value, ManagementFilter filter)
    {
        string text = filter.Value.Trim();
        int comparison;
        if (value is string actualText)
        {
            if (filter.Field == "Trạng thái")
            {
                // Tìm theo nhãn Việt đang thấy trên ListView hoặc theo mã lưu trong SQL.
                string label = actualText;
                if (actualText == "SCHEDULED") label = "Mở bán";
                else if (actualText == "COMPLETED") label = "Đã hoàn thành";
                else if (actualText == "CANCELLED") label = "Đã hủy";

                if (filter.Operator == "chứa")
                    return label.Contains(text, StringComparison.OrdinalIgnoreCase) ||
                        actualText.Contains(text, StringComparison.OrdinalIgnoreCase);

                string normalized = text.ToUpperInvariant();
                if (normalized == "MỞ BÁN") text = "SCHEDULED";
                else if (normalized == "ĐÃ HOÀN THÀNH") text = "COMPLETED";
                else if (normalized == "ĐÃ HỦY") text = "CANCELLED";
            }
            if (filter.Operator == "chứa")
                return actualText.Contains(text, StringComparison.OrdinalIgnoreCase);
            comparison = string.Compare(actualText, text, StringComparison.OrdinalIgnoreCase);
        }
        else if (value is DateOnly date)
        {
            string[] formats = { "dd/MM/yyyy", "yyyy-MM-dd" };
            if (!DateOnly.TryParseExact(text, formats, CultureInfo.InvariantCulture,
                    DateTimeStyles.None, out DateOnly target))
                throw new ManagementException("Ngày lọc dùng dd/MM/yyyy hoặc yyyy-MM-dd.");
            comparison = date.CompareTo(target);
        }
        else
        {
            // Không để TryParse bỏ dấu phân nhóm sai, ví dụ đọc 1.5 thành 15.
            bool validFormat = Regex.IsMatch(text, @"^[+-]?(?:[0-9]+|[0-9]{1,3}(?:\.[0-9]{3})+)(?:,[0-9]+)?$");
            if (!validFormat || !decimal.TryParse(text, NumberStyles.Number,
                    CultureInfo.GetCultureInfo("vi-VN"), out decimal number))
                throw new ManagementException("Giá trị lọc phải là số, ví dụ 1.000.000 hoặc 1,5.");
            comparison = Convert.ToDecimal(value).CompareTo(number);
        }

        switch (filter.Operator)
        {
            case "=": return comparison == 0;
            case "<>": return comparison != 0;
            case ">": return comparison > 0;
            case ">=": return comparison >= 0;
            case "<": return comparison < 0;
            case "<=": return comparison <= 0;
            default: throw new ManagementException("Phép chứa chỉ dùng cho văn bản.");
        }
    }
}
