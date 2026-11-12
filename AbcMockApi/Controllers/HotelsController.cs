using AbcMockApi.Models;
using AbcMockApi.Services;
using Microsoft.AspNetCore.Mvc;

namespace AbcMockApi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class HotelsController : ControllerBase
    {
        private readonly HotelMockData _data;

        public HotelsController(HotelMockData data)
        {
            _data = data;
        }

        [HttpGet]
        public IActionResult GetHotels()
        {
            return Ok(_data.Hotels);
        }

        [HttpGet("{hotelCode}")]
        public IActionResult GetHotel(string hotelCode)
        {
            var hotel = _data.Hotels.FirstOrDefault(h => h.HotelCode == hotelCode);
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
            var hotel = _data.Hotels.FirstOrDefault(h =>
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


            int roomsLeft = _data.GetRemainingRooms(
                hotel.HotelCode,
                room.RoomCode,
                checkIn,
                checkOut);


            decimal nightlyRate = _data.GetNightlyRate(room.RoomCode);

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
                Currency = _data.Currency,
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
