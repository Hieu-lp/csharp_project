param([string]$BaseUrl = 'http://127.0.0.1:5085')
$ErrorActionPreference = 'Stop'
function Assert($value,$label) { if(-not $value){throw "FAIL: $label"}; "PASS: $label" }
function ReadPage($query) { $response=Invoke-WebRequest "$BaseUrl/" -Method Get -Body $query -WebSession $script:session; [pscustomobject]@{Content=[Net.WebUtility]::HtmlDecode($response.Content)} }
function Offer($html) { [regex]::Match($html,'name="SelectedOffer"[^>]*value="([^"]+)"').Groups[1].Value }
$script:session=New-Object Microsoft.PowerShell.Commands.WebRequestSession
$landingPage=ReadPage @{}
$cards=[regex]::Matches($landingPage.Content,'name="SelectedDeal"[^>]*value="([^"]+)"')
Assert ($cards.Count -gt 0 -and $cards.Count -le 4) 'homepage has real deal cards'
Assert (-not $landingPage.Content.Contains('name="Search.Join"') -and -not $landingPage.Content.Contains('name="Search.Not"')) 'customer form has no technical predicates'
$ids=@($cards | ForEach-Object { $_.Groups[1].Value.Split(':')[0] })
Assert (($ids | Select-Object -Unique).Count -eq $ids.Count) 'one card per flight'
$pair=$cards[0].Groups[1].Value.Split(':')
# Simulate a previous round trip and tampered route: the selected card must reset it.
$selected=ReadPage @{'SelectedDeal'=$cards[0].Groups[1].Value;'Search.Passengers'='3';'Search.TripType'='RoundTrip';'Search.ReturnDate'='2027-01-31';'Search.DepartureAirportId'='999'}
Assert ($selected.Content.Contains('id="selected-title"') -and $selected.Content.Contains("Chuyến #$($pair[0])")) 'card opens exact selected flight immediately'
Assert ($selected.Content -match 'id="trip-one-way"[^>]*checked|checked[^>]*id="trip-one-way"') 'previous round-trip reset to one-way'
Assert ($selected.Content -match '<option value="3" selected="selected">') 'homepage passenger count preserved'
foreach($count in @(1,2,3)) {
 $query=@{'DealFlightId'=$pair[0];'DealFareId'=$pair[1];'Search.Passengers'="$count"}
 $detail=ReadPage $query
 Assert ($detail.Content.Contains('id="selected-title"') -and $detail.Content.Contains("Chuyến #$($pair[0])")) "quantity $count retains exact selection"
 $query.ContinueDeal='true'; $checkout=ReadPage $query
 Assert ([regex]::Matches($checkout.Content,'class="passenger-card"').Count -eq $count) "quantity $count reaches matching passenger forms without JavaScript"
}
$bad=ReadPage @{'DealFlightId'=$pair[0];'DealFareId'=$pair[1];'Search.Passengers'='10';'ContinueDeal'='true'}
Assert ($bad.Content.Contains('role="alert"') -and -not $bad.Content.Contains('class="passenger-card"')) 'tampered quantity rejected'
$bad=ReadPage @{'DealFlightId'=$pair[0];'DealFareId'='2147483647';'Search.Passengers'='1'}
Assert ($bad.Content.Contains('role="alert"') -and -not $bad.Content.Contains('id="selected-title"')) 'invalid fare rejected with escape link'
# Existing seed 09 fixture; IDs are only fixture values, not product defaults.
$search=@{'Submitted'='true';'Search.TripType'='RoundTrip';'Search.DepartureAirportId'='1';'Search.ArrivalAirportId'='2';'Search.DepartureDate'='2026-11-24';'Search.ReturnDate'='2026-11-27';'Search.Passengers'='2';'MinPriceText'='1.000.000';'MaxPriceText'='1.500.000';'Search.FareClassName'='Economy'}
$filtered=ReadPage $search
Assert ((Offer $filtered.Content) -ne '' -and -not $filtered.Content.Contains('· Business')) 'price range AND actual fare class filters result'
$search.MinPriceText='2.000.000';$search.MaxPriceText='1.000.000'
$bad=ReadPage $search
Assert ($bad.Content.Contains('role="alert"')) 'inverted price range rejected on server'
Assert ($bad.Content.Contains('Sân bay Tân Sơn Nhất') -and $bad.Content.Contains('2026-11-24')) 'invalid prices retain airport options and itinerary'
$search.FilterAction='clear'; $cleared=ReadPage $search
Assert ($cleared.Content -match '<option value="2" selected="selected">' -and $cleared.Content.Contains('2026-11-27') -and (Offer $cleared.Content) -ne '') 'clear removes invalid filters and retains round trip and quantity'
$search.Remove('FilterAction');$search.Remove('MinPriceText');$search.Remove('MaxPriceText');$search.Remove('Search.FareClassName')
$search.SelectedOffer=Offer $cleared.Content
$returning=ReadPage $search
Assert ($returning.Content.Contains('Chọn chuyến về') -and (Offer $returning.Content) -ne '') 'round-trip return search still works'
$search.OutboundFlightId=$search.SelectedOffer.Split(':')[0];$search.OutboundFareId=$search.SelectedOffer.Split(':')[1]
$search.SelectedOffer=Offer $returning.Content
$checkout=ReadPage $search
Assert ([regex]::Matches($checkout.Content,'class="passenger-card"').Count -eq 2 -and $checkout.Content.Contains('name="Input.ReturnFlightId"')) 'round trip keeps two passenger forms and return flight'



