namespace BioStack.Contracts.Requests;

/// <summary>
/// Input for POST /api/v1/stack-review/envelope.
/// The endpoint reviews the explicit, caller-supplied envelope payload. It does not resolve or bind
/// the deliberation to a persisted BioStack protocol.
/// </summary>
public sealed record StackReviewRequest(
    /// <summary>Explicit ad-hoc review payload.</summary>
    StackReviewEnvelopePayload? Payload);

public sealed record StackReviewEnvelopePayload(
    string Goal,
    IReadOnlyList<StackReviewCompoundRef> Compounds,
    IReadOnlyList<string> Pathways,
    IReadOnlyList<StackReviewDeterministicFinding> DeterministicFindings,
    IReadOnlyList<string> KnownPatternNames,
    decimal ProviderReviewPressure);

public sealed record StackReviewCompoundRef(
    string Slug,
    string DisplayName,
    string Form,
    string Category,
    string EvidenceTier);

public sealed record StackReviewDeterministicFinding(
    string Code,
    string Category,
    string Narrative,
    IReadOnlyList<string> CompoundSlugs,
    decimal RiskScoreContribution);
