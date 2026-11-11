
using AbcMockApi.Models;
using Microsoft.AspNetCore.Mvc;

namespace AbcMockApi.Controllers
{
    [ApiController]
    [Route("api/hotels/bookings")]
    public class HotelBookingsController : ControllerBase
    {
        private static readonly object Sync = new();

        private static readonly Dictionary<string, HotelBooking>
            Bookings = new();

        private static readonly Dictionary<string, string>
            IdempotencyKeys = new();

        private static int GetBaseInventory(string roomCode)
        {
            return roomCode.ToUpperInvariant() switch
            {
                "STD" => 5,
                "DLX" => 2,
                "SUITE" => 1,
                _ => 0
            };
        }

        private static decimal GetNightlyRate(string roomCode)
        {
            return roomCode.ToUpperInvariant() switch
            {
                "STD" => 12000m,
                "DLX" => 18000m,
                "SUITE" => 30000m,
                _ => 0m
            };
        }

        private static int GetAvailableRooms(
            string hotelCode,
            string roomCode,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            var overlappingBookings = Bookings.Values
                .Where(b =>
                    b.Status == "Confirmed" &&
                    b.HotelCode == hotelCode &&
                    b.RoomCode == roomCode &&
                    b.CheckIn < checkOut &&
                    b.CheckOut > checkIn)
                .ToList();

            int maximumUsed = 0;

            for (var date = checkIn;
                 date < checkOut;
                 date = date.AddDays(1))
            {
                int used = overlappingBookings
                    .Where(b => b.CheckIn <= date &&
                                b.CheckOut > date)
                    .Sum(b => b.Rooms);

                maximumUsed = Math.Max(maximumUsed, used);
            }

            return Math.Max(
                0,
                GetBaseInventory(roomCode) - maximumUsed);
        }

        [HttpPost]
        public IActionResult CreateBooking(
            [FromBody] CreateHotelBookingRequest request)
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
                        return Conflict("Idempotency key was used for a different request.");

                    return Ok(existing);
                }

                var hotel = HotelsController.Hotels.FirstOrDefault(
                    h => h.HotelCode.Equals(
                        request.HotelCode,
                        StringComparison.OrdinalIgnoreCase));

                if (hotel == null)
                    return NotFound("Hotel not found.");

                var room = hotel.RoomTypes.FirstOrDefault(
                    r => r.RoomCode.Equals(
                        request.RoomCode,
                        StringComparison.OrdinalIgnoreCase));

                if (room == null)
                    return NotFound("Room type not found.");

                if (request.CheckIn < DateOnly.FromDateTime(DateTime.UtcNow) ||
                    request.CheckOut <= request.CheckIn)
                {
                    return BadRequest("Invalid booking dates.");
                }

                if (request.Rooms < 1 ||
                    request.Adults < request.Rooms ||
                    request.Children < 0 ||
                    request.Adults > room.MaxAdults * request.Rooms ||
                    request.Children > room.MaxChildren * request.Rooms ||
                    string.IsNullOrWhiteSpace(request.GuestName))
                {
                    return BadRequest("Invalid guests or room count.");
                }

                int available = GetAvailableRooms(
                    hotel.HotelCode,
                    room.RoomCode,
                    request.CheckIn,
                    request.CheckOut);

                if (available < request.Rooms)
                {
                    return Conflict(
                        $"Only {available} rooms are available.");
                }

                int nights =
                    request.CheckOut.DayNumber -
                    request.CheckIn.DayNumber;

                var booking = new HotelBooking
                {
                    BookingId = $"ABC-B-{Guid.NewGuid():N}",
                    IdempotencyKey = request.IdempotencyKey,

                    HotelCode = hotel.HotelCode,
                    RoomCode = room.RoomCode,

                    CheckIn = request.CheckIn,
                    CheckOut = request.CheckOut,
                    Adults = request.Adults,
                    Children = request.Children,
                    Rooms = request.Rooms,
                    GuestName = request.GuestName,

                    TotalAmount = GetNightlyRate(room.RoomCode)* nights * request.Rooms
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
            HotelBooking booking,
            CreateHotelBookingRequest request)
        {
            return string.Equals(
                       booking.HotelCode, request.HotelCode,
                       StringComparison.OrdinalIgnoreCase)
                   && string.Equals(
                       booking.RoomCode, request.RoomCode,
                       StringComparison.OrdinalIgnoreCase)
                   && booking.CheckIn == request.CheckIn
                   && booking.CheckOut == request.CheckOut
                   && booking.Adults == request.Adults
                   && booking.Children == request.Children
                   && booking.Rooms == request.Rooms
                   && booking.GuestName == request.GuestName;
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

        internal static int GetRemainingRooms(
            string hotelCode,
            string roomCode,
            DateOnly checkIn,
            DateOnly checkOut)
        {
            lock (Sync)
            {
                return GetAvailableRooms(
                    hotelCode,
                    roomCode,
                    checkIn,
                    checkOut);
            }
        }

    }
}
