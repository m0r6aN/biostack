namespace BioStack.Contracts.Requests;

/// <summary>
/// Input for POST /api/v1/stack-review/envelope.
/// Supply either ProtocolId (resolved server-side) or an explicit envelope payload.
/// </summary>
public sealed record StackReviewRequest(
    /// <summary>When provided, the server loads InteractionIntelligence for this protocol.</summary>
    Guid? ProtocolId,
    /// <summary>Direct envelope payload, overrides ProtocolId if both are supplied.</summary>
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
