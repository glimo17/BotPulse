using System;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql.EntityFrameworkCore.PostgreSQL.Metadata;

#nullable disable

namespace BotPulse.Infrastructure.Migrations;

/// <inheritdoc />
public partial class InitialCreate : Migration
{
    private static readonly bool[] s_descending = Array.Empty<bool>();
    private static readonly string[] s_alertsAckSeverity = ["acknowledged", "severity"];
    private static readonly string[] s_auditActionTimestamp = ["action", "timestamp_utc"];
    private static readonly string[] s_auditUserTimestamp = ["user_id", "timestamp_utc"];
    private static readonly string[] s_logsJobTimestamp = ["job_external_id", "timestamp_utc"];
    private static readonly string[] s_logsSeverity = ["severity", "timestamp_utc"];
    private static readonly string[] s_jobsProviderExternal = ["provider_name", "external_job_id"];
    private static readonly string[] s_metricsRawNameTime = ["metric_name", "timestamp_utc"];
    private static readonly string[] s_metricsRollupBucket = ["bucket_start_utc", "metric_name", "granularity"];
    private static readonly string[] s_queueItemsQueueStatus = ["queue_name", "status"];
    private static readonly string[] s_queueItemsUnique = ["provider_name", "external_item_id"];
    private static readonly string[] s_usersProviderExternal = ["auth_provider", "external_id"];

