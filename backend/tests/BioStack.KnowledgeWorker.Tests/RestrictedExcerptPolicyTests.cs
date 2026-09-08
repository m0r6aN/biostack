namespace BioStack.KnowledgeWorker.Tests;

using System.Linq;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Containment tests for owner decision C1 (DrugBank).
///
/// None of these asserts that any source is authorized. Withholding text grants nothing, and a passing
/// test here is not evidence that a lane may be used. These prove one path — the emitted evidence-packet
/// artifact. Read-time and provider-boundary containment are separate requirements with their own tests;
/// this file cannot and does not cover artifacts already written or client-supplied packets.
/// </summary>
public sealed class RestrictedExcerptPolicyTests
{
    private const string ContainedQuote = "CONTAINED EXCERPT TEXT THAT MUST NOT ESCAPE";
    private const string PermittedQuote = "permitted excerpt text";

    [Fact]
    public void Matches_the_cross_application_containment_contract()
    {
        var fixture = JsonNode.Parse(File.ReadAllText(Path.Combine(TestPaths.RepositoryRoot(),
            "shared", "source-rights", "drugbank-containment.conformance.v1.json")))!;
        var packet = fixture["packet"]!;
        var before = packet.ToJsonString();
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(packet, sourceRegistry: null);
        var quotes = new JsonArray(result["claims"]!.AsArray()[0]!["extractedEvidence"]!.AsArray()
            .Select(item => item!["quote"]?.DeepClone()).ToArray());
        Assert.Equal(fixture["expectedQuotes"]!.ToJsonString(), quotes.ToJsonString());
        Assert.Equal(before, packet.ToJsonString());
    }

    /// <summary>A registry whose DrugBank entry declares content permitted — must not release anything.</summary>
    private static JsonNode PermissiveRegistry() =>
        JsonNode.Parse("""
        {
          "sources": [
            {
              "identity": {
                "sourceId": "drugbank",
                "aliases": ["drugbank-db11653", "basaria-2013-jgerontol-rct", "drugbank-registry-only-alias"],
                "primaryUrl": "https://go.drugbank.com/"
              },
              "dataBoundary": {
                "permittedContent": ["identity-fields", "mechanism-fields"],
                "restrictedContent": []
              }
            },
            {
              "identity": {
                "sourceId": "dailymed",
                "aliases": ["dailymed-vyleesi-label"],
                "primaryUrl": "https://dailymed.nlm.nih.gov/dailymed/"
              },
              "dataBoundary": {
                "permittedContent": ["source-cited factual fields"],
                "restrictedContent": []
              }
            }
          ]
        }
        """)!;

    private static JsonNode Packet() =>
        JsonNode.Parse($$"""
        {
          "schemaVersion": "1.0.0",
          "recordType": "evidence-packet",
          "compound": { "canonicalName": "Ligandrol" },
          "sources": [
            { "sourceId": "drugbank-db11653", "url": "https://go.drugbank.com/drugs/DB11653" },
            { "sourceId": "basaria-2013-jgerontol-rct", "url": "https://go.drugbank.com/articles/A31488" },
            { "sourceId": "dailymed-vyleesi-label", "url": "https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=x" }
          ],
          "claims": [
            {
              "claimId": "c1",
              "claimType": "mechanism",
              "sourceRefs": ["drugbank-db11653", "dailymed-vyleesi-label"],
              "extractedEvidence": [
                { "sourceRef": "drugbank-db11653", "quote": "{{ContainedQuote}}", "pageOrSection": "Pharmacology" },
                { "sourceRef": "dailymed-vyleesi-label", "quote": "{{PermittedQuote}}", "pageOrSection": "Indications" }
              ]
            },
            {
              "claimId": "c2",
              "claimType": "efficacy",
              "sourceRefs": ["basaria-2013-jgerontol-rct"],
              "extractedEvidence": [
                { "sourceRef": "basaria-2013-jgerontol-rct", "quote": "{{ContainedQuote}}", "pageOrSection": "Results" }
              ]
            }
          ],
          "ops": { "qualityFlags": ["pre-existing-review-flag"] }
        }
        """)!;

    // ---------------------------------------------------------------------
    // fail-closed: the registry must never be able to release a contained excerpt
    // ---------------------------------------------------------------------

    [Fact]
    public void Contains_excerpts_with_no_registry_at_all()
    {
        // The scope comes from the embedded manifest, not the optional registry file. A research run
        // configured without a registry must still contain.
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);

