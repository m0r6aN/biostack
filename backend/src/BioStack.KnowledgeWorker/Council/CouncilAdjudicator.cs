namespace BioStack.KnowledgeWorker.Council;

using System.Text.Json.Nodes;

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
        => AdjudicateRecord(item, verdicts) with { Claims = AdjudicateClaims(item, verdicts) };

    /// <summary>
    /// Claim-level sign-off (growth spec Phase 1, owner ruling: no human approval gates).
    /// Both reviewers approving at the claim's own tier is consensus and publishes the claim;
    /// both requesting changes at the same tier is consensus to hold it; anything else
    /// escalates to the owner with distinct options. A claim a reviewer skipped is incomplete
    /// and goes back to the council next run.
    /// </summary>
    public static IReadOnlyList<ClaimCouncilOutcome> AdjudicateClaims(CouncilReviewItem item, IReadOnlyList<ReviewerVerdict> verdicts)
    {
        if (verdicts.Count != 2) throw new ArgumentException("The council needs exactly two verdicts.", nameof(verdicts));
        var claims = (item.Payload["evidence"] as JsonObject)?["publicClaims"] as JsonArray;
        if (claims is null) return Array.Empty<ClaimCouncilOutcome>();
        var outcomes = new List<ClaimCouncilOutcome>();
        foreach (var claim in claims.OfType<JsonObject>())
        {
            var id = claim["id"]?.GetValue<string>() ?? string.Empty;
            var tier = claim["tier"]?.GetValue<string>() ?? string.Empty;
            if (id.Length == 0) continue;
            var pairs = verdicts.Select(v => (Reviewer: v, Verdict: v.Claims.FirstOrDefault(c => c.ClaimId == id))).ToList();
            if (pairs.Any(p => p.Verdict is null))
            {
                outcomes.Add(new ClaimCouncilOutcome(id, tier, ClaimCouncilOutcomes.Incomplete, null, null, pairs, Array.Empty<OwnerOption>()));
                continue;
            }
            var (a, b) = (pairs[0].Verdict!, pairs[1].Verdict!);
            var same = a.Decision == b.Decision && a.ProposedTier == b.ProposedTier;
            if (same && a.Decision == CouncilDecisions.Approve && a.ProposedTier == tier)
            {
                outcomes.Add(new ClaimCouncilOutcome(id, tier, ClaimCouncilOutcomes.Consensus, CouncilDecisions.Approve, tier, pairs, Array.Empty<OwnerOption>()));
                continue;
            }
            if (same && a.Decision == CouncilDecisions.RequestChanges)
            {
                outcomes.Add(new ClaimCouncilOutcome(id, tier, ClaimCouncilOutcomes.Consensus, CouncilDecisions.RequestChanges, a.ProposedTier, pairs, Array.Empty<OwnerOption>()));
                continue;
            }

            var options = new List<OwnerOption>();
            void Add(string summary, string decision, string? proposed, string basis)
            {
                if (options.Any(o => o.Decision == decision && o.ProposedTier == proposed)) return;
                options.Add(new OwnerOption(((char)('A' + options.Count)).ToString(), summary, decision, proposed, basis));
            }
            foreach (var (reviewer, verdict) in pairs)
                Add($"Side with {reviewer.ReviewerId} ({reviewer.Model}): {verdict!.Decision} at {verdict.ProposedTier}",
                    Map(verdict.Decision), verdict.ProposedTier,
                    string.IsNullOrWhiteSpace(verdict.Rationale) ? "(no rationale given)" : verdict.Rationale);
            Add("Keep the claim NOT YET GRADED and send it back to research", "request-changes", tier,
                "Neither position is adopted; the claim stays visible without a verdict.");
            outcomes.Add(new ClaimCouncilOutcome(id, tier, ClaimCouncilOutcomes.Escalated, null, null, pairs, options));
        }
        return outcomes;
    }

    private static CouncilOutcome AdjudicateRecord(CouncilReviewItem item, IReadOnlyList<ReviewerVerdict> verdicts)
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
