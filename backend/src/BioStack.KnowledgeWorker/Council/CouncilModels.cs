namespace BioStack.KnowledgeWorker.Council;

using System.Text.Json.Nodes;

/// <summary>What one reviewer sees about one compound.</summary>
public sealed record CouncilReviewItem(string CompoundName, string CurrentOverallTier, DateTimeOffset? LastReviewedAt, JsonObject Payload);

public static class CouncilDecisions
{
    public const string Approve = "approve";
    public const string RequestChanges = "request-changes";
    public const string Reject = "reject";
    public static readonly string[] All = { Approve, RequestChanges, Reject };
}

/// <summary>One reviewer's structured verdict.</summary>
public sealed record ReviewerVerdict(
    string ReviewerId,
    string Provider,
    string Model,
    string Decision,
    string ProposedTier,
    IReadOnlyList<string> Concerns,
    IReadOnlyList<string> LegalRisks,
    string Rationale)
{
    /// <summary>Per-claim verdicts on <c>evidence.publicClaims</c> (growth spec Phase 1).</summary>
    public IReadOnlyList<ClaimReviewVerdict> Claims { get; init; } = Array.Empty<ClaimReviewVerdict>();
}

/// <summary>One reviewer's verdict on one public claim.</summary>
public sealed record ClaimReviewVerdict(string ClaimId, string Decision, string ProposedTier, string Rationale);

public static class ClaimCouncilOutcomes
{
    public const string Consensus = "consensus";
    public const string Escalated = "escalated-to-owner";
    /// <summary>A reviewer returned no usable verdict for the claim; it is re-sent next run.</summary>
    public const string Incomplete = "incomplete";
}

/// <summary>The council's result for one public claim.</summary>
public sealed record ClaimCouncilOutcome(
    string ClaimId,
    string CurrentTier,
    string Outcome,
    string? Decision,
    string? Tier,
    IReadOnlyList<(ReviewerVerdict Reviewer, ClaimReviewVerdict? Verdict)> Reviewers,
    IReadOnlyList<OwnerOption> OwnerOptions);

public enum CouncilOutcomeKind { Consensus, EscalateToOwner, ReviewerFailed }

public sealed record OwnerOption(string OptionId, string Summary, string Decision, string? ProposedTier, string Basis);

public sealed record CouncilOutcome(
    CouncilOutcomeKind Kind,
    string CompoundName,
    string? ReviewDecision,
    bool ClearsSoftPromotionBlockers,
    IReadOnlyList<ReviewerVerdict> Verdicts,
    IReadOnlyList<OwnerOption> OwnerOptions,
    IReadOnlyList<string> LegalRisks,
    IReadOnlyList<string> Notes,
    string? Error = null)
{
    public IReadOnlyList<ClaimCouncilOutcome> Claims { get; init; } = Array.Empty<ClaimCouncilOutcome>();
}
