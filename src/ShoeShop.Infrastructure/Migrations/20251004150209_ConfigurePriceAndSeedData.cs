using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace ShoeShop.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ConfigurePriceAndSeedData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.InsertData(
                table: "Shoes",
                columns: new[] { "Id", "Brand", "Name", "Price", "Stock" },
                values: new object[,]
                {
                    { 1, "Nike", "Air Max 90", 120.00m, 25 },
                    { 2, "Adidas", "Stan Smith", 80.00m, 30 },
                    { 3, "Converse", "Chuck Taylor All Star", 65.00m, 15 },
                    { 4, "Vans", "Old Skool", 75.00m, 20 },
                    { 5, "Nike", "Air Jordan 1", 170.00m, 12 }
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DeleteData(
                table: "Shoes",
                keyColumn: "Id",
                keyValue: 1);

            migrationBuilder.DeleteData(
                table: "Shoes",
                keyColumn: "Id",
                keyValue: 2);

            migrationBuilder.DeleteData(
                table: "Shoes",
                keyColumn: "Id",
                keyValue: 3);

            migrationBuilder.DeleteData(
                table: "Shoes",
                keyColumn: "Id",
                keyValue: 4);

            migrationBuilder.DeleteData(
                table: "Shoes",
                keyColumn: "Id",
                keyValue: 5);
        }
    }
}
