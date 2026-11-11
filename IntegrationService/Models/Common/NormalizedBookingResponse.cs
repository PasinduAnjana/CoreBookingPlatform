
namespace IntegrationService.Models.Common
{
    public class NormalizedBookingResponse
    {
        public string SupplierCode { get; set; } = string.Empty;

        public string SupplierBookingId { get; set; }= string.Empty;

        public string SupplierProductId { get; set; } = string.Empty;

        public string SupplierOptionId { get; set; }  = string.Empty;

        public string Status { get; set; } = string.Empty;

        public string Currency { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }
    }
}
