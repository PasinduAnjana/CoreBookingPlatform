using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;

namespace ProductService.Services
{
    public class SupplierService : ISupplierService
    {
        private readonly ISupplierRepository _supplierRepository;

        public SupplierService(ISupplierRepository supplierRepository)
        {
            _supplierRepository = supplierRepository;
        }

        private static SupplierDto toDto(Supplier supplier)
        {
            return new SupplierDto
            {
                Id = supplier.Id,
                Name = supplier.Name,
                Code = supplier.Code
            };
        }

        public async Task<List<SupplierDto>> GetAllSuppliersAsync()
        {
            var suppliers = await _supplierRepository.GetAllSuppliersAsync();
            return suppliers.Select(s => toDto(s)).ToList();
        }

        public async Task<SupplierDto?> GetSupplierByIdAsync(int id)
        {
            var supplier = await _supplierRepository.GetSupplierByIdAsync(id);
            if (supplier == null)
            {
                return null;
            }
            return toDto(supplier);
        }

        public async Task<SupplierDto?> GetSupplierByCodeAsync(string code)
        {
            var supplier = await _supplierRepository.GetSupplierByCodeAsync(code);
            if (supplier == null)
            {
                return null;
            }
            return toDto(supplier);
        }

    }
}
