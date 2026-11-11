
namespace IntegrationService.Models.Common
{
    public class NormalizedCancellationRequest
    {
        public string SupplierCode { get; set; }
            = string.Empty;

        public string SupplierBookingId { get; set; }
            = string.Empty;
    }

    public class NormalizedCancellationResponse
    {
        public string SupplierCode { get; set; }
            = string.Empty;

        public string SupplierBookingId { get; set; }
            = string.Empty;

        public string Status { get; set; } = string.Empty;
    }
}
