using IntegrationService.Models.Common;

namespace IntegrationService.Adapters.Interfaces
{
    public interface ISupplierAdapter
    {
        string SupplierCode { get; }
        Task<List<NormalizedProduct>> GetProductsAsync(CancellationToken cancellationToken = default);
    }
} 