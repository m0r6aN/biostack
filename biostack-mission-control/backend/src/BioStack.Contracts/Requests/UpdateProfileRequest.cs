namespace BioStack.Contracts.Requests;

using BioStack.Domain.Enums;

public sealed record UpdateProfileRequest(
    string DisplayName,
    Sex Sex,
    decimal Weight,
    int? Age,
    string? GoalSummary,
    string? Notes
);
