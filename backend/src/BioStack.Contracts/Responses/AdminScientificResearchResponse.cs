namespace BioStack.Contracts.Responses;

public sealed record AdminScientificResearchJobResponse(
    string JobId,
    string ResearchRequestId,
    string Workflow,
    string Status,
    string CorrelationId,
    DateTimeOffset SubmittedAtUtc,
    string? ProgressMessage = null,
    bool Partial = false,
    string? ErrorCode = null,
    string? ErrorMessage = null);

public sealed record AdminScientificResearchStageResponse(
    string ArtifactId,
    string ReviewState,
    string SourceType,
    string Provider,
    string JobId,
    string Workflow,
    bool Partial,
    string CreatedAtUtc);
