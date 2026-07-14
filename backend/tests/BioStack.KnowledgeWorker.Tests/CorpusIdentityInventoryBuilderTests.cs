namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class CorpusIdentityInventoryBuilderTests
{
    [Fact]
    public void Build_CurrentRepository_RecordsIdentityCoverageWithoutPromotionAuthority()
    {
        var snapshot = new CorpusIdentityInventoryBuilder().Build();

        Assert.Equal("1.0.0", snapshot.SnapshotVersion);
        Assert.Equal("repository-identity-and-provenance-metadata-only", snapshot.Scope);
        Assert.Equal(49, snapshot.SeedRecordCount);
        Assert.Equal(16, snapshot.CandidateRecordCount);
        Assert.Equal(16, snapshot.EvidencePacketCount);
        Assert.Equal(13, snapshot.SourceRegistryRecordCount);
        Assert.Equal(10, snapshot.SeedCandidateOverlapCount);
        Assert.Equal(39, snapshot.SeedOnlyCanonicalIds.Count);
        Assert.Equal(6, snapshot.CandidateOnlyCanonicalIds.Count);
        Assert.Empty(snapshot.CandidatesMissingEvidenceCanonicalIds);
        Assert.Empty(snapshot.EvidenceWithoutCandidateCanonicalIds);
        Assert.Equal(0, snapshot.ApprovedRightsSourceCount);
        Assert.Equal(0, snapshot.ActiveOperationsSourceCount);
        Assert.Equal(0, snapshot.AcquisitionEnabledSourceCount);
        Assert.Equal(0, snapshot.RegistryAuthorizedEvidencePacketCount);
        Assert.Equal(2, snapshot.IdentityTokenCollisions.Count);
        Assert.Equal(
            ["creatine", "creatine-monohydrate"],
            snapshot.IdentityTokenCollisions.Select(collision => collision.Key));
        Assert.All(
            snapshot.IdentityTokenCollisions,
            collision => Assert.Equal(
                ["candidate:creatine", "seed:creatine-monohydrate"],
                collision.Owners));
        Assert.Empty(snapshot.ExternalIdentifierCollisions);
        Assert.False(snapshot.ModelInvoked);
        Assert.False(snapshot.NetworkAccessed);
    }

    [Fact]
    public void Build_CurrentRepository_ProducesStableSortedInventory()
    {
        var builder = new CorpusIdentityInventoryBuilder();

        var first = builder.BuildJson();
        var second = builder.BuildJson();
        var snapshot = builder.Build();

        Assert.Equal(first, second);
        Assert.Equal(
            snapshot.SeedOnlyCanonicalIds.Order(StringComparer.Ordinal),
            snapshot.SeedOnlyCanonicalIds);
        Assert.Equal(
            snapshot.CandidateOnlyCanonicalIds.Order(StringComparer.Ordinal),
            snapshot.CandidateOnlyCanonicalIds);
        Assert.Equal(
            snapshot.IdentityTokenCollisions.OrderBy(item => item.Key, StringComparer.Ordinal),
            snapshot.IdentityTokenCollisions);
        Assert.Equal(
            snapshot.ExternalIdentifierCollisions.OrderBy(item => item.Key, StringComparer.Ordinal),
            snapshot.ExternalIdentifierCollisions);
    }

    [Fact]
    public void BuildJson_CurrentRepository_OmitsClaimsAndRuntimeAssertions()
    {
        var json = new CorpusIdentityInventoryBuilder().BuildJson();
        var root = JsonNode.Parse(json)!.AsObject();

        Assert.Null(root["generatedAtUtc"]);
        Assert.False((bool)root["modelInvoked"]!);
        Assert.False((bool)root["networkAccessed"]!);
        Assert.DoesNotContain("\"claims\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"statement\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"dosingGuidance\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("\"sourceUrl\"", json, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("not establish taxonomy coverage targets", json, StringComparison.Ordinal);
    }
}
