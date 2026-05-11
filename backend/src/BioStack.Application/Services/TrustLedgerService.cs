namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Knowledge;

public sealed class TrustLedgerService(IKnowledgeSource knowledgeSource) : ITrustLedgerService
{
    public async Task<TrustLedgerResponse?> GetTrustLedgerAsync(
        string slug, CancellationToken ct = default)
    {
        var lookupName = SlugToName(slug);
        var entry = await knowledgeSource.GetCompoundAsync(lookupName, ct);
        if (entry is null) return null;

        return BuildResponse(slug, entry);
    }

    private static string SlugToName(string slug) =>
        slug.Replace('-', ' ').Trim();

    private static TrustLedgerResponse BuildResponse(string slug, KnowledgeEntry entry)
    {
        var qualityFlags = BuildQualityFlags(entry);
        var promotionBlockers = BuildPromotionBlockers(entry);
        var needsReview = entry.EvidenceTier == EvidenceTier.Unknown
                       || promotionBlockers.Count > 0;

        var claims = BuildClaims(entry);
        var completeness = DeriveCompleteness(entry);
        var conflicts = BuildConflicts(entry);
        var requiredNextActions = BuildRequiredNextActions(entry, promotionBlockers);

        return new TrustLedgerResponse(
            Slug: slug,
            CanonicalName: entry.CanonicalName,
            EvidenceTier: entry.EvidenceTier.ToString().ToLowerInvariant(),
            Completeness: completeness,
            NeedsReview: needsReview,
            QualityFlags: qualityFlags,
            RegulatoryBoundary: entry.RegulatoryStatus,
            Claims: claims,
            Conflicts: conflicts,
            PromotionBlockers: promotionBlockers,
            RequiredNextActions: requiredNextActions,
            Status: needsReview ? "review-gated" : "promoted");
    }

    private static List<string> BuildQualityFlags(KnowledgeEntry entry)
    {
        var flags = new List<string>();
        if (entry.SourceReferences.Count == 0) flags.Add("no-source-references");
        if (string.IsNullOrWhiteSpace(entry.MechanismSummary)) flags.Add("missing-mechanism-summary");
        if (entry.Benefits.Count == 0) flags.Add("no-benefits-documented");
        if (entry.Pathways.Count == 0) flags.Add("no-pathways-documented");
        if (entry.EvidenceTier == EvidenceTier.Unknown) flags.Add("evidence-tier-unclassified");
        return flags;
    }

    private static List<string> BuildPromotionBlockers(KnowledgeEntry entry)
    {
        var blockers = new List<string>();
        if (entry.EvidenceTier == EvidenceTier.Unknown)
            blockers.Add("Evidence tier must be classified before promotion");
        if (string.IsNullOrWhiteSpace(entry.RegulatoryStatus))
            blockers.Add("Regulatory boundary must be defined before promotion");
        if (entry.SourceReferences.Count == 0)
            blockers.Add("At least one source reference is required for promotion");
        return blockers;
    }

    private static IReadOnlyList<TrustLedgerClaim> BuildClaims(KnowledgeEntry entry)
    {
        var claims = new List<TrustLedgerClaim>();

        if (!string.IsNullOrWhiteSpace(entry.MechanismSummary))
            claims.Add(new TrustLedgerClaim(
                ClaimText: entry.MechanismSummary,
                Confidence: MapTierToConfidence(entry.EvidenceTier),
                SourceRefs: entry.SourceReferences,
                ExtractedQuote: null,
                ReviewFlags: []));

        foreach (var benefit in entry.Benefits)
            claims.Add(new TrustLedgerClaim(
                ClaimText: benefit,
                Confidence: MapTierToConfidence(entry.EvidenceTier),
                SourceRefs: entry.SourceReferences,
                ExtractedQuote: null,
                ReviewFlags: []));

        return claims;
    }

    private static string DeriveCompleteness(KnowledgeEntry entry)
    {
        var scored = 0;
        if (!string.IsNullOrWhiteSpace(entry.MechanismSummary)) scored++;
        if (entry.SourceReferences.Count > 0) scored++;
        if (entry.Benefits.Count > 0) scored++;
        if (entry.Pathways.Count > 0) scored++;
        if (entry.EvidenceTier != EvidenceTier.Unknown) scored++;
        return scored >= 4 ? "complete" : scored >= 2 ? "partial" : "minimal";
    }

    private static List<string> BuildConflicts(KnowledgeEntry entry)
    {
        var conflicts = new List<string>();
        if (entry.AvoidWith.Count > 0)
            conflicts.Add($"Avoid combining with: {string.Join(", ", entry.AvoidWith)}");
        foreach (var interaction in entry.DrugInteractions)
            conflicts.Add(interaction);
        return conflicts;
    }

    private static List<string> BuildRequiredNextActions(
        KnowledgeEntry entry, List<string> blockers)
    {
        var actions = new List<string>(blockers);
        if (entry.EvidenceTier is EvidenceTier.Limited or EvidenceTier.Unknown)
            actions.Add("Source review required to elevate evidence tier");
        return actions;
    }

    private static string MapTierToConfidence(EvidenceTier tier) =>
        tier switch
        {
            EvidenceTier.Strong      => "high",
            EvidenceTier.Mechanistic => "high",
            EvidenceTier.Moderate    => "moderate",
            EvidenceTier.Limited     => "low",
            _                        => "insufficient"
        };
}

public interface ITrustLedgerService
{
    Task<TrustLedgerResponse?> GetTrustLedgerAsync(string slug, CancellationToken ct = default);
}
