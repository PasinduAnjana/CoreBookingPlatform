namespace IntegrationService.Models.Cde
{
    public class CdeActivityAvailabilityResponse
    {
        public string ActivityId { get; set; } = string.Empty;
        public string PackageId { get; set; } = string.Empty;

        public DateOnly Date { get; set; }
        public string TimeSlot { get; set; } = string.Empty;

        public bool Available { get; set; }
        public int RemainingSeats { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }

        public string CurrencyCode { get; set; } = string.Empty;

        public decimal AdultTicketPrice { get; set; }
        public decimal ChildTicketPrice { get; set; }

        public decimal PayableAmount { get; set; }
    }
}
