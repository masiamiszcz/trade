using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class GenericAdminRequest_RemoveInstrumentFK : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AdminRequests_Instruments_InstrumentId",
                table: "AdminRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdminRequest_InstrumentId",
                table: "AdminRequests");

            migrationBuilder.DropColumn(
                name: "InstrumentId",
                table: "AdminRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "AdminRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: true,
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000);

            migrationBuilder.AddColumn<Guid>(
                name: "EntityId",
                table: "AdminRequests",
                type: "uniqueidentifier",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "EntityType",
                table: "AdminRequests",
                type: "nvarchar(100)",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_EntityId",
                table: "AdminRequests",
                column: "EntityId");

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_EntityType",
                table: "AdminRequests",
                column: "EntityType");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AdminRequest_EntityId",
                table: "AdminRequests");

            migrationBuilder.DropIndex(
                name: "IX_AdminRequest_EntityType",
                table: "AdminRequests");

            migrationBuilder.DropColumn(
                name: "EntityId",
                table: "AdminRequests");

            migrationBuilder.DropColumn(
                name: "EntityType",
                table: "AdminRequests");

            migrationBuilder.AlterColumn<string>(
                name: "Reason",
                table: "AdminRequests",
                type: "nvarchar(1000)",
                maxLength: 1000,
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "nvarchar(1000)",
                oldMaxLength: 1000,
                oldNullable: true);

            migrationBuilder.AddColumn<Guid>(
                name: "InstrumentId",
                table: "AdminRequests",
                type: "uniqueidentifier",
                nullable: false,
                defaultValue: new Guid("00000000-0000-0000-0000-000000000000"));

            migrationBuilder.CreateIndex(
                name: "IX_AdminRequest_InstrumentId",
                table: "AdminRequests",
                column: "InstrumentId");

            migrationBuilder.AddForeignKey(
                name: "FK_AdminRequests_Instruments_InstrumentId",
                table: "AdminRequests",
                column: "InstrumentId",
                principalTable: "Instruments",
                principalColumn: "Id");
        }
    }
}
