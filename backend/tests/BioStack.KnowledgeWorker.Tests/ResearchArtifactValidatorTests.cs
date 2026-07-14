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
    public void Validator_Accepts_Governed_Pilot_Source_Registry()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var path = Path.Combine(repositoryRoot, "research", "input", "sources", "pilot-source-registry.json");
        var artifact = new ResearchArtifactLoader().Load(ResearchArtifactKind.SourceRegistry, path);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
        var sources = artifact.Node["sources"]!.AsArray();
        Assert.Equal(13, sources.Count);
        Assert.All(sources, source =>
        {
            Assert.Equal("pending-human-legal", source!["rights"]!["reviewStatus"]!.GetValue<string>());
            Assert.Equal("disabled", source["operations"]!["status"]!.GetValue<string>());
            Assert.False(source["acquisition"]!["enabled"]!.GetValue<bool>());
            Assert.Empty(source["rights"]!["allowedUses"]!.AsArray());
        });
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("operations")]
    [InlineData("acquisition")]
    [InlineData("evidencePolicy")]
    [InlineData("provenanceRequirements")]
    [InlineData("refreshPolicy")]
    [InlineData("remediation")]
    [InlineData("dataBoundary")]
    public void Validator_Rejects_Source_Registry_Missing_Governance_Section(string section)
    {
        var artifact = LoadArtifact(ResearchArtifactKind.SourceRegistry, "source-registry.sample.json");
        artifact.Node["sources"]![0]!.AsObject().Remove(section);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Keyword.Equals("required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("operations")]
    [InlineData("acquisition")]
    [InlineData("evidence")]
    [InlineData("provenance")]
    [InlineData("refresh")]
    [InlineData("remediation")]
    [InlineData("data-boundary")]
    public void Validator_Rejects_Enabled_Source_With_Incomplete_Activation_Evidence(string prerequisite)
    {
        var artifact = LoadArtifact(ResearchArtifactKind.SourceRegistry, "source-registry.sample.json");
        var source = artifact.Node["sources"]![0]!;
        switch (prerequisite)
        {
            case "rights": source["rights"]!["legalBasisOrLicense"] = null; break;
            case "operations": source["operations"]!["ownerRole"] = null; break;
            case "acquisition": source["acquisition"]!["method"] = "none"; break;
            case "evidence": source["evidencePolicy"]!["authorizedFieldUse"] = new JsonArray(); break;
            case "provenance": source["provenanceRequirements"]!["requiredFields"] = new JsonArray(); break;
            case "refresh": source["refreshPolicy"]!["cadence"] = null; break;
            case "remediation": source["remediation"]!["contactRole"] = null; break;
            case "data-boundary": source["dataBoundary"]!["permittedContent"] = new JsonArray(); break;
        }
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.False(result.IsValid);
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