        Assert.DoesNotContain(ContainedQuote, result.ToJsonString());
    }

    [Fact]
    public void A_permissive_registry_cannot_release_contained_excerpts()
    {
        // Replaces an earlier test that asserted the opposite. A partial permitted-content declaration
        // is not a reviewed C1 release, and identity permission must not release narrative text.
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), PermissiveRegistry());

        Assert.DoesNotContain(ContainedQuote, result.ToJsonString());
    }

    [Fact]
    public void Registry_can_only_widen_containment_never_narrow_it()
    {
        var packet = Packet();
        packet["sources"]!.AsArray().Add(JsonNode.Parse(
            """{ "sourceId": "drugbank-registry-only-alias", "url": "https://example.org/x" }""")!);
        packet["claims"]!.AsArray().Add(JsonNode.Parse($$"""
        {
          "claimId": "c3",
          "claimType": "identity",
          "sourceRefs": ["drugbank-registry-only-alias"],
          "extractedEvidence": [
            { "sourceRef": "drugbank-registry-only-alias", "quote": "{{ContainedQuote}}", "pageOrSection": "X" }
          ]
        }
        """)!);

        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(packet, PermissiveRegistry());

        Assert.DoesNotContain(ContainedQuote, result.ToJsonString());
    }

    // ---------------------------------------------------------------------
    // matching
    // ---------------------------------------------------------------------

    [Fact]
    public void Contains_by_manifest_source_id()
    {
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);
        var evidence = result["claims"]!.AsArray()[0]!["extractedEvidence"]!.AsArray()[0]!;

        Assert.Null((string?)evidence["quote"]);
    }

    [Fact]
    public void Contains_the_aggregator_routed_record_whose_rightsholder_differs()
    {
        // basaria-2013-jgerontol-rct is DrugBank's index page for a Journals of Gerontology paper. The
        // route is contained; the underlying literature is not deleted and keeps its own rightsholder.
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);
        var evidence = result["claims"]!.AsArray()[1]!["extractedEvidence"]!.AsArray()[0]!;

        Assert.Null((string?)evidence["quote"]);
    }

    [Theory]
    [InlineData("https://drugbank.com/x", true)]           // exact host
    [InlineData("https://go.drugbank.com/x", true)]        // subdomain
    [InlineData("https://GO.DrugBank.COM/x", true)]        // case-insensitive
    [InlineData("https://notdrugbank.com/x", false)]       // substring match would wrongly contain this
    [InlineData("https://drugbank.com.example.org/x", false)] // lookalike suffix
    [InlineData("https://example.org/drugbank.com", false)]   // path, not host
    public void Host_matching_is_by_label_not_substring(string url, bool shouldContain)
    {
        var packet = JsonNode.Parse($$"""
        {
          "schemaVersion": "1.0.0", "recordType": "evidence-packet",
          "compound": { "canonicalName": "Ligandrol" },
          "sources": [ { "sourceId": "host-probe", "url": "{{url}}" } ],
          "claims": [ {
            "claimId": "c1", "claimType": "identity", "sourceRefs": ["host-probe"],
            "extractedEvidence": [ { "sourceRef": "host-probe", "quote": "{{ContainedQuote}}", "pageOrSection": null } ]
          } ],
          "ops": { "qualityFlags": [] }
        }
        """)!;

        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(packet, sourceRegistry: null);

        Assert.Equal(shouldContain, !result.ToJsonString().Contains(ContainedQuote));
    }

    // ---------------------------------------------------------------------
    // preservation
    // ---------------------------------------------------------------------

    [Fact]
    public void Preserves_the_citation_and_locator_it_withholds()
    {
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);
        var evidence = result["claims"]!.AsArray()[0]!["extractedEvidence"]!.AsArray()[0]!;

        Assert.Equal("drugbank-db11653", (string?)evidence["sourceRef"]);
        Assert.Equal("Pharmacology", (string?)evidence["pageOrSection"]);
    }

    [Fact]
    public void Leaves_sources_outside_the_contained_scope_untouched()
    {
        // C1 is DrugBank. This policy must not reach the other five lanes an earlier marker-driven
        // revision silently included, including the two retired authorization placeholders.
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);

        Assert.Contains(PermittedQuote, result.ToJsonString());
    }

    [Fact]
    public void Never_mutates_the_input_packet()
    {
        var packet = Packet();
        RestrictedExcerptPolicy.WithholdRestrictedExcerpts(packet, sourceRegistry: null);

        Assert.Contains(ContainedQuote, packet.ToJsonString());
    }

    [Fact]
    public void Appends_its_flag_without_dropping_existing_review_flags()
    {
        var result = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(Packet(), sourceRegistry: null);
        var flags = result["ops"]!["qualityFlags"]!.AsArray().Select(f => (string?)f).ToList();

        Assert.Contains(RestrictedExcerptPolicy.WithheldFlag, flags);
        Assert.Contains("pre-existing-review-flag", flags);
    }

    [Fact]
    public void Withholding_grants_no_authorization()
    {
        var before = Packet();
        var after = RestrictedExcerptPolicy.WithholdRestrictedExcerpts(before, sourceRegistry: null);

        Assert.Equal(before["claims"]!.AsArray().Count, after["claims"]!.AsArray().Count);
        foreach (var (original, emitted) in before["claims"]!.AsArray().Zip(after["claims"]!.AsArray()))
        {
            Assert.Equal((string?)original!["claimType"], (string?)emitted!["claimType"]);
            Assert.Equal(original["sourceRefs"]!.ToJsonString(), emitted["sourceRefs"]!.ToJsonString());
        }
    }

    [Fact]
    public void Binds_the_owner_decision_policy_id()
    {
        Assert.Equal("drugbank-excerpt-containment-20260908", RestrictedExcerptPolicy.PolicyId);
    }
}
