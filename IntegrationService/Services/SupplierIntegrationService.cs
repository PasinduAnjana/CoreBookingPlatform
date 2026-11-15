using IntegrationService.Models.Common;

namespace IntegrationService.Services
{
    public class SupplierIntegrationService
    {
        private readonly SupplierAdapterRegistry _registry;
        public SupplierIntegrationService(SupplierAdapterRegistry registry) => _registry = registry;

        public Task<List<NormalizedProduct>> GetProductsAsync(string supplierCode, CancellationToken cancellationToken = default)
            => _registry.GetAdapter(supplierCode).GetProductsAsync(cancellationToken);

        public async Task<List<NormalizedProduct>> GetAllProductsAsync(CancellationToken cancellationToken = default)
        {
            var results = await Task.WhenAll(_registry.Adapters.Select(a => a.GetProductsAsync(cancellationToken)));
            return results.SelectMany(products => products).ToList();
        }

        public Task<NormalizedAvailabilityResponse> CheckAvailabilityAsync(NormalizedAvailabilityRequest request, CancellationToken cancellationToken = default)
            => _registry.GetAdapter(request.SupplierCode).CheckAvailabilityAsync(request, cancellationToken);

        public Task<NormalizedBookingResponse> CreateBookingAsync(NormalizedBookingRequest request, CancellationToken cancellationToken = default)
            => _registry.GetAdapter(request.SupplierCode).CreateBookingAsync(request, cancellationToken);

        public Task<NormalizedCancellationResponse> CancelBookingAsync(NormalizedCancellationRequest request, CancellationToken cancellationToken = default)
            => _registry.GetAdapter(request.SupplierCode).CancelBookingAsync(request, cancellationToken);
    }
}
