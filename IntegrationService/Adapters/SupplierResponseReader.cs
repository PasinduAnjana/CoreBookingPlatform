using System.Net;
using System.Text.Json;
using SupplierContracts;

namespace IntegrationService.Adapters;

internal static class SupplierResponseReader
{
    public static async Task<T> SendAsync<T>(HttpClient client, HttpRequestMessage request,
        Func<HttpStatusCode, SupplierError> mapError, CancellationToken cancellationToken)
    {
        try
        {
            using var response = await client.SendAsync(request, cancellationToken);
            if (!response.IsSuccessStatusCode)
                throw new SupplierOperationException(mapError(response.StatusCode));

            return await response.Content.ReadFromJsonAsync<T>(cancellationToken: cancellationToken)
                ?? throw new SupplierOperationException(SupplierError.Create(SupplierErrorCode.InvalidResponse));
        }
        catch (HttpRequestException ex)
        {
            throw new SupplierOperationException(SupplierError.Create(SupplierErrorCode.Unreachable), ex);
        }
        catch (OperationCanceledException ex) when (!cancellationToken.IsCancellationRequested)
        {
            throw new SupplierOperationException(SupplierError.Create(SupplierErrorCode.Timeout), ex);
        }
        catch (Exception ex) when (ex is JsonException or NotSupportedException)
        {
            throw new SupplierOperationException(SupplierError.Create(SupplierErrorCode.InvalidResponse), ex);
        }
    }
}
