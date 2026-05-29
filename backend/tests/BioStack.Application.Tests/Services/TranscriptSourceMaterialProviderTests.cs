namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using BioStack.Application.Tests.Services.Fakes;
using BioStack.Contracts.Requests;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class TranscriptSourceMaterialProviderTests : IDisposable
{
    private readonly string _dbPath;
    private readonly BioStackDbContext _dbContext;
    private readonly KnowledgeSourceIntakeService _intakeService;
    private readonly FakeTranscriptSourceMaterialProvider _provider;

    public TranscriptSourceMaterialProviderTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-transcript-provider-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContext = new BioStackDbContext(options);
        _dbContext.Database.EnsureCreated();

        _intakeService = new KnowledgeSourceIntakeService(_dbContext);
        _provider = new FakeTranscriptSourceMaterialProvider();
    }

    [Fact]
    public async Task QueuedTb500Intake_MapsToTranscriptResolutionInput_WithoutChangingQueuedBehavior()
    {
        var request = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: "metadata only",
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata, RequestedOutputArea.TranscriptQuality },
            ChannelOptions: null);

        var response = await _intakeService.CreateAsync(request);
        Assert.Equal("queued", response.Status);

        var queued = await _dbContext.KnowledgeSourceIntakeRequests.SingleAsync(x => x.Id == response.IntakeRequestId);
        Assert.Equal("queued", queued.Status);

        var sourceReference = new TranscriptSourceReference(
            SourceType: queued.SourceType,
            SourceUrl: queued.SourceUrl);

        var resolved = await _provider.ResolveAsync(sourceReference);

        Assert.Equal(Tb500TranscriptFixture.SourceType, resolved.SourceReference.SourceType);
        Assert.Equal(Tb500TranscriptFixture.SourceUrl, resolved.SourceReference.SourceUrl);
        Assert.NotEmpty(resolved.Segments);

        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task FakeProvider_Resolves_StaticTb500Fixture_WithDeterministicContentAndMetadata()
    {
        var resolved = await _provider.ResolveAsync(Tb500TranscriptFixture.Reference);

        Assert.Equal(Tb500TranscriptFixture.Provider, resolved.Provider);
        Assert.Equal(Tb500TranscriptFixture.RetrievedAtIsoUtc, resolved.RetrievedAtIsoUtc);
        Assert.True(resolved.IsDeterministicFixture);

        Assert.Equal(Tb500TranscriptFixture.Segments.Count, resolved.Segments.Count);
        Assert.Equal(1, resolved.Segments[0].Sequence);
        Assert.Equal(0.0, resolved.Segments[0].StartSeconds);
        Assert.Contains("TB500", resolved.Segments[0].Text, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("fixtureId", resolved.Metadata.Keys, StringComparer.Ordinal);
        Assert.Equal("tb500-static-fixture-v1", resolved.Metadata["fixtureId"]);

        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task UnknownReference_FailsDeterministically()
    {
        var unknownReference = new TranscriptSourceReference(
            SourceType: "video_url",
            SourceUrl: "https://www.youtube.com/watch?v=unknown123");

        var exception = await Assert.ThrowsAsync<TranscriptSourceMaterialProviderException>(
            () => _provider.ResolveAsync(unknownReference));

        Assert.Equal("transcript_source_not_found", exception.Failure.Code);
        Assert.Contains("No deterministic transcript fixture exists", exception.Failure.Message, StringComparison.Ordinal);
        Assert.False(_provider.NetworkAttempted);
    }

    [Fact]
    public async Task TranscriptResolution_DoesNotWriteCanonicalKnowledge_AndProducesNoExtractionCandidateOrSafetyOutputs()
    {
        var initialKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();

        var intakeRequest = new AdminKnowledgeSourceIntakeRequest(
            SourceType: KnowledgeSourceType.VideoUrl,
            SourceUrl: Tb500TranscriptFixture.SourceUrl,
            OptionalInstructions: null,
            RequestedOutputs: new[] { RequestedOutputArea.SourceMetadata },
            ChannelOptions: null);

        var intakeResponse = await _intakeService.CreateAsync(intakeRequest);
        Assert.Equal("queued", intakeResponse.Status);

        var resolved = await _provider.ResolveAsync(Tb500TranscriptFixture.Reference);

        var currentKnowledgeCount = await _dbContext.KnowledgeEntries.CountAsync();
        Assert.Equal(initialKnowledgeCount, currentKnowledgeCount);

        Assert.NotNull(resolved);
        Assert.NotEmpty(resolved.Segments);

        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("candidate", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("safety", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(resolved.Metadata.Keys, key => key.Contains("classification", StringComparison.OrdinalIgnoreCase));

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
