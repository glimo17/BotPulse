namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of a machine from an RPA provider.</summary>
public sealed record MachineSnapshot(
    string ExternalId,
    string Name,
    string Status,
    DateTime LastHeartbeatUtc,
    int ConnectedRobotCount);
