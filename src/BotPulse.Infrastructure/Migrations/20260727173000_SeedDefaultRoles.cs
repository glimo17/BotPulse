using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <inheritdoc />
public partial class SeedDefaultRoles : Migration
{
    private static readonly string[] RoleColumns = new[] { "Id", "Name", "IsSystem" };
    private static readonly string[] RolePermissionColumns = new[] { "RoleId", "Permission" };
    private static readonly string[] RolePermissionKeyColumns = new[] { "RoleId", "Permission" };
    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Insert default system roles
        migrationBuilder.InsertData(
            table: "roles",
            columns: RoleColumns,
            values: new object[,]
            {
                { new Guid("11111111-1111-1111-1111-111111111111"), "Administrator", true },
                { new Guid("22222222-2222-2222-2222-222222222222"), "Operator", true },
                { new Guid("33333333-3333-3333-3333-333333333333"), "Viewer", true }
            });

        // Administrator: full permissions
        migrationBuilder.InsertData(
            table: "role_permissions",
            columns: RolePermissionColumns,
            values: new object[,]
            {
                { new Guid("11111111-1111-1111-1111-111111111111"), "DashboardView" },
                { new Guid("11111111-1111-1111-1111-111111111111"), "JobsView" },
                { new Guid("11111111-1111-1111-1111-111111111111"), "JobsExecute" },
                { new Guid("11111111-1111-1111-1111-111111111111"), "UsersManage" },

                // Operator: job operations + view
                { new Guid("22222222-2222-2222-2222-222222222222"), "DashboardView" },
                { new Guid("22222222-2222-2222-2222-222222222222"), "JobsView" },
                { new Guid("22222222-2222-2222-2222-222222222222"), "JobsExecute" },

                // Viewer: read-only
                { new Guid("33333333-3333-3333-3333-333333333333"), "DashboardView" },
                { new Guid("33333333-3333-3333-3333-333333333333"), "JobsView" }
            });
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove seeded permissions
        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "DashboardView" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "JobsView" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "JobsExecute" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("11111111-1111-1111-1111-111111111111"), "UsersManage" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "DashboardView" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "JobsView" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("22222222-2222-2222-2222-222222222222"), "JobsExecute" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), "DashboardView" });

        migrationBuilder.DeleteData(
            table: "role_permissions",
            keyColumns: RolePermissionKeyColumns,
            keyValues: new object[] { new Guid("33333333-3333-3333-3333-333333333333"), "JobsView" });

        // Remove roles
        migrationBuilder.DeleteData(
            table: "roles",
            keyColumn: "Id",
            keyValue: new Guid("11111111-1111-1111-1111-111111111111"));

        migrationBuilder.DeleteData(
            table: "roles",
            keyColumn: "Id",
            keyValue: new Guid("22222222-2222-2222-2222-222222222222"));

        migrationBuilder.DeleteData(
            table: "roles",
            keyColumn: "Id",
            keyValue: new Guid("33333333-3333-3333-3333-333333333333"));
    }
}
