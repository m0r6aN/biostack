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
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .FirstOrDefaultAsync(k => 
                k.CanonicalName.ToLower() == name.ToLower() ||
                EF.Functions.Like(EF.Property<string>(k, "Aliases"), $"%|{name}|%") ||
                k.Aliases.FirstOrDefault() == name, // Fallback for simple matches
                cancellationToken);
    }

    public async Task<List<KnowledgeEntry>> GetAllCompoundsAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .ToListAsync(cancellationToken);
    }

    public async Task<List<KnowledgeEntry>> SearchCompoundsByPathwayAsync(string pathway, CancellationToken cancellationToken = default)
    {
        return await _dbContext.KnowledgeEntries
            .AsNoTracking()
            .Where(k => EF.Functions.Like(EF.Property<string>(k, "Pathways"), $"%|{pathway}|%"))
            .ToListAsync(cancellationToken);
    }

    public async Task UpsertCompoundAsync(KnowledgeEntry entry, CancellationToken cancellationToken = default)
    {
        var existing = await _dbContext.KnowledgeEntries
            .FirstOrDefaultAsync(k => k.CanonicalName == entry.CanonicalName, cancellationToken);

        if (existing != null)
        {
            entry.Id = existing.Id;
            _dbContext.Entry(existing).CurrentValues.SetValues(entry);
            // Manually handle lists since they require conversion/re-assignment
            existing.Aliases = entry.Aliases;
            existing.Pathways = entry.Pathways;
            existing.Benefits = entry.Benefits;
            existing.PairsWellWith = entry.PairsWellWith;
            existing.AvoidWith = entry.AvoidWith;
            existing.CompatibleBlends = entry.CompatibleBlends;
            existing.WeeklyDosageSchedule = entry.WeeklyDosageSchedule;
            existing.IncrementalEscalationSteps = entry.IncrementalEscalationSteps;
            existing.DrugInteractions = entry.DrugInteractions;
            existing.OptimizationSupplements = entry.OptimizationSupplements;
            existing.TieredDosing = entry.TieredDosing;
            existing.SourceReferences = entry.SourceReferences;
        }
        else
        {
            if (entry.Id == Guid.Empty) entry.Id = Guid.NewGuid();
            await _dbContext.KnowledgeEntries.AddAsync(entry, cancellationToken);
        }

        await _dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> IngestBulkAsync(List<KnowledgeEntry> entries, CancellationToken cancellationToken = default)
    {
        int count = 0;
        foreach (var entry in entries)
        {
            await UpsertCompoundAsync(entry, cancellationToken);
            count++;
        }
        return count;
    }
}
