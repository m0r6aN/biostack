namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.Domain.Enums;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Owner rulings 2026-10-24: 7-tier methodology scale; "Bro-Science" renamed Hype Exceeds Evidence.</summary>
public sealed class SevenTierRegradeTests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    [Fact]
    public void Seed_Uses_Only_Seven_Tier_Labels_And_Keeps_Legacy_Grade_With_Rationale()
    {
        var records = JsonNode.Parse(File.ReadAllText(SeedPath))!.AsArray();
        Assert.Equal(158, records.Count);
        foreach (var record in records)
        {
            var evidence = record!["evidence"]!;
            Assert.Contains(evidence["overallTier"]!.GetValue<string>(), RecordEvidenceTiers.All);
            foreach (var claim in evidence["claimSpecificEvidence"]!.AsArray())
            {
                Assert.Contains(claim!["tier"]!.GetValue<string>(), RecordEvidenceTiers.All);
                Assert.False(string.IsNullOrWhiteSpace(claim["legacyTier"]?.GetValue<string>()));
                Assert.False(string.IsNullOrWhiteSpace(claim["gradeRationale"]?.GetValue<string>()));
            }
            foreach (var use in record["indications"]!.AsArray())
                Assert.Contains(use!["evidenceTier"]!.GetValue<string>(), RecordEvidenceTiers.All);
        }
    }

    [Fact]
    public void Seed_Never_Uses_The_Old_Bro_Science_Name()
    {
        var text = File.ReadAllText(SeedPath);
        Assert.DoesNotContain("bro-science", text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("broscience", text, StringComparison.OrdinalIgnoreCase);
    }

    [Theory]
    [InlineData("Strong", "Established")]
    [InlineData("Moderate", "Early Evidence")]
    [InlineData("Limited", "Plausible Theory")]
    [InlineData("Anecdotal", "Anecdotal Signal")]
    [InlineData("Insufficient", "Unsupported")]
    [InlineData("Unknown", "Not graded")]
    [InlineData("Hype Exceeds Evidence", "Hype Exceeds Evidence")]
    public void Packet_Tiers_Normalize_To_Record_Tiers(string packet, string expected)
        => Assert.Equal(expected, RecordEvidenceTiers.Normalize(packet));

    [Theory]
    [InlineData("Established", EvidenceTier.Established)]
    [InlineData("Plausible Theory", EvidenceTier.PlausibleTheory)]
    [InlineData("Early Evidence", EvidenceTier.EarlyEvidence)]
    [InlineData("Anecdotal Signal", EvidenceTier.AnecdotalSignal)]
    [InlineData("Contradicted", EvidenceTier.Contradicted)]
    [InlineData("Unsupported", EvidenceTier.Unsupported)]
    [InlineData("Hype Exceeds Evidence", EvidenceTier.HypeExceedsEvidence)]
    [InlineData("Not graded", EvidenceTier.Unknown)]
    public void Domain_Parses_Record_Labels(string label, EvidenceTier expected)
    {
        Assert.Equal(expected, EvidenceTiers.Parse(label));
        if (expected != EvidenceTier.Unknown) Assert.Equal(label, EvidenceTiers.Label(expected));
    }
}
