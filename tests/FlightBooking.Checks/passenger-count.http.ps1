param([string]$BaseUrl = 'http://127.0.0.1:5084')
$ErrorActionPreference = 'Stop'
function Assert($condition,$label) { if(-not $condition){throw "FAIL: $label"};"PASS: $label" }
function Fields($html) {
 $fields=@{}
 foreach($tag in [regex]::Matches($html,'<input\b[^>]*>')) {
  $n=[regex]::Match($tag.Value,'name="([^"]+)"');$v=[regex]::Match($tag.Value,'value="([^"]*)"')
  if($n.Success){$fields[[Net.WebUtility]::HtmlDecode($n.Groups[1].Value)]=[Net.WebUtility]::HtmlDecode($v.Groups[1].Value)}
 }
 return $fields
}
function Choice($html){
 $m=[regex]::Match($html,'<button[^>]*name="SelectedOffer"[^>]*value="([^"]+)"')
 if(-not $m.Success){throw 'Expected future flight choice from seed 04; do not rerun seed on an existing database.'}
 return [Net.WebUtility]::HtmlDecode($m.Groups[1].Value)
}
function Query($trip,$count,$draft) {
 return @{'Submitted'='true';'Search.TripType'=$trip;'Search.DepartureAirportId'='1';'Search.ArrivalAirportId'='2';'Search.DepartureDate'='2026-11-24';'Search.ReturnDate'=$(if($trip -eq 'RoundTrip'){'2026-11-27'}else{''});'Search.Passengers'="$count";'DraftId'=$draft}
}
# Uses SGN/HAN IDs from this local demo installation; no SQL writes or confirmation.
foreach($trip in @('OneWay','RoundTrip')) {
 $session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
 $search=Invoke-WebRequest "$BaseUrl/" -Method Get -Body (Query $trip 1 '') -WebSession $session
 $query=Query $trip 1 ''; $query.SelectedOffer=Choice $search.Content
 $page=Invoke-WebRequest "$BaseUrl/" -Method Get -Body $query -WebSession $session
 if($trip -eq 'RoundTrip'){
  $hidden=Fields $page.Content;$query.OutboundFlightId=$hidden.OutboundFlightId;$query.OutboundFareId=$hidden.OutboundFareId
  $query.SelectedOffer=Choice $page.Content
  $page=Invoke-WebRequest "$BaseUrl/" -Method Get -Body $query -WebSession $session
 }
 Assert ([regex]::Matches($page.Content,'class="passenger-card"').Count -eq 1) "$trip starts with 1 passenger form"
 $initial=Fields $page.Content
 $onePrice=[decimal]::Parse($initial['Input.ExpectedTotal'],[cultureinfo]::GetCultureInfo('vi-VN'))
 foreach($count in @(2,3)) {
  $posted=Fields $page.Content
  $posted['Input.Passengers[0].FullName']='Preserved Passenger'
  $posted['Input.Passengers[0].IdCard']='PRESERVED-001'
  $posted['Input.Customer.FullName']='Separate Booker'
  $posted['Input.Customer.Gender']='Nam';$posted.action='back'
  $back=Invoke-WebRequest "$BaseUrl/Booking/Checkout" -Method Post -Body $posted -WebSession $session
  # Change count but click the already rendered flight choice without another Search click.
  $query=Query $trip $count $posted.DraftId
  $query.SelectedOffer=Choice $back.Content
  $page=Invoke-WebRequest "$BaseUrl/" -Method Get -Body $query -WebSession $session
  if($trip -eq 'RoundTrip'){
   $hidden=Fields $page.Content;$query.OutboundFlightId=$hidden.OutboundFlightId;$query.OutboundFareId=$hidden.OutboundFareId
   $query.SelectedOffer=Choice $page.Content
   $page=Invoke-WebRequest "$BaseUrl/" -Method Get -Body $query -WebSession $session
  }
  $fields=Fields $page.Content
  Assert ([regex]::Matches($page.Content,'class="passenger-card"').Count -eq $count -and $fields['Input.Quantity'] -eq "$count") "$trip changed to $count has exactly $count forms and current quantity"
  Assert ($page.Content.Contains('Preserved Passenger') -and $page.Content.Contains('Separate Booker')) "$trip keeps passenger and separate booker"
  $total=[decimal]::Parse($fields['Input.ExpectedTotal'],[cultureinfo]::GetCultureInfo('vi-VN'))
  Assert ($total -eq ($onePrice*$count)) "$trip total recalculated for $count"
 }
}

