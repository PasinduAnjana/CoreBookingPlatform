
using System.ComponentModel.DataAnnotations;
using OrderService.Models;

namespace OrderService.DTOs
{
    public class CheckoutRequestDto
    {
        [Required]
        [MinLength(1)]
        public string CustomerName { get; set; } = "";
    }

    public class UpdateOrderRequestDto
    {
        [Required]
        [StringLength(200, MinimumLength = 1)]
        public string CustomerName { get; set; } = "";
    }

    public class BookingResultDto
    {
        public int Id { get; set; }
        public int ProductId { get; set; }

        public string SupplierCode { get; set; } = "";
        public string SupplierProductId { get; set; } = "";
        public string SupplierOptionId { get; set; } = "";
        public string? SupplierBookingId { get; set; }

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
        public DateOnly? ActivityDate { get; set; }
        public string? TimeSlot { get; set; }
        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; }

        public string Status { get; set; } = "";
        public decimal TotalPrice { get; set; }
        public string Currency { get; set; } = "";
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

        public static OrderDto FromOrder(Order order) => new()
        {
            Id = order.Id,
            CartId = order.CartId,
            CustomerName = order.CustomerName,
            Status = order.Status,
            Currency = order.Currency,
            TotalPrice = order.TotalPrice,
            ErrorMessage = order.ErrorMessage,
            CreatedAt = order.CreatedAt,
            Bookings = order.Bookings
                .OrderBy(b => b.CartItemId)
                .Select(b => new BookingResultDto
                {
                    Id = b.Id,
                    ProductId = b.ProductId,
                    SupplierCode = b.SupplierCode,
                    SupplierProductId = b.SupplierProductId,
                    SupplierOptionId = b.SupplierOptionId,
                    SupplierBookingId = b.SupplierBookingId,
                    CheckIn = b.CheckIn,
                    CheckOut = b.CheckOut,
                    ActivityDate = b.ActivityDate,
                    TimeSlot = b.TimeSlot,
                    Adults = b.Adults,
                    Children = b.Children,
                    Rooms = b.Rooms,
                    Status = b.Status,
                    TotalPrice = b.TotalPrice,
                    Currency = b.Currency,
                    ErrorMessage = b.ErrorMessage
                })
                .ToList()
        };
    }
}
