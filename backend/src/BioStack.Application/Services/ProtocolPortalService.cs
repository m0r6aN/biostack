namespace BioStack.Application.Services;

using System.Globalization;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;

/// <summary>
/// Composes the client-facing Protocol Portal. Operational sections (overview,
/// stats, schedule, milestones) are driven by real domain data or resolve to
/// honest empty-states; educational/narrative sections come from the curated
/// baseline. Provenance is tracked internally via <see cref="PortalSection{T}"/>
/// and surfaced additively as sectionMeta.
/// </summary>
public sealed class ProtocolPortalService : IProtocolPortalService
{
    // Marker stored on TimelineEvent.RelatedEntityType so dose logs and care-team
    // messages can be persisted (real) without a new table/migration.
    public const string DoseLogMarker = "ProtocolPortalDoseLog";
    public const string CareTeamMessageMarker = "ProtocolPortalCareTeamMessage";

    private const int AdherenceWindowDays = 7;

    private readonly IProtocolRepository _protocolRepository;
    private readonly IProtocolPhaseRepository _phaseRepository;
    private readonly ICompoundRecordRepository _compoundRepository;
    private readonly ICheckInRepository _checkInRepository;
    private readonly ITimelineEventRepository _timelineRepository;
    private readonly IProtocolPortalBaseline _baseline;
    private readonly IOwnershipGuard _ownershipGuard;
    private readonly IFeatureGate _featureGate;

    public ProtocolPortalService(
        IProtocolRepository protocolRepository,
        IProtocolPhaseRepository phaseRepository,
        ICompoundRecordRepository compoundRepository,
        ICheckInRepository checkInRepository,
        ITimelineEventRepository timelineRepository,
        IProtocolPortalBaseline baseline,
        IOwnershipGuard ownershipGuard,
        IFeatureGate featureGate)
    {
        _protocolRepository = protocolRepository;
        _phaseRepository = phaseRepository;
        _compoundRepository = compoundRepository;
        _checkInRepository = checkInRepository;
        _timelineRepository = timelineRepository;
        _baseline = baseline;
        _ownershipGuard = ownershipGuard;
        _featureGate = featureGate;
    }

    // ── Tier gating ─────────────────────────────────────────────────────────
    // Per spec: Overview/stats/today/supplements/resources = Observer,
    // calendar-week/diet/milestones = Operator, monitoring = Commander.

    private async Task EnsureTierAsync(ProductTier required, string code, string surface, CancellationToken ct)
    {
        var tier = await _featureGate.GetCurrentTierAsync(ct);
        if (tier < required)
        {
            throw new FeatureLimitExceededException(
                code,
                $"{required} is required for {surface}.",
                tier,
                null);
        }
    }

    private Task EnsureOperatorAsync(string code, string surface, CancellationToken ct)
        => EnsureTierAsync(ProductTier.Operator, code, surface, ct);

    private Task EnsureCommanderAsync(string code, string surface, CancellationToken ct)
        => EnsureTierAsync(ProductTier.Commander, code, surface, ct);

    // ── Aggregate ───────────────────────────────────────────────────────────

    public async Task<ProtocolPortalResponse> GetPortalAsync(Guid profileId, CancellationToken ct = default)
    {
        var ctx = await LoadContextAsync(profileId, ct);
        await EnsureCommanderAsync(
            "portal_aggregate_commander",
            "the complete protocol portal aggregate",
            ct);
        var meta = new Dictionary<string, PortalSectionMetaResponse>();

        var overview = BuildOverviewSection(ctx);
        var stats = BuildStatsSection(ctx);
        var today = BuildDayScheduleSection(ctx, ctx.Today);
        var week = BuildWeekSection(ctx, StartOfWeek(ctx.Today));
        var diet = BuildDietSection();
        var supplements = BuildSupplementsSection();
        var monitoring = BuildMonitoringSection();
        var milestones = BuildMilestonesSection(ctx);
        var resources = BuildResourcesSection();

        var daySchedules = new Dictionary<string, DayScheduleResponse>();
        var weekStart = StartOfWeek(ctx.Today);
        for (var offset = 0; offset < 7; offset++)
        {
            var date = weekStart.AddDays(offset);
            var key = IsoDate(date);
            daySchedules[key] = BuildDayScheduleSection(ctx, date).Data!;
        }

        Record("overview", overview, meta);
        Record("stats", stats, meta);
        Record("today", today, meta);
        Record("week", week, meta);
        Record("diet", diet, meta);
        Record("supplements", supplements, meta);
        Record("monitoring", monitoring, meta);
        Record("milestones", milestones, meta);
        Record("resources", resources, meta);

        return new ProtocolPortalResponse(
            overview.Data!,
            stats.Data!,
            today.Data!,
            week.Data!,
            daySchedules,
            diet.Data!,
            supplements.Data!,
            monitoring.Data!,
            milestones.Data!,
            resources.Data!,
            meta);
    }

