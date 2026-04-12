namespace BioStack.Contracts.Responses;

public sealed record ProtocolReviewResponse(
    Guid LineageRootProtocolId,
    Guid RequestedProtocolId,
    string LineageName,
    List<ProtocolReviewVersionResponse> Versions,
    List<ProtocolReviewSectionResponse> Sections,
    List<ProtocolReviewTimelineEventResponse> Timeline,
    List<string> SafetyNotes
);

public sealed record ProtocolReviewVersionResponse(
    Guid ProtocolId,
    string Name,
    int Version,
    bool IsDraft,
    Guid? ParentProtocolId,
    Guid? EvolvedFromRunId,
    string EvolutionContext,
    DateTime CreatedAtUtc,
    ProtocolVersionDiffResponse? VersionDiff,
    List<ProtocolReviewRunResponse> Runs
);

public sealed record ProtocolReviewRunResponse(
    ProtocolRunResponse Run,
    ProtocolRunSummaryResponse Summary,
    List<ProtocolRunObservationResponse> Observations,
    List<ActualTrendResponse> Trends,
    List<ProtocolRunInsightResponse> Insights
);

public sealed record ProtocolReviewSectionResponse(
    string Type,
    string Title,
    string Summary,
    List<string> Evidence
);

public sealed record ProtocolReviewTimelineEventResponse(
    DateTime OccurredAtUtc,
    string EventType,
    string Label,
    Guid? ProtocolId,
    Guid? RunId,
    Guid? CheckInId,
    Guid? ComputationId,
    Guid? ReviewCompletedEventId,
    string Detail
);
