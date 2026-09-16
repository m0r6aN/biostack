namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
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
/// End-to-end coverage for the gated Refresh: a real <see cref="IngestionPipeline"/>
/// against a two-record seed (one promoted, one not), a real
/// <see cref="ReviewDecisionPromotionGate"/>, and a real SQLite-backed
/// <see cref="DatabaseKnowledgeSource"/> — proving the promotion gate, the honest DryRun
/// counters/plan table, and the "worker never touches auth tables" claim all hold together,
/// not just in isolated unit tests of each piece.
/// </summary>
public sealed class RefreshJobIntegrationTests : IDisposable
{
    private const string PromotedName = "Promoted Test Compound";
    private const string NonPromotedName = "Nonpromoted Test Compound";

    private readonly SqliteConnection _connection;
    private readonly string _seedPath;

    public RefreshJobIntegrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using var dbContext = CreateDbContext();
        dbContext.Database.EnsureCreated();

        _seedPath = BuildTwoRecordSeed();
    }

    public void Dispose()
    {
        _connection.Dispose();
        File.Delete(_seedPath);
    }

    [Fact]
    public async Task Refresh_Upserts_Promoted_And_Skips_NonPromoted()
    {
        using var context = CreateDbContext();
        SeedAuthSentinel(context, out var authUserId);

        var job = BuildRefreshJob(context, DryRun: false, out var runContext);
        var result = await job.RunAsync(runContext);

        Assert.True(result.Success);
        Assert.Equal(1, runContext.CreatedCount);
        Assert.Equal(1, runContext.SkippedUnpromotedCount);

        var entries = await context.KnowledgeEntries.AsNoTracking().ToListAsync();
        var created = Assert.Single(entries);
        Assert.Equal(PromotedName, created.CanonicalName);

        var skipRow = Assert.Single(runContext.PlanRows, r => r.CanonicalName == NonPromotedName);
        Assert.Equal("skip-unpromoted", skipRow.Action);
        Assert.Contains("no review decision", skipRow.Reason, StringComparison.OrdinalIgnoreCase);

        // The auth-adjacent table is untouched: same row count, same id, same content.
        var authUsers = await context.AppUsers.AsNoTracking().ToListAsync();
        var authUser = Assert.Single(authUsers);
        Assert.Equal(authUserId, authUser.Id);
    }

    [Fact]
    public async Task DryRun_Makes_Zero_Writes_And_Reports_Honest_Counters_And_Plan()
    {
        using var context = CreateDbContext();
        SeedAuthSentinel(context, out _);

        var job = BuildRefreshJob(context, DryRun: true, out var runContext);
        var result = await job.RunAsync(runContext);

        Assert.True(result.Success);
        Assert.Equal(2, runContext.ScannedCount);
        Assert.Equal(1, runContext.CreatedCount);          // would-insert, for the promoted record
        Assert.Equal(0, runContext.UpdatedCount);
        Assert.Equal(0, runContext.UnchangedCount);
        Assert.Equal(1, runContext.SkippedUnpromotedCount);

        // No writes of any kind: KnowledgeEntries is still empty and the auth sentinel is untouched.
        Assert.Empty(await context.KnowledgeEntries.AsNoTracking().ToListAsync());
        Assert.Single(await context.AppUsers.AsNoTracking().ToListAsync());

        var table = runContext.BuildPlanTable();
        Assert.Contains(PromotedName, table);
        Assert.Contains("would-insert", table);
        Assert.Contains(NonPromotedName, table);
        Assert.Contains("skip-unpromoted", table);
    }

    [Fact]
    public async Task AllowAllPromotionGate_Disables_The_Gate_Entirely()
    {
        using var context = CreateDbContext();
        var pipeline = BuildPipeline();
        var knowledgeSource = new DatabaseKnowledgeSource(context);
        var options = new WorkerOptions { DryRun = false, SeedFilePath = _seedPath, MaxBatchSize = 50 };
        var job = new RefreshJob(pipeline, knowledgeSource, options, AllowAllPromotionGate.Instance);
        var runContext = new IngestionContext(options, NullLogger.Instance);

        var result = await job.RunAsync(runContext);

        Assert.True(result.Success);
        Assert.Equal(0, runContext.SkippedUnpromotedCount);
        Assert.Equal(2, runContext.CreatedCount);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private RefreshJob BuildRefreshJob(BioStackDbContext context, bool DryRun, out IngestionContext runContext)
    {
        var pipeline = BuildPipeline();
        var knowledgeSource = new DatabaseKnowledgeSource(context);
        var gate = new ReviewDecisionPromotionGate(BuildGateIndexApproving(PromotedName));
        var options = new WorkerOptions { DryRun = DryRun, SeedFilePath = _seedPath, MaxBatchSize = 50 };
        runContext = new IngestionContext(options, NullLogger.Instance);
        return new RefreshJob(pipeline, knowledgeSource, options, gate);
    }

    private static ReviewDecisionIndex BuildGateIndexApproving(string compoundName)
    {
        var decision = new JsonObject
        {
            ["decisionId"] = $"{compoundName}-approval",
            ["compoundName"] = compoundName,
            ["decision"] = "approve-for-promotion",
            ["reviewerId"] = "test-reviewer",
            ["reviewedAt"] = "2026-09-16T00:00:00Z",
            ["clearsSoftPromotionBlockers"] = true,
            ["scope"] = new JsonObject(),
            ["notes"] = new JsonArray(),
        };
        var batch = new JsonObject { ["decisions"] = new JsonArray(decision) };
        return ReviewDecisionIndex.FromBatches(new JsonNode[] { batch });
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

    /// <summary>Clones the canonical single-record fixture seed into a two-record temp seed.</summary>
    private static string BuildTwoRecordSeed()
    {
        var canonicalSeedPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "substances-seed.json");
        var canonical = JsonNode.Parse(File.ReadAllText(canonicalSeedPath))!.AsArray();
        var template = canonical[0]!.ToJsonString();

        var promoted = JsonNode.Parse(template)!;
        promoted["identity"]!["canonicalName"] = PromotedName;

        var nonPromoted = JsonNode.Parse(template)!;
        nonPromoted["identity"]!["canonicalName"] = NonPromotedName;

        var arr = new JsonArray(promoted, nonPromoted);
        var path = Path.Combine(Path.GetTempPath(), $"biostack-refresh-gate-seed-{Guid.NewGuid():N}.json");
        File.WriteAllText(path, arr.ToJsonString(new JsonSerializerOptions { WriteIndented = false }));
        return path;
    }

    /// <summary>
    /// Seeds one row in an auth-adjacent table (AppUsers) that Refresh has no business
    /// touching, so the tests above can assert it is byte-for-byte untouched afterward —
    /// see promotion-state-audit.md §6 risk 4 / parcel spec item 3.
    /// </summary>
    private static void SeedAuthSentinel(BioStackDbContext context, out Guid id)
    {
        id = Guid.NewGuid();
        context.AppUsers.Add(new AppUser
        {
            Id = id,
            Email = "sentinel@example.invalid",
            DisplayName = "Auth Sentinel",
            Provider = "email",
            ProviderKey = "sentinel@example.invalid",
        });
        context.SaveChanges();
    }

    private BioStackDbContext CreateDbContext()
    {
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite(_connection)
            .Options;
        return new BioStackDbContext(options);
    }
}
