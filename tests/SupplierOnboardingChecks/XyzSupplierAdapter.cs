using SupplierContracts;
using IntegrationService.Adapters;
using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Common;

// A third hotel supplier with the same external API contract as ABC.
// Only this adapter and its test DI registration know about XYZ.
public sealed class XyzSupplierAdapter(AbcSupplierAdapter hotels) : ISupplierAdapter
{
    public string SupplierCode => "XYZ";
    public string SupplierName => "XYZ Hotels";
    public bool RejectNextBooking { get; set; }
    public bool LoseNextBookingResponse { get; set; }
    public bool FailNextCancellation { get; set; }
    public int BookingCalls { get; private set; }

    public async Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default)
    {
        var products = await hotels.GetProductsAsync(cancellationToken);
        foreach (var product in products)
        {
            product.SupplierCode = SupplierCode;
            product.Name = "XYZ " + product.Name;
        }
        return products;
    }

    public async Task<NormalizedAvailabilityResponse> CheckAvailabilityAsync(NormalizedAvailabilityRequest request, CancellationToken cancellationToken = default)
    {
        var result = await hotels.CheckAvailabilityAsync(request, cancellationToken);
        result.SupplierCode = SupplierCode;
        return result;
    }

    public async Task<NormalizedBookingResponse> CreateBookingAsync(NormalizedBookingRequest request, CancellationToken cancellationToken = default)
    {
        BookingCalls++;
        if (RejectNextBooking)
        {
            RejectNextBooking = false;
            throw new SupplierOperationException(SupplierError.Create(SupplierErrorCode.Conflict, SupplierErrorOutcome.Rejected));
        }
        if (LoseNextBookingResponse)
        {
            LoseNextBookingResponse = false;
            throw new HttpRequestException("Test supplier connection lost.");
        }
        var result = await hotels.CreateBookingAsync(request, cancellationToken);
        result.SupplierCode = SupplierCode;
        return result;
    }

    public async Task<NormalizedCancellationResponse> CancelBookingAsync(NormalizedCancellationRequest request, CancellationToken cancellationToken = default)
    {
        if (FailNextCancellation)
        {
            FailNextCancellation = false;
            throw new HttpRequestException("Test cancellation connection lost.");
        }
        var result = await hotels.CancelBookingAsync(request, cancellationToken);
        result.SupplierCode = SupplierCode;
        return result;
    }
}
