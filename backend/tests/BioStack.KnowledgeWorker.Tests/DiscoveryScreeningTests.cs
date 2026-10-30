namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Discovery;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Owner direction t101u (2026-10-30): oncology drugs are no longer skipped (a person with cancer may track
/// anything), so junk is screened out by name instead. Procedures, ATC codes and excipient codes are rejected,
/// uncertain names are flagged for the council, and misspellings are corrected against RxNorm rather than
/// becoming a second record.
/// </summary>
public sealed class DiscoveryScreeningTests
{
    private static string FixtureDir => Path.Combine(TestPaths.BackendRoot(), "tests", "BioStack.KnowledgeWorker.Tests", "Fixtures", "discovery");
    private static DiscoveryScope Scope()
        => DiscoveryScope.Load(Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "discovery-scope.json"));

    // ── Name rules ──────────────────────────────────────────────────────────

    [Theory]
    [InlineData("blood sampling", "non-substance")]
    [InlineData("Blood Sampling", "non-substance")]
    [InlineData("Lumbar puncture and biopsy", "non-substance")]
    [InlineData("L01A", "atc-code")]
    [InlineData("L01AA", "atc-code")]
    [InlineData("L01AA01", "atc-code")]
    [InlineData("C09DA", "atc-code")]
    [InlineData("F-127", "excipient-code")]
    [InlineData("F127", "excipient-code")]
    [InlineData("Pluronic F-127", "excipient-code")]
    [InlineData("12345", "number-only")]
    [InlineData("ab", "too-short")]
    public void Certain_Non_Substances_Are_Rejected(string name, string code)
    {
        var result = CandidateScreening.Screen(name);
        Assert.Equal(ScreenVerdict.Reject, result.Verdict);
        Assert.Equal(code, result.Code);
        Assert.False(string.IsNullOrWhiteSpace(result.Reason));
    }

    [Theory]
    [InlineData("ER-100")]
    [InlineData("UBT251")]
    [InlineData("LY3437943")]
    [InlineData("SLU-PP-332")]
    [InlineData("CT-868")]
    [InlineData("GE-287")]
    [InlineData("Retatrutide")]
    [InlineData("Orforglipron")]
    [InlineData("Nicotinamide riboside")]
    [InlineData("Alpha-lipoic acid")]
    [InlineData("Fisetin")]
    // Oncology drugs are accepted: no oncology skip (owner direction t101u).
    [InlineData("Rituximab")]
    [InlineData("Ipilimumab")]
    [InlineData("Bevacizumab")]
    [InlineData("Sintilimab")]
    public void Real_Substances_And_Oncology_Drugs_Are_Accepted(string name)
        => Assert.Equal(ScreenVerdict.Accept, CandidateScreening.Screen(name).Verdict);

    [Theory]
    [InlineData("RADA16", "material-or-class-term")]
    [InlineData("Postbiotic", "material-or-class-term")]
    [InlineData("Poloxamer 188", "material-or-class-term")]
    [InlineData("DMSO", "material-or-class-term")]
    [InlineData("Mannitol", "material-or-class-term")]
    [InlineData("virus specific cytotoxic t lymphocytes targeting several viral antigens in transplant", "long-phrase")]
    public void Uncertain_Names_Are_Flagged_Not_Rejected(string name, string code)
    {
        var result = CandidateScreening.Screen(name);
        Assert.Equal(ScreenVerdict.Flag, result.Verdict);
        Assert.Equal(code, result.Code);
    }

    [Fact]
    public void Trademark_Symbols_And_Edge_Punctuation_Are_Cleaned()
    {
        Assert.Equal("EYELEA", CandidateScreening.CleanName("EYELEA ®"));
        Assert.Equal("Eylea", CandidateScreening.CleanName("  Eylea™, "));
        Assert.Equal("ER-100", CandidateScreening.CleanName("ER-100"));
    }

    [Fact]
    public void Oncology_Keywords_Are_No_Longer_Excluded_But_Infectious_Disease_Still_Is()
    {
        var scope = Scope();
        Assert.False(scope.IsExcluded("Rituximab in B-cell lymphoma", "lymphoma", "cancer", "metastatic melanoma", "carcinoma"));
        Assert.True(scope.IsExcluded("A trial in HIV", "hiv"));
        Assert.True(scope.IsExcluded("covid-19 vaccine"));
    }

    [Fact]
    public void Live_Draft_Names_Of_2026_10_30_Reject_Only_The_Three_Clear_Junk_Names()
    {
        var names = JsonNode.Parse(File.ReadAllText(Path.Combine(FixtureDir, "live-draft-names-2026-10-30.json")))!.AsArray()
            .Select(n => n!.GetValue<string>()).ToList();
        Assert.Equal(87, names.Count);

        var rejected = names.Where(n => CandidateScreening.Screen(n).Verdict == ScreenVerdict.Reject).Select(n => n.ToLowerInvariant()).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(new[] { "blood sampling", "f-127", "l01a" }, rejected);

        var flagged = names.Where(n => CandidateScreening.Screen(n).Verdict == ScreenVerdict.Flag).Select(n => n.ToLowerInvariant()).ToList();
        Assert.Contains("postbiotic", flagged);
        Assert.Contains("rada16", flagged);
        // Oncology drugs and the typo are accepted by the offline rules; the typo is the spelling check's job.
        foreach (var kept in new[] { "rituximab", "ipilimumab", "bevacizumab", "sintilimab", "tociliuzumab" })
            Assert.Equal(ScreenVerdict.Accept, CandidateScreening.Screen(kept).Verdict);
    }

    // ── Spelling check (RxNorm) ─────────────────────────────────────────────

    private sealed class FakeVerifier(Func<string, NameCheck> answer) : IDrugNameVerifier
    {
        public List<string> Calls { get; } = new();
        public Task<NameCheck> CheckAsync(string name, CancellationToken cancellationToken)
        {
            Calls.Add(name);
            return Task.FromResult(answer(name));
        }
    }

    private static DiscoveredCandidate Candidate(string name, int score = 3, CorpusMatchStatus status = CorpusMatchStatus.New, string? matched = null)
        => new(name, Array.Empty<string>(), "Biologic", new[] { "muscle-preservation" },
            new[] { new DiscoveryEvidence("clinicaltrials-gov", "NCT00000001", "t", "https://clinicaltrials.gov/study/NCT00000001", null, null) },
            new[] { "PHASE2" }, Array.Empty<string>(), Array.Empty<string>(), score, status, matched);

    private static CorpusNameIndex Corpus(params string[] names)
        => CorpusNameIndex.Build(new JsonArray(names.Select(n => (JsonNode?)new JsonObject { ["identity"] = new JsonObject { ["canonicalName"] = n } }).ToArray()));

    [Fact]
    public async Task A_Misspelling_Is_Corrected_To_The_Reference_Spelling_And_Keeps_The_Typed_Form_As_An_Alias()
    {
        var verifier = new FakeVerifier(n => n == "Tociliuzumab" ? new NameCheck(NameCheckOutcome.Misspelled, "tocilizumab") : new NameCheck(NameCheckOutcome.Known));
        var result = await CandidateScreener.ScreenAsync(new[] { Candidate("Tociliuzumab") }, Corpus(), verifier, 2, 10, default);

        var kept = Assert.Single(result.Kept);
        Assert.Equal("tocilizumab", kept.Name);
        Assert.Contains("Tociliuzumab", kept.Aliases);
        Assert.Equal(CorpusMatchStatus.New, kept.Status);
        Assert.Contains("name-corrected:rxnorm", kept.NameFlags!);
        Assert.Contains(result.Notes, n => n.Action == "renamed" && n.Code == "misspelling" && n.Replacement == "tocilizumab");
    }

    [Fact]
    public async Task A_Misspelling_Of_A_Name_Already_In_The_Corpus_Is_Merged_Not_Drafted()
    {
        var verifier = new FakeVerifier(_ => new NameCheck(NameCheckOutcome.Misspelled, "Tocilizumab"));
        var result = await CandidateScreener.ScreenAsync(new[] { Candidate("Tociliuzumab") }, Corpus("Tocilizumab"), verifier, 2, 10, default);

        var kept = Assert.Single(result.Kept);
        Assert.Equal(CorpusMatchStatus.InCorpus, kept.Status);
        Assert.Equal("Tocilizumab", kept.MatchedRecord);
        Assert.Contains(result.Notes, n => n.Action == "merged");
        // No draft is built for an in-corpus candidate.
        Assert.Empty(DiscoveryOutputBuilder.BuildDraftRecords(result.Kept, DateTimeOffset.UtcNow, 2, 50));
    }

    [Fact]
    public async Task Junk_Is_Dropped_Before_Any_Network_Call_And_Code_Names_Are_Not_Spell_Checked()
    {
        var verifier = new FakeVerifier(_ => NameCheck.NotChecked);
        var result = await CandidateScreener.ScreenAsync(
            new[] { Candidate("blood sampling"), Candidate("L01A"), Candidate("F-127"), Candidate("ER-100"), Candidate("Rituximab") },
            Corpus(), verifier, 2, 10, default);

        Assert.Equal(new[] { "ER-100", "Rituximab" }, result.Kept.Select(c => c.Name));
        Assert.Equal(3, result.Notes.Count(n => n.Action == "rejected"));
        Assert.Equal(new[] { "Rituximab" }, verifier.Calls); // ER-100 is a code name; the rejected names never reach the network
    }

    [Fact]
    public async Task An_Unavailable_Spelling_Service_Flags_The_Draft_And_Never_Blocks_It()
    {
        var verifier = new FakeVerifier(_ => new NameCheck(NameCheckOutcome.Unavailable));
        var result = await CandidateScreener.ScreenAsync(new[] { Candidate("Rituximab") }, Corpus(), verifier, 2, 10, default);
        var kept = Assert.Single(result.Kept);
        Assert.Contains(CandidateScreener.UnavailableFlag, kept.NameFlags!);
    }

    [Fact]
    public async Task Spelling_Checks_Are_Bounded_And_Only_Run_For_Candidates_That_Would_Be_Drafted()
    {
        var verifier = new FakeVerifier(_ => new NameCheck(NameCheckOutcome.Known));
        var result = await CandidateScreener.ScreenAsync(
            new[] { Candidate("Rituximab"), Candidate("Ipilimumab"), Candidate("Bevacizumab"), Candidate("Lowscoreab", score: 1), Candidate("Inabcorpus", status: CorpusMatchStatus.InCorpus, matched: "X") },
            Corpus(), verifier, minScore: 2, maxNameChecks: 2, default);
        Assert.Equal(5, result.Kept.Count);
        Assert.Equal(new[] { "Rituximab", "Ipilimumab" }, verifier.Calls);
    }

    [Fact]
    public async Task A_Flagged_Name_Still_Becomes_A_Hidden_Draft_Carrying_The_Flag_And_Passes_Schema_Validation()
    {
        var result = await CandidateScreener.ScreenAsync(new[] { Candidate("RADA16") }, Corpus(), null, 2, 10, default);
        var kept = Assert.Single(result.Kept);
        var draft = DiscoveryOutputBuilder.BuildDraft(kept, new DateTimeOffset(2026, 10, 30, 12, 0, 0, TimeSpan.Zero));

        var flags = ((JsonArray)draft["ops"]!["qualityFlags"]!).Select(f => f!.GetValue<string>()).ToList();
        Assert.Contains("auto-discovered", flags);
        Assert.Contains("name-check:material-or-class-term", flags);
        Assert.Contains(((JsonArray)draft["ops"]!["reviewReasons"]!).Select(r => r!.GetValue<string>()), r => r.StartsWith("Name flagged", StringComparison.Ordinal));

        var validator = SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json"));
        Assert.True(validator.Validate(draft).IsValid);
        var record = System.Text.Json.JsonSerializer.Deserialize<BioStack.KnowledgeWorker.Models.SubstanceRecord>(draft.ToJsonString())!;
        Assert.False(KnowledgeVisibility.IsPublic(record, councilApproved: false));
    }

    // ── RxNorm client ───────────────────────────────────────────────────────

    private sealed class RxNormHandler(Func<string, (HttpStatusCode, string)> reply) : HttpMessageHandler
    {
        public List<string> Urls { get; } = new();
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var url = request.RequestUri!.ToString();
            Urls.Add(url);
            var (status, body) = reply(url);
            return Task.FromResult(new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") });
        }
    }

    private static RxNormNameVerifier Verifier(RxNormHandler handler)
        => new(new DiscoveryHttp(new HttpClient(handler), TimeSpan.Zero, "t/1"));

    [Fact]
    public async Task RxNorm_Known_Misspelled_Unknown_And_Cached()
    {
        var handler = new RxNormHandler(rawUrl =>
        {
            var url = rawUrl.ToLowerInvariant();
            return url switch
            {
            _ when url.Contains("rxcui.json?name=tocilizumab") => (HttpStatusCode.OK, """{"idGroup":{"rxnormId":["612865"]}}"""),
            _ when url.Contains("rxcui.json") => (HttpStatusCode.OK, """{"idGroup":{}}"""),
            _ when url.Contains("name=tociliuzumab") => (HttpStatusCode.OK, """{"suggestionGroup":{"name":null,"suggestionList":{"suggestion":["tocilizumab"]}}}"""),
            _ when url.Contains("name=zzzzzzzzzz") => (HttpStatusCode.OK, """{"suggestionGroup":{"name":null,"suggestionList":{"suggestion":["acetaminophen"]}}}"""),
            _ => (HttpStatusCode.OK, """{"suggestionGroup":{"name":null,"suggestionList":{}}}"""),
            };
        });
        var verifier = Verifier(handler);

        Assert.Equal(NameCheckOutcome.Known, (await verifier.CheckAsync("Tocilizumab", default)).Outcome);
        var typo = await verifier.CheckAsync("Tociliuzumab", default);
        Assert.Equal(NameCheckOutcome.Misspelled, typo.Outcome);
        Assert.Equal("tocilizumab", typo.Suggestion);
        // A far-away suggestion is not a spelling correction.
        Assert.Equal(NameCheckOutcome.Unknown, (await verifier.CheckAsync("zzzzzzzzzz", default)).Outcome);
        Assert.Equal(NameCheckOutcome.Unknown, (await verifier.CheckAsync("Orforglipron", default)).Outcome);

        var before = handler.Urls.Count;
        await verifier.CheckAsync("tociliuzumab", default); // cached
        Assert.Equal(before, handler.Urls.Count);
        Assert.All(handler.Urls, u => Assert.StartsWith("https://rxnav.nlm.nih.gov/REST/", u));
    }

    [Fact]
    public async Task RxNorm_Does_Not_Check_Code_Names_Or_Phrases_And_Reports_Unavailable_On_Errors()
    {
        var handler = new RxNormHandler(_ => (HttpStatusCode.InternalServerError, "oops"));
        var verifier = Verifier(handler);

        Assert.Equal(NameCheckOutcome.NotChecked, (await verifier.CheckAsync("ER-100", default)).Outcome);
        Assert.Equal(NameCheckOutcome.NotChecked, (await verifier.CheckAsync("Nicotinamide riboside", default)).Outcome);
        Assert.Empty(handler.Urls);

        Assert.Equal(NameCheckOutcome.Unavailable, (await verifier.CheckAsync("Rituximab", default)).Outcome);
    }

    // ── Existing stored drafts (Combine) ────────────────────────────────────

    private static JsonObject StoredDraft(string name, bool autoDiscovered)
        => new()
        {
            ["identity"] = new JsonObject { ["canonicalName"] = name },
            ["ops"] = new JsonObject
            {
                ["ingestionSource"] = autoDiscovered ? "discovery:clinicaltrials-gov" : "curated:manual",
                ["qualityFlags"] = autoDiscovered ? new JsonArray("auto-discovered") : new JsonArray(),
            },
        };

    [Fact]
    public void Combine_Drops_Previously_Stored_Junk_Drafts_But_Keeps_Oncology_Drugs_And_Curated_Records()
    {
        var dir = Directory.CreateTempSubdirectory("screened-").FullName;
        File.WriteAllText(Path.Combine(dir, "a.json"), new JsonArray(
            StoredDraft("blood sampling", true),
            StoredDraft("L01A", true),
            StoredDraft("F-127", true),
            StoredDraft("Rituximab", true),
            StoredDraft("F127", false)).ToJsonString());   // curated records are never screened out

        var combined = DiscoveredRecordStore.Combine(new JsonArray(), new[] { dir });
        Assert.Equal(new[] { "Rituximab", "F127" }, combined.Select(r => r!["identity"]!["canonicalName"]!.GetValue<string>()));
    }
}
