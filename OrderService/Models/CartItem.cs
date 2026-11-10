namespace OrderService.Models
{
    public class CartItem
    {
        public int Id { get; set; }

        public Guid CartId { get; set; }
        public Cart Cart { get; set; } = null!;
        public int ProductId { get; set; }
        public string SupplierOptionId { get; set; }
            = string.Empty;

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }
        public DateOnly? ActivityDate { get; set; }
        public string? TimeSlot { get; set; }

        public int Adults { get; set; }
        public int Children { get; set; }
        public int Rooms { get; set; } = 1;
    }
}
