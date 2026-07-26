using System.Security.Claims;
using BotPulse.Core.Abstractions.Notifications;
using BotPulse.Core.Abstractions.Persistence;
using BotPulse.Core.Abstractions.Providers;
using BotPulse.Core.Abstractions.Providers.Models;
using BotPulse.Core.Application.Jobs;
using BotPulse.Core.Exceptions;
using FluentAssertions;
using NSubstitute;

namespace BotPulse.UnitTests.Application;

public sealed class JobCommandServiceTests
{
    private static ClaimsPrincipal MakeUser(string id = "user-1", string name = "alice") =>
        new(new ClaimsIdentity([
            new Claim(ClaimTypes.NameIdentifier, id),
            new Claim(ClaimTypes.Name, name)
        ]));

    [Fact]
    public async Task StartAsync_WhenProviderSucceeds_ShouldAuditAndNotify()
    {
        var jobProvider = Substitute.For<IJobProvider>();
        var jobRepo = Substitute.For<IJobRepository>();
        var audit = Substitute.For<IAuditRepository>();
        var notifications = Substitute.For<INotificationDelivery>();

        jobProvider.StartJobAsync(Arg.Any<StartJobRequest>()).Returns(new StartJobResult("job-ext-1"));

        var sut = new JobCommandService(jobProvider, jobRepo, audit, notifications);
        var result = await sut.StartAsync(
            new StartJobRequest("proc-1", null), MakeUser(), "corr-1");

        result.JobExternalId.Should().Be("job-ext-1");
        await audit.Received(1).RecordAsync(
            Arg.Is<AuditRecordData>(a => a.Action == "StartJob" && a.Outcome == "Success"),
            Arg.Any<CancellationToken>());
        await notifications.Received(1).PublishAsync(
            Arg.Is<NotificationEvent>(e => e.EventType == "job.action.requested"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StartAsync_WhenProviderFails_ShouldAuditError()
    {
        var jobProvider = Substitute.For<IJobProvider>();
        var audit = Substitute.For<IAuditRepository>();

        jobProvider.StartJobAsync(Arg.Any<StartJobRequest>())
            .Returns<StartJobResult>(_ => throw new Exception("provider error"));

        var sut = new JobCommandService(
            jobProvider, Substitute.For<IJobRepository>(),
            audit, Substitute.For<INotificationDelivery>());

        await Assert.ThrowsAsync<Exception>(() =>
            sut.StartAsync(new StartJobRequest("proc-1", null), MakeUser(), "corr-1"));

        await audit.Received(1).RecordAsync(
            Arg.Is<AuditRecordData>(a => a.Action == "StartJob" && a.Outcome == "Error"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task StopAsync_WhenJobNotFound_ShouldThrowEntityNotFoundException()
    {
        var jobRepo = Substitute.For<IJobRepository>();
        jobRepo.GetByExternalIdAsync(Arg.Any<string>(), Arg.Any<string>())
            .Returns((BotPulse.Core.Domain.Entities.Job?)null);

        var sut = new JobCommandService(
            Substitute.For<IJobProvider>(), jobRepo,
            Substitute.For<IAuditRepository>(), Substitute.For<INotificationDelivery>());

        await Assert.ThrowsAsync<EntityNotFoundException>(() =>
            sut.StopAsync("job-1", "UiPath", MakeUser(), "corr-1"));
    }
}
