using Microsoft.EntityFrameworkCore;
using OrderService.Models;

namespace OrderService.Data
{
    public class OrderDbContext : DbContext
    {
        public OrderDbContext(DbContextOptions<OrderDbContext> options): base(options)
        {
        }

        public DbSet<Cart> Carts => Set<Cart>();
        public DbSet<CartItem> CartItems => Set<CartItem>();

        public DbSet<Order> Orders => Set<Order>();
        public DbSet<Booking> Bookings => Set<Booking>();


        protected override void OnModelCreating(
            ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<CartItem>()
                .HasOne(i => i.Cart)
                .WithMany(c => c.Items)
                .HasForeignKey(i => i.CartId)
                .OnDelete(DeleteBehavior.Cascade);

            modelBuilder.Entity<CartItem>()
                .Property(i => i.SupplierOptionId)
                .HasMaxLength(100)
                .IsRequired();

            modelBuilder.Entity<CartItem>()
                .Property(i => i.TimeSlot)
                .HasMaxLength(20);

            modelBuilder.Entity<Order>(entity =>
            {
                entity.HasKey(o => o.Id);

                entity.HasIndex(o => o.CartId)
                    .IsUnique();

                entity.Property(o => o.CustomerName)
                    .HasMaxLength(200)
                    .IsRequired();

                entity.Property(o => o.Status)
                    .HasMaxLength(40);

                entity.Property(o => o.TotalPrice)
                    .HasPrecision(18, 2);

                entity.Property(o => o.Currency)
                    .HasMaxLength(3);
            });

            modelBuilder.Entity<Booking>(entity =>
            {
                entity.HasKey(b => b.Id);

                entity.HasOne(b => b.Order)
                    .WithMany(o => o.Bookings)
                    .HasForeignKey(b => b.OrderId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasIndex(b => b.IdempotencyKey)
                    .IsUnique();

                entity.HasIndex(b => new
                {
                    b.OrderId,
                    b.CartItemId
                }).IsUnique();

                entity.Property(b => b.SupplierCode)
                    .HasMaxLength(20);

                entity.Property(b => b.SupplierBookingId)
                    .HasMaxLength(150);

                entity.Property(b => b.IdempotencyKey)
                    .HasMaxLength(150);

                entity.Property(b => b.Status)
                    .HasMaxLength(40);

                entity.Property(b => b.Currency)
                    .HasMaxLength(3);

                entity.Property(b => b.TotalPrice)
                    .HasPrecision(18, 2);
            });

        }
    }
}
