namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using BioStack.Infrastructure.Persistence;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Discovery;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Owner ruling t101u (2026-10-30): an auto-discovered draft is hidden from every public read until the AI
/// council approves it; curated records stay public with their review label; screened-out junk drafts are
/// retired. Hidden drafts remain in storage for the review queue.
/// </summary>
public sealed class UnreviewedDraftVisibilityTests : IDisposable
{
    private const string Curated = "Curated Compound";
    private const string Draft = "Rituximab";
    private const string Junk = "L01A";

    private readonly SqliteConnection _connection;
    private readonly string _root;
    private readonly string _seedPath;
    private readonly string _draftDir;

    public UnreviewedDraftVisibilityTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        using (var db = CreateDbContext()) db.Database.EnsureCreated();

        _root = Directory.CreateTempSubdirectory("draft-visibility-").FullName;
        _seedPath = Path.Combine(_root, "seed.json");
        _draftDir = Path.Combine(_root, "drafts");
        Directory.CreateDirectory(_draftDir);

        var template = JsonNode.Parse(File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", "substances-seed.json")))!.AsArray()[0]!.ToJsonString();
        var curated = JsonNode.Parse(template)!;
        curated["identity"]!["canonicalName"] = Curated;
        curated["identity"]!["aliases"] = new JsonArray();
        File.WriteAllText(_seedPath, new JsonArray(curated).ToJsonString());

        var drafts = new JsonArray(
            DiscoveryOutputBuilder.BuildDraft(Candidate(Draft), Now),
            DiscoveryOutputBuilder.BuildDraft(Candidate(Junk), Now));
        File.WriteAllText(Path.Combine(_draftDir, "discovered-1.json"), drafts.ToJsonString());
    }

    public void Dispose()
    {
        _connection.Dispose();
        Directory.Delete(_root, recursive: true);
    }

    private static readonly DateTimeOffset Now = new(2026, 10, 30, 12, 0, 0, TimeSpan.Zero);

    private static DiscoveredCandidate Candidate(string name)
        => new(name, Array.Empty<string>(), "Biologic", new[] { "muscle-preservation" },
            new[] { new DiscoveryEvidence("clinicaltrials-gov", "NCT00000001", "A trial", "https://clinicaltrials.gov/study/NCT00000001", null, "PHASE2") },
            new[] { "PHASE2" }, Array.Empty<string>(), Array.Empty<string>(), 3, CorpusMatchStatus.New, null);

    private sealed class DraftGate(bool approveDraft) : IPromotionGate
    {
        public bool IsEnforced => true;
        public PromotionGateDecision Evaluate(string compoundName)
            => approveDraft || compoundName == Curated
                ? PromotionGateDecision.ApprovedByReviewer("ai-council")
                : PromotionGateDecision.Skip(PromotionSkipReason.NoDecision);
    }

    private async Task<(JobRunResult Result, IngestionContext Run)> Refresh(BioStackDbContext db, bool approveDraft, bool dryRun = false)
    {
        var options = new WorkerOptions { DryRun = dryRun, SeedFilePath = _seedPath, DiscoveredRecordDirectory = _draftDir, MaxBatchSize = 50, MaxRetirementsPerRun = 10 };
        var run = new IngestionContext(options, NullLogger.Instance);
        var job = new RefreshJob(BuildPipeline(), new DatabaseKnowledgeSource(db), options, new DraftGate(approveDraft));
        return (await job.RunAsync(run), run);
    }

    [Fact]
    public async Task An_Unreviewed_Draft_Is_Hidden_From_Every_Public_Read_And_Listed_For_Review()
    {
        using var db = CreateDbContext();
        var (result, _) = await Refresh(db, approveDraft: false);
        Assert.True(result.Success, result.ErrorMessage);

        var source = new DatabaseKnowledgeSource(db);
        Assert.Equal(new[] { Curated }, (await source.ListPublicCompoundsAsync()).Select(e => e.CanonicalName));
        Assert.Equal(new[] { Curated }, (await source.GetAllCompoundsAsync()).Select(e => e.CanonicalName));
        Assert.Null(await source.GetCompoundAsync(Draft));
        Assert.Null(await source.GetCompoundAsync("rituximab"));
        Assert.Empty(await source.SearchCompoundsByPathwayAsync("anything"));
        Assert.Equal(Curated, (await source.GetCompoundAsync(Curated))?.CanonicalName);

        // The draft is stored (review queue) and labeled pending review; the curated record is public and reviewed.
        var queue = await source.ListUnreviewedAsync();
        var stored = Assert.Single(queue);
        Assert.Equal(Draft, stored.CanonicalName);
        Assert.False(stored.IsPublic);
        Assert.Equal("pending-review", stored.Details!.ReviewStatus);
        Assert.True((await db.KnowledgeEntries.AsNoTracking().SingleAsync(e => e.CanonicalName == Curated)).IsPublic);
    }

