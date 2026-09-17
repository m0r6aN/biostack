namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record InteractionFlagResponse(
    Guid Id,
    List<string> CompoundNames,
    OverlapType OverlapType,
    string PathwayTag,
    string Description,
    string EvidenceConfidence,
    DateTime CreatedAtUtc
);

/// <summary>
/// Reduced projection of <see cref="InteractionFlagResponse"/> for a caller without the
/// reviewed_relationship_graph entitlement (owner ruling 2026-09-16, extended 2026-09-17 to
/// POST /api/v1/knowledge/overlap-check). Carries which pair was flagged and how — the same
/// public boundary already drawn for <c>InteractionIntelligenceResponse</c> via
/// <see cref="ReducedInteractionIntelligenceResponse"/> — but never <c>Description</c> (the
/// per-pair reasoning sentence) or <c>EvidenceConfidence</c> (free-text confidence derived from
/// that same unsourced reasoning).
/// </summary>
public sealed record ReducedInteractionFlagResponse(
    Guid Id,
    List<string> CompoundNames,
    OverlapType OverlapType,
    string PathwayTag,
    DateTime CreatedAtUtc
);
