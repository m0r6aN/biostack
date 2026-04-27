namespace BioStack.Application.Tests.Cognition;

using System.Net;
using System.Net.Http.Json;
using BioStack.Cognition.CollectiveApi;
using Keon.Collective;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

/// <summary>
/// Tests for CollectiveLiveOrchestrator.
/// All tests use a mocked HttpMessageHandler so no real HTTP calls are made.
///
/// Doctrine invariants asserted in every test:
///   • ContradictionReview.IsExecutable == false
///   • ContradictionReview.CounterPlanIsExecutable == false
/// </summary>
public sealed class CollectiveLiveOrchestratorTests
{
    // ── Fixture helpers ───────────────────────────────────────────────────────

    private static CollectiveApiOptions DefaultOptions(int pollMax = 3) => new()
    {
        LiveMode         = true,
        ControlBaseUrl   = "https://keon.control.test",
        PollMaxAttempts  = pollMax,
        PollDelayMs      = 0,       // No delay in tests
    };

    private static CollectiveIntent MakeIntent() => new(
        new IntentId("intent-live-001"),
        "recovery",
        "{}",
        new TenantContext("biostack-public"),
        new ActorContext("biostack-system", "Service"),
        new CorrelationContext("corr-001"));

    private static object FullLiveRunResponse(bool apiSaysExecutable = false) => new
    {
        dataMode = "LIVE",
        retrievalMode = "SYNC",
        run = new
        {
            intentId = "intent-live-001",
            correlationId = "corr-001",
            cognitionSurfaces = new
            {
                perspectiveReviews = new Dictionary<string, object>
                {
                    ["Optimizer"] = new { kind = "Optimizer", summary = "Good", findings = new[] { new { findingId = "OPT-001", category = "Efficiency", narrative = "Strong synergy", severity = "Info" } } },
                    ["Skeptic"]   = new { kind = "Skeptic",   summary = "Caution", findings = new[] { new { findingId = "SKP-001", category = "Risk", narrative = "Limited evidence", severity = "Warning" } } },
                    ["Regulator"] = new { kind = "Regulator", summary = "Review needed", findings = new[] { new { findingId = "REG-001", category = "Oversight", narrative = "Provider review advised", severity = "Warning" } } },
                    ["Historian"] = new { kind = "Historian", summary = "Known pairing", findings = new[] { new { findingId = "HST-001", category = "Pattern", narrative = "Regenerative pairing known", severity = "Info" } } },
                },
                contradictionReview = new
                {
                    counterPlanNarrative    = "Single compound alternative",
                    counterPlanIsExecutable = apiSaysExecutable,    // BioStack must override to false
                    isExecutable            = apiSaysExecutable,
                },
                confidenceProfile = new
                {
                    model = "epistemic-v1", epistemic = "moderate",
                    evidenceSupport = "anecdotal", contradictionDensity = "low",
                    calibrationVersion = "v1",
                },
                reasoningGraphRef = new { graphId = "graph-1", nodeCount = 5, edgeCount = 4 },
            },
        },
        operatorMessages = Array.Empty<object>(),
    };

    /// <summary>
    /// Builds the orchestrator with a mocked IHttpClientFactory.
    /// The factory returns an HttpClient backed by a mock handler,
    /// matching the production path through CollectiveLiveOrchestrator.RunAsync.
    /// </summary>
    private static CollectiveLiveOrchestrator MakeOrchestrator(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> respFactory,
        CollectiveApiOptions? options = null)
    {
        var opts = options ?? DefaultOptions();

        var handler = new Mock<HttpMessageHandler>();
        handler.Protected()
               .Setup<Task<HttpResponseMessage>>("SendAsync",
                   ItExpr.IsAny<HttpRequestMessage>(),
                   ItExpr.IsAny<CancellationToken>())
               .Returns<HttpRequestMessage, CancellationToken>(respFactory);

        var http = new HttpClient(handler.Object) { BaseAddress = new Uri(opts.ControlBaseUrl.TrimEnd('/') + "/") };

        var mockFactory = new Mock<IHttpClientFactory>();
        mockFactory.Setup(f => f.CreateClient(CollectiveLiveOrchestrator.HttpClientName)).Returns(http);

        return new CollectiveLiveOrchestrator(
            mockFactory.Object, opts, NullLogger<CollectiveLiveOrchestrator>.Instance);
    }

