using System.Text.Json;
using OrderService.DTOs;
using SupplierContracts;
using static OrderService.Dtos.ExternalServiceDtos;

namespace OrderService.Clients;

public class IntegrationServiceClient(HttpClient httpClient)
{
    public async Task<AvailabilityResponseDto> CheckAvailabilityAsync(
        AvailabilityRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/integration/availability", request, cancellationToken);
        try
        {
            return await ReadAsync<AvailabilityResponseDto>(response, cancellationToken);
        }
        catch (SupplierOperationException ex) when (
            ex.Error.Code == SupplierErrorCode.InvalidRequest && ex.Error.Outcome == SupplierErrorOutcome.Rejected)
        {
            throw new ArgumentException(ex.Message, ex);
        }
    }

    public async Task<SupplierBookingResponseDto> CreateBookingAsync(
        SupplierBookingRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/integration/bookings", request, cancellationToken);
        return await ReadAsync<SupplierBookingResponseDto>(response, cancellationToken);
    }

    public async Task<SupplierCancellationResponseDto> CancelBookingAsync(
        SupplierCancellationRequestDto request, CancellationToken cancellationToken = default)
    {
        using var response = await httpClient.PostAsJsonAsync(
            "api/integration/bookings/cancel", request, cancellationToken);
        return await ReadAsync<SupplierCancellationResponseDto>(response, cancellationToken);
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        if (!response.IsSuccessStatusCode)
        {
            SupplierError? error = null;
            try
            {
                error = await response.Content.ReadFromJsonAsync<SupplierError>(cancellationToken: cancellationToken);
            }
            catch (Exception ex) when (ex is JsonException or NotSupportedException)
            {
                // A proxy/legacy error body cannot establish the supplier's outcome.
            }

            var code = error is not null && Enum.IsDefined(error.Code) && error.Code != SupplierErrorCode.Unknown
                ? error.Code : SupplierErrorCode.SupplierFailure;
            var outcome = error?.Outcome == SupplierErrorOutcome.Rejected &&
                code is SupplierErrorCode.InvalidRequest or SupplierErrorCode.NotFound or SupplierErrorCode.Conflict
                ? SupplierErrorOutcome.Rejected : SupplierErrorOutcome.Unknown;
            throw new SupplierOperationException(SupplierError.Create(code, outcome), response.StatusCode);
        }

        try
        {
            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new SupplierOperationException(
                    SupplierError.Create(SupplierErrorCode.InvalidResponse), response.StatusCode);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new SupplierOperationException(
                SupplierError.Create(SupplierErrorCode.InvalidResponse), response.StatusCode);
        }
    }
}
