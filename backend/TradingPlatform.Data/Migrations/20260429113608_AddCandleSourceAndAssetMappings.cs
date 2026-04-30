using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCandleSourceAndAssetMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Candles",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<int>(
                name: "IntervalMinutes",
                table: "Candles",
                type: "int",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Source",
                table: "Candles",
                type: "nvarchar(450)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateTable(
                name: "AssetMappings",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Symbol = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    CoingeckoId = table.Column<string>(type: "nvarchar(450)", nullable: false),
                    Source = table.Column<string>(type: "nvarchar(max)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssetMappings", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles",
                columns: new[] { "Symbol", "Source", "IntervalMinutes", "OpenTime" });

            migrationBuilder.CreateIndex(
                name: "IX_AssetMappings_Symbol_CoingeckoId",
                table: "AssetMappings",
                columns: new[] { "Symbol", "CoingeckoId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AssetMappings");

            migrationBuilder.DropIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "IntervalMinutes",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "Source",
                table: "Candles");

            migrationBuilder.AlterColumn<string>(
                name: "Symbol",
                table: "Candles",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
