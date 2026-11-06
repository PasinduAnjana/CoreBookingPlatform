using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using ProductService.Repositories.Interfaces;

namespace ProductService.Repositories
{
    public class ProductContentRepository:IProductContentRepository
    {
        private readonly ProductDbContext _context;

        public ProductContentRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<ProductContent?> GetByProductIdAsync(int productId)
        {
            return await _context.ProductContents
                .FirstOrDefaultAsync(pc => pc.ProductId == productId);
        }

        public async Task<bool> ProductExistsAsync(int productId)
        {
            return await _context.Products.AnyAsync(p => p.Id == productId);
        }

        public async Task AddAsync(ProductContent content)
        {
            await _context.ProductContents.AddAsync(content);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public void Delete(ProductContent content)
        {
            _context.ProductContents.Remove(content);
        }

    }
}
