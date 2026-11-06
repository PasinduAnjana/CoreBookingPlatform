using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;

namespace ProductService.Services.Interfaces
{
    public interface IProductContentService
    {
        Task<ProductContentDto?> GetContentAsync(int productId);
        Task<bool> UpsertContentAsync(int productId, UpdateProductContentDto contentDto);
        Task<bool> DeleteContentAsync(int productId);
    }
}
