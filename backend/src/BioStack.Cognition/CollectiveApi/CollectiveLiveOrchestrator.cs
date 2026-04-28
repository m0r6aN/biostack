namespace BioStack.Cognition.CollectiveApi;

using Keon.Collective;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.Logging;

/// <summary>
/// Live implementation of <see cref="ICognitiveDensityOrchestrator"/> that calls
/// Keon Control /api/collective/live-runs rather than using the rule-based stub.
///
/// Uses <see cref="IHttpClientFactory"/> so the ASP.NET Core handler pool manages
/// socket lifecycle and DNS rotation — avoids the socket-exhaustion antipattern.
/// A fresh <see cref="HttpClient"/> is borrowed from the factory on each RunAsync
/// call and disposed when the call returns.
///
/// DOCTRINE (non-negotiable):
///   • IsEffectBearing is NEVER set to true anywhere in this class.
///   • ContradictionReview.IsExecutable and CounterPlanIsExecutable are
///     ALWAYS forced to false before returning, regardless of what the API says.
///   • On any failure the method returns a degraded envelope rather than throwing.
/// </summary>
internal sealed class CollectiveLiveOrchestrator : ICognitiveDensityOrchestrator
{
    /// <summary>Named HttpClient registered via AddHttpClient("keon-collective", ...).</summary>
    internal const string HttpClientName = "keon-collective";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly CollectiveApiOptions _options;
    private readonly ILogger<CollectiveLiveOrchestrator> _logger;

    // Degraded sentinel values
    private const string DegradedModel   = "COLLECTIVE_UNAVAILABLE";
    private const string DegradedSummary = "Collective Host unavailable — commentary degraded.";

    public CollectiveLiveOrchestrator(
        IHttpClientFactory httpClientFactory,
        CollectiveApiOptions options,
        ILogger<CollectiveLiveOrchestrator> logger)
    {
        _httpClientFactory = httpClientFactory;
        _options           = options;
        _logger            = logger;
    }

    public async Task<CognitiveDensityEnvelope> RunAsync(
        CollectiveIntent intent,
        TemporalEchoBranch? seedBranch = null,
        BranchRefinementOptions? refinementOptions = null,
        ClaimGraph? claimGraph = null,
        IReadOnlyList<BranchCollapseRecord>? historicalCollapses = null,
        CancellationToken ct = default)
    {
        var submitRequest = new CollectiveSubmitRequest(
            Objective:     intent.Goal,
            TenantId:      intent.TenantContext.TenantId,
            ActorId:       intent.ActorContext.ActorId,
            ActorType:     intent.ActorContext.ActorKind,
            CorrelationId: intent.CorrelationContext.CorrelationId,
            IntentId:      intent.IntentId.Value);

        // Borrow a handler-pooled HttpClient for this operation's lifetime only.
        using var http   = _httpClientFactory.CreateClient(HttpClientName);
        var apiClient    = new CollectiveApiClient(http, _options);

        CollectiveLiveRunResponse? response;
        try
        {
            response = await apiClient.SubmitAsync(submitRequest, ct);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "CollectiveLiveOrchestrator: submit failed for intent {IntentId}", intent.IntentId.Value);
            return BuildDegradedEnvelope();
        }

        if (response?.Run is null)
        {
            // POST returned 202 or empty body — poll GET
            response = await PollAsync(
                apiClient,
                intent.IntentId.Value,
                submitRequest.TenantId,
                submitRequest.ActorId,
                submitRequest.CorrelationId,
                ct);
        }

        if (response?.Run?.CognitionSurfaces is null)
        {
            _logger.LogWarning("CollectiveLiveOrchestrator: no cognition surfaces for intent {IntentId}", intent.IntentId.Value);
            return BuildDegradedEnvelope();
        }

        return MapToEnvelope(response.Run.CognitionSurfaces);
    }

    // ── Polling ──────────────────────────────────────────────────────────────

    private async Task<CollectiveLiveRunResponse?> PollAsync(
        CollectiveApiClient client,
        string intentId, string tenantId, string actorId, string correlationId,
        CancellationToken ct)
    {
        for (var attempt = 0; attempt < _options.PollMaxAttempts; attempt++)
        {
            if (attempt > 0)
                await Task.Delay(_options.PollDelayMs, ct);

            try
            {
                var result = await client.GetRunAsync(intentId, tenantId, actorId, correlationId, ct);
                if (result?.Run is not null)
                    return result;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "CollectiveLiveOrchestrator: poll attempt {Attempt} failed for {IntentId}", attempt, intentId);
            }
        }

        _logger.LogWarning("CollectiveLiveOrchestrator: polling exhausted for intent {IntentId}", intentId);
        return null;
    }

    // ── Mapping ──────────────────────────────────────────────────────────────

    private static CognitiveDensityEnvelope MapToEnvelope(CollectiveCognitionSurfaces surfaces)
    {
        var perspectiveMap = new Dictionary<PerspectiveKind, PerspectiveReview>();

        if (surfaces.PerspectiveReviews is not null)
        {
            foreach (var (key, pr) in surfaces.PerspectiveReviews)
            {
                if (!Enum.TryParse<PerspectiveKind>(key, ignoreCase: true, out var kind))
                    continue;

                perspectiveMap[kind] = new PerspectiveReview(
                    kind,
                    (pr.Findings ?? []).Select(f => new PerspectiveFinding(
                        f.FindingId,
                        f.Category,
                        f.Narrative,
                        ParseSeverity(f.Severity))).ToList(),
                    pr.Summary ?? string.Empty);
            }
        }

        var cr = surfaces.ContradictionReview;
        // DOCTRINE: always false, regardless of API response
        var contradiction = new ContradictionReview(
            cr?.CounterPlanNarrative ?? DegradedSummary,
            CounterPlanIsExecutable: false,
            IsExecutable: false);

        var cp = surfaces.ConfidenceProfile;
        var confidence = new ConfidenceProfile(
            cp?.Model               ?? DegradedModel,
            cp?.Epistemic           ?? string.Empty,
            cp?.EvidenceSupport     ?? string.Empty,
            cp?.ContradictionDensity ?? string.Empty,
            cp?.CalibrationVersion  ?? string.Empty);

        var gr = surfaces.ReasoningGraphRef;
        var graphRef = new ReasoningGraphRef(
            gr?.GraphId  ?? string.Empty,
            gr?.NodeCount ?? 0,
            gr?.EdgeCount ?? 0);

        return new CognitiveDensityEnvelope(
            new BranchPerspectiveReview(perspectiveMap, surfaces.WitnessSignature),
            contradiction,
            confidence,
            graphRef);
    }

    private static FindingSeverity ParseSeverity(string? s) =>
        s?.ToUpperInvariant() switch
        {
            "CRITICAL" => FindingSeverity.Critical,
            "WARNING"  => FindingSeverity.Warning,
            _          => FindingSeverity.Info,
        };

    private static CognitiveDensityEnvelope BuildDegradedEnvelope() =>
        new(
            new BranchPerspectiveReview(new Dictionary<PerspectiveKind, PerspectiveReview>()),
            new ContradictionReview(DegradedSummary, CounterPlanIsExecutable: false, IsExecutable: false),
            new ConfidenceProfile(DegradedModel, string.Empty, string.Empty, string.Empty, string.Empty),
            new ReasoningGraphRef(string.Empty, 0, 0));
}
