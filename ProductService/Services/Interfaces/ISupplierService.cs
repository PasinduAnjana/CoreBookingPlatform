using ProductService.DTOs;

namespace ProductService.Services.Interfaces
{
    public interface ISupplierService
    {
        Task<List<SupplierDto>> GetAllSuppliersAsync();
        Task<SupplierDto?> GetSupplierByIdAsync(int id);
        Task<SupplierDto?> GetSupplierByCodeAsync(string code);
    }
}
