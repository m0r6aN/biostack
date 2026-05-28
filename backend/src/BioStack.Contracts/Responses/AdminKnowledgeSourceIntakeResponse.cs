namespace BioStack.Contracts.Responses;

/// <summary>
/// Response returned when an admin intake request is accepted into the Knowledge Worker queue.
/// </summary>
public sealed record AdminKnowledgeSourceIntakeResponse(
    Guid IntakeRequestId,
    string Status,
    DateTimeOffset CreatedAtUtc,
    string Message);

/// <summary>
/// Snapshot view for admin intake request tracking.
/// </summary>
public sealed record KnowledgeSourceIntakeStatusResponse(
    Guid IntakeRequestId,
    string SourceType,
    string SourceUrl,
    string Status,
    string? FailureReason,
    DateTimeOffset CreatedAtUtc,
    DateTimeOffset? UpdatedAtUtc);
