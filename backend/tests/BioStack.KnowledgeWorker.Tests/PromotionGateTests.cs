namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Refresh-specific disposition coverage, including historical approvals, subsequent
/// holds and tied decisions. Shared research-index semantics are not changed.
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

    [Theory]
    [InlineData("request-changes")]
    [InlineData("archive-draft")]
    [InlineData("reject")]
    [InlineData("approve-claims")]
    [InlineData("approve-for-promotion")]
    public void Later_Blocking_Disposition_Revokes_Historical_Approval(string blocking)
    {
        var approval = Dated("approve-for-promotion", true, "2026-08-29T15:36:24Z");
        var later = Dated(blocking, false, "2026-09-05T10:46:48Z");
        Assert.False(new ReviewDecisionPromotionGate(BuildIndex(approval, later)).Evaluate("Tamoxifen").Allowed);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Equal_Time_Blocker_Wins_Regardless_Of_Input_Or_Id_Order(bool reverse)
    {
        var approval = Dated("approve-for-promotion", true, "2026-09-05T10:46:48Z");
        var blocker = Dated("request-changes", false, "2026-09-05T10:46:48Z");
        approval["decisionId"] = reverse ? "z" : "a";
        blocker["decisionId"] = reverse ? "a" : "z";
        var items = reverse ? new[] { blocker, approval } : new[] { approval, blocker };
        Assert.False(new ReviewDecisionPromotionGate(BuildIndex(items)).Evaluate("Tamoxifen").Allowed);
    }

    [Fact]
    public void New_Explicit_Promotion_Clears_Older_Hold_But_Item_Resolution_Does_Not()
    {
        var approval = Dated("approve-for-promotion", true, "2026-08-29T00:00:00Z");
        var hold = Dated("request-changes", false, "2026-09-05T00:00:00Z");
        var resolution = Dated("resolve-review-items", false, "2026-09-06T00:00:00Z");
        Assert.False(new ReviewDecisionPromotionGate(BuildIndex(approval, hold, resolution)).Evaluate("Tamoxifen").Allowed);
        var reapproval = Dated("approve-for-promotion", true, "2026-09-07T00:00:00Z");
        Assert.True(new ReviewDecisionPromotionGate(BuildIndex(approval, hold, resolution, reapproval)).Evaluate("Tamoxifen").Allowed);
    }

    [Fact]
    public void Tamoxifen_Recorded_Chronology_Reports_Later_RequestChanges()
    {
        // Exact disposition/date sequence from retained wave-r1 and wave006 hold batches.
        var old = Dated("approve-claims", false, "2026-08-29T15:36:24Z");
        var hold = Dated("request-changes", false, "2026-09-05T10:46:48Z");
        var gate = new ReviewDecisionPromotionGate(BuildIndex(old, hold));
        Assert.Equal(PromotionSkipReason.RequestChanges, gate.Evaluate("Tamoxifen").SkipReason);
    }

    private static JsonObject Dated(string kind, bool clears, string time)
    {
        var decision = Decision("Tamoxifen", kind, clears);
        decision["reviewedAt"] = time;
        return decision;
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
