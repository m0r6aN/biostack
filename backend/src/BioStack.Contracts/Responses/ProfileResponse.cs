namespace BioStack.Contracts.Responses;

using BioStack.Domain.Enums;

public sealed record ProfileResponse(
    Guid Id,
    string DisplayName,
    Sex Sex,
    int? Age,
    decimal Weight,
    string? GoalSummary,
    string? Notes,
    DateTime CreatedAtUtc,
    DateTime UpdatedAtUtc
);
