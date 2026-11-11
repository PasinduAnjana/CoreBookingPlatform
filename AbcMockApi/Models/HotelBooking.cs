
namespace AbcMockApi.Models
{
    public class CreateHotelBookingRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;

        public string HotelCode { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;

        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; } = 1;

        public string GuestName { get; set; } = string.Empty;
    }

    public class HotelBooking
    {
        public string BookingId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;

        public string HotelCode { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;

        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; }

        public string GuestName { get; set; } = string.Empty;

        public string Status { get; set; } = "Confirmed";
        public string Currency { get; set; } = "LKR";

        public decimal TotalAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
            = DateTimeOffset.UtcNow;
    }
}
