
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;

namespace ProductService.Services
{
    public class ProductService : IProductService
    {
        private readonly IProductRepository _repository;

        public ProductService(IProductRepository repository)
        {
            _repository = repository;
        }

        private static ProductDto ToDto(Product product)
        {
            return new ProductDto
            {
                Id = product.Id,
                Name = product.Name,

                CategoryId = product.CategoryId,

                Category = product.Category != null
                    ? new CategoryDto
                    {
                        Id = product.Category.Id,
                        Name = product.Category.Name,
                        Description = product.Category.Description
                    }
                    : null,

                AttributesJson = product.AttributesJson,
                Location = product.Location,

                SupplierId = product.SupplierId,

                Supplier = new SupplierDto
                {
                    Id = product.Supplier.Id,
                    Name = product.Supplier.Name
                },

                SupplierProductId = product.SupplierProductId,
                IsActive = product.IsActive,
                LastUpdated = product.LastUpdated
            };
        }

        public async Task<List<ProductDto>> GetAllProductsAsync()
        {
            var products = await _repository.GetAllProductsAsync();

            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto?> GetProductByIdAsync(int id)
        {
            var product = await _repository.GetProductByIdAsync(id);

            return product == null ? null : ToDto(product);
        }

        public async Task<List<ProductDto>> GetProductsBySupplierIdAsync(
            int supplierId)
        {
            var products = await _repository
                .GetProductsBySupplierIdAsync(supplierId);

            return products.Select(ToDto).ToList();
        }

        public async Task<ProductDto?> GetSupplierReferenceAsync(
            int supplierId,
            string supplierProductId)
        {
            var product = await _repository.GetSupplierReferenceAsync(
                supplierId,
                supplierProductId
            );

            return product == null ? null : ToDto(product);
        }


        public async Task<bool> UpdateProductAsync(
            int id,
            UpdateProductDto dto)
        {
            var existingProduct =
                await _repository.GetProductForUpdateAsync(id);

            if (existingProduct == null)
                return false;

            // Validate category before updating
            if (dto.CategoryId.HasValue)
            {
                bool categoryExists =
                    await _repository.CategoryExistsAsync(
                        dto.CategoryId.Value);

                if (!categoryExists)
                {
                    throw new ArgumentException(
                        "Category does not exist.");
                }
            }

            existingProduct.CategoryId = dto.CategoryId;
            existingProduct.IsActive = dto.IsActive;
            existingProduct.LastUpdated = DateTimeOffset.UtcNow;

            await _repository.UpdateProductAsync(existingProduct);

            return true;
        }
    }
}
