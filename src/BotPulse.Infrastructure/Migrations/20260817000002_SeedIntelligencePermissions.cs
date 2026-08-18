using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <summary>
/// Seeds the Intelligence.View and Intelligence.Diagnose permissions
/// (Phase 3 - Intelligence Platform, ADR-016) to the Administrator and
/// Operations Manager system roles.
/// </summary>
public partial class SeedIntelligencePermissions : Migration
{
    private static readonly Guid AdministratorRoleId     = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OperationsManagerRoleId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");

    private static readonly string[] PermissionColumns = { "RoleId", "Permission" };

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: PermissionColumns,
            values: new object[,]
            {
                { AdministratorRoleId, "Intelligence.View" },
                { AdministratorRoleId, "Intelligence.Diagnose" },
                { OperationsManagerRoleId, "Intelligence.View" },
                { OperationsManagerRoleId, "Intelligence.Diagnose" }
            });
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            DELETE FROM role_permissions
            WHERE "Permission" IN ('Intelligence.View', 'Intelligence.Diagnose')
              AND "RoleId" IN (
                'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa',
                'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'
              );
            """);
    }
}
