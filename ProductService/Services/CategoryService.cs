using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;
using ProductService.Repositories.Interfaces;
using ProductService.Services.Interfaces;

namespace ProductService.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly ICategoryRepository _repository;

        public CategoryService(ICategoryRepository repository)
        {
            _repository = repository;
        }

        private CategoryDto toDto(Category category)
        {
            return new CategoryDto
            {
                Id = category.Id,
                Name = category.Name,
                Description = category.Description
            };
        }   

        public async Task<List<CategoryDto>> GetAllCategoriesAsync()
        {
            var categories = await _repository.GetAllCategoriesAsync();
            return categories.Select(c => toDto(c)).ToList();
        }

        public async Task<CategoryDto> GetCategoryByIdAsync(int id)
        {
            var category = await _repository.GetCategoryByIdAsync(id);
            if (category == null)
            {
                return null;
            }
            return toDto(category);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            var category = new Category
            {
                Name = createCategoryDto.Name,
                Description = createCategoryDto.Description
            };
            var createdCategory = await _repository.CreateCategoryAsync(category);
            return toDto(createdCategory);
        }

        public async Task<CategoryDto> UpdateCategoryAsync(int id,UpdateCategoryDto updateCategoryDto)
        {
            var existingCategory = await _repository.GetCategoryByIdAsync(id);
            if (existingCategory == null)
            {
                return null;
            }
            existingCategory.Name = updateCategoryDto.Name;
            existingCategory.Description = updateCategoryDto.Description;
            var updatedCategory = await _repository.UpdateCategoryAsync(existingCategory);
            return toDto(updatedCategory);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            return await _repository.DeleteCategoryAsync(id);
        }


    }
}
