
namespace OrderService.Models
{
    public static class OrderStatus
    {
        public const string Pending = "Pending";
        public const string Confirmed = "Confirmed";
        public const string Compensating = "Compensating";
        public const string Failed = "Failed";
        public const string RequiresAttention = "RequiresAttention";
        public const string Cancelled = "Cancelled";
        public const string CancellationFailed = "CancellationFailed";
    }

    public static class BookingStatus
    {
        public const string Pending = "Pending";
        public const string Attempting = "Attempting";
        public const string Confirmed = "Confirmed";
        public const string Cancelled = "Cancelled";
        public const string Failed = "Failed";
        public const string Unknown = "Unknown";
    }
}
