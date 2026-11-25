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
    try { action(); } catch (LoiDatVe) { Check(true, label); return; }
    throw new Exception("FAIL: " + label);
}
var discount = new MaGiamGiaDTO(1, "TEST", "PERCENT", 10, 500000, 200000,
    now.AddDays(-1), now.AddDays(1), 10, 0, "ACTIVE");
Check(DatVeBUS.TinhGiamGia(discount, 3000000, now) == 200000, "percent discount capped");
Check(DatVeBUS.TinhGiamGia(discount with { Loai = "FIXED", GiaTri = 900000, GiamToiDa = null }, 500000, now) == 500000, "fixed discount cannot exceed subtotal");
Check(DatVeBUS.TinhGiamGia(discount with { GiaTriDonToiThieu = 0, GiaTri = 1, GiamToiDa = null }, 100.50m, now) == 1.01m, "decimal discount rounding");
Reject(() => DatVeBUS.TinhGiamGia(discount, 400000, now), "minimum spend");
Reject(() => DatVeBUS.TinhGiamGia(discount with { TrangThai = "INACTIVE" }, 1000000, now), "inactive code");
Reject(() => DatVeBUS.TinhGiamGia(discount with { NgayBatDau = now.AddDays(1) }, 1000000, now), "future code");
Reject(() => DatVeBUS.TinhGiamGia(discount with { NgayKetThuc = now.AddSeconds(-1) }, 1000000, now), "expired code");
Reject(() => DatVeBUS.TinhGiamGia(discount with { SoLanDaDung = 10 }, 1000000, now), "exhausted code");
var seats = DatVeBUS.CapGhe(6, new(StringComparer.OrdinalIgnoreCase) { "1A", "1C" }, 4);
Check(seats.SequenceEqual(new[] { "1B", "1D", "1E", "1F" }), "allocate distinct free seats");
Reject(() => DatVeBUS.CapGhe(2, new() { "1A", "1B" }, 1), "reject full aircraft");
var flight = new ChuyenBayDTO(1, 1, 2, "A", "B", "A", "B", new(2026, 12, 15), new(8, 30), new(2026, 12, 15), new(10, 45), 10, 10, "SCHEDULED");
var offers = new ArrayList
{
    new LuaChonChuyenBayDTO(flight, new(1, 1, "Economy", 1000000, 7)),
    new LuaChonChuyenBayDTO(flight, new(2, 1, "Business", 2000000, 20))
};
Check(DatVeBUS.LocChuyenBay(offers, new()).Count == 2, "empty advanced filter keeps all offers");
Check(DatVeBUS.LocChuyenBay(offers, new() { ArrivalDate = flight.NgayDen }).Count == 2, "arrival date matches");
Check(DatVeBUS.LocChuyenBay(offers, new() { ArrivalDate = flight.NgayDen.AddDays(1), Not = true }).Count == 0, "arrival date remains mandatory with NOT");
var overnight = new ArrayList { new LuaChonChuyenBayDTO(flight with { NgayDen = flight.NgayKhoiHanh.AddDays(1) }, new(3, 1, "Economy", 1000000, 7)) };
Check(DatVeBUS.LocChuyenBay(overnight, new() { DepartureDate = flight.NgayKhoiHanh, ArrivalDate = flight.NgayKhoiHanh.AddDays(1) }).Count == 1, "overnight arrival date");
Reject(() => DatVeBUS.KiemTraNgayTimKiem(new() { DepartureDate = flight.NgayKhoiHanh, ArrivalDate = flight.NgayKhoiHanh.AddDays(-1) }), "arrival before departure rejected");
Check(DatVeBUS.LocChuyenBay(offers, new() { Price = 1500000, Operator = "<", ClassName = "Eco", Join = "AND" }).Count == 1, "ArrayList AND");
Check(DatVeBUS.LocChuyenBay(offers, new() { Price = 1500000, Operator = "<", ClassName = "Business", Join = "OR" }).Count == 2, "ArrayList OR");
Check(DatVeBUS.LocChuyenBay(offers, new() { ClassName = "Eco", Not = true }).Cast<LuaChonChuyenBayDTO>().Single().HangVe.MaHangVe == 2, "ArrayList NOT");
foreach (var (op, count) in new[] { ("=",1), ("<>",1), (">",1), (">=",2), ("<",0), ("<=",1) })
    Check(DatVeBUS.LocChuyenBay(offers, new() { Price = 1000000, Operator = op }).Count == count, "comparison " + op);
