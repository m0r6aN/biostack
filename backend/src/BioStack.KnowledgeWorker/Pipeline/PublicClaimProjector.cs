using System.Text.RegularExpressions;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Domain.Evidence;
using BioStack.KnowledgeWorker.Models;

namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Turns a record's <c>evidence.publicClaims</c> into the public projection. The verdict
/// is derived from the 7-tier grade; it, the note and the Hype Gap band are withheld
/// (NOT YET GRADED) until the two-reviewer AI council reaches consensus on the latest
/// version (or the owner resolves an escalation) and the record passes <see cref="Validate"/>.
/// </summary>
public static class PublicClaimProjector
{
    private static readonly Regex DosePattern = new(
        @"\b\d+(\.\d+)?\s?(mg|mcg|µg|ug|g|iu|ml|mL|units?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public const string OwnerReviewerId = "owner:clinton-morgan";

    public static List<KnowledgePublicClaim> Project(SubstanceRecord record)
        => Project(record, ClaimSignOffIndex.Empty);

    public static List<KnowledgePublicClaim> Project(SubstanceRecord record, ClaimSignOffIndex signOffs)
    {
        var sources = SourcesById(record);
        return record.Evidence.PublicClaims
            .Select(c => Project(c, sources, signOffs.For(record.Identity.CanonicalName, c.Id)))
            .ToList();
    }

    public static KnowledgePublicClaim Project(PublicClaim claim, IReadOnlyDictionary<string, SourceRecord> sourcesById)
        => Project(claim, sourcesById, Array.Empty<ClaimSignOff>());

    /// <summary>
    /// Sign-off comes from the AI council (consensus approve at the claim's tier, dated on or
    /// after the latest version's verifiedAt) or, after an escalation, from the owner's
    /// recorded decision. No human approval is required otherwise.
    /// </summary>
    public static KnowledgePublicClaim Project(
        PublicClaim claim, IReadOnlyDictionary<string, SourceRecord> sourcesById, IReadOnlyList<ClaimSignOff> signOffs)
    {
        var tier = EvidenceTiers.Parse(claim.Tier);
        var candidate = CandidateVerdict(claim);
        var qualifier = candidate == ClaimVerdicts.ProvenInHumans
            ? ClaimVerdicts.Qualifier(claim.Qualifier?.Population, claim.Qualifier?.Outcome)
            : string.Empty;
        var resolved = claim.Sources
            .Where(sourcesById.ContainsKey)
            .Select(id => (id, s: sourcesById[id]))
            .Select(x => new KnowledgeClaimSource
            {
                Id = x.id,
                Title = x.s.Title,
                Url = x.s.Url ?? string.Empty,
                Publisher = x.s.Publisher ?? string.Empty,
                SourceType = x.s.SourceType,
            })
            .ToList();

        var latest = Latest(claim);
        var signOff = SignOffFor(claim, latest, NextVersionAt(claim, latest), signOffs);
        var blockers = ClaimVerdicts.PublishBlockers(
            candidate, resolved.Count, signOff?.Reviewers.Count ?? 0, latest?.VerifiedAt, signOff?.At, qualifier).ToList();
        blockers.AddRange(Validate(claim, sourcesById));
        var published = blockers.Count == 0;

        var current = latest?.VerifiedAt is { } verified
            ? signOffs.LastOrDefault(s => s.ReviewedAt.UtcDateTime >= verified)
            : signOffs.LastOrDefault();
        var status = published ? "published"
            : current?.Outcome == "escalated-to-owner" ? "escalated-to-owner"
            : current is { Outcome: "consensus", Decision: "request-changes" } ? "changes-requested"
            : "pending-council";

        var hypeScore = HypeGap.HypeScore(claim.Hype?.Rubric, claim.Hype?.Intensity);
        var band = published ? HypeGap.Band(candidate, tier, hypeScore) : string.Empty;

        return new KnowledgePublicClaim
        {
            Id = claim.Id,
            Statement = claim.Statement,
            Population = claim.Population,
            Outcome = claim.Outcome,
            Tier = claim.Tier,
            Verdict = published ? candidate : ClaimVerdicts.NotYetGraded,
            Qualifier = published ? qualifier : string.Empty,
            Note = published ? claim.Note : string.Empty,
            HypeBand = band,
            HypeMethodNote = band.Length == 0 ? string.Empty
                : hypeScore is null ? HypeGap.HypeExceedsEvidenceNote : HypeGap.MethodNote,
            Sources = resolved,
            Version = latest?.Version ?? 0,
            VerifiedAt = latest?.VerifiedAt,
            RecheckDueAt = latest?.VerifiedAt?.AddMonths(ClaimVerdicts.RecheckMonths(candidate)),
            PublishedAt = published ? signOff!.At : null,
            Published = published,
            Status = status,
            ReviewedBy = published ? signOff!.By : string.Empty,
            Reviewers = published ? signOff!.Reviewers.ToList() : new List<string>(),
            CouncilDecisionId = published ? signOff!.DecisionId : string.Empty,
            Changelog = Changelog(claim, signOffs),
        };
    }

    private sealed record SignOffInfo(string By, IReadOnlyList<string> Reviewers, DateTime At, string DecisionId);

    private static DateTime? NextVersionAt(PublicClaim claim, PublicClaimVersion? version)
        => version is null ? null : claim.Versions
            .Where(v => v.Kind != "edit" && v.Version > version.Version)
            .Select(v => v.VerifiedAt).Where(d => d is not null).Min();

    /// <summary>The sign-off that applies to one version, if any.</summary>
    private static SignOffInfo? SignOffFor(PublicClaim claim, PublicClaimVersion? version, DateTime? before, IReadOnlyList<ClaimSignOff> signOffs)
    {
        if (version is null) return null;
        // Edit entries (typo fixes) share the version number; an owner decision on any of them counts.
        var owner = claim.Versions
            .Where(v => v.Version == version.Version && v.OwnerDecision?.DecidedAt is not null)
            .Select(v => v.OwnerDecision!)
            .Where(d => version.VerifiedAt is null || d.DecidedAt >= version.VerifiedAt)
            .OrderBy(d => d.DecidedAt).LastOrDefault();
        if (owner is not null)
        {
            var who = string.IsNullOrWhiteSpace(owner.DecidedBy) ? OwnerReviewerId : owner.DecidedBy;
            // The owner is the final escalation step, so the decision stands in for both seats.
            return new SignOffInfo("owner", new[] { who, who }, owner.DecidedAt!.Value, $"owner-option-{owner.OptionId}");
        }

        var council = signOffs.FirstOrDefault(s =>
            s.Outcome == "consensus" && s.Decision == "approve" &&
            string.Equals(s.Tier, version.Tier, StringComparison.Ordinal) &&
            (version.VerifiedAt is null || s.ReviewedAt.UtcDateTime >= version.VerifiedAt) &&
            (before is null || s.ReviewedAt.UtcDateTime < before));
        return council is null ? null
            : new SignOffInfo("ai-council", council.ReviewerModels, council.ReviewedAt.UtcDateTime, council.DecisionId);
    }

    private static List<KnowledgeClaimChange> Changelog(PublicClaim claim, IReadOnlyList<ClaimSignOff> signOffs)
    {
        var entries = new List<KnowledgeClaimChange>();
        foreach (var v in claim.Versions.Where(v => v.Kind != "edit").OrderBy(v => v.Version))
        {
            var signed = SignOffFor(claim, v, NextVersionAt(claim, v), signOffs);
            if (signed is null || v.Verdict == ClaimVerdicts.NotYetGraded) continue;
            entries.Add(new KnowledgeClaimChange
            {
                Version = v.Version, Kind = v.Kind, Verdict = v.Verdict, Reason = v.ChangeReason,
                PublishedAt = signed.At, Reviewers = signed.Reviewers.Distinct().ToList(),
            });
        }
        return entries;
    }

    /// <summary>The verdict the grade earns, before any publish gate.</summary>
    public static string CandidateVerdict(PublicClaim claim)
        => ClaimVerdicts.FromTier(EvidenceTiers.Parse(claim.Tier), EvidenceTiers.Parse(claim.EvidenceBasis));

    public static PublicClaimVersion? Latest(PublicClaim claim)
        => claim.Versions.OrderBy(v => v.Version).ThenBy(v => v.Kind == "edit" ? 1 : 0).ThenBy(v => v.VerifiedAt ?? DateTime.MinValue).LastOrDefault();

    public static Dictionary<string, SourceRecord> SourcesById(SubstanceRecord record)
        => record.Provenance.SourceRecords
            .Where(s => !string.IsNullOrWhiteSpace(s.Id))
            .GroupBy(s => s.Id!, StringComparer.Ordinal)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.Ordinal);

