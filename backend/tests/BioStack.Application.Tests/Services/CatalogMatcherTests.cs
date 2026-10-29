namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using Xunit;

public sealed class CatalogMatcherTests
{
    private static readonly IReadOnlyList<CatalogItem> Catalog =
    [
        new("creatine", "Creatine", CompoundCategory.Supplement, ["creatine monohydrate"]),
        new("semaglutide", "Semaglutide", CompoundCategory.Peptide, ["Ozempic", "Wegovy", "Rybelsus"]),
        new("magnesium", "Magnesium", CompoundCategory.Mineral, []),
        new("ashwagandha", "Ashwagandha", CompoundCategory.Supplement, ["withania somnifera"]),
        new("vitamin-d3", "Vitamin D3", CompoundCategory.Vitamin, ["cholecalciferol"]),
        new("vitamin-c", "Vitamin C", CompoundCategory.Vitamin, ["ascorbic acid"]),
        new("vitamin-b12", "Vitamin B12", CompoundCategory.Vitamin, ["cobalamin"]),
        new("bpc-157", "BPC-157", CompoundCategory.Peptide, []),
        new("nad-plus", "NAD+", CompoundCategory.Coenzyme, []),
        new("omega-3", "Omega-3 fatty acids", CompoundCategory.Supplement, ["fish oil"]),
    ];

    private static IReadOnlyList<CatalogMatch> Match(string query) => CatalogMatcher.Match(query, Catalog);

    [Theory]
    [InlineData("Creatine")]
    [InlineData("  creatine ")]
    [InlineData("CREATINE")]
    public void TheSameNameIsSameAs_ExactlyOnce(string typed)
    {
        var best = Assert.Single(Match(typed), m => m.Slug == "creatine");
        Assert.Equal(CatalogMatchKind.Exact, best.Kind);
        Assert.Equal(CatalogMatchLabel.SameAs, best.Label);
        Assert.Equal(1.0f, best.Score);
        Assert.Equal("creatine", Match(typed)[0].Slug);
    }

    [Theory]
    [InlineData("bpc157")]
    [InlineData("BPC 157")]
    [InlineData("bpc-157")]
    [InlineData("B.P.C.-157")]
    public void SpacingPunctuationAndCaseDoNotMatter(string typed)
    {
        var best = Match(typed)[0];
        Assert.Equal("bpc-157", best.Slug);
        Assert.Equal(CatalogMatchLabel.SameAs, best.Label);
    }

    [Theory]
    [InlineData("Ozempic", "semaglutide")]
    [InlineData("wegovy", "semaglutide")]
    [InlineData("fish oil", "omega-3")]
    [InlineData("Cholecalciferol", "vitamin-d3")]
    [InlineData("Ascorbic Acid", "vitamin-c")]
    public void AnAlternateNameOrBrandIsSameAs(string typed, string slug)
    {
        var best = Match(typed)[0];
        Assert.Equal(slug, best.Slug);
        Assert.Equal(CatalogMatchKind.Alias, best.Kind);
        Assert.Equal(CatalogMatchLabel.SameAs, best.Label);
        Assert.Contains("alternate name", best.Reason, StringComparison.Ordinal);
    }

    [Fact]
    public void TheSlugIsSameAs()
    {
        var best = Match("vitamin-d3")[0];
        Assert.Equal("vitamin-d3", best.Slug);
        Assert.Equal(CatalogMatchLabel.SameAs, best.Label);
    }

    [Theory]
    [InlineData("Creatine capsules")]
    [InlineData("creatine powder")]
    [InlineData("Creatine Gummies")]
    public void FormWordsAreIgnored(string typed)
    {
        var best = Match(typed)[0];
        Assert.Equal("creatine", best.Slug);
        Assert.Equal(CatalogMatchKind.Exact, best.Kind);
    }

    [Fact]
    public void WordsThatAreOnlyAFormStillMatchSomething()
    {
        // Stripping "powder" leaves nothing; the typed words are kept rather than matching everything.
        Assert.Empty(Match("powder"));
    }

    [Fact]
    public void WordsThatBelongToAnotherNameAreSimilar_NotSameAs()
    {
        var best = Assert.Single(Match("Magnesium glycinate"));
        Assert.Equal("magnesium", best.Slug);
        Assert.Equal(CatalogMatchKind.Partial, best.Kind);
        Assert.Equal(CatalogMatchLabel.Similar, best.Label);
    }

    [Fact]
    public void AnExactNameComesFirst_AndASimilarOneFollows()
    {
        var matches = Match("creatine");
        Assert.Equal("creatine", matches[0].Slug);
        Assert.Equal(CatalogMatchLabel.SameAs, matches[0].Label);
    }

