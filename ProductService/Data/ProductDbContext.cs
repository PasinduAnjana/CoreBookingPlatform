using Microsoft.EntityFrameworkCore;

namespace ProductService.Data
{
    public class ProductDbContext:DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }
        public DbSet<ProductService.Models.Category> Categories { get; set; } = null!;
    }
}