    /// <summary>Data rules a public claim must meet. Empty means valid.</summary>
    public static IReadOnlyList<string> Validate(PublicClaim claim, IReadOnlyDictionary<string, SourceRecord> sourcesById)
    {
        var errors = new List<string>();
        var tier = EvidenceTiers.Parse(claim.Tier);
        var candidate = CandidateVerdict(claim);

        if (DosePattern.IsMatch(claim.Statement) || DosePattern.IsMatch(claim.Note))
            errors.Add($"{claim.Id}: statement and note must not carry a dose");
        if (tier == EvidenceTier.HypeExceedsEvidence)
        {
            var basis = EvidenceTiers.Parse(claim.EvidenceBasis);
            if (basis is EvidenceTier.Unknown or EvidenceTier.HypeExceedsEvidence)
                errors.Add($"{claim.Id}: Hype Exceeds Evidence needs an evidenceBasis tier");
        }
        if (candidate == ClaimVerdicts.ProvenInHumans &&
            ClaimVerdicts.Qualifier(claim.Qualifier?.Population, claim.Qualifier?.Outcome).Length == 0)
            errors.Add($"{claim.Id}: PROVEN IN HUMANS needs a qualifier");
        foreach (var id in claim.Sources.Where(id => !sourcesById.ContainsKey(id)))
            errors.Add($"{claim.Id}: source '{id}' does not resolve to a provenance source id");

        if (claim.Versions.Count == 0)
        {
            errors.Add($"{claim.Id}: needs at least one version");
            return errors;
        }
        var bumps = claim.Versions.Where(v => v.Kind != "edit").Select(v => v.Version).ToList();
        if (bumps.Distinct().Count() != bumps.Count)
            errors.Add($"{claim.Id}: version numbers must be unique (only 'edit' entries may repeat one)");
        if (claim.Versions.Min(v => v.Version) != 1)
            errors.Add($"{claim.Id}: versions start at 1");
        if (claim.Versions.Any(v => string.IsNullOrWhiteSpace(v.ChangeReason)))
            errors.Add($"{claim.Id}: every version needs a changeReason");

        var latest = Latest(claim)!;
        if (!string.Equals(latest.Tier, claim.Tier, StringComparison.Ordinal))
            errors.Add($"{claim.Id}: latest version tier '{latest.Tier}' differs from claim tier '{claim.Tier}'");
        if (latest.Verdict != candidate)
            errors.Add($"{claim.Id}: latest version verdict '{latest.Verdict}' differs from derived '{candidate}'");
        if (!latest.Sources.ToHashSet(StringComparer.Ordinal).SetEquals(claim.Sources))
            errors.Add($"{claim.Id}: latest version sources differ from claim sources (adding or removing one bumps the version)");
        return errors;
    }
}
