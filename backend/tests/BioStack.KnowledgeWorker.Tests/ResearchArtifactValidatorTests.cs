namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
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

    [Fact]
    public void Validator_Accepts_Recommended_Seven_Source_Decision_Batch_With_Exact_Registry_Binding()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var registryPath = Path.Combine(
            repositoryRoot,
            "research",
            "input",
            "sources",
            "pilot-source-registry.json");
        var artifact = new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
        var registry = JsonNode.Parse(File.ReadAllText(registryPath))!;
        Assert.Equal(
            registry["schemaVersion"]!.GetValue<string>(),
            artifact.Node["registryBinding"]!["schemaVersion"]!.GetValue<string>());
        using var registryStream = File.OpenRead(registryPath);
        var registrySha256 = Convert.ToHexString(SHA256.HashData(registryStream)).ToLowerInvariant();
        Assert.Equal(
            registrySha256,
            artifact.Node["registryBinding"]!["sha256"]!.GetValue<string>());
        Assert.Equal("0a625778407fc85f3e32ed620b578bf4fe37cd37acb09c938776d9ed82aa7163", registrySha256);
    }

    [Fact]
    public void Recommended_Seven_Source_Decisions_Have_Exact_Assignments_And_Remain_Fail_Closed()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var artifact = new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
        var expectedSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "fda",
            "pubchem",
            "pubmed",
            "clinicaltrials",
            "dailymed",
            "nih-ods",
            "nih-nccih",
        };
        var expectedOwners = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["product-owner"] = "Clint Morgan",
            ["legal-rights-approver"] = "Johnathan Harper",
            ["evidence-reviewer"] = "Ellison Nemoy",
            ["security-data-owner"] = "Pradic Patel",
        };

        var owners = artifact.Node["owners"]!.AsArray();
        Assert.Equal(4, owners.Count);
        Assert.Equal(4, owners.Select(owner => owner!["roleId"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, owners.Select(owner => owner!["personName"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
        foreach (var owner in owners)
        {
            var roleId = owner!["roleId"]!.GetValue<string>();
            Assert.Equal(expectedOwners[roleId], owner["personName"]!.GetValue<string>());
            Assert.Equal("assigned", owner["assignmentStatus"]!.GetValue<string>());
            Assert.Equal("pending", owner["approvalStatus"]!.GetValue<string>());
        }

        var sources = artifact.Node["sources"]!.AsArray();
        Assert.Equal(7, sources.Count);
        var actualSources = sources
            .Select(source => source!["sourceId"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expectedSources, actualSources);
        Assert.Equal(7, actualSources.Count);

        foreach (var source in sources)
        {
            Assert.Equal("pending-human-signoff", source!["decisionStatus"]!.GetValue<string>());
            Assert.False(source["activationReady"]!.GetValue<bool>());
            Assert.Equal("pending-human-legal", source["rights"]!["reviewStatus"]!.GetValue<string>());
            Assert.Empty(source["rights"]!["allowedUses"]!.AsArray());
            Assert.Null(source["rights"]!["legalBasisOrLicense"]);
            Assert.Null(source["rights"]!["reviewedBy"]);
            Assert.Equal("disabled", source["operations"]!["status"]!.GetValue<string>());
            Assert.False(source["acquisition"]!["enabled"]!.GetValue<bool>());
            Assert.Equal("none", source["acquisition"]!["method"]!.GetValue<string>());
            Assert.Equal("not-reviewed", source["acquisition"]!["robotsPolicyStatus"]!.GetValue<string>());
            Assert.Equal("not-reviewed", source["acquisition"]!["apiTermsStatus"]!.GetValue<string>());
            Assert.Equal("disabled-until-approved", source["refresh"]!["mode"]!.GetValue<string>());
            Assert.False(source["dataBoundary"]!["trainingUseAllowed"]!.GetValue<bool>());

            var approvals = source["approvals"]!.AsObject();
            Assert.Equal(4, approvals.Count);
            Assert.All(approvals, approval =>
            {
                Assert.Equal("pending", approval.Value!["status"]!.GetValue<string>());
                Assert.Null(approval.Value["decidedAtUtc"]);
                Assert.Empty(approval.Value["decisionNotes"]!.AsArray());
            });
            Assert.Equal("Clint Morgan", approvals["product"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("Johnathan Harper", approvals["legalRights"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("Ellison Nemoy", approvals["evidence"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("Pradic Patel", approvals["securityData"]!["assigneeName"]!.GetValue<string>());
        }
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
