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
    List<ProtocolIngestionArtifactResponse> Artifacts);

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
