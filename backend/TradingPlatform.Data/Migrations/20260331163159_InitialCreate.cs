using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

#pragma warning disable CA1814 // Prefer jagged arrays over multidimensional

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "MarketAssets",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Name = table.Column<string>(type: "nvarchar(120)", maxLength: 120, nullable: false),
                    Price = table.Column<decimal>(type: "decimal(18,2)", nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    ChangePercent = table.Column<decimal>(type: "decimal(5,2)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MarketAssets", x => x.Id);
                });

            migrationBuilder.InsertData(
                table: "MarketAssets",
                columns: new[] { "Id", "ChangePercent", "Currency", "Name", "Price", "Symbol" },
                values: new object[,]
                {
                    { new Guid("11111111-1111-1111-1111-111111111111"), 1.42m, "USD", "Apple", 214.32m, "AAPL" },
                    { new Guid("22222222-2222-2222-2222-222222222222"), 0.87m, "USD", "Microsoft", 428.11m, "MSFT" },
                    { new Guid("33333333-3333-3333-3333-333333333333"), 3.18m, "USD", "NVIDIA", 902.55m, "NVDA" },
                    { new Guid("44444444-4444-4444-4444-444444444444"), -1.24m, "USD", "Tesla", 181.09m, "TSLA" }
                });

            migrationBuilder.CreateIndex(
                name: "IX_MarketAssets_Symbol",
                table: "MarketAssets",
                column: "Symbol",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MarketAssets");
        }
    }
}
