namespace BioStack.KnowledgeWorker.Pipeline;

/// <summary>
/// Why a compound did not clear <see cref="Jobs.RefreshJob"/>'s promotion gate, for
/// human-readable Refresh/DryRun reporting only. This enum does not redefine what
/// "promoted" means — <see cref="ReviewDecisionIndex.HasPromotionApproval"/> remains the
/// single source of truth for that decision. It only classifies *why* a record that fails
/// that test failed it, mirroring the decision vocabulary in
/// Schemas/review-decision.schema.json.
/// </summary>
public enum PromotionSkipReason
{
    /// <summary>No review decision of any kind is on file for this compound.</summary>
    NoDecision,

    /// <summary>Latest applicable decision is <c>approve-claims</c>, not <c>approve-for-promotion</c>.</summary>
    ApproveClaimsOnly,

    /// <summary>Latest applicable decision is <c>request-changes</c> (and nothing later clears it).</summary>
    RequestChanges,

    /// <summary>An <c>approve-for-promotion</c> decision exists but <c>clearsSoftPromotionBlockers</c> is false.</summary>
    BlockersNotCleared,

    /// <summary>Compound has an <c>archive-draft</c> or <c>reject</c> decision on file.</summary>
    ArchivedOrRejected,
}

/// <summary>Outcome of evaluating one compound against the promotion gate.</summary>
public sealed record PromotionGateDecision(bool Allowed, PromotionSkipReason? SkipReason)
{
    public static readonly PromotionGateDecision Approved = new(true, null);

    public static PromotionGateDecision Skip(PromotionSkipReason reason) => new(false, reason);

    /// <summary>Human-readable reason, suitable for the per-record Refresh/DryRun table.</summary>
    public string SkipReasonText => SkipReason switch
    {
        null => "approved for promotion",
        PromotionSkipReason.NoDecision => "no review decision on file",
        PromotionSkipReason.ApproveClaimsOnly => "latest decision is approve-claims only (not approve-for-promotion)",
        PromotionSkipReason.RequestChanges => "latest decision is request-changes",
        PromotionSkipReason.BlockersNotCleared => "approve-for-promotion decision exists but clearsSoftPromotionBlockers=false",
        PromotionSkipReason.ArchivedOrRejected => "compound is archived or rejected",
        _ => "unknown",
    };
}

/// <summary>
/// Consulted by <see cref="Jobs.RefreshJob"/> before every upsert (write or DryRun
/// preview alike). <see cref="AllowAllPromotionGate"/> is the explicit
/// <c>--Worker:AllowUnpromoted=true</c> override; <see cref="ReviewDecisionPromotionGate"/>
/// is the default, review-decision-backed gate.
/// </summary>
public interface IPromotionGate
{
    /// <summary>False only for <see cref="AllowAllPromotionGate"/> — the explicit override.</summary>
    bool IsEnforced { get; }

    PromotionGateDecision Evaluate(string compoundName);
}

/// <summary>
/// Default gate. Reuses <see cref="ReviewDecisionIndex.HasPromotionApproval"/> for the
/// actual promotion test and only adds a reason classification for what to report when a
/// record is skipped.
/// </summary>
public sealed class ReviewDecisionPromotionGate : IPromotionGate
{
    private readonly ReviewDecisionIndex _index;

    public ReviewDecisionPromotionGate(ReviewDecisionIndex index)
    {
        _index = index ?? throw new ArgumentNullException(nameof(index));
    }

    public bool IsEnforced => true;

    public PromotionGateDecision Evaluate(string compoundName)
    {
        if (_index.HasPromotionApproval(compoundName))
        {
            return PromotionGateDecision.Approved;
        }

        var decisions = _index.ForCompound(compoundName);
        if (decisions.Count == 0)
        {
            return PromotionGateDecision.Skip(PromotionSkipReason.NoDecision);
        }

        if (_index.IsCompoundArchived(compoundName))
        {
            return PromotionGateDecision.Skip(PromotionSkipReason.ArchivedOrRejected);
        }

        if (decisions.Any(d =>
                d.Decision.Equals("approve-for-promotion", StringComparison.OrdinalIgnoreCase)
                && !d.ClearsSoftPromotionBlockers))
        {
            return PromotionGateDecision.Skip(PromotionSkipReason.BlockersNotCleared);
        }

        if (decisions.Any(d => d.Decision.Equals("approve-claims", StringComparison.OrdinalIgnoreCase)))
        {
            return PromotionGateDecision.Skip(PromotionSkipReason.ApproveClaimsOnly);
        }

        if (_index.HasPendingRequestedChanges(compoundName))
        {
            return PromotionGateDecision.Skip(PromotionSkipReason.RequestChanges);
        }

        // Only resolve-review-items (or nothing usable) on file — treat as no decision.
        return PromotionGateDecision.Skip(PromotionSkipReason.NoDecision);
    }
}

/// <summary>
/// The explicit <c>--Worker:AllowUnpromoted=true</c> dev/local override — every compound
/// is allowed through unconditionally. <see cref="Config.RefreshPromotionGateStartup"/> is
/// responsible for refusing to construct this against a non-local connection string
/// unless <c>Worker:AcknowledgeUnpromotedProduction=true</c> is also set, and for logging
/// the override loudly before it is used.
/// </summary>
public sealed class AllowAllPromotionGate : IPromotionGate
{
    public static readonly AllowAllPromotionGate Instance = new();

    private AllowAllPromotionGate() { }

    public bool IsEnforced => false;

    public PromotionGateDecision Evaluate(string compoundName) => PromotionGateDecision.Approved;
}

/// <summary>
/// Thrown when the review-decision index cannot be established. Refresh treats this as
/// fail-closed: the caller must abort before any database connection or write is attempted.
/// </summary>
public sealed class PromotionGateLoadException : Exception
{
    public PromotionGateLoadException(string message) : base(message) { }

    public PromotionGateLoadException(string message, Exception inner) : base(message, inner) { }
}
