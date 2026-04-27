namespace BioStack.Cognition.Models;

/// <summary>
/// BioStack-specific input contract for the Stack Review Board.
/// All fields are observational/educational. No field is effect-bearing.
/// </summary>
public sealed record StackDeliberationEnvelope(
    string Goal,
    IReadOnlyList<CompoundRef> Compounds,
    IReadOnlyList<string> Pathways,
    IReadOnlyDictionary<string, EvidenceTier> EvidenceTiers,
    IReadOnlyList<DeterministicFinding> DeterministicFindings,
    IReadOnlyList<KnownPattern> KnownPatterns,
    IReadOnlyList<string> MissingInputs,
    decimal ProviderReviewPressure,
    string SafetyBoundaryText);

public sealed record CompoundRef(
    string Slug,
    string DisplayName,
    string Form,
    string Category);

/// <summary>Evidence tier enum for BioStack.Cognition.Models — distinct from
/// BioStack.Domain.Enums.EvidenceTier which has a different value set.</summary>
public enum EvidenceTier { None, Anecdotal, Limited, Moderate, Strong }

public sealed record DeterministicFinding(
    string FindingId,
    string Code,
    string Category,
    string Narrative,
    IReadOnlyList<string> CompoundSlugs,
    IReadOnlyList<string> PathwayTags,
    decimal RiskScoreContribution,
    decimal UtilityScoreContribution,
    EvidenceTier EvidenceTier,
    string? QualifiesFindingId,
    string? ConflictsWithFindingId);

public sealed record KnownPattern(
    string PatternId,
    string Name,
    IReadOnlyList<string> MatchedCompoundSlugs,
    string Description);
