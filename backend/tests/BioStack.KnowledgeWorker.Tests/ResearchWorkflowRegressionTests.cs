namespace BioStack.KnowledgeWorker.Tests;

using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class ResearchWorkflowRegressionTests
{
    [Fact]
    public void RequestedCompound_RemediationDecision_CarriesIntoInitialResearchTask()
    {
        var requests = RequestIndex("Alcohol", "research-alcohol-001");
        var decisions = DecisionIndex("Alcohol", "request-changes", "apply-remediation-alcohol-resolution-1", "alcohol-resolution-1", "perform-initial-research");

        var (summary, manifest, plan, queue) = BuildWorkflow(new JsonArray(), requests, decisions);

        var compound = Assert.Single(summary.Compounds);
        Assert.Equal("research-requested", compound.PromotionReadiness);
        Assert.True(compound.HasRequestedChanges);
        Assert.Equal(new[] { "research-alcohol-001" }, compound.ResearchRequestIds);
        Assert.Equal(new[] { "alcohol-resolution-1" }, compound.RequestedRemediationPlanItemIds);
        Assert.Equal(new[] { "apply-remediation-alcohol-resolution-1" }, compound.ReviewDecisionIds);
        Assert.Single(manifest.ResearchRequested);
        Assert.Contains(plan.Items, item => item.ItemId == "alcohol-resolution-1" && item.ResolutionType == "perform-initial-research");

        var task = Assert.Single(queue.Items);
        Assert.Equal("generate-evidence-packet", task.TaskType);
        Assert.Equal("research/input/evidence/alcohol.evidence.json", task.TargetEvidencePath);
        Assert.Equal(new[] { "alcohol-resolution-1" }, task.RemediationPlanItemIds);
        Assert.Contains(task.Notes, note => note.Contains("Continue remediation item alcohol-resolution-1", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(task.SuggestedResearchDirectives, directive => directive.Contains("Original remediation action", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void EvidenceAppears_RequestedCompound_MovesOutOfInitialResearchQueue()
    {
        var requests = RequestIndex("Alcohol", "research-alcohol-001");
        var drafts = new JsonArray(Draft("Alcohol", "Limited", "partial", needsReview: true));

        var (_, _, _, queue) = BuildWorkflow(drafts, requests, ReviewDecisionIndex.Empty);

        Assert.DoesNotContain(queue.Items, item => item.TaskType == "generate-evidence-packet");
        var resolved = Assert.Single(queue.ResolvedItems);
        Assert.Equal("Alcohol", resolved.CompoundName);
        Assert.Equal("evidence-detected", resolved.Resolution);
        Assert.Equal("review-required", resolved.CurrentReadiness);
    }

    [Fact]
    public void SourceExpansion_ScopesToSelectedRemediationPlanItem()
    {
        var drafts = new JsonArray(Draft("Test Compound", "Limited", "partial", needsReview: true, sourceFamilies: new[] { "database" }));
        var decisions = DecisionIndex("Test Compound", "request-changes", "apply-test-compound-resolution-2", "test-compound-resolution-2", "expand-evidence-packet");

        var (_, _, plan, queue) = BuildWorkflow(drafts, ResearchRequestIndex.Empty, decisions, reviewSourceExpansionLimit: 3);

        Assert.Contains(plan.Items, item => item.ItemId == "test-compound-resolution-2");
        var selectedPlanItem = Assert.Single(plan.Items, item => item.ItemId == "test-compound-resolution-2");
        var task = Assert.Single(queue.Items, item => item.TaskType == "expand-review-sources");
        Assert.Equal(new[] { "test-compound-resolution-2" }, task.RemediationPlanItemIds);
        Assert.Equal(new[] { selectedPlanItem.ResolutionType }, task.RemediationResolutionTypes);
        Assert.DoesNotContain(task.RemediationPlanItemIds, id => id.Equals("test-compound-resolution-1", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void ClosingDecision_ClearsPendingRequestedRemediation()
    {
        var decisions = ReviewDecisionIndex.FromBatches(new[] { JsonNode.Parse("""
        {
          "schemaVersion": "1.0.0",
          "recordType": "review-decision-batch",
          "batch": { "batchId": "b1", "reviewerId": "r1", "reviewedAt": "2026-05-10T00:00:00Z", "notes": [] },
          "decisions": [
            {
              "decisionId": "apply-remediation-alcohol-resolution-1",
              "compoundName": "Alcohol",
              "decision": "request-changes",
              "reviewerId": "r1",
              "reviewedAt": "2026-05-10T00:00:00Z",
              "scope": { "claimIds": [], "reviewQueueItemIds": [], "qualityFlags": [], "reviewCategories": [], "promotionBlockers": [], "remediationPlanItemIds": ["alcohol-resolution-1"], "remediationResolutionTypes": ["perform-initial-research"] },
              "clearsSoftPromotionBlockers": false,
              "expiresAt": null,
              "notes": []
            },
            {
              "decisionId": "approve-alcohol-001",
              "compoundName": "Alcohol",
              "decision": "approve-for-promotion",
              "reviewerId": "r1",
              "reviewedAt": "2026-05-11T00:00:00Z",
              "scope": { "claimIds": [], "reviewQueueItemIds": [], "qualityFlags": [], "reviewCategories": [], "promotionBlockers": [] },
              "clearsSoftPromotionBlockers": true,
              "expiresAt": null,
              "notes": []
            }
          ]
        }
        """)! });

        Assert.False(decisions.HasPendingRequestedChanges("Alcohol"));
        Assert.Empty(decisions.PendingRequestedRemediationPlanItemIds("Alcohol"));
    }

    [Fact]
    public void DuplicateDecisionIds_AreDistinctInSummaryOutput()
    {
        var requests = RequestIndex("Alcohol", "research-alcohol-001");
        var decisions = ReviewDecisionIndex.FromBatches(new[] { JsonNode.Parse("""
        {
          "schemaVersion": "1.0.0",
          "recordType": "review-decision-batch",
          "batch": { "batchId": "b1", "reviewerId": "r1", "reviewedAt": "2026-05-10T00:00:00Z", "notes": [] },
          "decisions": [
            {
              "decisionId": "apply-remediation-alcohol-resolution-1",
              "compoundName": "Alcohol",
              "decision": "request-changes",
              "reviewerId": "r1",
              "reviewedAt": "2026-05-10T00:00:00Z",
              "scope": { "claimIds": [], "reviewQueueItemIds": [], "qualityFlags": [], "reviewCategories": [], "promotionBlockers": [], "remediationPlanItemIds": ["alcohol-resolution-1"], "remediationResolutionTypes": ["perform-initial-research"] },
              "clearsSoftPromotionBlockers": false,
              "expiresAt": null,
              "notes": []
            },
            {
              "decisionId": "apply-remediation-alcohol-resolution-1",
              "compoundName": "Alcohol",
              "decision": "request-changes",
              "reviewerId": "r1",
              "reviewedAt": "2026-05-10T01:00:00Z",
              "scope": { "claimIds": [], "reviewQueueItemIds": [], "qualityFlags": [], "reviewCategories": [], "promotionBlockers": [], "remediationPlanItemIds": ["alcohol-resolution-1"], "remediationResolutionTypes": ["perform-initial-research"] },
              "clearsSoftPromotionBlockers": false,
              "expiresAt": null,
              "notes": []
            }
          ]
        }
        """)! });

        var (summary, _, _, _) = BuildWorkflow(new JsonArray(), requests, decisions);

        Assert.Equal(new[] { "apply-remediation-alcohol-resolution-1" }, summary.Compounds.Single().ReviewDecisionIds);
    }

    private static (ResearchSummary Summary, PromotionManifest Manifest, ReviewResolutionPlan Plan, ResearchTaskQueue Queue) BuildWorkflow(
        JsonArray drafts,
        ResearchRequestIndex requests,
        ReviewDecisionIndex decisions,
        int reviewSourceExpansionLimit = 0)
    {
        var summary = new ResearchSummaryBuilder().Build(drafts, Array.Empty<ResearchReviewQueueItem>(), decisions, requests);
        var manifest = new PromotionManifestBuilder().Build(summary, new PromotionManifestOutputs(
            DraftSubstances: "draft-substances.json",
            ReviewQueue: "review-queue.json",
            ResearchSummary: "research-summary.json",
            RunReport: "research-run-report.json"));
        var plan = new ReviewResolutionPlanBuilder().Build(manifest, Array.Empty<ResearchReviewQueueItem>());
        var queue = new ResearchTaskQueueBuilder().Build(summary, requests, "research/input/evidence", reviewSourceExpansionLimit, plan);
        return (summary, manifest, plan, queue);
    }

    private static ResearchRequestIndex RequestIndex(string compoundName, string requestId)
        => ResearchRequestIndex.FromBatches(new[] { JsonNode.Parse($$"""
        {
          "schemaVersion": "1.0.0",
          "recordType": "research-request-batch",
          "batch": { "batchId": "rb1", "requesterId": "r1", "requestedAt": "2026-05-10T00:00:00Z", "notes": [] },
          "requests": [{
            "requestId": "{{requestId}}",
            "compoundName": "{{compoundName}}",
            "aliases": [],
            "categories": [],
            "classification": "Other",
            "priority": "normal",
            "requesterId": "r1",
            "requestedAt": "2026-05-10T00:00:00Z",
            "rationale": "List interaction and efficacy effects.",
            "notes": []
          }]
        }
        """)! });

    private static ReviewDecisionIndex DecisionIndex(string compoundName, string decision, string decisionId, string remediationId, string resolutionType)
        => ReviewDecisionIndex.FromBatches(new[] { JsonNode.Parse($$"""
        {
          "schemaVersion": "1.0.0",
          "recordType": "review-decision-batch",
          "batch": { "batchId": "b1", "reviewerId": "r1", "reviewedAt": "2026-05-10T00:00:00Z", "notes": [] },
          "decisions": [{
            "decisionId": "{{decisionId}}",
            "compoundName": "{{compoundName}}",
            "decision": "{{decision}}",
            "reviewerId": "r1",
            "reviewedAt": "2026-05-10T00:00:00Z",
            "scope": { "claimIds": [], "reviewQueueItemIds": [], "qualityFlags": ["research-requested"], "reviewCategories": [], "promotionBlockers": [], "remediationPlanItemIds": ["{{remediationId}}"], "remediationResolutionTypes": ["{{resolutionType}}"] },
            "clearsSoftPromotionBlockers": false,
            "expiresAt": null,
            "notes": []
          }]
        }
        """)! });

    private static JsonObject Draft(string name, string tier, string completeness, bool needsReview, IReadOnlyList<string>? sourceFamilies = null)
        => new()
        {
            ["identity"] = new JsonObject { ["canonicalName"] = name, ["classification"] = "Other" },
            ["evidence"] = new JsonObject { ["overallTier"] = tier },
            ["provenance"] = new JsonObject { ["sourceRecords"] = ToSourceRecords(sourceFamilies ?? Array.Empty<string>()) },
            ["ops"] = new JsonObject
            {
                ["completeness"] = completeness,
                ["needsReview"] = needsReview,
                ["qualityFlags"] = new JsonArray(),
                ["reviewReasons"] = new JsonArray()
            }
        };

    private static JsonArray ToSourceRecords(IEnumerable<string> sourceFamilies)
    {
        var records = new JsonArray();
        foreach (var family in sourceFamilies)
        {
            records.Add(new JsonObject { ["sourceType"] = family });
        }
        return records;
    }
}