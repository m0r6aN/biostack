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
    string Rationale);

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
    string? Error = null);
