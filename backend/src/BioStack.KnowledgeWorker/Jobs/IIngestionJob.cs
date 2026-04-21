namespace BioStack.KnowledgeWorker.Jobs;

/// <summary>
/// Implemented by all first-class ingestion job types: SeedJob, RefreshJob, ScopedJob.
/// Jobs are executed within an <see cref="IngestionContext"/> that carries counters,
/// dry-run state, and the scoped logger for the run.
/// </summary>
public interface IIngestionJob
{
    Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default);
}
