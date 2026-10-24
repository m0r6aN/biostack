namespace BioStack.Api.Tests;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class DatabaseKnowledgeSourceTests : IDisposable
{
    private readonly SqliteConnection _connection;

    public DatabaseKnowledgeSourceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();
    }

    [Fact]
    public async Task UpsertCompoundAsync_CaseInsensitiveDuplicate_UpdatesLowestIdAndRemovesTheRest()
    {
        // Production held two "Retatrutide" rows; the API served one while Refresh updated
        // the other. The upsert now converges on exactly one row per name.
        var low = Guid.Parse("00000000-0000-0000-0000-0000000000a1");
        var high = Guid.Parse("00000000-0000-0000-0000-0000000000f9");
        using (var seed = CreateDbContext())
        {
            seed.KnowledgeEntries.Add(new KnowledgeEntry { Id = high, CanonicalName = "retatrutide", RegulatoryStatus = "stale" });
            seed.KnowledgeEntries.Add(new KnowledgeEntry { Id = low, CanonicalName = "Retatrutide", RegulatoryStatus = "stale" });
            await seed.SaveChangesAsync();
        }

        using (var write = CreateDbContext())
        {
            var source = new DatabaseKnowledgeSource(write);
            var disposition = await source.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = "Retatrutide",
                RegulatoryStatus = "investigational",
                Details = new KnowledgeEntryDetails { ReviewStatus = "reviewed" },
            });
            Assert.Equal(KnowledgeUpsertDisposition.Updated, disposition);
        }

        using var read = CreateDbContext();
        var row = Assert.Single(await read.KnowledgeEntries.AsNoTracking().ToListAsync(), e => e.CanonicalName.ToLower() == "retatrutide");
        Assert.Equal(low, row.Id);
        Assert.Equal("investigational", row.RegulatoryStatus);
        Assert.Equal("reviewed", row.Details!.ReviewStatus);

        var served = await new DatabaseKnowledgeSource(read).GetCompoundAsync("RETATRUTIDE");
        Assert.Equal(low, served!.Id);
    }

    [Fact]
    public async Task UpsertCompoundAsync_WhenUpdatingExistingEntryWithoutId_PreservesExistingId()
    {
        var canonicalName = $"Integration-{Guid.NewGuid():N}";
        var existingId = Guid.NewGuid();

        using (var seedContext = CreateDbContext())
        {
            seedContext.KnowledgeEntries.Add(new KnowledgeEntry
            {
                Id = existingId,
                CanonicalName = canonicalName,
                Aliases = new List<string> { "Initial Alias" },
                Classification = CompoundCategory.Peptide,
                RegulatoryStatus = "Research-grade peptide",
                MechanismSummary = "Initial mechanism",
                EvidenceTier = EvidenceTier.Moderate,
                SourceReferences = new List<string> { "Initial source" },
                Notes = "Initial notes",
                Pathways = new List<string> { "initial-pathway" },
                Benefits = new List<string> { "Initial benefit" },
                PairsWellWith = new List<string>(),
                AvoidWith = new List<string>(),
                CompatibleBlends = new List<string>(),
                VialCompatibility = "Unknown",
                RecommendedDosage = "100mcg",
                StandardDosageRange = "100mcg-200mcg",
                MaxReportedDose = "500mcg",
                Frequency = "Daily",
                PreferredTimeOfDay = "Morning",
                WeeklyDosageSchedule = new List<string> { "Mon" },
                IncrementalEscalationSteps = new List<string>(),
                DrugInteractions = new List<string>(),
                OptimizationProtein = "Initial protein guidance",
                OptimizationCarbs = "Initial carb guidance",
                OptimizationSupplements = new List<string> { "Initial supplement" },
                OptimizationSleep = "Initial sleep guidance",
                OptimizationExercise = "Initial exercise guidance"
            });

            await seedContext.SaveChangesAsync();
        }

        using (var updateContext = CreateDbContext())
        {
            var source = new DatabaseKnowledgeSource(updateContext);

            await source.UpsertCompoundAsync(new KnowledgeEntry
            {
                CanonicalName = canonicalName,
                Aliases = new List<string> { "Updated Alias" },
                Classification = CompoundCategory.Peptide,
                RegulatoryStatus = "Research-grade peptide",
                MechanismSummary = "Updated mechanism",
                EvidenceTier = EvidenceTier.Moderate,
                SourceReferences = new List<string> { "Updated source" },
                Notes = "Updated notes",
                Pathways = new List<string> { "updated-pathway" },
                Benefits = new List<string> { "Updated benefit" },
                PairsWellWith = new List<string> { "TB-500" },
                AvoidWith = new List<string>(),
                CompatibleBlends = new List<string>(),
                VialCompatibility = "Separate vial preferred",
                RecommendedDosage = "250mcg",
                StandardDosageRange = "200mcg-500mcg",
                MaxReportedDose = "1000mcg",
                Frequency = "Daily",
                PreferredTimeOfDay = "Morning",
                WeeklyDosageSchedule = new List<string> { "Mon", "Thu" },
                IncrementalEscalationSteps = new List<string>(),
                DrugInteractions = new List<string>(),
                OptimizationProtein = "Updated protein guidance",
                OptimizationCarbs = "Updated carb guidance",
                OptimizationSupplements = new List<string> { "Vitamin C" },
                OptimizationSleep = "Updated sleep guidance",
                OptimizationExercise = "Updated exercise guidance"
            });
        }

        using var assertContext = CreateDbContext();
        var updated = Assert.Single(await assertContext.KnowledgeEntries
            .Where(k => k.CanonicalName == canonicalName)
            .ToListAsync());

        Assert.Equal(existingId, updated.Id);
        Assert.Equal("Updated mechanism", updated.MechanismSummary);
        Assert.Equal("Updated notes", updated.Notes);
        Assert.Equal(new[] { "Vitamin C" }, updated.OptimizationSupplements);
    }

    [Theory]
    [InlineData("first alias")]
    [InlineData("middle alias")]
    [InlineData("last alias")]
    [InlineData("CANONICAL ALIAS TEST")]
    public async Task GetCompoundAsync_FindsEntriesByCanonicalNameOrAlias(string lookup)
    {
        var entryId = Guid.NewGuid();

        using (var seedContext = CreateDbContext())
        {
            seedContext.KnowledgeEntries.Add(new KnowledgeEntry
            {
                Id = entryId,
                CanonicalName = "Canonical Alias Test",
                Aliases = new List<string> { "First Alias", "Middle Alias", "Last Alias" },
                Classification = CompoundCategory.Peptide,
                EvidenceTier = EvidenceTier.Moderate
            });

            await seedContext.SaveChangesAsync();
        }

        using var lookupContext = CreateDbContext();
        var source = new DatabaseKnowledgeSource(lookupContext);

        var entry = await source.GetCompoundAsync(lookup);

        Assert.NotNull(entry);
        Assert.Equal(entryId, entry.Id);
    }

    public void Dispose()
    {
        _connection.Dispose();
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;

        return new BioStackDbContext(options);
    }
}
