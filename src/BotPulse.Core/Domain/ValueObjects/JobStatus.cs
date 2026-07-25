namespace BotPulse.Core.Domain.ValueObjects;

/// <summary>Strongly typed job status value object.</summary>
public sealed record JobStatus
{
    public static readonly JobStatus Pending = new("Pending");
    public static readonly JobStatus Running = new("Running");
    public static readonly JobStatus Success = new("Success");
    public static readonly JobStatus Failed = new("Failed");
    public static readonly JobStatus Stopped = new("Stopped");
    public static readonly JobStatus Cancelled = new("Cancelled");

    private static readonly Dictionary<string, JobStatus> _all = new Dictionary<string, JobStatus>(StringComparer.OrdinalIgnoreCase)
    {
        ["Pending"] = Pending, ["Running"] = Running, ["Success"] = Success,
        ["Failed"] = Failed, ["Stopped"] = Stopped, ["Cancelled"] = Cancelled,
    };

    public string Value { get; }
    public bool IsTerminal => this == Success || this == Failed || this == Stopped || this == Cancelled;
    public bool IsActive => this == Pending || this == Running;

    private JobStatus(string value) => Value = value;

    public static JobStatus Parse(string value) =>
        _all.TryGetValue(value, out var status) ? status
            : throw new ArgumentException($"Unknown job status: '{value}'", nameof(value));

    public static bool TryParse(string value, out JobStatus? result) =>
        _all.TryGetValue(value, out result!);

    public override string ToString() => Value;
}
