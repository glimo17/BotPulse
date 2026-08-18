using System.Collections.Concurrent;
using BotPulse.Intelligence.Contracts.Diagnostics;

namespace BotPulse.Intelligence.Providers.InMemory;

/// <summary>
/// In-memory feedback store for tests and local development without an
/// external database.
/// </summary>
public sealed class InMemoryDiagnosisFeedbackStore : IDiagnosisFeedbackStore
{
    private readonly ConcurrentBag<DiagnosisFeedback> _feedback = new();

    public IReadOnlyCollection<DiagnosisFeedback> All => _feedback.ToArray();

    public Task RecordAsync(DiagnosisFeedback feedback, CancellationToken ct = default)
    {
        _feedback.Add(feedback);
        return Task.CompletedTask;
    }
}
