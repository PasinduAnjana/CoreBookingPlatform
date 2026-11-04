namespace ProductService.Models
{
    public class Product
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public decimal Price { get; set; }
        public int CategoryId { get; set; }
        public Category? Category { get; set; } = null!;

        public string? attributes { get; set; } 

        public int SupplierId { get; set; }
        public Supplier Supplier { get; set; } = null!;
        public string supplierProductId { get; set; } = string.Empty;

  

    }
}
