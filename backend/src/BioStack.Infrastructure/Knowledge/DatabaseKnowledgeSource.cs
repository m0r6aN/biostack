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
        // Deterministic: a unique index on lower(CanonicalName) prevents duplicates, but if a
        // legacy duplicate survives, always serve the same row (lowest Id) instead of an
        // unordered first match.
        // Public reads never see an unreviewed auto-discovered draft (IsPublic = false): the record
        // exists for the review queue only. A hidden draft is treated as absent here.
        var canonicalMatch = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.CanonicalName.ToLower() == normalizedCanonicalName && (k.IsPublic || k.RetiredAtUtc != null))
            .OrderBy(k => k.Id)
            .FirstOrDefaultAsync(cancellationToken);

        if (canonicalMatch is not null && canonicalMatch.RetiredAtUtc is null)
        {
            return canonicalMatch;
        }

        var entries = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.RetiredAtUtc == null && k.IsPublic)
            .ToListAsync(cancellationToken);

        var ordered = entries.OrderBy(k => k.Id).ToList();

        // A retired record that names its successor resolves to it, so old links and slugs
        // (e.g. /knowledge/testolone) keep working after a merge.
        if (canonicalMatch?.SupersededBy is { Length: > 0 } successor)
        {
            var successorMatch = ordered.FirstOrDefault(k =>
                string.Equals(k.CanonicalName, successor, StringComparison.OrdinalIgnoreCase));
            if (successorMatch is not null)
            {
                return successorMatch;
            }
        }

        var match = ordered.FirstOrDefault(k =>
                   k.Aliases.Any(alias => string.Equals(alias.Trim(), normalizedName, StringComparison.OrdinalIgnoreCase)))
               // Public dossier URLs use a slug (/knowledge/creatine-monohydrate, /knowledge/nad-plus).
               ?? ordered.FirstOrDefault(k => CompoundSlug.ForDossier(k.CanonicalName) == CompoundSlug.ForDossier(normalizedName));
        if (match is not null)
        {
            return match;
        }

        // The slug of a retired, superseded record (/knowledge/testolone) resolves to its successor.
        var requestedSlug = CompoundSlug.ForDossier(normalizedName);
        var retired = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.RetiredAtUtc != null && k.SupersededBy != null)
            .Select(k => new { k.CanonicalName, k.SupersededBy })
            .ToListAsync(cancellationToken);
        var superseded = retired.FirstOrDefault(r => CompoundSlug.ForDossier(r.CanonicalName) == requestedSlug);
        return superseded is null
            ? null
            : ordered.FirstOrDefault(k => string.Equals(k.CanonicalName, superseded.SupersededBy, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.RetiredAtUtc == null && k.IsPublic)
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeEntry>> ListPublicCompoundsAsync(CancellationToken cancellationToken = default)
    {
        // Public list columns only. Includes every field PublicEvidenceCompleteness reads.
        // Dose, schedule, pairing, and optimization columns stay on the full-entity read.
        var rows = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(entry => entry.RetiredAtUtc == null && entry.IsPublic)
            .Select(entry => new
            {
                entry.Id,
                entry.CanonicalName,
                entry.Aliases,
                entry.Classification,
                entry.RegulatoryStatus,
                entry.MechanismSummary,
                entry.EvidenceTier,
                entry.SourceReferences,
                entry.Notes,
                entry.Pathways,
                entry.Benefits,
                entry.AvoidWith,
                entry.DrugInteractions,
            })
            .ToListAsync(cancellationToken);

        return rows.Select(row => new KnowledgeEntry
        {
            Id = row.Id,
            CanonicalName = row.CanonicalName,
            Aliases = row.Aliases,
            Classification = row.Classification,
            RegulatoryStatus = row.RegulatoryStatus,
            MechanismSummary = row.MechanismSummary,
            EvidenceTier = row.EvidenceTier,
            SourceReferences = row.SourceReferences,
            Notes = row.Notes,
            Pathways = row.Pathways,
            Benefits = row.Benefits,
            AvoidWith = row.AvoidWith,
            DrugInteractions = row.DrugInteractions,
        }).ToList();
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
            .Where(k => k.RetiredAtUtc == null && k.IsPublic)
            .ToListAsync(cancellationToken);

        return entries
            .Where(k => k.Pathways.Any(candidate =>
                string.Equals(candidate.Trim(), normalizedPathway, StringComparison.OrdinalIgnoreCase)))
            .ToList();
    }

    public async Task<KnowledgeUpsertDisposition> UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
    {
        // Case-insensitive and ordered, so the upsert always writes the row the API serves.
        // Any legacy duplicates of the same name are removed here so a Refresh converges on
        // exactly one row per compound.
        var normalized = (entry.CanonicalName ?? string.Empty).Trim().ToLowerInvariant();
        var matches = await _dbContext.KnowledgeEntries
            .Where(k => k.CanonicalName.ToLower() == normalized)
            .OrderBy(k => k.Id)
            .ToListAsync(cancellationToken);
        var existing = matches.FirstOrDefault();
        var removedDuplicates = matches.Count > 1;
        if (removedDuplicates)
        {
            _dbContext.KnowledgeEntries.RemoveRange(matches.Skip(1));
        }

        if (existing != null)
        {
            if (!ApplyChanges(existing, entry) && !removedDuplicates)
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

    public async Task<KnowledgeUpsertDisposition> PreviewUpsertAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
    {
        // AsNoTracking: this instance is never attached to the change tracker, so running
        // it through ApplyChanges below to compute the diff is safe — there is nothing for
        // a stray SaveChangesAsync to persist, and this method never calls SaveChangesAsync.
        var normalized = (entry.CanonicalName ?? string.Empty).Trim().ToLowerInvariant();
        var matches = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.CanonicalName.ToLower() == normalized)
            .OrderBy(k => k.Id)
            .ToListAsync(cancellationToken);
        var existing = matches.FirstOrDefault();

        if (existing is null)
        {
            return KnowledgeUpsertDisposition.Created;
        }

        return ApplyChanges(existing, entry) || matches.Count > 1
            ? KnowledgeUpsertDisposition.Updated
            : KnowledgeUpsertDisposition.Unchanged;
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

    public async Task<IReadOnlyList<KnowledgeEntryIdentity>> ListActiveIdentitiesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.RetiredAtUtc == null)
            .OrderBy(k => k.CanonicalName)
            .Select(k => new { k.CanonicalName, k.Aliases })
            .ToListAsync(cancellationToken);
        return rows.Select(r => new KnowledgeEntryIdentity(r.CanonicalName, r.Aliases)).ToList();
    }

    public async Task<IReadOnlyList<KnowledgeEntry>> ListUnreviewedAsync(CancellationToken cancellationToken = default)
    {
        // Review-queue read (admin only): hidden drafts, never served to public readers.
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => k.RetiredAtUtc == null && !k.IsPublic)
            .OrderBy(k => k.CanonicalName)
            .ToListAsync(cancellationToken);
    }

    public async Task<bool> RetireCompoundAsync(string canonicalName, string? supersededBy, DateTime retiredAtUtc, CancellationToken cancellationToken = default)
    {
        var normalized = (canonicalName ?? string.Empty).Trim().ToLowerInvariant();
        var rows = await _dbContext.KnowledgeEntries
            .Where(k => k.CanonicalName.ToLower() == normalized && k.RetiredAtUtc == null)
            .ToListAsync(cancellationToken);
        if (rows.Count == 0)
        {
            return false;
        }

        foreach (var row in rows)
        {
            row.RetiredAtUtc = retiredAtUtc;
            row.SupersededBy = string.IsNullOrWhiteSpace(supersededBy) ? null : supersededBy.Trim();
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
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
        changed |= SetIfDifferent(existing.Details, incoming.Details, value => existing.Details = value, DetailsEquals);

        // Public visibility follows the record and its council decision on every run (drafts hide, approved drafts show).
        changed |= SetIfDifferent(existing.IsPublic, incoming.IsPublic, value => existing.IsPublic = value);

        // A record that is back in the seed is published again.
        changed |= SetIfDifferent(existing.RetiredAtUtc, null, value => existing.RetiredAtUtc = value);
        changed |= SetIfDifferent(existing.SupersededBy, null, value => existing.SupersededBy = value);

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

    private static bool DetailsEquals(KnowledgeEntryDetails? current, KnowledgeEntryDetails? incoming)
        => System.Text.Json.JsonSerializer.Serialize(current) == System.Text.Json.JsonSerializer.Serialize(incoming);

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
