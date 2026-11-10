
using System.ComponentModel.DataAnnotations;

namespace OrderService.DTOs
{
    public class AddCartItemDto
    {
        [Range(1, int.MaxValue)]
        public int ProductId { get; set; }

        [Required]
        public string SupplierOptionId { get; set; }
            = string.Empty;

        public DateOnly? CheckIn { get; set; }
        public DateOnly? CheckOut { get; set; }

        public DateOnly? ActivityDate { get; set; }
        public string? TimeSlot { get; set; }

        [Range(0, int.MaxValue)]
        public int Adults { get; set; }

        [Range(0, int.MaxValue)]
        public int Children { get; set; }

        [Range(1, int.MaxValue)]
        public int Rooms { get; set; } = 1;
    }

    public class CartItemDto : AddCartItemDto
    {
        public int Id { get; set; }
    }

    public class CartDto
    {
        public Guid Id { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<CartItemDto> Items { get; set; } = [];
    }
}
