using BotPulse.Intelligence.Contracts.AI;
using BotPulse.Intelligence.Contracts.Diagnostics;
using BotPulse.Intelligence.Contracts.Knowledge;
using BotPulse.Intelligence.Contracts.Vector;
using FluentAssertions;
using NSubstitute;

namespace BotPulse.UnitTests.Intelligence;

public sealed class RagDiagnosticServiceTests
{
    private readonly IKnowledgeBase _knowledgeBase = Substitute.For<IKnowledgeBase>();
    private readonly IChatCompletionProvider _chatProvider = Substitute.For<IChatCompletionProvider>();

    private BotPulse.Intelligence.Diagnostics.RagDiagnosticService CreateSut() =>
        new(_knowledgeBase, _chatProvider);

    private static readonly DiagnosisRequest DefaultRequest = new(
        JobId: "job-1",
        ErrorMessage: "Selector not found: btnSubmit",
        StackTrace: "at ClickActivity.Execute()");

    [Fact]
    public async Task DiagnoseAsync_WithRelevantContext_ShouldParseStructuredFields()
    {
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>
            {
                new("resolution-99", "Selector changed after UI update. Fix: update selector.", 0.85, new Dictionary<string, string>())
            });

        _chatProvider.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(
                Content: """
                    ROOT_CAUSE: The button selector changed after a UI update.
                    IMPACT: The process fails on every execution attempt.
                    RESOLUTION_STEPS: Update the selector | Redeploy the process | Re-run the job
                    CONFIDENCE: 0.85
                    """,
                PromptTokens: 100,
                CompletionTokens: 50));

        var sut = CreateSut();
        var result = await sut.DiagnoseAsync(DefaultRequest);

        result.RootCause.Should().Contain("button selector changed");
        result.Impact.Should().Contain("fails on every execution");
        result.ResolutionSteps.Should().HaveCount(3);
        result.ResolutionSteps[0].Should().Be("Update the selector");
        result.Confidence.Should().Be(0.85);
        result.ReferencedKnowledgeIds.Should().ContainSingle().Which.Should().Be("resolution-99");
    }

    [Fact]
    public async Task DiagnoseAsync_WithNoRelevantContext_ShouldCapConfidenceLow()
    {
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>()); // nothing found

        _chatProvider.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse(
                Content: """
                    ROOT_CAUSE: Unknown, likely an environment issue.
                    IMPACT: Uncertain.
                    RESOLUTION_STEPS: Check logs manually
                    CONFIDENCE: 0.9
                    """,
                PromptTokens: 50,
                CompletionTokens: 20));

        var sut = CreateSut();
        var result = await sut.DiagnoseAsync(DefaultRequest);

        // Even though the LLM claimed high confidence, the platform caps it
        // because no grounding context was available (Requisito 5.4).
        result.Confidence.Should().BeLessThanOrEqualTo(0.3);
        result.ReferencedKnowledgeIds.Should().BeEmpty();
    }

    [Fact]
    public async Task DiagnoseAsync_ShouldFilterOutLowRelevanceContext()
    {
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>
            {
                new("weak-match", "Unrelated content", 0.3, new Dictionary<string, string>())
            });

        _chatProvider.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("ROOT_CAUSE: unknown\nIMPACT: unknown\nRESOLUTION_STEPS: \nCONFIDENCE: 0.2", 10, 10));

        var sut = CreateSut();
        var result = await sut.DiagnoseAsync(DefaultRequest);

        result.ReferencedKnowledgeIds.Should().BeEmpty("the only match scored below the relevance threshold");
    }

    [Fact]
    public async Task DiagnoseAsync_ShouldPassOrganizationIdToKnowledgeBaseSearch()
    {
        var orgId = Guid.NewGuid();
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>());
        _chatProvider.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("ROOT_CAUSE: x\nIMPACT: y\nRESOLUTION_STEPS: z\nCONFIDENCE: 0.1", 10, 10));

        var sut = CreateSut();
        var request = DefaultRequest with { OrganizationId = orgId };

        await sut.DiagnoseAsync(request);

        await _knowledgeBase.Received(1).SearchAsync(
            Arg.Any<string>(), Arg.Any<int>(), orgId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task DiagnoseAsync_WithMalformedLlmResponse_ShouldNotThrowAndReturnFallbackFields()
    {
        _knowledgeBase.SearchAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(new List<VectorSearchResult>());
        _chatProvider.CompleteAsync(Arg.Any<ChatRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ChatResponse("this is not the expected format at all", 10, 10));

        var sut = CreateSut();
        var result = await sut.DiagnoseAsync(DefaultRequest);

        result.RootCause.Should().NotBeNullOrWhiteSpace();
        result.Impact.Should().NotBeNullOrWhiteSpace();
        result.ResolutionSteps.Should().BeEmpty();
    }
}
