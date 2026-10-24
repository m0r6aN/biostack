namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Council;
using BioStack.KnowledgeWorker.Jobs;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim-level sign-off by the two-reviewer AI council (no human approval gate).</summary>
public sealed class AiCouncilClaimTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 25, 8, 0, 0, TimeSpan.Zero);
    private static readonly ResearchArtifactValidator Validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

    private static CouncilReviewItem Item()
        => new("TB-500", "Hype Exceeds Evidence", Now.AddDays(-1), new JsonObject
        {
            ["evidence"] = new JsonObject
            {
                ["publicClaims"] = new JsonArray(
                    new JsonObject { ["id"] = "c-myth", ["tier"] = "Contradicted" },
                    new JsonObject { ["id"] = "c-animal", ["tier"] = "Plausible Theory" },
                    new JsonObject { ["id"] = "c-skip", ["tier"] = "Unsupported" }),
            },
        });

    private static ReviewerVerdict V(string id, params ClaimReviewVerdict[] claims)
        => new(id, "fake", id + "-model", "approve", "Hype Exceeds Evidence", Array.Empty<string>(), Array.Empty<string>(), "ok")
        { Claims = claims };

    [Fact]
    public void Parse_reads_claim_verdicts_and_drops_invalid_entries()
    {
        var v = ReviewerPrompt.Parse("a", "fake", "m", """
            {"decision":"approve","proposedTier":"Contradicted","concerns":[],"legalRisks":[],"rationale":"r",
             "claims":[{"id":"c1","decision":"approve","proposedTier":"contradicted","rationale":"fits"},
                       {"id":"c2","decision":"maybe","proposedTier":"Contradicted"},
                       {"id":"c3","decision":"approve","proposedTier":"Strong"}]}
            """);
        var c = Assert.Single(v.Claims);
        Assert.Equal(("c1", "approve", "Contradicted"), (c.ClaimId, c.Decision, c.ProposedTier));
        Assert.Contains("publicClaims", ReviewerPrompt.System);
    }

    [Fact]
    public void Consensus_at_claim_tier_signs_off_disagreement_escalates_and_missing_is_incomplete()
    {
        var a = V("a", new("c-myth", "approve", "Contradicted", "x"), new("c-animal", "approve", "Plausible Theory", "x"));
        var b = V("b", new("c-myth", "approve", "Contradicted", "y"), new("c-animal", "request-changes", "Anecdotal Signal", "y"));

        var outcome = CouncilAdjudicator.Adjudicate(Item(), new[] { a, b });
        var byId = outcome.Claims.ToDictionary(c => c.ClaimId);

        Assert.Equal(ClaimCouncilOutcomes.Consensus, byId["c-myth"].Outcome);
        Assert.Equal("approve", byId["c-myth"].Decision);
        Assert.Equal("Contradicted", byId["c-myth"].Tier);

        Assert.Equal(ClaimCouncilOutcomes.Escalated, byId["c-animal"].Outcome);
        Assert.Equal(new[] { "A", "B", "C" }, byId["c-animal"].OwnerOptions.Select(o => o.OptionId));
        Assert.Equal(3, byId["c-animal"].OwnerOptions.Select(o => (o.Decision, o.ProposedTier)).Distinct().Count());

        Assert.Equal(ClaimCouncilOutcomes.Incomplete, byId["c-skip"].Outcome);
    }

    [Fact]
    public void Consensus_approve_at_a_different_tier_escalates_instead_of_publishing()
    {
        var a = V("a", new ClaimReviewVerdict("c-myth", "approve", "Unsupported", "x"));
        var b = V("b", new ClaimReviewVerdict("c-myth", "approve", "Unsupported", "y"));
        var c = CouncilAdjudicator.AdjudicateClaims(Item(), new[] { a, b }).Single(x => x.ClaimId == "c-myth");
        Assert.Equal(ClaimCouncilOutcomes.Escalated, c.Outcome);
    }

    [Fact]
    public void Decision_batch_with_claims_is_schema_valid_and_feeds_the_sign_off_index()
    {
        var a = V("a", new ClaimReviewVerdict("c-myth", "approve", "Contradicted", "x"));
        var b = V("b", new ClaimReviewVerdict("c-myth", "approve", "Contradicted", "y"));
        var outcome = CouncilAdjudicator.Adjudicate(Item(), new[] { a, b });
        var batch = CouncilReviewJob.BuildDecisionBatch(new[] { outcome }, Now, "2026-10-25T08-00-00Z")!;

        var validation = Validator.Validate(ResearchArtifactKind.ReviewDecisionBatch, batch);
        Assert.True(validation.IsValid, validation.Summary());

        var signOff = Assert.Single(ClaimSignOffIndex.FromBatches(new[] { batch }).For("TB-500", "c-myth"));
        Assert.Equal("consensus", signOff.Outcome);
        Assert.Equal(new[] { "fake/a-model", "fake/b-model" }, signOff.ReviewerModels);
        Assert.Equal(Now, signOff.ReviewedAt);
    }

    [Fact]
    public void A_new_claim_version_makes_the_record_due_again()
    {
        var record = JsonNode.Parse("""
            {"identity":{"canonicalName":"TB-500"},"provenance":{"lastReviewedAt":"2026-10-01T00:00:00Z"},
             "evidence":{"overallTier":"Hype Exceeds Evidence","publicClaims":[{"id":"c","versions":[{"verifiedAt":"2026-10-24T00:00:00Z"}]}]}}
            """)!;
        var batch = JsonNode.Parse("""
            {"decisions":[{"decisionId":"d","compoundName":"TB-500","decision":"approve-for-promotion","reviewerId":"ai-council","reviewedAt":"2026-10-10T00:00:00Z"}]}
            """)!;
        var items = CouncilItemSelector.Select(new JsonArray(record), ReviewDecisionIndex.FromBatches(new[] { batch }), 10);
        Assert.Single(items);
    }
}
