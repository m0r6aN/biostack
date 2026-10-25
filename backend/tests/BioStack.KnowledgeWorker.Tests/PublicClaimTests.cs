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
    public void Every_seed_public_claim_is_valid_and_none_publishes_without_council_sign_off()
    {
        foreach (var record in Seed().Where(r => r.Evidence.PublicClaims.Count > 0))
        {
            var sources = PublicClaimProjector.SourcesById(record);
            foreach (var claim in record.Evidence.PublicClaims)
            {
                Assert.Empty(PublicClaimProjector.Validate(claim, sources));
                var projected = PublicClaimProjector.Project(claim, sources);
                Assert.False(projected.Published);
                Assert.Equal("pending-council", projected.Status);
                Assert.Equal(ClaimVerdicts.NotYetGraded, projected.Verdict);
                Assert.Equal(string.Empty, projected.Note);
                Assert.Equal(string.Empty, projected.HypeBand);
                Assert.Empty(projected.Changelog);
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

    private static ClaimSignOff Council(string outcome, string? decision, string? tier, DateTime at, string id = "x-claim")
        => new("X", id, "ai-council-1", outcome, decision, tier, new[] { "openai-compatible/model-a", "anthropic/model-b" },
            new DateTimeOffset(at, TimeSpan.Zero));

    [Fact]
    public void Council_consensus_publishes_with_model_ids_and_verdict_date()
    {
        var (claim, sources) = Fixture("Established");
        claim.Qualifier = new ClaimQualifier { Population = "adults with obesity", Outcome = "weight loss" };
        claim.Hype = new HypeAssessment { Rubric = "Viral", Intensity = 1.0, ScoredAt = Day, ScoredBy = "editor", Rationale = "r" };

        var p = PublicClaimProjector.Project(claim, sources, new[] { Council("consensus", "approve", "Established", Day.AddDays(1)) });

        Assert.True(p.Published);
        Assert.Equal("published", p.Status);
        Assert.Equal("ai-council", p.ReviewedBy);
        Assert.Equal(new[] { "openai-compatible/model-a", "anthropic/model-b" }, p.Reviewers);
        Assert.Equal(Day.AddDays(1), p.PublishedAt);
        Assert.Equal("ai-council-1", p.CouncilDecisionId);
        Assert.Equal(ClaimVerdicts.ProvenInHumans, p.Verdict);
        Assert.Equal("In adults with obesity, for weight loss, under trial conditions.", p.Qualifier);
        Assert.Equal("note", p.Note);
        Assert.Equal(HypeGap.Low, p.HypeBand); // 95 − 100 → 0
        Assert.Equal(HypeGap.MethodNote, p.HypeMethodNote);
        Assert.Equal(Day.AddMonths(6), p.RecheckDueAt);
        var entry = Assert.Single(p.Changelog);
        Assert.Equal(Day.AddDays(1), entry.PublishedAt);
    }

    [Fact]
    public void Proven_without_qualifier_stays_not_yet_graded_even_with_consensus()
    {
        var (claim, sources) = Fixture("Established");
        var p = PublicClaimProjector.Project(claim, sources, new[] { Council("consensus", "approve", "Established", Day.AddDays(1)) });
        Assert.False(p.Published);
        Assert.Equal(ClaimVerdicts.NotYetGraded, p.Verdict);
    }

    [Theory]
    [InlineData("consensus", "approve", "Early Evidence", 1, "pending-council")]   // agreed on a different tier
    [InlineData("consensus", "approve", "Contradicted", -1, "pending-council")]    // sign-off predates the version
    [InlineData("consensus", "request-changes", "Unsupported", 1, "changes-requested")]
    [InlineData("escalated-to-owner", null, null, 1, "escalated-to-owner")]
    [InlineData("incomplete", null, null, 1, "pending-council")]
    public void Anything_but_current_consensus_at_the_claim_tier_stays_unpublished(string outcome, string? decision, string? tier, int days, string status)
    {
        var (claim, sources) = Fixture("Contradicted");
        var p = PublicClaimProjector.Project(claim, sources, new[] { Council(outcome, decision, tier, Day.AddDays(days)) });
        Assert.False(p.Published);
        Assert.Equal(status, p.Status);
        Assert.Equal(ClaimVerdicts.NotYetGraded, p.Verdict);
        Assert.Empty(p.Reviewers);
    }

    [Fact]
    public void Owner_decision_on_an_escalation_publishes()
    {
        var (claim, sources) = Fixture("Contradicted");
        claim.Versions[0].OwnerDecision = new ClaimOwnerDecision { OptionId = "A", DecidedAt = Day.AddDays(2), DecidedBy = "owner:clinton-morgan" };
        var p = PublicClaimProjector.Project(claim, sources, new[] { Council("escalated-to-owner", null, null, Day.AddDays(1)) });
        Assert.True(p.Published);
        Assert.Equal("owner", p.ReviewedBy);
        Assert.Equal(ClaimVerdicts.Myth, p.Verdict);
        Assert.Equal(Day.AddDays(2), p.PublishedAt);
    }

    [Fact]
    public void Sign_off_index_reads_council_claims_from_decision_batches()
    {
        var batch = JsonNode.Parse("""
        { "decisions": [ { "decisionId": "d1", "compoundName": "TB-500", "reviewedAt": "2026-10-25T08:00:00Z",
          "council": { "claims": [ { "claimId": "tb-500-allowed-in-sport", "outcome": "consensus", "decision": "approve",
            "tier": "Contradicted", "reviewers": [ { "provider": "openai-compatible", "model": "m1" }, { "provider": "anthropic", "model": "m2" } ] } ] } } ] }
        """)!;
        var index = ClaimSignOffIndex.FromBatches(new[] { batch });
        var s = Assert.Single(index.For("tb-500", "tb-500-allowed-in-sport"));
        Assert.Equal(new[] { "openai-compatible/m1", "anthropic/m2" }, s.ReviewerModels);
        Assert.Equal("Contradicted", s.Tier);

        var record = Seed().Single(r => r.Identity.CanonicalName == "TB-500");
        var projected = PublicClaimProjector.Project(record, index).Single(c => c.Id == "tb-500-allowed-in-sport");
        Assert.True(projected.Published);
        Assert.Equal(ClaimVerdicts.Myth, projected.Verdict);
    }

    [Fact]
    public void Validate_rejects_doses_unresolved_sources_missing_basis_and_out_of_sync_versions()
    {
        var (claim, sources) = Fixture("Hype Exceeds Evidence");
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

    private static (PublicClaim, Dictionary<string, SourceRecord>) Fixture(string tier)
    {
        var sources = new Dictionary<string, SourceRecord>
        {
            ["s1"] = new() { Id = "s1", Title = "T", Url = "https://pubmed.ncbi.nlm.nih.gov/1/", SourceType = "paper" },
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
                    VerifiedAt = Day, ChangeReason = "initial",
                },
            },
        };
        return (claim, sources);
    }
}
