using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using OrderService.Data;
using OrderService.Models;
using ProductService.Data;

static class MigrationChecks
{
    public static async Task Run(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var products = scope.ServiceProvider.GetRequiredService<ProductDbContext>();
        var orders = scope.ServiceProvider.GetRequiredService<OrderDbContext>();
        var migrator = products.GetService<IMigrator>();
        await migrator.MigrateAsync("20261109102153_AddProductLastUpdated");
        await orders.Database.MigrateAsync();

        // Insert through the old schema before ProductType exists.
        await products.Database.ExecuteSqlRawAsync("""
            INSERT Categories (Name, Description) VALUES ('Kept category', 'Keep this');
            DECLARE @category int = SCOPE_IDENTITY();
            INSERT Products (Name, SupplierId, SupplierProductId, CategoryId, IsActive, AttributesJson, LastUpdated)
                VALUES ('Kept hotel', 1, 'legacy-hotel', @category, 0, '{{}}', '2026-11-01');
            DECLARE @product int = SCOPE_IDENTITY();
            INSERT ProductContents (ProductId, Description, ImageJson, LastUpdated)
                VALUES (@product, 'Kept content', '[]', '2026-11-01');
            INSERT Products (Name, SupplierId, SupplierProductId, IsActive, AttributesJson)
                VALUES ('Kept activity', 2, 'legacy-activity', 1, '{{}}');
            INSERT Suppliers (Name, Code, IsActive) VALUES ('Unknown legacy supplier', 'LEGACY', 1);
            DECLARE @supplier int = SCOPE_IDENTITY();
            INSERT Products (Name, SupplierId, SupplierProductId, IsActive, AttributesJson)
                VALUES ('Unknown legacy product', @supplier, 'legacy-unknown', 1, '{{}}');
            """);

        var legacyHotelId = await products.Database.SqlQueryRaw<int>("SELECT Id AS Value FROM Products WHERE SupplierProductId = 'legacy-hotel'").SingleAsync();
        var cart = new Cart();
        cart.Items.Add(new CartItem { ProductId = legacyHotelId, SupplierOptionId = "STD", Adults = 1, Rooms = 1 });
        orders.Carts.Add(cart);
        var order = new Order { CartId = cart.Id, CustomerName = "Kept customer", Status = OrderStatus.Cancelled };
        order.Bookings.Add(new Booking { ProductId = legacyHotelId, SupplierCode = "ABC", SupplierProductId = "legacy-hotel",
            SupplierOptionId = "STD", SupplierBookingId = "kept-reference", IdempotencyKey = "kept-key", Status = BookingStatus.Cancelled });
        orders.Orders.Add(order);
        await orders.SaveChangesAsync();

        try
        {
            await migrator.MigrateAsync();
            throw new Exception("Unknown legacy suppliers must not be silently assigned a product type.");
        }
        catch (SqlException ex) when (ex.Number == 50001) { }

        await using (var sql = new SqlConnection(products.Database.GetConnectionString()))
        {
            await sql.OpenAsync();
            await using var command = new SqlCommand("SELECT COL_LENGTH('Products', 'ProductType')", sql);
            if (await command.ExecuteScalarAsync() != DBNull.Value)
                throw new Exception("Failed migration did not roll back the schema change.");
        }
        Console.WriteLine("PASS migration refuses unknown legacy types and rolls back safely");

        await products.Database.ExecuteSqlRawAsync("""
            DELETE FROM Products WHERE SupplierProductId = 'legacy-unknown';
            DELETE FROM Suppliers WHERE Code = 'LEGACY';
            """);
        await migrator.MigrateAsync();
        var hotel = await products.Products.Include(p => p.Content).Include(p => p.Category)
            .SingleAsync(p => p.SupplierProductId == "legacy-hotel");
        var activity = await products.Products.SingleAsync(p => p.SupplierProductId == "legacy-activity");
        if (hotel.ProductType != "Hotel" || activity.ProductType != "Activity" || hotel.IsActive ||
            hotel.Name != "Kept hotel" || hotel.Content?.Description != "Kept content" || hotel.Category?.Name != "Kept category")
            throw new Exception("Migration changed existing product data.");



        // A second update is a no-op and leaves identity and order references intact.
        await migrator.MigrateAsync();
        products.ChangeTracker.Clear();
        if (await products.Products.CountAsync() != 2 ||
            (await products.Products.FindAsync(hotel.Id))?.SupplierId != 1 ||
            (await orders.Orders.Include(o => o.Bookings).SingleAsync()).Bookings.Single().SupplierBookingId != "kept-reference")
            throw new Exception("Migration repeat changed IDs or booking references.");
        Console.WriteLine("PASS Hotel/Activity backfill, content, category, active state, IDs and existing references preserved");
    }
}