Reject(() => DatVeBUS.LocChuyenBay(offers, new() { Operator = "DROP" }), "reject unknown comparison");
YeuCauDatVe Valid() => new()
{
    FlightId = 1, FareId = 1, ExpectedTotal = 1000000,
    Customer = new() { FullName = "Khách thử", Email = "test@example.test", Phone = "0900000000", DateOfBirth = new(2000,1,1), Gender = "Khác", IdPassportNumber = "TEST-BOOKER-001" },
    Passengers = [new() { FullName = "Hành khách thử", IdCard = "TEST-001" }]
};
DatVeBUS.KiemTraYeuCauDatVe(Valid(), DateOnly.FromDateTime(now)); Check(true, "valid request");
var bad = Valid(); bad.Quantity = 2; bad.Passengers.Add(new() { FullName = "Other", IdCard = "test-001" });
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(bad, DateOnly.FromDateTime(now)), "duplicate passenger identity");
bad = Valid(); bad.Customer.DateOfBirth = new(2027,1,1);
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(bad, DateOnly.FromDateTime(now)), "future birth date");
bad = Valid(); bad.Passengers[0].FullName = null!;
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(bad, DateOnly.FromDateTime(now)), "missing name");
bad = Valid(); bad.Customer.Email = "invalid";
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(bad, DateOnly.FromDateTime(now)), "invalid email");
var booking = new DatVeDTO(1,1,1,null,now,"WEB",1,0,1000000,"CONFIRMED","Test","test@example.test","0900000000","UNPAID",flight,[]);
DatVeBUS.KiemTraHuyVe(booking, now); Check(true, "allow unpaid before departure");
Reject(() => DatVeBUS.KiemTraHuyVe(booking with { TrangThaiHoaDon = "PAID" }, now), "paid cancellation needs staff");
Reject(() => DatVeBUS.KiemTraHuyVe(booking, flight.ThoiDiemKhoiHanh), "reject cancellation at departure");
Reject(() => new DatVeBUS("").TraCuuDatVe(1, "not-a-key"), "reject invalid access key before SQL");
for (var count = 1; count <= 3; count++)
{
    var request = Valid();
    DatVeBUS.DieuChinhSoHanhKhach(request, count);
    for (var i = 0; i < count; i++) { request.Passengers[i].FullName = "Người " + i; request.Passengers[i].IdCard = "ID-" + i; }
    DatVeBUS.KiemTraYeuCauDatVe(request, DateOnly.FromDateTime(now));
    Check(request.Passengers.Count == count && request.Passengers.Distinct().Count() == count, "independent passenger objects " + count);
}
var resize = Valid(); var kept = resize.Passengers[0];
DatVeBUS.DieuChinhSoHanhKhach(resize, 3); resize.Passengers[1].FullName = "Second";
DatVeBUS.DieuChinhSoHanhKhach(resize, 2);
Check(ReferenceEquals(kept, resize.Passengers[0]) && resize.Passengers[1].FullName == "Second", "resize keeps remaining passenger data");
var mismatch = Valid(); mismatch.Quantity = 3;
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(mismatch, DateOnly.FromDateTime(now)), "reject quantity mismatch");
var single = Valid(); single.ReturnDate = new(2026,1,1); single.ReturnFlightId = 999; single.ReturnFareId = 999;
DatVeBUS.ChuanHoaHanhTrinh(single);
Check(single.ReturnDate is null && single.ReturnFlightId is null && single.ReturnFareId is null, "one-way ignores return values");
Reject(() => DatVeBUS.KiemTraNgayTimKiem(new() { TripType = "RoundTrip", DepartureDate = new(2026,12,15) }), "round-trip requires return date");
Reject(() => DatVeBUS.KiemTraNgayTimKiem(new() { TripType = "RoundTrip", DepartureDate = new(2026,12,15), ReturnDate = new(2026,12,14) }), "return date before departure");
var returnFlight = flight with { MaChuyenBay = 2, MaSanBayDi = 2, MaSanBayDen = 1, GioKhoiHanh = new(12,0), GioDen = new(14,0) };
DatVeBUS.KiemTraChuyenVe(flight, returnFlight, flight.NgayKhoiHanh); Check(true, "same-day valid return");
Reject(() => DatVeBUS.KiemTraChuyenVe(flight, returnFlight with { GioKhoiHanh = new(10,45) }, flight.NgayKhoiHanh), "return must depart strictly after outbound arrival");
Reject(() => DatVeBUS.KiemTraChuyenVe(flight, returnFlight with { MaSanBayDi = 1 }, flight.NgayKhoiHanh), "return route must reverse");
Reject(() => DatVeBUS.KiemTraChuyenVe(flight, returnFlight, flight.NgayKhoiHanh.AddDays(1)), "selected return must match return date");
var acrossMidnight = flight with { NgayDen = flight.NgayKhoiHanh.AddDays(1), GioDen = new(1,0) };
Reject(() => DatVeBUS.KiemTraChuyenVe(acrossMidnight, returnFlight, flight.NgayKhoiHanh), "outbound overnight prevents earlier return");
var combined = new BaoGiaHanhTrinhDTO(new(flight, new(1,1,"Economy",100,7),2,200,20,180,1),new(returnFlight,new(2,2,"Economy",150,7),2,300,0,300,null));
Check(combined.TongTien == 480, "journey total sums both legs");
var linkedContact = Valid(); linkedContact.Customer.FullName = "Người đặt hộ"; linkedContact.Customer.IdPassportNumber = "OTHER";
DatVeBUS.KiemTraYeuCauDatVe(linkedContact, DateOnly.FromDateTime(now));
Check(linkedContact.Customer.FullName == "Người đặt hộ" && linkedContact.Customer.IdPassportNumber == "OTHER" && linkedContact.Passengers[0].FullName == "Hành khách thử", "booker remains independent from passenger one");
var missingBooker = Valid(); missingBooker.Customer.FullName = "";
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(missingBooker, DateOnly.FromDateTime(now)), "booker name required independently of passenger name");
var missingGender = Valid(); missingGender.Customer.Gender = "";
Reject(() => DatVeBUS.KiemTraYeuCauDatVe(missingGender, DateOnly.FromDateTime(now)), "no invented gender default");
Check(DatVeBUS.DocTienViet("1.250.000") == 1250000 && DatVeBUS.DocTienViet("1250000") == 1250000, "VND grouped and ungrouped input");
Check(DatVeBUS.DocTienViet(" ") is null && DatVeBUS.DocTienViet("0") == 0 && DatVeBUS.DocTienViet("1.000,50") == 1000.50m, "blank unlimited, zero and decimal VND");
foreach (var invalid in new[] { "-1", "1.23", "abc", "1e6", "99999999999999999" })
    Reject(() => DatVeBUS.DocTienViet(invalid), "reject invalid VND " + invalid);
