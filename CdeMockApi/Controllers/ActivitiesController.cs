using CdeMockApi.Models;
using CdeMockApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace CdeMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private readonly ActivityMockData _data;

        public ActivitiesController(ActivityMockData data)
        {
            _data = data;
        }

        [HttpGet]
        public ActionResult<List<Activity>> GetActivities()
        {
            return Ok(_data.Activities);
        }

        [HttpGet("{activityId}")]
        public ActionResult<Activity> GetActivity(
            string activityId)
        {
            var activity = _data.Activities.FirstOrDefault(
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
            var activity = _data.Activities.FirstOrDefault(a =>
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

            var validSlots = activity.TimeSlots;

            if (!validSlots.Contains(timeSlot))
                return BadRequest("Invalid time slot.");


            int remainingSeats =
                _data.GetRemainingSeats(
                    activity.ActivityId,
                    package.PackageId,
                    date,
                    timeSlot);


            decimal adultPrice = _data.GetAdultRate(package.PackageId);
            decimal childPrice = _data.GetChildRate(package.PackageId);

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
                CurrencyCode = _data.CurrencyCode,
                AdultTicketPrice = adultPrice,
                ChildTicketPrice = childPrice,
                PayableAmount = total
            };

            return Ok(result);
        }

    }
}
