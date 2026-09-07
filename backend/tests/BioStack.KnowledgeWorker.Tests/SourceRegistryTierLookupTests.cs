namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Registry schema v2 nests the registry id under <c>identity</c> and the tier under
/// <c>evidencePolicy</c>. These tests pin that shape: before the fix, lookups read
/// <c>sourceId</c>/<c>authorityTier</c> from the top level of a registry entry, which the schema does not
/// permit, so every registry fallback silently returned null.
/// </summary>
public class SourceRegistryTierLookupTests
{
    private static JsonNode Registry(params JsonObject[] entries)
    {
        var sources = new JsonArray();
        foreach (var e in entries) sources.Add(e);
        return new JsonObject { ["sources"] = sources };
    }

    private static JsonObject Entry(string sourceId, string tier, params string[] aliases)
    {
        var aliasArray = new JsonArray();
        foreach (var a in aliases) aliasArray.Add(a);
        return new JsonObject
        {
            ["identity"] = new JsonObject
            {
                ["sourceId"] = sourceId,
                ["aliases"] = aliasArray,
            },
            ["evidencePolicy"] = new JsonObject
            {
                ["authorityTier"] = tier,
            },
        };
    }

    [Fact]
    public void Resolves_Tier_By_Registry_SourceId()
    {
        var registry = Registry(Entry("nih-ods", "C2"));

        Assert.Equal("C2", SourceRegistryTierLookup.LookupAuthorityTier("nih-ods", registry));
    }

    [Fact]
    public void Resolves_Tier_By_Registered_Alias()
    {
        var registry = Registry(Entry("nih-ods", "C2", "nih-ods-vitamin-d-hp", "nih-ods-magnesium-hp"));

        Assert.Equal("C2", SourceRegistryTierLookup.LookupAuthorityTier("nih-ods-vitamin-d-hp", registry));
    }

    [Fact]
    public void Match_Is_Case_Insensitive()
    {
        var registry = Registry(Entry("DailyMed", "A1", "dailymed-ozempic"));

        Assert.Equal("A1", SourceRegistryTierLookup.LookupAuthorityTier("dailymed", registry));
        Assert.Equal("A1", SourceRegistryTierLookup.LookupAuthorityTier("DAILYMED-OZEMPIC", registry));
    }

    [Fact]
    public void Unregistered_Reference_Returns_Null()
    {
        var registry = Registry(Entry("fda", "A1", "fda-sarms-warning-consumer"));

        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda-grn-931-creatine-monohydrate", registry));
    }

    [Fact]
    public void Top_Level_Shape_Is_Not_Accepted()
    {
        // A registry entry that exposes sourceId/authorityTier at the top level is not schema-valid.
        // It must not resolve, or the lookup would accept an unauthorized shape.
        var registry = Registry(new JsonObject
        {
            ["sourceId"] = "rogue",
            ["authorityTier"] = "A1",
        });

        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("rogue", registry));
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Blank_Reference_Returns_Null(string? sourceRef)
    {
        var registry = Registry(Entry("fda", "A1"));

        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier(sourceRef, registry));
    }

    [Fact]
    public void Null_Or_Malformed_Registry_Returns_Null()
    {
        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda", null));
        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda", new JsonObject()));
        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier(
            "fda", new JsonObject { ["sources"] = "not-an-array" }));
    }

    [Fact]
    public void Entry_Without_EvidencePolicy_Returns_Null_Rather_Than_Throwing()
    {
        var registry = Registry(new JsonObject
        {
            ["identity"] = new JsonObject { ["sourceId"] = "fda", ["aliases"] = new JsonArray() },
        });

        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda", registry));
    }

    [Fact]
    public void Non_String_Tier_Returns_Null_Rather_Than_Throwing()
    {
        var registry = Registry(new JsonObject
        {
            ["identity"] = new JsonObject { ["sourceId"] = "fda", ["aliases"] = new JsonArray() },
            ["evidencePolicy"] = new JsonObject { ["authorityTier"] = 1 },
        });

        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda", registry));
    }

    [Fact]
    public void Resolves_Against_The_Real_Pilot_Registry()
    {
        var path = Path.Combine(
            TestPaths.RepositoryRoot(), "research", "input", "sources", "pilot-source-registry.json");
        Assert.True(File.Exists(path), $"pilot registry not found at {path}");
        var registry = JsonNode.Parse(File.ReadAllText(path));

        // Class id and a registered alias both resolve to the registry's declared tier, not to any
        // tier a packet might assert for the same source.
        Assert.Equal("C2", SourceRegistryTierLookup.LookupAuthorityTier("nih-ods", registry));
        Assert.Equal("C2", SourceRegistryTierLookup.LookupAuthorityTier("nih-ods-vitamin-d-hp", registry));
        Assert.Equal("A1", SourceRegistryTierLookup.LookupAuthorityTier("dailymed", registry));

        // An unregistered per-item id does not inherit authority from its naming prefix.
        Assert.Null(SourceRegistryTierLookup.LookupAuthorityTier("fda-grn-931-creatine-monohydrate", registry));
    }
}
