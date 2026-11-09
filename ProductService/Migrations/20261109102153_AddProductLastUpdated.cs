using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Migrations
{
    /// <inheritdoc />
    public partial class AddProductLastUpdated : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.RenameColumn(
                name: "description",
                table: "ProductContents",
                newName: "Description");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdated",
                table: "ProductContents",
                type: "datetimeoffset",
                nullable: false,
                defaultValue: new DateTimeOffset(new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified), new TimeSpan(0, 0, 0, 0, 0)));
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LastUpdated",
                table: "ProductContents");

            migrationBuilder.RenameColumn(
                name: "Description",
                table: "ProductContents",
                newName: "description");
        }
    }
}
