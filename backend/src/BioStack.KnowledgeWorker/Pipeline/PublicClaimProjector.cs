using System.Text.RegularExpressions;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Domain.Evidence;
using BioStack.KnowledgeWorker.Models;

namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Turns a record's <c>evidence.publicClaims</c> into the public projection. The verdict
/// is derived from the 7-tier grade; it, the note and the Hype Gap band are withheld
/// (NOT YET GRADED) until the latest version is verified, reviewed and published and
/// the record passes <see cref="Validate"/>.
/// </summary>
public static class PublicClaimProjector
{
    private static readonly Regex DosePattern = new(
        @"\b\d+(\.\d+)?\s?(mg|mcg|µg|ug|g|iu|ml|mL|units?)\b",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

    public static List<KnowledgePublicClaim> Project(SubstanceRecord record)
    {
        var sources = SourcesById(record);
        return record.Evidence.PublicClaims.Select(c => Project(c, sources)).ToList();
    }

    public static KnowledgePublicClaim Project(PublicClaim claim, IReadOnlyDictionary<string, SourceRecord> sourcesById)
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
        var blockers = ClaimVerdicts.PublishBlockers(
            candidate, resolved.Count, latest?.Reviewers.Count ?? 0, latest?.VerifiedAt, latest?.PublishedAt, qualifier).ToList();
        blockers.AddRange(Validate(claim, sourcesById));
        var published = blockers.Count == 0;

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
            PublishedAt = published ? latest?.PublishedAt : null,
            Published = published,
            Reviewers = published ? latest!.Reviewers.ToList() : new List<string>(),
            Changelog = claim.Versions
                .Where(v => v.PublishedAt is not null && v.Verdict != ClaimVerdicts.NotYetGraded)
                .OrderBy(v => v.Version).ThenBy(v => v.PublishedAt)
                .Select(v => new KnowledgeClaimChange
                {
                    Version = v.Version, Kind = v.Kind, Verdict = v.Verdict, Reason = v.ChangeReason,
                    PublishedAt = v.PublishedAt, Reviewers = v.Reviewers.ToList(),
                })
                .ToList(),
        };
    }

    /// <summary>The verdict the grade earns, before any publish gate.</summary>
    public static string CandidateVerdict(PublicClaim claim)
        => ClaimVerdicts.FromTier(EvidenceTiers.Parse(claim.Tier), EvidenceTiers.Parse(claim.EvidenceBasis));

    public static PublicClaimVersion? Latest(PublicClaim claim)
        => claim.Versions.OrderBy(v => v.Version).ThenBy(v => v.PublishedAt ?? v.VerifiedAt ?? DateTime.MinValue).LastOrDefault();

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
