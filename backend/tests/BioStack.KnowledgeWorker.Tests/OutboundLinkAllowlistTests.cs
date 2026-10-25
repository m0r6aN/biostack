namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>
/// Safety 06 §B blocker: every outbound link BioStack publishes passes a source-domain
/// allowlist. Runs over the full projected seed, so a curator adding a vendor, clinic,
/// forum or news link fails CI instead of shipping.
/// </summary>
public sealed class OutboundLinkAllowlistTests
{
    [Theory]
    [InlineData("https://pubmed.ncbi.nlm.nih.gov/18981485/")]
    [InlineData("https://pmc.ncbi.nlm.nih.gov/articles/PMC123/")]
    [InlineData("https://doi.org/10.1056/NEJMoa2301972")]
    [InlineData("https://clinicaltrials.gov/study/NCT04881760")]
    [InlineData("https://www.fda.gov/media/182087/download")]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=abc")]
    [InlineData("https://www.ema.europa.eu/en/medicines/x")]
    [InlineData("https://www.who.int/publications/x")]
    [InlineData("https://www.wada-ama.org/en/prohibited-list")]
    [InlineData("https://www.nejm.org/doi/full/10.1056/NEJMoa2301972")]
    public void AllowlistedHosts_Pass(string url) => Assert.True(PublicSourcePolicy.IsAllowlistedHost(url));

    [Theory]
    [InlineData("https://www.amazon.com/s?k=bpc-157")]
    [InlineData("https://www.caymanchem.com/product/1")]
    [InlineData("https://www.peptidedeck.com/x")]
    [InlineData("https://www.reddit.com/r/Peptides/x")]
    [InlineData("https://en.wikipedia.org/wiki/BPC-157")]
    [InlineData("https://notfda.gov.evil.example/x")]
    [InlineData("https://fda.gov.example.com/x")]
    [InlineData("javascript:alert(1)")]
    [InlineData("ftp://pubmed.ncbi.nlm.nih.gov/x")]
    [InlineData("")]
    public void OtherHosts_Fail(string url) => Assert.False(PublicSourcePolicy.IsAllowlistedHost(url));

    [Fact]
    public void EveryProjectedOutboundLink_InTheSeed_IsAllowlisted()
    {
        var canonicalizer = new SubstanceCanonicalizer();
        var offenders = new List<string>();
        var path = Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");
        foreach (var record in JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(path))!)
        {
            var entry = canonicalizer.ToKnowledgeEntry(record);
            var d = entry.Details;
            var urls = entry.SourceReferences
                .Concat(d?.Sources.Select(s => s.Url) ?? [])
                .Concat(d?.DoseReports.Select(r => r.SourceUrl) ?? [])
                .Concat(d?.PublicClaims.SelectMany(c => c.Sources).Select(s => s.Url) ?? [])
                .Where(u => u.StartsWith("http", StringComparison.OrdinalIgnoreCase));
            offenders.AddRange(urls.Where(u => !PublicSourcePolicy.IsAllowlistedHost(u))
                .Select(u => $"{record.Identity.CanonicalName}: {u}"));
        }
        Assert.True(offenders.Count == 0, string.Join(Environment.NewLine, offenders));
    }
}
