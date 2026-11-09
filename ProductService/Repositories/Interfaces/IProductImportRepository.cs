using ProductService.Models;

namespace ProductService.Repositories.Interfaces
{
    public interface IProductImportRepository
    {
        Task<Supplier?> GetSupplierByCodeAsync(string code);

        Task<Product?> GetProductBySupplierReferenceAsync(
            int supplierId,
            string supplierProductId);

        Task AddProductAsync(Product product);

        Task<int> SaveChangesAsync();
    }
}
