using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace OrderService.Migrations
{
    /// <inheritdoc />
    public partial class AddOrdersAndBookings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Orders",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CustomerName = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Orders", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Bookings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    OrderId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CartItemId = table.Column<int>(type: "int", nullable: false),
                    ProductId = table.Column<int>(type: "int", nullable: false),
                    SupplierCode = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    SupplierProductId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    SupplierOptionId = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    IdempotencyKey = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: false),
                    SupplierBookingId = table.Column<string>(type: "nvarchar(150)", maxLength: 150, nullable: true),
                    CheckIn = table.Column<DateOnly>(type: "date", nullable: true),
                    CheckOut = table.Column<DateOnly>(type: "date", nullable: true),
                    ActivityDate = table.Column<DateOnly>(type: "date", nullable: true),
                    TimeSlot = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    Adults = table.Column<int>(type: "int", nullable: false),
                    Children = table.Column<int>(type: "int", nullable: false),
                    Rooms = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(40)", maxLength: 40, nullable: false),
                    TotalPrice = table.Column<decimal>(type: "decimal(18,2)", precision: 18, scale: 2, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    ErrorMessage = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Bookings", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Bookings_Orders_OrderId",
                        column: x => x.OrderId,
                        principalTable: "Orders",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_IdempotencyKey",
                table: "Bookings",
                column: "IdempotencyKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Bookings_OrderId_CartItemId",
                table: "Bookings",
                columns: new[] { "OrderId", "CartItemId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Orders_CartId",
                table: "Orders",
                column: "CartId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Bookings");

            migrationBuilder.DropTable(
                name: "Orders");
        }
    }
}
