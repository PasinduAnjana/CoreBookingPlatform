using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;
using System.Text.Json;

namespace ProductService.Services
{
    public class ProductContentService : IProductContentService
    {
        private readonly IProductContentRepository _repository;

        public ProductContentService(IProductContentRepository productContentRepository)
        {
            _repository = productContentRepository;
        }

        private static string Serialize<T>(T value)
        {
            return JsonSerializer.Serialize(value);
        }

        private static List<T> DeserializeList<T>(string json)
        {
            return string.IsNullOrWhiteSpace(json) ? new List<T>() : JsonSerializer.Deserialize<List<T>>(json) ?? new List<T>();
        }
        private static ProductContentDto toDto(ProductContent content)
        {
            return new ProductContentDto
            {
                ProductId = content.ProductId,
                Description = content.Description,

                Images = DeserializeList<ProductImageDto>(
                    content.ImageJson),

                Highlights = DeserializeList<string>(
                    content.HighlightsJson),

                Facilities = DeserializeList<string>(
                    content.FacilitiesJson),

                Inclusions = DeserializeList<string>(
                    content.InclusionsJson),

                Exclusions = DeserializeList<string>(
                    content.ExclusionsJson),

                AdditionalInfoJson = content.AdditionalInfoJson
            };
        }

        public async Task<ProductContentDto?> GetContentAsync(int productId)
        {
            var content = await _repository.GetByProductIdAsync(productId);
            if (content == null)
            {
                return null;
            }
            return toDto(content);
        }

        public async Task<bool> UpsertContentAsync(
            int productId,
            UpdateProductContentDto dto)
        {
            if (!await _repository.ProductExistsAsync(productId))
                return false;

            var content = await _repository
                .GetByProductIdAsync(productId);

            if (content == null)
            {
                content = new ProductContent
                {
                    ProductId = productId
                };

                await _repository.AddAsync(content);
            }

            content.Description = dto.Description;

            content.ImageJson = Serialize(dto.Images);
            content.HighlightsJson = Serialize(dto.Highlights);
            content.FacilitiesJson = Serialize(dto.Facilities);
            content.InclusionsJson = Serialize(dto.Inclusions);
            content.ExclusionsJson = Serialize(dto.Exclusions);
            content.AdditionalInfoJson = dto.AdditionalInfoJson;


            await _repository.SaveChangesAsync();

            return true;
        }

        public async Task<bool> DeleteContentAsync(int productId)
        {
            var content = await _repository
                .GetByProductIdAsync(productId);

            if (content == null)
                return false;

            _repository.Delete(content);
            await _repository.SaveChangesAsync();

            return true;
        }

    }
}
