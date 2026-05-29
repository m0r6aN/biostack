namespace BioStack.Application.Services;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

public interface IQueuedIntakeTranscriptResolutionService
{
    Task<TranscriptSourceMaterialResult> ResolveAsync(
        Guid intakeRequestId,
        CancellationToken cancellationToken = default);

    Task<TranscriptSourceMaterialResult> ResolveAsync(
        KnowledgeSourceIntakeRequest intakeRequest,
        CancellationToken cancellationToken = default);
}

public sealed class QueuedIntakeTranscriptResolutionService : IQueuedIntakeTranscriptResolutionService
{
    private readonly BioStackDbContext _dbContext;
    private readonly ITranscriptSourceMaterialProvider _transcriptSourceMaterialProvider;

    public QueuedIntakeTranscriptResolutionService(
        BioStackDbContext dbContext,
        ITranscriptSourceMaterialProvider transcriptSourceMaterialProvider)
    {
        _dbContext = dbContext;
        _transcriptSourceMaterialProvider = transcriptSourceMaterialProvider;
    }

    public async Task<TranscriptSourceMaterialResult> ResolveAsync(
        Guid intakeRequestId,
        CancellationToken cancellationToken = default)
    {
        var intakeRequest = await _dbContext.KnowledgeSourceIntakeRequests
            .SingleOrDefaultAsync(x => x.Id == intakeRequestId, cancellationToken);

        if (intakeRequest is null)
        {
            throw new InvalidOperationException($"Knowledge source intake request '{intakeRequestId}' was not found.");
        }

        return await ResolveAsync(intakeRequest, cancellationToken);
    }

    public async Task<TranscriptSourceMaterialResult> ResolveAsync(
        KnowledgeSourceIntakeRequest intakeRequest,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!string.Equals(intakeRequest.Status, "queued", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Only queued intake requests are supported for transcript resolution. Intake '{intakeRequest.Id}' status is '{intakeRequest.Status}'.");
        }

        if (!IsTranscriptSourceType(intakeRequest.SourceType))
        {
            throw new InvalidOperationException(
                $"Unsupported transcript source type '{intakeRequest.SourceType}' for intake '{intakeRequest.Id}'.");
        }

        var sourceReference = new TranscriptSourceReference(
            SourceType: intakeRequest.SourceType,
            SourceUrl: intakeRequest.SourceUrl);

        return await _transcriptSourceMaterialProvider.ResolveAsync(sourceReference, cancellationToken);
    }

    private static bool IsTranscriptSourceType(string sourceType)
        => string.Equals(sourceType, "video_url", StringComparison.OrdinalIgnoreCase);
}