    /// <inheritdoc />
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "alert_rules",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                rule_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                parameters_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                channels_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                escalation_enabled = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                escalation_timeout_minutes = table.Column<int>(type: "integer", nullable: false, defaultValue: 15),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_alert_rules", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "alerts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                rule_id = table.Column<Guid>(type: "uuid", nullable: false),
                severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                raised_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                condition_description = table.Column<string>(type: "text", nullable: false),
                affected_resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                affected_resource_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                acknowledged = table.Column<bool>(type: "boolean", nullable: false, defaultValue: false),
                acknowledged_by = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                acknowledged_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                escalation_level = table.Column<int>(type: "integer", nullable: false, defaultValue: 0)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_alerts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "audit_records",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                user_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                action = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_type = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                resource_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                outcome = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                ip_address = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: true),
                details_json = table.Column<string>(type: "jsonb", nullable: true),
                correlation_id = table.Column<string>(type: "character varying(64)", maxLength: 64, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_audit_records", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "dashboard_layouts",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                user_id = table.Column<Guid>(type: "uuid", nullable: false),
                widgets_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "[]"),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_dashboard_layouts", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "execution_logs",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                severity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                logger_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                message = table.Column<string>(type: "text", nullable: false),
                job_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                robot_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                process_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                properties_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_execution_logs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "jobs",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                external_job_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                process_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                robot_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                machine_external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                start_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                end_time_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                duration = table.Column<TimeSpan>(type: "interval", nullable: true),
                error_type = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                error_message = table.Column<string>(type: "text", nullable: true),
                retry_of_job_id = table.Column<long>(type: "bigint", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_jobs", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "metrics_raw",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                timestamp_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                metric_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                value = table.Column<double>(type: "double precision", nullable: false),
                dimensions_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}"),
                provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_metrics_raw", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "metrics_rollups",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                bucket_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                granularity = table.Column<string>(type: "character varying(20)", maxLength: 20, nullable: false),
                metric_name = table.Column<string>(type: "character varying(100)", maxLength: 100, nullable: false),
                sum_value = table.Column<double>(type: "double precision", nullable: false),
                min_value = table.Column<double>(type: "double precision", nullable: false),
                max_value = table.Column<double>(type: "double precision", nullable: false),
                avg_value = table.Column<double>(type: "double precision", nullable: false),
                count_value = table.Column<long>(type: "bigint", nullable: false),
                dimensions_json = table.Column<string>(type: "jsonb", nullable: false, defaultValue: "{}")
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_metrics_rollups", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "queue_items",
            columns: table => new
            {
                id = table.Column<long>(type: "bigint", nullable: false)
                    .Annotation("Npgsql:ValueGenerationStrategy", NpgsqlValueGenerationStrategy.IdentityByDefaultColumn),
                external_item_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                provider_name = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                queue_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                status = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                retry_count = table.Column<int>(type: "integer", nullable: false, defaultValue: 0),
                processing_start_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                processing_end_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                output_metadata_json = table.Column<string>(type: "jsonb", nullable: true),
                original_item_id = table.Column<long>(type: "bigint", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_queue_items", x => x.id);
            });

        migrationBuilder.CreateTable(
            name: "users",
            columns: table => new
            {
                id = table.Column<Guid>(type: "uuid", nullable: false),
                external_id = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                user_name = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                email = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: false),
                role = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                auth_provider = table.Column<string>(type: "character varying(50)", maxLength: 50, nullable: false),
                password_hash = table.Column<string>(type: "character varying(255)", maxLength: 255, nullable: true),
                is_active = table.Column<bool>(type: "boolean", nullable: false, defaultValue: true),
                last_login_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: true),
                created_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false),
                updated_at_utc = table.Column<DateTime>(type: "timestamp with time zone", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_users", x => x.id);
            });

        migrationBuilder.CreateIndex(
            name: "idx_alerts_ack_severity",
            table: "alerts",
            columns: s_alertsAckSeverity);

        migrationBuilder.CreateIndex(
            name: "idx_alerts_raised",
            table: "alerts",
            column: "raised_at_utc",
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_audit_action_timestamp",
            table: "audit_records",
            columns: s_auditActionTimestamp);

        migrationBuilder.CreateIndex(
            name: "idx_audit_timestamp_desc",
            table: "audit_records",
            column: "timestamp_utc",
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_audit_user_timestamp",
            table: "audit_records",
            columns: s_auditUserTimestamp);

        migrationBuilder.CreateIndex(
            name: "idx_dashboard_layouts_user_unique",
            table: "dashboard_layouts",
            column: "user_id",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_logs_job_timestamp",
            table: "execution_logs",
            columns: s_logsJobTimestamp);

        migrationBuilder.CreateIndex(
            name: "idx_logs_severity",
            table: "execution_logs",
            columns: s_logsSeverity);

        migrationBuilder.CreateIndex(
            name: "idx_logs_timestamp_desc",
            table: "execution_logs",
            column: "timestamp_utc",
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_jobs_process",
            table: "jobs",
            column: "process_external_id");

        migrationBuilder.CreateIndex(
            name: "idx_jobs_provider_external_unique",
            table: "jobs",
            columns: s_jobsProviderExternal,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_jobs_robot",
            table: "jobs",
            column: "robot_external_id");

        migrationBuilder.CreateIndex(
            name: "idx_jobs_start_time_desc",
            table: "jobs",
            column: "start_time_utc",
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_jobs_status",
            table: "jobs",
            column: "status");

        migrationBuilder.CreateIndex(
            name: "idx_metrics_raw_name_time",
            table: "metrics_raw",
            columns: s_metricsRawNameTime,
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_metrics_rollup_bucket",
            table: "metrics_rollups",
            columns: s_metricsRollupBucket);

        migrationBuilder.CreateIndex(
            name: "idx_queue_items_queue_status",
            table: "queue_items",
            columns: s_queueItemsQueueStatus);

        migrationBuilder.CreateIndex(
            name: "idx_queue_items_unique",
            table: "queue_items",
            columns: s_queueItemsUnique,
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_queue_items_updated",
            table: "queue_items",
            column: "updated_at_utc",
            descending: s_descending);

        migrationBuilder.CreateIndex(
            name: "idx_users_email_unique",
            table: "users",
            column: "email",
            unique: true);

        migrationBuilder.CreateIndex(
            name: "idx_users_provider_external_unique",
            table: "users",
            columns: s_usersProviderExternal,
            unique: true);
    }

    /// <inheritdoc />
    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "alert_rules");
        migrationBuilder.DropTable(name: "alerts");
        migrationBuilder.DropTable(name: "audit_records");
        migrationBuilder.DropTable(name: "dashboard_layouts");
        migrationBuilder.DropTable(name: "execution_logs");
        migrationBuilder.DropTable(name: "jobs");
        migrationBuilder.DropTable(name: "metrics_raw");
        migrationBuilder.DropTable(name: "metrics_rollups");
        migrationBuilder.DropTable(name: "queue_items");
        migrationBuilder.DropTable(name: "users");
    }
}
