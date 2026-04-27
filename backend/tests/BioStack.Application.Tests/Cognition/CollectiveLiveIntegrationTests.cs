namespace BioStack.Application.Tests.Cognition;

using BioStack.Cognition;
using BioStack.Cognition.CollectiveApi;
using Keon.Collective;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Live integration tests for CollectiveLiveOrchestrator against the real
/// Keon Control endpoint.
///
/// These tests are SKIPPED unless the KEON_COLLECTIVE_CONTROL_URL environment
/// variable is set. Run them locally by setting the env vars, or trigger them
/// in CI via workflow_dispatch with the 'run-integration' label.
///
/// Filter commands:
///   All unit tests only:     dotnet test --filter Category!=Integration
///   Live integration only:   dotnet test --filter Category=Integration
///   Specific target:         dotnet test --filter "Category=Integration&amp;Target=Collective"
/// </summary>
[Trait("Category", "Integration")]
[Trait("Target", "Collective")]
public sealed class CollectiveLiveIntegrationTests
{
    // ── Credentials — set via environment variables or CI secrets ─────────────
    private static readonly string? ControlBaseUrl =
        Environment.GetEnvironmentVariable("KEON_COLLECTIVE_CONTROL_URL");
    private static readonly string? BearerToken =
        Environment.GetEnvironmentVariable("KEON_COLLECTIVE_BEARER_TOKEN");

    // ── Fixture ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Builds a CollectiveLiveOrchestrator backed by a real IHttpClientFactory.
    /// This is the actual handler-pool lifecycle used in production — not a mock.
    /// AddHttpClient registers the named client so the framework manages socket
    /// lifecycle, DNS rotation, and default header configuration for us.
    /// </summary>
    private static CollectiveLiveOrchestrator MakeLiveOrchestrator()
    {
        Skip.If(
            string.IsNullOrWhiteSpace(ControlBaseUrl),
            "KEON_COLLECTIVE_CONTROL_URL not set — skipping live Collective integration tests. " +
            "Set the env var locally or inject it as a CI secret to run this suite.");

        var options = new CollectiveApiOptions
        {
            LiveMode        = true,
            ControlBaseUrl  = ControlBaseUrl!,
            BearerToken     = BearerToken,
            TimeoutMs       = 15_000,
            PollMaxAttempts = 5,
            PollDelayMs     = 1_000,
        };

        // Real DI plumbing — same handler-pool lifecycle as production.
        // AddHttpClient registers the named client; BuildServiceProvider
        // hands back a factory backed by the framework's SocketsHttpHandler pool.
        var services = new ServiceCollection();
        services.AddHttpClient(CollectiveLiveOrchestrator.HttpClientName, c =>
        {
            c.BaseAddress = new Uri(options.ControlBaseUrl.TrimEnd('/') + "/");
            c.Timeout     = TimeSpan.FromMilliseconds(options.TimeoutMs);
        });
        var factory = services.BuildServiceProvider()
                              .GetRequiredService<IHttpClientFactory>();

        return new CollectiveLiveOrchestrator(
            factory, options, NullLogger<CollectiveLiveOrchestrator>.Instance);
    }

    private static CollectiveIntent MakeIntent(string id = "int-live-integration-001") => new(
        new IntentId(id),
        "BPC-157 + TB-500 recovery stack assessment",
        "{}",
        new TenantContext("biostack-public"),
        new ActorContext("biostack-system", "Service"),
        new CorrelationContext($"corr-{id}"));

    // ── Tests ─────────────────────────────────────────────────────────────────

    [SkippableFact]
    public async Task Live_Submit_ReturnsSurfaces_NotDegraded()
    {
        var orch     = MakeLiveOrchestrator();
        var envelope = await orch.RunAsync(MakeIntent());

        Assert.NotEqual("COLLECTIVE_UNAVAILABLE", envelope.ConfidenceProfile.Model);
        Assert.NotEmpty(envelope.BranchPerspectiveReview.PerspectiveReviews);
    }

    [SkippableFact]
    public async Task Live_ConfidenceProfile_IsBoundedAndTraceable()
    {
        var orch     = MakeLiveOrchestrator();
        var envelope = await orch.RunAsync(MakeIntent("int-live-integration-002"));

        Assert.True(
            envelope.ConfidenceProfile.IsBoundedAndTraceable(),
            $"Live ConfidenceProfile must be bounded and traceable. " +
            $"Model='{envelope.ConfidenceProfile.Model}', " +
            $"Epistemic='{envelope.ConfidenceProfile.Epistemic}', " +
            $"CalibrationVersion='{envelope.ConfidenceProfile.CalibrationVersion}'");
    }

    [SkippableFact]
    public async Task Live_WitnessSignature_Present()
    {
        var orch     = MakeLiveOrchestrator();
        var envelope = await orch.RunAsync(MakeIntent("int-live-integration-003"));

        Assert.NotNull(envelope.BranchPerspectiveReview.WitnessSignature);
        Assert.NotEmpty(envelope.BranchPerspectiveReview.WitnessSignature);
    }

    [SkippableFact]
    public async Task Live_AllFourPerspectives_Present()
    {
        var orch     = MakeLiveOrchestrator();
        var envelope = await orch.RunAsync(MakeIntent("int-live-integration-004"));

        var reviews = envelope.BranchPerspectiveReview.PerspectiveReviews;
        Assert.Contains(PerspectiveKind.Optimizer,  reviews);
        Assert.Contains(PerspectiveKind.Skeptic,    reviews);
        Assert.Contains(PerspectiveKind.Regulator,  reviews);
        Assert.Contains(PerspectiveKind.Historian,  reviews);
    }

    [SkippableFact]
    public async Task Live_Doctrine_IsExecutable_AlwaysFalse()
    {
        var orch     = MakeLiveOrchestrator();
        var envelope = await orch.RunAsync(MakeIntent("int-live-integration-005"));

        Assert.False(envelope.ContradictionReview.IsExecutable,
            "DOCTRINE: IsExecutable must ALWAYS be false on live responses, " +
            "regardless of what Keon Control returns.");
        Assert.False(envelope.ContradictionReview.CounterPlanIsExecutable,
            "DOCTRINE: CounterPlanIsExecutable must ALWAYS be false on live responses, " +
            "regardless of what Keon Control returns.");
    }
}
