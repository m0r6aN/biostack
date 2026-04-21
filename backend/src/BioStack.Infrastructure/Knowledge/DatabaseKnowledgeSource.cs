namespace BioStack.Infrastructure.Knowledge;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public sealed class DatabaseKnowledgeSource : IKnowledgeSource
{
    private readonly BioStackDbContext _dbContext;

    public DatabaseKnowledgeSource(BioStackDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<KnowledgeEntry?> GetCompoundAsync(string name, CancellationToken cancellationToken = default)
    {
        var normalizedName = name.Trim();
        if (string.IsNullOrWhiteSpace(normalizedName))
        {
            return null;
        }

        var normalizedCanonicalName = normalizedName.ToLowerInvariant();
        var canonicalMatch = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(k =>
                k.CanonicalName.ToLower() == normalizedCanonicalName,
                cancellationToken);

        if (canonicalMatch is not null)
        {
            return canonicalMatch;
        }

        var entries = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries.FirstOrDefault(k =>
            k.Aliases.Any(alias => string.Equals(alias.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase)));
    }

    public async Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(string pathway, CancellationToken cancellationToken = default)
    {
        var normalizedPathway = pathway.Trim();
        if (string.IsNullOrWhiteSpace(normalizedPathway))
        {
            return new List<KnowledgeEntry>();
        }

        var entries = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        return entries
            .Where(k => k.Pathways.Any(candidate =>
                string.Equals(candidate.Trim(), normalizedPathway, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<KnowledgeUpsertDisposition> UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeEntries
            .FirstOrDefaultAsync(k => k.CanonicalName == entry.CanonicalName, cancellationToken);

        if (existing != null)
        {
            if (!ApplyChanges(existing, entry))
            {
                return KnowledgeUpsertDisposition.Unchanged;
            }
        }
        else
        {
            if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
            await _dbContext.KnowledgeEntries.AddAsync(entry, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return existing is null
            ? KnowledgeUpsertDisposition.Created
            : KnowledgeUpsertDisposition.Updated;
    }

    public async Task<int> IngestBulkAsync(List<KnowledgeEntry> entries, CancellationToken cancellationToken = default)
    {
        int count = 0;
        foreach (var entry in entries)
        {
            var disposition = await UpsertCompoundAsync(entry, cancellationToken);
            if (disposition != KnowledgeUpsertDisposition.Unchanged)
            {
                count++;
            }
        }
        return count;
    }

    private static bool ApplyChanges(KnowledgeEntry existing, KnowledgeEntry incoming)
    {
        incoming.Id = existing.Id;

        var changed = false;

        changed |= SetIfDifferent(existing.CanonicalName, incoming.CanonicalName, value => existing.CanonicalName = value);
        changed |= SetIfDifferent(existing.Classification, incoming.Classification, value => existing.Classification = value);
        changed |= SetIfDifferent(existing.RegulatoryStatus, incoming.RegulatoryStatus, value => existing.RegulatoryStatus = value);
        changed |= SetIfDifferent(existing.MechanismSummary, incoming.MechanismSummary, value => existing.MechanismSummary = value);
        changed |= SetIfDifferent(existing.EvidenceTier, incoming.EvidenceTier, value => existing.EvidenceTier = value);
        changed |= SetIfDifferent(existing.Notes, incoming.Notes, value => existing.Notes = value);
        changed |= SetIfDifferent(existing.VialCompatibility, incoming.VialCompatibility, value => existing.VialCompatibility = value);
        changed |= SetIfDifferent(existing.RecommendedDosage, incoming.RecommendedDosage, value => existing.RecommendedDosage = value);
        changed |= SetIfDifferent(existing.StandardDosageRange, incoming.StandardDosageRange, value => existing.StandardDosageRange = value);
        changed |= SetIfDifferent(existing.MaxReportedDose, incoming.MaxReportedDose, value => existing.MaxReportedDose = value);
        changed |= SetIfDifferent(existing.Frequency, incoming.Frequency, value => existing.Frequency = value);
        changed |= SetIfDifferent(existing.PreferredTimeOfDay, incoming.PreferredTimeOfDay, value => existing.PreferredTimeOfDay = value);
        changed |= SetIfDifferent(existing.OptimizationProtein, incoming.OptimizationProtein, value => existing.OptimizationProtein = value);
        changed |= SetIfDifferent(existing.OptimizationCarbs, incoming.OptimizationCarbs, value => existing.OptimizationCarbs = value);
        changed |= SetIfDifferent(existing.OptimizationSleep, incoming.OptimizationSleep, value => existing.OptimizationSleep = value);
        changed |= SetIfDifferent(existing.OptimizationExercise, incoming.OptimizationExercise, value => existing.OptimizationExercise = value);
        changed |= SetIfDifferent(existing.TieredDosing, incoming.TieredDosing, value => existing.TieredDosing = value, TieredDosingEquals);

        changed |= SetListIfDifferent(existing.Aliases, incoming.Aliases, value => existing.Aliases = value);
        changed |= SetListIfDifferent(existing.SourceReferences, incoming.SourceReferences, value => existing.SourceReferences = value);
        changed |= SetListIfDifferent(existing.Pathways, incoming.Pathways, value => existing.Pathways = value);
        changed |= SetListIfDifferent(existing.Benefits, incoming.Benefits, value => existing.Benefits = value);
        changed |= SetListIfDifferent(existing.PairsWellWith, incoming.PairsWellWith, value => existing.PairsWellWith = value);
        changed |= SetListIfDifferent(existing.AvoidWith, incoming.AvoidWith, value => existing.AvoidWith = value);
        changed |= SetListIfDifferent(existing.CompatibleBlends, incoming.CompatibleBlends, value => existing.CompatibleBlends = value);
        changed |= SetListIfDifferent(existing.WeeklyDosageSchedule, incoming.WeeklyDosageSchedule, value => existing.WeeklyDosageSchedule = value);
        changed |= SetListIfDifferent(existing.IncrementalEscalationSteps, incoming.IncrementalEscalationSteps, value => existing.IncrementalEscalationSteps = value);
        changed |= SetListIfDifferent(existing.DrugInteractions, incoming.DrugInteractions, value => existing.DrugInteractions = value);
        changed |= SetListIfDifferent(existing.OptimizationSupplements, incoming.OptimizationSupplements, value => existing.OptimizationSupplements = value);

        return changed;
    }

    private static bool SetListIfDifferent(List<string> current, List<string> incoming, Action<List<string>> setter)
    {
        if (current.SequenceEqual(incoming, StringComparer.Ordinal))
        {
            return false;
        }

        setter(new List<string>(incoming));
        return true;
    }

    private static bool SetIfDifferent<T>(
        T current,
        T incoming,
        Action<T> setter,
        Func<T, T, bool>? comparer = null)
    {
        var equals = comparer?.Invoke(current, incoming) ?? EqualityComparer<T>.Default.Equals(current, incoming);
        if (equals)
        {
            return false;
        }

        setter(incoming);
        return true;
    }

    private static bool TieredDosingEquals(TieredDosingData? current, TieredDosingData? incoming)
    {
        if (current is null && incoming is null)
        {
            return true;
        }

        if (current is null || incoming is null)
        {
            return false;
        }

        return DoseTierEquals(current.Beginner, incoming.Beginner)
            && DoseTierEquals(current.Moderate, incoming.Moderate)
            && DoseTierEquals(current.Advanced, incoming.Advanced);
    }

    private static bool DoseTierEquals(DoseTier? current, DoseTier? incoming)
    {
        if (current is null && incoming is null)
        {
            return true;
        }

        if (current is null || incoming is null)
        {
            return false;
        }

        return current.StartDose == incoming.StartDose
            && current.Escalation == incoming.Escalation
            && current.MaxDose == incoming.MaxDose
            && current.SafetyNotes == incoming.SafetyNotes
            && current.WeeklySchedule.SequenceEqual(incoming.WeeklySchedule, StringComparer.Ordinal);
    }
}
