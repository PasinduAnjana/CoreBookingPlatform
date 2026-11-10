namespace IntegrationService.Models.Common
{
    public class NormalizedAvailabilityResponse
    {
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierProductId { get; set; } = string.Empty;
        public string SupplierOptionId { get; set; } = string.Empty;

        public bool Available { get; set; }
        public int AvailableQuantity { get; set; }

        public string Currency { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }
}
