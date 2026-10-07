namespace BioStack.Application.Abstractions.ScientificResearch;

/// <summary>
/// Execution modes for research and optional local acceleration.
/// </summary>
public enum ScientificExecutionMode
{
    Auto = 0,
    GpuPreferred = 1,
    GpuRequired = 2,
    CpuOnly = 3,
    HostedFallbackAllowed = 4,
}

public enum ResearchJobStatusCode
{
    Queued = 0,
    ResolvingIdentity = 1,
    GatheringEvidence = 2,
    Normalizing = 3,
    PendingReview = 4,
    Completed = 5,
    Failed = 6,
    Cancelled = 7,
    Partial = 8,
    RejectedByPolicy = 9,
}

public enum EvidenceRiskClass
{
    Low = 0,
    Medium = 1,
    High = 2,
}

/// <summary>
/// Provider-neutral execution profile. No CUDA/ToolUniverse types.
/// </summary>
public sealed record ScientificExecutionProfile(
    ScientificExecutionMode Mode,
    bool AllowGpu,
    bool AllowCpuFallback,
    bool AllowHostedFallback,
    long? MaximumGpuMemoryBytes,
    TimeSpan MaximumExecutionDuration,
    string? ApprovedModelProfile);

/// <summary>
/// Research request. Must not carry user health PII for the initial integration.
/// </summary>
public sealed record ScientificResearchRequest(
    string ResearchRequestId,
    string ResearchSubjectType,
    string SubjectName,
    IReadOnlyDictionary<string, string> KnownIdentifiers,
    string Workflow,
    IReadOnlyList<string> EvidenceCategories,
    IReadOnlyList<string> SourceAllowlist,
    int? MaximumSourceAgeDays,
    TimeSpan MaximumExecutionTime,
    int MaximumSourceCount,
    string CorrelationId,
    string RequestedByActor,
    string Purpose,
    ScientificExecutionProfile Execution,
    string DataClassification,
    string? TaskClass,
    EvidenceRiskClass EvidenceRiskClass,
    bool LocalInferencePermitted,
    bool HostedInferencePermitted,
    bool CompressionPermitted,
    bool CrossCheckRequired);

public sealed record ResearchJobHandle(
    string JobId,
    string ResearchRequestId,
    string Workflow,
    ResearchJobStatusCode Status,
    DateTimeOffset SubmittedAtUtc,
    string CorrelationId);

public sealed record ResearchJobStatus(
    string JobId,
    string ResearchRequestId,
    string Workflow,
    ResearchJobStatusCode Status,
    string? ProgressMessage,
    bool Partial,
    string? ErrorCode,
    string? ErrorMessage,
    DateTimeOffset SubmittedAtUtc,
    DateTimeOffset UpdatedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    string CorrelationId);

public sealed record ScientificResearchArtifact(
    string ResearchArtifactId,
    string JobId,
    string ResearchRequestId,
    string Provider,
    string ProviderVersion,
    string Workflow,
    string WorkflowVersion,
    string? ToolUniverseVersion,
    ResearchJobStatusCode Status,
    bool Partial,
    DateTimeOffset StartedAtUtc,
    DateTimeOffset? FinishedAtUtc,
    IReadOnlyList<string> ToolsInvoked,
    IReadOnlyList<string> Warnings,
    string? FailureDetails,
    string ExecutionDevice,
    IReadOnlyDictionary<string, string> Provenance);
