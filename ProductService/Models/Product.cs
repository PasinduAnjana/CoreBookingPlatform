namespace ProductService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? AttributesJson { get; set; }
        public string? Location { get; set; }
        public int? CategoryId { get; set; }
        public Category? Category { get; set; }
        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public string SupplierProductId { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;

        public ProductContent? Content { get; set; }
        public DateTimeOffset? LastUpdated { get; set; }
    }
}
