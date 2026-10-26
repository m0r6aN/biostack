namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Ledger gate G1: Refresh soft-retires published records that are no longer in the seed,
/// marks merged records as superseded so old names still resolve, reports retirements in
/// DryRun without writing, and refuses a mass retirement.
/// </summary>
public sealed class RefreshRetirementTests : IDisposable
{
    private const string Successor = "Successor Compound";
    private const string Kept = "Kept Compound";
    private const string Merged = "Merged Compound";   // alias of Successor in the seed
    private const string Orphan = "Orphan Compound";   // in no seed record at all

    private readonly SqliteConnection _connection;
    private readonly string _seedPath;

    public RefreshRetirementTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var db = CreateDbContext();
        db.Database.EnsureCreated();
        _seedPath = BuildSeed();
    }

    public void Dispose()
    {
        _connection.Dispose();
        File.Delete(_seedPath);
    }

    [Fact]
    public async Task Refresh_Retires_Records_Missing_From_Seed_And_Marks_Merged_Ones_Superseded()
    {
        using var db = CreateDbContext();
        SeedStoredRow(db, Merged);
        SeedStoredRow(db, Orphan);

        var (job, run) = BuildJob(db, dryRun: false);
        var result = await job.RunAsync(run);

        Assert.True(result.Success);
        Assert.Equal(2, run.RetiredCount);

        var rows = await db.KnowledgeEntries.AsNoTracking().ToListAsync();
        Assert.Equal(4, rows.Count); // soft retirement keeps the rows
        var merged = Assert.Single(rows, r => r.CanonicalName == Merged);
        Assert.NotNull(merged.RetiredAtUtc);
        Assert.Equal(Successor, merged.SupersededBy);
        var orphan = Assert.Single(rows, r => r.CanonicalName == Orphan);
        Assert.NotNull(orphan.RetiredAtUtc);
        Assert.Null(orphan.SupersededBy);

        Assert.Contains(run.PlanRows, r => r.CanonicalName == Merged && r.Action == "retired" && r.Reason.Contains(Successor));
        Assert.Contains(run.PlanRows, r => r.CanonicalName == Orphan && r.Action == "retired");

        // Public reads skip retired rows; a lookup by the merged name resolves to its successor.
        var source = new DatabaseKnowledgeSource(db);
        var listed = (await source.ListPublicCompoundsAsync()).Select(e => e.CanonicalName).OrderBy(n => n).ToList();
        Assert.Equal(new[] { Kept, Successor }, listed);
        Assert.DoesNotContain(await source.GetAllCompoundsAsync(), e => e.RetiredAtUtc is not null);
        Assert.Equal(Successor, (await source.GetCompoundAsync(Merged))?.CanonicalName);
        Assert.Equal(Successor, (await source.GetCompoundAsync("merged-compound"))?.CanonicalName);
        Assert.Null(await source.GetCompoundAsync(Orphan));
    }

    [Fact]
    public async Task DryRun_Reports_Would_Retire_And_Writes_Nothing()
    {
        using var db = CreateDbContext();
        SeedStoredRow(db, Merged);

        var (job, run) = BuildJob(db, dryRun: true);
        var result = await job.RunAsync(run);

        Assert.True(result.Success);
        Assert.Equal(1, run.RetiredCount);
        Assert.Contains(run.PlanRows, r => r.CanonicalName == Merged && r.Action == "would-retire" && r.Reason.Contains(Successor));
        Assert.Contains("would-retire", run.BuildPlanTable());
        var stored = Assert.Single(await db.KnowledgeEntries.AsNoTracking().ToListAsync());
        Assert.Null(stored.RetiredAtUtc);
    }

    [Fact]
    public async Task More_Retirements_Than_The_Limit_Retire_Nothing_And_Fail_The_Run()
    {
        using var db = CreateDbContext();
        SeedStoredRow(db, Merged);
        SeedStoredRow(db, Orphan);

        var (job, run) = BuildJob(db, dryRun: false, maxRetirements: 1);
        var result = await job.RunAsync(run);

        Assert.False(result.Success);
        Assert.Equal(0, run.RetiredCount);
        Assert.Equal(2, run.PlanRows.Count(r => r.Action == "retire-blocked"));
        Assert.All(await db.KnowledgeEntries.AsNoTracking().ToListAsync(), r => Assert.Null(r.RetiredAtUtc));
    }

    [Fact]
    public async Task A_Retired_Record_Back_In_The_Seed_Is_Published_Again()
    {
        using var db = CreateDbContext();
        var row = SeedStoredRow(db, Kept);
        row.RetiredAtUtc = DateTime.UtcNow.AddDays(-1);
        row.SupersededBy = Successor;
        db.SaveChanges();

        var (job, run) = BuildJob(db, dryRun: false);
        var result = await job.RunAsync(run);

        Assert.True(result.Success);
        var kept = Assert.Single(await db.KnowledgeEntries.AsNoTracking().Where(e => e.CanonicalName == Kept).ToListAsync());
        Assert.Null(kept.RetiredAtUtc);
        Assert.Null(kept.SupersededBy);
        Assert.Equal(0, run.RetiredCount);
    }

    [Fact]
    public async Task Retirement_Can_Be_Disabled()
    {
        using var db = CreateDbContext();
        SeedStoredRow(db, Orphan);

        var (job, run) = BuildJob(db, dryRun: false, maxRetirements: 0);
        var result = await job.RunAsync(run);

        Assert.True(result.Success);
        Assert.Equal(0, run.RetiredCount);
        Assert.Null(Assert.Single(await db.KnowledgeEntries.AsNoTracking().Where(e => e.CanonicalName == Orphan).ToListAsync()).RetiredAtUtc);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private (RefreshJob Job, IngestionContext Run) BuildJob(BioStackDbContext db, bool dryRun, int maxRetirements = 10)
    {
        var options = new WorkerOptions { DryRun = dryRun, SeedFilePath = _seedPath, MaxBatchSize = 50, MaxRetirementsPerRun = maxRetirements };
        var run = new IngestionContext(options, NullLogger.Instance);
        var job = new RefreshJob(BuildPipeline(), new DatabaseKnowledgeSource(db), options, AllowAllPromotionGate.Instance);
        return (job, run);
    }

    private static KnowledgeEntry SeedStoredRow(BioStackDbContext db, string name)
    {
        var entry = new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = name };
        db.KnowledgeEntries.Add(entry);
        db.SaveChanges();
        return entry;
    }

    private static IngestionPipeline BuildPipeline()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "substance-record.schema.json");
        return new IngestionPipeline(
            loader:        new SubstanceRecordLoader(),
            validator:     SubstanceRecordValidator.LoadFromFile(schemaPath),
            normalizer:    new SubstanceRecordNormalizer(),
            trustGate:     new TrustGate(),
            canonicalizer: new SubstanceCanonicalizer(),
            logger:        NullLogger<IngestionPipeline>.Instance);
    }

    /// <summary>Two-record seed: Successor (aliased as Merged) and Kept.</summary>
    private static string BuildSeed()
    {
        var canonical = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "substances-seed.json")))!.AsArray();
        var template = canonical[0]!.ToJsonString();

        var successor = JsonNode.Parse(template)!;
        successor["identity"]!["canonicalName"] = Successor;
        successor["identity"]!["aliases"] = new JsonArray(Merged);

        var kept = JsonNode.Parse(template)!;
        kept["identity"]!["canonicalName"] = Kept;
        kept["identity"]!["aliases"] = new JsonArray();

        var path = Path.Combine(Path.GetTempPath(), $"biostack-refresh-retire-seed-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, new JsonArray(successor, kept).ToJsonString());
        return path;
    }

    private BioStackDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite(_connection).Options);
}
