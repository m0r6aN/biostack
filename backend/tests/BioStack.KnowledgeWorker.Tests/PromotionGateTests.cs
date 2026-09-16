namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Unit coverage for the promotion gate's classification logic. The actual promotion
/// test is <see cref="ReviewDecisionIndex.HasPromotionApproval"/> (unchanged, reused
/// as-is); these tests only pin down what <see cref="ReviewDecisionPromotionGate"/>
/// reports when that test fails, for each class of non-promoted record the audit
/// (a1-promotion-audit/promotion-state-audit.md §2.2, §5) found sitting in the seed file.
/// </summary>
public class PromotionGateTests
{
    [Fact]
    public void Evaluate_Allows_ApproveForPromotion_With_ClearedBlockers()
    {
        var index = BuildIndex(Decision("Semaglutide", "approve-for-promotion", clears: true));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("Semaglutide");

        Assert.True(decision.Allowed);
        Assert.Null(decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Skips_Compound_With_No_Decision_On_File()
    {
        var index = BuildIndex(Decision("Semaglutide", "approve-for-promotion", clears: true));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("Creatine monohydrate");

        Assert.False(decision.Allowed);
        Assert.Equal(PromotionSkipReason.NoDecision, decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Skips_ApproveClaims_Only_As_Not_Promoted()
    {
        var index = BuildIndex(Decision("Epitalon", "approve-claims", clears: false));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("Epitalon");

        Assert.False(decision.Allowed);
        Assert.Equal(PromotionSkipReason.ApproveClaimsOnly, decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Skips_RequestChanges_As_Not_Promoted()
    {
        // Mirrors the wave-006 re-review state for Creatine/Vitamin D3/Tamoxifen
        // (promotion-state-audit.md §5): latest decision is request-changes.
        var index = BuildIndex(Decision("Creatine monohydrate", "request-changes", clears: false));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("Creatine monohydrate");

        Assert.False(decision.Allowed);
        Assert.Equal(PromotionSkipReason.RequestChanges, decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Skips_ApproveForPromotion_With_Blockers_Not_Cleared()
    {
        var index = BuildIndex(Decision("Semax", "approve-for-promotion", clears: false));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("Semax");

        Assert.False(decision.Allowed);
        Assert.Equal(PromotionSkipReason.BlockersNotCleared, decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Skips_Archived_Compound()
    {
        var index = BuildIndex(Decision("KPV", "archive-draft", clears: false));
        var gate = new ReviewDecisionPromotionGate(index);

        var decision = gate.Evaluate("KPV");

        Assert.False(decision.Allowed);
        Assert.Equal(PromotionSkipReason.ArchivedOrRejected, decision.SkipReason);
    }

    [Fact]
    public void Evaluate_Is_Case_Insensitive_On_Compound_Name()
    {
        var index = BuildIndex(Decision("Semaglutide", "approve-for-promotion", clears: true));
        var gate = new ReviewDecisionPromotionGate(index);

        Assert.True(gate.Evaluate("SEMAGLUTIDE").Allowed);
        Assert.True(gate.Evaluate("semaglutide").Allowed);
    }

    [Fact]
    public void AllowAllPromotionGate_Approves_Everything_And_Is_Not_Enforced()
    {
        var gate = AllowAllPromotionGate.Instance;

        Assert.False(gate.IsEnforced);
        Assert.True(gate.Evaluate("Anything At All").Allowed);
    }

    [Fact]
    public void ReviewDecisionPromotionGate_Is_Enforced()
    {
        Assert.True(new ReviewDecisionPromotionGate(ReviewDecisionIndex.Empty).IsEnforced);
    }

    // ── helpers ─────────────────────────────────────────────────────────────────

    private static ReviewDecisionIndex BuildIndex(params JsonObject[] decisions)
    {
        var batch = new JsonObject
        {
            ["decisions"] = new JsonArray(decisions.Select(d => (JsonNode)d.DeepClone()).ToArray()),
        };
        return ReviewDecisionIndex.FromBatches(new JsonNode[] { batch });
    }

    private static JsonObject Decision(string compoundName, string decision, bool clears) => new()
    {
        ["decisionId"] = $"{compoundName}-{decision}",
        ["compoundName"] = compoundName,
        ["decision"] = decision,
        ["reviewerId"] = "test-reviewer",
        ["reviewedAt"] = "2026-09-05T00:00:00Z",
        ["clearsSoftPromotionBlockers"] = clears,
        ["scope"] = new JsonObject(),
        ["notes"] = new JsonArray(),
    };
}
