namespace ProductService.DTOs
{

    public class ProductImageDto
    {
        public string Url { get; set; } = string.Empty;
        public string? Caption { get; set; }
        public bool IsPrimary { get; set; }
    }

    public class ProductContentDto
    {
        public int ProductId { get; set; }

        public string? Description { get; set; }
        public List<ProductImageDto> Images { get; set; } = [];
        public List<string> Highlights { get; set; } = [];
        public List<string> Facilities { get; set; } = [];
        public List<string> Inclusions { get; set; } = [];
        public List<string> Exclusions { get; set; } = [];
        public string? AdditionalInfoJson { get; set; }
    }

    public class UpdateProductContentDto
    {
        public string? Description { get; set; }
        public List<ProductImageDto> Images { get; set; } = [];
        public List<string> Highlights { get; set; } = [];
        public List<string> Facilities { get; set; } = [];
        public List<string> Inclusions { get; set; } = [];
        public List<string> Exclusions { get; set; } = [];
        public string? AdditionalInfoJson { get; set; }
    }
}
