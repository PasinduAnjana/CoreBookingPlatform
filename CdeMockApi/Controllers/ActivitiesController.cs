using CdeMockApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CdeMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        internal static readonly List<Activity> Activities =
        [
            new Activity
            {
                ActivityId = "CDE-A101",
                Title = "Colombo City Walking Tour",
                Destination = "Colombo",
                DurationMinutes = 180,
                Overview = "Explore the historic landmarks of Colombo.",
                Gallery =
                [
                    "https://images.pexels.com/photos/9584386/pexels-photo-9584386.jpeg"
                ],
                Includes = ["Tour guide", "Drinking water"],
                Excludes = ["Meals", "Transport"],
                Packages =
                [
                    new ActivityPackage
                    {
                        PackageId = "MORNING",
                        Label = "Morning Group Tour",
                        MaxParticipants = 15,
                        PassengerTypes = ["Adult", "Child"]
                    },
                    new ActivityPackage
                    {
                        PackageId = "PRIVATE",
                        Label = "Private Tour",
                        MaxParticipants = 6,
                        PassengerTypes = ["Adult", "Child"]
                    }
                ]
            },
            new Activity
            {
                ActivityId = "CDE-A102",
                Title = "Kandy Cultural Experience",
                Destination = "Kandy",
                DurationMinutes = 240,
                Overview = "Experience traditional Kandyan culture.",
                Gallery =
                [
                    "https://images.pexels.com/photos/20836970/pexels-photo-20836970.jpeg"
                ],
                Includes = ["Guide", "Entry tickets"],
                Excludes = ["Hotel pickup"],
                Packages =
                [
                    new ActivityPackage
                    {
                        PackageId = "STANDARD",
                        Label = "Standard Experience",
                        MaxParticipants = 20,
                        PassengerTypes = ["Adult", "Child"]
                    }
                ]
            }
        ];

        [HttpGet]
        public ActionResult<List<Activity>> GetActivities()
        {
            return Ok(Activities);
        }

        [HttpGet("{activityId}")]
        public ActionResult<Activity> GetActivity(
            string activityId)
        {
            var activity = Activities.FirstOrDefault(
                a => a.ActivityId == activityId);

            if (activity == null)
                return NotFound();

            return Ok(activity);
        }


        [HttpGet("{activityId}/availability")]
        public IActionResult CheckAvailability(
            string activityId,
            [FromQuery] string packageId,
            [FromQuery] DateOnly date,
            [FromQuery] string timeSlot,
            [FromQuery] int adults = 1,
            [FromQuery] int children = 0)
        {
            var activity = Activities.FirstOrDefault(a =>
                a.ActivityId.Equals(
                    activityId,
                    StringComparison.OrdinalIgnoreCase));

            if (activity == null)
                return NotFound("Activity not found.");

            var package = activity.Packages.FirstOrDefault(p =>
                p.PackageId.Equals(
                    packageId,
                    StringComparison.OrdinalIgnoreCase));

            if (package == null)
                return NotFound("Package not found.");

            if (date < DateOnly.FromDateTime(DateTime.UtcNow))
                return BadRequest("Invalid activity date.");

            if (adults < 0 || children < 0 || adults + children < 1)
                return BadRequest("Invalid participant count.");

            if (children > 0 &&
                !package.PassengerTypes.Contains("Child"))
            {
                return BadRequest(
                    "Children are not allowed for this package.");
            }

            if (adults > 0 &&
                !package.PassengerTypes.Contains("Adult"))
            {
                return BadRequest(
                    "Adults are not allowed for this package.");
            }

            var validSlots = new[] { "09:00", "14:00" };

            if (!validSlots.Contains(timeSlot))
                return BadRequest("Invalid time slot.");


            int remainingSeats =
                ActivityBookingsController.GetRemainingSeats(
                    activity.ActivityId,
                    package.PackageId,
                    date,
                    timeSlot);


            // Mock ticket prices vary by package.
            decimal adultPrice = package.PackageId.ToUpperInvariant() switch
            {
                "MORNING" => 5000m,
                "PRIVATE" => 12000m,
                "STANDARD" => 7000m,
                _ => 0m
            };

            decimal childPrice = adultPrice * 0.5m;

            int participants = adults + children;

            bool available =
                participants <= remainingSeats &&
                participants <= package.MaxParticipants;

            decimal total = available
                ? adults * adultPrice + children * childPrice
                : 0m;

            var result = new ActivityAvailabilityResponse
            {
                ActivityId = activity.ActivityId,
                PackageId = package.PackageId,
                Date = date,
                TimeSlot = timeSlot,
                Available = available,
                RemainingSeats = remainingSeats,
                Adults = adults,
                Children = children,
                CurrencyCode = "LKR",
                AdultTicketPrice = adultPrice,
                ChildTicketPrice = childPrice,
                PayableAmount = total
            };

            return Ok(result);
        }

    }
}
