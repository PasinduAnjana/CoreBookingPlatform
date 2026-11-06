using ProductService.DTOs;

namespace ProductService.Services.Interfaces
{
    public interface ICategoryService
    {
        Task<List<CategoryDto>> GetAllCategoriesAsync();
        Task<CategoryDto> GetCategoryByIdAsync(int id);
        Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto category);
        Task<CategoryDto> UpdateCategoryAsync(int id, UpdateCategoryDto category);
        Task<bool> DeleteCategoryAsync(int id);
    }
}
