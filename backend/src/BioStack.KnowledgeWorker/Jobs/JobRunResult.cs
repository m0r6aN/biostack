namespace BioStack.KnowledgeWorker.Jobs;

/// <summary>
/// Immutable result returned by every <see cref="IIngestionJob"/> implementation.
/// </summary>
public sealed record JobRunResult(
    bool   Success,
    int    ScannedCount,
    int    CreatedCount,
    int    UpdatedCount,
    int    UnchangedCount,
    int    FlaggedForReviewCount,
    int    FailedCount,
    string? ErrorMessage = null)
{
    /// <summary>Builds a result from the terminal state of an <see cref="IngestionContext"/>.</summary>
    public static JobRunResult FromContext(IngestionContext ctx) => new(
        Success:              ctx.FailedCount == 0,
        ScannedCount:         ctx.ScannedCount,
        CreatedCount:         ctx.CreatedCount,
        UpdatedCount:         ctx.UpdatedCount,
        UnchangedCount:       ctx.UnchangedCount,
        FlaggedForReviewCount: ctx.FlaggedForReviewCount,
        FailedCount:          ctx.FailedCount);

    /// <summary>Represents a run that failed before producing any results.</summary>
    public static JobRunResult Failure(string error) => new(
        Success:              false,
        ScannedCount:         0,
        CreatedCount:         0,
        UpdatedCount:         0,
        UnchangedCount:       0,
        FlaggedForReviewCount: 0,
        FailedCount:          1,
        ErrorMessage:         error);

    /// <summary>Represents a no-op dry-run or empty-catalog result.</summary>
    public static JobRunResult NoOp() => new(
        Success:              true,
        ScannedCount:         0,
        CreatedCount:         0,
        UpdatedCount:         0,
        UnchangedCount:       0,
        FlaggedForReviewCount: 0,
        FailedCount:          0);
}
