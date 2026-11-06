using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.Models;
using ProductService.Repositories.Interfaces;

namespace ProductService.Repositories
{
    public class SupplierRepository : ISupplierRepository
    {
        private readonly ProductDbContext _context;
        
        public SupplierRepository(ProductDbContext context)
        {
            _context = context;
        }

        public async Task<List<Supplier>> GetAllSuppliersAsync()
        {
            return await _context.Suppliers.AsNoTracking().OrderBy(s => s.Name).ToListAsync();
        }

        public async Task<Supplier?> GetSupplierByIdAsync(int id)
        {
            return await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Id == id);
        }

        public async Task<Supplier?> GetSupplierByCodeAsync(string code)
        {
            return await _context.Suppliers.AsNoTracking().FirstOrDefaultAsync(s => s.Code == code);
        }
         
    }
}
