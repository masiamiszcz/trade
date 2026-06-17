using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddUserIdToCryptoTransactionAndPosition : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add UserId column to CryptoTransactions (nullable first)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "CryptoTransactions",
                type: "uniqueidentifier",
                nullable: true);

            // Add UserId column to Positions (nullable first)
            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Positions",
                type: "uniqueidentifier",
                nullable: true);

            // Populate CryptoTransactions.UserId from AccountEntity
            migrationBuilder.Sql(@"
                UPDATE CryptoTransactions
                SET UserId = (SELECT UserId FROM Accounts WHERE Accounts.Id = CryptoTransactions.AccountId)
            ");

            // Populate Positions.UserId from AccountEntity
            migrationBuilder.Sql(@"
                UPDATE Positions
                SET UserId = (SELECT UserId FROM Accounts WHERE Accounts.Id = Positions.AccountId)
            ");

            // Make UserId NOT NULL after population
            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "CryptoTransactions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            migrationBuilder.AlterColumn<Guid>(
                name: "UserId",
                table: "Positions",
                type: "uniqueidentifier",
                nullable: false,
                oldClrType: typeof(Guid),
                oldType: "uniqueidentifier",
                oldNullable: true);

            // Create index on UserId for fast queries
            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_UserId",
                table: "CryptoTransactions",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId",
                table: "Positions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_CryptoTransactions_UserId",
                table: "CryptoTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_UserId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "CryptoTransactions");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Positions");
        }
    }
}
