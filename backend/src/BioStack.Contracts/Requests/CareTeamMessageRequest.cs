namespace BioStack.Contracts.Requests;

/// <summary>A free-text message routed from a client to their care team.</summary>
public sealed record CareTeamMessageRequest(string Message);
