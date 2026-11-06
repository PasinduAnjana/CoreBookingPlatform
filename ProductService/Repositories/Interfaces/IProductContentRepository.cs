using ProductService.Models;

namespace ProductService.Repositories.Interfaces
{
    public interface IProductContentRepository
    {
        Task<ProductContent?> GetByProductIdAsync(int productId);
        Task<bool> ProductExistsAsync(int productId);
        Task AddAsync(ProductContent content);
        Task<int> SaveChangesAsync();
        void Delete(ProductContent content);
    }
}
