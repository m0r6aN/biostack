namespace BioStack.Application.Tests.ProtocolIntelligence;

using System.Collections.Generic;
using System.Linq;
using BioStack.Application.Governance;
using BioStack.Application.ProtocolIntelligence;
using Xunit;

/// <summary>
/// Build-time gate over the canonical research/protocol-intelligence/*.json artifacts.
/// Deterministic and CI-friendly: it loads the real artifacts from the repo and proves the
/// gate is doctrine-clean via <see cref="DoctrineSanitizer"/> only — no parallel scanner.
/// </summary>
public sealed class ProtocolIntelligenceGateTests
{
    private static ProtocolIntelligenceGate BuildGate()
        => new(new ProtocolIntelligenceArtifactLoader(), new DoctrineSanitizer());

    // A fully-formed, approved relationship_artifact with doctrine-clean user-facing text.
    private static Dictionary<string, object?> ValidRelationshipArtifact() => new()
    {
        ["relationshipType"] = "synergy",
        ["subject"] = "compound:magnesium-glycinate",
        ["object"] = "compound:ashwagandha",
        ["phaseContext"] = "maintenance",
        ["goalContext"] = "sleep-support",
        ["evidenceTier"] = "observational",
        ["confidence"] = "moderate",
        ["sourceRefs"] = new[] { "source:pubmed:12345" },
        ["sourceAuthorityMix"] = "peer_reviewed",
        ["safetyConcernLevel"] = "low",
        ["productHandling"] = "dietary_supplement",
        ["reviewStatus"] = "approved",
        ["userFacingExplanation"] = "Observed co-occurrence in logged user context; educational reference only.",
    };

    [Fact]
    public void Load_AllSevenArtifacts_Succeeds()
    {
        var set = new ProtocolIntelligenceArtifactLoader().Load();

        // 6 promotion targets, all 7 artifact files versioned, source classes populated.
        Assert.Equal(6, set.PromotionTargets.Count);
        Assert.Equal(7, set.ArtifactVersions.Count);
        Assert.Contains("relationship_artifact", set.PromotionTargets.Keys);
        Assert.NotEmpty(set.SourceClasses);
        Assert.NotEmpty(set.SupportedRelationshipIds);
        Assert.NotEmpty(set.GlobalBlockedOutputs);
    }

    [Fact]
    public void Evaluate_UnknownArtifactType_Blocks()
    {
        var result = BuildGate().Evaluate(new PromotionGateRequest("not_a_real_type", new()));

        Assert.False(result.CanPromote);
        Assert.Equal([GateReasons.UnknownArtifactType], result.BlockingReasons);
    }

    [Fact]
    public void Evaluate_ValidApprovedRelationship_CanPromote()
    {
        var result = BuildGate().Evaluate(
            new PromotionGateRequest("relationship_artifact", ValidRelationshipArtifact()));

        Assert.True(result.CanPromote, $"blocked by: {string.Join(", ", result.BlockingReasons)}");
        Assert.Empty(result.BlockingReasons);
        Assert.Empty(result.RequiredFieldsMissing);
        Assert.Empty(result.DoctrineViolationFields);
        // The relationship taxonomy demands human review even when approved.
        Assert.True(result.RequiresHumanReview);
    }

    [Fact]
    public void Evaluate_MissingRequiredFields_Blocks()
    {
        var artifact = ValidRelationshipArtifact();
        artifact.Remove("subject");
        artifact.Remove("sourceRefs");

        var result = BuildGate().Evaluate(new PromotionGateRequest("relationship_artifact", artifact));

        Assert.False(result.CanPromote);
        Assert.Contains(GateReasons.RequiredFieldsMissing, result.BlockingReasons);
        Assert.Contains("subject", result.RequiredFieldsMissing);
        Assert.Contains("sourceRefs", result.RequiredFieldsMissing);
    }

    [Fact]
    public void Evaluate_DoctrineViolationInUserFacingField_BlocksViaSanitizerOnly()
    {
        var artifact = ValidRelationshipArtifact();
        // Imperative medical phrasing — caught by DoctrineSanitizer ("you should", "take 10 mg").
        artifact["userFacingExplanation"] = "You should take 10 mg twice daily for best results.";

        var result = BuildGate().Evaluate(new PromotionGateRequest("relationship_artifact", artifact));

        Assert.False(result.CanPromote);
        Assert.Contains(GateReasons.DoctrineViolation, result.BlockingReasons);
        Assert.Contains("userFacingExplanation", result.DoctrineViolationFields);
    }

    [Fact]
    public void Evaluate_NotApproved_BlocksAndRequiresHumanReview()
    {
        var artifact = ValidRelationshipArtifact();
        artifact["reviewStatus"] = "pending";

        var result = BuildGate().Evaluate(new PromotionGateRequest("relationship_artifact", artifact));

        Assert.False(result.CanPromote);
        Assert.Contains(GateReasons.ReviewStatusNotApproved, result.BlockingReasons);
        Assert.Contains(GateReasons.HumanReviewRequired, result.BlockingReasons);
    }
}
