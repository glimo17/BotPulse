using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Providers.Demo;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;
using Xunit;

namespace BotPulse.UnitTests.Providers;

[Trait("Category", "DemoProvider")]
public sealed class DemoProviderTests : IDisposable
{
    private readonly DemoDataSeed _seed = new();

    public void Dispose() => _seed.Dispose();

    /// <summary>
    /// Property 1: All robots have unique, non-empty ExternalId and valid Status.
    /// **Validates: Requirements REQ-2.3, REQ-3.2**
    /// </summary>
    [Fact]
    public void Robots_HaveUniqueNonEmptyExternalIds_AndValidStatus()
    {
        var robots = _seed.GetRobots();

        robots.Should().HaveCount(6);
        robots.Select(r => r.ExternalId).Should().OnlyHaveUniqueItems();
        robots.Should().AllSatisfy(r =>
        {
            r.ExternalId.Should().NotBeNullOrWhiteSpace();
            r.Name.Should().NotBeNullOrWhiteSpace();
            r.Status.Should().BeOneOf("Idle", "Busy", "Online", "Offline");
        });
    }

    /// <summary>
    /// Property 2: StartJob returns a valid ExternalId for known processes.
    /// For unknown processes, throws InvalidOperationException.
    /// **Validates: Requirements REQ-7.1, REQ-7.2**
    /// </summary>
    [Fact]
    public void StartJob_ReturnsValidId_ForKnownProcesses()
    {
        var processes = _seed.GetProcesses();
        foreach (var proc in processes)
        {
            var request = new StartJobRequest(proc.ExternalId, null);
            var result = _seed.StartJob(request);

            result.JobExternalId.Should().NotBeNullOrWhiteSpace();

            var jobs = _seed.GetJobs();
            jobs.Should().Contain(j => j.ExternalId == result.JobExternalId && j.Status == "Running");
        }
    }

    [Fact]
    public void StartJob_ThrowsForUnknownProcess()
    {
        var request = new StartJobRequest("unknown-process-xyz", null);
        var act = () => _seed.StartJob(request);

        act.Should().Throw<InvalidOperationException>();
    }

    /// <summary>
    /// Property 3: StopJob and CancelJob are idempotent - never throw.
    /// **Validates: Requirements REQ-7.3, REQ-7.4**
    /// </summary>
    [Property(Arbitrary = new[] { typeof(NonNullStringArbitrary) })]
    public void StopJob_NeverThrows(NonNull<string> externalId)
    {
        var act = () => _seed.TransitionJob(externalId.Get, "Stopped");
        act.Should().NotThrow();
    }

    [Property(Arbitrary = new[] { typeof(NonNullStringArbitrary) })]
    public void CancelJob_NeverThrows(NonNull<string> externalId)
    {
        var act = () => _seed.TransitionJob(externalId.Get, "Cancelled");
        act.Should().NotThrow();
    }

    /// <summary>
    /// Property 4: For every queue, TotalItems == ProcessedItems + FailedItems + PendingItems.
    /// **Validates: Requirements REQ-3.7**
    /// </summary>
    [Fact]
    public void Queues_TotalItemsInvariant()
    {
        var queues = _seed.GetQueues();

        queues.Should().HaveCount(3);
        queues.Should().AllSatisfy(q =>
        {
            q.TotalItems.Should().Be(q.ProcessedItems + q.FailedItems + q.PendingItems);
            q.PendingItems.Should().BeGreaterThanOrEqualTo(0);
        });
    }

    /// <summary>
    /// Property 5: Filtering logs by JobExternalId returns only logs for that job.
    /// **Validates: Requirements REQ-8.2**
    /// </summary>
    [Fact]
    public void Logs_FilterByJobId_ReturnsOnlyMatchingLogs()
    {
        var provider = new DemoLogProvider(_seed);
        var jobs = _seed.GetJobs().Take(10);

        foreach (var job in jobs)
        {
            var query = new LogQuery(JobExternalId: job.ExternalId);
            var logs = provider.GetExecutionLogsAsync(query).GetAwaiter().GetResult();

            logs.Should().NotBeEmpty($"job {job.ExternalId} should have logs");
            logs.Should().AllSatisfy(l => l.JobExternalId.Should().Be(job.ExternalId));
        }
    }

    /// <summary>
    /// Property 6: GetMachineByIdAsync returns null for unknown IDs.
    /// **Validates: Requirements REQ-9.2**
    /// </summary>
    [Theory]
    [InlineData("nonexistent-machine")]
    [InlineData("machine-99")]
    [InlineData("abc")]
    public void Machines_GetById_ReturnsNullForUnknownIds(string unknownId)
    {
        var provider = new DemoMachineProvider(_seed);
        var result = provider.GetMachineByIdAsync(unknownId).GetAwaiter().GetResult();

        result.Should().BeNull();
    }

    [Fact]
    public void Machines_GetById_ReturnsForKnownIds()
    {
        var provider = new DemoMachineProvider(_seed);

        foreach (var id in new[] { "machine-01", "machine-02", "machine-03" })
        {
            var result = provider.GetMachineByIdAsync(id).GetAwaiter().GetResult();
            result.Should().NotBeNull();
            result!.ExternalId.Should().Be(id);
        }
    }

    /// <summary>
    /// Property 7: Exactly 4 processes returned, all Published, all with valid semver.
    /// **Validates: Requirements REQ-3.4**
    /// </summary>
    [Fact]
    public void Processes_ExactlyFourPublished_WithSemver()
    {
        var processes = _seed.GetProcesses();

        processes.Should().HaveCount(4);
        processes.Should().AllSatisfy(p =>
        {
            p.PublicationStatus.Should().Be("Published");
            p.Version.Should().MatchRegex(@"^\d+\.\d+\.\d+$");
            p.Name.Should().NotBeNullOrWhiteSpace();
            p.Description.Should().NotBeNullOrWhiteSpace();
            p.CompatibleRobotCount.Should().BeGreaterThan(0);
        });
    }

    /// <summary>
    /// Additional: Verify seed generates sufficient data.
    /// </summary>
    [Fact]
    public void Seed_GeneratesSufficientData()
    {
        _seed.GetRobots().Should().HaveCount(6);
        _seed.GetMachines().Should().HaveCount(3);
        _seed.GetProcesses().Should().HaveCount(4);
        _seed.GetJobs().Count.Should().BeGreaterThanOrEqualTo(75);
        _seed.GetQueues().Should().HaveCount(3);
        _seed.GetAssets().Should().HaveCount(5);
        _seed.GetLogs().Count.Should().BeGreaterThanOrEqualTo(600);
    }
}

/// <summary>Helper arbitrary for non-null strings in FsCheck properties.</summary>
public static class NonNullStringArbitrary
{
    public static Arbitrary<NonNull<string>> NonNullString()
    {
        return Arb.Default.NonNull<string>();
    }
}
