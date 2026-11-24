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
Check(BookingBUS.Filter(offers, new() { ArrivalDate = flight.ArrivalDate }).Count == 2, "arrival date matches");
Check(BookingBUS.Filter(offers, new() { ArrivalDate = flight.ArrivalDate.AddDays(1), Not = true }).Count == 0, "arrival date remains mandatory with NOT");
var overnight = new ArrayList { new FlightOfferDTO(flight with { ArrivalDate = flight.DepartureDate.AddDays(1) }, new(3, 1, "Economy", 1000000, 7)) };
Check(BookingBUS.Filter(overnight, new() { DepartureDate = flight.DepartureDate, ArrivalDate = flight.DepartureDate.AddDays(1) }).Count == 1, "overnight arrival date");
Reject(() => BookingBUS.ValidateSearchDates(new() { DepartureDate = flight.DepartureDate, ArrivalDate = flight.DepartureDate.AddDays(-1) }), "arrival before departure rejected");
Check(BookingBUS.Filter(offers, new() { Price = 1500000, Operator = "<", ClassName = "Eco", Join = "AND" }).Count == 1, "ArrayList AND");
Check(BookingBUS.Filter(offers, new() { Price = 1500000, Operator = "<", ClassName = "Business", Join = "OR" }).Count == 2, "ArrayList OR");
Check(BookingBUS.Filter(offers, new() { ClassName = "Eco", Not = true }).Cast<FlightOfferDTO>().Single().Fare.FareId == 2, "ArrayList NOT");
foreach (var (op, count) in new[] { ("=",1), ("<>",1), (">",1), (">=",2), ("<",0), ("<=",1) })
    Check(BookingBUS.Filter(offers, new() { Price = 1000000, Operator = op }).Count == count, "comparison " + op);
