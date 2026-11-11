
namespace OrderService.DTOs
{
    public class SupplierBookingRequestDto
    {
        public string SupplierCode { get; set; } = "";
        public string SupplierProductId { get; set; } = "";
        public string SupplierOptionId { get; set; } = "";

        public string IdempotencyKey { get; set; } = "";
        public string CustomerName { get; set; } = "";

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
        public DateOnly? Date { get; set; }
        public string? TimeSlot { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; } = 1;
    }

    public class SupplierBookingResponseDto
    {
        public string SupplierCode { get; set; } = "";
        public string SupplierBookingId { get; set; } = "";
        public string SupplierProductId { get; set; } = "";
        public string SupplierOptionId { get; set; } = "";
        public string Status { get; set; } = "";
        public string Currency { get; set; } = "";
        public decimal TotalPrice { get; set; }
    }

    public class SupplierCancellationRequestDto
    {
        public string SupplierCode { get; set; } = "";
        public string SupplierBookingId { get; set; } = "";
    }

    public class SupplierCancellationResponseDto
    {
        public string SupplierCode { get; set; } = "";
        public string SupplierBookingId { get; set; } = "";
        public string Status { get; set; } = "";
    }
}
