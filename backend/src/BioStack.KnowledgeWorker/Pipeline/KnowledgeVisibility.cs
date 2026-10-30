namespace BioStack.KnowledgeWorker.Pipeline;

using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Which stored records the public may see. Owner ruling t101u (2026-10-30): an auto-discovered draft is
/// hidden from every public read (list, detail, search, sitemap, analyzer pools) until the two-reviewer AI
/// council approves it. Curated records (the seed and the owner-authored files in Seeds/discovered-records)
/// stay public with their review-state label, as before (owner ruling 9, 2026-10-24).
/// </summary>
public static class KnowledgeVisibility
{
    public const string AutoDiscoveredFlag = "auto-discovered";
    public const string AutoDiscoveredFamilyPrefix = "unknown (auto-discovered";
    public const string DiscoveryIngestionPrefix = "discovery:";

    /// <summary>True for a draft the discovery job wrote (never for a seed or curated record).</summary>
    public static bool IsAutoDiscovered(SubstanceRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        return record.Ops.QualityFlags.Any(f => string.Equals(f, AutoDiscoveredFlag, StringComparison.OrdinalIgnoreCase))
               || (record.Ops.IngestionSource ?? string.Empty).StartsWith(DiscoveryIngestionPrefix, StringComparison.OrdinalIgnoreCase)
               || (record.Identity.CompoundFamily ?? string.Empty).StartsWith(AutoDiscoveredFamilyPrefix, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Public when the record is curated, or when it is an auto-discovered draft that the promotion gate
    /// allows (an approve-for-promotion decision from the AI council or the owner).
    /// </summary>
    public static bool IsPublic(SubstanceRecord record, bool councilApproved)
        => !IsAutoDiscovered(record) || councilApproved;
}
