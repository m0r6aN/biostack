namespace BioStack.Contracts.Responses;

public sealed record ProtocolIntelligenceResponse(
    string Status,
    IReadOnlyList<ProtocolIntelligencePhaseMapItem> PhaseMap,
    IReadOnlyList<ProtocolIntelligenceRelationshipCard> Relationships,
    IReadOnlyList<ProtocolIntelligenceAmbiguitySignal> AmbiguitySignals,
    IReadOnlyList<ProtocolIntelligenceSourceQualityWarning> SourceQualityWarnings,
    IReadOnlyList<ProtocolIntelligenceHighRiskWarning> HighRiskWarnings,
    IReadOnlyList<string> Unknowns,
    IReadOnlyList<string> SafetyNotes,
    IReadOnlyList<ProtocolIntelligenceUpgradeHook> UpgradeHooks);

public sealed record ProtocolIntelligencePhaseMapItem(
    string Phase,
    string Label,
    string EvidenceTier,
    string Confidence,
    int SourceRefsCount,
    string ReviewStatus,
    string UserFacingBoundary);

public sealed record ProtocolIntelligenceRelationshipCard(
    string RelationshipType,
    string Subject,
    string Object,
    string EvidenceTier,
    string Confidence,
    int SourceRefsCount,
    string ReviewStatus,
    string UserFacingExplanation,
    string UserFacingBoundary);

public sealed record ProtocolIntelligenceAmbiguitySignal(
    string SymptomOrOutcome,
    string OnsetWindow,
    IReadOnlyList<string> RecentChanges,
    IReadOnlyList<string> OverlapDomains,
    string EvidenceTier,
    string Confidence,
    int SourceRefsCount,
    string ReviewStatus,
    string UserFacingBoundary);

public sealed record ProtocolIntelligenceSourceQualityWarning(
    string Subject,
    string SourceClass,
    IReadOnlyList<string> BlockedOutputs,
    string EvidenceTier,
    string Confidence,
    int SourceRefsCount,
    string ReviewStatus,
    string UserFacingBoundary);

public sealed record ProtocolIntelligenceHighRiskWarning(
    string Category,
    IReadOnlyList<string> RequiredWarnings,
    IReadOnlyList<string> BlockedOutputs,
    string EvidenceTier,
    string Confidence,
    int SourceRefsCount,
    string ReviewStatus,
    string UserFacingBoundary);

public sealed record ProtocolIntelligenceUpgradeHook(
    string RequiredTier,
    string FeatureCode,
    string Message);

public sealed record ProtocolIntelligenceContractsResponse(
    IReadOnlyDictionary<string, string> ArtifactVersions,
    IReadOnlyList<string> SupportedRelationshipIds,
    IReadOnlyList<string> BlockedOutputIds,
    IReadOnlyList<string> AvailableObservabilityModules);
