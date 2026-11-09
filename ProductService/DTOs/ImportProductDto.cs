using System.Text.Json;

namespace ProductService.DTOs
{
    public class ImportProductDto
    {
        public string SupplierCode { get; set; } = string.Empty;
        public string SupplierProductId { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string? Location { get; set; }
        public JsonElement Attributes { get; set; }
        public ImportProductContentDto Content { get; set; } = new();
    }

    public class ImportProductContentDto
    {
        public string? Description { get; set; }
        public List<string> Images { get; set; } = [];
        public List<string> Highlights { get; set; } = [];
        public List<string> Facilities { get; set; } = [];
        public List<string> Inclusions { get; set; } = [];
        public List<string> Exclusions { get; set; } = [];
    }
}
