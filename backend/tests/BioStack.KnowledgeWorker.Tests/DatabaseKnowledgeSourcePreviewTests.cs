namespace BioStack.KnowledgeWorker.Tests;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

/// <summary>
/// <see cref="DatabaseKnowledgeSource.PreviewUpsertAsync"/> is what makes DryRun's
/// summary counters honest (promotion-state-audit.md §4.2/§6 risk 3: a naive "log and
/// return" DryRun always reports Created=0 Updated=0 Unchanged=0, which cannot be used to
/// size the diff). These tests pin down that it reports the real would-be disposition
/// without ever calling SaveChangesAsync — i.e. without writing anything.
/// </summary>
public sealed class DatabaseKnowledgeSourcePreviewTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseKnowledgeSourcePreviewTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    public void Dispose() => _connection.Dispose();

    [Fact]
    public async Task PreviewUpsertAsync_Reports_Created_For_A_New_Compound_Without_Writing_It()
    {
        using var context = CreateDbContext();
        var source = new DatabaseKnowledgeSource(context);

        var disposition = await source.PreviewUpsertAsync(new KnowledgeEntry { CanonicalName = "Brand New Compound" });

        Assert.Equal(KnowledgeUpsertDisposition.Created, disposition);

        using var assertContext = CreateDbContext();
        Assert.Empty(await assertContext.KnowledgeEntries.ToListAsync());
    }

    [Fact]
    public async Task PreviewUpsertAsync_Reports_Updated_When_A_Field_Differs_Without_Writing_It()
    {
        var name = $"Preview-{Guid.NewGuid():N}";
        using (var seedContext = CreateDbContext())
        {
            seedContext.KnowledgeEntries.Add(new KnowledgeEntry
            {
                CanonicalName = name,
                Classification = CompoundCategory.Peptide,
                EvidenceTier = EvidenceTier.Moderate,
                MechanismSummary = "Original mechanism",
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateDbContext();
        var source = new DatabaseKnowledgeSource(context);

        var disposition = await source.PreviewUpsertAsync(new KnowledgeEntry
        {
            CanonicalName = name,
            Classification = CompoundCategory.Peptide,
            EvidenceTier = EvidenceTier.Moderate,
            MechanismSummary = "Changed mechanism",
        });

        Assert.Equal(KnowledgeUpsertDisposition.Updated, disposition);

        using var assertContext = CreateDbContext();
        var stored = Assert.Single(await assertContext.KnowledgeEntries
            .Where(k => k.CanonicalName == name).ToListAsync());
        Assert.Equal("Original mechanism", stored.MechanismSummary); // untouched by the preview
    }

    [Fact]
    public async Task PreviewUpsertAsync_Reports_Unchanged_When_Nothing_Differs()
    {
        var name = $"Preview-{Guid.NewGuid():N}";
        using (var seedContext = CreateDbContext())
        {
            seedContext.KnowledgeEntries.Add(new KnowledgeEntry
            {
                CanonicalName = name,
                Classification = CompoundCategory.Peptide,
                EvidenceTier = EvidenceTier.Moderate,
                MechanismSummary = "Same mechanism",
            });
            await seedContext.SaveChangesAsync();
        }

        using var context = CreateDbContext();
        var source = new DatabaseKnowledgeSource(context);

        var disposition = await source.PreviewUpsertAsync(new KnowledgeEntry
        {
            CanonicalName = name,
            Classification = CompoundCategory.Peptide,
            EvidenceTier = EvidenceTier.Moderate,
            MechanismSummary = "Same mechanism",
        });

        Assert.Equal(KnowledgeUpsertDisposition.Unchanged, disposition);
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new BioStackDbContext(options);
    }
}
