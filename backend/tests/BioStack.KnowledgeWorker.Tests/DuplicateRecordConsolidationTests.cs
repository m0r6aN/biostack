namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using BioStack.KnowledgeWorker.Models;
using Xunit;

/// <summary>
/// 2026-10-25 consolidation: testolone folded into vosilasarm and creatine-monohydrate into creatine.
/// The retired names stay searchable as aliases on the survivors; the frontend redirects the old slugs.
/// </summary>
public sealed class DuplicateRecordConsolidationTests
{
    private static readonly string SeedPath = Path.GetFullPath(Path.Combine(
        AppContext.BaseDirectory, "..", "..", "..", "..", "..",
        "src", "BioStack.KnowledgeWorker", "Seeds", "substances-seed.json"));

    private static List<SubstanceRecord> Records() =>
        JsonSerializer.Deserialize<List<SubstanceRecord>>(File.ReadAllText(SeedPath))!;

    [Theory]
    [InlineData("testolone", "vosilasarm", "Testolone")]
    [InlineData("creatine-monohydrate", "creatine", "Creatine monohydrate")]
    public void Retired_slug_is_gone_and_survives_as_alias(string retired, string canonical, string alias)
    {
        var records = Records();
        Assert.DoesNotContain(records, r => r.Identity.Slug == retired || r.Identity.CanonicalId == retired);
        var survivor = Assert.Single(records, r => r.Identity.Slug == canonical);
        Assert.Contains(survivor.Identity.Aliases, a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase));
        Assert.NotEmpty(survivor.Evidence.PublicClaims);
        Assert.Single(records, r => r.Identity.Aliases.Any(a => string.Equals(a, alias, StringComparison.OrdinalIgnoreCase))
                                    || string.Equals(r.Identity.CanonicalName, alias, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Creatine_keeps_the_retired_record_slug_and_brand_as_aliases()
    {
        var creatine = Records().Single(r => r.Identity.Slug == "creatine");
        Assert.Contains("creatine-monohydrate", creatine.Identity.Aliases);
        Assert.Contains("Creapure", creatine.Identity.Aliases);
    }
}
