namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.RegularExpressions;

/// <summary>
/// Which source references may be shown publicly (growth spec Phase 0, approved by Clint
/// 2026-10-24; methodology sourcing rule): every public source must point at a specific study,
/// review, label, registry entry or regulator document. Search-results URLs and vendor / clinic
/// marketing pages are kept in the curated record for curators to replace, but are never
/// projected into a public <c>KnowledgeEntry</c>.
/// </summary>
public static partial class PublicSourcePolicy
{
    // Query-string search parameters and search endpoints (PubMed ?term=, DailyMed search.cfm,
    // ChEMBL /search?q=, openFDA ?search=, Europe PMC /search?query=).
    [GeneratedRegex(@"([?&](term|q|query|kw|k|search|searchTerm)=)|(/search(\.cfm|\.aspx|/|\?|$))", RegexOptions.IgnoreCase)]
    private static partial Regex SearchUrlPattern();

    // Publisher labels the curation pipeline uses for commercial sources.
    [GeneratedRegex(@"\bvendor\b|\bmarketing\b|gray-market|research-chemical|commercial peptide", RegexOptions.IgnoreCase)]
    private static partial Regex CommercialPublisherPattern();

    public static bool IsSearchResultsUrl(string? url) =>
        !string.IsNullOrWhiteSpace(url) && SearchUrlPattern().IsMatch(url);

    public static bool IsCommercialMarketing(string? publisher) =>
        !string.IsNullOrWhiteSpace(publisher) && CommercialPublisherPattern().IsMatch(publisher);

    public static bool IsPublicCitable(string? url, string? publisher) =>
        !IsSearchResultsUrl(url) && !IsCommercialMarketing(publisher);
}
