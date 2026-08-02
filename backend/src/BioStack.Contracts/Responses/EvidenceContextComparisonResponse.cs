namespace BioStack.Contracts.Responses;

/// <summary>
/// Class B evidence-context comparison surface for protocol analysis.
/// Non-prescriptive; never a personal dosing instruction.
/// </summary>
public sealed record EvidenceContextComparisonResponse(
    string CompoundName,
    double EnteredAmount,
    string EnteredUnit,
    string? EnteredFrequency,
    string? EnteredRoute,
    IReadOnlyList<string> RiskSignals,
    double? ReviewedInitiationMin,
    double? ReviewedInitiationMax,
    double? TimesAboveInitiationMin,
    double? TimesAboveInitiationMax,
    string NormalizedUnit,
    IReadOnlyList<string> Statements,
    IReadOnlyList<string> UncertaintyMarkers,
    IReadOnlyList<string> SourceReferences,
    bool ComparisonAvailable);
