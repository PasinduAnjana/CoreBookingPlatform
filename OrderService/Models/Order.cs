namespace OrderService.Models
    {
        public class Order
        {
            public Guid Id { get; set; } = Guid.NewGuid();

            public Guid CartId { get; set; }

            public string CustomerName { get; set; } = string.Empty;

            public string Status { get; set; } = OrderStatus.Pending;

            public string Currency { get; set; } = "LKR";

            public decimal TotalPrice { get; set; }

            public string? ErrorMessage { get; set; }

            public DateTimeOffset CreatedAt { get; set; }
                = DateTimeOffset.UtcNow;

            public ICollection<Booking> Bookings { get; set; }
                = new List<Booking>();
        }
    }

