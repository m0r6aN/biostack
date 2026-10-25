namespace BioStack.Api.Alerts;

using System.Text.RegularExpressions;
using BioStack.Domain.Entities;

/// <summary>A published claim version that may need to reach its watchers.</summary>
public sealed record ClaimChangeEvent(
    string CompoundSlug, string CompoundName, string ClaimId, string Statement,
    int Version, string OldVerdict, string NewVerdict, string Reason, DateTime PublishedAtUtc, string AlertType);

/// <summary>
/// Growth Phase 6 (03 §6). Pure planning: turns published claim changelogs into alert events
/// and matches them to watches. Only published versions (the AI council or Clint signed off)
/// are in a changelog, so raw ingestion can never alert. A version whose verdict differs from
/// the previous published one is a verdict change; a version with the same verdict (sources
/// added on review) is "study added".
/// </summary>
public static partial class EvidenceAlertPlanner
{
    public const string VerdictChange = "verdict-change";
    public const string StudyAdded = "study-added";

    public static IReadOnlyList<ClaimChangeEvent> Events(
        string compoundSlug, string compoundName, IEnumerable<KnowledgePublicClaim> claims, DateTime sinceUtc)
    {
        var events = new List<ClaimChangeEvent>();
        foreach (var claim in claims)
        {
            var published = claim.Changelog.Where(c => c.PublishedAt.HasValue).OrderBy(c => c.Version).ToList();
            for (var i = 0; i < published.Count; i++)
            {
                var change = published[i];
                if (change.PublishedAt!.Value < sinceUtc) continue;
                var previous = i > 0 ? published[i - 1].Verdict : "NOT YET GRADED";
                var type = string.Equals(previous, change.Verdict, StringComparison.Ordinal) ? StudyAdded : VerdictChange;
                events.Add(new ClaimChangeEvent(compoundSlug, compoundName, claim.Id, RedactDoses(claim.Statement), change.Version,
                    previous, change.Verdict, RedactDoses(change.Reason), change.PublishedAt.Value, type));
            }
        }
        return events;
    }

    /// <summary>Does this watch cover the event? Compound, claim or a /vs pair containing the compound.</summary>
    public static bool Matches(EvidenceWatch watch, ClaimChangeEvent e) => watch.TargetType switch
    {
        "compound" => string.Equals(watch.TargetId, e.CompoundSlug, StringComparison.OrdinalIgnoreCase),
        "claim" => string.Equals(watch.TargetId, e.ClaimId, StringComparison.Ordinal),
        "vs" => watch.TargetId.Split('/').Contains(e.CompoundSlug, StringComparer.OrdinalIgnoreCase),
        _ => false,
    };

    /// <summary>Alerts never contain doses (03 §6): any "[number] [unit]" is removed.</summary>
    public static string RedactDoses(string text) => DoseToken().Replace(text ?? string.Empty, "[dose removed]");

    [GeneratedRegex(@"\b\d+(?:[.,]\d+)?(?:\s?(?:[-–/]|to)\s?\d+(?:[.,]\d+)?)*\s?(?:mg|mcg|µg|ug|iu|units?|ml)\b", RegexOptions.IgnoreCase)]
    private static partial Regex DoseToken();
}
