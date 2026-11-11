
namespace CdeMockApi.Models
{
    public class CreateActivityBookingRequest
    {
        public string IdempotencyKey { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string TimeSlot { get; set; } = string.Empty;
        public int Adults { get; set; }
        public int Children { get; set; }
        public string CustomerName { get; set; } = string.Empty;
    }

    public class ActivityBooking
    {
        public string BookingId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;
        public DateOnly Date { get; set; }
        public string TimeSlot { get; set; } = string.Empty;

        public int Adults { get; set; }
        public int Children { get; set; }

        public string CustomerName { get; set; } = string.Empty;

        public string Status { get; set; } = "Confirmed";
        public string CurrencyCode { get; set; } = "LKR";
        public decimal PayableAmount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }  = DateTimeOffset.UtcNow;
    }
}
