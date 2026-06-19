using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddCryptoTrading : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles");

            migrationBuilder.AddColumn<Guid>(
                name: "UserId",
                table: "Positions",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "LastUpdateTime",
                table: "Positions",
                type: "datetimeoffset",
                nullable: false,
                defaultValueSql: "SYSDATETIMEOFFSET()");

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Positions",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.AddColumn<byte[]>(
                name: "RowVersion",
                table: "Accounts",
                type: "rowversion",
                rowVersion: true,
                nullable: false,
                defaultValue: new byte[0]);

            migrationBuilder.CreateTable(
                name: "CryptoTransactions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    UserId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AccountId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    InstrumentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Symbol = table.Column<string>(type: "nvarchar(20)", maxLength: 20, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(10)", maxLength: 10, nullable: false),
                    Quantity = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    RequestedPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    ExecutionPrice = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    TotalValue = table.Column<decimal>(type: "decimal(18,8)", nullable: false),
                    Status = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    Currency = table.Column<string>(type: "nvarchar(3)", maxLength: 3, nullable: false),
                    CreatedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: false, defaultValueSql: "SYSDATETIMEOFFSET()"),
                    CompletedAt = table.Column<DateTimeOffset>(type: "datetimeoffset", nullable: true),
                    ErrorReason = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CryptoTransactions", x => x.Id);
                    table.CheckConstraint("CK_CryptoTransactions_Prices_NonNegative", "[RequestedPrice] > 0 AND [ExecutionPrice] > 0 AND [TotalValue] >= 0");
                    table.CheckConstraint("CK_CryptoTransactions_Quantity_NonNegative", "[Quantity] > 0");
                    table.ForeignKey(
                        name: "FK_CryptoTransactions_Accounts_AccountId",
                        column: x => x.AccountId,
                        principalTable: "Accounts",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CryptoTransactions_Instruments_InstrumentId",
                        column: x => x.InstrumentId,
                        principalTable: "Instruments",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_CryptoTransactions_Users_UserId",
                        column: x => x.UserId,
                        principalTable: "Users",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles",
                columns: new[] { "Symbol", "Source", "IntervalMinutes", "OpenTime" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_AccountId_CreatedAt",
                table: "CryptoTransactions",
                columns: new[] { "AccountId", "CreatedAt" });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_AccountId_Status",
                table: "CryptoTransactions",
                columns: new[] { "AccountId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_AccountId_Symbol",
                table: "CryptoTransactions",
                columns: new[] { "AccountId", "Symbol" });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_AccountId_Type",
                table: "CryptoTransactions",
                columns: new[] { "AccountId", "Type" });

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_InstrumentId",
                table: "CryptoTransactions",
                column: "InstrumentId");

            migrationBuilder.CreateIndex(
                name: "IX_CryptoTransactions_UserId",
                table: "CryptoTransactions",
                column: "UserId");

            migrationBuilder.AddForeignKey(
                name: "FK_Positions_Users_UserId",
                table: "Positions",
                column: "UserId",
                principalTable: "Users",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.CreateIndex(
                name: "IX_Positions_UserId",
                table: "Positions",
                column: "UserId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_Positions_Users_UserId",
                table: "Positions");

            migrationBuilder.DropIndex(
                name: "IX_CryptoTransactions_UserId",
                table: "CryptoTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Positions_UserId",
                table: "Positions");

            migrationBuilder.DropTable(
                name: "CryptoTransactions");

            migrationBuilder.DropIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles");

            migrationBuilder.DropColumn(
                name: "UserId",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "LastUpdateTime",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Positions");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Accounts");

            migrationBuilder.CreateIndex(
                name: "IX_Candles_Symbol_Source_Interval_OpenTime",
                table: "Candles",
                columns: new[] { "Symbol", "Source", "IntervalMinutes", "OpenTime" });
        }
    }
}
