using AbcMockApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AbcMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        internal static readonly List<Hotel> Hotels =
        [
            new Hotel
            {
                HotelCode = "ABC-H001",
                HotelName = "Ocean View Hotel",
                City = "Colombo",
                StarRating = 4,
                Description = "A beachfront hotel in Colombo.",
                Facilities = ["Wi-Fi", "Pool", "Restaurant"],
                Images =
                [
                    "https://images.pexels.com/photos/32334284/pexels-photo-32334284.jpeg",
                    "https://images.pexels.com/photos/9752555/pexels-photo-9752555.jpeg"
                ],
                RoomTypes =
                [
                    new HotelRoom
                    {
                        RoomCode = "STD",
                        RoomName = "Standard Room",
                        MaxAdults = 2,
                        MaxChildren = 1,
                        BedType = "Double"
                    },
                    new HotelRoom
                    {
                        RoomCode = "DLX",
                        RoomName = "Deluxe Room",
                        MaxAdults = 2,
                        MaxChildren = 2,
                        BedType = "King"
                    }
                ]
            },
            new Hotel
            {
                HotelCode = "ABC-H002",
                HotelName = "Mountain Retreat",
                City = "Kandy",
                StarRating = 5,
                Description = "A luxury retreat surrounded by mountains.",
                Facilities = ["Wi-Fi", "Spa", "Restaurant"],
                Images = ["https://images.pexels.com/photos/1234567/pexels-photo-1234567.jpeg"],
                RoomTypes =
                [
                    new HotelRoom
                    {
                        RoomCode = "SUITE",
                        RoomName = "Mountain Suite",
                        MaxAdults = 3,
                        MaxChildren = 1,
                        BedType = "King"
                    }
                ]
            }
        ];

        [HttpGet]
        public IActionResult GetHotels()
        {
            return Ok(Hotels);
        }

        [HttpGet("{hotelCode}")]
        public IActionResult GetHotel(string hotelCode)
        {
            var hotel = Hotels.FirstOrDefault(h => h.HotelCode == hotelCode);
            if (hotel == null)
            {
                return NotFound();
            }
            return Ok(hotel);
        }


        [HttpGet("{hotelCode}/availability")]
        public IActionResult CheckAvailability(
            string hotelCode,
            [FromQuery] string roomCode,
            [FromQuery] DateOnly checkIn,
            [FromQuery] DateOnly checkOut,
            [FromQuery] int adults = 2,
            [FromQuery] int children = 0,
            [FromQuery] int rooms = 1)
        {
            var hotel = Hotels.FirstOrDefault(h =>
                h.HotelCode.Equals(hotelCode,StringComparison.OrdinalIgnoreCase));

            if (hotel == null)
                return NotFound("Hotel not found.");

            var room = hotel.RoomTypes.FirstOrDefault(r =>
                r.RoomCode.Equals( roomCode,StringComparison.OrdinalIgnoreCase));

            if (room == null)
                return NotFound("Room type not found.");

            if (checkIn < DateOnly.FromDateTime(DateTime.UtcNow) ||
                checkOut <= checkIn)
            {
                return BadRequest("Invalid booking dates.");
            }

            if (rooms < 1 || adults < 1 || children < 0)
                return BadRequest("Invalid guest or room count.");

            if (adults > room.MaxAdults * rooms ||
                children > room.MaxChildren * rooms ||
                adults < rooms)
            {
                return BadRequest(
                    "Guest count exceeds room capacity.");
            }


            int roomsLeft = HotelBookingsController.GetRemainingRooms(
                hotel.HotelCode,
                room.RoomCode,
                checkIn,
                checkOut);


            // Fixed mock nightly rates in LKR.
            decimal nightlyRate = room.RoomCode.ToUpperInvariant() switch
            {
                "STD" => 12000m,
                "DLX" => 18000m,
                "SUITE" => 30000m,
                _ => 0m
            };

            int nights = checkOut.DayNumber - checkIn.DayNumber;
            bool available = roomsLeft >= rooms;

            var result = new HotelAvailabilityResponse
            {
                HotelCode = hotel.HotelCode,
                RoomCode = room.RoomCode,
                CheckIn = checkIn,
                CheckOut = checkOut,
                IsAvailable = available,
                RoomsLeft = roomsLeft,
                Currency = "LKR",
                NightlyRate = nightlyRate,
                Nights = nights,
                TotalAmount = available
                    ? nightlyRate * nights * rooms
                    : 0m
            };

            return Ok(result);
        }

    }
}
