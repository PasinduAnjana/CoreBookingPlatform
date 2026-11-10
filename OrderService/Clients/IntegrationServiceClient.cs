using static OrderService.Dtos.ExternalServiceDtos;

namespace OrderService.Clients
{
    public class IntegrationServiceClient
    {
        private readonly HttpClient _httpClient;

        public IntegrationServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(
            AvailabilityRequestDto request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/integration/availability",
                request,
                cancellationToken);

            if (response.StatusCode == System.Net.HttpStatusCode.BadRequest)
            {
                var message = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new ArgumentException(message);
            }

            response.EnsureSuccessStatusCode();

            var result = await response.Content
                .ReadFromJsonAsync<AvailabilityResponseDto>(cancellationToken: cancellationToken);

            return result ?? throw new InvalidOperationException(
                "IntegrationService returned an empty response.");
        }
    }
}
