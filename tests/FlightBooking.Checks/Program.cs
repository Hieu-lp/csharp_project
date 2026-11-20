using System.Collections;
using FlightBooking.BUS;
using FlightBooking.DTO;

var now = new DateTime(2026, 11, 20, 12, 0, 0);
var checks = 0;
void Check(bool value, string label)
{
    if (!value) throw new Exception("FAIL: " + label);
    checks++;
    Console.WriteLine("PASS: " + label);
}
void Reject(Action action, string label)
{
    try { action(); } catch (BookingException) { Check(true, label); return; }
    throw new Exception("FAIL: " + label);
}
var discount = new DiscountCodeDTO(1, "TEST", "PERCENT", 10, 500000, 200000,
    now.AddDays(-1), now.AddDays(1), 10, 0, "ACTIVE");
Check(BookingBUS.CalculateDiscount(discount, 3000000, now) == 200000, "percent discount capped");
Check(BookingBUS.CalculateDiscount(discount with { Type = "FIXED", Value = 900000, MaxDiscountAmount = null }, 500000, now) == 500000, "fixed discount cannot exceed subtotal");
Check(BookingBUS.CalculateDiscount(discount with { MinOrderAmount = 0, Value = 1, MaxDiscountAmount = null }, 100.50m, now) == 1.01m, "decimal discount rounding");
Reject(() => BookingBUS.CalculateDiscount(discount, 400000, now), "minimum spend");
Reject(() => BookingBUS.CalculateDiscount(discount with { Status = "INACTIVE" }, 1000000, now), "inactive code");
Reject(() => BookingBUS.CalculateDiscount(discount with { StartDate = now.AddDays(1) }, 1000000, now), "future code");
Reject(() => BookingBUS.CalculateDiscount(discount with { EndDate = now.AddSeconds(-1) }, 1000000, now), "expired code");
Reject(() => BookingBUS.CalculateDiscount(discount with { UsedCount = 10 }, 1000000, now), "exhausted code");
var seats = BookingBUS.AllocateSeats(6, new(StringComparer.OrdinalIgnoreCase) { "1A", "1C" }, 4);
Check(seats.SequenceEqual(new[] { "1B", "1D", "1E", "1F" }), "allocate distinct free seats");
Reject(() => BookingBUS.AllocateSeats(2, new() { "1A", "1B" }, 1), "reject full aircraft");
var flight = new FlightDTO(1, 1, 2, "A", "B", "A", "B", new(2026, 12, 15), new(8, 30), new(2026, 12, 15), new(10, 45), 10, 10, "SCHEDULED");
var offers = new ArrayList
{
    new FlightOfferDTO(flight, new(1, 1, "Economy", 1000000, 7)),
    new FlightOfferDTO(flight, new(2, 1, "Business", 2000000, 20))
};
Check(BookingBUS.Filter(offers, new()).Count == 2, "empty advanced filter keeps all offers");
Check(BookingBUS.Filter(offers, new() { Price = 1500000, Operator = "<", ClassName = "Eco", Join = "AND" }).Count == 1, "ArrayList AND");
Check(BookingBUS.Filter(offers, new() { Price = 1500000, Operator = "<", ClassName = "Business", Join = "OR" }).Count == 2, "ArrayList OR");
Check(BookingBUS.Filter(offers, new() { ClassName = "Eco", Not = true }).Cast<FlightOfferDTO>().Single().Fare.FareId == 2, "ArrayList NOT");
foreach (var (op, count) in new[] { ("=",1), ("<>",1), (">",1), (">=",2), ("<",0), ("<=",1) })
    Check(BookingBUS.Filter(offers, new() { Price = 1000000, Operator = op }).Count == count, "comparison " + op);
Reject(() => BookingBUS.Filter(offers, new() { Operator = "DROP" }), "reject unknown comparison");
BookingRequest Valid() => new()
{
    FlightId = 1, FareId = 1, ExpectedTotal = 1000000,
    Customer = new() { FullName = "Khách thử", Email = "test@example.test", Phone = "0900000000", DateOfBirth = new(2000,1,1), Gender = "Khác" },
    Passengers = [new() { FullName = "Hành khách thử", IdCard = "TEST-001" }]
};
BookingBUS.Validate(Valid(), DateOnly.FromDateTime(now)); Check(true, "valid request");
var bad = Valid(); bad.Passengers.Add(new() { FullName = "Other", IdCard = "test-001" });
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "duplicate passenger identity");
bad = Valid(); bad.Customer.DateOfBirth = new(2027,1,1);
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "future birth date");
bad = Valid(); bad.Customer.FullName = null!;
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "missing name");
bad = Valid(); bad.Customer.Email = "invalid";
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "invalid email");
var booking = new BookingDTO(1,1,1,null,now,"WEB",1,0,1000000,"CONFIRMED","Test","test@example.test","0900000000","UNPAID",flight,[]);
BookingBUS.CheckCancellation(booking, now); Check(true, "allow unpaid before departure");
Reject(() => BookingBUS.CheckCancellation(booking with { InvoiceStatus = "PAID" }, now), "paid cancellation needs staff");
Reject(() => BookingBUS.CheckCancellation(booking, flight.DepartsAt), "reject cancellation at departure");
Reject(() => new BookingBUS("").Get(1, "not-a-key"), "reject invalid access key before SQL");
if (args.Contains("--sql")) await SqlChecks.Run(Check);
else Console.WriteLine("SKIP: SQL integration checks (pass --sql and FLIGHTBOOKING_TEST_CONNECTION for a dedicated *_BookingTests database).");
Console.WriteLine($"{checks} checks passed.");
