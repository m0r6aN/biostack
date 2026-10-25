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
        // Go-live library 2026-10-14 expanded the seed corpus from the BIO-LOCAL-014 count of 99 to 158;
        // the 2026-10-25 consolidation folded testolone into vosilasarm and creatine-monohydrate into creatine (156).
        Assert.Equal(156, snapshot.SeedRecordCount);
        Assert.Equal(16, snapshot.CandidateRecordCount);
        Assert.Equal(78, snapshot.EvidencePacketCount);
        Assert.Equal(30, snapshot.SourceRegistryRecordCount);
        Assert.Equal(16, snapshot.SeedCandidateOverlapCount);
        // The 59 records added by the 2026-10-14 library are seed-only: 83 + 59 = 142, less the two
        // seed-only records folded in by the 2026-10-25 consolidation = 140.
        Assert.Equal(140, snapshot.SeedOnlyCanonicalIds.Count);
        Assert.Equal(0, snapshot.CandidateOnlyCanonicalIds.Count);
        Assert.Empty(snapshot.CandidatesMissingEvidenceCanonicalIds);
        Assert.Equal(62, snapshot.EvidenceWithoutCandidateCanonicalIds.Count);
        Assert.Equal(7, snapshot.ApprovedRightsSourceCount);
        Assert.Equal(7, snapshot.ActiveOperationsSourceCount);
        Assert.Equal(7, snapshot.AcquisitionEnabledSourceCount);
        Assert.Equal(2, snapshot.RegistryAuthorizedEvidencePacketCount);
        // The 2026-10-25 consolidation folded testolone into vosilasarm and creatine-monohydrate
        // into creatine, so their token collisions are gone; only the magnesium-oxide overlap remains.
        var collision = Assert.Single(snapshot.IdentityTokenCollisions);
        Assert.Equal("magnesium-oxide", collision.Key);
        Assert.Equal(["candidate:magnesium", "seed:magnesium", "seed:magnesium-oxide"], collision.Owners);
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
