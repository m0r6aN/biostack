namespace BioStack.Contracts.Requests;

public sealed record OverlapCheckRequest(
    List<string> CompoundNames
);
