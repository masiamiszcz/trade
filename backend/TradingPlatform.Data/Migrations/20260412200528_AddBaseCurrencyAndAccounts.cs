using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddBaseCurrencyAndAccounts : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "BaseCurrency",
                table: "Users",
                type: "nvarchar(3)",
                maxLength: 3,
                nullable: false,
                defaultValue: "PLN");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "BaseCurrency",
                table: "Users");
        }
    }
}
