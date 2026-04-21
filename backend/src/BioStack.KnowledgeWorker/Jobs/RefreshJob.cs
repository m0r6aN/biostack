namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.Infrastructure.Knowledge;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Scheduled periodic upsert. Runs once per Azure Container App Job invocation
/// in <see cref="RunMode.Refresh"/>. Idempotent — no-ops against unchanged
/// records. Stamps <c>ops.lastChangeType = "refresh"</c>.
/// </summary>
public interface IRefreshJob : IIngestionJob { }

public sealed class RefreshJob : IngestionJobBase, IRefreshJob
{
    public RefreshJob(
        IIngestionPipeline pipeline,
        IKnowledgeSource   knowledgeSource,
        WorkerOptions      options)
        : base(pipeline, knowledgeSource, options) { }

    protected override string JobName    => "RefreshJob";
    protected override string ChangeType => "refresh";
}
