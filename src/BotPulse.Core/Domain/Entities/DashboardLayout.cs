namespace BotPulse.Core.Domain.Entities;

/// <summary>User-specific dashboard widget layout configuration.</summary>
public sealed class DashboardLayout
{
    private DashboardLayout() { }

    public Guid Id { get; private set; }
    public Guid UserId { get; private set; }
    public string WidgetsJson { get; private set; } = "[]";
    public DateTime UpdatedAtUtc { get; private set; }

    public static DashboardLayout CreateDefault(Guid userId, string defaultWidgetsJson = "[]")
    {
        return new DashboardLayout
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            WidgetsJson = defaultWidgetsJson,
            UpdatedAtUtc = DateTime.UtcNow,
        };
    }

    public void UpdateWidgets(string widgetsJson)
    {
        WidgetsJson = widgetsJson;
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