    // ── Granular endpoints ──────────────────────────────────────────────────

    public async Task<ProtocolActiveResponse> GetActiveAsync(Guid profileId, CancellationToken ct = default)
    {
        var ctx = await LoadContextAsync(profileId, ct);
        return new ProtocolActiveResponse(
            BuildOverviewSection(ctx).Data!,
            BuildStatsSection(ctx).Data!);
    }

    public async Task<DayScheduleResponse> GetScheduleAsync(Guid profileId, DateOnly? date, CancellationToken ct = default)
    {
        var ctx = await LoadContextAsync(profileId, ct);
        var target = date?.ToDateTime(TimeOnly.MinValue) ?? ctx.Today;
        return BuildDayScheduleSection(ctx, target).Data!;
    }

    public async Task<IReadOnlyList<WeekDayResponse>> GetWeekAsync(Guid profileId, DateOnly? start, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        await EnsureOperatorAsync("portal_calendar_operator", "the weekly calendar", ct);
        var ctx = await LoadContextAsync(profileId, ct);
        var weekStart = start?.ToDateTime(TimeOnly.MinValue) ?? StartOfWeek(ctx.Today);
        return BuildWeekSection(ctx, weekStart).Data!;
    }

    public async Task<DietFrameworkResponse> GetDietAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        await EnsureOperatorAsync("portal_diet_operator", "the diet framework", ct);
        return _baseline.Diet;
    }

    public async Task<SupplementPlanResponse> GetSupplementsAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        return _baseline.Supplements;
    }

    public async Task<MonitoringProtocolResponse> GetMonitoringAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        await EnsureCommanderAsync("portal_monitoring_commander", "monitoring & adjustment rules", ct);
        return _baseline.Monitoring;
    }

    public async Task<IReadOnlyList<MilestoneResponse>> GetMilestonesAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        await EnsureOperatorAsync("portal_milestones_operator", "progress & milestones", ct);
        var ctx = await LoadContextAsync(profileId, ct);
        return BuildMilestonesSection(ctx).Data!;
    }

    public async Task<IReadOnlyList<ResourceEntryResponse>> GetResourcesAsync(Guid profileId, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);
        return _baseline.Resources;
    }

    // ── Mutations ───────────────────────────────────────────────────────────

    public async Task LogDosesAsync(Guid profileId, LogProtocolDosesRequest request, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);

        if (!TryParseDate(request.Date, out var date))
            throw new InvalidOperationException("A valid date (YYYY-MM-DD) is required.");

        // Persist a real dose-log marker as a timeline event (idempotent per day).
        var existing = (await _timelineRepository.GetByPersonIdAsync(profileId, ct))
            .Any(e => e.RelatedEntityType == DoseLogMarker && e.OccurredAtUtc.Date == date.Date);
        if (existing)
            return;

        var @event = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = profileId,
            EventType = EventType.NoteAdded,
            Title = "Scheduled doses marked taken",
            Description = $"Doses logged for {IsoDate(date)}.",
            OccurredAtUtc = date,
            RelatedEntityId = null,
            RelatedEntityType = DoseLogMarker,
        };

        await _timelineRepository.AddAsync(@event, ct);
        await _timelineRepository.SaveChangesAsync(ct);
    }

    public async Task SendCareTeamMessageAsync(Guid profileId, CareTeamMessageRequest request, CancellationToken ct = default)
    {
        await _ownershipGuard.EnsureProfileOwnedAsync(profileId, ct);

        if (string.IsNullOrWhiteSpace(request.Message))
            throw new InvalidOperationException("A non-empty message is required.");

        var message = request.Message.Trim();
        if (message.Length > 2000)
            throw new InvalidOperationException("Message is too long (2000 character maximum).");

        // Lightweight rate-limit: reject if a message was recorded in the last 10s.
        var recent = (await _timelineRepository.GetByPersonIdAsync(profileId, ct))
            .Where(e => e.RelatedEntityType == CareTeamMessageMarker)
            .OrderByDescending(e => e.OccurredAtUtc)
            .FirstOrDefault();
        if (recent is not null && DateTime.UtcNow - recent.OccurredAtUtc < TimeSpan.FromSeconds(10))
            throw new InvalidOperationException("Please wait a moment before sending another message.");

        // TODO: route to an actual care-team channel/notification. For now the message
        // is persisted as a timeline event so it is not silently dropped.
        var @event = new TimelineEvent
        {
            Id = Guid.NewGuid(),
            PersonId = profileId,
            EventType = EventType.NoteAdded,
            Title = "Care-team message",
            Description = message,
            OccurredAtUtc = DateTime.UtcNow,
            RelatedEntityId = null,
            RelatedEntityType = CareTeamMessageMarker,
        };

        await _timelineRepository.AddAsync(@event, ct);
        await _timelineRepository.SaveChangesAsync(ct);
    }

    // ── Context loading ─────────────────────────────────────────────────────

    private async Task<PortalContext> LoadContextAsync(Guid profileId, CancellationToken ct)
    {
        var profile = await _ownershipGuard.GetOwnedProfileAsync(profileId, ct);
        var protocols = (await _protocolRepository.GetByPersonIdAsync(profileId, ct)).ToList();
        var activeProtocol = protocols
            .Where(p => !p.IsDraft)
            .OrderByDescending(p => p.CreatedAtUtc)
            .FirstOrDefault()
            ?? protocols.OrderByDescending(p => p.CreatedAtUtc).FirstOrDefault();

        var phases = (await _phaseRepository.GetByPersonIdAsync(profileId, ct))
            .OrderBy(p => p.StartDate ?? DateTime.MaxValue)
            .ThenBy(p => p.CreatedAtUtc)
            .ToList();

        var compounds = (await _compoundRepository.GetByPersonIdAsync(profileId, ct)).ToList();
        var checkIns = (await _checkInRepository.GetByPersonIdAsync(profileId, ct))
            .OrderBy(c => c.Date)
            .ToList();
        var timeline = (await _timelineRepository.GetByPersonIdAsync(profileId, ct)).ToList();

        return new PortalContext(profile, activeProtocol, phases, compounds, checkIns, timeline, DateTime.UtcNow.Date);
    }

    // ── Section builders (operational → real or honest empty-state) ─────────

    private static PortalSection<ProtocolOverviewResponse> BuildOverviewSection(PortalContext ctx)
    {
        var phases = ctx.Phases;
        var portalPhases = BuildPortalPhases(ctx);
        var currentPhase = ResolveCurrentPhase(portalPhases, ctx)
            ?? new PortalPhaseResponse(1, "Phase 1", 1, 1);

        if (ctx.ActiveProtocol is null && phases.Count == 0)
        {
            // No real protocol or phases — honest minimal overview.
            var emptyOverview = new ProtocolOverviewResponse(
                ProtocolName: "No active protocol",
                Objective: ctx.Profile.GoalSummary.Length > 0 ? ctx.Profile.GoalSummary : "No protocol started yet",
                Status: "draft",
                StartedOnUtc: string.Empty,
                ClientName: ctx.Profile.DisplayName,
                ClientAvatarUrl: null,
                CurrentPhase: currentPhase,
                Phases: portalPhases);
            return PortalSection<ProtocolOverviewResponse>.Empty(
                emptyOverview, PortalSource.Protocol, "No active protocol has been started yet.");
        }

        var startedOn = ResolveStart(ctx);
        var overview = new ProtocolOverviewResponse(
            ProtocolName: ctx.ActiveProtocol?.Name is { Length: > 0 } name ? name : "Your Personalized Protocol",
            Objective: ctx.Profile.GoalSummary.Length > 0 ? ctx.Profile.GoalSummary : "Personalized protocol",
            Status: ctx.ActiveProtocol is null ? "draft" : (ctx.ActiveProtocol.IsDraft ? "draft" : "active"),
            StartedOnUtc: startedOn is null ? string.Empty : Iso(startedOn.Value),
            ClientName: ctx.Profile.DisplayName,
            ClientAvatarUrl: null,
            CurrentPhase: currentPhase,
            Phases: portalPhases);

        return PortalSection<ProtocolOverviewResponse>.Real(overview, PortalSource.Protocol);
    }

    private PortalSection<IReadOnlyList<ProtocolStatResponse>> BuildStatsSection(PortalContext ctx)
    {
        var stats = new List<ProtocolStatResponse>
        {
            BuildPrimaryDoseStat(ctx),
            BuildAdherenceStat(ctx),
            BuildWeightTrendStat(ctx),
            BuildNextLabsStat(ctx),
        };

        var anyReal = ctx.Compounds.Any(c => c.Status == CompoundStatus.Active) || ctx.CheckIns.Count > 0;
        return anyReal
            ? PortalSection<IReadOnlyList<ProtocolStatResponse>>.Derived(stats, PortalSource.Computed)
            : PortalSection<IReadOnlyList<ProtocolStatResponse>>.Empty(
                stats, PortalSource.Computed, "Not enough logged data yet for protocol stats.");
    }

    private static ProtocolStatResponse BuildPrimaryDoseStat(PortalContext ctx)
    {
        var primary = ctx.Compounds
            .Where(c => c.Status == CompoundStatus.Active)
            .OrderBy(c => c.StartDate ?? DateTime.MaxValue)
            .FirstOrDefault();

        return primary is null
            ? new ProtocolStatResponse("Primary compound", "No active compound", null, "Add a compound to begin", "neutral")
            : new ProtocolStatResponse("Primary compound", primary.Name, null, "Active", "emerald");
    }

    private ProtocolStatResponse BuildAdherenceStat(PortalContext ctx)
    {
        // Adherence from real dose-log timeline events only — never fabricated.
        var windowStart = ctx.Today.AddDays(-(AdherenceWindowDays - 1));
        var loggedDays = ctx.Timeline
            .Where(e => e.RelatedEntityType == DoseLogMarker
                && e.OccurredAtUtc.Date >= windowStart
                && e.OccurredAtUtc.Date <= ctx.Today)
            .Select(e => e.OccurredAtUtc.Date)
            .Distinct()
            .Count();

        if (loggedDays == 0)
            return new ProtocolStatResponse(
                "Adherence (last 7 days)", "Not enough logs yet", null, "Log doses to track adherence", "neutral");

        var pct = (int)Math.Round(100.0 * loggedDays / AdherenceWindowDays);
        var accent = pct >= 85 ? "emerald" : pct >= 60 ? "amber" : "red";
        return new ProtocolStatResponse(
            "Adherence (last 7 days)", pct.ToString(CultureInfo.InvariantCulture), "%",
            $"{loggedDays} of {AdherenceWindowDays} days logged", accent);
    }

    private static ProtocolStatResponse BuildWeightTrendStat(PortalContext ctx)
    {
        // Weight trend from real check-ins only.
        var withWeight = ctx.CheckIns.Where(c => c.Weight > 0).OrderBy(c => c.Date).ToList();
        if (withWeight.Count < 2)
            return new ProtocolStatResponse(
                "Weight trend", "Not enough check-ins yet", null, "Record check-ins to see trend", "neutral");

        var delta = withWeight[^1].Weight - withWeight[0].Weight;
        var accent = delta < 0 ? "emerald" : delta > 0 ? "amber" : "neutral";
        var sign = delta > 0 ? "+" : string.Empty;
        return new ProtocolStatResponse(
            "Weight trend",
            sign + delta.ToString("0.0", CultureInfo.InvariantCulture),
            "lbs",
            $"Across {withWeight.Count} check-ins since start",
            accent);
    }

    private static ProtocolStatResponse BuildNextLabsStat(PortalContext ctx)
    {
        // No real lab-scheduling entity exists — never fabricate a date.
        return new ProtocolStatResponse(
            "Next labs due", "Not scheduled", null, "No lab date on file", "neutral");
    }

    private PortalSection<DayScheduleResponse> BuildDayScheduleSection(PortalContext ctx, DateTime date)
    {
        var items = BuildScheduleItems(ctx, date);
        var weekdayName = date.ToString("dddd", CultureInfo.InvariantCulture);
        var title = date.Date == ctx.Today ? "Today's Schedule" : date.ToString("dddd, MMMM d", CultureInfo.InvariantCulture);
        var phase = ResolveCurrentPhase(BuildPortalPhases(ctx), ctx);
        var phaseLabel = phase is null ? string.Empty : $" · Week {phase.CurrentWeek}, {phase.Label}";

        if (items.Count == 0)
        {
            var emptyDay = new DayScheduleResponse(
                IsoDate(date),
                title,
                $"{weekdayName}, {date:MMMM d, yyyy} · No scheduled items",
                new List<ScheduleItemResponse>());
            return PortalSection<DayScheduleResponse>.Empty(
                emptyDay, PortalSource.ProtocolItem, "No active compounds scheduled for this day.");
        }

        var day = new DayScheduleResponse(
            IsoDate(date),
            title,
            $"{weekdayName}, {date:MMMM d, yyyy}{phaseLabel}",
            items);
        return PortalSection<DayScheduleResponse>.Derived(day, PortalSource.ProtocolItem);
    }

    private static List<ScheduleItemResponse> BuildScheduleItems(PortalContext ctx, DateTime date)
    {
        // Expand active compounds (from protocol items / compound records) that are
        // in-window on the target date. We deliberately do NOT invent dose statuses;
        // an item resolves to "completed" only if that day's doses were really logged.
        var dayLogged = ctx.Timeline.Any(e =>
            e.RelatedEntityType == DoseLogMarker && e.OccurredAtUtc.Date == date.Date);

        var sources = ResolveScheduledCompounds(ctx);
        var items = new List<ScheduleItemResponse>();
        foreach (var c in sources)
        {
            var startOk = c.StartDate is null || c.StartDate.Value.Date <= date.Date;
            var endOk = c.EndDate is null || c.EndDate.Value.Date >= date.Date;
            if (!startOk || !endOk)
                continue;

            items.Add(new ScheduleItemResponse(
                Time: "As scheduled",
                Name: c.Name,
                Detail: string.IsNullOrWhiteSpace(c.Notes) ? c.Category.ToString() : c.Notes,
                Icon: IconFor(c.Category),
                Accent: AccentFor(c.Category),
                Status: dayLogged ? "completed" : "upcoming"));
        }

        return items
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static List<CompoundRecord> ResolveScheduledCompounds(PortalContext ctx)
    {
        // Prefer the active protocol's item snapshots; fall back to active compound records.
        if (ctx.ActiveProtocol is { Items.Count: > 0 })
        {
            return ctx.ActiveProtocol.Items
                .Select(item => item.CompoundRecord ?? new CompoundRecord
                {
                    Id = item.CompoundRecordId,
                    PersonId = ctx.Profile.Id,
                    Name = item.CompoundNameSnapshot,
                    Notes = item.CompoundNotesSnapshot,
                    StartDate = item.CompoundStartDateSnapshot,
                    EndDate = item.CompoundEndDateSnapshot,
                    Category = Enum.TryParse<CompoundCategory>(item.CompoundCategorySnapshot, out var cat)
                        ? cat
                        : CompoundCategory.Unknown,
                    Status = Enum.TryParse<CompoundStatus>(item.CompoundStatusSnapshot, out var st)
                        ? st
                        : CompoundStatus.Active,
                })
                .Where(c => c.Status == CompoundStatus.Active)
                .ToList();
        }

        return ctx.Compounds.Where(c => c.Status == CompoundStatus.Active).ToList();
    }

    private PortalSection<IReadOnlyList<WeekDayResponse>> BuildWeekSection(PortalContext ctx, DateTime weekStart)
    {
        var start = weekStart.Date;
        var days = new List<WeekDayResponse>();
        var anyItems = false;
        for (var offset = 0; offset < 7; offset++)
        {
            var date = start.AddDays(offset);
            var count = BuildScheduleItems(ctx, date).Count;
            anyItems |= count > 0;
            days.Add(new WeekDayResponse(
                IsoDate(date),
                date.Day.ToString(CultureInfo.InvariantCulture),
                date.ToString("ddd", CultureInfo.InvariantCulture),
                date.Date == ctx.Today,
                count,
                null));
        }

        return anyItems
            ? PortalSection<IReadOnlyList<WeekDayResponse>>.Derived(days, PortalSource.ProtocolItem)
            : PortalSection<IReadOnlyList<WeekDayResponse>>.Empty(
                days, PortalSource.ProtocolItem, "No scheduled items this week.");
    }

    private PortalSection<IReadOnlyList<MilestoneResponse>> BuildMilestonesSection(PortalContext ctx)
    {
        // Educational period text is baseline; "current" flag is derived from real
        // elapsed time since protocol start.
        var baseline = _baseline.Milestones;
        var start = ResolveStart(ctx);

        if (start is null)
        {
            var pending = baseline.Select(m => m with { Current = false }).ToList();
            return PortalSection<IReadOnlyList<MilestoneResponse>>.Curated(pending, _baseline.Version);
        }

        var weeksElapsed = Math.Max(0, (int)Math.Floor((ctx.Today - start.Value.Date).TotalDays / 7.0));
        var currentOrder = weeksElapsed switch
        {
            < 4 => 1,
            < 12 => 2,
            _ => 3,
        };

        var milestones = baseline.Select(m => m with { Current = m.Order == currentOrder }).ToList();
        return PortalSection<IReadOnlyList<MilestoneResponse>>.Derived(milestones, PortalSource.ProtocolPhase);
    }

    // ── Curated (educational) section builders ──────────────────────────────

    private PortalSection<DietFrameworkResponse> BuildDietSection()
        => PortalSection<DietFrameworkResponse>.Curated(_baseline.Diet, _baseline.Version);

    private PortalSection<SupplementPlanResponse> BuildSupplementsSection()
        => PortalSection<SupplementPlanResponse>.Curated(_baseline.Supplements, _baseline.Version);

    private PortalSection<MonitoringProtocolResponse> BuildMonitoringSection()
        => PortalSection<MonitoringProtocolResponse>.Curated(_baseline.Monitoring, _baseline.Version);

    private PortalSection<IReadOnlyList<ResourceEntryResponse>> BuildResourcesSection()
        => PortalSection<IReadOnlyList<ResourceEntryResponse>>.Curated(_baseline.Resources, _baseline.Version);

    // ── Phase helpers ───────────────────────────────────────────────────────

    private static IReadOnlyList<PortalPhaseResponse> BuildPortalPhases(PortalContext ctx)
    {
        if (ctx.Phases.Count == 0)
            return new List<PortalPhaseResponse> { new(1, "Phase 1", 1, 1) };

        var result = new List<PortalPhaseResponse>();
        for (var i = 0; i < ctx.Phases.Count; i++)
        {
            var phase = ctx.Phases[i];
            var totalWeeks = phase.StartDate is not null && phase.EndDate is not null
                ? Math.Max(1, (int)Math.Ceiling((phase.EndDate.Value - phase.StartDate.Value).TotalDays / 7.0))
                : 1;
            var currentWeek = phase.StartDate is not null
                ? Math.Clamp((int)Math.Floor((ctx.Today - phase.StartDate.Value.Date).TotalDays / 7.0) + 1, 1, totalWeeks)
                : 1;
            result.Add(new PortalPhaseResponse(
                i + 1,
                string.IsNullOrWhiteSpace(phase.Name) ? $"Phase {i + 1}" : phase.Name,
                currentWeek,
                totalWeeks));
        }

        return result;
    }

    private static PortalPhaseResponse? ResolveCurrentPhase(IReadOnlyList<PortalPhaseResponse> portalPhases, PortalContext ctx)
    {
        if (portalPhases.Count == 0)
            return null;

        if (ctx.Phases.Count == 0)
            return portalPhases[0];

        // The current phase is the latest one whose start date is on or before today.
        for (var i = ctx.Phases.Count - 1; i >= 0; i--)
        {
            var start = ctx.Phases[i].StartDate;
            if (start is null || start.Value.Date <= ctx.Today)
                return portalPhases[i];
        }

        return portalPhases[0];
    }

    private static DateTime? ResolveStart(PortalContext ctx)
    {
        var phaseStart = ctx.Phases
            .Where(p => p.StartDate.HasValue)
            .Select(p => p.StartDate!.Value)
            .DefaultIfEmpty(DateTime.MaxValue)
            .Min();
        if (phaseStart != DateTime.MaxValue)
            return phaseStart;

        if (ctx.ActiveProtocol is not null)
            return ctx.ActiveProtocol.CreatedAtUtc;

        var compoundStart = ctx.Compounds
            .Where(c => c.StartDate.HasValue)
            .Select(c => c.StartDate!.Value)
            .DefaultIfEmpty(DateTime.MaxValue)
            .Min();
        return compoundStart == DateTime.MaxValue ? null : compoundStart;
    }

    // ── Meta + formatting helpers ───────────────────────────────────────────

    private static void Record<T>(string key, PortalSection<T> section, IDictionary<string, PortalSectionMetaResponse> meta)
        => meta[key] = new PortalSectionMetaResponse(
            section.Status,
            section.Source,
            Iso(section.GeneratedAtUtc),
            section.BaselineVersion,
            section.EmptyState);

    private static DateTime StartOfWeek(DateTime date)
    {
        // Week starts Sunday to match the frontend week strip.
        var diff = (int)date.DayOfWeek;
        return date.Date.AddDays(-diff);
    }

    private static string IsoDate(DateTime date)
        => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    private static string Iso(DateTime utc)
        => DateTime.SpecifyKind(utc, DateTimeKind.Utc).ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);

    private static bool TryParseDate(string? value, out DateTime date)
    {
        if (DateTime.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out date))
        {
            date = date.Date;
            return true;
        }

        date = default;
        return false;
    }

    private static string IconFor(CompoundCategory category) => category switch
    {
        CompoundCategory.Peptide => "syringe",
        _ => "beaker",
    };

    private static string AccentFor(CompoundCategory category) => category switch
    {
        CompoundCategory.Peptide => "violet",
        _ => "blue",
    };

    private sealed record PortalContext(
        PersonProfile Profile,
        Protocol? ActiveProtocol,
        IReadOnlyList<ProtocolPhase> Phases,
        IReadOnlyList<CompoundRecord> Compounds,
        IReadOnlyList<CheckIn> CheckIns,
        IReadOnlyList<TimelineEvent> Timeline,
        DateTime Today);
}

