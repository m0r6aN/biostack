namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using BioStack.KnowledgeWorker.Models;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class PublicSourcePolicyTests
{
    [Theory]
    [InlineData("https://pubmed.ncbi.nlm.nih.gov/?term=Retatrutide")]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/search.cfm?query=Metformin")]
    [InlineData("https://www.ebi.ac.uk/chembl/api/data/molecule/search?q=liraglutide&format=json")]
    [InlineData("https://api.fda.gov/drug/drugsfda.json?search=products.active_ingredients.name:%22X%22")]
    [InlineData("https://www.ebi.ac.uk/europepmc/webservices/rest/search?query=EXT_ID:7561636")]
    public void SearchResultsUrls_AreNotPublicCitable(string url)
    {
        Assert.True(PublicSourcePolicy.IsSearchResultsUrl(url));
        Assert.False(PublicSourcePolicy.IsPublicCitable(url, "PubMed"));
    }

    [Theory]
    [InlineData("https://pubmed.ncbi.nlm.nih.gov/18981485/")]
    [InlineData("https://www.nejm.org/doi/full/10.1056/NEJMoa2301972")]
    [InlineData("https://clinicaltrials.gov/study/NCT04881760")]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=abc")]
    [InlineData("https://www.fda.gov/media/182087/download")]
    public void SpecificDocuments_ArePublicCitable(string url)
    {
        Assert.True(PublicSourcePolicy.IsPublicCitable(url, "U.S. Food and Drug Administration"));
    }

    [Theory]
    [InlineData("Revolution Health & Wellness (clinic/vendor blog)")]
    [InlineData("Full Potential HRT Clinic (clinic marketing page)")]
    [InlineData("sarms.io (gray-market vendor/content site)")]
    [InlineData("GlpBio (research-chemical vendor)")]
    [InlineData("PeakedLabs (commercial peptide vendor/blog)")]
    public void CommercialPublishers_AreNotPublicCitable(string publisher)
    {
        Assert.False(PublicSourcePolicy.IsPublicCitable("https://example.com/page", publisher));
    }

    [Theory]
    [InlineData("Mayo Clinic (drug information derived from IBM Micromedex)")]
    [InlineData("ClinicalTrials.gov")]
    [InlineData("Dopinglinkki, A-Clinic Foundation (Finnish national doping/substance information service)")]
    public void ClinicalInstitutions_AreNotMistakenForMarketing(string publisher)
    {
        Assert.False(PublicSourcePolicy.IsCommercialMarketing(publisher));
    }

    [Fact]
    public void ShippedSeed_ProjectsNoSearchOrVendorSourcesPublicly()
    {
        var canonicalizer = new SubstanceCanonicalizer();
        foreach (var record in LoadSeed())
        {
            var entry = canonicalizer.ToKnowledgeEntry(record);
            Assert.All(entry.SourceReferences, url => Assert.False(PublicSourcePolicy.IsSearchResultsUrl(url), $"{entry.CanonicalName}: {url}"));
            Assert.All(entry.Details!.Sources, s => Assert.True(PublicSourcePolicy.IsPublicCitable(s.Url, s.Publisher), $"{entry.CanonicalName}: {s.Url}"));
        }
    }

    [Theory]
    [InlineData("Retatrutide")]
    [InlineData("Ibutamoren")]
    [InlineData("BPC-157")]
    [InlineData("TB-500")]
    public void GrowthSpecRecords_KeepSpecificPublicSources(string name)
    {
        var record = LoadSeed().Single(r => r.Identity.CanonicalName == name);
        var entry = new SubstanceCanonicalizer().ToKnowledgeEntry(record);

        Assert.NotEmpty(entry.SourceReferences);
        Assert.Equal(record.Provenance.SourceRecords.Count, entry.SourceReferences.Count);
        Assert.DoesNotContain(entry.SourceReferences, url => url.Contains("revolutionhealth", StringComparison.OrdinalIgnoreCase));
    }

    private static List<SubstanceRecord> LoadSeed()
    {
        var path = Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");
        return JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(path))!;
    }
}

public sealed class DossierSlugTests
{
    [Theory]
    [InlineData("Retatrutide", "retatrutide")]
    [InlineData("BPC-157", "bpc-157")]
    [InlineData("NAD+", "nad")]
    [InlineData("Lion's mane", "lions-mane")]
    [InlineData("St John\u2019s wort", "st-johns-wort")]
    [InlineData("  IGF-1 LR3 ", "igf-1-lr3")]
    [InlineData("", "")]
    public void ForDossier_MatchesTheFrontendRule(string name, string expected)
    {
        Assert.Equal(expected, BioStack.Infrastructure.Knowledge.CompoundSlug.ForDossier(name));
    }

    [Fact]
    public void ForDossier_ReproducesEveryCuratedRecordSlug()
    {
        var path = Path.Combine(TestPaths.BackendRoot(), "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json");
        var records = JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(path))!;
        Assert.All(records, r => Assert.Equal(r.Identity.Slug, BioStack.Infrastructure.Knowledge.CompoundSlug.ForDossier(r.Identity.CanonicalName)));
        Assert.Equal(records.Count, records.Select(r => r.Identity.Slug).Distinct().Count());
    }
}
