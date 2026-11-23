param([string]$BaseUrl = 'http://127.0.0.1:5087')
$ErrorActionPreference = 'Stop'
$script:checks = 0
$script:webSession = New-Object Microsoft.PowerShell.Commands.WebRequestSession
function Assert($value, $label) {
    if (-not $value) { throw "FAIL: $label" }
    $script:checks++; "PASS: $label"
}
function GetPage([string]$path) {
    [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -UseBasicParsing ($BaseUrl + $path) -WebSession $script:webSession).Content)
}
function Fields([string]$html) {
    $data = @{}
    foreach ($inputTag in [regex]::Matches($html, '<input\b[^>]*>')) {
        $name = [regex]::Match($inputTag.Value, 'name="([^"]+)"').Groups[1].Value
        $value = [regex]::Match($inputTag.Value, 'value="([^"]*)"').Groups[1].Value
        if ($name) { $data[$name] = [Net.WebUtility]::HtmlDecode($value) }
    }
    return $data
}
function Preview($data) {
    # Chỉ cho phép xem giá; tuyệt đối không gửi book/cancel, kể cả khi dữ liệu hợp lệ.
    if ($data.ContainsKey('action') -and $data.action -ne 'quote') { throw 'Only quote previews are allowed.' }
    [Net.WebUtility]::HtmlDecode((Invoke-WebRequest -UseBasicParsing ($BaseUrl + '/Booking/Checkout') -Method Post -Body $data -WebSession $script:webSession).Content)
}
function Total([string]$html) {
    [decimal]::Parse((Fields $html)['Input.ExpectedTotal'], [cultureinfo]::GetCultureInfo('vi-VN'))
}

$homePageHtml = GetPage '/'
Assert ($homePageHtml.Contains('id="flight-search"')) 'home renders flight search'
# Lấy ID từ dữ liệu server thực tế, không dùng ID seed hoặc bản bàn giao cũ.
$offer = [regex]::Match($homePageHtml, 'name="SelectedDeal"\s+value="([0-9]+):([0-9]+)"')
Assert $offer.Success 'existing available deal can be selected'
$flightId = $offer.Groups[1].Value
$fareId = $offer.Groups[2].Value
$selected = GetPage "/?DealFlightId=$flightId&DealFareId=$fareId&Search.Passengers=2"
Assert ($selected.Contains('id="selected-title"') -and $selected.Contains('Tổng 2 người')) 'selected deal uses current quantity'
$checkoutPath = "/Booking/Checkout?flightId=$flightId&fareId=$fareId&quantity=2"
$checkout = GetPage $checkoutPath
Assert (([regex]::Matches($checkout, 'class="passenger-card"')).Count -eq 2) 'two independent passenger forms'
Assert (([regex]::Matches($checkout, 'name="Input.Customer.FullName"')).Count -eq 1) 'booker is separate from passengers'
Assert ((Fields $checkout)['Input.Quantity'] -eq '2') 'checkout quantity matches selection'
$expected = Total $checkout
Assert ($expected -gt 0) 'server quote has positive total for existing offer'
$data = Fields $checkout
$data['Input.Customer.FullName'] = 'Read Only Booker'
$data['Input.Customer.Email'] = 'readonly@example.test'
$data['Input.Customer.Phone'] = '0900000000'
$data['Input.Customer.DateOfBirth'] = '2000-01-01'
$data['Input.Customer.Gender'] = 'Nam'
$data['Input.Customer.IdPassportNumber'] = 'TEST-READ-BOOKER'
foreach ($i in 0..1) {
    $data["Input.Passengers[$i].FullName"] = "Read Only Passenger $i"
    $data["Input.Passengers[$i].IdCard"] = "TEST-READ-PAX-$i"
}
$data.action = 'quote'
$data['Input.ExpectedTotal'] = '1'
$preview = Preview $data
Assert ((Total $preview) -eq $expected) 'preview recomputes client-modified total'
Assert ($preview.Contains('Read Only Booker') -and $preview.Contains('Read Only Passenger 1')) 'preview retains independent identities'
$data = Fields $preview
$data.Remove('action')
$preview = Preview $data
Assert ($preview.Contains('id="checkout-form"') -and (Total $preview) -eq $expected) 'missing action defaults to price preview'
$data = Fields $preview
$data.action = 'quote'
$data['Input.DiscountCode'] = 'READONLY-NOT-A-CODE'
$preview = Preview $data
Assert ($preview.Contains('Mã giảm giá không tồn tại')) 'invalid discount has readable error'
Assert ($preview.Contains('Read Only Passenger 1') -and $preview.Contains('Read Only Booker')) 'invalid discount retains form data'
$data = Fields $preview
$data.action = 'quote'
$data['Input.DiscountCode'] = ''
$preview = Preview $data
Assert ((Total $preview) -eq $expected) 'clearing discount restores base quote'
$data = Fields $preview
$data.action = 'quote'
$data['Input.Quantity'] = '3'
$tampered = Preview $data
Assert ($tampered.Contains('không khớp')) 'tampered quantity is rejected against session draft'
$lookup = GetPage '/Booking/Details/2147483647'
Assert ($lookup.Contains('name="BookingId"') -and $lookup.Contains('name="AccessKey"')) 'details without authorization redirects to lookup'
$invalidQuantity = GetPage "/Booking/Checkout?flightId=$flightId&fareId=$fareId&quantity=10"
Assert ($invalidQuantity.Contains('1 đến 9') -and -not $invalidQuantity.Contains('id="checkout-form"')) 'out-of-range passenger count is rejected'
$statusWithoutToken = 0
try {
    Invoke-WebRequest -UseBasicParsing ($BaseUrl + '/Booking/Checkout') -Method Post -Body @{ action = 'quote' } -WebSession $script:webSession | Out-Null
} catch {
    if ($null -eq $_.Exception.Response) { throw }
    $statusWithoutToken = [int]$_.Exception.Response.StatusCode
}
Assert ($statusWithoutToken -eq 400) 'POST without antiforgery token is rejected'
"$script:checks read-only HTTP checks passed. No booking or cancellation submitted."
