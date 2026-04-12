namespace BioStack.Contracts.Responses;

public sealed record MissionControlResponse(
    ProtocolRunResponse? ActiveRun,
    ProtocolRunResponse? LatestClosedRun,
    MissionControlReviewSummaryResponse? LatestReviewSummary,
    MissionControlEvolutionResponse? RecentEvolution,
    MissionControlCheckInSignalResponse LatestCheckInSignal,
    List<MissionControlObservationSignalResponse> ObservationSignals,
    List<ProtocolReviewTimelineEventResponse> CohesionTimeline
);

public sealed record MissionControlReviewSummaryResponse(
    Guid ProtocolId,
    Guid LineageRootProtocolId,
    string LineageName,
    string Cue,
    string SignalType,
    int VersionCount,
    int RunCount,
    int CheckInCount
);

public sealed record MissionControlEvolutionResponse(
    Guid ProtocolId,
    Guid? ParentProtocolId,
    Guid? EvolvedFromRunId,
    string Label,
    string Summary,
    DateTime OccurredAtUtc,
    List<ProtocolVersionChangeResponse> Changes
);

public sealed record MissionControlCheckInSignalResponse(
    Guid? CheckInId,
    Guid? ProtocolRunId,
    DateTime? Date,
    string Cue,
    int AttachedCheckInCount,
    bool HasObservationGap
);

public sealed record MissionControlObservationSignalResponse(
    string Type,
    string Severity,
    string? Metric,
    string Detail
);
