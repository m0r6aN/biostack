namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class SourceAuthorizationVersionTests
{
    [Fact]
    public void Successor_Continuity_Preserves_Historical_Approvals_And_Only_Applies_A1_Owner_Change()
    {
        var record = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot,
            "research/source-authorization/source-authorization-continuity-2026-09-08.v1.json")))!;
        Assert.Equal("source-authorization-mechanical-continuity-record", (string?)record["recordType"]);
        Assert.Equal("none", (string?)record["authority"]);
        Assert.Equal("mechanical-reemission-time-not-approval-or-review-time", (string?)record["timestampBasis"]);
        var bindings = record["historicalBindings"]!.AsArray().Concat(record["successorBindings"]!.AsArray()).ToArray();
        string[] expectedPaths = [
            "research/source-authorization/recommended-seven-source-decisions.v1.json",
            "backend/src/BioStack.KnowledgeWorker/Schemas/source-authorization-decision.schema.json",
            "research/source-authorization/keo-74-reviewer-owner-transfer-receipt.v1.json",
            "research/source-authorization/keo-74-nccih-manual-capture-reviewer-receipt.v1.json",
            "research/source-authorization/owner-source-decisions-2026-09-08.v1.json",
            "research/source-authorization/snapshots/pilot-source-registry.2026-07-25.json",
            "research/source-authorization/recommended-seven-source-decisions.v2.json",
            "backend/src/BioStack.KnowledgeWorker/Schemas/source-authorization-decision.v2.schema.json",
            "research/input/sources/pilot-source-registry.json",
        ];
        Assert.Equal(expectedPaths.Order(), bindings.Select(b => (string)b!["artifactPath"]!).Order());
        foreach (var binding in bindings)
        {
            Assert.Equal((string?)binding!["sha256"], HashFile((string)binding["artifactPath"]!));
        }
        Assert.Equal("a5ad6ef136b3b7628416ea4fc516548dbf3d3fb1f0a77ff74a60e74d54968ea2",
            HashFile(expectedPaths[0]));
        Assert.Equal("a9b094fc8d6ac87916cf7858124a16f8d00562ba338365d93418d0f211edf4db",
            HashFile(expectedPaths[1]));

        var historical = HistoricalDecision();
        var successor = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, expectedPaths[6])))!;
        var ownerReceipt = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, expectedPaths[4])))!;
        var a1 = ownerReceipt["decisions"]!.AsArray().Single(d => (string?)d?["decisionId"] == "A1")!;
        Assert.True(JsonNode.DeepEquals(a1["effectiveOwners"], successor["owners"]));
        Assert.True(JsonNode.DeepEquals(historical["sources"], successor["sources"]));
        Assert.Equal("2.0.0", (string?)successor["schemaVersion"]);
        Assert.Equal(HashFile(expectedPaths[8]), (string?)successor["registryBinding"]?["sha256"]);
        var validation = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory())
            .Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor);
        Assert.True(validation.IsValid, validation.Summary());
        SourceAuthorizationScopeGuard.RequireMatchingActiveSources(successor,
            JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot, expectedPaths[8])))!);

        // Restore the four permitted re-emission fields and require the whole document to match.
        var restored = successor.DeepClone();
        foreach (var key in new[] { "schemaVersion", "generatedAt", "owners" })
            restored[key] = historical[key]!.DeepClone();
        restored["registryBinding"]!["sha256"] = historical["registryBinding"]!["sha256"]!.DeepClone();
        Assert.True(JsonNode.DeepEquals(historical, restored), "Successor changes more than the documented mechanical fields.");
        Assert.Equal("pending", (string?)record["invariants"]?["nccihReviewerActionStatus"]);
        Assert.Equal("review-required", (string?)record["invariants"]?["evidenceApprovalStatus"]);
        foreach (var key in new[] { "assignmentIsApproval", "newApprovalGranted", "newRuntimeAuthorizationGranted" })
            Assert.False((bool)record["invariants"]![key]!);
        Assert.False((bool)record["enforcement"]!["recordIsRuntimeAuthorizationInput"]!);
    }

    [Theory]
    [InlineData("drugbank", true)]
    [InlineData("fda", false)]
    public void Active_Source_Set_Drift_Is_Rejected_Even_With_A_Matching_Registry_Hash(string sourceId, bool enabled)
    {
        var registry = JsonNode.Parse(File.ReadAllText(Path.Combine(RepositoryRoot,
            "research/input/sources/pilot-source-registry.json")))!;
        var source = registry["sources"]!.AsArray().Single(s => (string?)s?["identity"]?["sourceId"] == sourceId)!;
        source["rights"]!["reviewStatus"] = enabled ? "approved" : "pending-human-legal";
        source["operations"]!["status"] = enabled ? "active" : "disabled";
        source["acquisition"]!["enabled"] = enabled;
        var successor = SuccessorForSchemaReview();
        successor["registryBinding"]!["sha256"] = Convert.ToHexString(SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(registry.ToJsonString()))).ToLowerInvariant();
        Assert.Throws<InvalidOperationException>(() =>
            SourceAuthorizationScopeGuard.RequireMatchingActiveSources(successor, registry));
    }

    [Fact]
    public void Historical_Decision_Still_Binds_The_Exact_Archived_Registry()
    {
        var bytes = File.ReadAllBytes(Path.Combine(RepositoryRoot,
            "research/source-authorization/snapshots/pilot-source-registry.2026-07-25.json"));
        var historical = HistoricalDecision();
        Assert.Equal(historical["registryBinding"]!["sha256"]!.GetValue<string>(),
            Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant());
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());
        Assert.True(validator.Validate(ResearchArtifactKind.SourceRegistry, JsonNode.Parse(bytes)!).IsValid);
        Assert.True(validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, historical).IsValid);
    }

    [Fact]
    public void Historical_And_Successor_Batches_Use_Separate_Bundled_Schemas()
    {
        var validator = ResearchArtifactValidator.LoadFromDirectory(
            Path.Combine(AppContext.BaseDirectory, "Schemas"));
        var historical = HistoricalDecision();
        Assert.True(validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, historical).IsValid);

        var successor = SuccessorForSchemaReview();
        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor);
        Assert.True(result.IsValid, result.Summary());

        // A changed registry binding must not be smuggled through the issued version.
        successor["schemaVersion"] = "1.2.0";
        Assert.False(validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor).IsValid);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("1.0.0")]
    [InlineData("3.0.0")]
    [InlineData("../../source-registry.schema.json")]
    public void Missing_Or_Unsupported_Version_Is_Rejected(string? version)
    {
        var successor = SuccessorForSchemaReview();
        successor["schemaVersion"] = version;
        var result = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory())
            .Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor);
        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, error => error.Location == "/schemaVersion");
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-hash")]
    [InlineData("FDBC1A57702A6942776820266B54AEB1BBD4B5F4957C9D8E789AB1F99D28F97E")]
    public void Successor_Rejects_Malformed_Registry_Binding(string hash)
    {
        var successor = SuccessorForSchemaReview();
        successor["registryBinding"]!["sha256"] = hash;
        Assert.False(ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory())
            .Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor).IsValid);
    }

    [Fact]
    public void Successor_Does_Not_Add_DrugBank_To_The_Historical_Seven_Source_Scope()
    {
        var successor = SuccessorForSchemaReview();
        successor["sources"]![0]!["sourceId"] = "drugbank";
        Assert.False(ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory())
            .Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, successor).IsValid);
    }

    [Fact]
    public void Schema_Valid_Successor_Still_Fails_Planning_When_One_Registry_Byte_Changes()
    {
        var successor = SuccessorForSchemaReview();
        var bytes = File.ReadAllBytes(Path.Combine(RepositoryRoot, "research/input/sources/pilot-source-registry.json"));
        var changedBytes = bytes.Concat(new byte[] { (byte)' ' }).ToArray();
        var registry = JsonNode.Parse(changedBytes)!;
        var requests = JsonNode.Parse(File.ReadAllText(Path.Combine(
            RepositoryRoot, "research/research-requests/market-interest-coverage-2026-07-24.v1.json")))!;
        var plan = new SourceAcquisitionPlanBuilder().Build(
            requests, successor, registry,
            Convert.ToHexString(SHA256.HashData(changedBytes)).ToLowerInvariant(),
            RecommendedOfficialSourcePlanningAdapters.All);
        Assert.Equal(0, plan.ReadyCount);
        Assert.All(plan.Intents, intent => Assert.Contains(
            $"{intent.SourceId}:source-registry-sha256-mismatch", intent.BlockingReasons));
    }

    private static string RepositoryRoot => Directory.GetParent(TestPaths.BackendRoot())!.FullName;

    private static string HashFile(string relativePath) => Convert.ToHexString(
        SHA256.HashData(File.ReadAllBytes(Path.Combine(RepositoryRoot, relativePath)))).ToLowerInvariant();

    private static JsonNode HistoricalDecision() => JsonNode.Parse(File.ReadAllText(Path.Combine(
        RepositoryRoot, "research/source-authorization/recommended-seven-source-decisions.v1.json")))!;

    // Synthetic compatibility input, not an issued decision or a source activation.
    private static JsonNode SuccessorForSchemaReview()
    {
        var node = HistoricalDecision();
        node["schemaVersion"] = "2.0.0";
        node["registryBinding"]!["sha256"] = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(
            Path.Combine(RepositoryRoot, "research/input/sources/pilot-source-registry.json")))).ToLowerInvariant();
        return node;
    }
}
