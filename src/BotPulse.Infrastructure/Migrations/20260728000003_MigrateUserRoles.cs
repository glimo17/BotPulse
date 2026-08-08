using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <summary>
/// Migrates existing users from the legacy role string column to user_roles assignments.
/// Maps: "Administrator" → aaaaaaaa, "Operator"/"Operations Manager" → bbbbbbbb, "Viewer" → cccccccc.
/// Then drops the role column from the users table.
/// </summary>
public partial class MigrateUserRoles : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── Migrate existing user role assignments ────────────────────────────
        // Map legacy role string values to new system role IDs
        migrationBuilder.Sql(@"
            INSERT INTO user_roles (user_id, role_id, assigned_at_utc)
            SELECT
                u.id,
                CASE u.role
                    WHEN 'Administrator' THEN 'aaaaaaaa-aaaa-aaaa-aaaa-aaaaaaaaaaaa'::uuid
                    WHEN 'Operator'      THEN 'bbbbbbbb-bbbb-bbbb-bbbb-bbbbbbbbbbbb'::uuid
                    WHEN 'Viewer'        THEN 'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid
                    ELSE                      'cccccccc-cccc-cccc-cccc-cccccccccccc'::uuid
                END,
                NOW()
            FROM users u
            WHERE u.role IS NOT NULL
            ON CONFLICT (user_id, role_id) DO NOTHING;
        ");

        // ── Drop legacy role column ────────────────────────────────────────────
        migrationBuilder.DropColumn(name: "role", table: "users");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── Restore role column ────────────────────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "role",
            table: "users",
            type: "character varying(50)",
            maxLength: 50,
            nullable: false,
            defaultValue: "Viewer");

        // ── Restore role values from user_roles ────────────────────────────────
        migrationBuilder.Sql(@"
            UPDATE users SET role = (
                SELECT r.name
                FROM roles r
                JOIN user_roles ur ON ur.role_id = r.id
                WHERE ur.user_id = users.id
                ORDER BY r.is_system DESC
                LIMIT 1
            )
            WHERE EXISTS (
                SELECT 1 FROM user_roles ur WHERE ur.user_id = users.id
            );
        ");
    }
}