Reject(() => BookingBUS.Filter(offers, new() { Operator = "DROP" }), "reject unknown comparison");
BookingRequest Valid() => new()
{
    FlightId = 1, FareId = 1, ExpectedTotal = 1000000,
    Customer = new() { FullName = "Khách thử", Email = "test@example.test", Phone = "0900000000", DateOfBirth = new(2000,1,1), Gender = "Khác", IdPassportNumber = "TEST-BOOKER-001" },
    Passengers = [new() { FullName = "Hành khách thử", IdCard = "TEST-001" }]
};
BookingBUS.Validate(Valid(), DateOnly.FromDateTime(now)); Check(true, "valid request");
var bad = Valid(); bad.Quantity = 2; bad.Passengers.Add(new() { FullName = "Other", IdCard = "test-001" });
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "duplicate passenger identity");
bad = Valid(); bad.Customer.DateOfBirth = new(2027,1,1);
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "future birth date");
bad = Valid(); bad.Passengers[0].FullName = null!;
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "missing name");
bad = Valid(); bad.Customer.Email = "invalid";
Reject(() => BookingBUS.Validate(bad, DateOnly.FromDateTime(now)), "invalid email");
var booking = new BookingDTO(1,1,1,null,now,"WEB",1,0,1000000,"CONFIRMED","Test","test@example.test","0900000000","UNPAID",flight,[]);
BookingBUS.CheckCancellation(booking, now); Check(true, "allow unpaid before departure");
Reject(() => BookingBUS.CheckCancellation(booking with { InvoiceStatus = "PAID" }, now), "paid cancellation needs staff");
Reject(() => BookingBUS.CheckCancellation(booking, flight.DepartsAt), "reject cancellation at departure");
Reject(() => new BookingBUS("").Get(1, "not-a-key"), "reject invalid access key before SQL");
for (var count = 1; count <= 3; count++)
{
    var request = Valid();
    BookingBUS.ResizePassengers(request, count);
    for (var i = 0; i < count; i++) { request.Passengers[i].FullName = "Người " + i; request.Passengers[i].IdCard = "ID-" + i; }
    BookingBUS.Validate(request, DateOnly.FromDateTime(now));
    Check(request.Passengers.Count == count && request.Passengers.Distinct().Count() == count, "independent passenger objects " + count);
}
var resize = Valid(); var kept = resize.Passengers[0];
BookingBUS.ResizePassengers(resize, 3); resize.Passengers[1].FullName = "Second";
BookingBUS.ResizePassengers(resize, 2);
Check(ReferenceEquals(kept, resize.Passengers[0]) && resize.Passengers[1].FullName == "Second", "resize keeps remaining passenger data");
var mismatch = Valid(); mismatch.Quantity = 3;
Reject(() => BookingBUS.Validate(mismatch, DateOnly.FromDateTime(now)), "reject quantity mismatch");
var single = Valid(); single.ReturnDate = new(2026,1,1); single.ReturnFlightId = 999; single.ReturnFareId = 999;
BookingBUS.NormalizeJourney(single);
Check(single.ReturnDate is null && single.ReturnFlightId is null && single.ReturnFareId is null, "one-way ignores return values");
Reject(() => BookingBUS.ValidateSearchDates(new() { TripType = "RoundTrip", DepartureDate = new(2026,12,15) }), "round-trip requires return date");
Reject(() => BookingBUS.ValidateSearchDates(new() { TripType = "RoundTrip", DepartureDate = new(2026,12,15), ReturnDate = new(2026,12,14) }), "return date before departure");
var returnFlight = flight with { FlightId = 2, DepartureAirportId = 2, ArrivalAirportId = 1, DepartureTime = new(12,0), ArrivalTime = new(14,0) };
BookingBUS.ValidateReturn(flight, returnFlight, flight.DepartureDate); Check(true, "same-day valid return");
Reject(() => BookingBUS.ValidateReturn(flight, returnFlight with { DepartureTime = new(10,45) }, flight.DepartureDate), "return must depart strictly after outbound arrival");
Reject(() => BookingBUS.ValidateReturn(flight, returnFlight with { DepartureAirportId = 1 }, flight.DepartureDate), "return route must reverse");
Reject(() => BookingBUS.ValidateReturn(flight, returnFlight, flight.DepartureDate.AddDays(1)), "selected return must match return date");
var acrossMidnight = flight with { ArrivalDate = flight.DepartureDate.AddDays(1), ArrivalTime = new(1,0) };
Reject(() => BookingBUS.ValidateReturn(acrossMidnight, returnFlight, flight.DepartureDate), "outbound overnight prevents earlier return");
var combined = new JourneyQuoteDTO(new(flight, new(1,1,"Economy",100,7),2,200,20,180,1),new(returnFlight,new(2,2,"Economy",150,7),2,300,0,300,null));
Check(combined.TotalAmount == 480, "journey total sums both legs");
var linkedContact = Valid(); linkedContact.Customer.FullName = "Người đặt hộ"; linkedContact.Customer.IdPassportNumber = "OTHER";
BookingBUS.Validate(linkedContact, DateOnly.FromDateTime(now));
Check(linkedContact.Customer.FullName == "Người đặt hộ" && linkedContact.Customer.IdPassportNumber == "OTHER" && linkedContact.Passengers[0].FullName == "Hành khách thử", "booker remains independent from passenger one");
var missingBooker = Valid(); missingBooker.Customer.FullName = "";
Reject(() => BookingBUS.Validate(missingBooker, DateOnly.FromDateTime(now)), "booker name required independently of passenger name");
var missingGender = Valid(); missingGender.Customer.Gender = "";
Reject(() => BookingBUS.Validate(missingGender, DateOnly.FromDateTime(now)), "no invented gender default");
Check(BookingBUS.ParseVnd("1.250.000") == 1250000 && BookingBUS.ParseVnd("1250000") == 1250000, "VND grouped and ungrouped input");
Check(BookingBUS.ParseVnd(" ") is null && BookingBUS.ParseVnd("0") == 0 && BookingBUS.ParseVnd("1.000,50") == 1000.50m, "blank unlimited, zero and decimal VND");
foreach (var invalid in new[] { "-1", "1.23", "abc", "1e6", "99999999999999999" })
    Reject(() => BookingBUS.ParseVnd(invalid), "reject invalid VND " + invalid);
