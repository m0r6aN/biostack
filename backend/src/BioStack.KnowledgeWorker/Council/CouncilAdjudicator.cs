namespace BioStack.KnowledgeWorker.Council;

/// <summary>
/// Turns two reviewer verdicts into a council outcome (owner ruling 5, 2026-10-24):
/// <list type="bullet">
/// <item>Consensus (same decision and same proposed tier) passes as a review decision.</item>
/// <item>Disagreement escalates to the owner, Clinton Morgan, with distinct options.</item>
/// <item>Consensus <c>reject</c> also escalates: rejecting removes a record from display, and
/// ruling 9 says show everything, so only the owner can withhold a record.</item>
/// <item>Legal items are logged as known risks and never change the outcome.</item>
/// </list>
/// </summary>
public static class CouncilAdjudicator
{
    public static CouncilOutcome Adjudicate(CouncilReviewItem item, IReadOnlyList<ReviewerVerdict> verdicts)
    {
        if (verdicts.Count != 2) throw new ArgumentException("The council needs exactly two verdicts.", nameof(verdicts));
        var (a, b) = (verdicts[0], verdicts[1]);
        var legal = verdicts.SelectMany(v => v.LegalRisks.Select(r => $"{v.ReviewerId} ({v.Model}): {r}")).Distinct().ToList();
        var notes = verdicts.Select(v => $"{v.ReviewerId} ({v.Provider}/{v.Model}): {v.Decision}, {v.ProposedTier}. {v.Rationale}".Trim()).ToList();

        var agree = a.Decision == b.Decision && a.ProposedTier == b.ProposedTier;
        if (agree && a.Decision != CouncilDecisions.Reject)
        {
            var regrade = a.Decision == CouncilDecisions.Approve && !string.Equals(a.ProposedTier, item.CurrentOverallTier, StringComparison.Ordinal);
            string decision; bool clears;
            if (a.Decision == CouncilDecisions.Approve && !regrade) { decision = "approve-for-promotion"; clears = true; }
            else
            {
                decision = "request-changes"; clears = false;
                if (regrade) notes.Add($"Council consensus: regrade overall tier from {item.CurrentOverallTier} to {a.ProposedTier}.");
            }
            notes.AddRange(verdicts.SelectMany(v => v.Concerns).Distinct().Select(c => "Concern: " + c));
            return new CouncilOutcome(CouncilOutcomeKind.Consensus, item.CompoundName, decision, clears, verdicts,
                Array.Empty<OwnerOption>(), legal, notes);
        }

        var options = new List<OwnerOption>();
        void Add(string summary, string decision, string? tier, string basis)
        {
            if (options.Any(o => o.Decision == decision && o.ProposedTier == tier)) return;
            options.Add(new OwnerOption(((char)('A' + options.Count)).ToString(), summary, decision, tier, basis));
        }
        foreach (var v in verdicts)
        {
            Add($"Side with {v.ReviewerId} ({v.Model}): {Describe(v)}", Map(v.Decision), v.ProposedTier,
                string.IsNullOrWhiteSpace(v.Rationale) ? "(no rationale given)" : v.Rationale);
        }
        Add("Keep it published as pending review and request more evidence", "request-changes", item.CurrentOverallTier,
            "Neither position is adopted; the record stays visible, labeled, and goes back to research.");

        notes.Add(agree
            ? "Escalated: both reviewers proposed reject, which would withhold the record; only the owner can withhold."
            : $"Escalated: reviewers disagree ({a.Decision}/{a.ProposedTier} vs {b.Decision}/{b.ProposedTier}).");
        return new CouncilOutcome(CouncilOutcomeKind.EscalateToOwner, item.CompoundName, "escalate-to-owner", false,
            verdicts, options, legal, notes);
    }

    public static CouncilOutcome Failed(CouncilReviewItem item, IReadOnlyList<ReviewerVerdict> verdicts, string error)
        => new(CouncilOutcomeKind.ReviewerFailed, item.CompoundName, null, false, verdicts,
            Array.Empty<OwnerOption>(), Array.Empty<string>(), Array.Empty<string>(), error);

    private static string Describe(ReviewerVerdict v) => v.Decision switch
    {
        CouncilDecisions.Approve => $"approve at {v.ProposedTier}",
        CouncilDecisions.Reject => "reject (withhold the record)",
        _ => $"request changes, overall tier {v.ProposedTier}",
    };

    private static string Map(string councilDecision) => councilDecision switch
    {
        CouncilDecisions.Approve => "approve-for-promotion",
        CouncilDecisions.Reject => "reject",
        _ => "request-changes",
    };
}
