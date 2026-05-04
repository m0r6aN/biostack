namespace BioStack.Contracts.Responses;

public sealed record AnalyzeProtocolResponse(
    List<ProtocolEntryResponse> Protocol,
    int Score,
    ProtocolScoreExplanationResponse ScoreExplanation,
    List<ProtocolIssueResponse> Issues,
    List<ProtocolSuggestionResponse> Suggestions,
    List<ProtocolBlendExpansionResponse> DecomposedBlends,
    List<string> UnknownCompounds,
    CounterfactualResultDto Counterfactuals,
    string InputType,
    string? SourceName,
    List<string> ExtractionWarnings,
    List<string> ParserWarnings,
    bool LowConfidenceExtraction,
    string? ExtractedTextPreview,
    List<ProtocolIngestionArtifactResponse> Artifacts,
    List<KnownPatternResponse> KnownPatterns,
    List<EmergentPatternResponse> EmergentPatterns,
    StackReviewBoardResponse? StackReviewBoard);

public sealed record StackReviewBoardResponse(
    Dictionary<string, PerspectiveReviewResponse> BranchPerspectiveReview,
    ContradictionReviewResponse ContradictionReview,
    ConfidenceProfileResponse ConfidenceProfile,
    ReasoningGraphRefResponse ReasoningGraphRef);

public sealed record PerspectiveReviewResponse(
    string Kind,
    List<PerspectiveFindingResponse> Findings,
    string Summary);

public sealed record PerspectiveFindingResponse(
    string FindingId,
    string Category,
    string Narrative,
    string Severity);

public sealed record ContradictionReviewResponse(
    string CounterPlanNarrative,
    bool CounterPlanIsExecutable,
    bool IsExecutable);

public sealed record ConfidenceProfileResponse(
    string Model,
    string Epistemic,
    string EvidenceSupport,
    string ContradictionDensity,
    string CalibrationVersion);

public sealed record ReasoningGraphRefResponse(
    string GraphId,
    int NodeCount,
    int EdgeCount);

public sealed record KnownPatternResponse(
    string PatternId,
    string Name,
    List<string> MatchedCompoundSlugs,
    string Description);

public sealed record EmergentPatternResponse(
    string Id,
    string Title,
    string PatternType,
    List<string> Compounds,
    List<string> Pathways,
    List<string> Mechanisms,
    string Confidence,
    string Basis,
    string Explanation,
    List<string> EvidenceNotes,
    string UserFacingLabel);

public sealed record ProtocolIngestionArtifactResponse(
    string Kind,
    string Label,
    string Preview);

public sealed record ProtocolEntryResponse(
    string CompoundName,
    double Dose,
    string Unit,
    string Frequency,
    string Duration);

public sealed record ProtocolIssueResponse(
    string Type,
    string Message,
    List<string> Compounds);

public sealed record ProtocolSuggestionResponse(
    string Type,
    string Message,
    List<string> Compounds);

public sealed record ProtocolScoreExplanationResponse(
    int BaseScore,
    int Synergy,
    int Redundancy,
    int Interference);

public sealed record ProtocolBlendExpansionResponse(
    string BlendName,
    List<string> Components);

public sealed record CounterfactualResultDto(
    int BaselineScore,
    List<InteractionCounterfactualResponse> BestRemoveOne,
    List<InteractionSwapRecommendationResponse> BestSwapOne,
    SimplifiedProtocolResponse? BestSimplifiedProtocol,
    List<GoalAwareOptimizationResponse> GoalAwareOptions);

public sealed record SimplifiedProtocolResponse(
    List<ProtocolEntryResponse> Compounds,
    int Score,
    List<string> Removed,
    List<string> Reasons);

public sealed record GoalAwareOptimizationResponse(
    string Goal,
    List<ProtocolEntryResponse> Compounds,
    int Score,
    List<string> Reasons);
