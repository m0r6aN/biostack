namespace BioStack.Application.Tests.Services;
using System.Text.Json;
using System.Text.Json.Serialization;
using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.Infrastructure.Repositories;
using Moq;
using Xunit;

public sealed class CompoundCreateBoundaryTests
{
    private static CreateCompoundRequest Request(string category, string? start = "2026-09-14", string? end = "2026-09-20")
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return JsonSerializer.Deserialize<CreateCompoundRequest>(JsonSerializer.Serialize(new { name = "Fixture", category, startDate = start, endDate = end, status = "Active", notes = "", sourceType = "Manual" }), options)!;
    }

    [Theory]
    [InlineData("Nutraceutical", 9)]
    [InlineData("Other", 10)]
    public void ExistingFormCategories_DeserializeWithoutRenumbering(string category, int value)
    {
        Assert.Equal(value, (int)Request(category).Category);
        Assert.Equal(1, (int)CompoundCategory.Peptide);
        Assert.Equal(8, (int)CompoundCategory.Hormone);
    }

    [Theory]
    [InlineData("2026-09-14", "2026-09-20")]
    [InlineData("2026-09-14T03:00:00Z", null)]
    [InlineData("2026-09-14T03:00:00+02:00", null)]
    [InlineData(null, null)]
    public async Task Create_NormalizesStoredDatesAndTimelineToUtc(string? start, string? end)
    {
        var request = Request("Peptide", start, end);
        var repository = new Mock<ICompoundRecordRepository>();
        var timeline = new Mock<ITimelineEventRepository>();
        var guard = new Mock<IOwnershipGuard>();
        var feature = new Mock<IFeatureGate>();
        feature.Setup(x => x.GetLimitAsync(It.IsAny<string>(), It.IsAny<CancellationToken>())).ReturnsAsync((int?)null);
        CompoundRecord? saved = null; TimelineEvent? addedEvent = null;
        repository.Setup(x => x.AddAsync(It.IsAny<CompoundRecord>(), It.IsAny<CancellationToken>())).Callback<CompoundRecord, CancellationToken>((c, _) => saved = c).Returns(Task.CompletedTask);
        timeline.Setup(x => x.AddAsync(It.IsAny<TimelineEvent>(), It.IsAny<CancellationToken>())).Callback<TimelineEvent, CancellationToken>((e, _) => addedEvent = e).Returns(Task.CompletedTask);
        var profileId = Guid.NewGuid();
        await new CompoundService(repository.Object, timeline.Object, guard.Object, feature.Object).CreateCompoundAsync(profileId, request);
        Assert.NotNull(saved);
        Assert.Equal(profileId, saved.PersonId);
        foreach (var date in new[] { saved.StartDate, saved.EndDate, addedEvent?.OccurredAtUtc }.Where(x => x.HasValue)) Assert.Equal(DateTimeKind.Utc, date!.Value.Kind);
        var expectedStart = request.StartDate?.Kind == DateTimeKind.Local ? request.StartDate.Value.ToUniversalTime() : request.StartDate;
        Assert.Equal(expectedStart?.Ticks, saved.StartDate?.Ticks);
        Assert.Equal(request.EndDate?.Ticks, saved.EndDate?.Ticks);
        if (start is null) Assert.Null(addedEvent); else Assert.Equal(saved.StartDate, addedEvent!.OccurredAtUtc);
        guard.Verify(x => x.EnsureProfileOwnedAsync(profileId, It.IsAny<CancellationToken>()), Times.Once);
        repository.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Update_AlsoKeepsCalendarDatesUtc()
    {
        var repository = new Mock<ICompoundRecordRepository>();
        var profileId = Guid.NewGuid(); var id = Guid.NewGuid();
        var existing = new CompoundRecord { Id = id, PersonId = profileId };
        repository.Setup(x => x.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync(existing);
        var request = new UpdateCompoundRequest("Fixture", CompoundCategory.Peptide, new DateTime(2026, 9, 14), null, CompoundStatus.Paused, "", SourceType.Manual);
        await new CompoundService(repository.Object, Mock.Of<ITimelineEventRepository>(), Mock.Of<IOwnershipGuard>(), Mock.Of<IFeatureGate>()).UpdateCompoundAsync(profileId, id, request);
        Assert.Equal(new DateTime(2026, 9, 14, 0, 0, 0, DateTimeKind.Utc), existing.StartDate);
        Assert.Equal(DateTimeKind.Utc, existing.StartDate!.Value.Kind);
        Assert.Null(existing.EndDate);
    }
}
