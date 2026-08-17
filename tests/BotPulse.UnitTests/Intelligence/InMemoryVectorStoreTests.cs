using BotPulse.Intelligence.Contracts.Vector;
using BotPulse.Intelligence.Providers.InMemory;
using FluentAssertions;

namespace BotPulse.UnitTests.Intelligence;

public sealed class InMemoryVectorStoreTests
{
    private readonly InMemoryVectorStore _sut = new();

    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>();

    [Fact]
    public async Task SearchAsync_ShouldRankMostSimilarRecordFirst()
    {
        await _sut.UpsertAsync(new VectorRecord("robot-offline", new[] { 1f, 0f, 0f }, "Robot went offline", EmptyMetadata));
        await _sut.UpsertAsync(new VectorRecord("queue-backlog", new[] { 0f, 1f, 0f }, "Queue backlog exceeded threshold", EmptyMetadata));
        await _sut.UpsertAsync(new VectorRecord("robot-similar", new[] { 0.9f, 0.1f, 0f }, "Robot disconnected unexpectedly", EmptyMetadata));

        var results = await _sut.SearchAsync(new VectorQuery(new[] { 1f, 0f, 0f }, TopK: 2, MinScore: 0));

        results.Should().HaveCount(2);
        results[0].Id.Should().Be("robot-offline");
        results[1].Id.Should().Be("robot-similar");
        results[0].Score.Should().BeGreaterThan(results[1].Score);
    }

    [Fact]
    public async Task SearchAsync_ShouldExcludeResultsBelowMinScore()
    {
        await _sut.UpsertAsync(new VectorRecord("unrelated", new[] { 0f, 1f, 0f }, "Completely unrelated content", EmptyMetadata));

        var results = await _sut.SearchAsync(new VectorQuery(new[] { 1f, 0f, 0f }, TopK: 5, MinScore: 0.5));

        results.Should().BeEmpty();
    }

    [Fact]
    public async Task SearchAsync_ShouldScopeByOrganizationId()
    {
        var orgA = Guid.NewGuid();
        var orgB = Guid.NewGuid();

        await _sut.UpsertAsync(new VectorRecord("org-a-item", new[] { 1f, 0f, 0f }, "Org A knowledge", EmptyMetadata, orgA));
        await _sut.UpsertAsync(new VectorRecord("org-b-item", new[] { 1f, 0f, 0f }, "Org B knowledge", EmptyMetadata, orgB));

        var results = await _sut.SearchAsync(new VectorQuery(new[] { 1f, 0f, 0f }, TopK: 5, MinScore: 0, OrganizationId: orgA));

        results.Should().ContainSingle();
        results[0].Id.Should().Be("org-a-item");
    }

    [Fact]
    public async Task UpsertAsync_WithSameId_ShouldOverwriteExistingRecord()
    {
        await _sut.UpsertAsync(new VectorRecord("item-1", new[] { 1f, 0f, 0f }, "Original content", EmptyMetadata));
        await _sut.UpsertAsync(new VectorRecord("item-1", new[] { 1f, 0f, 0f }, "Updated content", EmptyMetadata));

        var results = await _sut.SearchAsync(new VectorQuery(new[] { 1f, 0f, 0f }, TopK: 5, MinScore: 0));

        results.Should().ContainSingle();
        results[0].Content.Should().Be("Updated content");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveRecordFromSearchResults()
    {
        await _sut.UpsertAsync(new VectorRecord("to-delete", new[] { 1f, 0f, 0f }, "Content", EmptyMetadata));

        await _sut.DeleteAsync("to-delete");
        var results = await _sut.SearchAsync(new VectorQuery(new[] { 1f, 0f, 0f }, TopK: 5, MinScore: 0));

        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(new float[] { 1, 0 }, new float[] { 1, 0 }, 1.0)]
    [InlineData(new float[] { 1, 0 }, new float[] { 0, 1 }, 0.0)]
    [InlineData(new float[] { 1, 0 }, new float[] { -1, 0 }, -1.0)]
    public void CosineSimilarity_ShouldComputeExpectedScore(float[] a, float[] b, double expected)
    {
        var score = InMemoryVectorStore.CosineSimilarity(a, b);
        score.Should().BeApproximately(expected, 0.0001);
    }
}
