using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Migrations
{
    /// <inheritdoc />
    public partial class UpdateProductModel : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Price",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "supplierProductId",
                table: "Products",
                newName: "SupplierProductId");

            migrationBuilder.RenameColumn(
                name: "attributes",
                table: "Products",
                newName: "Location");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SupplierId_supplierProductId",
                table: "Products",
                newName: "IX_Products_SupplierId_SupplierProductId");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "int",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "int");

            migrationBuilder.AddColumn<string>(
                name: "AttributesJson",
                table: "Products",
                type: "nvarchar(max)",
                nullable: true);

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Products",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "Products",
                type: "datetimeoffset",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AttributesJson",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Products");

            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "Products");

            migrationBuilder.RenameColumn(
                name: "SupplierProductId",
                table: "Products",
                newName: "supplierProductId");

            migrationBuilder.RenameColumn(
                name: "Location",
                table: "Products",
                newName: "attributes");

            migrationBuilder.RenameIndex(
                name: "IX_Products_SupplierId_SupplierProductId",
                table: "Products",
                newName: "IX_Products_SupplierId_supplierProductId");

            migrationBuilder.AlterColumn<int>(
                name: "CategoryId",
                table: "Products",
                type: "int",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "int",
                oldNullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Price",
                table: "Products",
                type: "decimal(18,2)",
                nullable: false,
                defaultValue: 0m);
        }
    }
}
