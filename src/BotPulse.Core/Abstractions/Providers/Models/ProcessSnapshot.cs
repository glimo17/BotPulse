namespace BotPulse.Core.Abstractions.Providers.Models;

/// <summary>Point-in-time snapshot of a process (release) from an RPA provider.</summary>
public sealed record ProcessSnapshot(
    string ExternalId,
    string Name,
    string Version,
    string PublicationStatus,
    string? Description,
    int CompatibleRobotCount);

/// <summary>Defines an input parameter for a process.</summary>
public sealed record ProcessParameter(
    string Name,
    string Type,
    bool IsRequired,
    string? DefaultValue);
