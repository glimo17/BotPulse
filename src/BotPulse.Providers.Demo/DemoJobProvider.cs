using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;

namespace BotPulse.Providers.Demo;

internal sealed class DemoJobProvider : IJobProvider
{
    private readonly DemoDataSeed _seed;
    public DemoJobProvider(DemoDataSeed seed) => _seed = seed;

    public Task<IReadOnlyList<JobSnapshot>> GetJobsAsync(JobQuery query, CancellationToken ct = default)
    {
        var jobs = _seed.GetJobs().AsEnumerable();

        if (query.UpdatedSinceUtc.HasValue)
        {
            jobs = jobs.Where(j => j.StartTimeUtc >= query.UpdatedSinceUtc.Value ||
                                   (j.EndTimeUtc.HasValue && j.EndTimeUtc >= query.UpdatedSinceUtc.Value));
        }

        if (!string.IsNullOrEmpty(query.Status))
        {
            jobs = jobs.Where(j => j.Status.Equals(query.Status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrEmpty(query.RobotExternalId))
        {
            jobs = jobs.Where(j => j.RobotExternalId == query.RobotExternalId);
        }

        if (!string.IsNullOrEmpty(query.ProcessExternalId))
        {
            jobs = jobs.Where(j => j.ProcessExternalId == query.ProcessExternalId);
        }

        jobs = jobs.OrderByDescending(j => j.StartTimeUtc).Skip(query.Skip);

        if (query.Top > 0)
        {
            jobs = jobs.Take(query.Top);
        }

        return Task.FromResult<IReadOnlyList<JobSnapshot>>(jobs.ToList());
    }

    public Task<JobSnapshot?> GetJobByIdAsync(string externalId, CancellationToken ct = default)
    {
        var job = _seed.GetJobs().FirstOrDefault(j => j.ExternalId == externalId);
        return Task.FromResult(job);
    }

    public Task<StartJobResult> StartJobAsync(StartJobRequest request, CancellationToken ct = default)
        => Task.FromResult(_seed.StartJob(request));

    public Task StopJobAsync(string externalId, CancellationToken ct = default)
    {
        _seed.TransitionJob(externalId, "Stopped");
        return Task.CompletedTask;
    }

    public Task CancelJobAsync(string externalId, CancellationToken ct = default)
    {
        _seed.TransitionJob(externalId, "Cancelled");
        return Task.CompletedTask;
    }
}
