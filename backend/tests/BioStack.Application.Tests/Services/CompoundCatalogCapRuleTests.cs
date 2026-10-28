namespace BioStack.Application.Tests.Services;

using System.Reflection;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

/// <summary>
/// Observer cap (spec v5 D4/D6). Only a catalog-linked item whose catalog category is Supplement,
/// Vitamin, Mineral, AminoAcid or Nutraceutical is outside the cap. Everything else counts: research
/// categories, Coenzyme, and every unverified (custom) item whatever category the client sent. The
/// server derives the state; a client-sent category is never trusted.
/// </summary>
public sealed class CompoundCatalogCapRuleTests
{
    private const int Limit = 2;
    private static readonly Guid Profile = Guid.NewGuid();

    private sealed class FakeCatalog : ICatalogItemResolver
    {
        public readonly Dictionary<string, CatalogItem> Items = new(StringComparer.Ordinal)
        {
            ["vitamin-d3"] = new("vitamin-d3", "Vitamin D3", CompoundCategory.Vitamin, ["cholecalciferol"]),
            ["magnesium"] = new("magnesium", "Magnesium", CompoundCategory.Mineral, []),
            ["taurine"] = new("taurine", "Taurine", CompoundCategory.AminoAcid, []),
            ["ashwagandha"] = new("ashwagandha", "Ashwagandha", CompoundCategory.Nutraceutical, []),
            ["creatine"] = new("creatine", "Creatine", CompoundCategory.Supplement, []),
            ["coenzyme-q10"] = new("coenzyme-q10", "Coenzyme Q10", CompoundCategory.Coenzyme, []),
            ["bpc-157"] = new("bpc-157", "BPC-157", CompoundCategory.Peptide, []),
            ["ostarine"] = new("ostarine", "Ostarine", CompoundCategory.Sarm, []),
        };

        public Task<CatalogItem?> FindBySlugAsync(string slug, CancellationToken ct = default) =>
            Task.FromResult(Items.TryGetValue(KnowledgeCatalogItemResolver.Normalize(slug), out var item) ? item : null);

        public Task<IReadOnlyDictionary<string, CatalogItem>> FindBySlugsAsync(IEnumerable<string> slugs, CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyDictionary<string, CatalogItem>>(slugs
                .Select(KnowledgeCatalogItemResolver.Normalize)
                .Where(Items.ContainsKey)
                .Distinct()
                .ToDictionary(slug => slug, slug => Items[slug]));

        public Task<IReadOnlyList<CatalogItem>> ListAsync(CancellationToken ct = default) =>
            Task.FromResult<IReadOnlyList<CatalogItem>>(Items.Values.ToList());
    }

    private sealed class Harness
    {
        public readonly List<CompoundRecord> Store = new();
        public readonly FakeCatalog Catalog = new();
        public readonly CompoundService Service;

        public Harness(int? limit = Limit)
        {
            var repository = new Mock<ICompoundRecordRepository>();
            repository.Setup(x => x.GetByPersonIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(() => Store.ToList());
            repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((Guid id, CancellationToken _) => Store.FirstOrDefault(c => c.Id == id));
            repository.Setup(x => x.AddAsync(It.IsAny<CompoundRecord>(), It.IsAny<CancellationToken>()))
                .Callback<CompoundRecord, CancellationToken>((c, _) => Store.Add(c))
                .Returns(Task.CompletedTask);
            var feature = new Mock<IFeatureGate>();
            feature.Setup(x => x.GetLimitAsync(FeatureCodes.ActiveCompounds, It.IsAny<CancellationToken>())).ReturnsAsync(limit);
            Service = new CompoundService(
                repository.Object, Mock.Of<ITimelineEventRepository>(), Mock.Of<IOwnershipGuard>(), feature.Object, Catalog);
        }

        public Task<CompoundResponse> Add(
            string name, CompoundCategory category, string? slug = null, CompoundStatus status = CompoundStatus.Active) =>
            Service.CreateCompoundAsync(Profile, new CreateCompoundRequest(
                name, category, null, null, status, "", SourceType.Manual, CatalogSlug: slug));

        public Task<CompoundResponse> Update(
            Guid id, string name, CompoundCategory category, string? slug, CompoundStatus status) =>
            Service.UpdateCompoundAsync(Profile, id, new UpdateCompoundRequest(
                name, category, null, null, status, "", SourceType.Manual, CatalogSlug: slug));

        public async Task FillWithResearch(int count = Limit)
        {
            for (var i = 1; i <= count; i++)
                await Add($"Research {i}", CompoundCategory.Peptide, "bpc-157");
        }
    }

