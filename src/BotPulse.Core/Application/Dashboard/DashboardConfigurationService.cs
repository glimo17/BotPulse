using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Domain.Entities;
using BotPulse.Core.Domain.ValueObjects;

namespace BotPulse.Core.Application.Dashboard;

/// <summary>Manages per-user dashboard widget layout configuration.</summary>
public sealed class DashboardConfigurationService
{
    private readonly IDashboardLayoutRepository _layouts;

    public DashboardConfigurationService(IDashboardLayoutRepository layouts) => _layouts = layouts;

    public async Task<DashboardLayout?> GetLayoutAsync(Guid userId, CancellationToken ct = default) =>
        await _layouts.GetByUserIdAsync(userId, ct).ConfigureAwait(false);

    public async Task<DashboardLayout> UpdateLayoutAsync(
        Guid userId, string widgetsJson, CancellationToken ct = default)
    {
        var layout = await _layouts.GetByUserIdAsync(userId, ct).ConfigureAwait(false);

        if (layout is null)
        {
            layout = DashboardLayout.CreateDefault(userId, widgetsJson);
            await _layouts.AddAsync(layout, ct).ConfigureAwait(false);
        }
        else
        {
            layout.UpdateWidgets(widgetsJson);
            _layouts.Update(layout);
        }

        return layout;
    }

    public async Task<DashboardLayout> ResetToDefaultAsync(
        Guid userId, UserRole role, CancellationToken ct = default)
    {
        var defaultWidgets = GetDefaultWidgetsForRole(role);
        return await UpdateLayoutAsync(userId, defaultWidgets, ct).ConfigureAwait(false);
    }

    private static string GetDefaultWidgetsForRole(UserRole role) => role switch
    {
        UserRole.Administrator => """[{"type":"KPISummary","order":0},{"type":"RobotMonitor","order":1},{"type":"MachineHealth","order":2},{"type":"JobQueue","order":3},{"type":"QueueProgress","order":4},{"type":"Alerts","order":5},{"type":"ExecutionTimeline","order":6}]""",
        UserRole.Operator => """[{"type":"KPISummary","order":0},{"type":"RobotMonitor","order":1},{"type":"JobQueue","order":2},{"type":"QueueProgress","order":3},{"type":"Alerts","order":4}]""",
        _ => """[{"type":"KPISummary","order":0},{"type":"JobQueue","order":1},{"type":"Alerts","order":2}]""",
    };
}
