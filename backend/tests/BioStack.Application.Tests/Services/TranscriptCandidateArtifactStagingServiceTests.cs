namespace BioStack.Application.Tests.Services;

using BioStack.Application.Services;
using BioStack.Application.Tests.Fixtures;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Xunit;

public sealed class TranscriptCandidateArtifactStagingServiceTests : IDisposable
{
    private readonly string _dbPath;
    private readonly BioStackDbContext _dbContext;
    private readonly ITranscriptCandidateArtifactStagingService _service;

    public TranscriptCandidateArtifactStagingServiceTests()
    {
        _dbPath = Path.Combine(Path.GetTempPath(), $"biostack-transcript-candidate-staging-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<BioStackDbContext>()
            .UseSqlite($"Data Source={_dbPath}")
            .Options;

        _dbContext = new BioStackDbContext(options);
        _dbContext.Database.EnsureCreated();
        _service = new TranscriptCandidateArtifactStagingService();
    }

    [Fact]
    public void Stage_FromResolvedTranscriptMaterial_ReturnsDeterministicCandidateDescriptor()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();

        var descriptor = _service.Stage(sourceMaterial);

        Assert.Equal("transcript_source_material_candidate", descriptor.ArtifactKind);
        Assert.Equal("non_canonical", descriptor.Canonicality);
        Assert.Equal("staged_candidate", descriptor.StageStatus);
        Assert.Equal(sourceMaterial.SourceReference.SourceType, descriptor.SourceType);
        Assert.Equal(sourceMaterial.SourceReference.SourceUrl, descriptor.SourceUrl);
    }

    [Fact]
    public void Stage_IncludesSourceReferenceProviderAndSegmentSnapshotMetadata()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();

        var descriptor = _service.Stage(sourceMaterial);

        Assert.Equal(sourceMaterial.Provider, descriptor.Provider);
        Assert.Equal(sourceMaterial.RetrievedAtIsoUtc, descriptor.RetrievedAtIsoUtc);
        Assert.Equal(sourceMaterial.IsDeterministicFixture, descriptor.IsDeterministicFixture);
        Assert.Equal(sourceMaterial.Segments.Count, descriptor.SegmentCount);
        Assert.False(string.IsNullOrWhiteSpace(descriptor.SegmentSnapshotSignature));
        Assert.NotEmpty(descriptor.SourceMetadata);
        Assert.Equal(sourceMaterial.Metadata.Count, descriptor.SourceMetadata.Count);
    }

    [Fact]
    public void Stage_DoesNotSummarizeOrExtractClaimsOrSafetyOrMedicalFields()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();

        var descriptor = _service.Stage(sourceMaterial);

        Assert.DoesNotContain("summary", descriptor.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("claims", descriptor.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("safety", descriptor.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("medical", descriptor.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
        Assert.DoesNotContain("recommendation", descriptor.SourceMetadata.Keys, StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Stage_DoesNotPromoteOrWriteCanonicalKnowledge()
    {
        var beforeCount = await _dbContext.KnowledgeEntries.CountAsync();

        var sourceMaterial = Tb500TranscriptFixture.CreateResult();
        var descriptor = _service.Stage(sourceMaterial);

        Assert.NotNull(descriptor);
        var afterCount = await _dbContext.KnowledgeEntries.CountAsync();
        Assert.Equal(beforeCount, afterCount);
    }

    [Fact]
    public void Stage_IsDeterministicForSameInput()
    {
        var sourceMaterial = Tb500TranscriptFixture.CreateResult();

        var descriptorA = _service.Stage(sourceMaterial);
        var descriptorB = _service.Stage(sourceMaterial);

        Assert.Equal(descriptorA.ArtifactKind, descriptorB.ArtifactKind);
        Assert.Equal(descriptorA.Canonicality, descriptorB.Canonicality);
        Assert.Equal(descriptorA.StageStatus, descriptorB.StageStatus);
        Assert.Equal(descriptorA.SourceType, descriptorB.SourceType);
        Assert.Equal(descriptorA.SourceUrl, descriptorB.SourceUrl);
        Assert.Equal(descriptorA.Provider, descriptorB.Provider);
        Assert.Equal(descriptorA.RetrievedAtIsoUtc, descriptorB.RetrievedAtIsoUtc);
        Assert.Equal(descriptorA.IsDeterministicFixture, descriptorB.IsDeterministicFixture);
        Assert.Equal(descriptorA.SegmentCount, descriptorB.SegmentCount);
        Assert.Equal(descriptorA.SegmentSnapshotSignature, descriptorB.SegmentSnapshotSignature);
        Assert.Equal(descriptorA.SourceMetadata.Count, descriptorB.SourceMetadata.Count);

        foreach (var key in descriptorA.SourceMetadata.Keys)
        {
            Assert.True(descriptorB.SourceMetadata.ContainsKey(key));
            Assert.Equal(descriptorA.SourceMetadata[key], descriptorB.SourceMetadata[key]);
        }
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