    [Theory]
    [InlineData("vitamin-d3", CompoundCategory.Vitamin)]
    [InlineData("magnesium", CompoundCategory.Mineral)]
    [InlineData("taurine", CompoundCategory.AminoAcid)]
    [InlineData("ashwagandha", CompoundCategory.Nutraceutical)]
    [InlineData("creatine", CompoundCategory.Supplement)]
    public async Task LinkedExemptCatalogCategory_IsAddableAtTheCap_AndTakesNoSlot(string slug, CompoundCategory expected)
    {
        var h = new Harness();
        await h.FillWithResearch();

        var added = await h.Add("My thing", CompoundCategory.Peptide, slug);

        Assert.Equal(expected, added.Category); // category comes from the catalog record, not the client
        Assert.Equal("catalog", added.VerificationState);
        Assert.Equal("catalog", added.CategorySource);
        Assert.Equal(slug, added.CatalogSlug);
        // and a further research item is still refused: the supplement took no slot, the research ones did
        await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("One more", CompoundCategory.Peptide, "bpc-157"));
    }

    [Theory]
    [InlineData("coenzyme-q10")] // Coenzyme counts (ruled in D10)
    [InlineData("bpc-157")]
    [InlineData("ostarine")]
    public async Task LinkedNonExemptCatalogCategory_CountsTowardTheCap(string slug)
    {
        var h = new Harness();
        await h.FillWithResearch();

        var ex = await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("Item", CompoundCategory.Supplement, slug));

        Assert.Equal("observer_active_compound_limit", ex.Code);
        Assert.Contains("research or unconfirmed", ex.Message);
    }

