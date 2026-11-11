
using CdeMockApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace CdeMockApi.Controllers
{
    [ApiController]
    [Route("api/activities/bookings")]
    public class ActivityBookingsController : ControllerBase
    {
        private static readonly object Sync = new();

        private static readonly Dictionary<string, ActivityBooking>
            Bookings = new();

        private static readonly Dictionary<string, string>
            IdempotencyKeys = new();

        private static int GetBaseInventory(string packageId)
        {
            return packageId.ToUpperInvariant() switch
            {
                "MORNING" => 8,
                "PRIVATE" => 6,
                "STANDARD" => 12,
                _ => 0
            };
        }

        private static decimal GetAdultRate(string packageId)
        {
            return packageId.ToUpperInvariant() switch
            {
                "MORNING" => 5000m,
                "PRIVATE" => 12000m,
                "STANDARD" => 7000m,
                _ => 0m
            };
        }

        private static int GetAvailableSeats(
            string activityId,
            string packageId,
            DateOnly date,
            string timeSlot)
        {
            int reserved = Bookings.Values
                .Where(b =>
                    b.Status == "Confirmed" &&
                    b.ActivityId == activityId &&
                    b.PackageId == packageId &&
                    b.Date == date &&
                    b.TimeSlot == timeSlot)
                .Sum(b => b.Adults + b.Children);

            return Math.Max(
                0,
                GetBaseInventory(packageId) - reserved);
        }

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

                var activity = ActivitiesController.Activities
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
                    (request.TimeSlot != "09:00" &&
                     request.TimeSlot != "14:00"))
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

                int available = GetAvailableSeats(
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
                    GetAdultRate(package.PackageId);

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

                    PayableAmount =
                        request.Adults * adultRate +
                        request.Children * adultRate * 0.5m
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


        internal static int GetRemainingSeats(
            string activityId,
            string packageId,
            DateOnly date,
            string timeSlot)
        {
            lock (Sync)
            {
                return GetAvailableSeats(
                    activityId,
                    packageId,
                    date,
                    timeSlot);
            }
        }

    }
}
