namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record CurrentStackIntelligenceResponse(
    Guid PersonId,
    List<StackCompoundIntelligenceResponse> ActiveCompounds,
    List<StackSignalResponse> Signals,
    List<PathwayOverlapResponse> SharedPathwayOverlap,
    List<EvidenceTierSummaryResponse> EvidenceTierSummary,
    List<StackCompoundIntelligenceResponse> UnresolvedCompounds,
    string Framing
);

public sealed record StackCompoundIntelligenceResponse(
    Guid CompoundRecordId,
    string Name,
    CompoundStatus Status,
    DateTime? StartDate,
    DateTime? EndDate,
    bool IsCanonical,
    Guid? KnowledgeEntryId,
    string CanonicalName,
    List<string> Pathways,
    EvidenceTier EvidenceTier,
    SchedulePreviewResponse? SchedulePreview
);

public sealed record StackSignalResponse(
    string Kind,
    string Severity,
    string Title,
    string Detail,
    List<string> CompoundNames,
    string Source
);

public sealed record PathwayOverlapResponse(
    string Pathway,
    List<string> CompoundNames
);

public sealed record EvidenceTierSummaryResponse(
    EvidenceTier EvidenceTier,
    int Count,
    List<string> CompoundNames
);
