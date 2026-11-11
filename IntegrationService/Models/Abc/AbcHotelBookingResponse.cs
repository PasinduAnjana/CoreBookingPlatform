
namespace IntegrationService.Models.Abc
{
    public class AbcHotelBookingResponse
    {
        public string BookingId { get; set; } = string.Empty;
        public string HotelCode { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;

        public string Status { get; set; } = string.Empty;
        public string Currency { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
    }
}
