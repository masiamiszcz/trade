using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AllowInstrumentSymbolAcrossTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments");

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "Symbol",
                value: "BTCUSDT");

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "BaseCurrency", "Description", "Name", "QuoteCurrency", "Symbol" },
                values: new object[] { "BTC", "Bitcoin / Tether Contract for Difference", "Bitcoin / Tether CFD", "USDT", "BTCUSDT" });

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Symbol_Type",
                table: "Instruments",
                columns: new[] { "Symbol", "Type" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_Instruments_Symbol_Type",
                table: "Instruments");

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "Symbol",
                value: "BTCUSD");

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "BaseCurrency", "Description", "Name", "QuoteCurrency", "Symbol" },
                values: new object[] { "USD", "S&P 500 Contract for Difference", "S&P 500 CFD", "USD", "US500CFD" });

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments",
                column: "Symbol",
                unique: true);
        }
    }
}
