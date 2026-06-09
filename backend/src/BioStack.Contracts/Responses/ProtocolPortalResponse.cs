namespace BioStack.Contracts.Responses;

// ─── Client-facing Protocol Portal contract ─────────────────────────────────
// Wire shape mirrors frontend/src/lib/types.ts (ProtocolPortalData and friends),
// serialized camelCase. The flat shape is the primary contract; SectionMeta is an
// OPTIONAL additive provenance map the shipping frontend can ignore today.

public sealed record PortalPhaseResponse(
    int Number,
    string Label,
    int CurrentWeek,
    int TotalWeeks);

public sealed record ProtocolOverviewResponse(
    string ProtocolName,
    string Objective,
    string Status,
    string StartedOnUtc,
    string ClientName,
    string? ClientAvatarUrl,
    PortalPhaseResponse CurrentPhase,
    IReadOnlyList<PortalPhaseResponse> Phases);

public sealed record ProtocolStatResponse(
    string Label,
    string Value,
    string? Unit,
    string? Caption,
    string Accent);

public sealed record ScheduleItemResponse(
    string Time,
    string Name,
    string Detail,
    string? Icon,
    string? Accent,
    string? Status);

public sealed record DayScheduleResponse(
    string DateIso,
    string Title,
    string Subtitle,
    IReadOnlyList<ScheduleItemResponse> Items);

public sealed record WeekDayResponse(
    string DateIso,
    string DayLabel,
    string WeekdayLabel,
    bool IsToday,
    int ItemCount,
    string? Tag);

public sealed record DietTargetResponse(
    string Label,
    string Value,
    bool? Caution);

public sealed record DietFrameworkResponse(
    string Title,
    string Summary,
    IReadOnlyList<DietTargetResponse> Targets,
    string Rationale,
    IReadOnlyList<string> Lifestyle);

public sealed record SupplementEntryResponse(
    string Name,
    string Dose,
    string? Note,
    bool? Emphasis);

public sealed record SupplementPlanResponse(
    string Title,
    string Summary,
    IReadOnlyList<SupplementEntryResponse> Entries,
    IReadOnlyList<string> Additional);

public sealed record AdjustmentRuleResponse(
    string Trigger,
    string Action);

public sealed record MonitoringProtocolResponse(
    string BaselineCompleted,
    string RecurringCadence,
    IReadOnlyList<string> RecurringLabs,
    IReadOnlyList<AdjustmentRuleResponse> AdjustmentRules);

public sealed record MilestoneResponse(
    int Order,
    string Period,
    string Detail,
    bool? Current);

public sealed record ResourceEntryResponse(
    string Heading,
    string Body);

/// <summary>
/// Optional, additive provenance for a single flattened section. Surfaces the
/// backend-internal <c>PortalSection&lt;T&gt;</c> model at the API edge so clients
/// can (eventually) distinguish real / derived / curated / empty data.
/// </summary>
public sealed record PortalSectionMetaResponse(
    string Status,
    string Source,
    string GeneratedAtUtc,
    string? BaselineVersion,
    string? EmptyState);

public sealed record ProtocolPortalResponse(
    ProtocolOverviewResponse Overview,
    IReadOnlyList<ProtocolStatResponse> Stats,
    DayScheduleResponse Today,
    IReadOnlyList<WeekDayResponse> Week,
    IReadOnlyDictionary<string, DayScheduleResponse> DaySchedules,
    DietFrameworkResponse Diet,
    SupplementPlanResponse Supplements,
    MonitoringProtocolResponse Monitoring,
    IReadOnlyList<MilestoneResponse> Milestones,
    IReadOnlyList<ResourceEntryResponse> Resources,
    IReadOnlyDictionary<string, PortalSectionMetaResponse> SectionMeta);

/// <summary>Response for the granular GET .../protocol/active endpoint.</summary>
public sealed record ProtocolActiveResponse(
    ProtocolOverviewResponse Overview,
    IReadOnlyList<ProtocolStatResponse> Stats);
