namespace IntegrationService.Models.Abc
{
    public class AbcHotel
    {
        public string HotelCode { get; set; } = string.Empty;
        public string HotelName { get; set; } = string.Empty;
        public string City { get; set; } = string.Empty;
        public int StarRating { get; set; }
        public string Description { get; set; } = string.Empty;

        public List<string> Facilities { get; set; } = [];
        public List<string> Images { get; set; } = [];

        public List<AbcRoom> RoomTypes { get; set; } = [];
    }
    public class AbcRoom
    {
        public string RoomCode { get; set; } = string.Empty;
        public string RoomName { get; set; } = string.Empty;
        public int MaxAdults { get; set; }
        public int MaxChildren { get; set; }
        public string BedType { get; set; } = string.Empty;
    }
}
