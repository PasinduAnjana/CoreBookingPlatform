namespace ProductService.Models
{
    public class ProductContent
    {
        public int Id { get; set; }
        public int ProductId { get; set; }
        public Product Product { get; set; } = null!;
        public string? Description { get; set; }
        public string? ImageJson { get; set; }
        public string? HighlightsJson { get; set; }
        public string? FacilitiesJson { get; set; }
        public string? InclusionsJson { get; set; }
        public string? ExclusionsJson { get; set; }
        public string? AdditionalInfoJson { get; set; }
        public DateTimeOffset LastUpdated { get; set; }

    }
}
