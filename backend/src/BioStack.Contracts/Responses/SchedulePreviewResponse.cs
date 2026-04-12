namespace BioStack.Contracts.Responses;

public sealed record SchedulePreviewResponse(
    string Label,
    string Source,
    string Frequency,
    string PreferredTimeOfDay,
    List<string> WeeklyDosageSchedule,
    List<string> IncrementalEscalationSteps,
    List<string> TieredDosingNotes,
    string StandardDosageRange,
    string MaxReportedDose
);
