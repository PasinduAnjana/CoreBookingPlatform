
using CdeMockApi.Models;
using CdeMockApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CdeMockApi.Controllers
{
    [ApiController]
    [Route("api/activities/bookings")]
    public class ActivityBookingsController : ControllerBase
    {
        private readonly ActivityMockData _data;

        public ActivityBookingsController(ActivityMockData data)
        {
            _data = data;
        }

        private object Sync => _data.Sync;
        private Dictionary<string, ActivityBooking> Bookings => _data.Bookings;
        private Dictionary<string, string> IdempotencyKeys => _data.IdempotencyKeys;

        [HttpPost]
        public IActionResult CreateBooking(
            [FromBody] CreateActivityBookingRequest request)
        {
            lock (Sync)
            {
                if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
                    return BadRequest("Idempotency key is required.");

                if (IdempotencyKeys.TryGetValue(
                    request.IdempotencyKey,
                    out var existingBookingId))
                {
                    var existing = Bookings[existingBookingId];

                    if (!SameRequest(existing, request))
                        return Conflict(
                            "Idempotency key was used for a different request.");

                    return Ok(existing);
                }

                var activity = _data.Activities
                    .FirstOrDefault(a =>
                        a.ActivityId.Equals(
                            request.ActivityId,
                            StringComparison.OrdinalIgnoreCase));

                if (activity == null)
                    return NotFound("Activity not found.");

                var package = activity.Packages.FirstOrDefault(p =>
                    p.PackageId.Equals(
                        request.PackageId,
                        StringComparison.OrdinalIgnoreCase));

                if (package == null)
                    return NotFound("Package not found.");

                if (request.Date <
                        DateOnly.FromDateTime(DateTime.UtcNow) ||
                    !activity.TimeSlots.Contains(request.TimeSlot))
                {
                    return BadRequest("Invalid date or time slot.");
                }

                if (request.Adults < 0 ||
                    request.Children < 0 ||
                    request.Adults + request.Children < 1 ||
                    string.IsNullOrWhiteSpace(request.CustomerName))
                {
                    return BadRequest("Invalid participants.");
                }

                if ((request.Adults > 0 &&
                        !package.PassengerTypes.Contains("Adult")) ||
                    (request.Children > 0 &&
                        !package.PassengerTypes.Contains("Child")))
                {
                    return BadRequest(
                        "Unsupported passenger type.");
                }

                int participants =
                    request.Adults + request.Children;

                if (participants > package.MaxParticipants)
                    return BadRequest(
                        "Package participant limit exceeded.");

                int available = _data.GetRemainingSeats(
                    activity.ActivityId,
                    package.PackageId,
                    request.Date,
                    request.TimeSlot);

                if (available < participants)
                {
                    return Conflict(
                        $"Only {available} seats remain.");
                }

                decimal adultRate =
                    _data.GetAdultRate(package.PackageId);

                var booking = new ActivityBooking
                {
                    BookingId = $"CDE-B-{Guid.NewGuid():N}",
                    IdempotencyKey = request.IdempotencyKey,

                    ActivityId = activity.ActivityId,
                    PackageId = package.PackageId,

                    Date = request.Date,
                    TimeSlot = request.TimeSlot,
                    Adults = request.Adults,
                    Children = request.Children,
                    CustomerName = request.CustomerName,

                    CurrencyCode = _data.CurrencyCode,
                    PayableAmount =
                        request.Adults * adultRate +
                        request.Children * _data.GetChildRate(package.PackageId)
                };

                Bookings[booking.BookingId] = booking;
                IdempotencyKeys[request.IdempotencyKey] =
                    booking.BookingId;

                return CreatedAtAction(
                    nameof(GetBooking),
                    new { bookingId = booking.BookingId },
                    booking);
            }
        }

        private static bool SameRequest(
            ActivityBooking booking,
            CreateActivityBookingRequest request)
        {
            return string.Equals(
                       booking.ActivityId, request.ActivityId,
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       booking.PackageId, request.PackageId,
                       StringComparison.OrdinalIgnoreCase)
                   && booking.Date == request.Date
                   && booking.TimeSlot == request.TimeSlot
                   && booking.Adults == request.Adults
                   && booking.Children == request.Children
                   && booking.CustomerName == request.CustomerName;
        }

        [HttpGet("{bookingId}")]
        public IActionResult GetBooking(string bookingId)
        {
            lock (Sync)
            {
                if (!Bookings.TryGetValue(
                    bookingId, out var booking))
                    return NotFound();

                return Ok(booking);
            }
        }

        [HttpPost("{bookingId}/cancel")]
        public IActionResult CancelBooking(string bookingId)
        {
            lock (Sync)
            {
                if (!Bookings.TryGetValue(
                    bookingId, out var booking))
                    return NotFound();

                if (booking.Status == "Cancelled")
                    return Ok(booking);

                booking.Status = "Cancelled";

                return Ok(booking);
            }
        }


    }
}
