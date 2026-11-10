namespace IntegrationService.Models.Abc
{
    public class AbcHotelAvailabilityResponse
    {
        public string HotelCode { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;

        public DateOnly CheckIn { get; set; }
        public DateOnly CheckOut { get; set; }

        public bool IsAvailable { get; set; }
        public int RoomsLeft { get; set; }

        public string Currency { get; set; } = string.Empty;
        public decimal NightlyRate { get; set; }
        public int Nights { get; set; }
        public decimal TotalAmount { get; set; }
    }
}
