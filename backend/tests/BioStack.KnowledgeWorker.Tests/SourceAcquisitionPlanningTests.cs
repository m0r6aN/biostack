namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class SourceAcquisitionPlanningTests
{
    private const string ReviewedAt = "2026-07-25T13:15:00Z";

    [Fact]
    public void ActivationPolicy_PilotRegistry_Activates_Approved_RecommendedSeven()
    {
        var index = new SourceRegistryActivationPolicy().Build(LoadPilotRegistry());

        var selected = RecommendedOfficialSourcePlanningAdapters.SourceIds
            .Select(sourceId => index.BySourceId(sourceId))
            .ToList();

        Assert.All(selected, snapshot => Assert.NotNull(snapshot));
        Assert.All(selected, snapshot => Assert.True(
            snapshot!.CanAcquire,
            string.Join(Environment.NewLine, snapshot.BlockingReasons)));
        Assert.All(selected, snapshot => Assert.Empty(snapshot!.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_CurrentArtifacts_Produce_Ready_Intents()
    {
        var requests = LoadMarketInterestRequests();
        var requestCount = ResearchRequestIndex.FromBatches(new[] { requests }).All().Count();

        var plan = Build(
            requests,
            LoadSourceAuthorizationDecisions(),
            LoadPilotRegistry(),
            PilotRegistrySha256());

        Assert.Equal(requestCount * 7, plan.Intents.Count);
        Assert.Equal(plan.Intents.Count, plan.ReadyCount);
        Assert.Equal(0, plan.BlockedCount);
        Assert.All(
            plan.Intents,
            intent =>
            {
                Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition);
                Assert.Empty(intent.BlockingReasons);
            });
    }

    [Fact]
    public void PlanBuilder_Preserves_Request_And_Provenance_Lineage()
    {
        var plan = Build(
            LoadPlanningFixture(),
            LoadSourceAuthorizationDecisions(),
            LoadPilotRegistry(),
            PilotRegistrySha256());

        var intent = plan.Intents.Single(item =>
            item.SourceId == "pubchem"
            && item.RequestId == "planning-semaglutide-001");

        Assert.Equal("Semaglutide", intent.CompoundName);
        Assert.Equal(
            ["Semaglutide", "Ozempic", "Wegovy"],
            intent.SearchTerms);
        Assert.Equal("pubchem-planning-v1", intent.AdapterId);
        Assert.Equal("api", intent.CandidateMethod);
        Assert.Contains("identity", intent.AuthorizedFieldUses);
        Assert.Contains("sourceRegistryId", intent.RequiredProvenanceFields);
        Assert.Contains("sourceItemId", intent.RequiredProvenanceFields);
        Assert.Equal("2.0.0", intent.RegistrySchemaVersion);
        Assert.Equal(
            "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28",
            intent.RegistryBindingSha256);
    }

    [Fact]
    public void PlanBuilder_SyntheticAuthorized_State_Becomes_Ready_Without_Evidence_Promotion()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var validation = ResearchArtifactValidator
            .LoadFromDirectory(TestPaths.WorkerSchemaDirectory())
            .Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, decisions);
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.True(validation.IsValid, validation.Summary());
        Assert.Equal(14, plan.Intents.Count);
        Assert.True(
            plan.ReadyCount == 14,
            string.Join(Environment.NewLine, plan.Intents.SelectMany(intent => intent.BlockingReasons).Distinct()));
        Assert.Equal(0, plan.BlockedCount);
        Assert.All(plan.Intents, intent => Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition));
        Assert.All(
            decisions["sources"]!.AsArray(),
            source => Assert.Equal(
                "review-required",
                source!["approvals"]!["evidence"]!["reviewStatus"]!.GetValue<string>()));
    }

    [Theory]
    [InlineData("rights-review-status", "source-rights-not-reviewed")]
    [InlineData("rights-legal-basis", "source-rights-legal-basis-missing")]
    [InlineData("rights-allowed-uses", "source-rights-allowed-uses-missing")]
    [InlineData("rights-reviewer", "source-rights-reviewer-missing")]
    [InlineData("rights-reviewer-mismatch", "source-rights-reviewer-approval-assignee-mismatch")]
    [InlineData("rights-review-timestamp", "source-rights-review-timestamp-missing")]
    [InlineData("operations-status", "source-operations-not-approved")]
    [InlineData("operations-review-timestamp", "source-operations-review-timestamp-missing")]
    [InlineData("acquisition-enabled", "source-acquisition-disabled")]
    [InlineData("acquisition-method", "source-acquisition-method-not-approved")]
    [InlineData("api-terms-status", "source-api-terms-not-approved")]
    [InlineData("robots-policy-status", "source-robots-policy-not-approved")]
    [InlineData("refresh-mode", "source-refresh-not-active")]
    [InlineData("refresh-cadence", "source-refresh-cadence-missing")]
    [InlineData("source-name", "source-name-missing")]
    [InlineData("authorized-field-use", "source-authorized-field-use-missing")]
    [InlineData("provenance-fields", "source-provenance-fields-missing")]
    public void PlanBuilder_ActiveRegistry_Blocks_Each_Disabled_Or_Incomplete_Decision_Layer(
        string mutation,
        string expectedBlocker)
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var fda = SourceDecision(decisions, "fda");
        MutateDecisionLayer(fda, mutation);
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent =>
            {
                Assert.Equal(SourceAcquisitionDisposition.Blocked, intent.Disposition);
                Assert.Contains($"fda:{expectedBlocker}", intent.BlockingReasons);
            });
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId != "fda"),
            intent => Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition));
    }

    [Theory]
    [InlineData("product", "missing-timestamp", "product-selection-not-approved")]
    [InlineData("product", "invalid-timestamp", "product-selection-not-approved")]
    [InlineData("product", "empty-notes", "product-selection-not-approved")]
    [InlineData("product", "missing-assignee", "product-selection-not-approved")]
    [InlineData("product", "wrong-scope", "product-selection-not-approved")]
    [InlineData("product", "wrong-stage", "product-selection-not-approved")]
    [InlineData("legalRights", "missing-timestamp", "legal-rights-not-approved")]
    [InlineData("legalRights", "invalid-timestamp", "legal-rights-not-approved")]
    [InlineData("legalRights", "empty-notes", "legal-rights-not-approved")]
    [InlineData("legalRights", "missing-assignee", "legal-rights-not-approved")]
    [InlineData("legalRights", "wrong-scope", "legal-rights-not-approved")]
    [InlineData("legalRights", "wrong-stage", "legal-rights-not-approved")]
    [InlineData("securityData", "missing-timestamp", "security-data-review-required")]
    [InlineData("securityData", "invalid-timestamp", "security-data-review-required")]
    [InlineData("securityData", "empty-notes", "security-data-review-required")]
    [InlineData("securityData", "missing-assignee", "security-data-review-required")]
    [InlineData("securityData", "wrong-scope", "security-data-review-required")]
    [InlineData("securityData", "wrong-stage", "security-data-review-required")]
    public void PlanBuilder_Incomplete_Approval_Metadata_Blocks_Affected_Source(
        string approvalName,
        string mutation,
        string expectedBlocker)
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var fda = SourceDecision(decisions, "fda");
        var approval = fda["approvals"]![approvalName]!;

        if (approvalName == "securityData")
        {
            fda["securityDataTriggersDetected"] =
                new JsonArray("untrusted-bulk-archive-or-parser");
            approval["reviewStatus"] = "reviewed";
            approval["decision"] = "approved-with-controls";
            approval["decidedAtUtc"] = ReviewedAt;
            approval["decisionNotes"] =
                new JsonArray("Synthetic triggered-security approval for planning tests.");
        }

        switch (mutation)
        {
            case "missing-timestamp":
                approval["decidedAtUtc"] = null;
                break;
            case "invalid-timestamp":
                approval["decidedAtUtc"] = "not-a-timestamp";
                break;
            case "empty-notes":
                approval["decisionNotes"] = new JsonArray();
                break;
            case "missing-assignee":
                approval["assigneeName"] = null;
                break;
            case "wrong-scope":
                approval["decisionScope"] = approvalName == "product"
                    ? "legal-rights"
                    : "product-capability";
                break;
            case "wrong-stage":
                approval["blockingStage"] = "canonical-claim-promotion";
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown mutation.");
        }
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent => Assert.Contains($"fda:{expectedBlocker}", intent.BlockingReasons));
    }

    [Theory]
    [InlineData("decision")]
    [InlineData("decidedAtUtc")]
    public void PlanBuilder_No_Trigger_Security_NotApplicable_Requires_Null_Decision_And_Timestamp(
        string mutation)
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var securityApproval =
            SourceDecision(decisions, "fda")["approvals"]!["securityData"]!;
        securityApproval[mutation] = mutation == "decision"
            ? "approved"
            : ReviewedAt;
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent => Assert.Contains(
                "fda:security-data-applicability-unresolved",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_No_Trigger_Security_Reviewed_Approval_Is_Contradictory_And_Blocked()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var securityApproval =
            SourceDecision(decisions, "fda")["approvals"]!["securityData"]!;
        securityApproval["reviewStatus"] = "reviewed";
        securityApproval["decision"] = "approved";
        securityApproval["decidedAtUtc"] = ReviewedAt;
        securityApproval["decisionNotes"] =
            new JsonArray("Synthetic contradictory no-trigger approval.");
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent => Assert.Contains(
                "fda:security-data-applicability-unresolved",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_Ready_Uses_Approved_Method_And_Blocked_May_Use_Review_Candidate()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var fda = SourceDecision(decisions, "fda");
        fda["acquisition"]!["method"] = "manual-review";
        RegistrySource(registry, "fda")["acquisition"]!["method"] = "manual-review";
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var readyPlan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.All(
            readyPlan.Intents.Where(intent => intent.SourceId == "fda"),
            intent =>
            {
                Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition);
                Assert.Equal("manual-review", intent.CandidateMethod);
            });

        fda["acquisition"]!["enabled"] = false;
        fda["acquisition"]!["method"] = "none";
        var blockedPlan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.All(
            blockedPlan.Intents.Where(intent => intent.SourceId == "fda"),
            intent =>
            {
                Assert.Equal(SourceAcquisitionDisposition.Blocked, intent.Disposition);
                Assert.Equal("api", intent.CandidateMethod);
            });
    }

    [Fact]
    public void PlanBuilder_Security_Trigger_Blocks_Only_The_Affected_Source_While_Review_Is_Pending()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var registrySha256 = BindSyntheticRegistry(registry, decisions);
        var fda = decisions["sources"]!.AsArray()
            .Single(source => source!["sourceId"]!.GetValue<string>() == "fda")!;
        fda["securityDataTriggersDetected"] =
            new JsonArray("untrusted-bulk-archive-or-parser");
        fda["approvals"]!["securityData"]!["reviewStatus"] = "review-required";
        fda["approvals"]!["securityData"]!["decision"] = null;
        fda["approvals"]!["securityData"]!["decidedAtUtc"] = null;

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent =>
            {
                Assert.Equal(SourceAcquisitionDisposition.Blocked, intent.Disposition);
                Assert.Contains(
                    "fda:security-data-review-required",
                    intent.BlockingReasons);
            });
    }

    [Theory]
    [InlineData("not-a-sha256", "source-registry-sha256-invalid")]
    [InlineData(
        "AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
        "source-registry-sha256-invalid")]
    [InlineData(
        "0000000000000000000000000000000000000000000000000000000000000000",
        "source-registry-sha256-mismatch")]
    public void PlanBuilder_Invalid_Or_Mismatched_Registry_Hash_Blocks_All_Intents(
        string actualRegistrySha256,
        string expectedBlocker)
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();

        var plan = Build(
            LoadPlanningFixture(),
            decisions,
            registry,
            actualRegistrySha256);

        Assert.Equal(0, plan.ReadyCount);
        Assert.All(
            plan.Intents,
            intent => Assert.Contains(
                $"{intent.SourceId}:{expectedBlocker}",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_Real_Registry_Raw_Byte_Hash_Matches_Decision_Binding()
    {
        var decisions = LoadSourceAuthorizationDecisions();

        Assert.Equal(
            decisions["registryBinding"]!["sha256"]!.GetValue<string>(),
            PilotRegistrySha256());
    }

    [Fact]
    public void PlanBuilder_Invalid_Decision_Registry_Binding_Blocks_All_Intents()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        decisions["registryBinding"]!["sha256"] = "invalid-binding";

        var plan = Build(
            LoadPlanningFixture(),
            decisions,
            registry,
            PilotRegistrySha256());

        Assert.Equal(0, plan.ReadyCount);
        Assert.All(
            plan.Intents,
            intent => Assert.Contains(
                $"{intent.SourceId}:source-registry-sha256-invalid",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_Same_Version_Mutated_Registry_With_Stale_Binding_Blocks_All_Intents()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        BindSyntheticRegistry(registry, decisions);
        RegistrySource(registry, "fda")["identity"]!["name"] =
            "Synthetic mutation after binding";
        var actualMutatedSha256 = ComputeSyntheticRegistrySha256(registry);

        var plan = Build(
            LoadPlanningFixture(),
            decisions,
            registry,
            actualMutatedSha256);

        Assert.Equal(0, plan.ReadyCount);
        Assert.All(
            plan.Intents,
            intent => Assert.Contains(
                $"{intent.SourceId}:source-registry-sha256-mismatch",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_Decision_And_Registry_Method_Drift_Blocks_Only_Affected_Source()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var fda = SourceDecision(decisions, "fda");
        fda["acquisition"]!["method"] = "manual-review";
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.Equal(12, plan.ReadyCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent => Assert.Contains(
                "fda:source-acquisition-method-registry-mismatch",
                intent.BlockingReasons));
    }

    [Fact]
    public void PlanBuilder_Ready_Intent_Uses_Registry_Authoritative_Field_And_Provenance_Order()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var registrySource = RegistrySource(registry, "fda");
        var registryFields = NodeStrings(
            registrySource["evidencePolicy"]!["authorizedFieldUse"]);
        var registryProvenance = NodeStrings(
            registrySource["provenanceRequirements"]!["requiredFields"]);
        var fdaDecision = SourceDecision(decisions, "fda");
        fdaDecision["evidenceBoundary"]!["authorizedFieldUse"] =
            StringArray(registryFields.Reverse());
        fdaDecision["provenance"]!["requiredFields"] =
            StringArray(registryProvenance.Reverse());
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        var intent = plan.Intents.First(item => item.SourceId == "fda");
        Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition);
        Assert.Equal(registryFields, intent.AuthorizedFieldUses);
        Assert.Equal(registryProvenance, intent.RequiredProvenanceFields);
    }

    [Fact]
    public void PlanBuilder_Ready_Uses_Field_Intersection_And_Provenance_Union()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        var registrySource = RegistrySource(registry, "fda");
        var registryFields = NodeStrings(
            registrySource["evidencePolicy"]!["authorizedFieldUse"]);
        var registryProvenance = NodeStrings(
            registrySource["provenanceRequirements"]!["requiredFields"]);
        var fdaDecision = SourceDecision(decisions, "fda");
        var narrowerFieldScope = registryFields.Take(2).ToList();
        fdaDecision["evidenceBoundary"]!["authorizedFieldUse"] =
            StringArray(narrowerFieldScope);
        fdaDecision["provenance"]!["requiredFields"]!.AsArray()
            .Add("decisionOnlyProvenanceField");
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        var intent = plan.Intents.First(item => item.SourceId == "fda");
        Assert.Equal(SourceAcquisitionDisposition.Ready, intent.Disposition);
        Assert.Equal(narrowerFieldScope, intent.AuthorizedFieldUses);
        Assert.Equal(
            registryProvenance.Concat(["decisionOnlyProvenanceField"]),
            intent.RequiredProvenanceFields);
    }

    [Fact]
    public void PlanBuilder_No_Decision_And_Registry_Field_Overlap_Blocks_Affected_Source()
    {
        var registry = LoadPilotRegistry();
        var decisions = LoadSourceAuthorizationDecisions();
        AuthorizeSyntheticPlanningState(registry, decisions);
        SourceDecision(decisions, "fda")["evidenceBoundary"]!["authorizedFieldUse"] =
            new JsonArray("decision-only-field");
        var registrySha256 = BindSyntheticRegistry(registry, decisions);

        var plan = Build(LoadPlanningFixture(), decisions, registry, registrySha256);

        Assert.Equal(2, plan.BlockedCount);
        Assert.All(
            plan.Intents.Where(intent => intent.SourceId == "fda"),
            intent => Assert.Contains(
                "fda:source-authorized-field-use-no-registry-overlap",
                intent.BlockingReasons));
    }

    [Fact]
    public void AdapterCatalog_Contains_Exactly_Seven_Unique_Selected_Sources()
    {
        Assert.Equal(
            RecommendedOfficialSourcePlanningAdapters.SourceIds,
            RecommendedOfficialSourcePlanningAdapters.All.Select(adapter => adapter.SourceId));
        Assert.Equal(
            7,
            RecommendedOfficialSourcePlanningAdapters.All
                .Select(adapter => adapter.SourceId)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .Count());
    }

    [Fact]
    public void PlanBuilder_Rejects_Duplicate_Adapter()
    {
        var adapters = RecommendedOfficialSourcePlanningAdapters.All
            .Concat(new[] { RecommendedOfficialSourcePlanningAdapters.All[0] });

        var exception = Assert.Throws<InvalidOperationException>(() =>
            new SourceAcquisitionPlanBuilder().Build(
                LoadPlanningFixture(),
                LoadSourceAuthorizationDecisions(),
                LoadPilotRegistry(),
                PilotRegistrySha256(),
                adapters));

        Assert.Contains("Duplicate source planning adapter", exception.Message);
    }

    [Fact]
    public void ActivationPolicy_Preserves_Existing_Authorizer_Alias_And_Ambiguity_Behavior()
    {
        var registry = JsonNode.Parse(
            File.ReadAllText(TestPaths.FixturePath("source-registry.sample.json")))!;
        var index = new SourceRegistryActivationPolicy().Build(registry);

        Assert.True(index.Resolve("declared-creatine-alias")!.CanAcquire);

        var duplicate = JsonNode.Parse(registry["sources"]![0]!.ToJsonString())!;
        duplicate["identity"]!["sourceId"] = "second-source";
        duplicate["identity"]!["aliases"] = new JsonArray("declared-creatine-alias");
        registry["sources"]!.AsArray().Add(duplicate);

        var ambiguousIndex = new SourceRegistryActivationPolicy().Build(registry);

        Assert.Null(ambiguousIndex.Resolve("declared-creatine-alias"));
    }

    [Fact]
    public void ActivationPolicy_Fails_Closed_For_Duplicate_Source_Id()
    {
        var registry = JsonNode.Parse(
            File.ReadAllText(TestPaths.FixturePath("source-registry.sample.json")))!;
        var duplicate = JsonNode.Parse(registry["sources"]![0]!.ToJsonString())!;
        duplicate["identity"]!["aliases"] = new JsonArray("second-alias");
        registry["sources"]!.AsArray().Add(duplicate);

        var index = new SourceRegistryActivationPolicy().Build(registry);

        Assert.Null(index.BySourceId("pubchem-creatine"));
        Assert.Null(index.Resolve("pubchem-creatine"));
        Assert.Null(index.Resolve("declared-creatine-alias"));
        Assert.Null(index.Resolve("second-alias"));
    }

    private static SourceAcquisitionPlan Build(
        JsonNode requests,
        JsonNode decisions,
        JsonNode registry,
        string actualRegistrySha256)
        => new SourceAcquisitionPlanBuilder().Build(
            requests,
            decisions,
            registry,
            actualRegistrySha256,
            RecommendedOfficialSourcePlanningAdapters.All);

    private static void AuthorizeSyntheticPlanningState(JsonNode registry, JsonNode decisions)
    {
        var selected = RecommendedOfficialSourcePlanningAdapters.SourceIds
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var decisionsBySourceId = decisions["sources"]!.AsArray()
            .ToDictionary(
                source => source!["sourceId"]!.GetValue<string>(),
                source => source!,
                StringComparer.OrdinalIgnoreCase);

        foreach (var source in registry["sources"]!.AsArray()
                     .Where(source => selected.Contains(
                         source!["identity"]!["sourceId"]!.GetValue<string>())))
        {
            var sourceId = source!["identity"]!["sourceId"]!.GetValue<string>();
            var decision = decisionsBySourceId[sourceId];
            source!["rights"]!["reviewStatus"] = "approved";
            source["rights"]!["legalBasisOrLicense"] =
                "Synthetic authorization fixture; no real-world rights assertion.";
            source["rights"]!["termsUrl"] = "https://example.invalid/synthetic-source-terms";
            source["rights"]!["verifiedAtUtc"] = ReviewedAt;
            source["rights"]!["allowedUses"] = new JsonArray("offline-planning-tests");
            source["rights"]!["reviewedByRole"] = "synthetic-test-fixture";
            source["operations"]!["status"] = "active";
            source["operations"]!["ownerRole"] = "synthetic-test-fixture";
            source["operations"]!["securityOwnerRole"] = "synthetic-test-fixture";
            source["operations"]!["lastReviewedAtUtc"] = ReviewedAt;
            source["acquisition"]!["enabled"] = true;
            source["acquisition"]!["method"] =
                decision["acquisition"]!["reviewCandidateMethod"]!.GetValue<string>();
            source["acquisition"]!["robotsPolicyStatus"] = "not-applicable";
            source["acquisition"]!["apiTermsStatus"] = "not-applicable";
            source["acquisition"]!["rateLimitPolicy"] = "synthetic-test-fixture";
            source["acquisition"]!["accessNotes"] =
                "No network or external acquisition occurs in this fixture.";
            source["refreshPolicy"]!["mode"] = "manual";
            source["refreshPolicy"]!["cadence"] = "synthetic-test-fixture";
            source["remediation"]!["contactRole"] = "synthetic-test-fixture";
            source["dataBoundary"]!["permittedContent"] =
                new JsonArray("synthetic-test-data");
            source["evidencePolicy"]!["authorizedFieldUse"] =
                decision["evidenceBoundary"]!["authorizedFieldUse"]!.DeepClone();
            source["provenanceRequirements"]!["requiredFields"] =
                decision["provenance"]!["requiredFields"]!.DeepClone();
        }

        foreach (var source in decisions["sources"]!.AsArray())
        {
            source!["decisionStatus"] = "approved";
            source["activationReady"] = true;
            source["rights"]!["reviewStatus"] = "reviewed";
            source["rights"]!["legalBasisOrLicense"] =
                "Synthetic authorization fixture; no real-world rights assertion.";
            source["rights"]!["allowedUses"] = new JsonArray("offline-planning-tests");
            source["rights"]!["reviewedBy"] =
                source["approvals"]!["legalRights"]!["assigneeName"]!.GetValue<string>();
            source["rights"]!["verifiedAtUtc"] = ReviewedAt;
            source["operations"]!["status"] = "approved";
            source["operations"]!["lastReviewedAtUtc"] = ReviewedAt;
            source["acquisition"]!["enabled"] = true;
            source["acquisition"]!["method"] =
                source["acquisition"]!["reviewCandidateMethod"]!.GetValue<string>();
            source["acquisition"]!["apiTermsStatus"] = "reviewed";
            source["acquisition"]!["robotsPolicyStatus"] = "reviewed";
            source["refresh"]!["mode"] = "manual";
            var legal = source["approvals"]!["legalRights"]!;
            legal["reviewStatus"] = "reviewed";
            legal["decision"] = "approved-with-controls";
            legal["decidedAtUtc"] = ReviewedAt;
            legal["decisionNotes"] =
                new JsonArray("Synthetic planning fixture only; no real-world approval.");
        }
    }

    private static string BindSyntheticRegistry(JsonNode registry, JsonNode decisions)
    {
        var sha256 = ComputeSyntheticRegistrySha256(registry);
        decisions["registryBinding"]!["sha256"] = sha256;
        return sha256;
    }

    private static string ComputeSyntheticRegistrySha256(JsonNode registry)
        => Sha256(Encoding.UTF8.GetBytes(registry.ToJsonString()));

    private static string PilotRegistrySha256()
        => Sha256(File.ReadAllBytes(PilotRegistryPath()));

    private static string Sha256(byte[] bytes)
        => Convert.ToHexString(SHA256.HashData(bytes)).ToLowerInvariant();

    private static IReadOnlyList<string> NodeStrings(JsonNode? node)
        => node!.AsArray()
            .Select(item => item!.GetValue<string>())
            .ToList();

    private static JsonArray StringArray(IEnumerable<string> values)
        => new(values.Select(value => JsonValue.Create(value)).ToArray());

    private static JsonNode SourceDecision(JsonNode decisions, string sourceId)
        => decisions["sources"]!.AsArray()
            .Single(source => string.Equals(
                source!["sourceId"]!.GetValue<string>(),
                sourceId,
                StringComparison.OrdinalIgnoreCase))!;

    private static JsonNode RegistrySource(JsonNode registry, string sourceId)
        => registry["sources"]!.AsArray()
            .Single(source => string.Equals(
                source!["identity"]!["sourceId"]!.GetValue<string>(),
                sourceId,
                StringComparison.OrdinalIgnoreCase))!;

    private static void MutateDecisionLayer(JsonNode source, string mutation)
    {
        switch (mutation)
        {
            case "rights-review-status":
                source["rights"]!["reviewStatus"] = "review-required";
                break;
            case "rights-legal-basis":
                source["rights"]!["legalBasisOrLicense"] = null;
                break;
            case "rights-allowed-uses":
                source["rights"]!["allowedUses"] = new JsonArray();
                break;
            case "rights-reviewer":
                source["rights"]!["reviewedBy"] = null;
                break;
            case "rights-reviewer-mismatch":
                source["rights"]!["reviewedBy"] = "Different Legal Reviewer";
                break;
            case "rights-review-timestamp":
                source["rights"]!["verifiedAtUtc"] = null;
                break;
            case "operations-status":
                source["operations"]!["status"] = "disabled";
                break;
            case "operations-review-timestamp":
                source["operations"]!["lastReviewedAtUtc"] = null;
                break;
            case "acquisition-enabled":
                source["acquisition"]!["enabled"] = false;
                break;
            case "acquisition-method":
                source["acquisition"]!["method"] = "none";
                break;
            case "api-terms-status":
                source["acquisition"]!["apiTermsStatus"] = "review-required";
                break;
            case "robots-policy-status":
                source["acquisition"]!["robotsPolicyStatus"] = "review-required";
                break;
            case "refresh-mode":
                source["refresh"]!["mode"] = "disabled-until-approved";
                break;
            case "refresh-cadence":
                source["refresh"]!["proposedCadence"] = "";
                break;
            case "source-name":
                source["sourceName"] = "";
                break;
            case "authorized-field-use":
                source["evidenceBoundary"]!["authorizedFieldUse"] = new JsonArray();
                break;
            case "provenance-fields":
                source["provenance"]!["requiredFields"] = new JsonArray();
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(mutation), mutation, "Unknown mutation.");
        }
    }

    private static JsonNode LoadPlanningFixture()
        => LoadJson(TestPaths.FixturePath("source-acquisition-planning.sample.json"));

    private static JsonNode LoadMarketInterestRequests()
        => LoadJson(Path.Combine(
            RepositoryRoot(),
            "research",
            "research-requests",
            "market-interest-coverage-2026-07-24.v1.json"));

    private static JsonNode LoadSourceAuthorizationDecisions()
        => LoadJson(Path.Combine(
            RepositoryRoot(),
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json"));

    private static JsonNode LoadPilotRegistry()
        => LoadJson(PilotRegistryPath());

    private static string PilotRegistryPath()
        => Path.Combine(
            RepositoryRoot(),
            "research",
            "input",
            "sources",
            "pilot-source-registry.json");

    private static JsonNode LoadJson(string path)
        => JsonNode.Parse(File.ReadAllText(path))!;

    private static string RepositoryRoot()
        => Directory.GetParent(TestPaths.BackendRoot())!.FullName;
}
