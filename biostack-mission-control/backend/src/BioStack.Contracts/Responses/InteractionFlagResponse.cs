namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record InteractionFlagResponse(
    Guid Id,
    List<string> CompoundNames,
    OverlapType OverlapType,
    string PathwayTag,
    string Description,
    string EvidenceConfidence,
    DateTime CreatedAtUtc
);
