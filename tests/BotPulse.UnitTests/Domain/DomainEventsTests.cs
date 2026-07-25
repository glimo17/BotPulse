using BotPulse.Core.Domain.Events;
using FluentAssertions;

namespace BotPulse.UnitTests.Domain;

public sealed class DomainEventsTests
{
    [Fact]
    public void JobStateChanged_RequiredFields_ShouldNotBeEmpty()
    {
        var evt = new JobStateChanged("job-1", "UiPath", "Pending", "Running");
        evt.JobExternalId.Should().NotBeNullOrEmpty();
        evt.ProviderName.Should().NotBeNullOrEmpty();
        evt.OldStatus.Should().NotBeNullOrEmpty();
        evt.NewStatus.Should().NotBeNullOrEmpty();
        evt.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void AlertRaised_RequiredFields_ShouldNotBeEmpty()
    {
        var evt = new AlertRaised(Guid.NewGuid(), Guid.NewGuid(), "RobotOffline", "Critical",
            "Robot", "robot-1", "Robot has been offline for more than 10 minutes");
        evt.AlertId.Should().NotBeEmpty();
        evt.RuleId.Should().NotBeEmpty();
        evt.Severity.Should().NotBeNullOrEmpty();
        evt.OccurredAtUtc.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }
}
