namespace BioStack.Contracts.Responses;

public sealed record StackScoreResponse(
    int Score,
    StackScoreBreakdownResponse Breakdown,
    List<string> Chips
);

public sealed record StackScoreBreakdownResponse(
    int Synergy,
    int Redundancy,
    int Conflicts,
    int Evidence
);
