using IntegrationService.Models.Common;

namespace IntegrationService.Adapters.Interfaces
{
    public interface ISupplierAdapter
    {
        string SupplierCode { get; }
        Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default);

        Task<NormalizedAvailabilityResponse> CheckAvailabilityAsync(NormalizedAvailabilityRequest request,CancellationToken cancellationToken = default);
    }
} 