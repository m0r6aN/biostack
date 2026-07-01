namespace BioStack.Contracts.Responses;

// ─── Protocol Operations Report contract ────────────────────────────────────
// Observational summary of a profile's protocol state and activity log:
// counts, a bounded recent-activity log, and honest missing-data/review-needed
// warnings. Strictly non-medical — no recommendations, dosing instructions,
// diagnosis, or protocol-change guidance. This is independent of Protocol
// Intelligence (offline/build-time artifact evaluation); see
// docs/architecture/protocol-intelligence-offline-boundary.md.

public sealed record ProtocolOperationsEvidenceReference(
    string Label,
    string? Url,
    string? Note);

public sealed record ProtocolOperationsEvent(
    string EventType,
    DateTime OccurredAtUtc,
    string Description);

public sealed record ProtocolOperationsSummary(
    int ActiveCompoundsCount,
    int LoggedDosesCount,
    int CheckInCount,
    int MonitoringEntryCount,
    int MilestoneCount,
    int EvidenceReferenceCount,
    DateTime? LatestActivityUtc);

public sealed record ProtocolOperationsReport(
    Guid ProfileId,
    Guid? ProtocolId,
    DateTime GeneratedAtUtc,
    ProtocolOperationsSummary Summary,
    IReadOnlyList<ProtocolOperationsEvent> RecentEvents,
    IReadOnlyList<ProtocolOperationsEvidenceReference> EvidenceReferences,
    IReadOnlyList<string> Warnings);
