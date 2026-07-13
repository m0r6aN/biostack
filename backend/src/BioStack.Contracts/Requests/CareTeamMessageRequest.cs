namespace BioStack.Contracts.Requests;

/// <summary>
/// A free-text note stored on the client's protocol record. The compatibility endpoint does not
/// identify a recipient, deliver a message, or send a notification.
/// </summary>
public sealed record CareTeamMessageRequest(string Message);
