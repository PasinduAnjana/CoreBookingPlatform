using AbcMockApi.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace AbcMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private static readonly List<Hotel> Hotels =
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
    }
}
