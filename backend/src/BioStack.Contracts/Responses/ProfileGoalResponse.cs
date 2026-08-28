namespace BioStack.Contracts.Responses;

public sealed record ProfileGoalResponse(
    Guid Id,
    Guid ProfileId,
    string GoalDefinitionId,
    GoalDefinitionResponse GoalDefinition,
    DateTime CreatedAtUtc
);
