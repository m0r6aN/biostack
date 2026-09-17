namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;
using System.Text.Json.Serialization;

public sealed record InteractionFlagResponse(
    Guid Id,
    List<string> CompoundNames,
    OverlapType OverlapType,
    string PathwayTag,
    string Description,
    string EvidenceConfidence,
    DateTime CreatedAtUtc
);

/// <summary>
/// Reduced pair-signal projection without direction, pathway or reasoning.
/// Severity is explicitly null because the current flag has no qualified severity measurement.
/// Positive classifications are signals too; presence does not assert a hazard.
/// </summary>
public sealed record ReducedInteractionFlagResponse(
    Guid Id,
    List<string> CompoundNames,
    [property: JsonIgnore(Condition = JsonIgnoreCondition.Never)] string? Severity,
    DateTime CreatedAtUtc
);
