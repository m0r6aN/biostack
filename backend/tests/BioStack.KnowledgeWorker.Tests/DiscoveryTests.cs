namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.Domain.Enums;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Discovery;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Open discovery (owner report 2026-10-24: "BioStack has no information on ER-100 ... the research
/// process isn't finding these substances and therapies"). Fixtures under Fixtures/discovery are
/// trimmed real API responses (ClinicalTrials.gov v2, PubMed E-utilities, openFDA, 2026-10-24) except
/// the bioRxiv page, which is synthetic in the API's shape.
/// </summary>
public sealed class DiscoveryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 24, 12, 0, 0, TimeSpan.Zero);
    private static string WorkerDir => Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker");
    private static string SeedPath => Path.Combine(WorkerDir, "Seeds", "substances-seed.json");
    private static string ScopePath => Path.Combine(WorkerDir, "Seeds", "discovery-scope.json");
    private static string CuratedDir => Path.Combine(WorkerDir, "Seeds", "discovered-records");
    private static string FixtureDir => Path.Combine(TestPaths.BackendRoot(), "tests", "BioStack.KnowledgeWorker.Tests", "Fixtures", "discovery");
    private static readonly ResearchArtifactValidator ArtifactValidator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());
    private static readonly SubstanceRecordValidator RecordValidator =
        SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json"));

    private static DiscoveryScope Scope() => DiscoveryScope.Load(ScopePath);
    private static JsonArray Seed() => (JsonArray)JsonNode.Parse(File.ReadAllText(SeedPath))!;
    private static JsonNode Fixture(string name) => JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDir, name)))!;

    private sealed class FixtureHandler : HttpMessageHandler
    {
        public List<string> Urls { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            Urls.Add(url);
            var file = url switch
            {
                _ when url.Contains("clinicaltrials.gov/api/v2/studies") => "ctgov-studies.json",
                _ when url.Contains("esearch.fcgi") => "pubmed-esearch.json",
                _ when url.Contains("esummary.fcgi") => "pubmed-esummary.json",
                _ when url.Contains("api.fda.gov") => "openfda-drugsfda.json",
                _ when url.Contains("api.biorxiv.org") => "biorxiv-details.json",
                _ => null,
            };
            if (file is null) return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(File.ReadAllText(Path.Combine(FixtureDir, file)), Encoding.UTF8, "application/json"),
            });
        }
    }

    private static DiscoveryOptions Options(string outDir, string? email = "owner@example.com") => new()
    {
        ScopePath = ScopePath,
        OutputDirectory = outDir,
        LookbackDays = 60,
        RequestDelayMilliseconds = 0,
        MaxPagesPerQuery = 1,
        OpenFdaMaxPages = 1,
        PreprintMaxPagesPerServer = 1,
        NcbiEmail = email,
        NcbiApiKey = "test-ncbi-key-not-real",
    };

    // ── Names ───────────────────────────────────────────────────────────────

    [Theory]
    [InlineData("ER-100 epigenetic therapy", "ER-100")]
    [InlineData("Retatrutide (LY3437943) phase 3", "LY3437943")]
    [InlineData("SLU-PP-332 exercise mimetic", "SLU-PP-332")]
    [InlineData("UBT251, a novel triple agonist", "UBT251")]
    [InlineData("CT-388 in obesity", "CT-388")]
    [InlineData("LOY-002 dog lifespan", "LOY-002")]
    public void Code_Names_Are_Extracted(string text, string expected)
        => Assert.Contains(expected, CandidateNames.ExtractCodeNames(text, Scope().CodeNamePrefixDenylist));

    [Theory]
    [InlineData("NCT07290244 registry entry")]
    [InlineData("COVID-19 outcomes")]
    [InlineData("IL-6 and GLP-1 signaling")]
    [InlineData("Week 52 results in phase 12")]
    [InlineData("CD34 positive cells")]
    public void Non_Compound_Codes_Are_Ignored(string text)
        => Assert.Empty(CandidateNames.ExtractCodeNames(text, Scope().CodeNamePrefixDenylist));

    [Fact]
    public void Normalization_Matches_Hyphen_Space_And_Case_Variants()
    {
        Assert.Equal("er100", CandidateNames.Normalize("ER-100"));
        Assert.Equal(CandidateNames.Normalize("er 100"), CandidateNames.Normalize("ER100"));
        Assert.Equal(CandidateNames.Normalize("SLU-PP-332"), CandidateNames.Normalize("slu pp 332"));
    }

    [Fact]
    public void Inn_Stems_Find_Names_And_Skip_English_Words()
    {
        var names = CandidateNames.ExtractInnNames("Orforglipron versus semaglutide peptide and nucleotide; nusinersen; bimagrumab", Scope().InnStems)
            .ToDictionary(n => n.Name, n => n.Classification);
        Assert.Equal("Small Molecule", names["Orforglipron"]);
        Assert.Equal("Peptide", names["Semaglutide"]);
        Assert.Equal("Oligonucleotide", names["Nusinersen"]);
        Assert.Equal("Biologic", names["Bimagrumab"]);
        Assert.DoesNotContain("Peptide", names.Keys);
        Assert.DoesNotContain("Nucleotide", names.Keys);
    }

    // ── Sources ─────────────────────────────────────────────────────────────

    [Fact]
    public void ClinicalTrials_Finds_ER100_As_An_Epigenetic_Therapy_And_Drops_Noise()
    {
        var scope = Scope();
        var area = scope.Areas.Single(a => a.AreaId == "gene-epigenetic-therapy");
        var types = new HashSet<string>(scope.ClinicalTrialsInterventionTypes);
        var hits = ((JsonArray)Fixture("ctgov-studies.json")["studies"]!).OfType<JsonObject>()
            .SelectMany(s => ClinicalTrialsGovDiscoverySource.ParseStudy(s, area, scope, types)).ToList();

        // PIMS screening (phase NA) and rTMS (device) are dropped. The rectal-cancer trial is NOT dropped any more:
        // oncology is no longer out of scope (owner direction t101u), so its drug comes through for council review.
        Assert.Equal(new[] { "ER-100", "Serplulimab" }, hits.Select(h => h.Name));
        Assert.Equal("Biologic", hits[1].Classification);
        var er100 = hits[0];
        Assert.Equal("ER-100", er100.Name);
        Assert.Equal("Epigenetic Therapy", er100.Classification);
        Assert.Contains("OSK epigenetic therapy", er100.Aliases);
        Assert.Equal("NCT07290244", er100.Evidence.Identifier);
        Assert.Equal("https://clinicaltrials.gov/study/NCT07290244", er100.Evidence.Url);
        Assert.Equal("PHASE1", er100.Phase);
        Assert.Equal("Life Biosciences Inc.", er100.Sponsor);
    }

    [Fact]
    public void ClinicalTrials_Query_Is_Open_Ended_And_Date_Bounded()
    {
        var area = Scope().Areas.Single(a => a.AreaId == "gene-epigenetic-therapy");
        var url = Uri.UnescapeDataString(ClinicalTrialsGovDiscoverySource.BuildUrl(area,
            new DiscoveryWindow(new DateOnly(2026, 9, 24), new DateOnly(2026, 10, 24)), 100, null));
        Assert.StartsWith("https://clinicaltrials.gov/api/v2/studies?", url);
        Assert.Contains("query.term=\"gene therapy\" OR \"epigenetic reprogramming\"", url);
        Assert.Contains("AREA[StudyFirstPostDate]RANGE[2026-09-24,2026-10-24]", url);
        Assert.DoesNotContain("query.intr", url); // not a lookup of names we already know
    }

    [Fact]
    public void Combination_Arms_Are_Split_And_Placebo_Dropped()
    {
        var scope = Scope();
        var area = scope.Areas[0];
        var hits = ClinicalTrialsGovDiscoverySource.CandidatesFromIntervention("Petrelintide + Placebo", Array.Empty<string>(), "DRUG", area, scope).ToList();
        var hit = Assert.Single(hits);
        Assert.Equal("Petrelintide", hit.Name);
        Assert.Equal("Peptide", hit.Classification);
    }

    [Fact]
    public void PubMed_Titles_Yield_Code_And_Inn_Names()
    {
        var scope = Scope();
        var names = PubMedDiscoverySource.ParseSummary(Fixture("pubmed-esummary.json"), scope.Areas[0], scope).Select(h => h.Name).ToList();
        Assert.Contains("UBT251", names);
        Assert.Contains("Orforglipron", names);
        Assert.Contains("Retatrutide", names);
        Assert.DoesNotContain("Polypeptide", names);
    }

    [Fact]
    public async Task PubMed_Is_Skipped_Without_A_Contact_Email()
    {
        var handler = new FixtureHandler();
        var source = new PubMedDiscoverySource(new DiscoveryHttp(new HttpClient(handler), TimeSpan.Zero, "t/1"), Options(Path.GetTempPath(), email: null));
        var result = await source.DiscoverAsync(Scope(), new DiscoveryWindow(new DateOnly(2026, 9, 1), new DateOnly(2026, 10, 24)), default);
        Assert.Empty(handler.Urls);
        Assert.Contains(result.Errors, e => e.Contains("NcbiEmail"));
    }

    [Fact]
    public void OpenFda_Keeps_New_Molecular_Entities_In_Window_Only()
    {
        var hits = OpenFdaDiscoverySource.ParseResults((JsonArray)Fixture("openfda-drugsfda.json")["results"]!,
            new DiscoveryWindow(new DateOnly(2026, 8, 25), new DateOnly(2026, 10, 24)), Scope()).ToList();
        var hit = Assert.Single(hits);
        Assert.Equal("Zilganersen", hit.Name); // salt stripped; Breztri (type 5), old originals and March approvals excluded
        Assert.Equal("Oligonucleotide", hit.Classification);
        Assert.Equal("FDA-approved", hit.Phase);
        Assert.Contains("Zanvastro", hit.Aliases);
    }

    [Fact]
    public void Preprints_Match_Scope_Keywords_And_Are_Labelled()
    {
        var hits = PreprintDiscoverySource.ParseCollection((JsonArray)Fixture("biorxiv-details.json")["collection"]!, Scope()).ToList();
        var hit = Assert.Single(hits);
        Assert.Equal("XRP-2041", hit.Name);
        Assert.StartsWith("[Preprint, not peer reviewed]", hit.Evidence.Title);
    }

    // ── Corpus matching ─────────────────────────────────────────────────────

    [Fact]
    public void Corpus_Index_Matches_Aliases_And_Flags_Possible_Missing_Aliases()
    {
        var corpus = new JsonArray(
            new JsonObject { ["identity"] = new JsonObject { ["canonicalName"] = "Foo", ["aliases"] = new JsonArray("FOO-12") },
                             ["mechanism"] = new JsonObject { ["mechanismSummary"] = "Foo (barazepam) is studied for X." } },
            new JsonObject { ["identity"] = new JsonObject { ["canonicalName"] = "Qux", ["aliases"] = new JsonArray() } });
        var index = CorpusNameIndex.Build(corpus);
        Assert.Equal("Foo", index.FindRecord(new[] { "foo 12" }));
        Assert.Null(index.FindRecord(new[] { "Barazepam" }));
        Assert.Equal("Foo", index.FindPossibleAliasOwner(new[] { "Barazepam" }));
        Assert.Null(index.FindPossibleAliasOwner(new[] { "Unmentioned" }));
    }

    [Fact]
    public void Enobosarm_Is_Now_An_Ostarine_Alias()
    {
        var index = CorpusNameIndex.Build(Seed());
        Assert.Equal("Ostarine", index.FindRecord(new[] { "Enobosarm" }));
        Assert.Equal("Ostarine", index.FindRecord(new[] { "GTx-024" }));
    }

    [Fact]
    public void Aggregator_Merges_By_Alias_And_Scores_Independent_Sources()
    {
        var e1 = new DiscoveryEvidence("clinicaltrials-gov", "NCT1", "t", "u", null, null);
        var e2 = new DiscoveryEvidence("pubmed", "PMID:1", "t", "u", null, null);
        var hits = new[]
        {
            new DiscoveryHit("Orforglipron", new[] { "LY3502970" }, "Small Molecule", "incretin-obesity", e1),
            new DiscoveryHit("LY-3502970", Array.Empty<string>(), "Research Compound", "incretin-obesity", e2),
        };
        var c = Assert.Single(DiscoveryAggregator.Aggregate(hits, CorpusNameIndex.Build(new JsonArray()), Scope().CodeNamePrefixDenylist));
        Assert.Equal("Orforglipron", c.Name);
        Assert.Equal("Small Molecule", c.Classification);
        Assert.Equal(5, c.Score);
        Assert.Equal(CorpusMatchStatus.New, c.Status);
    }

    // ── End to end ──────────────────────────────────────────────────────────

    private static async Task<(JobRunResult Result, string Root, FixtureHandler Handler)> RunJob(string? curatedDir)
    {
        var root = Directory.CreateTempSubdirectory("discovery-").FullName;
        var handler = new FixtureHandler();
        var options = Options(Path.Combine(root, "discovery"));
        var worker = new WorkerOptions
        {
            SeedFilePath = SeedPath,
            ResearchPlanningOutputDirectory = Path.Combine(root, "requests"),
            DiscoveredRecordDirectory = Path.Combine(root, "discovered"),
            CuratedDiscoveredRecordDirectory = curatedDir,
        };
        var job = new DiscoveryJob(worker, options, ArtifactValidator, RecordValidator,
            DiscoveryRunner.CreateSources(options, () => new HttpClient(handler)), new FixedClock(Now));
        var result = await job.RunAsync(new IngestionContext(worker, NullLogger.Instance));
        return (result, root, handler);
    }

    [Fact]
    public async Task Discovery_Run_Finds_ER100_And_Writes_Valid_Requests_And_Drafts()
    {
        var (result, root, handler) = await RunJob(curatedDir: null);
        Assert.True(result.Success, result.ErrorMessage);

        var report = JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(root, "discovery"), "discovery-report-*.json").Single()))!;
        var candidates = ((JsonArray)report["candidates"]!).OfType<JsonObject>().ToDictionary(c => c["name"]!.GetValue<string>());
        Assert.Equal("New", candidates["ER-100"]["status"]!.GetValue<string>());
        Assert.Equal("InCorpus", candidates["Retatrutide"]["status"]!.GetValue<string>());
        Assert.DoesNotContain("test-ncbi-key-not-real", report.ToJsonString());
        Assert.Contains(handler.Urls, u => u.Contains("api_key=test-ncbi-key-not-real")); // the key is sent, never written

        var batch = JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(root, "requests"), "research-request-discovery-*.json").Single()))!;
        Assert.True(ArtifactValidator.Validate(ResearchArtifactKind.ResearchRequestBatch, batch).IsValid);
        var requested = ((JsonArray)batch["requests"]!).Select(r => r!["compoundName"]!.GetValue<string>()).ToList();
        Assert.Contains("ER-100", requested);
        Assert.Contains("UBT251", requested);
        Assert.Contains("Orforglipron", requested);
        Assert.DoesNotContain("XRP-2041", requested); // one preprint alone is below MinScore

        var drafts = (JsonArray)JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(root, "discovered"), "discovered-*.json").Single()))!;
        foreach (var d in drafts) Assert.True(RecordValidator.Validate(d!).IsValid, d!["identity"]!["canonicalName"]!.ToString());
        var er100 = drafts.OfType<JsonObject>().Single(d => d["identity"]!["canonicalName"]!.GetValue<string>() == "ER-100");
        Assert.Equal("Epigenetic Therapy", er100["identity"]!["classification"]!.GetValue<string>());
        Assert.Equal("Not graded", er100["evidence"]!["overallTier"]!.GetValue<string>());
        Assert.Contains("NCT07290244", er100["regulatory"]!["regulatoryStatus"]!.GetValue<string>());
        Assert.Equal("draft", er100["provenance"]!["reviewStatus"]!.GetValue<string>());
    }

    [Fact]
    public async Task Curated_ER100_Record_Means_Discovery_Does_Not_Draft_It_Again()
    {
        var (result, root, _) = await RunJob(curatedDir: CuratedDir);
        Assert.True(result.Success, result.ErrorMessage);
        var report = JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(root, "discovery"), "discovery-report-*.json").Single()))!;
        var er100 = ((JsonArray)report["candidates"]!).OfType<JsonObject>().Single(c => c["name"]!.GetValue<string>() == "ER-100");
        Assert.Equal("InCorpus", er100["status"]!.GetValue<string>());
        var drafts = (JsonArray)JsonNode.Parse(File.ReadAllText(Directory.GetFiles(Path.Combine(root, "discovered"), "discovered-*.json").Single()))!;
        Assert.DoesNotContain(drafts, d => d!["identity"]!["canonicalName"]!.GetValue<string>() == "ER-100");
    }

    // ── ER-100 curated record ───────────────────────────────────────────────

    [Fact]
    public void Curated_ER100_Record_Is_Schema_Valid_Cited_And_Graded()
    {
        var records = (JsonArray)JsonNode.Parse(File.ReadAllText(Path.Combine(CuratedDir, "er-100.json")))!;
        var r = (JsonObject)Assert.Single(records)!;
        var validation = RecordValidator.Validate(r);
        Assert.True(validation.IsValid, validation.Summary());
        Assert.Equal("Early Evidence", r["evidence"]!["overallTier"]!.GetValue<string>());
        var urls = ((JsonArray)r["provenance"]!["sourceRecords"]!).Select(s => s!["url"]!.GetValue<string>()).ToList();
        Assert.Contains("https://clinicaltrials.gov/study/NCT07290244", urls);
        Assert.Equal(3, urls.Count(u => u.StartsWith("https://www.lifebiosciences.com/", StringComparison.Ordinal)));
        foreach (var claim in ((JsonArray)r["evidence"]!["claimSpecificEvidence"]!).OfType<JsonObject>())
            Assert.Contains(claim["tier"]!.GetValue<string>(), RecordEvidenceTiers.All);
        var doses = ((JsonArray)r["doseReports"]!).OfType<JsonObject>().ToList();
        Assert.Contains(doses, d => d["amount"]!.GetValue<double>() == 2e11 && d["unit"]!.GetValue<string>() == "vg/eye");
        Assert.Contains(doses, d => d["amount"]!.GetValue<double>() == 6e11);
        Assert.Contains(doses, d => d["side"]!.GetValue<string>() == "reported" && d["unit"]!.GetValue<string>() == "n/a");
    }

    [Fact]
    public void Curated_ER100_Record_Passes_The_Ingestion_Pipeline_As_Epigenetic_Therapy()
    {
        var pipeline = new IngestionPipeline(new SubstanceRecordLoader(), RecordValidator, new SubstanceRecordNormalizer(),
            new TrustGate(), new SubstanceCanonicalizer(), NullLogger<IngestionPipeline>.Instance);
        var result = pipeline.Run(Path.Combine(CuratedDir, "er-100.json"));
        Assert.Empty(result.Rejected);
        var prepared = Assert.Single(result.Accepted);
        Assert.Equal(CompoundCategory.EpigeneticTherapy, prepared.Entry.Classification);
    }

    [Fact]
    public void Discovered_Records_Never_Override_The_Seed()
    {
        var dir = Directory.CreateTempSubdirectory("discovered-").FullName;
        File.WriteAllText(Path.Combine(dir, "a.json"), new JsonArray(
            new JsonObject { ["identity"] = new JsonObject { ["canonicalName"] = "retatrutide", ["classification"] = "Other" } },
            new JsonObject { ["identity"] = new JsonObject { ["canonicalName"] = "Brand New" } }).ToJsonString());
        var seed = Seed();
        var combined = DiscoveredRecordStore.Combine(seed, new[] { dir });
        Assert.Equal(seed.Count + 1, combined.Count);
        Assert.Equal("Brand New", combined[^1]!["identity"]!["canonicalName"]!.GetValue<string>());
    }

    // ── Wiring ──────────────────────────────────────────────────────────────

    [Fact]
    public void Discovery_Is_A_Database_Free_Run_Mode() => Assert.True(WorkerRunModePolicy.IsDatabaseFree(RunMode.Discovery));

    [Fact]
    public void Scope_Covers_Gene_And_Epigenetic_Therapies()
    {
        var scope = Scope();
        Assert.Contains(scope.Areas, a => a.AreaId == "gene-epigenetic-therapy" && a.ClinicalTrialsTerms.Contains("epigenetic reprogramming"));
        Assert.Contains("GENETIC", scope.ClinicalTrialsInterventionTypes);
        Assert.Contains("BIOLOGICAL", scope.ClinicalTrialsInterventionTypes);
    }

    [Fact]
    public void Watchlist_Seeds_ER100()
    {
        var watch = ResearchSchedulePlanner.ParseWatchlist(JsonNode.Parse(File.ReadAllText(Path.Combine(WorkerDir, "Seeds", "discovery-watchlist.json"))));
        Assert.Contains(watch, w => w.CompoundName == "ER-100");
    }

    [Fact]
    public void Discovery_Is_Scheduled_In_Bicep_With_Keys_Only_As_Secret_Refs()
    {
        var bicep = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot(), "infra", "azure", "knowledge-schedules.bicep"));
        Assert.Contains("'Worker__RunMode', value: 'Discovery'", bicep);
        Assert.Contains("param discoveryCron string", bicep);
        Assert.Contains("{ name: 'Discovery__NcbiApiKey', secretRef: 'ncbi-api-key' }", bicep);
        Assert.Contains("{ name: 'Discovery__OpenFdaApiKey', secretRef: 'openfda-api-key' }", bicep);
        Assert.Contains("'Worker__DiscoveredRecordDirectory'", bicep);
        Assert.Contains("{ name: 'Worker__CuratedDiscoveredRecordDirectory', value: 'Seeds/discovered-records' }", bicep);
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }
}
