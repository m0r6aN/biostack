namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

public interface ISourceAcquisitionRetentionJob : IIngestionJob;

public sealed class SourceAcquisitionRetentionJob
    : ISourceAcquisitionRetentionJob
{
    private readonly WorkerOptions _options;
    private readonly ISourceAcquisitionRetentionService _retention;
    private readonly IHostEnvironment _environment;

    public SourceAcquisitionRetentionJob(
        WorkerOptions options,
        ISourceAcquisitionRetentionService retention,
        IHostEnvironment environment)
    {
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _retention =
            retention ?? throw new ArgumentNullException(nameof(retention));
        _environment =
            environment ?? throw new ArgumentNullException(nameof(environment));
    }

    public async Task<JobRunResult> RunAsync(
        IngestionContext context,
        CancellationToken cancellationToken = default)
    {
        var result = await _retention.EnforceAsync(
            _options,
            _environment.IsProduction(),
            cancellationToken);
        for (var index = 0; index < result.ScannedCount; index++)
        {
            context.IncrementScanned();
        }
        for (var index = 0; index < result.RemovedCount; index++)
        {
            context.IncrementUpdated();
        }
        for (var index = 0; index < result.QuarantinedCount; index++)
        {
            context.IncrementFlaggedForReview();
        }
        for (var index = 0; index < result.FailedCount; index++)
        {
            context.IncrementFailed();
        }
        context.LogSummary(nameof(SourceAcquisitionRetentionJob));
        return JobRunResult.FromContext(context);
    }
}