Reject(() => BookingBUS.ValidatePriceRange(new() { MinPrice = -1 }), "negative minimum");
Reject(() => BookingBUS.ValidatePriceRange(new() { MaxPrice = -1 }), "negative maximum");
Reject(() => BookingBUS.ValidatePriceRange(new() { MinPrice = 2, MaxPrice = 1 }), "inverted price range");
Check(BookingBUS.Filter(offers, new() { MinPrice = 1000000, MaxPrice = 1000000 }).Count == 1, "inclusive price bounds");
Check(BookingBUS.Filter(offers, new() { MaxPrice = 1500000, FareClassName = "Business" }).Count == 0, "price AND exact class");
var exactOffers = new ArrayList(offers) { new FlightOfferDTO(flight, new(3, 1, "Economy Plus", 1000000, 7)) };
Check(BookingBUS.Filter(exactOffers, new() { FareClassName = "Economy" }).Count == 1, "Economy excludes Economy Plus");
Check(BookingBUS.Filter(exactOffers, new() { MinPrice = 1000000, ClassName = "Eco", Not = true }).Count == 1, "legacy NOT preserved with mandatory range");
var selectionQuote = new BookingQuoteDTO(flight with { SeatsAvailable = 2 }, new(1,1,"Economy",1000000,7),3,3000000,0,3000000,null);
Check(!new FlightSelectionDTO(selectionQuote).HasSeats && new FlightSelectionDTO(selectionQuote with { Quantity = 2 }).HasSeats, "selected flight detects insufficient seats and boundary");
foreach (var (birth, rejected) in new[] { (new DateOnly(2008,11,19),false), (new DateOnly(2008,11,20),true), (new DateOnly(2008,11,21),true), (new DateOnly(2027,1,1),true) })
{
    var ageRequest = Valid(); ageRequest.Customer.DateOfBirth = birth;
    if (rejected) Reject(() => BookingBUS.Validate(ageRequest, new(2026,11,20)), "contact birthday boundary " + birth);
    else { BookingBUS.Validate(ageRequest, new(2026,11,20)); Check(true, "contact older than 18 by one day"); }
}
Check(BookingBUS.ContactBirthDateError(new(2008,2,29), new(2026,2,28)) is not null && BookingBUS.ContactBirthDateError(new(2008,2,29), new(2026,3,1)) is null, "leap-day eighteenth birthday must have passed");
foreach (var today in new[] { new DateOnly(2026,3,1), new DateOnly(2024,2,29), new DateOnly(2026,11,20) })
{
    var latest = BookingBUS.LatestContactBirthDate(today);
    Check(BookingBUS.ContactBirthDateError(latest,today) is null && BookingBUS.ContactBirthDateError(latest.AddDays(1),today) is not null, "UI age max agrees with BUS " + today);
}
foreach (var document in new string?[] { null, "", "   ", "DOC@BAD", new string('A',51) })
{
    var paperRequest = Valid(); paperRequest.Customer.IdPassportNumber = document;
    Reject(() => BookingBUS.Validate(paperRequest, DateOnly.FromDateTime(now)), "contact paper required and valid");
}
var trimmedPaper = Valid(); trimmedPaper.Customer.IdPassportNumber = "  TEST-BOOKER-001  ";
BookingBUS.Validate(trimmedPaper, DateOnly.FromDateTime(now));
Check(trimmedPaper.Customer.IdPassportNumber == "TEST-BOOKER-001", "contact paper trims without borrowing passenger identity");
Check(BookingBUS.NormalizeCode("  welcome10 ") == BookingBUS.NormalizeCode("WELCOME10"), "discount identity ignores whitespace and case");
Check(new BookingBUS("", new ContactTestClock(new DateTimeOffset(2026,11,20,17,5,0,TimeSpan.Zero))).Today == new DateOnly(2026,11,21), "contact age uses Vietnam date across UTC midnight boundary");
BookingStyleChecks.Run(Check, Valid, DateOnly.FromDateTime(now));
BookingDataAccessChecks.Run(Check, Valid);
if (args.Contains("--browse")) BrowseChecks.Run(Check);
if (args.Contains("--demo-rollback")) DemoRollbackChecks.Run(Check);
if (args.Contains("--sql")) await SqlChecks.Run(Check);
else Console.WriteLine("SKIP: SQL integration checks (pass --sql and FLIGHTBOOKING_TEST_CONNECTION for a dedicated *_BookingTests database).");
Console.WriteLine($"{checks} checks passed.");

internal sealed class ContactTestClock(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
