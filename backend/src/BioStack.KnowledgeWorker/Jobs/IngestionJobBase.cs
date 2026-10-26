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

    /// <summary>
    /// Record files ingested after the seed. SeedJob ingests the seed only; RefreshJob adds
    /// discovered records (curated and auto-discovered), which still pass its promotion gate.
    /// </summary>
    protected virtual IEnumerable<string> AdditionalRecordFiles() => Array.Empty<string>();

    /// <summary>
    /// When true (RefreshJob), published records missing from the seed and discovered records
    /// are soft-retired after the upserts. SeedJob never retires.
    /// </summary>
    protected virtual bool RetiresMissingRecords => false;

    public async Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        context.Logger.LogInformation(
            "[{Job}] Starting — DryRun={DryRun} MaxBatchSize={Batch} SeedFile={File}",
            JobName, context.DryRun, Options.MaxBatchSize, Options.SeedFilePath);

        PipelineResult result;
        try
        {
            result = Pipeline.Run(Options.SeedFilePath);
            var extraFiles = AdditionalRecordFiles().ToList();
            if (extraFiles.Count > 0)
            {
                var accepted = result.Accepted.ToList();
                var rejected = result.Rejected.ToList();
                var names = new HashSet<string>(accepted.Select(a => BioStack.KnowledgeWorker.Discovery.CandidateNames.Normalize(a.Record.Identity.CanonicalName)));
                foreach (var file in extraFiles)
                {
                    var extra = Pipeline.Run(file);
                    accepted.AddRange(extra.Accepted.Where(a => names.Add(BioStack.KnowledgeWorker.Discovery.CandidateNames.Normalize(a.Record.Identity.CanonicalName))));
                    rejected.AddRange(extra.Rejected);
                }
                context.Logger.LogInformation("[{Job}] Added {Count} discovered record file(s).", JobName, extraFiles.Count);
                result = new PipelineResult(accepted, rejected);
            }
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
            context.RecordPlanRow(
                string.IsNullOrWhiteSpace(rej.CanonicalNameOrEmpty) ? $"[source index {rej.SourceIndex}]" : rej.CanonicalNameOrEmpty,
                "rejected", string.Join(" | ", rej.Errors.Select(e => e.ToString())));
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
                    context.RecordPlanRow(prepared.Record.Identity.CanonicalName,
                        context.DryRun ? "preview-failed" : "write-failed",
                        $"{ex.GetType().Name}: see the corresponding error log");
                    context.Logger.LogError(ex,
                        "[{Job}] RECORD FAIL idx={Idx} name='{Name}'",
                        JobName, prepared.SourceIndex, prepared.Record.Identity.CanonicalName);
                }
            }
        }

        if (RetiresMissingRecords && !cancellationToken.IsCancellationRequested)
        {
            await RetireMissingAsync(result, context, cancellationToken);
        }

        context.LogSummary(JobName);
        return JobRunResult.FromContext(context);
    }

    /// <summary>
    /// Soft-retires published rows whose record is no longer in the seed or discovered records.
    /// A retired name that is an alias of a current record is marked superseded by it, so
    /// lookups and redirects by the old name resolve to the successor. Fails safe: nothing is
    /// retired when the seed produced no records, when any record failed to write or preview,
    /// when a rejected record has no readable name, or when more than <see cref="WorkerOptions.MaxRetirementsPerRun"/> rows would retire.
    /// </summary>
    private async Task RetireMissingAsync(PipelineResult result, IngestionContext context, CancellationToken ct)
    {
        if (Options.MaxRetirementsPerRun <= 0)
        {
            return;
        }

        if (result.Accepted.Count == 0)
        {
            context.Logger.LogWarning("[{Job}] Retirement skipped: the seed produced no accepted records.", JobName);
            return;
        }

        if (context.FailedCount > result.Rejected.Count)
        {
            context.Logger.LogWarning("[{Job}] Retirement skipped: some records failed to write or preview in this run.", JobName);
            return;
        }

        if (result.Rejected.Any(r => string.IsNullOrWhiteSpace(r.CanonicalNameOrEmpty)))
        {
            context.Logger.LogWarning("[{Job}] Retirement skipped: a rejected record has no readable name, so its stored row cannot be told apart.", JobName);
            return;
        }

        static string Key(string name) => name.Trim().ToLowerInvariant();

        var known = new HashSet<string>(StringComparer.Ordinal);
        var successorByAlias = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var prepared in result.Accepted)
        {
            var canonical = prepared.Record.Identity.CanonicalName;
            known.Add(Key(canonical));
            foreach (var alias in prepared.Record.Identity.Aliases ?? new List<string>())
            {
                if (!string.IsNullOrWhiteSpace(alias))
                {
                    successorByAlias.TryAdd(Key(alias), canonical);
                }
            }
        }

        foreach (var rejected in result.Rejected)
        {
            known.Add(Key(rejected.CanonicalNameOrEmpty));
        }

        IReadOnlyList<KnowledgeEntryIdentity> active;
        try
        {
            active = await KnowledgeSource.ListActiveIdentitiesAsync(ct);
        }
        catch (Exception ex)
        {
            context.IncrementFailed();
            context.Logger.LogError(ex, "[{Job}] Retirement failed: could not list published records.", JobName);
            return;
        }

        var candidates = active
            .Where(a => !known.Contains(Key(a.CanonicalName)))
            .Select(a => (Name: a.CanonicalName, SupersededBy: successorByAlias.GetValueOrDefault(Key(a.CanonicalName))))
            .ToList();

        if (candidates.Count == 0)
        {
            return;
        }

        if (candidates.Count > Options.MaxRetirementsPerRun)
        {
            context.IncrementFailed();
            foreach (var c in candidates)
            {
                context.RecordPlanRow(c.Name, "retire-blocked",
                    $"{candidates.Count} records would retire; limit is {Options.MaxRetirementsPerRun} (Worker:MaxRetirementsPerRun)");
            }
            context.Logger.LogError(
                "[{Job}] Retirement blocked: {Count} published records are missing from the seed (limit {Limit}). Nothing was retired.",
                JobName, candidates.Count, Options.MaxRetirementsPerRun);
            return;
        }

        var now = DateTime.UtcNow;
        foreach (var (name, supersededBy) in candidates)
        {
            var reason = supersededBy is null
                ? "no longer in the seed or discovered records"
                : $"merged into {supersededBy}; lookups by this name resolve to it";

            if (context.DryRun)
            {
                context.IncrementRetired();
                context.RecordPlanRow(name, "would-retire", reason);
                context.Logger.LogInformation("[{Job}] DRY-RUN would retire '{Name}': {Reason}", JobName, name, reason);
                continue;
            }

            try
            {
                if (await KnowledgeSource.RetireCompoundAsync(name, supersededBy, now, ct))
                {
                    context.IncrementRetired();
                    context.RecordPlanRow(name, "retired", reason);
                    context.Logger.LogInformation("[{Job}] RETIRED '{Name}': {Reason}", JobName, name, reason);
                }
            }
            catch (Exception ex)
            {
                context.IncrementFailed();
                context.RecordPlanRow(name, "retire-failed", $"{ex.GetType().Name}: see the corresponding error log");
                context.Logger.LogError(ex, "[{Job}] RETIRE FAIL name='{Name}'", JobName, name);
            }
        }
    }

    private async Task ApplyAsync(PreparedRecord prepared, IngestionContext context, CancellationToken ct)
    {
        var canonicalName = prepared.Record.Identity.CanonicalName;

        // Promotion gate runs before anything else — including before DryRun's read-only
        // preview — so a skip is reported the same way whether this is a DryRun or a write.
        var gate = EvaluatePromotionGate(prepared);
        var planReason = string.Empty;
        if (!gate.Allowed && gate.SkipReason != PromotionSkipReason.ArchivedOrRejected)
        {
            planReason = $"published pending review: {gate.SkipReasonText}";
            // Owner ruling 9 (2026-10-24): the promotion gate no longer hides records. A
            // record without an approve-for-promotion decision is published and labeled
            // with its review state instead of being skipped. Only archive/reject withholds.
            if (prepared.Entry.Details is not null)
            {
                prepared.Entry.Details.ReviewStatus = gate.SkipReason switch
                {
                    PromotionSkipReason.RequestChanges => "changes-requested",
                    PromotionSkipReason.AwaitingOwner => "escalated-to-owner",
                    _ => "pending-review",
                };
            }
            context.IncrementPublishedPendingReview();
            context.Logger.LogInformation(
                "[{Job}] PUBLISH (pending review) '{Name}': {Reason}",
                JobName, canonicalName, gate.SkipReasonText);
        }
        else if (!gate.Allowed)
        {
            context.IncrementSkippedUnpromoted();
            context.RecordPlanRow(canonicalName, "skip-unpromoted", gate.SkipReasonText);
            context.Logger.LogInformation(
                "[{Job}] SKIP (not promoted) '{Name}': {Reason}",
                JobName, canonicalName, gate.SkipReasonText);
            return;
        }
        else if (gate.ApprovedBy is { } approver && prepared.Entry.Details is not null)
        {
            // Consensus approval from the two-reviewer AI council vs. an owner decision.
            prepared.Entry.Details.ReviewStatus = approver.StartsWith("ai-council", StringComparison.OrdinalIgnoreCase)
                ? "ai-council-reviewed"
                : "reviewed";
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
            context.RecordPlanRow(canonicalName, action, planReason);
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
        context.RecordPlanRow(canonicalName, disposition.ToString(), planReason);
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
