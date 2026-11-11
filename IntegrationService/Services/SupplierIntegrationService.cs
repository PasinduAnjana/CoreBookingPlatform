using IntegrationService.Adapters.Interfaces;
using IntegrationService.Models.Common;

namespace IntegrationService.Services
{
    public class SupplierIntegrationService
    {
        private readonly IEnumerable<ISupplierAdapter> _adapters;
        public SupplierIntegrationService(IEnumerable<ISupplierAdapter> adapters)
        {
            _adapters = adapters;
        }
        
        public async Task<List<NormalizedProduct>> GetProductsAsync(string supplierCode,CancellationToken cancellationToken = default)
        {
            var adapter = _adapters.FirstOrDefault(a => a.SupplierCode.Equals(supplierCode,StringComparison.OrdinalIgnoreCase));
            if (adapter == null)
            {
                throw new KeyNotFoundException($"Supplier {supplierCode} not found");
            }
            return await adapter.GetProductsAsync(cancellationToken);
        }

        public async Task<List<NormalizedProduct>> GetAllProductsAsync(
            CancellationToken cancellationToken = default)
        {
            var results = await Task.WhenAll(
                _adapters.Select(adapter => adapter.GetProductsAsync(cancellationToken)));

            return results.SelectMany(products => products).ToList();
        }


        public async Task<NormalizedAvailabilityResponse> CheckAvailabilityAsync(
            NormalizedAvailabilityRequest request,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(request.SupplierCode))
            {
                throw new ArgumentException(
                    "Supplier code is required.");
            }

            var adapter = _adapters.FirstOrDefault(a =>
                a.SupplierCode.Equals(
                    request.SupplierCode,
                    StringComparison.OrdinalIgnoreCase));

            if (adapter == null)
            {
                throw new KeyNotFoundException(
                    $"Supplier '{request.SupplierCode}' is not supported.");
            }

            return await adapter.CheckAvailabilityAsync(
                request,
                cancellationToken);
        }


        private ISupplierAdapter GetAdapter(string supplierCode)
        {
            if (string.IsNullOrWhiteSpace(supplierCode))
                throw new ArgumentException("Supplier code is required.");

            return _adapters.FirstOrDefault(a =>
                a.SupplierCode.Equals(
                    supplierCode,
                    StringComparison.OrdinalIgnoreCase))
                ?? throw new KeyNotFoundException(
                    $"Supplier '{supplierCode}' is not supported.");
        }



        public async Task<NormalizedBookingResponse> CreateBookingAsync(
            NormalizedBookingRequest request,
            CancellationToken cancellationToken = default)
        {
            var adapter = GetAdapter(request.SupplierCode);

            return await adapter.CreateBookingAsync(
                request, cancellationToken);
        }

        public async Task<NormalizedCancellationResponse> CancelBookingAsync(
            NormalizedCancellationRequest request,
            CancellationToken cancellationToken = default)
        {
            var adapter = GetAdapter(request.SupplierCode);

            return await adapter.CancelBookingAsync(
                request, cancellationToken);
        }


    }
}
