namespace BotPulse.Core.Abstractions.Time;

/// <summary>
/// Provides the current UTC time. Abstracted to enable deterministic testing.
/// </summary>
public interface ISystemClock
{
    DateTime UtcNow { get; }
}
