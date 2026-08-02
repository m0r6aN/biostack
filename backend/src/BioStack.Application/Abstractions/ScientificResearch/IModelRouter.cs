namespace BioStack.Application.Abstractions.ScientificResearch;

/// <summary>
/// Execution-policy model router. Selects approved models; never promotes evidence or changes protocols.
/// </summary>
public interface IModelRouter
{
    Task<ModelRouteDecision> SelectAsync(
        ModelRoutingRequest request,
        CancellationToken cancellationToken = default);
}

public interface IInferenceProvider
{
    Task<InferenceResult> ExecuteAsync(
        InferenceRequest request,
        CancellationToken cancellationToken = default);
}

public interface IModelRegistry
{
    Task<IReadOnlyList<ModelCapabilityProfile>> GetAvailableAsync(
        CancellationToken cancellationToken = default);
}

public interface IInferenceOutputValidator
{
    Task<InferenceValidationResult> ValidateAsync(
        InferenceResult result,
        InferenceValidationContract contract,
        CancellationToken cancellationToken = default);
}

public interface IContextCompressionProvider
{
    Task<CompressionResult> CompressAsync(
        CompressionRequest request,
        CancellationToken cancellationToken = default);
}

public interface IOriginalContextRetriever
{
    Task<OriginalContextResult> RetrieveAsync(
        OriginalContextRequest request,
        CancellationToken cancellationToken = default);
}

public sealed record ModelRoutingRequest(
    string TaskClass,
    EvidenceRiskClass EvidenceRiskClass,
    string DataClassification,
    bool LocalOnly,
    bool HostedFallbackPermitted,
    int? RequiredContextTokens,
    bool StructuredOutputRequired,
    string? OutputSchemaVersion);

public sealed record ModelRouteDecision(
    string DecisionId,
    string Provider,
    string ModelName,
    string? ModelDigest,
    string RoutingPolicyVersion,
    string Rationale,
    IReadOnlyList<string> RejectedCandidates);

public sealed record ModelCapabilityProfile(
    string Provider,
    string CanonicalModelName,
    string? ModelDigest,
    string? Quantization,
    int? AdvertisedContext,
    int? ValidatedContext,
    string ApprovalStatus);

public sealed record InferenceRequest(
    string RouteDecisionId,
    string TaskClass,
    string PromptVersion,
    string? OutputSchemaVersion,
    string InputPayloadHash,
    int MaxOutputTokens);

public sealed record InferenceResult(
    string ExecutionId,
    string Provider,
    string ModelName,
    string? ModelDigest,
    string RawOutput,
    bool Structured,
    IReadOnlyDictionary<string, string> Metrics);

public sealed record InferenceValidationContract(
    string SchemaVersion,
    bool RequireSourceLocations,
    bool RequireDeterministicUnitCheck);

public sealed record InferenceValidationResult(
    bool IsValid,
    IReadOnlyList<string> Errors);

public sealed record CompressionRequest(
    string TenantId,
    string ResearchJobId,
    string CorrelationId,
    string ContextSegmentId,
    string MessageRole,
    string ContentType,
    string ExactnessMode,
    string OriginalContent,
    string BioStackFullHash);

public sealed record CompressionResult(
    string CompressionExecutionId,
    string CompressedContent,
    int TokensBefore,
    int TokensAfter,
    bool Lossless,
    string? RetrievalHash,
    DateTimeOffset? RetrievalExpirationUtc,
    bool OriginalStoreConfirmed,
    string? BypassReason);

public sealed record OriginalContextRequest(
    string TenantId,
    string ResearchJobId,
    string CorrelationId,
    string RetrievalHash);

public sealed record OriginalContextResult(
    string OriginalContent,
    string BioStackFullHash,
    bool Found);
