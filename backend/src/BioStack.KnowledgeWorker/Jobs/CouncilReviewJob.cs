namespace BioStack.KnowledgeWorker.Jobs;

using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Config;
using BioStack.KnowledgeWorker.Council;
using BioStack.KnowledgeWorker.Pipeline;

public interface ICouncilReviewJob : IIngestionJob { }

/// <summary>
/// RunMode=CouncilReview (DB-free). Sends each due record to both reviewers, then writes:
/// a schema-valid review-decision batch (consensus decisions and escalate-to-owner markers),
/// an owner escalation file with distinct options for Clint, and a known-risk log of legal items.
/// Refresh reads the decision batch: consensus approvals publish as reviewed, escalations
/// publish labeled "escalated to owner".
/// </summary>
public sealed class CouncilReviewJob : ICouncilReviewJob
{
    public const string CouncilReviewerId = "ai-council";
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private readonly WorkerOptions _worker;
    private readonly CouncilOptions _council;
    private readonly IResearchArtifactValidator _validator;
    private readonly Func<IReadOnlyList<IReviewer>> _reviewers;
    private readonly TimeProvider _clock;

    public CouncilReviewJob(WorkerOptions worker, CouncilOptions council, IResearchArtifactValidator validator,
        Func<IReadOnlyList<IReviewer>>? reviewers = null, TimeProvider? clock = null)
    {
        _worker = worker ?? throw new ArgumentNullException(nameof(worker));
        _council = council ?? throw new ArgumentNullException(nameof(council));
        _validator = validator ?? throw new ArgumentNullException(nameof(validator));
        _reviewers = reviewers ?? (() => ReviewerFactory.Create(_council));
        _clock = clock ?? TimeProvider.System;
    }

