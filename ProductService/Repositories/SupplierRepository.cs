using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using Microsoft.Data.SqlClient;
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

        public async Task<Supplier> GetOrAddAsync(string code, string name)
        {
            var existing = await GetSupplierByCodeAsync(code);
            if (existing != null) return existing;

            var supplier = new Supplier { Code = code, Name = name, IsActive = true };
            _context.Suppliers.Add(supplier);
            try
            {
                await _context.SaveChangesAsync();
                return supplier;
            }
            catch (DbUpdateException ex) when (ex.InnerException is SqlException { Number: 2601 or 2627 })
            {
                // Another catalog sync registered the same code first.
                _context.Entry(supplier).State = EntityState.Detached;
                var registered = await GetSupplierByCodeAsync(code);
                if (registered == null) throw;
                return registered;
            }
        }
         
    }
}
