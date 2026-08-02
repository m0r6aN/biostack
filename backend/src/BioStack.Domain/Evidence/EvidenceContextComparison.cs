namespace BioStack.Domain.Evidence;

/// <summary>
/// Class B evidence-comparison result. Deterministic; no LLM required.
/// Language must remain non-prescriptive (Guidance Content Contract Class B).
/// </summary>
public sealed record EvidenceContextComparison(
    string SubjectName,
    ProtocolExposure Exposure,
    IReadOnlyList<string> RiskSignals,
    decimal? ClosestInitiationMin,
    decimal? ClosestInitiationMax,
    decimal? ClosestMaintenanceMin,
    decimal? ClosestMaintenanceMax,
    decimal? HighestStudiedExposure,
    string NormalizedUnit,
    decimal? UnitNormalizedUserAmount,
    decimal? TimesAboveInitiationMin,
    decimal? TimesAboveInitiationMax,
    bool RouteMismatch,
    bool FrequencyMismatch,
    bool UnitMismatchSuspected,
    bool DecimalErrorSuspected,
    IReadOnlyList<string> SourceReferences,
    IReadOnlyList<string> Statements,
    IReadOnlyList<string> UncertaintyMarkers);

/// <summary>
/// Stable risk-signal codes for evidence comparison.
/// </summary>
public static class EvidenceComparisonSignals
{
    public const string NoReviewedInitiationMatch = "NO_REVIEWED_INITIATION_MATCH";
    public const string AboveReviewedInitiationRange = "ABOVE_REVIEWED_INITIATION_RANGE";
    public const string AboveHighestReviewedExposure = "ABOVE_HIGHEST_REVIEWED_EXPOSURE";
    public const string BelowReviewedRange = "BELOW_REVIEWED_RANGE";
    public const string RouteNotStudied = "ROUTE_NOT_STUDIED";
    public const string FrequencyNotStudied = "FREQUENCY_NOT_STUDIED";
    public const string UnitMismatchSuspected = "UNIT_MISMATCH_SUSPECTED";
    public const string DecimalErrorSuspected = "DECIMAL_ERROR_SUSPECTED";
    public const string EvidenceLimitedToAnimals = "EVIDENCE_LIMITED_TO_ANIMALS";
    public const string EvidenceLimitedToCaseReports = "EVIDENCE_LIMITED_TO_CASE_REPORTS";
    public const string ConflictingHumanEvidence = "CONFLICTING_HUMAN_EVIDENCE";
    public const string NoHumanEvidence = "NO_HUMAN_EVIDENCE";
    public const string ExactMatchStudyContext = "EXACT_MATCH_STUDY_CONTEXT";
}
