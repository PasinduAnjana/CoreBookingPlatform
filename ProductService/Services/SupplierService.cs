using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;
using System.Text.RegularExpressions;

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
                Code = supplier.Code,
                IsActive = supplier.IsActive
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

        public async Task<SupplierDto> RegisterAsync(RegisterSupplierDto dto)
        {
            var code = (dto.Code ?? "").Trim().ToUpperInvariant();
            var name = (dto.Name ?? "").Trim();
            if (!Regex.IsMatch(code, "^[A-Z0-9][A-Z0-9_-]{0,49}$") || name.Length == 0 || name.Length > 200)
                throw new ArgumentException("A valid supplier code and name of at most 200 characters are required.");

            // Registration never renames or reactivates an existing supplier.
            var supplier = await _supplierRepository.GetOrAddAsync(code, name);
            if (!supplier.IsActive)
                throw new InvalidOperationException("This supplier is inactive.");
            return toDto(supplier);
        }

    }
}
