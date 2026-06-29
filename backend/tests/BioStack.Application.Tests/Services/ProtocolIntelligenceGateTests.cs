namespace BioStack.Application.Tests.Services;

using BioStack.Application.ProtocolIntelligence;
using BioStack.Application.Services;
using BioStack.Domain.Enums;
using Xunit;

public sealed class ProtocolIntelligenceGateTests
{
    [Fact]
    public void MissingSourceRefsBlocksRelationshipArtifact()
    {
        var gate = CreateGate();
        var request = new PromotionGateRequest("relationship_artifact", new Dictionary<string, object?>
        {
            ["relationshipType"] = "substance_affects_pathway",
            ["subject"] = "Semaglutide",
            ["object"] = "GI symptoms",
            ["phaseContext"] = "active",
            ["goalContext"] = "observation",
            ["evidenceTier"] = "clinical_study",
            ["confidence"] = "moderate",
            ["sourceAuthorityMix"] = "official_and_literature",
            ["safetyConcernLevel"] = "medium",
            ["productHandling"] = "label_specific",
            ["reviewStatus"] = "approved",
            ["userFacingExplanation"] = "Reviewed relationship only."
        });

        var result = gate.Evaluate(request);

        Assert.False(result.CanPromote);
        Assert.Contains("sourceRefs", result.RequiredFieldsMissing);
    }

    [Fact]
    public void MissingUserFacingExplanationBlocksRelationshipArtifact()
    {
        var gate = CreateGate();
        var request = ValidRelationship();
        request.Artifact.Remove("userFacingExplanation");

        var result = gate.Evaluate(request);

        Assert.False(result.CanPromote);
        Assert.Contains("userFacingExplanation", result.RequiredFieldsMissing);
    }

    [Fact]
    public void UnapprovedReviewStatusBlocksRuntimeVisibility()
    {
        var gate = CreateGate();
        var request = ValidRelationship();
        request.Artifact["reviewStatus"] = "pending";

        var result = gate.Evaluate(request);

        Assert.False(result.CanPromote);
        Assert.Contains("review_status_not_approved", result.BlockingReasons);
    }

    [Fact]
    public void SideEffectAmbiguityArtifactAcceptsCanonicalFieldsAndRejectsLegacyFields()
    {
        var gate = CreateGate();
        var canonical = new PromotionGateRequest("side_effect_ambiguity_artifact", new Dictionary<string, object?>
        {
            ["symptomOrOutcome"] = "nausea reported",
            ["onsetWindow"] = "within recent check-ins",
            ["recentChanges"] = new[] { "phase changed" },
            ["phaseContext"] = "active",
            ["overlapDomains"] = new[] { "gi_symptoms" },
            ["sourceQualityFlags"] = Array.Empty<string>(),
            ["highRiskCategoryFlags"] = Array.Empty<string>(),
            ["evidenceTier"] = "observational",
            ["confidence"] = "low",
            ["userFacingBoundary"] = "Observation prompt only.",
            ["reviewStatus"] = "approved"
        });

        var canonicalResult = gate.Evaluate(canonical);

        Assert.True(canonicalResult.CanPromote);

        var legacy = new PromotionGateRequest("side_effect_ambiguity_artifact", new Dictionary<string, object?>
        {
            ["symptom"] = "nausea reported",
            ["onsetWindow"] = "within recent check-ins",
            ["recentChanges"] = new[] { "phase changed" },
            ["overlapDomains"] = new[] { "gi_symptoms" },
            ["sourceQualityFlags"] = Array.Empty<string>(),
            ["evidenceTier"] = "observational",
            ["confidence"] = "low",
            ["boundaryText"] = "Observation prompt only.",
            ["reviewStatus"] = "approved"
        });

        var legacyResult = gate.Evaluate(legacy);

        Assert.False(legacyResult.CanPromote);
        Assert.Contains("symptomOrOutcome", legacyResult.RequiredFieldsMissing);
        Assert.Contains("userFacingBoundary", legacyResult.RequiredFieldsMissing);
    }

    [Fact]
    public void ForbiddenOutputBlocksPromotionAndReturnsMatchedRuleId()
    {
        var gate = CreateGate();
        var request = ValidRelationship();
        request.Artifact["userFacingExplanation"] = "You should start this because it is safe and effective.";

        var result = gate.Evaluate(request);

        Assert.False(result.CanPromote);
        Assert.Contains("recommend_start_stop_taper_combine_or_escalate", result.ForbiddenOutputMatches);
        Assert.Contains("claims_investigational_peptides_safe_or_effective", result.ForbiddenOutputMatches);
    }

    [Fact]
    public void HighRiskSourceQualityClaimsRequireHumanReview()
    {
        var gate = CreateGate();
        var request = new PromotionGateRequest("source_quality_artifact", new Dictionary<string, object?>
        {
            ["subject"] = "Research chemical",
            ["sourceClass"] = "gray_market",
            ["authorityRefs"] = new[] { "source-registry:wada" },
            ["identityConfidence"] = "low",
            ["purityConfidence"] = "low",
            ["labelConfidence"] = "low",
            ["warningFirst"] = true,
            ["blockedOutputs"] = new[] { "sourcing_guidance" }
        });

        var result = gate.Evaluate(request);

        Assert.False(result.CanPromote);
        Assert.True(result.RequiresHumanReview);
        Assert.Contains("human_review_required", result.BlockingReasons);
    }

    [Fact]
    public void ProtocolIntelligenceServiceReturnsUnknownWhenNoReviewedArtifactExists()
    {
        var service = new ProtocolIntelligenceService(CreateGate(), new ForbiddenOutputScanner(CreateLoader()));

        var result = service.BuildResponse(
            Guid.NewGuid(),
            ProductTier.Observer,
            []);

        Assert.Equal("Unknown", result.Status);
        Assert.NotEmpty(result.Unknowns);
        Assert.Empty(result.Relationships);
    }

    private static PromotionGateRequest ValidRelationship() => new("relationship_artifact", new Dictionary<string, object?>
    {
        ["relationshipType"] = "substance_affects_pathway",
        ["subject"] = "Semaglutide",
        ["object"] = "GI symptoms",
        ["phaseContext"] = "active",
        ["goalContext"] = "observation",
        ["evidenceTier"] = "clinical_study",
        ["confidence"] = "moderate",
        ["sourceRefs"] = new[] { "pmid:123" },
        ["sourceAuthorityMix"] = "official_and_literature",
        ["safetyConcernLevel"] = "medium",
        ["productHandling"] = "label_specific",
        ["reviewStatus"] = "approved",
        ["userFacingExplanation"] = "Reviewed relationship only."
    });

    private static ProtocolIntelligenceGate CreateGate()
        => new(CreateLoader(), new ForbiddenOutputScanner(CreateLoader()));

    private static ProtocolIntelligenceArtifactLoader CreateLoader()
        => new(RepositoryRoot());

    private static string RepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, "docs")) &&
                Directory.Exists(Path.Combine(directory.FullName, "research")) &&
                Directory.Exists(Path.Combine(directory.FullName, "backend")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the BioStack repository root.");
    }
}
