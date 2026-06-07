namespace BioStack.Api.Endpoints;

/// <summary>
/// Evidence gate sub-document returned in the promotion-preview response.
/// </summary>
public sealed record PromotionPreviewEvidenceGateDto(
    bool Passed,
    string? Tier,
    int CitationCount,
    bool MechanismSummaryPresent,
    IReadOnlyList<string> FailureReasons);

/// <summary>
/// RR-2: Promotion dry-run preview response body.
/// WouldWrite is always false — this endpoint never persists anything.
/// </summary>
public sealed record PromotionPreviewResponse(
    string ArtifactId,
    bool CanPromote,
    string ReviewState,
    bool TargetAssigned,
    string? TargetCanonicalName,
    Guid? ResolvedTargetKnowledgeEntryId,
    bool AlreadyPromoted,
    Guid? PromotedKnowledgeEntryId,
    PromotionPreviewEvidenceGateDto EvidenceGate,
    IReadOnlyList<string> BlockingReasons,
    bool WouldWrite);
