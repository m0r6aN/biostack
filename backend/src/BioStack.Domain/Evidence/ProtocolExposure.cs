namespace BioStack.Domain.Evidence;

/// <summary>
/// A user-recorded or proposed protocol exposure for comparison against reviewed evidence.
/// Comparison is local to BioStack; this is not sent to the research sidecar.
/// </summary>
public sealed record ProtocolExposure(
    string SubjectName,
    decimal Amount,
    string Unit,
    string? Route = null,
    string? Frequency = null,
    string? RoleHint = null);
