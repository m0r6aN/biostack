using System.Text.Json;
using Xunit;

namespace BioStack.KnowledgeWorker.Tests;

public sealed class ProtocolIntelligenceContractTests
{
    private static readonly string[] RelationshipRuntimeRequiredFields =
    [
        "evidenceTier",
        "confidence",
        "sourceRefs",
        "reviewStatus",
        "userFacingExplanation"
    ];

    [Fact]
    public void EveryProtocolIntelligenceJsonSourceResearchPathExists()
    {
        var root = RepositoryRoot();
        foreach (var path in Directory.EnumerateFiles(ArtifactDirectory(root), "*.json"))
        {
            using var document = JsonDocument.Parse(File.ReadAllText(path));
            Assert.True(
                document.RootElement.TryGetProperty("sourceResearch", out var sourceResearch),
                $"{RelativeToRoot(root, path)} missing sourceResearch.");

            var sourcePath = sourceResearch.GetString();
            Assert.False(string.IsNullOrWhiteSpace(sourcePath), $"{RelativeToRoot(root, path)} has empty sourceResearch.");
            Assert.True(
                File.Exists(Path.Combine(root, sourcePath!)),
                $"{RelativeToRoot(root, path)} sourceResearch path does not exist: {sourcePath}");
        }
    }

    [Fact]
    public void PromotionRuntimeRequirementsUseRuntimeCamelCaseFields()
    {
        using var document = ReadArtifact("promotion-target-specs.json");
        var requirements = document.RootElement
            .GetProperty("globalRequirements")
            .GetProperty("runtimeVisibilityRequires")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Contains("sourceRefs", requirements);
        Assert.Contains("evidenceTier", requirements);
        Assert.Contains("reviewStatusApproved", requirements);
        Assert.DoesNotContain("source_refs", requirements);
        Assert.DoesNotContain("evidence_tier", requirements);
        Assert.DoesNotContain("review_status_approved", requirements);
    }

    [Fact]
    public void PromotionBlockedOutputsAreGlobalHighRiskBlockedOutputIds()
    {
        using var promotionTargets = ReadArtifact("promotion-target-specs.json");
        using var highRisk = ReadArtifact("high-risk-guardrails.json");
        var globalBlockedOutputs = highRisk.RootElement
            .GetProperty("globalBlockedOutputs")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToHashSet(StringComparer.Ordinal);

        var promotionBlockedOutputs = promotionTargets.RootElement
            .GetProperty("globalRequirements")
            .GetProperty("blockedOutputs")
            .EnumerateArray()
            .Select(item => item.GetString());

        foreach (var blockedOutput in promotionBlockedOutputs)
        {
            Assert.Contains(blockedOutput, globalBlockedOutputs);
        }
    }

    [Fact]
    public void SideEffectPromotionTargetFieldsMatchDetectorRequiredFieldsExactly()
    {
        using var promotionTargets = ReadArtifact("promotion-target-specs.json");
        using var detector = ReadArtifact("side-effect-ambiguity-detector.json");

        var promotionFields = promotionTargets.RootElement
            .GetProperty("targets")
            .EnumerateArray()
            .Single(target => target.GetProperty("id").GetString() == "side_effect_ambiguity_artifact")
            .GetProperty("requiredFields")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        var detectorFields = detector.RootElement
            .GetProperty("requiredArtifactFields")
            .EnumerateArray()
            .Select(item => item.GetString())
            .ToArray();

        Assert.Equal(detectorFields, promotionFields);
    }

    [Fact]
    public void EveryRelationshipTypeRequiresEvidenceConfidenceCitationsReviewStateAndUserExplanation()
    {
        using var document = ReadArtifact("relationship-taxonomy.json");
        foreach (var relationshipType in document.RootElement.GetProperty("relationshipTypes").EnumerateArray())
        {
            var fields = relationshipType
                .GetProperty("requiredFields")
                .EnumerateArray()
                .Select(item => item.GetString())
                .ToHashSet(StringComparer.Ordinal);

            foreach (var requiredField in RelationshipRuntimeRequiredFields)
            {
                Assert.Contains(requiredField, fields);
            }
        }
    }

    [Fact]
    public void SourceQualityBlockedOutputsAreNormalizedIds()
    {
        using var document = ReadArtifact("source-quality-taxonomy.json");
        foreach (var sourceClass in document.RootElement.GetProperty("sourceClasses").EnumerateArray())
        {
            var sourceClassId = sourceClass.GetProperty("id").GetString();
            foreach (var blockedOutput in sourceClass.GetProperty("blockedOutputs").EnumerateArray())
            {
                var id = blockedOutput.GetString();
                Assert.False(string.IsNullOrWhiteSpace(id), $"{sourceClassId} has empty blockedOutputs entry.");
                Assert.DoesNotContain(" ", id);
                Assert.DoesNotContain("-", id);
                Assert.Matches("^[a-z0-9_]+$", id!);
            }
        }
    }

    private static JsonDocument ReadArtifact(string fileName)
        => JsonDocument.Parse(File.ReadAllText(Path.Combine(ArtifactDirectory(RepositoryRoot()), fileName)));

    private static string ArtifactDirectory(string root)
        => Path.Combine(root, "research", "protocol-intelligence");

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

    private static string RelativeToRoot(string root, string path)
        => Path.GetRelativePath(root, path).Replace('\\', '/');
}
