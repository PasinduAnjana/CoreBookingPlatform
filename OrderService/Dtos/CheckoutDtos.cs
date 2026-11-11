
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class CheckoutRequestDto
    {
        [Required]
        [MinLength(1)]
        public string CustomerName { get; set; } = "";
    }

    public class BookingResultDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public string SupplierCode { get; set; } = "";
        public string? SupplierBookingId { get; set; }

        public string Status { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public string? ErrorMessage { get; set; }
    }

    public class OrderDto
    {
        public Guid Id { get; set; }
        public Guid CartId { get; set; }

        public string CustomerName { get; set; } = "";
        public string Status { get; set; } = "";

        public string Currency { get; set; } = "";
        public decimal TotalPrice { get; set; }

        public string? ErrorMessage { get; set; }
        public DateTimeOffset CreatedAt { get; set; }

        public List<BookingResultDto> Bookings { get; set; } = [];
    }
}
