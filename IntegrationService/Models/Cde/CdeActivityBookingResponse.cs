
namespace IntegrationService.Models.Cde
{
    public class CdeActivityBookingResponse
    {
        public string BookingId { get; set; } = string.Empty;
        public string ActivityId { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string CurrencyCode { get; set; } = string.Empty;
        public decimal PayableAmount { get; set; }
    }
}
