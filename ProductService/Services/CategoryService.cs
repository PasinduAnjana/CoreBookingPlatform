using Microsoft.EntityFrameworkCore;
using ProductService.Data;
using ProductService.DTOs;
using ProductService.Models;

namespace ProductService.Services
{
    public class CategoryService
    {
        private readonly ProductDbContext _context;

        public CategoryService(ProductDbContext context)
        {
            _context = context;
        }

        private static CategoryDto toDto(Category category)
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
            var categories = await _context.Categories.ToListAsync();
            return categories.Select(toDto).ToList();
        }

        public async Task<CategoryDto?> GetCategoryByIdAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            return category == null ? null : toDto(category);
        }

        public async Task<CategoryDto> CreateCategoryAsync(CreateCategoryDto createCategoryDto)
        {
            var category = new Category
            {
                Name = createCategoryDto.Name,
                Description = createCategoryDto.Description
            };
            _context.Categories.Add(category);
            await _context.SaveChangesAsync();
            return toDto(category);
        }

        public async Task<CategoryDto?> UpdateCategoryAsync(int id, UpdateCategoryDto updateCategoryDto)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return null;
            }
            category.Name = updateCategoryDto.Name;
            category.Description = updateCategoryDto.Description;
            await _context.SaveChangesAsync();
            return toDto(category);
        }

        public async Task<bool> DeleteCategoryAsync(int id)
        {
            var category = await _context.Categories.FindAsync(id);
            if (category == null)
            {
                return false;
            }
            _context.Categories.Remove(category);
            await _context.SaveChangesAsync();
            return true;
        }


    }
}
