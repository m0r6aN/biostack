namespace BioStack.Application.Services;

/// <summary>
/// Optional filter criteria for listing staged transcript candidate review records.
/// Null fields are ignored (no constraint applied). Multiple non-null fields are ANDed.
/// </summary>
public sealed record TranscriptCandidateReviewFilter(
    string? ReviewState = null,
    bool? IsPromoted = null,
    bool? IsTargetAssigned = null);
