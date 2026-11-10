using System.ComponentModel.DataAnnotations;

namespace ProductService.DTOs
{
    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public CategoryDto? Category { get; set; } = null!;
        public string? AttributesJson { get; set; }
        public string? Location { get; set; }
        public int SupplierId { get; set; }
        public SupplierDto? Supplier { get; set; } = null!;
        public string SupplierProductId { get; set; } = string.Empty;
        public string SupplierCode { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public DateTimeOffset? LastUpdated { get; set; }
    }

    public class UpdateProductDto
    {
        public int? CategoryId { get; set; }
        public bool IsActive { get; set; } = true;
    }
}