Reject(() => DatVeBUS.KiemTraKhoangGia(new() { MinPrice = -1 }), "negative minimum");
Reject(() => DatVeBUS.KiemTraKhoangGia(new() { MaxPrice = -1 }), "negative maximum");
Reject(() => DatVeBUS.KiemTraKhoangGia(new() { MinPrice = 2, MaxPrice = 1 }), "inverted price range");
Check(DatVeBUS.LocChuyenBay(offers, new() { MinPrice = 1000000, MaxPrice = 1000000 }).Count == 1, "inclusive price bounds");
Check(DatVeBUS.LocChuyenBay(offers, new() { MaxPrice = 1500000, FareClassName = "Business" }).Count == 0, "price AND exact class");
var exactOffers = new ArrayList(offers) { new LuaChonChuyenBayDTO(flight, new(3, 1, "Economy Plus", 1000000, 7)) };
Check(DatVeBUS.LocChuyenBay(exactOffers, new() { FareClassName = "Economy" }).Count == 1, "Economy excludes Economy Plus");
Check(DatVeBUS.LocChuyenBay(exactOffers, new() { MinPrice = 1000000, ClassName = "Eco", Not = true }).Count == 1, "legacy NOT preserved with mandatory range");
var selectionQuote = new BaoGiaDatVeDTO(flight with { SoGheCon = 2 }, new(1,1,"Economy",1000000,7),3,3000000,0,3000000,null);
Check(!new ChuyenBayDaChonDTO(selectionQuote).ConDuGhe && new ChuyenBayDaChonDTO(selectionQuote with { SoLuong = 2 }).ConDuGhe, "selected flight detects insufficient seats and boundary");
foreach (var (birth, rejected) in new[] { (new DateOnly(2008,11,19),false), (new DateOnly(2008,11,20),true), (new DateOnly(2008,11,21),true), (new DateOnly(2027,1,1),true) })
{
    var ageRequest = Valid(); ageRequest.Customer.DateOfBirth = birth;
    if (rejected) Reject(() => DatVeBUS.KiemTraYeuCauDatVe(ageRequest, new(2026,11,20)), "contact birthday boundary " + birth);
    else { DatVeBUS.KiemTraYeuCauDatVe(ageRequest, new(2026,11,20)); Check(true, "contact older than 18 by one day"); }
}
Check(DatVeBUS.LoiNgaySinhKhachDat(new(2008,2,29), new(2026,2,28)) is not null && DatVeBUS.LoiNgaySinhKhachDat(new(2008,2,29), new(2026,3,1)) is null, "leap-day eighteenth birthday must have passed");
foreach (var today in new[] { new DateOnly(2026,3,1), new DateOnly(2024,2,29), new DateOnly(2026,11,20) })
{
    var latest = DatVeBUS.NgaySinhKhachDatToiDa(today);
    Check(DatVeBUS.LoiNgaySinhKhachDat(latest,today) is null && DatVeBUS.LoiNgaySinhKhachDat(latest.AddDays(1),today) is not null, "UI age max agrees with BUS " + today);
}
foreach (var document in new string?[] { null, "", "   ", "DOC@BAD", new string('A',51) })
{
    var paperRequest = Valid(); paperRequest.Customer.IdPassportNumber = document;
    Reject(() => DatVeBUS.KiemTraYeuCauDatVe(paperRequest, DateOnly.FromDateTime(now)), "contact paper required and valid");
}
var trimmedPaper = Valid(); trimmedPaper.Customer.IdPassportNumber = "  TEST-BOOKER-001  ";
DatVeBUS.KiemTraYeuCauDatVe(trimmedPaper, DateOnly.FromDateTime(now));
Check(trimmedPaper.Customer.IdPassportNumber == "TEST-BOOKER-001", "contact paper trims without borrowing passenger identity");
Check(DatVeBUS.ChuanHoaMa("  welcome10 ") == DatVeBUS.ChuanHoaMa("WELCOME10"), "discount identity ignores whitespace and case");
Check(new DatVeBUS("", new ContactTestClock(new DateTimeOffset(2026,11,20,17,5,0,TimeSpan.Zero))).HomNay == new DateOnly(2026,11,21), "contact age uses Vietnam date across UTC midnight boundary");
BookingStyleChecks.Run(Check, Valid, DateOnly.FromDateTime(now));
BookingDataAccessChecks.Run(Check, Valid);
HopDongJsonChecks.Run(Check);
FlightBooking.Testing.SqlTestPolicyChecks.Run(Check);
if (args.Contains("--browse")) BrowseChecks.Run(Check);
if (args.Contains("--connection-settings")) ConnectionSettingsReadChecks.Run(Check);
if (args.Contains("--demo-rollback")) DemoRollbackChecks.Run(Check);
if (args.Contains("--sql")) await SqlChecks.Run(Check);
else Console.WriteLine("SKIP: SQL write checks (require the approved FlightBooking_Hieu_ManagementTests database and mandatory preflight settings).");
Console.WriteLine($"{checks} checks passed.");

internal sealed class ContactTestClock(DateTimeOffset value) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => value;
}