    [Fact]
    public async Task Council_Approval_Publishes_The_Draft_On_The_Next_Refresh()
    {
        using var db = CreateDbContext();
        await Refresh(db, approveDraft: false);
        var source = new DatabaseKnowledgeSource(db);
        Assert.Null(await source.GetCompoundAsync(Draft));

        var (result, run) = await Refresh(db, approveDraft: true);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Equal(1, run.UpdatedCount);

        Assert.Equal(new[] { Curated, Draft }, (await source.ListPublicCompoundsAsync()).Select(e => e.CanonicalName).OrderBy(n => n, StringComparer.Ordinal));
        Assert.Equal(Draft, (await source.GetCompoundAsync(Draft))?.CanonicalName);
        Assert.Empty(await source.ListUnreviewedAsync());
        Assert.Equal("ai-council-reviewed", (await source.GetCompoundAsync(Draft))!.Details!.ReviewStatus);
    }

    [Fact]
    public async Task A_Draft_Whose_Council_Decision_Is_Withdrawn_Is_Hidden_Again()
    {
        using var db = CreateDbContext();
        await Refresh(db, approveDraft: true);
        var source = new DatabaseKnowledgeSource(db);
        Assert.NotNull(await source.GetCompoundAsync(Draft));

        await Refresh(db, approveDraft: false);
        Assert.Null(await source.GetCompoundAsync(Draft));
    }

    [Fact]
    public async Task Screened_Out_Junk_Is_Never_Published_And_A_Stored_Junk_Row_Is_Retired()
    {
        using var db = CreateDbContext();
        // A row stored before screening existed.
        db.KnowledgeEntries.Add(new KnowledgeEntry { Id = Guid.NewGuid(), CanonicalName = Junk, IsPublic = true });
        db.SaveChanges();

        var (result, run) = await Refresh(db, approveDraft: true);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Contains(run.PlanRows, r => r.CanonicalName == Junk && r.Action == "screened-out");
        Assert.Contains(run.PlanRows, r => r.CanonicalName == Junk && r.Action == "retired");

        var junk = await db.KnowledgeEntries.AsNoTracking().SingleAsync(e => e.CanonicalName == Junk);
        Assert.NotNull(junk.RetiredAtUtc);
        var source = new DatabaseKnowledgeSource(db);
        Assert.DoesNotContain(await source.ListPublicCompoundsAsync(), e => e.CanonicalName == Junk);
        Assert.Null(await source.GetCompoundAsync(Junk));
        Assert.DoesNotContain(await source.ListUnreviewedAsync(), e => e.CanonicalName == Junk);
    }

    [Fact]
    public async Task DryRun_Writes_Nothing_And_A_Row_Stored_Before_The_Change_Reads_As_Public()
    {
        using var db = CreateDbContext();
        var (result, run) = await Refresh(db, approveDraft: false, dryRun: true);
        Assert.True(result.Success, result.ErrorMessage);
        Assert.Empty(await db.KnowledgeEntries.AsNoTracking().ToListAsync());
        Assert.Equal(2, run.CreatedCount);   // curated + the draft (junk is screened out)

        // A KnowledgeEntry created without setting visibility is public: existing rows and tests keep today's behaviour.
        Assert.True(new KnowledgeEntry().IsPublic);
    }

    [Theory]
    [InlineData("discovery:clinicaltrials-gov", true)]
    [InlineData("discovered:er-100.json", false)]
    [InlineData("seed", false)]
    public void Only_Discovery_Written_Records_Count_As_Auto_Discovered(string ingestionSource, bool expected)
    {
        var record = new BioStack.KnowledgeWorker.Models.SubstanceRecord();
        record.Ops.IngestionSource = ingestionSource;
        Assert.Equal(expected, KnowledgeVisibility.IsAutoDiscovered(record));
        Assert.Equal(!expected, KnowledgeVisibility.IsPublic(record, councilApproved: false));
        Assert.True(KnowledgeVisibility.IsPublic(record, councilApproved: true));
    }

    [Fact]
    public void The_Curated_ER100_Record_Is_Not_Auto_Discovered()
    {
        var path = Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "discovered-records", "er-100.json");
        var node = JsonNode.Parse(File.ReadAllText(path))!;
        var record = System.Text.Json.JsonSerializer.Deserialize<BioStack.KnowledgeWorker.Models.SubstanceRecord>((node is JsonArray a ? a[0]! : node).ToJsonString())!;
        Assert.False(KnowledgeVisibility.IsAutoDiscovered(record));
        Assert.True(KnowledgeVisibility.IsPublic(record, councilApproved: false));
    }

    private static IngestionPipeline BuildPipeline()
    {
        var schemaPath = Path.Combine(AppContext.BaseDirectory, "Fixtures", "substance-record.schema.json");
        return new IngestionPipeline(
            loader: new SubstanceRecordLoader(),
            validator: SubstanceRecordValidator.LoadFromFile(schemaPath),
            normalizer: new SubstanceRecordNormalizer(),
            trustGate: new TrustGate(),
            canonicalizer: new SubstanceCanonicalizer(),
            logger: NullLogger<IngestionPipeline>.Instance);
    }

    private BioStackDbContext CreateDbContext()
        => new(new DbContextOptionsBuilder<BioStackDbContext>().UseSqlite(_connection).Options);
}
