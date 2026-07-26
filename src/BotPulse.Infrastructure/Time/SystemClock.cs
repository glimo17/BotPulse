using BotPulse.Core.Abstractions.Time;

namespace BotPulse.Infrastructure.Time;

/// <summary>Production implementation of ISystemClock using UTC.</summary>
public sealed class SystemClock : ISystemClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
