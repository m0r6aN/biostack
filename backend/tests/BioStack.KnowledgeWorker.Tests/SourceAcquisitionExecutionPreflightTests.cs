namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class SourceAcquisitionExecutionPreflightTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";

    [Fact]
    public void Evaluate_CurrentCampaignShape_IsActivationReady()
    {
        var plan = BuildPlan(
            Enumerable.Range(1, 70)
                .Select(index => $"request-{index:D3}"));

        var result = Evaluate(
            plan,
            expectation:
                SourceAcquisitionCampaignExpectation.CurrentRecommendedSevenActivation);

        Assert.True(result.CanActivate, FormatIssues(result));
        Assert.Equal(70, result.UniqueRequestCount);
        Assert.Equal(490, result.IntentCount);
        Assert.Equal(490, result.ReadyCount);
        Assert.Equal(0, result.BlockedCount);
        Assert.Equal(7, result.SourceCount);
        Assert.Equal(420, result.ReadyAutomatedCount);
        Assert.Equal(70, result.ManualReviewPendingCount);
        Assert.Equal(420, result.DispatchableCount);
        Assert.Empty(result.Issues);
        Assert.Equal(
            Enumerable.Range(1, 490),
            result.Entries.Select(entry => entry.StableOrdinal));
        Assert.All(
            result.Entries.Where(entry => entry.SourceId == "nih-nccih"),
            entry =>
            {
                Assert.Equal(
                    SourceAcquisitionPreflightClassification.ManualReviewPending,
                    entry.Classification);
                Assert.False(entry.IsDispatchable);
            });
        Assert.All(
            result.Entries.Where(entry => entry.SourceId != "nih-nccih"),
            entry =>
            {
                Assert.Equal(
                    SourceAcquisitionPreflightClassification.ReadyAutomated,
                    entry.Classification);
                Assert.True(entry.IsDispatchable);
            });
        Assert.Equal(
            result.DispatchableCount,
            result.Entries.Count(entry => entry.IsDispatchable));
    }

    [Fact]
    public void Evaluate_ReorderedInputs_ProduceSameStableEntries()
    {
        var plan = BuildPlan(["request-b", "request-a"]);
        var reversed = plan with { Intents = plan.Intents.Reverse().ToList() };

        var first = Evaluate(plan);
        var second = Evaluate(reversed, Descriptors().Reverse());

        Assert.True(first.CanActivate, FormatIssues(first));
        Assert.True(second.CanActivate, FormatIssues(second));
        Assert.Equal(first.Entries, second.Entries);
    }

    [Fact]
    public void Evaluate_PreservesIntentExecutionContract()
    {
        var plan = BuildPlan(["request-a"]);

        var result = Evaluate(plan);
        var entry = result.Entries.Single(item => item.SourceId == "pubchem");
        var intent = plan.Intents.Single(item => item.SourceId == "pubchem");

        Assert.True(result.CanActivate, FormatIssues(result));
        Assert.Equal(intent.SourceId, entry.SourceId);
        Assert.Equal(intent.AdapterId, entry.AdapterId);
        Assert.Equal(intent.CandidateMethod, entry.CandidateMethod);
        Assert.Equal(intent.RegistryBindingSha256, entry.RegistryBindingSha256);
    }

    [Fact]
    public void Evaluate_ContradictoryPlanCounts_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]) with
        {
            ReadyCount = 6,
            BlockedCount = 0,
        };

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, "plan-counts-contradictory");
    }

    [Fact]
    public void Evaluate_UnknownSource_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("unknown-source", "request-a");
        plan = ReplaceIntent(plan, "fda", replacement);

        var result = Evaluate(plan);
        var entry = result.Entries.Single(item => item.SourceId == "unknown-source");

        Assert.False(result.CanActivate);
        AssertIssue(result, "intent-source-unknown");
        AssertIssue(result, "intent-source-missing");
        Assert.Equal(
            SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
            entry.Classification);
    }

    [Fact]
    public void Evaluate_MissingDescriptor_FailsClosed()
    {
        var descriptors = Descriptors()
            .Where(descriptor => descriptor.SourceId != "pubchem");

        var result = Evaluate(BuildPlan(["request-a"]), descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, "adapter-descriptor-missing");
        AssertIssue(result, "adapter-descriptor-source-set-mismatch");
        AssertIssue(result, "intent-adapter-unavailable");
    }

    [Fact]
    public void Evaluate_DuplicateDescriptor_FailsClosed()
    {
        var descriptors = Descriptors()
            .Concat([Descriptor("pubchem")]);

        var result = Evaluate(BuildPlan(["request-a"]), descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, "adapter-descriptor-duplicate");
    }

    [Theory]
    [InlineData("adapter", "intent-adapter-id-mismatch")]
    [InlineData("method", "intent-candidate-method-mismatch")]
    public void Evaluate_DescriptorMismatch_FailsClosed(
        string mismatch,
        string expectedIssue)
    {
        var descriptors = Descriptors()
            .Select(descriptor => descriptor.SourceId != "pubchem"
                ? descriptor
                : mismatch == "adapter"
                    ? descriptor with { AdapterId = "wrong-planning-adapter" }
                    : descriptor with { CandidateMethod = "manual-review" });

        var result = Evaluate(BuildPlan(["request-a"]), descriptors);
        var entry = result.Entries.Single(item => item.SourceId == "pubchem");

        Assert.False(result.CanActivate);
        AssertIssue(result, expectedIssue);
        Assert.Equal(
            SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
            entry.Classification);
    }

    [Fact]
    public void Evaluate_MatchingWrongAdapterSelfAttestation_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            AdapterId = "matching-but-unapproved-planner",
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);
        var descriptors = Descriptors()
            .Select(descriptor => descriptor.SourceId == "pubchem"
                ? descriptor with { AdapterId = replacement.AdapterId }
                : descriptor);

        var result = Evaluate(plan, descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, "adapter-descriptor-approved-adapter-id-mismatch");
        AssertIssue(result, "intent-approved-adapter-id-mismatch");
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "intent-adapter-id-mismatch");
    }

    [Theory]
    [InlineData("nih-nccih", "api")]
    [InlineData("fda", "manual-review")]
    public void Evaluate_MatchingWrongMethodSelfAttestation_FailsClosed(
        string sourceId,
        string wrongMethod)
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent(sourceId, "request-a") with
        {
            CandidateMethod = wrongMethod,
        };
        plan = ReplaceIntent(plan, sourceId, replacement);
        var descriptors = Descriptors()
            .Select(descriptor => descriptor.SourceId == sourceId
                ? descriptor with { CandidateMethod = wrongMethod }
                : descriptor);

        var result = Evaluate(plan, descriptors);
        var entry = result.Entries.Single(item => item.SourceId == sourceId);

        Assert.False(result.CanActivate);
        AssertIssue(result, "adapter-descriptor-approved-method-mismatch");
        AssertIssue(result, "intent-approved-method-mismatch");
        Assert.DoesNotContain(
            result.Issues,
            issue => issue.Code == "intent-candidate-method-mismatch");
        Assert.Equal(
            SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
            entry.Classification);
        Assert.False(entry.IsDispatchable);
    }

    [Theory]
    [InlineData("source-id", true, "adapter-descriptor-source-id-required")]
    [InlineData("source-id", false, "adapter-descriptor-source-id-required")]
    [InlineData("adapter-id", true, "adapter-descriptor-adapter-id-required")]
    [InlineData("adapter-id", false, "adapter-descriptor-adapter-id-required")]
    [InlineData("candidate-method", true, "adapter-descriptor-candidate-method-required")]
    [InlineData("candidate-method", false, "adapter-descriptor-candidate-method-required")]
    public void Evaluate_NullOrBlankDescriptorFields_FailClosed(
        string field,
        bool useNull,
        string expectedIssue)
    {
        var descriptors = Descriptors()
            .Select(descriptor => descriptor.SourceId != "pubchem"
                ? descriptor
                : field switch
                {
                    "source-id" => descriptor with
                    {
                        SourceId = useNull ? null! : " ",
                    },
                    "adapter-id" => descriptor with
                    {
                        AdapterId = useNull ? null! : " ",
                    },
                    _ => descriptor with
                    {
                        CandidateMethod = useNull ? null! : " ",
                    },
                });

        var result = Evaluate(BuildPlan(["request-a"]), descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, expectedIssue);
    }

    [Fact]
    public void Evaluate_NullDescriptor_FailsClosedWithoutThrowing()
    {
        var descriptors = Descriptors()
            .Concat([null!]);

        var result = Evaluate(BuildPlan(["request-a"]), descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, "adapter-descriptor-null");
    }

    [Fact]
    public void Evaluate_NullIntent_FailsClosedWithoutThrowing()
    {
        var plan = BuildPlan(["request-a"]);
        var intents = plan.Intents
            .Concat([null!])
            .ToList();
        plan = new SourceAcquisitionPlan(
            intents,
            ReadyCount: plan.ReadyCount,
            BlockedCount: plan.BlockedCount);

        var result = Evaluate(plan);
        var entry = result.Entries.Single(item => item.SourceId is null);

        Assert.False(result.CanActivate);
        AssertIssue(result, "intent-null");
        AssertIssue(result, "plan-counts-contradictory");
        Assert.Equal(
            SourceAcquisitionPreflightClassification.UnsupportedOrMismatched,
            entry.Classification);
    }

    [Fact]
    public void Evaluate_NullIntentCollection_FailsClosedWithoutThrowing()
    {
        var plan = new SourceAcquisitionPlan(
            null!,
            ReadyCount: 0,
            BlockedCount: 0);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, "plan-intents-null");
    }

    [Theory]
    [InlineData("source-id", true, "intent-source-id-required")]
    [InlineData("source-id", false, "intent-source-id-required")]
    [InlineData("request-id", true, "intent-request-id-required")]
    [InlineData("request-id", false, "intent-request-id-required")]
    [InlineData("compound-name", true, "intent-compound-name-required")]
    [InlineData("compound-name", false, "intent-compound-name-required")]
    [InlineData("adapter-id", true, "intent-adapter-id-required")]
    [InlineData("adapter-id", false, "intent-adapter-id-required")]
    [InlineData("candidate-method", true, "intent-candidate-method-required")]
    [InlineData("candidate-method", false, "intent-candidate-method-required")]
    [InlineData("registry-hash", true, "registry-binding-hash-required")]
    [InlineData("registry-hash", false, "registry-binding-hash-required")]
    public void Evaluate_NullOrBlankIntentFields_FailClosedWithoutThrowing(
        string field,
        bool useNull,
        string expectedIssue)
    {
        var plan = BuildPlan(["request-a"]);
        var original = ReadyIntent("pubchem", "request-a");
        var replacement = field switch
        {
            "source-id" => original with { SourceId = useNull ? null! : " " },
            "request-id" => original with { RequestId = useNull ? null! : " " },
            "compound-name" => original with
            {
                CompoundName = useNull ? null! : " ",
            },
            "adapter-id" => original with { AdapterId = useNull ? null! : " " },
            "candidate-method" => original with
            {
                CandidateMethod = useNull ? null! : " ",
            },
            _ => original with
            {
                RegistryBindingSha256 = useNull ? null! : " ",
            },
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, expectedIssue);
    }

    [Theory]
    [InlineData("null-collection", "intent-blocking-reasons-null")]
    [InlineData("blank-reason", "intent-blocking-reason-required")]
    public void Evaluate_NullOrBlankBlockingReasons_FailClosedWithoutThrowing(
        string scenario,
        string expectedIssue)
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            BlockingReasons = scenario == "null-collection"
                ? null!
                : [null!, " "],
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, expectedIssue);
    }

    [Fact]
    public void Evaluate_InvalidRegistryHash_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            RegistryBindingSha256 = RegistrySha256.ToUpperInvariant(),
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);

        var result = Evaluate(plan);
        var entry = result.Entries.Single(item => item.SourceId == "pubchem");

        Assert.False(result.CanActivate);
        AssertIssue(result, "registry-binding-hash-invalid");
        Assert.Equal(replacement.RegistryBindingSha256, entry.RegistryBindingSha256);
    }

    [Fact]
    public void Evaluate_InconsistentRegistryHashes_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            RegistryBindingSha256 =
                "aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, "registry-binding-hash-mismatch");
    }

    [Fact]
    public void Evaluate_DuplicateIntentIdentity_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var intents = plan.Intents.Concat([plan.Intents[0]]).ToList();
        plan = new SourceAcquisitionPlan(
            intents,
            ReadyCount: intents.Count,
            BlockedCount: 0);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, "intent-identity-duplicate");
    }

    [Fact]
    public void Evaluate_MissingSourceForOneRequest_FailsClosed()
    {
        var plan = BuildPlan(["request-a", "request-b"]);
        var intents = plan.Intents
            .Where(intent => intent.RequestId != "request-b"
                             || intent.SourceId != "pubchem")
            .ToList();
        plan = new SourceAcquisitionPlan(
            intents,
            ReadyCount: intents.Count,
            BlockedCount: 0);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, "request-source-missing");
    }

    [Fact]
    public void Evaluate_BlockedIntent_IsNotRunnable_WithoutExpectation()
    {
        var plan = BuildPlan(["request-a"]);
        var blocked = ReadyIntent("pubchem", "request-a") with
        {
            Disposition = SourceAcquisitionDisposition.Blocked,
            BlockingReasons = ["security-data-review-required"],
        };
        plan = ReplaceIntent(plan, "pubchem", blocked);
        var result = Evaluate(plan);
        var entry = result.Entries.Single(item => item.SourceId == "pubchem");

        Assert.False(result.CanActivate);
        Assert.Empty(result.Issues);
        Assert.Equal(
            SourceAcquisitionPreflightClassification.Blocked,
            entry.Classification);
        Assert.Equal(["security-data-review-required"], entry.BlockingReasons);
        Assert.False(entry.IsDispatchable);
    }

    [Theory]
    [InlineData(SourceAcquisitionDisposition.Ready, true, "ready-intent-has-blockers")]
    [InlineData(SourceAcquisitionDisposition.Blocked, false, "blocked-intent-has-no-blockers")]
    public void Evaluate_ContradictoryIntentDisposition_FailsClosed(
        SourceAcquisitionDisposition disposition,
        bool includeBlocker,
        string expectedIssue)
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            Disposition = disposition,
            BlockingReasons = includeBlocker ? ["unexpected-blocker"] : [],
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);

        var result = Evaluate(plan);

        Assert.False(result.CanActivate);
        AssertIssue(result, expectedIssue);
    }

    [Fact]
    public void Evaluate_UnsupportedCandidateMethod_FailsClosed()
    {
        var plan = BuildPlan(["request-a"]);
        var replacement = ReadyIntent("pubchem", "request-a") with
        {
            CandidateMethod = "bulk-download",
        };
        plan = ReplaceIntent(plan, "pubchem", replacement);
        var descriptors = Descriptors()
            .Select(descriptor => descriptor.SourceId == "pubchem"
                ? descriptor with { CandidateMethod = "bulk-download" }
                : descriptor);

        var result = Evaluate(plan, descriptors);

        Assert.False(result.CanActivate);
        AssertIssue(result, "intent-candidate-method-unsupported");
    }

    [Fact]
    public void Evaluate_StrictCurrentCampaignExpectation_FailsOnSmallerPlan()
    {
        var result = Evaluate(
            BuildPlan(["request-a"]),
            expectation:
                SourceAcquisitionCampaignExpectation.CurrentRecommendedSevenActivation);

        Assert.False(result.CanActivate);
        AssertIssue(result, "campaign-expectation-mismatch");
    }

    private static SourceAcquisitionExecutionPreflightResult Evaluate(
        SourceAcquisitionPlan plan,
        IEnumerable<SourceAcquisitionAdapterDescriptor>? descriptors = null,
        SourceAcquisitionCampaignExpectation? expectation = null) =>
        new SourceAcquisitionExecutionPreflight().Evaluate(
            plan,
            descriptors ?? Descriptors(),
            expectation);

    private static SourceAcquisitionPlan BuildPlan(IEnumerable<string> requestIds)
    {
        var intents = requestIds
            .SelectMany(requestId =>
                RecommendedOfficialSourcePlanningAdapters.SourceIds.Select(sourceId =>
                    ReadyIntent(sourceId, requestId)))
            .ToList();
        return new SourceAcquisitionPlan(
            intents,
            ReadyCount: intents.Count,
            BlockedCount: 0);
    }

    private static SourceAcquisitionIntent ReadyIntent(
        string sourceId,
        string requestId)
    {
        var candidateMethod = sourceId == "nih-nccih"
            ? "manual-review"
            : "api";
        return new SourceAcquisitionIntent(
            SourceId: sourceId,
            AdapterId: $"{sourceId}-planning-v1",
            RequestId: requestId,
            CompoundName: $"Compound {requestId}",
            SearchTerms: [$"Compound {requestId}"],
            CandidateMethod: candidateMethod,
            AuthorizedFieldUses: ["identity"],
            RequiredProvenanceFields: ["sourceRegistryId"],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);
    }

    private static IReadOnlyList<SourceAcquisitionAdapterDescriptor> Descriptors() =>
        RecommendedOfficialSourcePlanningAdapters.SourceIds
            .Select(Descriptor)
            .ToList();

    private static SourceAcquisitionAdapterDescriptor Descriptor(string sourceId) =>
        new(
            SourceId: sourceId,
            AdapterId: $"{sourceId}-planning-v1",
            CandidateMethod: sourceId == "nih-nccih"
                ? "manual-review"
                : "api");

    private static SourceAcquisitionPlan ReplaceIntent(
        SourceAcquisitionPlan plan,
        string sourceId,
        SourceAcquisitionIntent replacement)
    {
        var replaced = false;
        var intents = plan.Intents
            .Select(intent =>
            {
                if (!replaced && intent.SourceId == sourceId)
                {
                    replaced = true;
                    return replacement;
                }

                return intent;
            })
            .ToList();
        return new SourceAcquisitionPlan(
            intents,
            ReadyCount: intents.Count(
                intent => intent.Disposition == SourceAcquisitionDisposition.Ready),
            BlockedCount: intents.Count(
                intent => intent.Disposition == SourceAcquisitionDisposition.Blocked));
    }

    private static void AssertIssue(
        SourceAcquisitionExecutionPreflightResult result,
        string code) =>
        Assert.Contains(result.Issues, issue => issue.Code == code);

    private static string FormatIssues(
        SourceAcquisitionExecutionPreflightResult result) =>
        string.Join(
            Environment.NewLine,
            result.Issues.Select(issue => $"{issue.Code}: {issue.Detail}"));
}
