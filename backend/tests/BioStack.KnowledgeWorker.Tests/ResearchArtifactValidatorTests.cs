namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class ResearchArtifactValidatorTests
{
    public static IEnumerable<object[]> ValidFixtures => new[]
    {
        new object[] { ResearchArtifactKind.CompoundCandidateBatch, "compound-candidates.sample.json" },
        new object[] { ResearchArtifactKind.SourceRegistry, "source-registry.sample.json" },
        new object[] { ResearchArtifactKind.EvidencePacket, "evidence-packet.sample.json" },
        new object[] { ResearchArtifactKind.ReviewDecisionBatch, "review-decision.sample.json" },
        new object[] { ResearchArtifactKind.ResearchRequestBatch, "research-request.sample.json" },
    };

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void Validator_Accepts_Valid_Research_Fixtures(ResearchArtifactKind kind, string fixtureName)
    {
        var artifact = LoadArtifact(kind, fixtureName);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(kind, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
    }

    [Fact]
    public void Validator_Rejects_Candidate_Batch_Missing_Candidates()
    {
        var artifact = LoadArtifact(ResearchArtifactKind.CompoundCandidateBatch, "compound-candidates.sample.json");
        artifact.Node.AsObject().Remove("candidates");
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.CompoundCandidateBatch, artifact.Node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Keyword.Equals("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_Rejects_Evidence_Packet_With_Unknown_ClaimType()
    {
        var artifact = LoadArtifact(ResearchArtifactKind.EvidencePacket, "evidence-packet.sample.json");
        artifact.Node["claims"]![0]!["claimType"] = "unsupported-claim-type";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.EvidencePacket, artifact.Node);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Loader_Rejects_Non_Object_Root()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"biostack-research-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, "[]");
            var loader = new ResearchArtifactLoader();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                loader.Load(ResearchArtifactKind.SourceRegistry, tempPath));

            Assert.Contains("must be a JSON object", ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static LoadedResearchArtifact LoadArtifact(ResearchArtifactKind kind, string fixtureName)
    {
        var loader = new ResearchArtifactLoader();
        return loader.Load(kind, TestPaths.FixturePath(fixtureName));
    }
}