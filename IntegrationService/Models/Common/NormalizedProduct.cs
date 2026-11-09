using System.Text.Json;

namespace IntegrationService.Models.Common
{
    public class NormalizedProduct
    {
        public string SupplierCode { get; set; } = string.Empty;

        public string SupplierProductId { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string? Location { get; set; }

        public JsonElement Attributes { get; set; }

        public NormalizedProductContent Content { get; set; } = new();
    }

    public class NormalizedProductContent
    {
        public string? Description { get; set; }

        public List<string> Images { get; set; } = [];

        public List<string> Highlights { get; set; } = [];

        public List<string> Facilities { get; set; } = [];

        public List<string> Inclusions { get; set; } = [];

        public List<string> Exclusions { get; set; } = [];
    }
}
