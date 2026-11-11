
using System.Net.Http.Json;

namespace IntegrationService.Adapters
{
    public static class SupplierResponseReader
    {
        public static async Task<T> ReadAsync<T>(
            HttpResponseMessage response,
            CancellationToken cancellationToken = default)
        {
            if (!response.IsSuccessStatusCode)
            {
                var message = await response.Content
                    .ReadAsStringAsync(cancellationToken);

                throw new SupplierApiException(
                    response.StatusCode,
                    string.IsNullOrWhiteSpace(message)
                        ? "Supplier rejected the request."
                        : message);
            }

            var result = await response.Content
                .ReadFromJsonAsync<T>(
                    cancellationToken: cancellationToken);

            return result ?? throw new InvalidOperationException(
                "Supplier returned an empty response.");
        }
    }
}
