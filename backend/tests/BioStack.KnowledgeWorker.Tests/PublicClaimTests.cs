namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.Domain.Evidence;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

/// <summary>Growth spec Phase 1: claim-level verdicts, versioning and the rebuilt Retatrutide, BPC-157 and TB-500 claims.</summary>
public sealed class PublicClaimTests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static readonly DateTime Day = new(2026, 10, 24, 0, 0, 0, DateTimeKind.Utc);

    private static List<SubstanceRecord> Seed()
        => JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(SeedPath))!;

    [Fact]
    public void Seed_records_with_public_claims_pass_the_schema()
    {
        var validator = SubstanceRecordValidator.LoadFromFile(Path.Combine(TestPaths.WorkerSchemaDirectory(), "substance-record.schema.json"));
        var nodes = JsonNode.Parse(File.ReadAllText(SeedPath))!.AsArray();
        var withClaims = nodes.Where(n => n!["evidence"]!["publicClaims"] is JsonArray { Count: > 0 }).ToList();
        Assert.True(withClaims.Count >= 3);
        foreach (var node in withClaims)
        {
            var result = validator.Validate(node!);
            Assert.True(result.IsValid, $"{node!["identity"]!["canonicalName"]}: {result.Summary()}");
        }
    }

    [Theory]
    [InlineData("Retatrutide", 3)]
    [InlineData("BPC-157", 4)]
    [InlineData("TB-500", 3)]
    public void Rebuilt_records_carry_valid_sourced_claims(string name, int count)
    {
        var record = Seed().Single(r => r.Identity.CanonicalName == name);
        var sources = PublicClaimProjector.SourcesById(record);
        Assert.Equal(count, record.Evidence.PublicClaims.Count);
        foreach (var claim in record.Evidence.PublicClaims)
        {
            Assert.Empty(PublicClaimProjector.Validate(claim, sources));
            Assert.NotEmpty(claim.Sources);
            Assert.All(claim.Sources, id => Assert.StartsWith("https://", sources[id].Url));
        }
        // Legacy claims now point at resolvable source ids too.
        Assert.All(record.Evidence.ClaimSpecificEvidence.SelectMany(c => c.Sources), id => Assert.True(sources.ContainsKey(id), id));
    }

    [Fact]
    public void Every_seed_public_claim_is_valid_and_none_is_published_without_reviewers()
    {
        foreach (var record in Seed().Where(r => r.Evidence.PublicClaims.Count > 0))
        {
            var sources = PublicClaimProjector.SourcesById(record);
            foreach (var claim in record.Evidence.PublicClaims)
            {
                Assert.Empty(PublicClaimProjector.Validate(claim, sources));
                var projected = PublicClaimProjector.Project(claim, sources);
                if (PublicClaimProjector.Latest(claim)!.Reviewers.Count == 0)
                {
                    Assert.False(projected.Published);
                    Assert.Equal(ClaimVerdicts.NotYetGraded, projected.Verdict);
                    Assert.Equal(string.Empty, projected.Note);
                    Assert.Equal(string.Empty, projected.HypeBand);
                    Assert.Empty(projected.Changelog);
                }
            }
        }
    }

    [Fact]
    public void Expected_candidate_verdicts_for_the_rebuilt_claims()
    {
        var claims = Seed().SelectMany(r => r.Evidence.PublicClaims).ToDictionary(c => c.Id);
        Assert.Equal(ClaimVerdicts.ProvenInHumans, PublicClaimProjector.CandidateVerdict(claims["retatrutide-weight-loss"]));
        Assert.Equal(ClaimVerdicts.Myth, PublicClaimProjector.CandidateVerdict(claims["retatrutide-fda-approved"]));
        Assert.Equal(ClaimVerdicts.AnimalOnly, PublicClaimProjector.CandidateVerdict(claims["bpc-157-heals-injuries"]));
        Assert.Equal(ClaimVerdicts.Thin, PublicClaimProjector.CandidateVerdict(claims["bpc-157-knee-pain"]));
        Assert.Equal(ClaimVerdicts.Thin, PublicClaimProjector.CandidateVerdict(claims["bpc-157-proven-safe"]));
        Assert.Equal(ClaimVerdicts.Myth, PublicClaimProjector.CandidateVerdict(claims["tb-500-is-thymosin-beta-4"]));
        Assert.Equal(ClaimVerdicts.AnimalOnly, PublicClaimProjector.CandidateVerdict(claims["tb-500-heals-injuries"]));
        Assert.Equal(ClaimVerdicts.Myth, PublicClaimProjector.CandidateVerdict(claims["tb-500-allowed-in-sport"]));
    }

    [Fact]
    public void Published_claim_shows_verdict_qualifier_band_and_changelog()
    {
        var (claim, sources) = Fixture("Established", reviewers: new() { "AB", "CD" }, published: true);
        claim.Qualifier = new ClaimQualifier { Population = "adults with obesity", Outcome = "weight loss" };
        claim.Hype = new HypeAssessment { Rubric = "Viral", Intensity = 1.0, ScoredAt = Day, ScoredBy = "AB", Rationale = "r" };

        var p = PublicClaimProjector.Project(claim, sources);

        Assert.True(p.Published);
        Assert.Equal(ClaimVerdicts.ProvenInHumans, p.Verdict);
        Assert.Equal("In adults with obesity, for weight loss, under trial conditions.", p.Qualifier);
        Assert.Equal("note", p.Note);
        Assert.Equal(HypeGap.Low, p.HypeBand); // 95 − 100 → 0
        Assert.Equal(HypeGap.MethodNote, p.HypeMethodNote);
        Assert.Equal(Day.AddMonths(6), p.RecheckDueAt);
        Assert.Single(p.Changelog);
        Assert.Equal(new[] { "AB", "CD" }, p.Reviewers);
    }

    [Fact]
    public void Proven_without_qualifier_or_with_one_reviewer_stays_not_yet_graded()
    {
        var (claim, sources) = Fixture("Established", reviewers: new() { "AB" }, published: true);
        var p = PublicClaimProjector.Project(claim, sources);
        Assert.False(p.Published);
        Assert.Equal(ClaimVerdicts.NotYetGraded, p.Verdict);
        Assert.Equal(string.Empty, p.Qualifier);
    }

    [Fact]
    public void Validate_rejects_doses_unresolved_sources_missing_basis_and_out_of_sync_versions()
    {
        var (claim, sources) = Fixture("Hype Exceeds Evidence", reviewers: new(), published: false);
        claim.Statement = "Take 5 mg daily to heal.";
        claim.Sources.Add("missing-id");
        claim.Versions[0].Verdict = ClaimVerdicts.Myth;

        var errors = PublicClaimProjector.Validate(claim, sources);

        Assert.Contains(errors, e => e.Contains("dose"));
        Assert.Contains(errors, e => e.Contains("evidenceBasis"));
        Assert.Contains(errors, e => e.Contains("missing-id"));
        Assert.Contains(errors, e => e.Contains("differs from derived"));
        Assert.Contains(errors, e => e.Contains("sources differ"));
    }

    [Fact]
    public void Canonicalized_details_carry_public_claims()
    {
        var record = Seed().Single(r => r.Identity.CanonicalName == "TB-500");
        var details = new SubstanceCanonicalizer().ToKnowledgeEntry(record).Details!;
        Assert.Equal(3, details.PublicClaims.Count);
        Assert.All(details.PublicClaims, c => Assert.Equal(ClaimVerdicts.NotYetGraded, c.Verdict));
        Assert.Contains(details.PublicClaims.SelectMany(c => c.Sources), s => s.Url.Contains("wada-ama.org"));
    }

    private static (PublicClaim, Dictionary<string, SourceRecord>) Fixture(string tier, List<string> reviewers, bool published)
    {
        var sources = new Dictionary<string, SourceRecord>
        {
            ["s1"] = new() { Id = "s1", Title = "T", Url = "https://example.org/a", SourceType = "paper" },
        };
        var derived = ClaimVerdicts.FromTier(BioStack.Domain.Enums.EvidenceTiers.Parse(tier));
        var claim = new PublicClaim
        {
            Id = "x-claim", Statement = "X works.", Population = "adults", Outcome = "o", Tier = tier,
            Note = "note", Sources = new() { "s1" },
            Versions = new()
            {
                new PublicClaimVersion
                {
                    Version = 1, Kind = "initial", Tier = tier, Verdict = derived, Sources = new() { "s1" },
                    VerifiedAt = Day, Reviewers = reviewers, ChangeReason = "initial",
                    PublishedAt = published ? Day : null,
                },
            },
        };
        return (claim, sources);
    }
}
