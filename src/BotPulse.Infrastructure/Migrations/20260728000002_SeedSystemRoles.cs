using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <summary>
/// Seeds the 3 system roles with complete permission sets from PermissionCatalog.
/// Replaces the previous partial SeedDefaultRoles migration (wrong permission names, incomplete sets).
/// </summary>
public partial class SeedSystemRoles : Migration
{
    // Deterministic GUIDs for system roles — never change these
    private static readonly Guid AdministratorRoleId    = new("aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa");
    private static readonly Guid OperationsManagerRoleId = new("bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb");
    private static readonly Guid ViewerRoleId            = new("cccccccc-cccc-cccc-cccc-cccccccccccc");

    private static readonly string[] RoleColumns = { "id", "name", "description", "is_system", "created_at_utc", "updated_at_utc" };
    private static readonly string[] PermissionColumns = { "RoleId", "Permission" };
    private static readonly string[] PermissionKeyColumns = { "RoleId", "Permission" };

    // ── Permission sets ────────────────────────────────────────────────────────
    private static readonly string[] AllPermissions =
    {
        "Dashboard.View", "Dashboard.Export",
        "Jobs.View", "Jobs.Execute", "Jobs.Cancel", "Jobs.Retry",
        "Robots.View", "Robots.Restart",
        "Queues.View",
        "Logs.View",
        "Machines.View",
        "Assets.View",
        "Metrics.View",
        "Alerts.View", "Alerts.Acknowledge",
        "Users.View", "Users.Create", "Users.Update", "Users.Delete",
        "Roles.View", "Roles.Update",
        "Settings.View", "Settings.Edit",
        "Integrations.Configure"
    };

    private static readonly string[] OperationsManagerPermissions =
    {
        "Dashboard.View",
        "Robots.View",
        "Queues.View",
        "Jobs.View", "Jobs.Execute", "Jobs.Cancel", "Jobs.Retry",
        "Logs.View",
        "Machines.View",
        "Assets.View",
        "Metrics.View",
        "Alerts.View", "Alerts.Acknowledge"
    };

    private static readonly string[] ViewerPermissions =
    {
        "Dashboard.View",
        "Robots.View",
        "Queues.View",
        "Jobs.View",
        "Metrics.View",
        "Logs.View",
        "Machines.View",
        "Assets.View",
        "Alerts.View"
    };

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        var now = DateTimeOffset.UtcNow;

        // ── Insert system roles ────────────────────────────────────────────────
        migrationBuilder.InsertData(
            table: "roles",
            columns: RoleColumns,
            values: new object[,]
            {
                { AdministratorRoleId,     "Administrator",      "Full access to the platform.",                          true, now, now },
                { OperationsManagerRoleId, "Operations Manager", "Monitor and manage RPA operations.",                    true, now, now },
                { ViewerRoleId,            "Viewer",             "Read-only access to platform resources.",               true, now, now }
            });

        // ── Administrator: all 23 permissions ─────────────────────────────────
        var adminValues = new object[AllPermissions.Length, 2];
        for (var i = 0; i < AllPermissions.Length; i++)
        {
            adminValues[i, 0] = AdministratorRoleId;
            adminValues[i, 1] = AllPermissions[i];
        }
        migrationBuilder.InsertData(table: "role_permissions", columns: PermissionColumns, values: adminValues);

        // ── Operations Manager: 13 permissions ────────────────────────────────
        var omValues = new object[OperationsManagerPermissions.Length, 2];
        for (var i = 0; i < OperationsManagerPermissions.Length; i++)
        {
            omValues[i, 0] = OperationsManagerRoleId;
            omValues[i, 1] = OperationsManagerPermissions[i];
        }
        migrationBuilder.InsertData(table: "role_permissions", columns: PermissionColumns, values: omValues);

        // ── Viewer: 9 permissions ──────────────────────────────────────────────
        var viewerValues = new object[ViewerPermissions.Length, 2];
        for (var i = 0; i < ViewerPermissions.Length; i++)
        {
            viewerValues[i, 0] = ViewerRoleId;
            viewerValues[i, 1] = ViewerPermissions[i];
        }
        migrationBuilder.InsertData(table: "role_permissions", columns: PermissionColumns, values: viewerValues);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Remove permissions then roles (FK order)
        foreach (var roleId in new[] { AdministratorRoleId, OperationsManagerRoleId, ViewerRoleId })
        {
            migrationBuilder.Sql(
                $"DELETE FROM role_permissions WHERE \"RoleId\" = '{roleId}'");
        }

        migrationBuilder.DeleteData(table: "roles", keyColumn: "id", keyValue: AdministratorRoleId);
        migrationBuilder.DeleteData(table: "roles", keyColumn: "id", keyValue: OperationsManagerRoleId);
        migrationBuilder.DeleteData(table: "roles", keyColumn: "id", keyValue: ViewerRoleId);
    }
}
