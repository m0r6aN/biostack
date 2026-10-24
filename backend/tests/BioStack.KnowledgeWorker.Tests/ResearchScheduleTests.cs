namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using BioStack.KnowledgeWorker.Workers;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>Owner ruling 7 (2026-10-24): schedules for new-compound research and updates to existing entries.</summary>
public sealed class ResearchScheduleTests
{
    private static readonly DateTimeOffset Now = new(2026, 12, 1, 6, 0, 0, TimeSpan.Zero);
    private static string SeedPath => Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");
    private static string WatchlistPath => Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "discovery-watchlist.json");
    private static readonly ResearchArtifactValidator Validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

    private static JsonArray Seed() => (JsonArray)JsonNode.Parse(File.ReadAllText(SeedPath))!;

    private static JsonObject Record(string name, string? reviewed, string tier = "Established", string? regulatory = "Approved", params string[] aliases)
        => new()
        {
            ["identity"] = new JsonObject
            {
                ["canonicalName"] = name, ["classification"] = "Peptide",
                ["aliases"] = new JsonArray(aliases.Select(a => (JsonNode?)a).ToArray()), ["brandNames"] = new JsonArray(),
            },
            ["regulatory"] = new JsonObject { ["regulatoryStatus"] = regulatory },
            ["evidence"] = new JsonObject { ["overallTier"] = tier },
            ["provenance"] = new JsonObject { ["lastReviewedAt"] = reviewed },
        };

    [Fact]
    public void Full_Corpus_Plan_Covers_Every_Stale_Entry_And_Is_Schema_Valid()
    {
        var batch = ResearchSchedulePlanner.Plan(Seed(), Array.Empty<DiscoveryCandidate>(),
            new ResearchScheduleSettings(true, false, 30, 1000, 0), Now)!;
        Assert.True(Validator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch).IsValid);
        Assert.Equal(158, ((JsonArray)batch["requests"]!).Count);
        Assert.All((JsonArray)batch["requests"]!, r => Assert.StartsWith("update-", r!["requestId"]!.GetValue<string>()));
    }

    [Fact]
    public void Update_Checks_Skip_Fresh_Entries_Order_Oldest_First_And_Prioritize_Weak()
    {
        var seed = new JsonArray(
            Record("Fresh", "2026-11-25T00:00:00Z"),
            Record("Newer", "2026-09-01T00:00:00Z"),
            Record("Older", "2026-01-01T00:00:00Z", tier: "Plausible Theory"),
            Record("NoRegulatory", "2026-05-01T00:00:00Z", regulatory: null),
            Record("Never", null));
        var batch = ResearchSchedulePlanner.Plan(seed, Array.Empty<DiscoveryCandidate>(),
            new ResearchScheduleSettings(true, false, 30, 10, 0), Now)!;
        var requests = ((JsonArray)batch["requests"]!).Select(r => r!).ToList();
        Assert.Equal(new[] { "Never", "Older", "NoRegulatory", "Newer" }, requests.Select(r => r["compoundName"]!.GetValue<string>()));
        Assert.Equal("high", requests[1]["priority"]!.GetValue<string>());
        Assert.Equal("high", requests[2]["priority"]!.GetValue<string>());
        Assert.Equal("normal", requests[3]["priority"]!.GetValue<string>());
        Assert.True(Validator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch).IsValid);
    }

    [Fact]
    public async Task Successive_Passes_Rotate_Through_The_Whole_Corpus()
    {
        var output = Path.Combine(Path.GetTempPath(), "biostack-rotation-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.ResearchPlanning, SeedFilePath = SeedPath, ResearchPlanningIncludeDiscovery = false,
                ResearchPlanningOutputDirectory = output, MaxBatchSize = 50,
            };
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            for (var day = 0; day < 4; day++)
            {
                var result = await new ResearchPlanningJob(options, Validator, new FixedClock(Now.AddDays(day)))
                    .RunAsync(new IngestionContext(options, NullLogger.Instance));
                Assert.True(result.Success);
            }
            foreach (var file in Directory.GetFiles(output))
                foreach (var r in JsonNode.Parse(File.ReadAllText(file))!["requests"]!.AsArray())
                    Assert.True(names.Add(r!["compoundName"]!.GetValue<string>()), "requested twice within the stale window");
            Assert.Equal(158, names.Count);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public void Update_Checks_Respect_The_Batch_Cap()
    {
        var batch = ResearchSchedulePlanner.Plan(Seed(), Array.Empty<DiscoveryCandidate>(),
            new ResearchScheduleSettings(true, false, 30, 25, 0), Now)!;
        Assert.Equal(25, ((JsonArray)batch["requests"]!).Count);
    }

    [Fact]
    public void Discovery_Skips_Compounds_Already_In_The_Corpus_By_Name_Or_Alias()
    {
        var seed = new JsonArray(Record("Retatrutide", "2026-11-29T00:00:00Z", aliases: "LY3437943"));
        var watch = new List<DiscoveryCandidate>
        {
            new("retatrutide", Array.Empty<string>(), "Peptide", Array.Empty<string>(), null),
            new("Triple G", new[] { "LY3437943" }, "Peptide", Array.Empty<string>(), null),
            new("Orforglipron", new[] { "LY3502970" }, "SmallMolecule", new[] { "glp-1" }, null),
        };
        var batch = ResearchSchedulePlanner.Plan(seed, watch, new ResearchScheduleSettings(true, true, 30, 10, 10), Now)!;
        var request = Assert.Single((JsonArray)batch["requests"]!)!;
        Assert.Equal("discover-orforglipron-20261201", request["requestId"]!.GetValue<string>());
        Assert.Contains("scheduled-discovery", request["categories"]!.AsArray().Select(c => c!.GetValue<string>()));
        Assert.True(Validator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch).IsValid);
    }

    [Fact]
    public void Nothing_Due_Returns_Null_So_No_Empty_Batch_Is_Written()
        => Assert.Null(ResearchSchedulePlanner.Plan(new JsonArray(Record("Fresh", "2026-11-29T00:00:00Z")),
            Array.Empty<DiscoveryCandidate>(), new ResearchScheduleSettings(true, true, 30, 10, 10), Now));

    [Fact]
    public void Shipped_Watchlist_Parses_And_Has_No_Corpus_Duplicates()
    {
        var watch = ResearchSchedulePlanner.ParseWatchlist(JsonNode.Parse(File.ReadAllText(WatchlistPath)));
        Assert.NotEmpty(watch);
        var batch = ResearchSchedulePlanner.Plan(Seed(), watch, new ResearchScheduleSettings(false, true, 30, 0, 100), Now)!;
        Assert.Equal(watch.Count, ((JsonArray)batch["requests"]!).Count);
    }

    [Fact]
    public async Task Job_Writes_A_Valid_Batch_And_DryRun_Writes_Nothing()
    {
        var output = Path.Combine(Path.GetTempPath(), "biostack-planning-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new WorkerOptions
            {
                RunMode = RunMode.ResearchPlanning, SeedFilePath = SeedPath, ResearchDiscoveryWatchlistPath = WatchlistPath,
                ResearchPlanningOutputDirectory = output, MaxBatchSize = 20,
            };
            var clock = new FixedClock(Now);
            var dry = await new ResearchPlanningJob(new WorkerOptions
            {
                RunMode = RunMode.ResearchPlanning, SeedFilePath = SeedPath, ResearchDiscoveryWatchlistPath = WatchlistPath,
                ResearchPlanningOutputDirectory = output, MaxBatchSize = 20, DryRun = true,
            }, Validator, clock).RunAsync(new IngestionContext(new WorkerOptions { DryRun = true }, NullLogger.Instance));
            Assert.True(dry.Success);
            Assert.False(Directory.Exists(output));

            var result = await new ResearchPlanningJob(options, Validator, clock)
                .RunAsync(new IngestionContext(options, NullLogger.Instance));
            Assert.True(result.Success);
            var file = Assert.Single(Directory.GetFiles(output));
            var batch = JsonNode.Parse(File.ReadAllText(file))!;
            Assert.True(Validator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch).IsValid);
            Assert.Equal(result.CreatedCount, batch["requests"]!.AsArray().Count);
        }
        finally
        {
            if (Directory.Exists(output)) Directory.Delete(output, recursive: true);
        }
    }

    [Fact]
    public async Task Loop_Repeats_Survives_A_Failing_Pass_And_Stops_On_Cancel()
    {
        var calls = 0;
        var reports = new List<(int, bool, Exception?)>();
        var count = await WorkerLoop.RunAsync(_ =>
        {
            calls++;
            if (calls == 2) throw new InvalidOperationException("boom");
            return Task.FromResult(true);
        }, TimeSpan.FromMilliseconds(1), (n, ok, ex) => reports.Add((n, ok, ex)), maxIterations: 3);
        Assert.Equal(3, count);
        Assert.False(reports[1].Item2);
        Assert.NotNull(reports[1].Item3);

        using var cts = new CancellationTokenSource();
        var cancelled = await WorkerLoop.RunAsync(_ => { cts.Cancel(); return Task.FromResult(true); },
            TimeSpan.FromHours(1), (_, _, _) => { }, cancellationToken: cts.Token);
        Assert.Equal(1, cancelled);
    }

    [Theory]
    [InlineData(24, null, 24 * 60)]
    [InlineData(0, null, 60)]
    [InlineData(24, 15, 15)]
    public void Loop_Interval_Resolves(int hours, int? minutes, int expectedMinutes)
        => Assert.Equal(TimeSpan.FromMinutes(expectedMinutes), WorkerLoop.ResolveInterval(hours, minutes));

    [Fact]
    public void Schedule_Bicep_Uses_Real_RunModes_And_No_Inline_Secrets()
    {
        var bicep = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot(), "infra", "azure", "knowledge-schedules.bicep"));
        var modes = System.Text.RegularExpressions.Regex.Matches(bicep, @"'Worker__RunMode', value: '(\w+)'")
            .Select(m => m.Groups[1].Value).Distinct().ToList();
        Assert.Contains("ResearchPlanning", modes);
        Assert.Contains("Refresh", modes);
        Assert.All(modes, m => Assert.True(Enum.TryParse<RunMode>(m, out _), m));
        Assert.DoesNotContain("Password=", bicep, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secretRef: 'db-conn-string'", bicep);
        Assert.Matches(@"keyVaultUrl: dbConnectionStringSecretUri", bicep);
    }

    [Fact]
    public void ResearchPlanning_Is_Database_Free()
        => Assert.True(WorkerRunModePolicy.IsDatabaseFree(RunMode.ResearchPlanning));

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
