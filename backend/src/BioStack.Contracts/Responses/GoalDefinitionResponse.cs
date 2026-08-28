namespace BioStack.Contracts.Responses;

public sealed record GoalDefinitionResponse(
    string Id,
    string Name,
    string Category,
    string Description,
    bool IsActive
);
