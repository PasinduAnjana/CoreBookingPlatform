
namespace OrderService.Models
{
    public class Booking
    {
        public int Id { get; set; }

        public Guid OrderId { get; set; }
        public Order Order { get; set; } = null!;

        public int CartItemId { get; set; }
        public int ProductId { get; set; }

        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierProductId { get; set; } = string.Empty;
        public string SupplierOptionId { get; set; } = string.Empty;

        public string IdempotencyKey { get; set; } = string.Empty;
        public string? SupplierBookingId { get; set; }

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }

        public DateOnly? ActivityDate { get; set; }
        public string? TimeSlot { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; }

        public string Status { get; set; } = BookingStatus.Pending;

        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "LKR";

        public string? ErrorMessage { get; set; }
    }
}
