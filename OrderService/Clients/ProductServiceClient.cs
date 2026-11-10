using System.Net;
using static OrderService.Dtos.ExternalServiceDtos;

namespace OrderService.Clients
{
    public class ProductServiceClient
    {
        private readonly HttpClient _httpClient;

        public ProductServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ProductLookupDto?> GetProductAsync(int productId,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.GetAsync(
                $"api/product/{productId}",
                cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<ProductLookupDto>(
                    cancellationToken: cancellationToken);
        }
    }
}
