namespace IntegrationService.Models.Common
{
    public class NormalizedAvailabilityRequest
    {
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierProductId { get; set; } = string.Empty;
        public string SupplierOptionId { get; set; } = string.Empty;

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }

        public DateOnly? Date { get; set; }
        public string? TimeSlot { get; set; }

        public int Adults { get; set; } = 1;
        public int Children { get; set; } = 0;

        public int Rooms { get; set; } = 1;
    }
}
