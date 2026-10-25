namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim verdict rebuild, batch 6 (research peptides), 2026-10-25.</summary>
public sealed class ClaimBatch6Tests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly Regex DoseText = new(@"\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units?|ml|nmol)\b", RegexOptions.IgnoreCase);

    [Theory]
    [InlineData("Thymosin alpha-1", 2)]
    [InlineData("LL-37", 2)]
    [InlineData("Epitalon", 2)]
    [InlineData("GHRP-2", 3)]
    [InlineData("GHRP-6", 3)]
    [InlineData("Hexarelin", 3)]
    [InlineData("IGF-1 LR3", 2)]
    [InlineData("KPV", 2)]
    [InlineData("PEG-MGF", 2)]
    [InlineData("Selank", 2)]
    [InlineData("Semax", 2)]
    [InlineData("NAD+", 2)]
    public void Batch6_record_carries_valid_sourced_dose_free_claims(string name, int count)
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
