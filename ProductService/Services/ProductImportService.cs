using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;
using System.Text.Json;

namespace ProductService.Services
{
    public class ProductImportService : IProductImportService
    {
        private readonly IProductImportRepository _repository;
        public ProductImportService(IProductImportRepository repository)
        {
            _repository = repository;
        }

        public async Task<ImportProductResultDto> ImportAsync(ImportProductDto dto)
        {
            if (string.IsNullOrWhiteSpace(dto.SupplierCode) ||
                string.IsNullOrWhiteSpace(dto.SupplierProductId) ||
                string.IsNullOrWhiteSpace(dto.Name))
            {
                throw new ArgumentException(
                    "Supplier code, product ID and name are required.");
            }

            if (dto.Attributes.ValueKind != JsonValueKind.Object)
            {
                throw new ArgumentException(
                    "Attributes must be a JSON object.");
            }

            var supplier = await _repository
                .GetSupplierByCodeAsync(dto.SupplierCode);

            if (supplier == null)
            {
                throw new ArgumentException(
                    $"Supplier '{dto.SupplierCode}' not found.");
            }

            if (!supplier.IsActive)
            {
                throw new ArgumentException(
                    $"Supplier '{dto.SupplierCode}' is inactive.");
            }

            var product = await _repository
                .GetProductBySupplierReferenceAsync(
                    supplier.Id,
                    dto.SupplierProductId);

            bool created = product == null;

            if (product == null)
            {
                product = new Product
                {
                    SupplierId = supplier.Id,
                    SupplierProductId = dto.SupplierProductId,
                    CategoryId = null,
                    IsActive = true
                };

                await _repository.AddProductAsync(product);
            }

            // Update supplier-managed catalog fields.
            product.Name = dto.Name;
            product.Location = dto.Location;
            product.AttributesJson = dto.Attributes.GetRawText();
            product.LastUpdated = DateTimeOffset.UtcNow;

            // Create content if it doesn't exist.
            if (product.Content == null)
            {
                product.Content = new ProductContent();
            }

            var content = product.Content;
            var source = dto.Content ?? new ImportProductContentDto();

            content.Description = source.Description;

            // Convert supplier image URLs to our content image structure.
            var images = source.Images.Select((url, index) =>
                new ProductImageDto
                {
                    Url = url,
                    Caption = null,
                    IsPrimary = index == 0
                }).ToList();

            content.ImageJson = JsonSerializer.Serialize(images);
            content.HighlightsJson =
                JsonSerializer.Serialize(source.Highlights);
            content.FacilitiesJson =
                JsonSerializer.Serialize(source.Facilities);
            content.InclusionsJson =
                JsonSerializer.Serialize(source.Inclusions);
            content.ExclusionsJson =
                JsonSerializer.Serialize(source.Exclusions);

            content.LastUpdated = DateTimeOffset.UtcNow;

            // Persist product and content together.
            await _repository.SaveChangesAsync();

            return new ImportProductResultDto
            {
                ProductId = product.Id,
                Created = created
            };
        }

    }
}
