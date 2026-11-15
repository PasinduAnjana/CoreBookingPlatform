using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Migrations
{
    /// <inheritdoc />
    public partial class AddProductType : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: true);

            // One-time backfill for products imported before types were explicit.
            // No supplier IDs, product IDs, content, carts or bookings are replaced.
            migrationBuilder.Sql("""
                UPDATE p SET ProductType = CASE s.Code
                    WHEN 'ABC' THEN 'Hotel'
                    WHEN 'CDE' THEN 'Activity'
                END
                FROM Products p INNER JOIN Suppliers s ON s.Id = p.SupplierId;

                IF EXISTS (SELECT 1 FROM Products WHERE ProductType IS NULL)
                    THROW 50001, 'ProductType backfill requires review: an existing product has an unknown legacy supplier.', 1;
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ProductType",
                table: "Products",
                type: "nvarchar(20)",
                maxLength: 20,
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(20)",
                oldMaxLength: 20,
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProductType",
                table: "Products");
        }
    }
}
