using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Domain.Entities;
using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Knowledge.Indexers;
using FluentAssertions;

namespace BotPulse.UnitTests.Intelligence;

public sealed class IndexersTests
{
    [Fact]
    public void ExecutionLogIndexer_ShouldProduceDeterministicIdBasedOnLogId()
    {
        var log = ExecutionLog.FromSnapshot(new ExecutionLogSnapshot(
            TimestampUtc: DateTime.UtcNow,
            Severity: "Error",
            LoggerName: "Robot.Executor",
            Message: "Selector not found",
            JobExternalId: "job-1",
            RobotExternalId: "robot-1",
            ProcessExternalId: "process-1",
            PropertiesJson: "{}"), "UiPath");

        var itemA = ExecutionLogIndexer.ToKnowledgeItem(log);
        var itemB = ExecutionLogIndexer.ToKnowledgeItem(log);

        itemA.Id.Should().Be(itemB.Id);
        itemA.SourceType.Should().Be(KnowledgeSourceType.ExecutionLog);
        itemA.Content.Should().Contain("Selector not found");
    }

    [Fact]
    public void JobExceptionIndexer_JobWithoutError_ShouldReturnNull()
    {
        var job = Job.FromSnapshot(new JobSnapshot(
            ExternalId: "ext-1",
            ProcessExternalId: "proc-1",
            RobotExternalId: "robot-1",
            MachineExternalId: null,
            Status: "Success",
            StartTimeUtc: DateTime.UtcNow,
            EndTimeUtc: DateTime.UtcNow,
            Duration: TimeSpan.FromSeconds(5),
            ErrorType: null,
            ErrorMessage: null), "UiPath");

        var item = JobExceptionIndexer.ToKnowledgeItem(job);

        item.Should().BeNull();
    }

    [Fact]
    public void JobExceptionIndexer_FailedJob_ShouldProduceKnowledgeItem()
    {
        var job = Job.FromSnapshot(new JobSnapshot(
            ExternalId: "ext-2",
            ProcessExternalId: "proc-1",
            RobotExternalId: "robot-1",
            MachineExternalId: null,
            Status: "Failed",
            StartTimeUtc: DateTime.UtcNow,
            EndTimeUtc: DateTime.UtcNow,
            Duration: TimeSpan.FromSeconds(2),
            ErrorType: "System.NullReferenceException",
            ErrorMessage: "Object reference not set"), "UiPath");

        var item = JobExceptionIndexer.ToKnowledgeItem(job);

        item.Should().NotBeNull();
        item!.SourceType.Should().Be(KnowledgeSourceType.Exception);
        item.Content.Should().Contain("NullReferenceException").And.Contain("Object reference not set");
    }

    [Fact]
    public void ResolutionIndexer_ShouldIncludeRootCauseImpactAndSteps()
    {
        var diagnosis = new DiagnosisResult(
            RootCause: "Selector changed after UI update",
            Impact: "Job fails on every run",
            ResolutionSteps: new[] { "Update selector", "Redeploy process" },
            Confidence: 0.9,
            ReferencedKnowledgeIds: Array.Empty<string>());

        var item = ResolutionIndexer.ToKnowledgeItem("job-99", diagnosis);

        item.Id.Should().Be("resolution-job-99");
        item.SourceType.Should().Be(KnowledgeSourceType.Resolution);
        item.Content.Should().Contain("Selector changed after UI update")
            .And.Contain("Update selector")
            .And.Contain("Redeploy process");
        item.Metadata["validated"].Should().Be("true");
    }
}
