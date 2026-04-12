namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;

public sealed class SchedulePreviewService : ISchedulePreviewService
{
    public SchedulePreviewResponse? BuildPreview(KnowledgeEntry entry)
    {
        var tieredNotes = BuildTieredDosingNotes(entry.TieredDosing);
        var hasScheduleData =
            !string.IsNullOrWhiteSpace(entry.Frequency) ||
            !string.IsNullOrWhiteSpace(entry.PreferredTimeOfDay) ||
            entry.WeeklyDosageSchedule.Count > 0 ||
            entry.IncrementalEscalationSteps.Count > 0 ||
            tieredNotes.Count > 0 ||
            !string.IsNullOrWhiteSpace(entry.StandardDosageRange) ||
            !string.IsNullOrWhiteSpace(entry.MaxReportedDose);

        if (!hasScheduleData)
        {
            return null;
        }

        return new SchedulePreviewResponse(
            "Reference schedule from knowledge base",
            "KnowledgeEntry",
            entry.Frequency,
            entry.PreferredTimeOfDay,
            entry.WeeklyDosageSchedule,
            entry.IncrementalEscalationSteps,
            tieredNotes,
            entry.StandardDosageRange,
            entry.MaxReportedDose
        );
    }

    private static List<string> BuildTieredDosingNotes(TieredDosingData? tieredDosing)
    {
        var notes = new List<string>();
        if (tieredDosing is null)
        {
            return notes;
        }

        AddNote(notes, "Beginner", tieredDosing.Beginner);
        AddNote(notes, "Moderate", tieredDosing.Moderate);
        AddNote(notes, "Advanced", tieredDosing.Advanced);
        return notes;
    }

    private static void AddNote(List<string> notes, string tier, DoseTier? value)
    {
        if (value is null)
        {
            return;
        }

        var parts = new[]
        {
            string.IsNullOrWhiteSpace(value.StartDose) ? null : $"start {value.StartDose}",
            string.IsNullOrWhiteSpace(value.Escalation) ? null : $"escalation {value.Escalation}",
            string.IsNullOrWhiteSpace(value.MaxDose) ? null : $"max reference {value.MaxDose}",
            string.IsNullOrWhiteSpace(value.SafetyNotes) ? null : value.SafetyNotes
        }.Where(part => part is not null).Select(part => part!);

        notes.Add($"{tier}: {string.Join("; ", parts)}");
    }
}

public interface ISchedulePreviewService
{
    SchedulePreviewResponse? BuildPreview(KnowledgeEntry entry);
}
