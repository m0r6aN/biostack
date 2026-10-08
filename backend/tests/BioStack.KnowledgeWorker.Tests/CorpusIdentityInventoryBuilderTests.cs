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
        Assert.Equal(100, snapshot.SeedRecordCount);
        Assert.Equal(16, snapshot.CandidateRecordCount);
        Assert.Equal(78, snapshot.EvidencePacketCount);
        Assert.Equal(30, snapshot.SourceRegistryRecordCount);
        Assert.Equal(16, snapshot.SeedCandidateOverlapCount);
        Assert.Equal(84, snapshot.SeedOnlyCanonicalIds.Count);
        Assert.Equal(0, snapshot.CandidateOnlyCanonicalIds.Count);
        Assert.Empty(snapshot.CandidatesMissingEvidenceCanonicalIds);
        Assert.Equal(62, snapshot.EvidenceWithoutCandidateCanonicalIds.Count);
        Assert.Equal(7, snapshot.ApprovedRightsSourceCount);
        Assert.Equal(7, snapshot.ActiveOperationsSourceCount);
        Assert.Equal(7, snapshot.AcquisitionEnabledSourceCount);
        Assert.Equal(2, snapshot.RegistryAuthorizedEvidencePacketCount);
        // BIO-LOCAL-011: 100-record corpus (57 existing + 43 batched) observed 3 identity-token
        // collisions (was 2 at 57 records); each key's owners are now itemized explicitly
        // (previously 2 keys shared one owners list, which no longer holds at 100 records).
        Assert.Equal(3, snapshot.IdentityTokenCollisions.Count);
        Assert.Equal(
            ["chorionic-gonadotropin", "creatine", "creatine-monohydrate"],
            snapshot.IdentityTokenCollisions.Select(collision => collision.Key));
        Assert.Equal(
            ["seed:chorionic-gonadotropin", "seed:human-chorionic-gonadotropin"],
            snapshot.IdentityTokenCollisions[0].Owners);
        Assert.Equal(
            ["candidate:creatine", "seed:creatine", "seed:creatine-monohydrate"],
            snapshot.IdentityTokenCollisions[1].Owners);
        Assert.Equal(
            ["candidate:creatine", "seed:creatine-monohydrate"],
            snapshot.IdentityTokenCollisions[2].Owners);
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
