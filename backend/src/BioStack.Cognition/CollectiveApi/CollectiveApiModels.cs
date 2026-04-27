namespace BioStack.Cognition.CollectiveApi;

using System.Text.Json.Serialization;

// ── Submit request ─────────────────────────────────────────────────────────
// POST /api/collective/live-runs
// Required fields per integration guide: objective, tenantId, actorId,
// actorType, correlationId.

internal sealed record CollectiveSubmitRequest(
    [property: JsonPropertyName("objective")]    string Objective,
    [property: JsonPropertyName("tenantId")]     string TenantId,
    [property: JsonPropertyName("actorId")]      string ActorId,
    [property: JsonPropertyName("actorType")]    string ActorType,
    [property: JsonPropertyName("correlationId")] string CorrelationId,
    [property: JsonPropertyName("context")]      string? Context = null,
    // IntentId is always supplied by CollectiveLiveOrchestrator from the intent value object.
    [property: JsonPropertyName("intentId")]     string IntentId = "");

// ── Live-run response ──────────────────────────────────────────────────────
// POST and GET /api/collective/live-runs return CollectiveLiveRun.
// dataMode: "LIVE" | "MOCK" | "DEGRADED"

internal sealed record CollectiveLiveRunResponse(
    [property: JsonPropertyName("dataMode")]        string? DataMode,
    [property: JsonPropertyName("retrievalMode")]   string? RetrievalMode,
    [property: JsonPropertyName("run")]             CollectiveRunRecord? Run,
    [property: JsonPropertyName("operatorMessages")] IReadOnlyList<CollectiveOperatorMessage>? OperatorMessages);

internal sealed record CollectiveRunRecord(
    [property: JsonPropertyName("intentId")]       string IntentId,
    [property: JsonPropertyName("correlationId")]  string CorrelationId,
    [property: JsonPropertyName("cognitionSurfaces")] CollectiveCognitionSurfaces? CognitionSurfaces);

// ── Cognition surfaces ─────────────────────────────────────────────────────
// Returned inside run.cognitionSurfaces.
// Keys in perspectiveReviews: "Optimizer", "Skeptic", "Regulator", "Historian".

internal sealed record CollectiveCognitionSurfaces(
    [property: JsonPropertyName("perspectiveReviews")]
    IReadOnlyDictionary<string, CollectivePerspectiveReview>? PerspectiveReviews,

    [property: JsonPropertyName("contradictionReview")]
    CollectiveContradictionSurface? ContradictionReview,

    [property: JsonPropertyName("confidenceProfile")]
    CollectiveConfidenceSurface? ConfidenceProfile,

    [property: JsonPropertyName("reasoningGraphRef")]
    CollectiveGraphRef? ReasoningGraphRef);

internal sealed record CollectivePerspectiveReview(
    [property: JsonPropertyName("kind")]     string Kind,
    [property: JsonPropertyName("findings")] IReadOnlyList<CollectivePerspectiveFinding>? Findings,
    [property: JsonPropertyName("summary")] string? Summary);

internal sealed record CollectivePerspectiveFinding(
    [property: JsonPropertyName("findingId")]  string FindingId,
    [property: JsonPropertyName("category")]   string Category,
    [property: JsonPropertyName("narrative")]  string Narrative,
    [property: JsonPropertyName("severity")]   string? Severity);

internal sealed record CollectiveContradictionSurface(
    [property: JsonPropertyName("counterPlanNarrative")]    string? CounterPlanNarrative,
    // NOTE: these are read from the API response but always overridden to false
    // per BioStack doctrine before being placed in CognitiveDensityEnvelope.
    [property: JsonPropertyName("counterPlanIsExecutable")] bool CounterPlanIsExecutable,
    [property: JsonPropertyName("isExecutable")]            bool IsExecutable);

internal sealed record CollectiveConfidenceSurface(
    [property: JsonPropertyName("model")]                string? Model,
    [property: JsonPropertyName("epistemic")]            string? Epistemic,
    [property: JsonPropertyName("evidenceSupport")]      string? EvidenceSupport,
    [property: JsonPropertyName("contradictionDensity")] string? ContradictionDensity,
    [property: JsonPropertyName("calibrationVersion")]   string? CalibrationVersion);

internal sealed record CollectiveGraphRef(
    [property: JsonPropertyName("graphId")]   string? GraphId,
    [property: JsonPropertyName("nodeCount")] int NodeCount,
    [property: JsonPropertyName("edgeCount")] int EdgeCount);

internal sealed record CollectiveOperatorMessage(
    [property: JsonPropertyName("code")]     string Code,
    [property: JsonPropertyName("message")]  string Message,
    [property: JsonPropertyName("severity")] string? Severity);