    public async Task<JobRunResult> RunAsync(IngestionContext context, CancellationToken cancellationToken = default)
    {
        IReadOnlyList<IReviewer> reviewers;
        try { reviewers = _reviewers(); }
        catch (InvalidOperationException ex) { return JobRunResult.Failure(ex.Message); }
        if (reviewers.Count != 2) return JobRunResult.Failure($"The council needs exactly two reviewers (found {reviewers.Count}).");

        var seedPath = ResolveBase(_worker.SeedFilePath);
        if (!File.Exists(seedPath)) return JobRunResult.Failure($"Seed not found at '{seedPath}'.");
        var seed = JsonNode.Parse(await File.ReadAllTextAsync(seedPath, cancellationToken)) as JsonArray
                   ?? throw new InvalidOperationException("Seed must be a JSON array.");

        var decisionDir = ResolveCwd(_council.DecisionDirectory);
        var index = LoadIndex(decisionDir);
        // Discovered records (curated + auto-discovered drafts) are reviewed first so new
        // compounds and therapies are not starved behind the 158-record seed.
        var combined = BioStack.KnowledgeWorker.Discovery.DiscoveredRecordStore.Combine(seed, DiscoveryJob.DiscoveredDirectories(_worker));
        var seedKeys = BioStack.KnowledgeWorker.Discovery.DiscoveredRecordStore.NameKeys(seed);
        var ordered = new JsonArray(combined
            .OrderBy(r => seedKeys.Contains(BioStack.KnowledgeWorker.Discovery.CandidateNames.Normalize(r?["identity"]?["canonicalName"]?.GetValue<string>())))
            .Select(r => r?.DeepClone()).ToArray());
        var items = CouncilItemSelector.Select(ordered, index, _council.MaxItemsPerRun);
        context.Logger.LogInformation("[CouncilReview] {Count} record(s) due; reviewers {A} and {B}.",
            items.Count, $"{reviewers[0].Provider}/{reviewers[0].Model}", $"{reviewers[1].Provider}/{reviewers[1].Model}");

        var outcomes = new List<CouncilOutcome>();
        foreach (var item in items)
        {
            cancellationToken.ThrowIfCancellationRequested();
            context.IncrementScanned();
            var tasks = reviewers.Select(r => Safe(r, item, cancellationToken)).ToArray();
            var results = await Task.WhenAll(tasks);
            var verdicts = results.Where(r => r.Verdict is not null).Select(r => r.Verdict!).ToList();
            var errors = results.Where(r => r.Error is not null).Select(r => r.Error!).ToList();
            CouncilOutcome outcome;
            if (errors.Count > 0)
            {
                outcome = CouncilAdjudicator.Failed(item, verdicts, string.Join(" | ", errors));
                context.IncrementFailed();
                context.Logger.LogWarning("[CouncilReview] '{Name}': reviewer failure, left pending review: {Error}", item.CompoundName, outcome.Error);
            }
            else
            {
                outcome = CouncilAdjudicator.Adjudicate(item, verdicts);
                if (outcome.Kind == CouncilOutcomeKind.Consensus) context.IncrementCreated();
                else context.IncrementFlaggedForReview();
                context.Logger.LogInformation("[CouncilReview] '{Name}': {Kind} {Decision}", item.CompoundName, outcome.Kind, outcome.ReviewDecision);
            }
            outcomes.Add(outcome);
        }

        var now = _clock.GetUtcNow();
        var stamp = now.UtcDateTime.ToString("yyyy-MM-dd'T'HH-mm-ss'Z'", CultureInfo.InvariantCulture);
        var batch = BuildDecisionBatch(outcomes, now, stamp);
        if (batch is not null)
        {
            var validation = _validator.Validate(ResearchArtifactKind.ReviewDecisionBatch, batch);
            if (!validation.IsValid)
                return JobRunResult.Failure("Council decision batch failed schema validation: " + validation.Summary());
        }

        if (context.DryRun)
        {
            context.Logger.LogInformation("[CouncilReview] DryRun — nothing written.");
            return JobRunResult.FromContext(context);
        }

        if (batch is not null)
        {
            Directory.CreateDirectory(decisionDir);
            await File.WriteAllTextAsync(Path.Combine(decisionDir, $"review-decision-batch-{stamp}-ai-council.json"),
                batch.ToJsonString(JsonOptions), cancellationToken);
        }

        var escalations = outcomes.Where(o => o.Kind == CouncilOutcomeKind.EscalateToOwner
            || o.Claims.Any(c => c.Outcome == ClaimCouncilOutcomes.Escalated)).ToList();
        if (escalations.Count > 0)
        {
            var dir = ResolveCwd(_council.EscalationDirectory);
            Directory.CreateDirectory(dir);
            await File.WriteAllTextAsync(Path.Combine(dir, $"council-escalation-{stamp}.json"),
                BuildEscalationJson(escalations, now).ToJsonString(JsonOptions), cancellationToken);
            await File.WriteAllTextAsync(Path.Combine(dir, $"council-escalation-{stamp}.md"),
                BuildEscalationMarkdown(escalations, now), cancellationToken);
        }

        var legal = outcomes.Where(o => o.LegalRisks.Count > 0).ToList();
        if (legal.Count > 0)
        {
            var dir = ResolveCwd(_council.KnownRiskDirectory);
            Directory.CreateDirectory(dir);
            var log = new JsonObject
            {
                ["recordType"] = "council-known-risk-log",
                ["loggedAt"] = Iso(now),
                ["note"] = "Legal items are logged as known risks only. They do not block or change any decision (owner ruling 5, 2026-10-24).",
                ["items"] = new JsonArray(legal.Select(o => (JsonNode?)new JsonObject
                {
                    ["compoundName"] = o.CompoundName,
                    ["risks"] = new JsonArray(o.LegalRisks.Select(r => (JsonNode?)r).ToArray()),
                }).ToArray()),
            };
            await File.WriteAllTextAsync(Path.Combine(dir, $"council-known-risks-{stamp}.json"), log.ToJsonString(JsonOptions), cancellationToken);
        }

        return JobRunResult.FromContext(context);
    }

