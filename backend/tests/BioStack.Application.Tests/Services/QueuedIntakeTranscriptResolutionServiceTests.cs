namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using BioStack.Application.Tests.Services.Fakes;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class QueuedIntakeTranscriptResolutionServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly BioStackDbContext _dbContext;
    private readonly KnowledgeSourceIntakeService _intakeService;
    private readonly FakeTranscriptSourceMaterialProvider _provider;
    private readonly QueuedIntakeTranscriptResolutionService _resolver;

    public QueuedIntakeTranscriptResolutionServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-queued-intake-resolver-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContext = new BioStackDbContext(options);
        _dbContext.Database.EnsureCreated();

        _intakeService = new KnowledgeSourceIntakeService(_dbContext);
        _provider = new FakeTranscriptSourceMaterialProvider();
        _resolver = new QueuedIntakeTranscriptResolutionService(_dbContext, _provider);
    }

    [Fact]
    public async Task ResolveById_QueuedIntake_ResolvesTranscriptMaterialThroughProvider()
    {
        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null));

        var resolved = await _resolver.ResolveAsync(createResponse.IntakeRequestId);

        Assert.Equal(Tb500TranscriptFixture.SourceType, resolved.SourceReference.SourceType);
        Assert.Equal(Tb500TranscriptFixture.SourceUrl, resolved.SourceReference.SourceUrl);
        Assert.Equal(Tb500TranscriptFixture.Provider, resolved.Provider);
        Assert.NotEmpty(resolved.Segments);
        Assert.False(_provider.NetworkAttempted);

        var intake = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == createResponse.IntakeRequestId);
        Assert.Equal("resolved", intake.Status);
        Assert.Null(intake.FailureReason);
        Assert.NotNull(intake.UpdatedAtUtc);
    }

    [Fact]
    public async Task ResolveById_ProviderFailure_PersistsFailedStatusAndReason()
    {
        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: "https://www.youtube.com/watch?v=missing-fixture",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null));

        var ex = await Assert.ThrowsAsync<TranscriptSourceMaterialProviderException>(
            () => _resolver.ResolveAsync(createResponse.IntakeRequestId));

        Assert.Equal("transcript_source_not_found", ex.Failure.Code);
        var intake = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == createResponse.IntakeRequestId);
        Assert.Equal("failed", intake.Status);
        Assert.Contains("transcript_source_not_found", intake.FailureReason, StringComparison.Ordinal);
        Assert.Contains("No deterministic transcript fixture exists", intake.FailureReason, StringComparison.Ordinal);
        Assert.NotNull(intake.UpdatedAtUtc);
    }

    [Fact]
    public async Task ResolveByEntity_QueuedIntake_ResolvesTranscriptMaterialThroughProvider()
    {
        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: "transcript only",
            RequestedOutputs: new[] { RequestedOutputArea.TranscriptQuality },
            ChannelOptions: null));

        var intake = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == createResponse.IntakeRequestId);

        var resolved = await _resolver.ResolveAsync(intake);

        Assert.Equal(Tb500TranscriptFixture.Reference, resolved.SourceReference);
        Assert.True(resolved.IsDeterministicFixture);
        Assert.NotEmpty(resolved.Metadata);
        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task NonQueuedIntake_RejectsBeforeProviderCall()
    {
        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.Claims },
            ChannelOptions: null));

        var intake = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == createResponse.IntakeRequestId);

        intake.Status = "processing";
        await _dbContext.SaveChangesAsync();

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _resolver.ResolveAsync(intake));

        Assert.Contains("Only queued intake requests are supported", exception.Message, StringComparison.Ordinal);
        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task UnknownIntakeId_RejectsDeterministically()
    {
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _resolver.ResolveAsync(Guid.NewGuid()));

        Assert.Contains("was not found", exception.Message, StringComparison.Ordinal);
        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task UnsupportedSourceType_RejectsBeforeProviderCall()
    {
        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.ChannelUrl,
            SourceUrl: "https://www.youtube.com/@HubermanLab",
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: new ChannelIngestionOptions(
                MaxVideos: 1,
                PublishedAfterUtc: null,
                PublishedBeforeUtc: null)));

        var intake = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleAsync(x => x.Id == createResponse.IntakeRequestId);

        var exception = await Assert.ThrowsAsync<InvalidOperationException>(() => _resolver.ResolveAsync(intake));

        Assert.Contains("Unsupported transcript source type", exception.Message, StringComparison.Ordinal);
        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task ResolutionPath_DoesNotWriteCanonicalKnowledge()
    {
        var initialKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();

        var createResponse = await _intakeService.CreateAsync(new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null));

        var resolved = await _resolver.ResolveAsync(createResponse.IntakeRequestId);

        Assert.NotNull(resolved);
        var finalKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();
        Assert.Equal(initialKnowledgeCount, finalKnowledgeCount);
        Assert.False(_provider.NetworkAttempted);
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
