using CdeMockApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace CdeMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ActivitiesController : ControllerBase
    {
        private static readonly List<Activity> Activities =
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
    }
}
