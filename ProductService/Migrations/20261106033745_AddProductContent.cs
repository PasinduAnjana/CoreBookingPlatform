using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace ProductService.Migrations
{
    /// <inheritdoc />
    public partial class AddProductContent : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ProductContents",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImageJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    HighlightsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    FacilitiesJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    InclusionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ExclusionsJson = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    AdditionalInfoJson = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProductContents", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProductContents_Products_ProductId",
                        column: x => x.ProductId,
                        principalTable: "Products",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ProductContents_ProductId",
                table: "ProductContents",
                column: "ProductId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ProductContents");
        }
    }
}
