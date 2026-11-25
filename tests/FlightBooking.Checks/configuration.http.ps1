param([string]$BaseUrl = 'http://127.0.0.1:5088')
$ErrorActionPreference = 'Stop'
$script:checks = 0
$session = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Assert($value, $label) {
    if (-not $value) { throw "FAIL: $label" }
    $script:checks++; "PASS: $label"
}
function GetPage([string]$path) {
    [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -UseBasicParsing ($BaseUrl + $path) -WebSession $session).Content)
}
# Website phải được cấu hình chuỗi kết nối chỉ có khoảng trắng: DAO từ chối trước kết nối.
# Chỉ GET, POST Lookup dữ liệu sai và POST thiếu antiforgery; không book/cancel.
$message = 'Chưa kết nối được database. Kiểm tra cấu hình FlightBookingDB.'
Assert ((GetPage '/').Contains($message)) 'Index displays the translated configuration error'
$checkout = GetPage '/Booking/Checkout?flightId=1&fareId=1&quantity=2'
$bookingDisabled = [regex]::IsMatch($checkout, '<button\b[^>]*name="action"[^>]*value="book"[^>]*disabled')
Assert ($checkout.Contains($message) -and $bookingDisabled -and -not $checkout.Contains('class="leg-summary"')) 'Checkout retains the form but disables booking without a server quote'
$lookup = GetPage '/Booking/Lookup'
Assert ($lookup.Contains('name="BookingId"') -and $lookup.Contains('name="AccessKey"')) 'Lookup GET renders independently of database'
$token = [regex]::Match($lookup, 'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
if (-not $token) { throw 'Lookup antiforgery token missing.' }
$invalid = [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -UseBasicParsing ($BaseUrl + '/Booking/Lookup') -Method Post -WebSession $session -Body @{
    __RequestVerificationToken = $token; BookingId = '0'; AccessKey = ''
}).Content)
Assert ($invalid.Contains('Mã đơn phải là số nguyên dương') -and $invalid.Contains('Nhập khóa tra cứu')) 'Lookup preserves ModelState errors before calling BUS'
Assert ((GetPage '/Booking/Details/2147483647').Contains('name="AccessKey"')) 'Details without session authorization redirects to Lookup'
$status = 0
try {
    Invoke-WebRequest -UseBasicParsing ($BaseUrl + '/Booking/Checkout') -Method Post -Body @{ action = 'quote' } -WebSession $session | Out-Null
} catch {
    if ($null -eq $_.Exception.Response) { throw }
    $status = [int]$_.Exception.Response.StatusCode
}
Assert ($status -eq 400) 'Checkout retains antiforgery rejection'
"$script:checks configuration HTTP checks passed. No SQL connection or booking/cancellation submitted."
