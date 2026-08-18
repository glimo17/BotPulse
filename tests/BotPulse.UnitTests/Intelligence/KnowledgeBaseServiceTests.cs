using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Configuration;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Contracts.Vector;
using BotPulse.Intelligence.Knowledge;
using BotPulse.Intelligence.Providers.InMemory;
using FluentAssertions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace BotPulse.UnitTests.Intelligence;

public sealed class KnowledgeBaseServiceTests
{
    private static readonly IReadOnlyDictionary<string, string> EmptyMetadata =
        new Dictionary<string, string>();

    private readonly IEmbeddingProvider _embeddingProvider = Substitute.For<IEmbeddingProvider>();
    private readonly InMemoryVectorStore _vectorStore = new();

    private KnowledgeBaseService CreateSut(int maxContextTokens = 4000)
    {
        var options = Options.Create(new IntelligenceOptions { MaxContextTokens = maxContextTokens });
        return new KnowledgeBaseService(_embeddingProvider, _vectorStore, options);
    }

    [Fact]
    public async Task IndexAsync_CalledTwiceWithSameId_ShouldNotDuplicateRecords()
    {
        _embeddingProvider.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f, 0f }, 2));

        var sut = CreateSut();
        var item = new KnowledgeItem("job-exception-42", KnowledgeSourceType.Exception,
            "NullReferenceException at line 10", EmptyMetadata);

        await sut.IndexAsync(item);
        await sut.IndexAsync(item); // re-index same deterministic Id

        var results = await sut.SearchAsync("null reference", topK: 10);
        results.Should().ContainSingle();
    }

    [Fact]
    public async Task IndexAsync_ShouldTagRecordWithSourceTypeMetadata()
    {
        _embeddingProvider.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f, 0f }, 2));

        var sut = CreateSut();
        var item = new KnowledgeItem("log-1", KnowledgeSourceType.ExecutionLog, "Robot offline", EmptyMetadata);

        await sut.IndexAsync(item);

        var stored = await _vectorStore.SearchAsync(new VectorQuery(new[] { 1f, 0f }, TopK: 1, MinScore: 0));
        stored.Single().Metadata["sourceType"].Should().Be("ExecutionLog");
    }

    [Fact]
    public async Task IndexAsync_ShouldTruncateContentBeforeEmbedding()
    {
        string? capturedContent = null;
        _embeddingProvider.EmbedAsync(Arg.Do<string>(t => capturedContent = t), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f }, 1));

        // maxContextTokens=2 => ~8 chars at 4 chars/token
        var sut = CreateSut(maxContextTokens: 2);
        var longContent = new string('x', 100);
        var item = new KnowledgeItem("item-1", KnowledgeSourceType.Runbook, longContent, EmptyMetadata);

        await sut.IndexAsync(item);

        capturedContent!.Length.Should().Be(8);
    }

    [Fact]
    public async Task IndexAsync_ShouldPropagateOrganizationIdToVectorRecord()
    {
        var orgId = Guid.NewGuid();
        _embeddingProvider.EmbedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f }, 1));

        var sut = CreateSut();
        var item = new KnowledgeItem("item-1", KnowledgeSourceType.Alert, "content", EmptyMetadata, orgId);

        await sut.IndexAsync(item);

        var results = await _vectorStore.SearchAsync(new VectorQuery(new[] { 1f }, TopK: 1, MinScore: 0, OrganizationId: orgId));
        results.Should().ContainSingle();
    }

    [Fact]
    public async Task SearchAsync_ShouldEmbedQueryAndDelegateToVectorStore()
    {
        _embeddingProvider.EmbedAsync("robot failure", Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f, 0f }, 2));
        _embeddingProvider.EmbedAsync("indexed content", Arg.Any<CancellationToken>())
            .Returns(new EmbeddingResult(new[] { 1f, 0f }, 2));

        var sut = CreateSut();
        await sut.IndexAsync(new KnowledgeItem("item-1", KnowledgeSourceType.HistoricalIncident, "indexed content", EmptyMetadata));

        var results = await sut.SearchAsync("robot failure", topK: 5);

        results.Should().ContainSingle();
        results[0].Id.Should().Be("item-1");
    }
}
