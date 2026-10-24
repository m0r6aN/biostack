namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Council;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>Owner ruling 5 (2026-10-24): two-reviewer AI council gate.</summary>
public sealed class AiCouncilTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);
    private static readonly ResearchArtifactValidator Validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());
    private static string SeedPath => Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");

    private static CouncilReviewItem Item(string tier = "Established")
        => new("Retatrutide", tier, Now.AddDays(-1), new JsonObject());

    private static ReviewerVerdict V(string id, string decision, string tier, params string[] legal)
        => new(id, "fake", id + "-model", decision, tier, new[] { "check dose framing" }, legal, $"{id} rationale");

    [Fact]
    public void Consensus_Approve_At_Current_Tier_Passes_As_Approve_For_Promotion()
    {
        var o = CouncilAdjudicator.Adjudicate(Item(), new[] { V("a", "approve", "Established"), V("b", "approve", "Established") });
        Assert.Equal(CouncilOutcomeKind.Consensus, o.Kind);
        Assert.Equal("approve-for-promotion", o.ReviewDecision);
        Assert.True(o.ClearsSoftPromotionBlockers);
        Assert.Empty(o.OwnerOptions);
    }

    [Fact]
    public void Consensus_On_A_Different_Tier_Passes_As_Request_Changes_With_Regrade_Note()
    {
        var o = CouncilAdjudicator.Adjudicate(Item(), new[] { V("a", "approve", "Early Evidence"), V("b", "approve", "Early Evidence") });
        Assert.Equal(CouncilOutcomeKind.Consensus, o.Kind);
        Assert.Equal("request-changes", o.ReviewDecision);
        Assert.Contains(o.Notes, n => n.Contains("regrade overall tier from Established to Early Evidence"));
    }

    [Fact]
    public void Disagreement_Escalates_To_Owner_With_Distinct_Options()
    {
        var o = CouncilAdjudicator.Adjudicate(Item(), new[] { V("a", "approve", "Established"), V("b", "request-changes", "Early Evidence") });
        Assert.Equal(CouncilOutcomeKind.EscalateToOwner, o.Kind);
        Assert.Equal("escalate-to-owner", o.ReviewDecision);
        Assert.Equal(new[] { "A", "B", "C" }, o.OwnerOptions.Select(x => x.OptionId));
        Assert.Equal(o.OwnerOptions.Count, o.OwnerOptions.Select(x => (x.Decision, x.ProposedTier)).Distinct().Count());
    }

    [Fact]
    public void Tier_Only_Disagreement_Also_Escalates_And_Duplicate_Options_Collapse()
    {
        var o = CouncilAdjudicator.Adjudicate(Item("Early Evidence"),
            new[] { V("a", "request-changes", "Early Evidence"), V("b", "approve", "Established") });
        Assert.Equal(CouncilOutcomeKind.EscalateToOwner, o.Kind);
        // Option C (keep pending at current tier) equals reviewer A's request-changes/Early Evidence: collapsed.
        Assert.Equal(2, o.OwnerOptions.Count);
    }

    [Fact]
    public void Consensus_Reject_Escalates_Because_Only_The_Owner_Can_Withhold()
    {
        var o = CouncilAdjudicator.Adjudicate(Item(), new[] { V("a", "reject", "Unsupported"), V("b", "reject", "Unsupported") });
        Assert.Equal(CouncilOutcomeKind.EscalateToOwner, o.Kind);
        Assert.Contains(o.OwnerOptions, x => x.Decision == "reject");
        Assert.Contains(o.OwnerOptions, x => x.Decision == "request-changes");
    }

    [Fact]
    public void Legal_Items_Are_Logged_But_Never_Change_The_Outcome()
    {
        var clean = CouncilAdjudicator.Adjudicate(Item(), new[] { V("a", "approve", "Established"), V("b", "approve", "Established") });
        var legal = CouncilAdjudicator.Adjudicate(Item(), new[]
        {
            V("a", "approve", "Established", "Reddit commercial terms for quoted doses"),
            V("b", "approve", "Established", "MHMDA consent"),
        });
        Assert.Equal(clean.Kind, legal.Kind);
        Assert.Equal(clean.ReviewDecision, legal.ReviewDecision);
        Assert.Equal(2, legal.LegalRisks.Count);
    }

    [Theory]
    [InlineData("{\"decision\":\"approve\",\"proposedTier\":\"Early Evidence\",\"concerns\":[],\"legalRisks\":[],\"rationale\":\"ok\"}", true)]
    [InlineData("Sure! ```json\n{\"decision\":\"Request-Changes\",\"proposedTier\":\"hype exceeds evidence\",\"rationale\":\"x\"}\n```", true)]
    [InlineData("{\"decision\":\"maybe\",\"proposedTier\":\"Established\"}", false)]
    [InlineData("{\"decision\":\"approve\",\"proposedTier\":\"Strong\"}", false)]
    [InlineData("no json here", false)]
    public void Verdict_Parsing_Is_Strict(string reply, bool ok)
    {
        if (ok) Assert.NotNull(ReviewerPrompt.Parse("a", "p", "m", reply));
        else Assert.Throws<ReviewerResponseException>(() => ReviewerPrompt.Parse("a", "p", "m", reply));
    }

    [Fact]
    public void Missing_Config_Lists_Env_Var_Names_Never_Values()
    {
        var options = new CouncilOptions
        {
            Reviewers = { new CouncilReviewerOptions { Provider = "openai-compatible", Model = "m1", ApiKey = "sk-should-not-leak" }, new CouncilReviewerOptions { Provider = "anthropic" } },
        };
        var ex = Assert.Throws<InvalidOperationException>(() => ReviewerFactory.Create(options));
        Assert.Contains("Council__Reviewers__1__Model", ex.Message);
        Assert.Contains("Council__Reviewers__1__ApiKey", ex.Message);
        Assert.DoesNotContain("sk-should-not-leak", ex.Message);
        Assert.Contains("exactly 2", string.Join(";", new CouncilOptions().MissingSettings()));
    }

    [Fact]
    public async Task OpenAi_Compatible_And_Anthropic_Send_The_Right_Requests()
    {
        var handler = new RecordingHandler();
        var options = new CouncilOptions
        {
            Reviewers =
            {
                new CouncilReviewerOptions { Id = "a", Provider = "openai-compatible", Model = "gpt-x", Endpoint = "https://llm.example/v1/", ApiKey = "key-a" },
                new CouncilReviewerOptions { Id = "b", Provider = "anthropic", Model = "claude-x", ApiKey = "key-b" },
            },
        };
        var reviewers = ReviewerFactory.Create(options, handler);
        var va = await reviewers[0].ReviewAsync(Item(), CancellationToken.None);
        var vb = await reviewers[1].ReviewAsync(Item(), CancellationToken.None);

        Assert.Equal("https://llm.example/v1/chat/completions", handler.Requests[0].Uri);
        Assert.Equal("Bearer key-a", handler.Requests[0].Headers["Authorization"]);
        Assert.Equal("gpt-x", JsonNode.Parse(handler.Requests[0].Body)!["model"]!.GetValue<string>());
        Assert.Equal("https://api.anthropic.com/v1/messages", handler.Requests[1].Uri);
        Assert.Equal("key-b", handler.Requests[1].Headers["x-api-key"]);
        Assert.Equal("2023-06-01", handler.Requests[1].Headers["anthropic-version"]);
        Assert.Equal("approve", va.Decision);
        Assert.Equal("Early Evidence", vb.ProposedTier);
    }

    [Fact]
    public async Task Job_Writes_Schema_Valid_Decisions_Escalations_And_Known_Risks_And_Gate_Reads_Them()
    {
        var root = Path.Combine(Path.GetTempPath(), "biostack-council-" + Guid.NewGuid().ToString("N"));
        try
        {
            var council = new CouncilOptions
            {
                MaxItemsPerRun = 4,
                DecisionDirectory = Path.Combine(root, "review-decisions"),
                EscalationDirectory = Path.Combine(root, "owner-escalations"),
                KnownRiskDirectory = Path.Combine(root, "known-risks"),
            };
            var worker = new WorkerOptions { RunMode = RunMode.CouncilReview, SeedFilePath = SeedPath };
            var calls = 0;
            IReadOnlyList<IReviewer> Reviewers() => new IReviewer[]
            {
                new FakeReviewer("a", item => V("a", "approve", item.CurrentOverallTier, "logged legal item")),
                new FakeReviewer("b", item => Interlocked.Increment(ref calls) % 2 == 0
                    ? V("b", "request-changes", "Plausible Theory")
                    : V("b", "approve", item.CurrentOverallTier)),
            };

            var dry = await new CouncilReviewJob(worker, council, Validator, Reviewers, new FixedClock(Now))
                .RunAsync(new IngestionContext(new WorkerOptions { DryRun = true }, NullLogger.Instance));
            Assert.True(dry.Success);
            Assert.False(Directory.Exists(root));

            var result = await new CouncilReviewJob(worker, council, Validator, Reviewers, new FixedClock(Now))
                .RunAsync(new IngestionContext(worker, NullLogger.Instance));
            Assert.True(result.Success, result.ErrorMessage);
            Assert.Equal(4, result.ScannedCount);
            Assert.Equal(2, result.CreatedCount);
            Assert.Equal(2, result.FlaggedForReviewCount);

            var batchFile = Assert.Single(Directory.GetFiles(council.DecisionDirectory));
            var batch = JsonNode.Parse(File.ReadAllText(batchFile))!;
            Assert.True(Validator.Validate(ResearchArtifactKind.ReviewDecisionBatch, batch).IsValid);
            Assert.Equal(2, Directory.GetFiles(council.EscalationDirectory).Length); // .json + .md
            Assert.Single(Directory.GetFiles(council.KnownRiskDirectory));

            var gate = new ReviewDecisionPromotionGate(ReviewDecisionIndex.FromBatches(new[] { batch }));
            var names = batch["decisions"]!.AsArray().Select(d => (Name: d!["compoundName"]!.GetValue<string>(), Kind: d["decision"]!.GetValue<string>())).ToList();
            foreach (var (name, kind) in names)
            {
                var g = gate.Evaluate(name);
                if (kind == "approve-for-promotion") { Assert.True(g.Allowed); Assert.StartsWith("ai-council", g.ApprovedBy); }
                else Assert.Equal(PromotionSkipReason.AwaitingOwner, g.SkipReason);
            }

            // Second run: escalated items wait for the owner; reviewed items are not re-sent.
            var second = await new CouncilReviewJob(worker, council, Validator, Reviewers, new FixedClock(Now.AddMinutes(5)))
                .RunAsync(new IngestionContext(worker, NullLogger.Instance));
            var secondNames = JsonNode.Parse(File.ReadAllText(Directory.GetFiles(council.DecisionDirectory).Max()!))!["decisions"]!
                .AsArray().Select(d => d!["compoundName"]!.GetValue<string>());
            Assert.Empty(secondNames.Intersect(names.Select(n => n.Name)));
            Assert.True(second.Success);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task Reviewer_Failure_Leaves_Record_Pending_And_Fails_The_Run_Visibly()
    {
        var root = Path.Combine(Path.GetTempPath(), "biostack-council-fail-" + Guid.NewGuid().ToString("N"));
        try
        {
            var council = new CouncilOptions { MaxItemsPerRun = 1, DecisionDirectory = Path.Combine(root, "d"), EscalationDirectory = Path.Combine(root, "e"), KnownRiskDirectory = Path.Combine(root, "k") };
            var worker = new WorkerOptions { RunMode = RunMode.CouncilReview, SeedFilePath = SeedPath };
            var result = await new CouncilReviewJob(worker, council, Validator, () => new IReviewer[]
            {
                new FakeReviewer("a", i => V("a", "approve", i.CurrentOverallTier)),
                new FakeReviewer("b", _ => throw new HttpRequestException("timeout")),
            }, new FixedClock(Now)).RunAsync(new IngestionContext(worker, NullLogger.Instance));
            Assert.False(result.Success);
            Assert.False(Directory.Exists(council.DecisionDirectory));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void Unconfigured_Job_Fails_Before_Any_Work()
    {
        var result = new CouncilReviewJob(new WorkerOptions(), new CouncilOptions(), Validator)
            .RunAsync(new IngestionContext(new WorkerOptions(), NullLogger.Instance)).GetAwaiter().GetResult();
        Assert.False(result.Success);
        Assert.Contains("not configured", result.ErrorMessage);
    }

    [Fact]
    public void No_Council_Api_Key_Is_Committed_In_Config_Or_Infra()
    {
        var files = Directory.GetFiles(Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker"), "appsettings*.json")
            .Concat(Directory.GetFiles(Path.Combine(TestPaths.RepositoryRoot(), "infra", "azure"), "*.bicep", SearchOption.AllDirectories));
        foreach (var file in files)
        {
            var text = File.ReadAllText(file);
            Assert.DoesNotMatch(@"(sk-[A-Za-z0-9]{16,}|sk-ant-[A-Za-z0-9-]{16,}|""ApiKey""\s*:\s*""[^""]+"")", text);
        }
        var bicep = File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot(), "infra", "azure", "knowledge-schedules.bicep"));
        Assert.Contains("Council__Reviewers__0__ApiKey", bicep);
        Assert.Contains("Council__Reviewers__1__ApiKey", bicep);
        Assert.Contains("'Worker__RunMode', value: 'CouncilReview'", bicep);
    }

    [Fact]
    public void Owner_Decision_After_Escalation_Resolves_The_Gate()
    {
        static JsonObject Batch(string decision, string reviewer, string at, bool clears) => new()
        {
            ["decisions"] = new JsonArray(new JsonObject
            {
                ["decisionId"] = reviewer + at, ["compoundName"] = "Retatrutide", ["decision"] = decision,
                ["reviewerId"] = reviewer, ["reviewedAt"] = at, ["clearsSoftPromotionBlockers"] = clears,
            }),
        };
        var escalated = Batch("escalate-to-owner", "ai-council:a+b", "2026-10-25T08:00:00Z", false);
        var owner = Batch("approve-for-promotion", "owner:clinton-morgan", "2026-10-25T12:00:00Z", true);
        Assert.Equal(PromotionSkipReason.AwaitingOwner,
            new ReviewDecisionPromotionGate(ReviewDecisionIndex.FromBatches(new[] { escalated })).Evaluate("Retatrutide").SkipReason);
        var resolved = new ReviewDecisionPromotionGate(ReviewDecisionIndex.FromBatches(new[] { escalated, owner })).Evaluate("Retatrutide");
        Assert.True(resolved.Allowed);
        Assert.Equal("owner:clinton-morgan", resolved.ApprovedBy);
    }

    [Fact]
    public void CouncilReview_Is_Database_Free()
        => Assert.True(WorkerRunModePolicy.IsDatabaseFree(RunMode.CouncilReview));

    private sealed class FakeReviewer(string id, Func<CouncilReviewItem, ReviewerVerdict> verdict) : IReviewer
    {
        public string Id => id;
        public string Provider => "fake";
        public string Model => id + "-model";
        public Task<ReviewerVerdict> ReviewAsync(CouncilReviewItem item, CancellationToken cancellationToken) => Task.FromResult(verdict(item));
    }

    private sealed class FixedClock(DateTimeOffset now) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => now;
    }

    private sealed class RecordingHandler : HttpMessageHandler
    {
        public List<(string Uri, Dictionary<string, string> Headers, string Body)> Requests { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var headers = request.Headers.ToDictionary(h => h.Key, h => string.Join(",", h.Value));
            Requests.Add((request.RequestUri!.ToString(), headers, await request.Content!.ReadAsStringAsync(cancellationToken)));
            var verdict = "{\"decision\":\"approve\",\"proposedTier\":\"Early Evidence\",\"concerns\":[],\"legalRisks\":[],\"rationale\":\"ok\"}";
            var body = request.RequestUri!.AbsolutePath.EndsWith("/messages")
                ? new JsonObject { ["content"] = new JsonArray(new JsonObject { ["type"] = "text", ["text"] = verdict }) }
                : new JsonObject { ["choices"] = new JsonArray(new JsonObject { ["message"] = new JsonObject { ["content"] = verdict } }) };
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(body.ToJsonString(), Encoding.UTF8, "application/json") };
        }
    }
}
