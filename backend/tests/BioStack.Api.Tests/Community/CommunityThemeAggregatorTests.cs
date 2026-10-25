namespace BioStack.Api.Tests.Community;

using BioStack.Api.Endpoints;
using Microsoft.Extensions.Configuration;
using Xunit;

public sealed class CommunityThemeAggregatorTests
{
    private static readonly DateTime Now = new(2026, 10, 24, 12, 0, 0, DateTimeKind.Utc);

    private static IEnumerable<ApprovedThemeRow> People(string theme, int count, int daysAgo = 1, string prefix = "p") =>
        Enumerable.Range(0, count).Select(i => new ApprovedThemeRow(theme, $"{prefix}{i}", Now.AddDays(-daysAgo)));

    [Fact]
    public void ThemeBelowK_IsHidden_AtK_IsShown()
    {
        var (shown, n) = CommunityThemeAggregator.Aggregate(People("nausea", 9), Now, 10, 180);
        Assert.Empty(shown);
        Assert.Equal(0, n);
        (shown, n) = CommunityThemeAggregator.Aggregate(People("nausea", 10), Now, 10, 180);
        Assert.Equal("nausea", Assert.Single(shown).Id);
        Assert.Equal(10, n);
    }

    [Fact]
    public void RepeatSubmissionsFromOnePerson_CountOnce()
    {
        var rows = People("headache", 9).Concat(Enumerable.Repeat(new ApprovedThemeRow("headache", "p0", Now.AddDays(-2)), 20));
        Assert.Empty(CommunityThemeAggregator.Aggregate(rows, Now, 10, 180).Shown);
    }

    [Fact]
    public void OnlyTheWindowCounts_AndUnknownThemesAreIgnored()
    {
        var rows = People("fatigue", 5, daysAgo: 10).Concat(People("fatigue", 5, daysAgo: 200, prefix: "old"))
            .Concat(People("miracle-cure", 50));
        Assert.Empty(CommunityThemeAggregator.Aggregate(rows, Now, 10, 180).Shown);
    }

    [Fact]
    public void K_CanBeRaisedByConfig_NeverLoweredBelowTen()
    {
        IConfiguration Cfg(string? v) => new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> { ["CommunityColumns:MinCount"] = v }).Build();
        Assert.Equal(10, CommunityThemeAggregator.ResolveK(Cfg(null)));
        Assert.Equal(10, CommunityThemeAggregator.ResolveK(Cfg("1")));
        Assert.Equal(25, CommunityThemeAggregator.ResolveK(Cfg("25")));
        Assert.Equal(180, CommunityThemeAggregator.ResolveWindowDays(Cfg(null)));
    }

    [Fact]
    public void Vocabulary_HasBothKinds_AndUniqueIds()
    {
        Assert.Contains(CommunityThemeVocabulary.All, t => t.Kind == CommunityThemeVocabulary.SideEffect);
        Assert.Contains(CommunityThemeVocabulary.All, t => t.Kind == CommunityThemeVocabulary.Benefit);
        Assert.Equal(CommunityThemeVocabulary.All.Count, CommunityThemeVocabulary.All.Select(t => t.Id).Distinct().Count());
        Assert.DoesNotMatch(@"\d", string.Join(' ', CommunityThemeVocabulary.All.Select(t => t.Label)));
    }
}
