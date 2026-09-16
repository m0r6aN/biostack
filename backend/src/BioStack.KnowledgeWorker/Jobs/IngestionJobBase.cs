namespace BioStack.KnowledgeWorker.Jobs;

using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Pipeline;
using Microsoft.Extensions.Logging;

/// <summary>
/// Shared execution shell for both SeedJob and RefreshJob. Delegates shape to
/// <see cref="IIngestionPipeline"/> for parse/validate/normalize/gate/canonicalize,
/// then persists via <see cref="IKnowledgeSource"/> in MaxBatchSize-bounded batches
/// unless <see cref="IngestionContext.DryRun"/> is set.
/// </summary>
public abstract class IngestionJobBase : IIngestionJob
{
    protected readonly IIngestionPipeline Pipeline;
    protected readonly IKnowledgeSource   KnowledgeSource;
    protected readonly WorkerOptions      Options;

    protected IngestionJobBase(
        IIngestionPipeline pipeline,
        IKnowledgeSource   knowledgeSource,
        WorkerOptions      options)
    {
        Pipeline        = pipeline;
        KnowledgeSource = knowledgeSource;
        Options         = options;
    }

    /// <summary>Display name used in logs / result summaries.</summary>
    protected abstract string JobName { get; }

    /// <summary>
    /// Writing semantics. SeedJob uses initial-seed semantics (lastChangeType=seed);
    /// RefreshJob uses update semantics (lastChangeType=refresh/upsert). This is
    /// reflected in the record's Ops block, but both jobs write via the same upsert.
    /// </summary>
    protected abstract string ChangeType { get; }

    /// <summary>
    /// Promotion gate hook. The base implementation approves every record — SeedJob (one-shot
    /// initial bootstrap) is intentionally never gated. <see cref="RefreshJob"/> overrides
    /// this to consult its injected <see cref="Pipeline.IPromotionGate"/>.
    /// </summary>
    protected virtual PromotionGateDecision EvaluatePromotionGate(PreparedRecord prepared)
        => PromotionGateDecision.Approved;

    public async Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "[{Job}] Starting — DryRun={DryRun} MaxBatchSize={Batch} SeedFile={File}",
            JobName, context.DryRun, Options.MaxBatchSize, Options.SeedFilePath);

        PipelineResult result;
        try
        {
            result = Pipeline.Run(Options.SeedFilePath);
        }
        catch (Exception ex)
        {
            context.Logger.LogError(ex, "[{Job}] Pipeline failure — aborting before any writes.", JobName);
            return JobRunResult.Failure($"{JobName}: pipeline failure: {ex.Message}");
        }

        foreach (var rej in result.Rejected)
        {
            context.IncrementScanned();
            context.IncrementFailed();
            context.Logger.LogWarning(
                "[{Job}] REJECT idx={Idx} name='{Name}' errors={Count}: {Errors}",
                JobName, rej.SourceIndex, rej.CanonicalNameOrEmpty, rej.Errors.Count,
                string.Join(" | ", rej.Errors.Select(e => e.ToString())));
        }

        var batchSize = Math.Max(1, Options.MaxBatchSize);
        var batchIdx  = 0;

        foreach (var batch in Chunk(result.Accepted, batchSize))
        {
            batchIdx++;
            context.Logger.LogInformation(
                "[{Job}] Batch {BatchIdx}: {Size} record(s)",
                JobName, batchIdx, batch.Count);

            foreach (var prepared in batch)
            {
                if (cancellationToken.IsCancellationRequested) break;

                context.IncrementScanned();

                try
                {
                    await ApplyAsync(prepared, context, cancellationToken);
                }
                catch (Exception ex)
                {
                    context.IncrementFailed();
                    context.Logger.LogError(ex,
                        "[{Job}] WRITE FAIL idx={Idx} name='{Name}'",
                        JobName, prepared.SourceIndex, prepared.Record.Identity.CanonicalName);
                }
            }
        }

        context.LogSummary(JobName);
        return JobRunResult.FromContext(context);
    }

    private async Task ApplyAsync(PreparedRecord prepared, IngestionContext context, CancellationToken ct)
    {
        var canonicalName = prepared.Record.Identity.CanonicalName;

        // Promotion gate runs before anything else — including before DryRun's read-only
        // preview — so a skip is reported the same way whether this is a DryRun or a write.
        var gate = EvaluatePromotionGate(prepared);
        if (!gate.Allowed)
        {
            context.IncrementSkippedUnpromoted();
            context.RecordPlanRow(canonicalName, "skip-unpromoted", gate.SkipReasonText);
            context.Logger.LogInformation(
                "[{Job}] SKIP (not promoted) '{Name}': {Reason}",
                JobName, canonicalName, gate.SkipReasonText);
            return;
        }

        if (prepared.Record.Ops.NeedsReview)
        {
            context.IncrementFlaggedForReview();
        }

        if (context.DryRun)
        {
            // Read-only preview: never calls UpsertCompoundAsync, so a DryRun makes no
            // writes here. The disposition is a real comparison against the database, not
            // a placeholder — the summary counters and per-record table both reflect what
            // a live Refresh would actually do.
            var previewDisposition = await KnowledgeSource.PreviewUpsertAsync(prepared.Entry, ct);
            var action = DryRunActionLabel(previewDisposition);
            RecordDispositionCounters(context, previewDisposition);
            context.RecordPlanRow(canonicalName, action, string.Empty);
            context.Logger.LogInformation(
                "[{Job}] DRY-RUN would {Action} '{Name}' class={Class} strippedFields={Stripped}",
                JobName, action, canonicalName, prepared.TrustClass, prepared.StrippedFields.Count);
            return;
        }

        // The persistence layer returns create/update/unchanged so refresh runs can
        // honestly report no-op records instead of counting every existing row as updated.
        prepared.Record.Ops.IngestedAt     = DateTime.UtcNow;
        prepared.Record.Ops.UpdatedAt      = DateTime.UtcNow;
        prepared.Record.Ops.LastChangeType = ChangeType;

        var disposition = await KnowledgeSource.UpsertCompoundAsync(prepared.Entry, ct);
        RecordDispositionCounters(context, disposition);
        context.RecordPlanRow(canonicalName, disposition.ToString(), string.Empty);
    }

    private static void RecordDispositionCounters(IngestionContext context, KnowledgeUpsertDisposition disposition)
    {
        switch (disposition)
        {
            case KnowledgeUpsertDisposition.Created:
                context.IncrementCreated();
                break;
            case KnowledgeUpsertDisposition.Updated:
                context.IncrementUpdated();
                break;
            case KnowledgeUpsertDisposition.Unchanged:
                context.IncrementUnchanged();
                break;
        }
    }

    private static string DryRunActionLabel(KnowledgeUpsertDisposition disposition) => disposition switch
    {
        KnowledgeUpsertDisposition.Created   => "would-insert",
        KnowledgeUpsertDisposition.Updated   => "would-update",
        KnowledgeUpsertDisposition.Unchanged => "unchanged",
        _                                    => "unknown",
    };

    private static IEnumerable<List<T>> Chunk<T>(IReadOnlyList<T> source, int size)
    {
        var batch = new List<T>(size);
        foreach (var item in source)
        {
            batch.Add(item);
            if (batch.Count >= size)
            {
                yield return batch;
                batch = new List<T>(size);
            }
        }
        if (batch.Count > 0) yield return batch;
    }
}
