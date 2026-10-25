namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim verdict rebuild, batch 4 (hormones, AAS and remaining SARMs), 2026-10-25.</summary>
public sealed class ClaimBatch4Tests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly Regex DoseText = new(@"\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units?|ml|nmol)\b", RegexOptions.IgnoreCase);

    [Theory]
    [InlineData("Testosterone cypionate", 4)]
    [InlineData("Oxandrolone", 3)]
    [InlineData("Nandrolone decanoate", 2)]
    [InlineData("Anastrozole", 3)]
    [InlineData("Letrozole", 2)]
    [InlineData("Human chorionic gonadotropin", 3)]
    [InlineData("Finasteride", 3)]
    [InlineData("DHEA", 2)]
    [InlineData("Andarine", 2)]
    [InlineData("AC-262536", 2)]
    [InlineData("LGD-3303", 2)]
    public void Batch4_record_carries_valid_sourced_dose_free_claims(string name, int count)
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

    [Fact]
    public void Oxandrolone_is_not_presented_as_approved()
    {
        var record = JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(SeedPath))!.Single(r => r.Identity.CanonicalName == "Oxandrolone");
        Assert.StartsWith("FDA approval withdrawn", record.Regulatory.RegulatoryStatus);
        Assert.Empty(record.Regulatory.ApprovedIndications);
        Assert.DoesNotContain(record.Indications, i => i.LabelStatus == "on-label");
    }

    [Fact]
    public void Dea_diversion_list_is_citable()
        => Assert.True(PublicSourcePolicy.IsPublicCitable("https://www.deadiversion.usdoj.gov/schedules/orangebook/c_cs_alpha.pdf", "U.S. Drug Enforcement Administration"));
}
