namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class KnowledgeSourceIntakeServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly BioStackDbContext _dbContext;
    private readonly KnowledgeSourceIntakeService _service;

    public KnowledgeSourceIntakeServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-intake-service-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContext = new BioStackDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new KnowledgeSourceIntakeService(_dbContext);
    }

    [Fact]
    public async Task CreateAsync_VideoUrlHappyPath_PersistsQueuedIntake()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: " Focus on compounds ",
            RequestedOutputs: new[]
            {
                RequestedOutputArea.Claims,
                RequestedOutputArea.CompoundsMentioned,
                RequestedOutputArea.SafetyFlags,
            },
            ChannelOptions: null);

        var response = await _service.CreateAsync(request);

        Assert.Equal("queued", response.Status);

        var entity = await _dbContext.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == response.IntakeRequestId);
        Assert.Equal("video_url", entity.SourceType);
        Assert.Equal("https://www.youtube.com/watch?v=SpzHHYvCNGU", entity.SourceUrl);
        Assert.Equal("Focus on compounds", entity.OptionalInstructions);
        Assert.Contains("claims", entity.RequestedOutputs);
        Assert.Contains("compounds_mentioned", entity.RequestedOutputs);
        Assert.Contains("safety_flags", entity.RequestedOutputs);
        Assert.Equal("queued", entity.Status);
    }

    [Fact]
    public async Task CreateAsync_ChannelUrlHappyPath_PersistsChannelFilters()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata, RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 25,
                PublishedAfterUtc: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PublishedBeforeUtc: new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero)));

        var response = await _service.CreateAsync(request);

        Assert.Equal("queued", response.Status);

        var entity = await _dbContext.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == response.IntakeRequestId);
        Assert.Equal("channel_url", entity.SourceType);
        Assert.Equal(25, entity.MaxVideos);
        Assert.Equal(new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero), entity.PublishedAfterUtc);
        Assert.Equal(new DateTimeOffset(2025, 12, 31, 0, 0, 0, TimeSpan.Zero), entity.PublishedBeforeUtc);
        Assert.Equal("queued", entity.Status);
    }

    [Fact]
    public async Task CreateAsync_InvalidVideoUrl_Throws()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidChannelUrl_Throws()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_MismatchedSourceTypeUrl_Throws()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/channel/UC2D2CMWXMOVWx7giW1n3LIg",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_MaxVideosOutOfBounds_Throws()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 999,
                PublishedAfterUtc: null,
                PublishedBeforeUtc: null));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_InvalidDateRange_Throws()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 5,
                PublishedAfterUtc: new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
                PublishedBeforeUtc: new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.Zero)));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_OnlyWritesIntakeRecords_NoCanonicalKnowledgeWrites()
    {
        var initialKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://youtu.be/SpzHHYvCNGU",
            OptionalInstructions: "Track compounds",
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _service.CreateAsync(request);

        Assert.Equal("queued", response.Status);
        var currentKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();
        Assert.Equal(initialKnowledgeCount, currentKnowledgeCount);

        var intake = await _dbContext.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == response.IntakeRequestId);
        Assert.Equal("queued", intake.Status);
        Assert.Null(intake.FailureReason);
    }

    [Fact]
    public async Task CreateAsync_DuplicateQueuedRequest_ReturnsExistingIntakeAndMarksDeduplicated()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var firstResponse = await _service.CreateAsync(request);
        Assert.False(firstResponse.Deduplicated);

        var secondResponse = await _service.CreateAsync(request);

        Assert.Equal(firstResponse.IntakeRequestId, secondResponse.IntakeRequestId);
        Assert.True(secondResponse.Deduplicated);

        var count = await _dbContext.KnowledgeSourceIntakeRequests
            .CountAsync(x => x.SourceUrl == "https://www.youtube.com/watch?v=SpzHHYvCNGU");
        Assert.Equal(1, count);
    }

    [Fact]
    public async Task CreateAsync_PreExistingDuplicateQueuedRows_DeduplicatesToOldestWithoutThrowing()
    {
        const string sourceUrl = "https://www.youtube.com/watch?v=SpzHHYvCNGU";
        var oldest = new KnowledgeSourceIntakeRequest
        {
            Id = Guid.NewGuid(),
            SourceType = "video_url",
            SourceUrl = sourceUrl,
            RequestedOutputs = new List<string> { "claims" },
            Status = "queued",
            CreatedAtUtc = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero),
        };
        var newer = new KnowledgeSourceIntakeRequest
        {
            Id = Guid.NewGuid(),
            SourceType = "video_url",
            SourceUrl = sourceUrl,
            RequestedOutputs = new List<string> { "claims" },
            Status = "queued",
            CreatedAtUtc = new DateTimeOffset(2026, 2, 1, 0, 0, 0, TimeSpan.Zero),
        };
        await _dbContext.KnowledgeSourceIntakeRequests.AddRangeAsync(oldest, newer);
        await _dbContext.SaveChangesAsync();

        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: sourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var response = await _service.CreateAsync(request);

        Assert.Equal(oldest.Id, response.IntakeRequestId);
        Assert.True(response.Deduplicated);

        var count = await _dbContext.KnowledgeSourceIntakeRequests
            .CountAsync(x => x.SourceUrl == sourceUrl);
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task CreateAsync_DuplicateUrlWithNonQueuedExisting_CreatesNewRequestAndDoesNotDeduplicate()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=SpzHHYvCNGU",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null);

        var firstResponse = await _service.CreateAsync(request);

        var existing = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == firstResponse.IntakeRequestId);
        existing.Status = "failed";
        await _dbContext.SaveChangesAsync();

        var secondResponse = await _service.CreateAsync(request);

        Assert.False(secondResponse.Deduplicated);
        Assert.NotEqual(firstResponse.IntakeRequestId, secondResponse.IntakeRequestId);

        var count = await _dbContext.KnowledgeSourceIntakeRequests
            .CountAsync(x => x.SourceUrl == "https://www.youtube.com/watch?v=SpzHHYvCNGU");
        Assert.Equal(2, count);
    }

    public void Dispose()
    {
        _dbContext.Dispose();
        try
        {
            if (File.Exists(_dbPath))
            {
                File.Delete(_dbPath);
            }
        }
        catch (IOException)
        {
        }
    }
}
