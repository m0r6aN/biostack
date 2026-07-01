namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;

/// <summary>
/// Builds the observational Protocol Operations Report from existing protocol,
/// compound, check-in, and timeline read models. Purely factual counts and a
/// bounded recent-activity log — no recommendations, dosing guidance,
/// diagnosis, or protocol-change narrative. Independent of, and not a
/// replacement for, the offline-only Protocol Intelligence evaluation.
/// </summary>
public sealed class ProtocolOperationsReportService : IProtocolOperationsReportService
{
    private const int RecentEventsLimit = 10;

    private static readonly EventType[] OperationalEventTypes =
    [
        EventType.CompoundStarted,
        EventType.CompoundEnded,
        EventType.CheckInCreated,
        EventType.ProtocolPhaseStarted,
        EventType.ProtocolPhaseEnded,
    ];

    private readonly IProtocolRepository _protocolRepository;
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IProtocolPortalBaseline _baseline;

    public ProtocolOperationsReportService(
        IProtocolRepository protocolRepository,
        ICompoundRecordRepository compoundRepository,
        ICheckInRepository checkInRepository,
        ITimelineEventRepository timelineRepository,
        IProtocolPortalBaseline baseline)
    {
        _protocolRepository = protocolRepository;
        _compoundRepository = compoundRepository;
        _checkInRepository = checkInRepository;
        _timelineRepository = timelineRepository;
        _baseline = baseline;
    }

    public async Task<ProtocolOperationsReport> GetReportAsync(Guid profileId, CancellationToken ct = default)
    {
        var protocols = (await _protocolRepository.GetByPersonIdAsync(profileId, ct)).ToList();
        var activeProtocol = protocols
            .Where(p => !p.IsDraft)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefault()
            ?? protocols.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();

        var compounds = (await _compoundRepository.GetByPersonIdAsync(profileId, ct)).ToList();
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(profileId, ct)).ToList();
        var timeline = (await _timelineRepository.GetByPersonIdAsync(profileId, ct)).ToList();

        var activeCompoundsCount = compounds.Count(c => c.Status == CompoundStatus.Active);
        var loggedDosesCount = timeline.Count(IsDoseLogEvent);
        var monitoringEntryCount = timeline.Count(e => OperationalEventTypes.Contains(e.EventType));

        // No per-protocol evidence-reference tracking exists yet; honest empty
        // list rather than fabricating references.
        var evidenceReferences = Array.Empty<ProtocolOperationsEvidenceReference>();
        var milestoneCount = _baseline.Milestones.Count;

        var latestActivityUtc = timeline.Count > 0
            ? timeline.Max(e => e.OccurredAtUtc)
            : (DateTime?)null;

        var recentEvents = timeline
            .Where(e => OperationalEventTypes.Contains(e.EventType) || IsDoseLogEvent(e))
            .OrderByDescending(e => e.OccurredAtUtc)
            .Take(RecentEventsLimit)
            .Select(e => new ProtocolOperationsEvent(e.EventType.ToString(), e.OccurredAtUtc, e.Title))
            .ToList();

        var warnings = new List<string>();
        if (activeProtocol is null)
            warnings.Add("No active protocol found for this profile.");
        if (activeCompoundsCount == 0)
            warnings.Add("No active compounds recorded.");
        if (checkIns.Count == 0)
            warnings.Add("No check-ins recorded yet.");
        if (evidenceReferences.Length == 0)
            warnings.Add("No evidence references recorded for this protocol yet.");

        var summary = new ProtocolOperationsSummary(
            activeCompoundsCount,
            loggedDosesCount,
            checkIns.Count,
            monitoringEntryCount,
            milestoneCount,
            evidenceReferences.Length,
            latestActivityUtc);

        return new ProtocolOperationsReport(
            profileId,
            activeProtocol?.Id,
            DateTime.UtcNow,
            summary,
            recentEvents,
            evidenceReferences,
            warnings);
    }

    private static bool IsDoseLogEvent(BioStack.Domain.Entities.TimelineEvent e)
        => e.EventType == EventType.NoteAdded && e.RelatedEntityType == ProtocolPortalService.DoseLogMarker;
}

public interface IProtocolOperationsReportService
{
    Task<ProtocolOperationsReport> GetReportAsync(Guid profileId, CancellationToken ct = default);
}
