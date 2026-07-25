namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of a robot from an RPA provider.</summary>
public sealed record RobotSnapshot(
    string ExternalId,
    string Name,
    string Status,
    string? MachineExternalId,
    string? LicenseType,
    DateTime LastHeartbeatUtc);
