namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim verdict rebuild, batch 13 (herbals and other supplements, part 2), 2026-10-25.</summary>
public sealed class ClaimBatch13Tests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly Regex DoseText = new(@"\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units?|ml|nmol)\b", RegexOptions.IgnoreCase);

    [Theory]
    [InlineData("Ginseng", 2)]
    [InlineData("Bacopa", 1)]
    [InlineData("Rhodiola", 1)]
    [InlineData("Lion's mane", 1)]
    [InlineData("Passionflower", 1)]
    [InlineData("SAMe", 1)]
    [InlineData("Glucosamine", 2)]
    [InlineData("Chondroitin", 2)]
    [InlineData("MSM", 1)]
    [InlineData("Inositol", 1)]
    [InlineData("Collagen peptides", 2)]
    [InlineData("Spermidine", 1)]
    [InlineData("Urolithin A", 2)]
    public void Batch13_record_carries_valid_sourced_dose_free_claims(string name, int count)
    {
        var record = JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(SeedPath))!.Single(r => r.Identity.CanonicalName == name);
        var sources = PublicClaimProjector.SourcesById(record);
        Assert.Equal(count, record.Evidence.PublicClaims.Count);
        foreach (var claim in record.Evidence.PublicClaims)
        {
            Assert.Empty(PublicClaimProjector.Validate(claim, sources));
            Assert.All(claim.Sources, id => Assert.True(PublicSourcePolicy.IsPublicCitable(sources[id].Url, sources[id].Publisher), id));
            Assert.False(DoseText.IsMatch(claim.Statement), claim.Statement);
            Assert.False(DoseText.IsMatch(claim.Note ?? string.Empty), claim.Note);
        }
        Assert.All(record.Evidence.ClaimSpecificEvidence.SelectMany(c => c.Sources).Where(id => !id.StartsWith("src:", StringComparison.Ordinal)),
            id => Assert.True(sources.ContainsKey(id), $"legacy claim cites missing source {id}"));
        Assert.All(record.DoseReports, d =>
        {
            Assert.Equal("study", d.Side);
            Assert.True(PublicSourcePolicy.IsAllowlistedHost(d.SourceUrl), d.SourceUrl);
            Assert.False(string.IsNullOrWhiteSpace(d.Grade));
        });
    }
}
