namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.Infrastructure.Knowledge;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Scheduled periodic upsert. Runs once per Azure Container App Job invocation
/// in <see cref="RunMode.Refresh"/>. Idempotent — no-ops against unchanged
/// records. Stamps <c>ops.lastChangeType = "refresh"</c>.
///
/// Gated: every record is checked against <see cref="IPromotionGate"/> before it is
/// upserted (or, in DryRun, previewed). A record whose review decision does not resolve
/// to <c>approve-for-promotion</c> with <c>clearsSoftPromotionBlockers: true</c> is
/// skipped and reported by canonical name and reason — see
/// <see cref="ReviewDecisionPromotionGate"/>. The gate itself is constructed once at
/// startup (<see cref="Config.RefreshPromotionGateStartup"/>) and fails closed: if it
/// cannot be loaded, the worker process never reaches this job.
/// </summary>
public interface IRefreshJob : IIngestionJob { }

public sealed class RefreshJob : IngestionJobBase, IRefreshJob
{
    private readonly IPromotionGate _promotionGate;

    public RefreshJob(
        IIngestionPipeline pipeline,
        IKnowledgeSource   knowledgeSource,
        WorkerOptions      options,
        IPromotionGate     promotionGate)
        : base(pipeline, knowledgeSource, options)
    {
        _promotionGate = promotionGate ?? throw new ArgumentNullException(nameof(promotionGate));
    }

    protected override string JobName    => "RefreshJob";
    protected override string ChangeType => "refresh";

    protected override PromotionGateDecision EvaluatePromotionGate(PreparedRecord prepared)
        => _promotionGate.Evaluate(prepared.Record.Identity.CanonicalName);
}
