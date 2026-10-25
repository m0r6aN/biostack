namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Claim verdict rebuild batches (2026-10-25): valid, sourced, dose-free public claims and sourced study doses.</summary>
public sealed class ClaimBatchTests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly Regex DoseText = new(@"\d+(\.\d+)?\s?(mg|mcg|µg|ug|iu|units?|ml)\b", RegexOptions.IgnoreCase);

    private static List<SubstanceRecord> Seed()
        => JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(SeedPath))!;

    [Theory]
    // Batch 1: GLP-1 / incretin class.
    [InlineData("Semaglutide", 3)]
    [InlineData("Tirzepatide", 4)]
    [InlineData("Liraglutide", 2)]
    [InlineData("Dulaglutide", 2)]
    [InlineData("Survodutide", 3)]
    [InlineData("Mazdutide", 2)]
    [InlineData("Cagrilintide", 2)]
    [InlineData("Amycretin", 1)]
    [InlineData("VK2735", 1)]
    [InlineData("Pemvidutide", 2)]
    public void Batch_record_carries_valid_sourced_claims_and_study_doses(string name, int count)
    {
        var record = Seed().Single(r => r.Identity.CanonicalName == name);
        var sources = PublicClaimProjector.SourcesById(record);
        Assert.Equal(count, record.Evidence.PublicClaims.Count);
        foreach (var claim in record.Evidence.PublicClaims)
        {
            Assert.Empty(PublicClaimProjector.Validate(claim, sources));
            Assert.All(claim.Sources, id => Assert.True(PublicSourcePolicy.IsPublicCitable(sources[id].Url, sources[id].Publisher), id));
        }
        Assert.NotEmpty(record.DoseReports);
        Assert.All(record.DoseReports, d =>
        {
            Assert.Equal("study", d.Side);
            Assert.True(PublicSourcePolicy.IsAllowlistedHost(d.SourceUrl), d.SourceUrl);
            Assert.False(string.IsNullOrWhiteSpace(d.Grade));
        });
    }

    [Fact]
    public void No_public_claim_statement_or_note_carries_a_dose()
    {
        foreach (var record in Seed())
            foreach (var claim in record.Evidence.PublicClaims)
            {
                Assert.False(DoseText.IsMatch(claim.Statement), $"{claim.Id}: {claim.Statement}");
                Assert.False(DoseText.IsMatch(claim.Note ?? string.Empty), $"{claim.Id}: {claim.Note}");
            }
    }
}
