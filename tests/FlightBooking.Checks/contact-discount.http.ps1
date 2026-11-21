param([string]$BaseUrl='http://127.0.0.1:5086')
$ErrorActionPreference='Stop'
function Assert($value,$label){if(-not $value){throw "FAIL: $label"};"PASS: $label"}
function Fields($html){
 $data=@{}
 foreach($match in [regex]::Matches($html,'<input\b[^>]*>')){
  $name=[regex]::Match($match.Value,'name="([^"]+)"').Groups[1].Value
  $value=[regex]::Match($match.Value,'value="([^"]*)"').Groups[1].Value
  if($name){$data[$name]=[Net.WebUtility]::HtmlDecode($value)}
 }
 foreach($select in [regex]::Matches($html,'<select\b[^>]*name="([^"]+)"[^>]*>([\s\S]*?)</select>')){
  $selected=[regex]::Match($select.Groups[2].Value,'<option[^>]*value="([^"]*)"[^>]*selected[^>]*>')
  if($selected.Success){$data[$select.Groups[1].Value]=[Net.WebUtility]::HtmlDecode($selected.Groups[1].Value)}
 }
 return $data
}
function Post($fields){[Net.WebUtility]::HtmlDecode((Invoke-WebRequest "$BaseUrl/Booking/Checkout" -Method Post -Body $fields -WebSession $script:webSession).Content)}
function Total($html){[decimal]::Parse((Fields $html)['Input.ExpectedTotal'],[cultureinfo]::GetCultureInfo('vi-VN'))}
$script:webSession=New-Object Microsoft.PowerShell.Commands.WebRequestSession
# Existing seed 09 fixtures, read and preview only; never submit valid action=book.
$page=[Net.WebUtility]::HtmlDecode((Invoke-WebRequest "$BaseUrl/Booking/Checkout?flightId=42&fareId=82&quantity=2&tripType=RoundTrip&returnDate=2026-11-27&returnFlightId=43&returnFareId=84" -WebSession $script:webSession).Content)
$data=Fields $page
$baseTotal=Total $page
$data['Input.Customer.FullName']='Contact Test';$data['Input.Customer.Email']='contact.test@example.test';$data['Input.Customer.Phone']='0900000000';$data['Input.Customer.Gender']='Nam';$data['Input.Customer.DateOfBirth']='2000-01-01';$data['Input.Customer.IdPassportNumber']='  TEST-BOOKER-001  '
foreach($i in @(0,1)){$data["Input.Passengers[$i].FullName"]="Passenger $i";$data["Input.Passengers[$i].IdCard"]="TEST-PAX-$i"}
$data.action='quote';$data['Input.DiscountCode']=' expand5 '
$page=Post $data
Assert ($page.Contains('Đã áp dụng mã giảm giá.') -and (Total $page) -lt $baseTotal) 'first valid code discounts server quote'
$discounted=Total $page
$data=Fields $page;$data.action='quote';$data['Input.DiscountCode']='EXPAND5';$data['Input.ExpectedTotal']='1';$page=Post $data
Assert ($page.Contains('Mã giảm giá này đã được áp dụng') -and (Total $page) -eq $discounted) 'same normalized code reports already applied without stacking'
Assert ($page.Contains('Passenger 0') -and $page.Contains('Passenger 1') -and $page.Contains('Contact Test') -and $page.Contains('name="Input.ReturnFlightId" value="43"')) 'contact, passengers and both legs survive apply'
$data=Fields $page;$data.Remove('action');$page=Post $data
Assert ($page.Contains('id="checkout-form"') -and $page.Contains('Mã giảm giá này đã được áp dụng')) 'missing action defaults to safe preview'
$data=Fields $page;$data.action='quote';$data['Input.DiscountCode']='SAVE100';$page=Post $data
Assert ($page.Contains('Đã áp dụng mã giảm giá.') -and -not $page.Contains('Mã giảm giá này đã được áp dụng')) 'different code gets fresh application state'
$data=Fields $page;$data.action='quote';$data['Input.DiscountCode']='DOES_NOT_EXIST';$page=Post $data
Assert ($page.Contains('Mã giảm giá không tồn tại') -and $page.Contains('Passenger 1') -and $page.Contains('#42') -and $page.Contains('#43')) 'invalid code retains inputs and itinerary with undiscounted fallback'
$data=Fields $page;$data.action='quote';$data['Input.DiscountCode']='';$page=Post $data
Assert ((Total $page) -eq $baseTotal -and -not $page.Contains('Mã giảm giá này đã được áp dụng')) 'clearing code restores undiscounted total'
$vietnamToday=[TimeZoneInfo]::ConvertTimeBySystemTimeZoneId([DateTimeOffset]::UtcNow,'SE Asia Standard Time').Date
$data=Fields $page;$data.action='book';$data['Input.Customer.DateOfBirth']=$vietnamToday.AddYears(-18).ToString('yyyy-MM-dd');$data['Input.Customer.IdPassportNumber']='   ';$page=Post $data
Assert ($page -match 'data-valmsg-for="Input.Customer.DateOfBirth"[^>]*>[^<]*sinh nhật lần thứ 18' -and $page -match 'data-valmsg-for="Input.Customer.IdPassportNumber"[^>]*>[^<]*Nhập CCCD') 'exact eighteenth birthday and missing contact paper show inline errors'
Assert ($page.Contains('Passenger 0') -and $page.Contains('Passenger 1') -and $page.Contains('Contact Test')) 'validation retains all entered identities'
$data=Fields $page;$data.action='quote';$data['Input.Customer.DateOfBirth']='not-a-date';$page=Post $data
Assert ($page.Contains('value="not-a-date"') -and $page.Contains('Nhập ngày sinh người đặt hợp lệ')) 'invalid date preserves attempted value and Vietnamese error'
$data=Fields $page;$data.action='quote';$data['Input.Customer.DateOfBirth']='2000-01-01';$data['Input.Customer.IdPassportNumber']='TEST-BOOKER-001';$data['Input.DiscountCode']='EXPAND5';$page=Post $data
# Concurrent repeated previews share the same session and anti-forgery token.
$data=Fields $page;$data.action='quote'
$handler=[Net.Http.HttpClientHandler]::new();$handler.CookieContainer=$script:webSession.Cookies
$client=[Net.Http.HttpClient]::new($handler)
try {
 $pairs=[Collections.Generic.Dictionary[string,string]]::new();foreach($key in $data.Keys){$pairs[$key]=[string]$data[$key]}
 $first=$client.PostAsync("$BaseUrl/Booking/Checkout",[Net.Http.FormUrlEncodedContent]::new($pairs))
 $second=$client.PostAsync("$BaseUrl/Booking/Checkout",[Net.Http.FormUrlEncodedContent]::new($pairs))
 foreach($task in @($first,$second)){
  $response=$task.GetAwaiter().GetResult();$response.EnsureSuccessStatusCode() | Out-Null
  $body=[Net.WebUtility]::HtmlDecode($response.Content.ReadAsStringAsync().GetAwaiter().GetResult())
  Assert ((Total $body) -eq $discounted -and $body.Contains('Mã giảm giá này đã được áp dụng')) 'concurrent repeated preview keeps one discount and consistent notice'
 }
} finally {$client.Dispose()}
