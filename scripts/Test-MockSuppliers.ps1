# Requires PowerShell 7. Run against isolated mock instances, not an active demo.
param(
    [string]$AbcBaseUrl = 'http://127.0.0.1:15102',
    [string]$CdeBaseUrl = 'http://127.0.0.1:15101'
)
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$abc = Get-Content (Join-Path $root 'AbcMockApi/Data/hotels.json') -Raw | ConvertFrom-Json
$cde = Get-Content (Join-Path $root 'CdeMockApi/Data/activities.json') -Raw | ConvertFrom-Json
$start = [DateOnly]::FromDateTime([DateTime]::UtcNow).AddDays(30)
$day0 = $start.ToString('yyyy-MM-dd')
$day2 = $start.AddDays(2).ToString('yyyy-MM-dd')
$day4 = $start.AddDays(4).ToString('yyyy-MM-dd')
$runId = [Guid]::NewGuid().ToString('N')
function Assert-Equal($actual, $expected, [string]$message) {
    if ($actual -ne $expected) { throw "$message (expected $expected, got $actual)" }
}
function Post-Json([string]$url, $body) {
    Invoke-RestMethod $url -Method Post -ContentType 'application/json' -Body (ConvertTo-Json -InputObject $body -Depth 10) -TimeoutSec 15
}
function Assert-PostStatus([string]$url, $body, [int]$status) {
    $response = Invoke-WebRequest $url -Method Post -ContentType 'application/json' -Body (ConvertTo-Json -InputObject $body -Depth 10) -SkipHttpErrorCheck -TimeoutSec 15
    Assert-Equal ([int]$response.StatusCode) $status "POST $url"
}
function Hotel-Quote([string]$code, [string]$room, [string]$from = $day0, [string]$to = $day2) {
    Invoke-RestMethod "$AbcBaseUrl/api/hotels/$code/availability?roomCode=$room&checkIn=$from&checkOut=$to&adults=2&children=0&rooms=1" -TimeoutSec 15
}
function Activity-Quote([string]$id, [string]$package, [string]$slot) {
    Invoke-RestMethod "$CdeBaseUrl/api/activities/$id/availability?packageId=$package&date=$day0&timeSlot=$slot&adults=2&children=1" -TimeoutSec 15
}
$hotelBookings = @()
$activityBookings = @()
try {
    $hotels = Invoke-RestMethod "$AbcBaseUrl/api/hotels" -TimeoutSec 15
    $activities = Invoke-RestMethod "$CdeBaseUrl/api/activities" -TimeoutSec 15
    Assert-Equal (ConvertTo-Json -InputObject $hotels -Depth 20 -Compress) (ConvertTo-Json -InputObject $abc.hotels -Depth 20 -Compress) 'Hotel catalog contract differs from the dataset'
    Assert-Equal (ConvertTo-Json -InputObject $activities -Depth 20 -Compress) (ConvertTo-Json -InputObject $cde.activities -Depth 20 -Compress) 'Activity catalog contract differs from the dataset'
    foreach ($hotel in $abc.hotels) {
        foreach ($room in $hotel.roomTypes) {
            $quote = Hotel-Quote $hotel.hotelCode $room.roomCode
            Assert-Equal $quote.roomsLeft $abc.roomTypes.($room.roomCode).inventory 'Hotel starting inventory'
            Assert-Equal $quote.nightlyRate $abc.roomTypes.($room.roomCode).nightlyRate 'Hotel nightly rate'
            Assert-Equal $quote.currency $abc.currency 'Hotel currency'
        }
    }
    foreach ($activity in $cde.activities) {
        foreach ($package in $activity.packages) {
            $quote = Activity-Quote $activity.activityId $package.packageId $activity.timeSlots[0]
            Assert-Equal $quote.remainingSeats $cde.packages.($package.packageId).inventory 'Activity starting inventory'
            Assert-Equal $quote.adultTicketPrice $cde.packages.($package.packageId).adultRate 'Adult ticket price'
            Assert-Equal $quote.childTicketPrice $cde.packages.($package.packageId).childRate 'Child ticket price'
            Assert-Equal $quote.currencyCode $cde.currencyCode 'Activity currency'
        }
    }
    'PASS: both catalog contracts, every option rate and starting inventory'

    $request = @{idempotencyKey="json-$runId-hotel";hotelCode='ABC-H001';roomCode='STD';checkIn=$day0;checkOut=$day2;adults=2;children=0;rooms=2;guestName='JSON migration smoke test'}
    $hotel = Post-Json "$AbcBaseUrl/api/hotels/bookings" $request
    $hotelBookings += $hotel.bookingId
    Assert-Equal $hotel.totalAmount ($abc.roomTypes.STD.nightlyRate * 2 * 2) 'Hotel booking price'
    Assert-Equal $hotel.currency $abc.currency 'Hotel booking currency'
    $replay = Post-Json "$AbcBaseUrl/api/hotels/bookings" $request
    Assert-Equal $replay.bookingId $hotel.bookingId 'Hotel idempotency'
    Assert-Equal (Hotel-Quote 'ABC-H001' 'STD').roomsLeft ($abc.roomTypes.STD.inventory - 2) 'Hotel reservation was not shared across requests'
    $conflict = $request.Clone(); $conflict.guestName = 'Different request'
    Assert-PostStatus "$AbcBaseUrl/api/hotels/bookings" $conflict 409
    $oversell = $request.Clone(); $oversell.idempotencyKey += '-oversell'; $oversell.rooms = $abc.roomTypes.STD.inventory; $oversell.adults = $oversell.rooms
    Assert-PostStatus "$AbcBaseUrl/api/hotels/bookings" $oversell 409
    $adjacent = $request.Clone(); $adjacent.idempotencyKey += '-adjacent'; $adjacent.checkIn = $day2; $adjacent.checkOut = $day4
    $secondHotel = Post-Json "$AbcBaseUrl/api/hotels/bookings" $adjacent
    $hotelBookings += $secondHotel.bookingId
    Assert-Equal (Hotel-Quote 'ABC-H001' 'STD' $day0 $day4).roomsLeft ($abc.roomTypes.STD.inventory - 2) 'Hotel inventory must use peak nightly occupancy'
    'PASS: hotel booking, shared inventory, idempotency, conflicts and adjacent-date occupancy'

    $request = @{idempotencyKey="json-$runId-activity";activityId='CDE-A101';packageId='MORNING';date=$day0;timeSlot=$cde.activities[0].timeSlots[0];adults=2;children=1;customerName='JSON migration smoke test'}
    $activity = Post-Json "$CdeBaseUrl/api/activities/bookings" $request
    $activityBookings += $activity.bookingId
    Assert-Equal $activity.payableAmount ($cde.packages.MORNING.adultRate * 2 + $cde.packages.MORNING.childRate) 'Activity booking price'
    Assert-Equal $activity.currencyCode $cde.currencyCode 'Activity booking currency'
    $replay = Post-Json "$CdeBaseUrl/api/activities/bookings" $request
    Assert-Equal $replay.bookingId $activity.bookingId 'Activity idempotency'
    Assert-Equal (Activity-Quote 'CDE-A101' 'MORNING' $request.timeSlot).remainingSeats ($cde.packages.MORNING.inventory - 3) 'Activity reservation was not shared across requests'
    Assert-Equal (Activity-Quote 'CDE-A101' 'MORNING' $cde.activities[0].timeSlots[1]).remainingSeats $cde.packages.MORNING.inventory 'Activity time slots must have separate inventory'
    $conflict = $request.Clone(); $conflict.children = 0
    Assert-PostStatus "$CdeBaseUrl/api/activities/bookings" $conflict 409
    $oversell = $request.Clone(); $oversell.idempotencyKey += '-oversell'; $oversell.adults = $cde.packages.MORNING.inventory; $oversell.children = 0
    Assert-PostStatus "$CdeBaseUrl/api/activities/bookings" $oversell 409
    $badSlot = $request.Clone(); $badSlot.idempotencyKey += '-bad-slot'; $badSlot.timeSlot = 'invalid'
    Assert-PostStatus "$CdeBaseUrl/api/activities/bookings" $badSlot 400
    'PASS: activity booking, shared inventory, idempotency, conflicts and time-slot validation'
}
finally {
    foreach ($id in $hotelBookings) {
        $cancelled = Invoke-RestMethod "$AbcBaseUrl/api/hotels/bookings/$id/cancel" -Method Post -TimeoutSec 15
        Assert-Equal $cancelled.status 'Cancelled' 'Hotel cancellation'
    }
    foreach ($id in $activityBookings) {
        $cancelled = Invoke-RestMethod "$CdeBaseUrl/api/activities/bookings/$id/cancel" -Method Post -TimeoutSec 15
        Assert-Equal $cancelled.status 'Cancelled' 'Activity cancellation'
    }
}
Assert-Equal (Hotel-Quote 'ABC-H001' 'STD').roomsLeft $abc.roomTypes.STD.inventory 'Hotel inventory was not restored'
Assert-Equal (Activity-Quote 'CDE-A101' 'MORNING' $cde.activities[0].timeSlots[0]).remainingSeats $cde.packages.MORNING.inventory 'Activity inventory was not restored'
'PASS: cancellation restored inventory for both suppliers'
'All JSON-backed mock supplier smoke tests passed.'
