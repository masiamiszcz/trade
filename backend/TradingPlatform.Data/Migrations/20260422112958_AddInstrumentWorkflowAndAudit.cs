using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddInstrumentWorkflowAndAudit : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "IsActive",
                table: "Instruments");

            migrationBuilder.AddColumn<Guid>(
                name: "CreatedBy",
                table: "Instruments",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.AddColumn<string>(
                name: "Description",
                table: "Instruments",
                type: "nvarchar(max)",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ModifiedAtUtc",
                table: "Instruments",
                type: "datetimeoffset",
                nullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "ModifiedBy",
                table: "Instruments",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<long>(
                name: "RowVersion",
                table: "Instruments",
                type: "bigint",
                rowVersion: true,
                nullable: false,
                defaultValue: 0L);

            migrationBuilder.AddColumn<int>(
                name: "Status",
                table: "Instruments",
                type: "int",
                nullable: false,
                defaultValue: 1);  // Draft = 1

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                columns: new[] { "CreatedBy", "Description", "ModifiedAtUtc", "ModifiedBy", "Status" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "Apple Inc. stock", null, null, 3 });

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                columns: new[] { "CreatedBy", "Description", "ModifiedAtUtc", "ModifiedBy", "Status" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "Bitcoin cryptocurrency", null, null, 3 });

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                columns: new[] { "CreatedBy", "Description", "ModifiedAtUtc", "ModifiedBy", "Status" },
                values: new object[] { new Guid("00000000-0000-0000-0000-000000000000"), "S&P 500 Contract for Difference", null, null, 3 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CreatedBy",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "Description",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "ModifiedAtUtc",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "ModifiedBy",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "RowVersion",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "Status",
                table: "Instruments");

            migrationBuilder.AddColumn<bool>(
                name: "IsActive",
                table: "Instruments",
                type: "bit",
                nullable: false,
                defaultValue: false);

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb"),
                column: "IsActive",
                value: true);

            migrationBuilder.UpdateData(
                table: "Instruments",
                keyColumn: "Id",
                keyValue: new Guid("cccccccc-cccc-cccc-cccc-cccccccccccc"),
                column: "IsActive",
                value: true);
        }
    }
}