    public static JsonObject? BuildDecisionBatch(IReadOnlyList<CouncilOutcome> outcomes, DateTimeOffset now, string stamp)
    {
        var decided = outcomes.Where(o => o.ReviewDecision is not null).ToList();
        if (decided.Count == 0) return null;
        var reviewedAt = Iso(now);
        return new JsonObject
        {
            ["schemaVersion"] = "1.1.0",
            ["recordType"] = "review-decision-batch",
            ["batch"] = new JsonObject
            {
                ["batchId"] = $"ai-council-{stamp}",
                ["reviewerId"] = CouncilReviewerId,
                ["reviewedAt"] = reviewedAt,
                ["notes"] = new JsonArray(
                    "Two-reviewer AI council. Consensus passes; disagreement escalates to the owner, Clinton Morgan.",
                    "Legal items are logged as known risks only."),
            },
            ["decisions"] = new JsonArray(decided.Select(o => (JsonNode?)new JsonObject
            {
                ["decisionId"] = $"ai-council-{stamp}-{Slug(o.CompoundName)}",
                ["compoundName"] = o.CompoundName,
                ["decision"] = o.ReviewDecision,
                ["reviewerId"] = $"{CouncilReviewerId}:{string.Join("+", o.Verdicts.Select(v => v.Model))}",
                ["reviewedAt"] = reviewedAt,
                ["scope"] = new JsonObject
                {
                    ["claimIds"] = new JsonArray(), ["qualityFlags"] = new JsonArray(),
                    ["reviewCategories"] = new JsonArray("ai-council"), ["promotionBlockers"] = new JsonArray(),
                },
                ["clearsSoftPromotionBlockers"] = o.ClearsSoftPromotionBlockers,
                ["notes"] = new JsonArray(o.Notes.Distinct().Select(n => (JsonNode?)n).ToArray()),
                ["council"] = new JsonObject
                {
                    ["outcome"] = o.Kind == CouncilOutcomeKind.Consensus ? "consensus" : "escalated-to-owner",
                    ["reviewers"] = new JsonArray(o.Verdicts.Select(v => (JsonNode?)new JsonObject
                    {
                        ["reviewerId"] = v.ReviewerId, ["provider"] = v.Provider, ["model"] = v.Model,
                        ["decision"] = v.Decision, ["proposedTier"] = v.ProposedTier, ["rationale"] = v.Rationale,
                    }).ToArray()),
                    ["ownerOptions"] = new JsonArray(o.OwnerOptions.Select(x => (JsonNode?)new JsonObject
                    {
                        ["optionId"] = x.OptionId, ["summary"] = x.Summary, ["decision"] = x.Decision,
                        ["proposedTier"] = x.ProposedTier, ["basis"] = x.Basis,
                    }).ToArray()),
                    ["legalRisks"] = new JsonArray(o.LegalRisks.Select(r => (JsonNode?)r).ToArray()),
                    ["claims"] = new JsonArray(o.Claims.Select(c => (JsonNode?)new JsonObject
                    {
                        ["claimId"] = c.ClaimId,
                        ["currentTier"] = c.CurrentTier,
                        ["outcome"] = c.Outcome,
                        ["decision"] = c.Decision,
                        ["tier"] = c.Tier,
                        ["reviewers"] = new JsonArray(c.Reviewers.Select(r => (JsonNode?)new JsonObject
                        {
                            ["reviewerId"] = r.Reviewer.ReviewerId, ["provider"] = r.Reviewer.Provider, ["model"] = r.Reviewer.Model,
                            ["decision"] = r.Verdict?.Decision, ["proposedTier"] = r.Verdict?.ProposedTier,
                            ["rationale"] = r.Verdict?.Rationale ?? string.Empty,
                        }).ToArray()),
                        ["ownerOptions"] = new JsonArray(c.OwnerOptions.Select(x => (JsonNode?)new JsonObject
                        {
                            ["optionId"] = x.OptionId, ["summary"] = x.Summary, ["decision"] = x.Decision,
                            ["proposedTier"] = x.ProposedTier, ["basis"] = x.Basis,
                        }).ToArray()),
                    }).ToArray()),
                },
            }).ToArray()),
        };
    }

