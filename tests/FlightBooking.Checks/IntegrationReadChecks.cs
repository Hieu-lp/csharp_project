using FlightBooking.BUS;
using FlightBooking.DTO;

internal static class IntegrationReadChecks
{
    public static void Run(Action<bool, string> check, string connectionString)
    {
        // Chỉ đọc danh mục/tìm chuyến/xem giá; không gọi Save, Create, Cancel hoặc SQL script.
        var management = new ManagementBUS(connectionString);
        var booking = new BookingBUS(connectionString);
        var airports = management.Airports().Cast<AirportDTO>().OrderBy(a => a.AirportId).ToArray();
        var flights = management.Flights().Cast<FlightManagementDTO>().ToArray();
        var fares = management.Fares().Cast<FareClassDTO>().ToArray();
        int checks = 0;
        void Compare(bool value, string label)
        {
            check(value, "SQL-READ integration: " + label);
            checks++;
        }

        Compare(airports.SequenceEqual(booking.GetAirports().OrderBy(a => a.AirportId)), "same airport catalog in Management and Booking");
        foreach (FlightManagementDTO managed in flights)
        {
            Compare(booking.GetFlight(managed.Flight.FlightId) == managed.Flight,
                "same route/schedule/seats/status for flight " + managed.Flight.FlightId);
        }
        foreach (FlightOfferDTO deal in booking.GetCheapestFlights())
        {
            FareClassDTO fare = fares.Single(f => f.FareId == deal.Fare.FareId);
            Compare(fare == deal.Fare, "same fare and server price for fare " + fare.FareId);
            int quantity = Math.Min(2, deal.Flight.SeatsAvailable);
            BookingQuoteDTO quote = booking.Quote(deal.Flight.FlightId, fare.FareId, quantity, null);
            Compare(quote.Fare == fare && quote.Subtotal == fare.Price * quantity &&
                quote.TotalAmount == quote.Subtotal && quote.DiscountAmount == 0,
                "server quote uses Management fare price for flight " + deal.Flight.FlightId);
            var search = new FlightSearch
            {
                DepartureAirportId = deal.Flight.DepartureAirportId,
                ArrivalAirportId = deal.Flight.ArrivalAirportId,
                DepartureDate = deal.Flight.DepartureDate, Passengers = quantity
            };
            Compare(booking.Search(search).Cast<FlightOfferDTO>().Any(o => o.Flight.FlightId == deal.Flight.FlightId && o.Fare == fare),
                "eligible catalog flight/fare appears in Booking search " + deal.Flight.FlightId);
        }

        DateTime now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime;
        FlightDTO[] future = flights.Select(m => m.Flight).Where(f => f.Status == "SCHEDULED" && f.DepartsAt > now && f.SeatsAvailable > 0).ToArray();
        bool hasReturnPair = future.Any(outbound => future.Any(returning =>
            returning.FlightId != outbound.FlightId && returning.DepartureAirportId == outbound.ArrivalAirportId &&
            returning.ArrivalAirportId == outbound.DepartureAirportId && returning.DepartsAt > outbound.ArrivesAt));
        Console.WriteLine(hasReturnPair ? "READ-ONLY DATA: future reverse-route pair exists." :
            "SKIP: no future reverse-route pair in existing data; do not create flights on the main database for round-trip testing.");
        Console.WriteLine($"{checks} Management/Booking integration read checks passed. No SQL writes.");
    }
}
