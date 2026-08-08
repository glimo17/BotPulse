using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <summary>
/// Adds missing columns to roles and user_roles tables:
/// - roles: description, created_at_utc, updated_at_utc, unique index on name
/// - user_roles: assigned_by_user_id, assigned_at_utc, scope, index on user_id
/// </summary>
public partial class AddRbacColumns : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // ── roles: add description + timestamps ────────────────────────────────
        migrationBuilder.AddColumn<string>(
            name: "description",
            table: "roles",
            type: "text",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "created_at_utc",
            table: "roles",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "updated_at_utc",
            table: "roles",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.RenameColumn(
            name: "IsSystem",
            table: "roles",
            newName: "is_system");

        migrationBuilder.RenameColumn(
            name: "Name",
            table: "roles",
            newName: "name");

        migrationBuilder.RenameColumn(
            name: "Id",
            table: "roles",
            newName: "id");

        migrationBuilder.CreateIndex(
            name: "idx_roles_name_unique",
            table: "roles",
            column: "name",
            unique: true);

        // ── user_roles: add audit + scope columns ──────────────────────────────
        migrationBuilder.AddColumn<Guid>(
            name: "assigned_by_user_id",
            table: "user_roles",
            type: "uuid",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "assigned_at_utc",
            table: "user_roles",
            type: "timestamp with time zone",
            nullable: false,
            defaultValueSql: "NOW()");

        migrationBuilder.AddColumn<string>(
            name: "scope",
            table: "user_roles",
            type: "character varying(255)",
            maxLength: 255,
            nullable: true);

        migrationBuilder.RenameColumn(
            name: "UserId",
            table: "user_roles",
            newName: "user_id");

        migrationBuilder.RenameColumn(
            name: "RoleId",
            table: "user_roles",
            newName: "role_id");

        migrationBuilder.CreateIndex(
            name: "idx_user_roles_user_id",
            table: "user_roles",
            column: "user_id");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // ── user_roles: revert ─────────────────────────────────────────────────
        migrationBuilder.DropIndex(name: "idx_user_roles_user_id", table: "user_roles");
        migrationBuilder.DropColumn(name: "assigned_by_user_id", table: "user_roles");
        migrationBuilder.DropColumn(name: "assigned_at_utc", table: "user_roles");
        migrationBuilder.DropColumn(name: "scope", table: "user_roles");

        migrationBuilder.RenameColumn(name: "user_id", table: "user_roles", newName: "UserId");
        migrationBuilder.RenameColumn(name: "role_id", table: "user_roles", newName: "RoleId");

        // ── roles: revert ──────────────────────────────────────────────────────
        migrationBuilder.DropIndex(name: "idx_roles_name_unique", table: "roles");
        migrationBuilder.DropColumn(name: "description", table: "roles");
        migrationBuilder.DropColumn(name: "created_at_utc", table: "roles");
        migrationBuilder.DropColumn(name: "updated_at_utc", table: "roles");

        migrationBuilder.RenameColumn(name: "is_system", table: "roles", newName: "IsSystem");
        migrationBuilder.RenameColumn(name: "name", table: "roles", newName: "Name");
        migrationBuilder.RenameColumn(name: "id", table: "roles", newName: "Id");
    }
}
