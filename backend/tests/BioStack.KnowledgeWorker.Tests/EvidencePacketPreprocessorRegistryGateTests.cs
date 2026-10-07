namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// A packet's own authorityTier is an assertion, not an authorization. When a source registry is supplied,
/// the registry's tier governs and an unregistered source confers no authority however the packet tiers it.
/// Passing no registry preserves the historical self-asserted behaviour.
/// </summary>
public class EvidencePacketPreprocessorRegistryGateTests
{
    private static JsonNode Packet(string claimType, bool fieldAuthorityRequired,
        params (string id, string tier)[] sources)
    {
        var srcs = new JsonArray();
        foreach (var (id, tier) in sources)
        {
            srcs.Add(new JsonObject { ["sourceId"] = id, ["authorityTier"] = tier });
        }
        var refs = new JsonArray();
        foreach (var (id, _) in sources) refs.Add(id);

        return new JsonObject
        {
            ["schemaVersion"] = "1.0.0",
            ["recordType"] = "compound-evidence-packet",
            ["sources"] = srcs,
            ["claims"] = new JsonArray
            {
                new JsonObject
                {
                    ["claimId"] = "c-1",
                    ["claimType"] = claimType,
                    ["fieldAuthorityRequired"] = fieldAuthorityRequired,
                    ["sourceRefs"] = refs,
                },
            },
            ["ops"] = new JsonObject(),
        };
    }

    private static JsonNode Registry(params (string id, string tier)[] entries)
    {
        var sources = new JsonArray();
        foreach (var (id, tier) in entries)
        {
            sources.Add(new JsonObject
            {
                ["identity"] = new JsonObject { ["sourceId"] = id, ["aliases"] = new JsonArray() },
                ["evidencePolicy"] = new JsonObject { ["authorityTier"] = tier },
            });
        }
        return new JsonObject { ["sources"] = sources };
    }

    [Fact]
    public void Without_Registry_Packet_Self_Assertion_Still_Satisfies_The_Gate()
    {
        // Historical behaviour, preserved so existing callers are unaffected.
        var result = new EvidencePacketPreprocessor()
            .Preprocess(Packet("regulatory", true, ("some-source", "A1")));

        Assert.DoesNotContain("missing-authoritative-support", result.QualityFlags);
    }

    [Fact]
    public void With_Registry_An_Unregistered_Source_Confers_No_Authority()
    {
        var result = new EvidencePacketPreprocessor().Preprocess(
            Packet("regulatory", true, ("not-registered", "A1")),
            Registry(("fda", "A1")));

        Assert.Contains("missing-authoritative-support", result.QualityFlags);
        Assert.Contains("self-asserted-authority-unregistered", result.QualityFlags);
    }

    [Fact]
    public void With_Registry_A_Registered_Authoritative_Source_Satisfies_The_Gate()
    {
        var result = new EvidencePacketPreprocessor().Preprocess(
            Packet("regulatory", true, ("fda", "A1")),
            Registry(("fda", "A1")));

        Assert.DoesNotContain("missing-authoritative-support", result.QualityFlags);
        Assert.DoesNotContain("self-asserted-authority-unregistered", result.QualityFlags);
    }

    [Fact]
    public void Registry_Tier_Overrides_A_Stronger_Packet_Assertion()
    {
        // The packet claims A2; the registry says C2. The registry governs, so the gate is not satisfied.
        var result = new EvidencePacketPreprocessor().Preprocess(
            Packet("dose-context", true, ("nih-ods", "A2")),
            Registry(("nih-ods", "C2")));

        Assert.Contains("missing-authoritative-support", result.QualityFlags);
        // It is registered, so this is a downgrade rather than an unregistered assertion.
        Assert.DoesNotContain("self-asserted-authority-unregistered", result.QualityFlags);
    }

    [Fact]
    public void Alias_Resolution_Counts_As_Registered()
    {
        var registry = new JsonObject
        {
            ["sources"] = new JsonArray
            {
                new JsonObject
                {
                    ["identity"] = new JsonObject
                    {
                        ["sourceId"] = "dailymed",
                        ["aliases"] = new JsonArray("dailymed-evista-label"),
                    },
                    ["evidencePolicy"] = new JsonObject { ["authorityTier"] = "A1" },
                },
            },
        };

        var result = new EvidencePacketPreprocessor().Preprocess(
            Packet("approved-indication", true, ("dailymed-evista-label", "A1")), registry);

        Assert.DoesNotContain("missing-authoritative-support", result.QualityFlags);
    }

    [Fact]
    public void Claims_Not_Requiring_Authority_Are_Unaffected()
    {
        var result = new EvidencePacketPreprocessor().Preprocess(
            Packet("mechanism", false, ("not-registered", "B1")),
            Registry(("fda", "A1")));

        Assert.DoesNotContain("missing-authoritative-support", result.QualityFlags);
        // B1 is not an authoritative tier, so there is nothing self-asserted to flag either.
        Assert.DoesNotContain("self-asserted-authority-unregistered", result.QualityFlags);
    }
}
