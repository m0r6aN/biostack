namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;

public sealed record NormalizedProtocol(
    List<NormalizedProtocolCompound> Compounds,
    List<ProtocolBlendExpansionResponse> DecomposedBlends);

public sealed record NormalizedProtocolCompound(
    string CanonicalName,
    double DoseMcg,
    string OriginalUnit,
    string Frequency,
    string Duration,
    string GoalSignals,
    bool IsKnownCompound);

public sealed record AnalysisContext(
    string Goal,
    string Sex,
    string AgeBand,
    string WeightBand,
    List<string> ExistingStackContext,
    string ParserVersion,
    string KnowledgeVersion,
    string ScoringVersion);

public sealed record OptimizationContext(
    string Goal,
    int MaxCompounds,
    List<string> RequiredCompoundIds,
    List<string> ExcludedCompoundIds,
    List<string> ExistingProfileContext,
    string OptimizationMode,
    string ScoringVersion,
    string KnowledgeVersion,
    string CounterfactualVersion,
    int BeamWidth = 3,
    int ScoreFloor = 45);

public sealed record ParsedProtocolCacheDto(
    List<ProtocolEntryResponse> Protocol,
    List<ProtocolBlendExpansionResponse> DecomposedBlends);

public sealed record ProtocolAnalysisCacheDto(
    int Score,
    ProtocolScoreExplanationResponse ScoreExplanation,
    List<ProtocolIssueResponse> Issues,
    List<string> UnknownCompounds);

public sealed record ProtocolCandidate(
    string CanonicalName,
    KnowledgeEntry KnowledgeEntry,
    double GoalAlignmentScore,
    double PathwaySimilarityScore,
    string Reason);
