using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Core.Abstractions.Providers;

/// <summary>Provides read access to robot entities from an RPA vendor.</summary>
public interface IRobotProvider
{
    /// <summary>Retrieves all robots from the provider.</summary>
    Task<IReadOnlyList<RobotSnapshot>> GetRobotsAsync(CancellationToken ct = default);

    /// <summary>Retrieves a single robot by its external identifier.</summary>
    Task<RobotSnapshot?> GetRobotByIdAsync(string externalId, CancellationToken ct = default);
}
