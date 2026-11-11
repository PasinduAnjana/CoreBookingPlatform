using OrderService.DTOs;
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


        public async Task<SupplierBookingResponseDto> CreateBookingAsync(
            SupplierBookingRequestDto request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/integration/bookings",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<SupplierBookingResponseDto>(
                    cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    "Empty supplier booking response.");
        }

        public async Task<SupplierCancellationResponseDto> CancelBookingAsync(
            SupplierCancellationRequestDto request,
            CancellationToken cancellationToken = default)
        {
            using var response = await _httpClient.PostAsJsonAsync(
                "api/integration/bookings/cancel",
                request,
                cancellationToken);

            response.EnsureSuccessStatusCode();

            return await response.Content
                .ReadFromJsonAsync<SupplierCancellationResponseDto>(
                    cancellationToken: cancellationToken)
                ?? throw new InvalidOperationException(
                    "Empty supplier cancellation response.");
        }

    }
}
