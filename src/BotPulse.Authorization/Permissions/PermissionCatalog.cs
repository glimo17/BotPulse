namespace BotPulse.Authorization.Permissions;

/// <summary>
/// Full catalog of granular permissions used across BotPulse.
/// Each permission follows the format: Area.Action
/// </summary>
public static class PermissionCatalog
{
    // ─── Dashboard ────────────────────────────────────────────────────────────
    public const string DashboardView   = "Dashboard.View";
    public const string DashboardExport = "Dashboard.Export";

    // ─── Jobs ─────────────────────────────────────────────────────────────────
    public const string JobsView    = "Jobs.View";
    public const string JobsExecute = "Jobs.Execute";
    public const string JobsCancel  = "Jobs.Cancel";
    public const string JobsRetry   = "Jobs.Retry";

    // ─── Robots ───────────────────────────────────────────────────────────────
    public const string RobotsView    = "Robots.View";
    public const string RobotsRestart = "Robots.Restart";

    // ─── Queues ───────────────────────────────────────────────────────────────
    public const string QueuesView = "Queues.View";

    // ─── Logs ─────────────────────────────────────────────────────────────────
    public const string LogsView = "Logs.View";

    // ─── Machines ─────────────────────────────────────────────────────────────
    public const string MachinesView = "Machines.View";

    // ─── Assets ───────────────────────────────────────────────────────────────
    public const string AssetsView = "Assets.View";

    // ─── Metrics ──────────────────────────────────────────────────────────────
    public const string MetricsView = "Metrics.View";

    // ─── Alerts ───────────────────────────────────────────────────────────────
    public const string AlertsView        = "Alerts.View";
    public const string AlertsAcknowledge = "Alerts.Acknowledge";

    // ─── Users ────────────────────────────────────────────────────────────────
    public const string UsersView   = "Users.View";
    public const string UsersCreate = "Users.Create";
    public const string UsersUpdate = "Users.Update";
    public const string UsersDelete = "Users.Delete";

    // ─── Roles ────────────────────────────────────────────────────────────────
    public const string RolesView   = "Roles.View";
    public const string RolesUpdate = "Roles.Update";

    // ─── Settings ─────────────────────────────────────────────────────────────
    public const string SettingsView = "Settings.View";
    public const string SettingsEdit = "Settings.Edit";

    // ─── Integrations ─────────────────────────────────────────────────────────
    public const string IntegrationsConfigure = "Integrations.Configure";

    // ─── Audit ────────────────────────────────────────────────────────────────
    public const string AuditView = "Audit.View";

    // ─── Intelligence (ADR-016 §11: consumed by, never bypassed by, the AI platform) ──
    public const string IntelligenceView     = "Intelligence.View";
    public const string IntelligenceDiagnose = "Intelligence.Diagnose";

    // ─── All permissions (used for Administrator role seeding) ────────────────
    public static readonly IReadOnlyList<string> All = new[]
    {
        DashboardView, DashboardExport,
        JobsView, JobsExecute, JobsCancel, JobsRetry,
        RobotsView, RobotsRestart,
        QueuesView,
        LogsView,
        MachinesView,
        AssetsView,
        MetricsView,
        AlertsView, AlertsAcknowledge,
        UsersView, UsersCreate, UsersUpdate, UsersDelete,
        RolesView, RolesUpdate,
        SettingsView, SettingsEdit,
        IntegrationsConfigure,
        AuditView,
        IntelligenceView, IntelligenceDiagnose
    };

    // ─── Operations Manager permission set ────────────────────────────────────
    public static readonly IReadOnlyList<string> OperationsManager = new[]
    {
        DashboardView,
        RobotsView,
        QueuesView,
        JobsView, JobsExecute, JobsCancel, JobsRetry,
        LogsView,
        MachinesView,
        AssetsView,
        MetricsView,
        AlertsView, AlertsAcknowledge,
        IntelligenceView, IntelligenceDiagnose
    };

    // ─── Viewer permission set ────────────────────────────────────────────────
    public static readonly IReadOnlyList<string> Viewer = new[]
    {
        DashboardView,
        RobotsView,
        QueuesView,
        JobsView,
        MetricsView,
        LogsView,
        MachinesView,
        AssetsView,
        AlertsView
    };
}
