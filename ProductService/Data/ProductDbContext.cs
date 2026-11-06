using Microsoft.EntityFrameworkCore;
using ProductService.Models;

namespace ProductService.Data
{
    public class ProductDbContext:DbContext
    {
        public ProductDbContext(DbContextOptions<ProductDbContext> options) : base(options)
        {
        }
        public DbSet<Category> Categories { get; set; } = null!;
        public DbSet<Supplier> Suppliers { get; set; } = null!;
        public DbSet<Product> Products { get; set; } = null!;

        public DbSet<ProductContent> ProductContents { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            // Relationships and constraints
            modelBuilder.Entity<Supplier>()
                .HasIndex(s => s.Code)
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasIndex(p => new { p.SupplierId, p.SupplierProductId })
                .IsUnique();

            modelBuilder.Entity<Product>()
                .HasOne(p => p.Category)
                .WithMany()
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);
            modelBuilder.Entity<Product>()
                .HasOne(p => p.Supplier)
                .WithMany(s => s.Products)
                .HasForeignKey(p => p.SupplierId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<ProductContent>()
                .HasOne(pc => pc.Product)
                .WithOne(p => p.Content)
                .HasForeignKey<ProductContent>(pc => pc.ProductId)
                .OnDelete(DeleteBehavior.Cascade);

            //seed supplier data
            modelBuilder.Entity<Supplier>().HasData(
                new Supplier { Id = 1, Name = "ABC Hotels",Code = "ABC", IsActive = true },
                new Supplier { Id = 2, Name = "CDE Activities", Code = "CDE", IsActive = true }
            );

            
        }

    }
}
