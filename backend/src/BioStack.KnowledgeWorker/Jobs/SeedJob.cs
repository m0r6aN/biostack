namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.Infrastructure.Knowledge;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// One-shot initial ingestion. Runs once per Azure Container App Job invocation
/// in <see cref="RunMode.Seed"/>. Intended for first deployment / environment
/// promotion. Stamps <c>ops.lastChangeType = "seed"</c>.
/// </summary>
public interface ISeedJob : IIngestionJob { }

public sealed class SeedJob : IngestionJobBase, ISeedJob
{
    public SeedJob(
        IIngestionPipeline pipeline,
        IKnowledgeSource   knowledgeSource,
        WorkerOptions      options)
        : base(pipeline, knowledgeSource, options) { }

    protected override string JobName    => "SeedJob";
    protected override string ChangeType => "seed";
}
