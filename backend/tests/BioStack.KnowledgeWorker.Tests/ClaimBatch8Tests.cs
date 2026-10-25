namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim verdict rebuild, batch 8 (OTC drugs and Rx hormones/peptides), 2026-10-25.</summary>
public sealed class ClaimBatch8Tests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly Regex DoseText = new(@"\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units?|ml|nmol)\b", RegexOptions.IgnoreCase);

    [Theory]
    [InlineData("Ibuprofen", 2)]
    [InlineData("Acetaminophen", 2)]
    [InlineData("Acetylcysteine", 1)]
    [InlineData("Phytonadione", 1)]
    [InlineData("Potassium iodide", 2)]
    [InlineData("Potassium chloride", 1)]
    [InlineData("Calcium carbonate", 2)]
    [InlineData("Somatropin", 2)]
    [InlineData("Mecasermin", 2)]
    [InlineData("Oxytocin", 2)]
    [InlineData("Pramlintide", 2)]
    [InlineData("Setmelanotide", 2)]
    public void Batch8_record_carries_valid_sourced_dose_free_claims(string name, int count)
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
