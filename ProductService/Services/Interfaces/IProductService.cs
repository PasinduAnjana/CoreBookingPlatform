using ProductService.DTOs;

namespace ProductService.Services.Interfaces
{
    public interface IProductService
    {
        Task<List<ProductDto>> GetAllProductsAsync();
        Task<ProductDto?> GetProductByIdAsync(int id);
        Task<List<ProductDto>> GetProductsBySupplierIdAsync(int supplierId);
        Task<ProductDto?> GetSupplierReferenceAsync(int supplierId, string supplierProductId);
        Task<bool> UpdateProductAsync(int id, UpdateProductDto dto);
    }
}
