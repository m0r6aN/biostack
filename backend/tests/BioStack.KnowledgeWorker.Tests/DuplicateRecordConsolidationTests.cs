namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json;
using BioStack.KnowledgeWorker.Models;
using Xunit;

/// <summary>
/// 2026-10-25 consolidation: testolone folded into vosilasarm. Creatine and creatine-monohydrate stay
/// distinct per owner ruling D-D (2026-10-08).
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
    public void Creatine_and_creatine_monohydrate_remain_distinct_per_DD()
    {
        var records = Records();
        Assert.Single(records, r => r.Identity.Slug == "creatine");
        Assert.Single(records, r => r.Identity.Slug == "creatine-monohydrate");
    }
}