    [Theory]
    [InlineData(CompoundCategory.Supplement)]
    [InlineData(CompoundCategory.Vitamin)]
    [InlineData(CompoundCategory.Mineral)]
    [InlineData(CompoundCategory.AminoAcid)]
    [InlineData(CompoundCategory.Nutraceutical)]
    public async Task UnverifiedCustomItem_CountsWhateverCategoryTheClientSent(CompoundCategory forged)
    {
        var h = new Harness();
        await h.FillWithResearch();

        await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("Totally a vitamin", forged));
    }

    [Fact]
    public async Task UnverifiedItems_FillTheCap_AndAreServerLabelledUnverified()
    {
        var h = new Harness();
        var first = await h.Add("Custom A", CompoundCategory.Supplement);
        await h.Add("Custom B", CompoundCategory.Vitamin);

        Assert.Equal("unverified", first.VerificationState);
        Assert.Equal("user", first.CategorySource);
        Assert.Null(first.CatalogSlug);
        await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("Custom C", CompoundCategory.Supplement));
    }

    [Fact]
    public async Task LegacyRows_WithNoCatalogLink_StillCount_EvenWithASupplementCategory()
    {
        var h = new Harness();
        for (var i = 0; i < Limit; i++)
        {
            h.Store.Add(new CompoundRecord
            {
                Id = Guid.NewGuid(), PersonId = Profile, Name = $"Legacy {i}", Category = CompoundCategory.Supplement,
                Status = CompoundStatus.Active, VerificationState = null, CategorySource = null, CatalogSlug = null,
            });
        }

        await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("New", CompoundCategory.Peptide, "bpc-157"));
        var taking = await h.Service.GetTakingAsync(Profile);
        Assert.Equal(Limit, taking.Cap.Counted);
        Assert.All(taking.Items, item => Assert.Equal("unverified", item.Compound.VerificationState));
    }

    [Fact]
    public async Task ManyExemptItems_NeverBlock_AndDoNotConsumeResearchSlots()
    {
        var h = new Harness();
        foreach (var slug in new[] { "vitamin-d3", "magnesium", "taurine", "ashwagandha", "creatine" })
            await h.Add(slug, CompoundCategory.Other, slug);
        await h.FillWithResearch(); // both research slots still free

        var taking = await h.Service.GetTakingAsync(Profile);
        Assert.Equal(Limit, taking.Cap.Counted);
        Assert.Equal(Limit, taking.Cap.Limit);
    }

    [Fact]
    public async Task Reactivation_IsChecked_ResearchBlocked_ExemptAllowed()
    {
        var h = new Harness();
        var paused = await h.Add("Paused research", CompoundCategory.Peptide, "bpc-157", CompoundStatus.Paused);
        var pausedVitamin = await h.Add("Paused D", CompoundCategory.Peptide, "vitamin-d3", CompoundStatus.Paused);
        await h.FillWithResearch();

        await Assert.ThrowsAsync<FeatureLimitExceededException>(() =>
            h.Update(paused.Id, "Paused research", CompoundCategory.Peptide, null, CompoundStatus.Active));
        var reactivated = await h.Update(pausedVitamin.Id, "Paused D", CompoundCategory.Peptide, null, CompoundStatus.Active);
        Assert.Equal(CompoundStatus.Active, reactivated.Status);
    }

    [Fact]
    public async Task Update_ForgedCategoryOnLinkedItem_IsIgnored_CatalogCategoryWins()
    {
        var h = new Harness();
        var peptide = await h.Add("BPC", CompoundCategory.Peptide, "bpc-157");

        var updated = await h.Update(peptide.Id, "BPC", CompoundCategory.Supplement, null, CompoundStatus.Active);

        Assert.Equal(CompoundCategory.Peptide, updated.Category);
        Assert.Equal("bpc-157", updated.CatalogSlug); // null slug on update keeps the link
    }

    [Fact]
    public async Task Update_RelinkingToAnotherCatalogItem_TakesTheNewCatalogCategory()
    {
        var h = new Harness();
        var item = await h.Add("Thing", CompoundCategory.Other, "bpc-157");

        var updated = await h.Update(item.Id, "Thing", CompoundCategory.Other, "creatine", CompoundStatus.Active);

        Assert.Equal(CompoundCategory.Supplement, updated.Category);
        Assert.Equal("catalog", updated.VerificationState);
    }

    [Fact]
    public async Task Update_EmptySlug_RemovesTheLink_AndTheItemStartsCounting()
    {
        var h = new Harness();
        var supplement = await h.Add("Creatine", CompoundCategory.Other, "creatine");
        await h.FillWithResearch();

        // unlinking would make it a counted unverified item: 3 counted > limit 2
        await Assert.ThrowsAsync<FeatureLimitExceededException>(() =>
            h.Update(supplement.Id, "Creatine", CompoundCategory.Supplement, "", CompoundStatus.Active));
    }

    [Fact]
    public async Task UnknownCatalogSlug_IsRejected_AsAValidationError()
    {
        var h = new Harness();

        var ex = await Assert.ThrowsAsync<DoseValidationException>(() => h.Add("Nope", CompoundCategory.Supplement, "not-in-the-catalog"));

        Assert.Equal("catalogSlug", ex.Field);
        Assert.Empty(h.Store);
    }

    [Fact]
    public async Task LiveCatalogReclassification_IsHonoured_ResearchClassLinkedItemCounts()
    {
        var h = new Harness();
        h.Store.Add(new CompoundRecord
        {
            Id = Guid.NewGuid(), PersonId = Profile, Name = "Was supplement", Category = CompoundCategory.Supplement,
            Status = CompoundStatus.Active, CatalogSlug = "bpc-157", VerificationState = "catalog", CategorySource = "catalog",
        });
        await h.Add("Research", CompoundCategory.Peptide, "bpc-157");

        // the stale stored Supplement category is not trusted when the live record is a Peptide
        await Assert.ThrowsAsync<FeatureLimitExceededException>(() => h.Add("Another", CompoundCategory.Peptide, "bpc-157"));
    }

    [Fact]
    public async Task LinkWhoseRecordWasRetired_FallsBackToTheServerSetStoredCategory()
    {
        var h = new Harness();
        h.Store.Add(new CompoundRecord
        {
            Id = Guid.NewGuid(), PersonId = Profile, Name = "Old", Category = CompoundCategory.Supplement,
            Status = CompoundStatus.Active, CatalogSlug = "retired-slug", VerificationState = "catalog", CategorySource = "catalog",
        });
        await h.FillWithResearch();

        var taking = await h.Service.GetTakingAsync(Profile);
        Assert.Equal(Limit, taking.Cap.Counted);
    }

    [Fact]
    public async Task NoLimit_PaidTiers_NeverBlock()
    {
        var h = new Harness(limit: null);
        for (var i = 0; i < 12; i++)
            await h.Add($"Custom {i}", CompoundCategory.Peptide);

        Assert.Equal(12, h.Store.Count);
    }

    [Fact]
    public async Task Taking_OffersExactNameOrAliasMatches_ForUnlinkedItems_AndNeverLinksThem()
    {
        var h = new Harness();
        await h.Add("  vitamin   d3 ", CompoundCategory.Other);
        await h.Add("Cholecalciferol", CompoundCategory.Other, status: CompoundStatus.Paused);
        await h.Add("Something unknown", CompoundCategory.Other, status: CompoundStatus.Paused);
        await h.Add("Vitamin", CompoundCategory.Other, status: CompoundStatus.Paused); // partial: no offer, no fuzzy

        var taking = await h.Service.GetTakingAsync(Profile);

        var byName = taking.Items.ToDictionary(i => i.Compound.Name.Trim());
        Assert.Equal("vitamin-d3", Assert.Single(byName["vitamin   d3"].Suggestions).Slug);
        Assert.Equal("alias", Assert.Single(byName["Cholecalciferol"].Suggestions).MatchedOn);
        Assert.Empty(byName["Something unknown"].Suggestions);
        Assert.Empty(byName["Vitamin"].Suggestions);
        Assert.All(taking.Items, item => Assert.Null(item.Compound.CatalogSlug)); // offers only
    }

    [Fact]
    public void ClientCannotSupplyVerificationStateOrCategorySource()
    {
        foreach (var type in new[] { typeof(CreateCompoundRequest), typeof(UpdateCompoundRequest) })
        {
            var names = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Select(p => p.Name).ToHashSet();
            Assert.DoesNotContain("VerificationState", names);
            Assert.DoesNotContain("CategorySource", names);
            Assert.Contains("CatalogSlug", names); // the only client input: a pick the server validates
        }
    }

    [Fact]
    public void ExemptSet_IsExactlyTheFiveEverydayCategories()
    {
        Assert.Equal(
            new[] { CompoundCategory.Supplement, CompoundCategory.Vitamin, CompoundCategory.Mineral, CompoundCategory.AminoAcid, CompoundCategory.Nutraceutical }.OrderBy(c => c),
            ObserverCapRules.ExemptCategories.OrderBy(c => c));
        Assert.DoesNotContain(CompoundCategory.Coenzyme, ObserverCapRules.ExemptCategories);
    }

    [Fact]
    public async Task DoseAndPricingFields_AreNeverPrefilledFromTheCatalog()
    {
        // Linking copies only the category (and the slug). No dose, unit, frequency, time or amount
        // may ever be derived from a catalog record.
        var h = new Harness();
        var added = await h.Add("Vitamin D3", CompoundCategory.Other, "vitamin-d3");

        Assert.Null(added.DoseAmount);
        Assert.Null(added.DoseUnit);
        Assert.Null(added.DoseFrequency);
        Assert.Null(added.DoseTime);
        Assert.Null(added.FrequencyKind);
        Assert.Null(added.PricePaid);
    }
}
