using IntegrationService.Models.Common;

namespace IntegrationService.Services
{
    public class ProductServiceClient
    {
        private readonly HttpClient _httpClient;

        public ProductServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ImportResult?> ImportProductAsync(
            NormalizedProduct product,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/internal/products/import",
                product,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content.ReadFromJsonAsync<ImportResult>(cancellationToken: cancellationToken);
        }
    }

    public class ImportResult
    {
        public int ProductId { get; set; }
        public bool Created { get; set; }
    }
}