public interface IProtocolPortalService
{
    Task<ProtocolPortalResponse> GetPortalAsync(Guid profileId, CancellationToken ct = default);
    Task<ProtocolActiveResponse> GetActiveAsync(Guid profileId, CancellationToken ct = default);
    Task<DayScheduleResponse> GetScheduleAsync(Guid profileId, DateOnly? date, CancellationToken ct = default);
    Task<IReadOnlyList<WeekDayResponse>> GetWeekAsync(Guid profileId, DateOnly? start, CancellationToken ct = default);
    Task<DietFrameworkResponse> GetDietAsync(Guid profileId, CancellationToken ct = default);
    Task<SupplementPlanResponse> GetSupplementsAsync(Guid profileId, CancellationToken ct = default);
    Task<MonitoringProtocolResponse> GetMonitoringAsync(Guid profileId, CancellationToken ct = default);
    Task<IReadOnlyList<MilestoneResponse>> GetMilestonesAsync(Guid profileId, CancellationToken ct = default);
    Task<IReadOnlyList<ResourceEntryResponse>> GetResourcesAsync(Guid profileId, CancellationToken ct = default);
    Task LogDosesAsync(Guid profileId, LogProtocolDosesRequest request, CancellationToken ct = default);
    Task SendCareTeamMessageAsync(Guid profileId, CareTeamMessageRequest request, CancellationToken ct = default);
}