    private static JsonObject BuildEscalationJson(IReadOnlyList<CouncilOutcome> escalations, DateTimeOffset now)
        => new()
        {
            ["recordType"] = "council-owner-escalation",
            ["owner"] = "Clinton Morgan",
            ["createdAt"] = Iso(now),
            ["howToDecide"] = "Pick one option per compound. Copy its decisionTemplate into a review-decision-batch file " +
                              "in the review-decisions folder (reviewerId owner:clinton-morgan, reviewedAt = now). The next Refresh applies it.",
            ["items"] = new JsonArray(escalations.Select(o => (JsonNode?)new JsonObject
            {
                ["compoundName"] = o.CompoundName,
                ["why"] = new JsonArray(o.Notes.Select(n => (JsonNode?)n).ToArray()),
                ["options"] = new JsonArray(o.OwnerOptions.Select(x => (JsonNode?)new JsonObject
                {
                    ["optionId"] = x.OptionId,
                    ["summary"] = x.Summary,
                    ["basis"] = x.Basis,
                    ["decisionTemplate"] = new JsonObject
                    {
                        ["decisionId"] = $"owner-{Slug(o.CompoundName)}-{x.OptionId.ToLowerInvariant()}",
                        ["compoundName"] = o.CompoundName,
                        ["decision"] = x.Decision,
                        ["reviewerId"] = "owner:clinton-morgan",
                        ["reviewedAt"] = "<set to now>",
                        ["scope"] = new JsonObject
                        {
                            ["claimIds"] = new JsonArray(), ["qualityFlags"] = new JsonArray(),
                            ["reviewCategories"] = new JsonArray("owner-decision"), ["promotionBlockers"] = new JsonArray(),
                        },
                        ["clearsSoftPromotionBlockers"] = x.Decision == "approve-for-promotion",
                        ["notes"] = new JsonArray($"Owner chose option {x.OptionId}: {x.Summary}"),
                    },
                }).ToArray()),
                ["legalRisksLogged"] = new JsonArray(o.LegalRisks.Select(r => (JsonNode?)r).ToArray()),
                ["claims"] = new JsonArray(o.Claims.Where(c => c.Outcome == ClaimCouncilOutcomes.Escalated).Select(c => (JsonNode?)new JsonObject
                {
                    ["claimId"] = c.ClaimId,
                    ["currentTier"] = c.CurrentTier,
                    ["options"] = new JsonArray(c.OwnerOptions.Select(x => (JsonNode?)new JsonObject
                    {
                        ["optionId"] = x.OptionId, ["summary"] = x.Summary, ["proposedTier"] = x.ProposedTier, ["basis"] = x.Basis,
                    }).ToArray()),
                    ["howToDecide"] = "If the chosen tier differs from currentTier, add a new claim version at that tier. Then set " +
                                      "ownerDecision on the claim's latest version in the seed: {\"optionId\": <letter>, \"decidedAt\": <now>, " +
                                      "\"decidedBy\": \"owner:clinton-morgan\"}. That publishes the claim at its tier on the next Seed/Refresh.",
                }).ToArray()),
            }).ToArray()),
        };

    private static string BuildEscalationMarkdown(IReadOnlyList<CouncilOutcome> escalations, DateTimeOffset now)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# AI council: decisions for Clint ({Iso(now)})").AppendLine();
        foreach (var o in escalations)
        {
            sb.AppendLine($"## {o.CompoundName}").AppendLine();
            foreach (var n in o.Notes) sb.AppendLine($"- {n}");
            sb.AppendLine();
            foreach (var x in o.OwnerOptions) sb.AppendLine($"- **Option {x.OptionId}.** {x.Summary}. _{x.Basis}_");
            foreach (var c in o.Claims.Where(c => c.Outcome == ClaimCouncilOutcomes.Escalated))
            {
                sb.AppendLine().AppendLine($"### Claim `{c.ClaimId}` (currently {c.CurrentTier})").AppendLine();
                foreach (var x in c.OwnerOptions) sb.AppendLine($"- **Option {x.OptionId}.** {x.Summary}. _{x.Basis}_");
            }
            if (o.LegalRisks.Count > 0)
            {
                sb.AppendLine().AppendLine("Known risks logged (not blocking):");
                foreach (var r in o.LegalRisks) sb.AppendLine($"- {r}");
            }
            sb.AppendLine();
        }
        return sb.ToString();
    }

    private static async Task<(ReviewerVerdict? Verdict, string? Error)> Safe(IReviewer reviewer, CouncilReviewItem item, CancellationToken ct)
    {
        try { return (await reviewer.ReviewAsync(item, ct), null); }
        catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
        catch (Exception ex) { return (null, $"{reviewer.Id} ({reviewer.Provider}/{reviewer.Model}): {ex.Message}"); }
    }

    private ReviewDecisionIndex LoadIndex(string dir)
    {
        if (!Directory.Exists(dir)) return ReviewDecisionIndex.Empty;
        var batches = Directory.EnumerateFiles(dir, "review-decision-batch-*.json")
            .Select(f => JsonNode.Parse(File.ReadAllText(f)))
            .Where(n => n is not null).Select(n => n!).ToList();
        return ReviewDecisionIndex.FromBatches(batches);
    }

    private static string Iso(DateTimeOffset t) => t.UtcDateTime.ToString("yyyy-MM-dd'T'HH:mm:ss'Z'", CultureInfo.InvariantCulture);
    private static string Slug(string v) => System.Text.RegularExpressions.Regex.Replace(v.ToLowerInvariant(), "[^a-z0-9]+", "-").Trim('-');
    private static string ResolveBase(string p) => Path.IsPathRooted(p) ? p : Path.Combine(AppContext.BaseDirectory, p);
    private static string ResolveCwd(string p) => Path.IsPathRooted(p) ? p : Path.Combine(Directory.GetCurrentDirectory(), p);
}
