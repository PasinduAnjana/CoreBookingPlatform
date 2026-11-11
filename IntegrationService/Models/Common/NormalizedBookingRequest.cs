
namespace IntegrationService.Models.Common
{
    public class NormalizedBookingRequest
    {
        public string SupplierCode { get; set; } = string.Empty;

        public string SupplierProductId { get; set; } = string.Empty;

        public string SupplierOptionId { get; set; }  = string.Empty;

        public string IdempotencyKey { get; set; }  = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        // Hotel-specific
        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
        public int Rooms { get; set; } = 1;

        // Activity-specific
        public DateOnly? Date { get; set; }
        public string? TimeSlot { get; set; }

        // Shared
        public int Adults { get; set; }
        public int Children { get; set; }
    }
}
