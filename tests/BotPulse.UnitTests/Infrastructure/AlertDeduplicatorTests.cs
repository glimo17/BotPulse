using BotPulse.Core.Abstractions.Alerts;
using BotPulse.Infrastructure.Alerts;
using FluentAssertions;
using FsCheck;
using FsCheck.Xunit;

namespace BotPulse.UnitTests.Infrastructure;

public sealed class AlertDeduplicatorTests
{
    private static readonly Guid DefaultRuleId = Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001");

    private static AlertRuleContext MakeRule(Guid? id = null) =>
        new(id ?? DefaultRuleId, "RobotOffline", "Critical", "{}");

    private static AlertCandidate MakeCandidate(string resourceId = "res-1") =>
        new("Robot", resourceId, "Offline too long");

    [Fact]
    public void ShouldEmit_FirstCall_ShouldReturnTrue()
    {
        var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
        var result = sut.ShouldEmit(MakeRule(), MakeCandidate(), DateTime.UtcNow);
        result.Should().BeTrue();
    }

    [Fact]
    public void ShouldEmit_SecondCallWithinWindow_ShouldReturnFalse()
    {
        var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
        var rule = MakeRule();
        var candidate = MakeCandidate();
        var now = DateTime.UtcNow;

        sut.ShouldEmit(rule, candidate, now).Should().BeTrue();
        sut.ShouldEmit(rule, candidate, now.AddMinutes(1)).Should().BeFalse();
    }

    [Fact]
    public void ShouldEmit_AfterWindowExpires_ShouldReturnTrue()
    {
        var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
        var rule = MakeRule();
        var candidate = MakeCandidate();
        var now = DateTime.UtcNow;

        sut.ShouldEmit(rule, candidate, now).Should().BeTrue();
        sut.ShouldEmit(rule, candidate, now.AddMinutes(6)).Should().BeTrue();
    }

    [Fact]
    public void ShouldEmit_DifferentResources_ShouldBeIndependent()
    {
        var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
        var rule = MakeRule();
        var now = DateTime.UtcNow;

        sut.ShouldEmit(rule, MakeCandidate("res-1"), now).Should().BeTrue();
        sut.ShouldEmit(rule, MakeCandidate("res-2"), now).Should().BeTrue();
    }

    [Fact]
    public void ShouldEmit_DifferentRules_SameCandidateShouldBeIndependent()
    {
        var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
        var rule1 = MakeRule(Guid.NewGuid());
        var rule2 = MakeRule(Guid.NewGuid());
        var candidate = MakeCandidate();
        var now = DateTime.UtcNow;

        sut.ShouldEmit(rule1, candidate, now).Should().BeTrue();
        sut.ShouldEmit(rule2, candidate, now).Should().BeTrue();
    }

    /// <summary>
    /// **Validates: Requirements 8.3**
    /// Property: for any sequence of N calls within the deduplication window for the same
    /// (rule, resource) pair, ShouldEmit returns true exactly once.
    /// </summary>
    [Property]
    public Property ShouldEmit_WithinWindow_AtMostOncePerRuleResource()
    {
        return Prop.ForAll(
            Arb.From(Gen.Choose(1, 20).Select(n => n)),
            count =>
            {
                var sut = new AlertDeduplicator(TimeSpan.FromMinutes(5));
                var rule = MakeRule();
                var candidate = MakeCandidate();
                var baseTime = DateTime.UtcNow;

                // Each call advances by 10 seconds — all within the 5-minute window.
                var emittedCount = Enumerable.Range(0, count)
                    .Count(i => sut.ShouldEmit(rule, candidate, baseTime.AddSeconds(i * 10)));

                return emittedCount == 1;
            });
    }
}
