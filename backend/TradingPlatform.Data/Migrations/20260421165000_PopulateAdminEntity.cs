using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradingPlatform.Data.Migrations
{
    /// <inheritdoc />
    public partial class PopulateAdminEntity : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Populate AdminEntity for existing admins (if any)
            // Find all Users with Role='Admin' that don't have an AdminEntity entry
            // Mark the FIRST admin as super admin (for bootstrap protection)
            migrationBuilder.Sql(@"
                -- Find all admins without AdminEntity and create entries for them
                INSERT INTO [Admins] ([UserId], [IsSuperAdmin])
                SELECT [Id], CASE 
                    WHEN ROW_NUMBER() OVER (ORDER BY [CreatedAtUtc]) = 1 THEN CAST(1 AS bit)
                    ELSE CAST(0 AS bit)
                END as IsSuperAdmin
                FROM [Users]
                WHERE [Role] = 1  -- UserRole.Admin = 1
                  AND [Id] NOT IN (SELECT [UserId] FROM [Admins])
                ORDER BY [CreatedAtUtc];
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove AdminEntity entries that were created for existing users
            migrationBuilder.Sql(@"
                DELETE FROM [Admins]
                WHERE [UserId] IN (
                    SELECT [Id]
                    FROM [Users]
                    WHERE [Role] = 1  -- UserRole.Admin = 1
                );
            ");
        }
    }
}
