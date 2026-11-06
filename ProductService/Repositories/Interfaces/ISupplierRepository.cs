using ProductService.Models;

namespace ProductService.Repositories.Interfaces
{
    public interface ISupplierRepository
    {
        Task<List<Supplier>> GetAllSuppliersAsync();
        Task<Supplier?> GetSupplierByIdAsync(int id);
        Task<Supplier?> GetSupplierByCodeAsync(string code);
    }
}
