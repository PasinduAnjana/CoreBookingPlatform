using ProductService.Models;

namespace ProductService.Repositories.Interfaces
{
    public interface IProductRepository
    {
        Task<List<Product>> GetAllProductsAsync();

        Task<Product?> GetProductByIdAsync(int id);

        Task<List<Product>> GetProductsBySupplierIdAsync(
            int supplierId);

        Task<Product?> GetProductForUpdateAsync(int id);

        Task<Product?> GetSupplierReferenceAsync(
            int supplierId,
            string supplierProductId);

        Task<Product> CreateProductAsync(Product product);

        Task<Product> UpdateProductAsync(Product product);

        Task<bool> CategoryExistsAsync(int categoryId);
    }
}
