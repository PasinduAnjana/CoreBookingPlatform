using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using ProductService.Repositories.Interfaces;

namespace ProductService.Repositories
{
    public class ProductImportRepository : IProductImportRepository
    {
        private readonly ProductDbContext _context;
        public ProductImportRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<Supplier?> GetSupplierByCodeAsync(string code)
        {
            return await _context.Suppliers
                .FirstOrDefaultAsync(s => s.Code == code);
        }

        public async Task<Product?> GetProductBySupplierReferenceAsync(
            int supplierId,
            string supplierProductId)
        {
            return await _context.Products
                .Include(p => p.Content)
                .FirstOrDefaultAsync(p =>
                    p.SupplierId == supplierId &&
                    p.SupplierProductId == supplierProductId);
        }

        public async Task AddProductAsync(Product product)
        {
            await _context.Products.AddAsync(product);
        }

        public async Task<int> SaveChangesAsync()
        {
            return await _context.SaveChangesAsync();
        }
    }
}
