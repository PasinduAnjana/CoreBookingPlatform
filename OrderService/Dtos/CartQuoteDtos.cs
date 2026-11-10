
namespace OrderService.DTOs
{
    public class CartItemQuoteDto
    {
        public int CartItemId { get; set; }
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;

        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierOptionId { get; set; } = string.Empty;

        public bool Available { get; set; }
        public int AvailableQuantity { get; set; }

        public string Currency { get; set; } = string.Empty;
        public decimal TotalPrice { get; set; }
    }

    public class CartQuoteDto
    {
        public Guid CartId { get; set; }

        public List<CartItemQuoteDto> Items { get; set; } = [];

        public bool AllAvailable { get; set; }

        public string? Currency { get; set; }


        public decimal? TotalPrice { get; set; }

        public DateTimeOffset QuotedAt { get; set; }
    }
}