    // ── T_Live1: Happy path — POST succeeds, surfaces fully mapped ────────────
    [Fact]
    public async Task T_Live1_Submit_Success_MapsEnvelope()
    {
        var orch = MakeOrchestrator((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(FullLiveRunResponse()) }));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.True(envelope.BranchPerspectiveReview.PerspectiveReviews.ContainsKey(PerspectiveKind.Optimizer));
        Assert.True(envelope.BranchPerspectiveReview.PerspectiveReviews.ContainsKey(PerspectiveKind.Historian));
        Assert.Equal("epistemic-v1", envelope.ConfidenceProfile.Model);
        Assert.Equal(5, envelope.ReasoningGraphRef.NodeCount);
        Assert.False(envelope.ContradictionReview.IsExecutable,            "IsExecutable must be false (doctrine)");
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable, "CounterPlanIsExecutable must be false (doctrine)");
    }

    // ── T_Live2: 202 polling — POST returns 202 then GET returns 200 ──────────
    [Fact]
    public async Task T_Live2_Polling_202_Then_200_MapsEnvelope()
    {
        var callCount = 0;
        var orch = MakeOrchestrator((_, _) =>
        {
            callCount++;
            // Call 1 = POST (202), Call 2 = GET (202 still processing), Call 3 = GET (200 ready)
            var status = callCount < 3 ? HttpStatusCode.Accepted : HttpStatusCode.OK;
            var resp = new HttpResponseMessage(status);
            if (status == HttpStatusCode.OK)
                resp.Content = JsonContent.Create(FullLiveRunResponse());
            return Task.FromResult(resp);
        }, DefaultOptions(pollMax: 4));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.True(envelope.BranchPerspectiveReview.PerspectiveReviews.ContainsKey(PerspectiveKind.Skeptic));
        Assert.False(envelope.ContradictionReview.IsExecutable);
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable);
    }

    // ── T_Live3: Submit throws → degraded envelope returned (no throw) ────────
    [Fact]
    public async Task T_Live3_SubmitException_ReturnsDegradedEnvelope()
    {
        var orch = MakeOrchestrator((_, _) => throw new HttpRequestException("Connection refused"));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.Empty(envelope.BranchPerspectiveReview.PerspectiveReviews);
        Assert.Equal("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.False(envelope.ContradictionReview.IsExecutable);
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable);
    }

    // ── T_Live4: Doctrine — API says IsExecutable=true, envelope must be false ─
    [Fact]
    public async Task T_Live4_Doctrine_IsExecutable_AlwaysFalse_EvenIfApiSaysTrue()
    {
        var orch = MakeOrchestrator((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(FullLiveRunResponse(apiSaysExecutable: true)) }));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.False(envelope.ContradictionReview.IsExecutable,
            "Doctrine: IsExecutable must ALWAYS be false regardless of API response");
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable,
            "Doctrine: CounterPlanIsExecutable must ALWAYS be false regardless of API response");
    }

    // ── T_Live5: Null cognition surfaces → degraded envelope ─────────────────
    [Fact]
    public async Task T_Live5_NullCognitionSurfaces_ReturnsDegradedEnvelope()
    {
        var emptyRun = new { dataMode = "DEGRADED", retrievalMode = "SYNC", run = new { intentId = "i", correlationId = "c", cognitionSurfaces = (object?)null } };
        var orch = MakeOrchestrator((_, _) =>
            Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
                { Content = JsonContent.Create(emptyRun) }));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.Equal("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.False(envelope.ContradictionReview.IsExecutable);
    }

    // ── T_Live6: Polling exhaustion — all GET attempts return 202 → degraded ──
    [Fact]
    public async Task T_Live6_PollExhausted_ReturnsDegradedEnvelope()
    {
        // POST returns 202, every GET poll also returns 202 — never resolves.
        var orch = MakeOrchestrator(
            (_, _) => Task.FromResult(new HttpResponseMessage(HttpStatusCode.Accepted)),
            options: DefaultOptions(pollMax: 3));

        var envelope = await orch.RunAsync(MakeIntent());

        Assert.Empty(envelope.BranchPerspectiveReview.PerspectiveReviews);
        Assert.Equal("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.False(envelope.ContradictionReview.IsExecutable,
            "Doctrine: degraded path must also force IsExecutable = false");
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable,
            "Doctrine: degraded path must also force CounterPlanIsExecutable = false");
    }
}
