using FlightBooking.BUS;
using FlightBooking.DAO;

internal static class BrowseChecks
{
    // Read-only checks against the same local database as the website. No seed or schema changes.
    public static void Run(Action<bool, string> check)
    {
        var cs = Environment.GetEnvironmentVariable("FLIGHTBOOKING_BROWSE_CONNECTION")
            ?? throw new InvalidOperationException("Set FLIGHTBOOKING_BROWSE_CONNECTION explicitly.");
        var bus = new BookingBUS(cs);
        var dao = new BookingDAO(new MyConnection(cs));
        var now = TimeZoneInfo.ConvertTimeBySystemTimeZoneId(DateTimeOffset.UtcNow, "SE Asia Standard Time").DateTime;
        var deals = bus.GetCheapestFlights();
        check(deals.Count <= 4 && deals.Select(x => x.Flight.FlightId).Distinct().Count() == deals.Count, "SQL: at most four distinct flights");
        check(deals.All(x => x.Flight.DepartsAt > now && x.Flight.Status == "SCHEDULED" && x.Flight.SeatsAvailable > 0), "SQL: future Vietnam time, open and available");
        check(deals.SequenceEqual(deals.OrderBy(x => x.Fare.Price).ThenBy(x => x.Flight.DepartsAt).ThenBy(x => x.Flight.FlightId).ThenBy(x => x.Fare.FareId)), "SQL: stable price/departure/id order");
        foreach (var deal in deals)
        {
            var cheapest = dao.Read(d => d.Fares(deal.Flight.FlightId).OrderBy(f => f.Price).ThenBy(f => f.FareId).First());
            check(deal.Fare == cheapest, "SQL: cheapest fare for flight " + deal.Flight.FlightId);
            foreach (var quantity in new[] { 1, 2, 3 })
            {
                var selected = bus.SelectFlight(deal.Flight.FlightId, deal.Fare.FareId, quantity);
                check(selected.Quote.Flight == deal.Flight && selected.Quote.Fare == deal.Fare && selected.Quote.TotalAmount == deal.Fare.Price * quantity && selected.HasSeats == (deal.Flight.SeatsAvailable >= quantity), "SQL: selection preserves flight/fare and recalculates " + quantity);
            }
            Console.WriteLine($"DEAL: {deal.Flight.FlightId}:{deal.Fare.FareId} {deal.Flight.DepartureCity} -> {deal.Flight.ArrivalCity} {deal.Flight.DepartsAt:yyyy-MM-dd HH:mm} {deal.Fare.Price}");
        }
        if (deals.Count > 1)
        {
            try { bus.SelectFlight(deals[0].Flight.FlightId, deals[1].Fare.FareId, 1); throw new Exception("Expected mismatched fare rejection"); }
            catch (FlightBooking.DTO.BookingException) { check(true, "SQL: reject fare belonging to another flight"); }
        }
        check(bus.GetFareClassNames().Count > 0, "SQL: fare dropdown reads database");
    }
}
