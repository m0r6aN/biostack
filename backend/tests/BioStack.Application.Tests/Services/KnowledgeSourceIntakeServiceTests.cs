namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Contracts.Requests;
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
