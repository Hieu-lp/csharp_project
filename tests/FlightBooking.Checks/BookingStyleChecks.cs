using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

internal static class BookingStyleChecks
{
    public static void Run(Action<bool, string> check, Func<BookingRequest> valid, DateOnly today)
    {
        var flight = new FlightDTO(1, 1, 2, "A", "B", "Airport A", "Airport B",
            new(2026, 12, 15), new(8, 0), new(2026, 12, 15), new(10, 0), 10, 10, "SCHEDULED");
        var offers = new ArrayList
        {
            new FlightOfferDTO(flight, new(1, 1, "Economy", 100, 7)),
            new FlightOfferDTO(flight, new(2, 1, "Business", 200, 20)),
            new FlightOfferDTO(flight, new(3, 1, "Economy Plus", 300, 10))
        };
        FlightSearch Condition(string join, bool not = false) => new()
        {
            Price = 200, Operator = ">=", ClassName = "eco", Join = join, Not = not
        };
        check(Ids(BookingBUS.Filter(offers, Condition("AND"))) == "3", "combined price/class AND truth table");
        check(Ids(BookingBUS.Filter(offers, Condition("OR"))) == "1,2,3", "combined price/class OR truth table");
        check(Ids(BookingBUS.Filter(offers, Condition("AND", true))) == "1,2", "NOT negates the complete AND group");
        check(BookingBUS.Filter(offers, Condition("OR", true)).Count == 0, "NOT negates the complete OR group");
        check(Ids(BookingBUS.Filter(offers, new() { MaxPrice = 200, Price = 100, Operator = ">=", ClassName = "eco", Not = true })) == "2",
            "NOT cannot bring back offers excluded by mandatory price limits");
        check(BookingBUS.Filter(offers, new() { Join = "OR", Not = true }).Count == 3,
            "empty advanced conditions keep all offers even with OR/NOT");

        var start = new DateTime(2026, 12, 1);
        var end = start.AddDays(1);
        var discount = new DiscountCodeDTO(1, "BOUNDARY", "PERCENT", 10, 100, null, start, end, null, 0, "ACTIVE");
        check(BookingBUS.CalculateDiscount(discount, 100, start) == 10, "discount includes exact start and minimum subtotal");
        check(BookingBUS.CalculateDiscount(discount, 100, end) == 10, "discount includes exact end");

        BookingRequest missingPassenger = valid();
        missingPassenger.ReturnFlightId = 99;
        missingPassenger.Passengers.Add(null!);
        check(Message(() => BookingBUS.Validate(missingPassenger, today)) == "Thiếu thông tin khách đặt hoặc hành khách." &&
            missingPassenger.ReturnFlightId == 99, "null passenger is rejected before journey normalization or quantity errors");

        BookingRequest duplicate = valid();
        duplicate.Quantity = 2;
        duplicate.Passengers = [new() { FullName = "Passenger A", IdCard = " doc-1 " }, new() { FullName = "Passenger B", IdCard = "DOC-1" }];
        check(Message(() => BookingBUS.Validate(duplicate, today)) == "Giấy tờ các hành khách trong cùng đơn không được trùng." &&
            duplicate.Passengers[0].IdCard == "DOC-1", "duplicate documents still compare after trimming and normalization");

        BookingRequest invalid = valid();
        invalid.Customer.FullName = "";
        invalid.ExpectedTotal = -1;
        check(Message(() => BookingBUS.Validate(invalid, today)) == "Nhập họ tên khách đặt.",
            "data annotation error order stays ahead of expected-total validation");
    }

    private static string Ids(ArrayList offers)
    {
        return string.Join(",", offers.Cast<FlightOfferDTO>().Select(offer => offer.Fare.FareId));
    }

    private static string Message(Action action)
    {
        try { action(); }
        catch (BookingException error) { return error.Message; }
        throw new Exception("FAIL: expected a BookingException.");
    }
}