    [Fact]
    public void AShortFormOfEachWordIsSimilar()
    {
        var best = Match("vit d3")[0];
        Assert.Equal("vitamin-d3", best.Slug);
        Assert.Equal(CatalogMatchLabel.Similar, best.Label);
        Assert.Contains("short form", best.Reason, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("cretine", "creatine")]
    [InlineData("ashwaganda", "ashwagandha")]
    [InlineData("magnesuim", "magnesium")]
    [InlineData("semaglutdie", "semaglutide")]
    public void AMisspellingIsSimilar(string typed, string slug)
    {
        var best = Match(typed)[0];
        Assert.Equal(slug, best.Slug);
        Assert.Equal(CatalogMatchKind.Fuzzy, best.Kind);
        Assert.Equal(CatalogMatchLabel.Similar, best.Label);
    }

    [Theory]
    [InlineData("vitamin")]
    [InlineData("acid")]
    [InlineData("oil")]
    public void AGenericWordAloneOffersNothing(string typed)
    {
        Assert.DoesNotContain(Match(typed), m => m.Kind == CatalogMatchKind.Partial);
    }

    [Fact]
    public void ATruncatedNameIsSimilar()
    {
        var best = Match("semaglutid")[0];
        Assert.Equal("semaglutide", best.Slug);
        Assert.Equal(CatalogMatchLabel.Similar, best.Label);
    }

    [Fact]
    public void ANumberOrASingleLetterIsNeverGuessed()
    {
        var numbered = new List<CatalogItem>(Catalog) { new("bpc-158", "BPC-158", CompoundCategory.Peptide, []) };
        Assert.DoesNotContain(CatalogMatcher.Match("bpc 159", numbered), m => m.Kind == CatalogMatchKind.Fuzzy);
        Assert.DoesNotContain(Match("vitamin b6"), m => m.Slug == "vitamin-b12");
    }

    [Fact]
    public void DifferentLettersAreNotSimilar()
    {
        Assert.DoesNotContain(Match("vitamin d"), m => m.Slug is "vitamin-c" or "vitamin-b12");
        Assert.DoesNotContain(Match("Vitamin C"), m => m.Slug is "vitamin-d3" or "vitamin-b12");
    }

    [Theory]
    [InlineData("nac")]
    [InlineData("na")]
    [InlineData("vit")]
    public void VeryShortNamesNeverGuess(string typed)
    {
        Assert.DoesNotContain(Match(typed), m => m.Kind == CatalogMatchKind.Fuzzy);
    }

    [Theory]
    [InlineData("zzzzzzzz")]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("---")]
    public void NothingFoundIsAnEmptyList(string typed)
    {
        Assert.Empty(Match(typed));
    }

    [Fact]
    public void OfferAtMostThree_BestFirst_AndNeverTheSameItemTwice()
    {
        var crowded = new List<CatalogItem>(Catalog)
        {
            new("magnesium-oxide", "Magnesium oxide", CompoundCategory.Mineral, []),
            new("magnesium-citrate", "Magnesium citrate", CompoundCategory.Mineral, ["magnesium"]),
            new("magnesium-threonate", "Magnesium threonate", CompoundCategory.Mineral, []),
            new("magnesium-glycinate", "Magnesium glycinate", CompoundCategory.Mineral, []),
        };

        var matches = CatalogMatcher.Match("magnesium", crowded);

        Assert.Equal(3, matches.Count);
        Assert.Equal(matches.Count, matches.Select(m => m.Slug).Distinct().Count());
        Assert.Equal(CatalogMatchLabel.SameAs, matches[0].Label);
        Assert.Equal(matches.OrderBy(m => m.Label == CatalogMatchLabel.SameAs ? 0 : 1).ThenByDescending(m => m.Score).Select(m => m.Slug), matches.Select(m => m.Slug));
    }

    [Fact]
    public void TheResultDoesNotDependOnTheOrderOfTheCatalog()
    {
        var forward = CatalogMatcher.Match("magnesium glycinate", Catalog.Append(new("magnesium-glycinate-x", "Magnesium glycinate complex", CompoundCategory.Mineral, [])).ToList());
        var reversed = CatalogMatcher.Match("magnesium glycinate", Catalog.Append(new("magnesium-glycinate-x", "Magnesium glycinate complex", CompoundCategory.Mineral, [])).Reverse().ToList());

        Assert.Equal(forward.Select(m => m.Slug), reversed.Select(m => m.Slug));
    }

    [Fact]
    public void NoReasonEverNamesAnAmount()
    {
        var reasons = new[] { "Creatine", "Ozempic", "vit d3", "cretine", "Magnesium glycinate", "fish oil" }
            .SelectMany(Match).Select(m => m.Reason);

        foreach (var reason in reasons)
        {
            foreach (var word in new[] { " mg", " mcg", "dose", "daily" })
            {
                Assert.DoesNotContain(word, reason, StringComparison.OrdinalIgnoreCase);
            }
        }
    }

    [Theory]
    [InlineData("kitten", "sitting", 3)]
    [InlineData("ab", "ba", 1)]
    [InlineData("creatine", "creatine", 0)]
    public void EditDistanceCountsASwapOfNeighboursAsOne(string a, string b, int expected)
    {
        Assert.Equal(expected, CatalogMatcher.EditDistance(a, b, 5));
    }

    [Fact]
    public void KeyKeepsOnlyLettersAndDigits()
    {
        Assert.Equal("er100", CatalogMatcher.Key("ER-100"));
        Assert.Equal("er100", CatalogMatcher.Key("er 100"));
        Assert.Equal("nad", CatalogMatcher.Key("NAD+"));
        Assert.Equal("cafe", CatalogMatcher.Key("Café"));
        Assert.Equal(string.Empty, CatalogMatcher.Key(null));
    }
}
