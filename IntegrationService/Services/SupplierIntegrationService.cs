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
    }
}
