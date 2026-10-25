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

    /// <summary>
    /// Source-domain allowlist (voice guide 3c, safety 06 §B blocker): PubMed/PMC, DOI,
    /// trial registries, regulators and government health agencies, anti-doping agencies,
    /// peer-reviewed journal platforms, institutional repositories and the NIH/NLM reference
    /// databases. News, consumer-health, encyclopedia, manufacturer, supplier and forum
    /// hosts are not on it; editorial can add a host here with a reviewed change.
    /// </summary>
    public static readonly IReadOnlyList<string> AllowedHostSuffixes =
    [
        // Literature indexes and identifiers
        "pubmed.ncbi.nlm.nih.gov", "pmc.ncbi.nlm.nih.gov", "ncbi.nlm.nih.gov", "europepmc.org", "doi.org",
        // Trial registries
        "clinicaltrials.gov", "isrctn.com", "trialsearch.who.int", "clinicaltrialsregister.eu", "gsk-studyregister.com",
        // Regulators, labels and government health / reference data
        "fda.gov", "nih.gov", "nlm.nih.gov", "cdc.gov", "dea.gov", "deadiversion.usdoj.gov", "federalregister.gov", "ncats.io",
        "ema.europa.eu", "europa.eu", "who.int", "canada.ca", "tga.gov.au", "gov.uk", "australianprescriber.tg.org.au",
        "go.drugbank.com",
        // Anti-doping agencies
        "wada-ama.org", "usada.org",
        // Peer-reviewed journal platforms and society abstracts
        "nejm.org", "jamanetwork.com", "thelancet.com", "bmj.com", "nature.com", "link.springer.com", "springer.com",
        "academic.oup.com", "frontiersin.org", "journals.sagepub.com", "onlinelibrary.wiley.com", "sciencedirect.com",
        "cell.com", "mdpi.com", "plos.org", "jstage.jst.go.jp", "ahajournals.org", "diabetesjournals.org",
        "acrabstracts.org", "endocrine.org", "tandfonline.com", "karger.com", "biomedcentral.com",
        // Institutional repositories hosting the published paper
        "pure.johnshopkins.edu", "digitalcommons.wustl.edu",
    ];

    public static bool IsAllowlistedHost(string? url)
    {
        if (string.IsNullOrWhiteSpace(url) || !Uri.TryCreate(url, UriKind.Absolute, out var uri)) return false;
        if (uri.Scheme != Uri.UriSchemeHttps && uri.Scheme != Uri.UriSchemeHttp) return false;
        var host = uri.Host.ToLowerInvariant();
        return AllowedHostSuffixes.Any(s => host == s || host.EndsWith("." + s, StringComparison.Ordinal));
    }

    /// <summary>A URL-less reference (e.g. a label citation) is citable as text; a link must be allowlisted.</summary>
    public static bool IsPublicCitable(string? url, string? publisher) =>
        !IsSearchResultsUrl(url) && !IsCommercialMarketing(publisher)
        && (string.IsNullOrWhiteSpace(url) || IsAllowlistedHost(url));
}
