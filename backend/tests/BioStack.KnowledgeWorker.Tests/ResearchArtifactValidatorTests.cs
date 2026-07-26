namespace BioStack.KnowledgeWorker.Tests;

using System.Security.Cryptography;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class ResearchArtifactValidatorTests
{
    public static IEnumerable<object[]> ValidFixtures => new[]
    {
        new object[] { ResearchArtifactKind.CompoundCandidateBatch, "compound-candidates.sample.json" },
        new object[] { ResearchArtifactKind.SourceRegistry, "source-registry.sample.json" },
        new object[] { ResearchArtifactKind.EvidencePacket, "evidence-packet.sample.json" },
        new object[] { ResearchArtifactKind.ReviewDecisionBatch, "review-decision.sample.json" },
        new object[] { ResearchArtifactKind.ResearchRequestBatch, "research-request.sample.json" },
    };

    [Theory]
    [MemberData(nameof(ValidFixtures))]
    public void Validator_Accepts_Valid_Research_Fixtures(ResearchArtifactKind kind, string fixtureName)
    {
        var artifact = LoadArtifact(kind, fixtureName);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(kind, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
    }

    [Fact]
    public void Validator_Rejects_Candidate_Batch_Missing_Candidates()
    {
        var artifact = LoadArtifact(ResearchArtifactKind.CompoundCandidateBatch, "compound-candidates.sample.json");
        artifact.Node.AsObject().Remove("candidates");
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.CompoundCandidateBatch, artifact.Node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Keyword.Equals("required", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Validator_Rejects_Evidence_Packet_With_Unknown_ClaimType()
    {
        var artifact = LoadArtifact(ResearchArtifactKind.EvidencePacket, "evidence-packet.sample.json");
        artifact.Node["claims"]![0]!["claimType"] = "unsupported-claim-type";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.EvidencePacket, artifact.Node);

        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void Validator_Accepts_Governed_Pilot_Source_Registry()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var path = Path.Combine(repositoryRoot, "research", "input", "sources", "pilot-source-registry.json");
        var artifact = new ResearchArtifactLoader().Load(ResearchArtifactKind.SourceRegistry, path);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
        var sources = artifact.Node["sources"]!.AsArray();
        Assert.Equal(13, sources.Count);
        var approvedSourceIds = new HashSet<string>(StringComparer.Ordinal)
        {
            "fda",
            "pubchem",
            "pubmed",
            "clinicaltrials",
            "dailymed",
            "nih-ods",
            "nih-nccih",
        };
        foreach (var source in sources)
        {
            var sourceId = source!["identity"]!["sourceId"]!.GetValue<string>();
            if (approvedSourceIds.Contains(sourceId))
            {
                Assert.Equal("approved", source["rights"]!["reviewStatus"]!.GetValue<string>());
                Assert.Equal("active", source["operations"]!["status"]!.GetValue<string>());
                Assert.True(source["acquisition"]!["enabled"]!.GetValue<bool>());
                Assert.NotEmpty(source["rights"]!["allowedUses"]!.AsArray());
                continue;
            }

            Assert.Equal("pending-human-legal", source["rights"]!["reviewStatus"]!.GetValue<string>());
            Assert.Equal("disabled", source["operations"]!["status"]!.GetValue<string>());
            Assert.False(source["acquisition"]!["enabled"]!.GetValue<bool>());
            Assert.Empty(source["rights"]!["allowedUses"]!.AsArray());
        }
    }

    [Fact]
    public void Validator_Accepts_Recommended_Seven_Source_Decision_Batch_With_Exact_Registry_Binding()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var registryPath = Path.Combine(
            repositoryRoot,
            "research",
            "input",
            "sources",
            "pilot-source-registry.json");
        var artifact = new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
        var registry = JsonNode.Parse(File.ReadAllText(registryPath))!;
        Assert.Equal(
            registry["schemaVersion"]!.GetValue<string>(),
            artifact.Node["registryBinding"]!["schemaVersion"]!.GetValue<string>());
        using var registryStream = File.OpenRead(registryPath);
        var registrySha256 = Convert.ToHexString(SHA256.HashData(registryStream)).ToLowerInvariant();
        Assert.Equal(
            registrySha256,
            artifact.Node["registryBinding"]!["sha256"]!.GetValue<string>());
        Assert.Equal("3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28", registrySha256);
    }

    [Fact]
    public void Recommended_Seven_Source_Decisions_Keep_Stage_Specific_Gates_Without_Blanket_Suppression()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var artifact = new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
        var expectedSources = new HashSet<string>(StringComparer.Ordinal)
        {
            "fda",
            "pubchem",
            "pubmed",
            "clinicaltrials",
            "dailymed",
            "nih-ods",
            "nih-nccih",
        };
        var expectedOwners = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["product-owner"] = "Clint Morgan",
            ["legal-rights-approver"] = "Johnathan Harper",
            ["evidence-reviewer"] = "Ellison Nemoy",
            ["security-data-owner"] = "Pradic Patel",
        };

        var owners = artifact.Node["owners"]!.AsArray();
        Assert.Equal(4, owners.Count);
        Assert.Equal(4, owners.Select(owner => owner!["roleId"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
        Assert.Equal(4, owners.Select(owner => owner!["personName"]!.GetValue<string>()).Distinct(StringComparer.Ordinal).Count());
        foreach (var owner in owners)
        {
            var roleId = owner!["roleId"]!.GetValue<string>();
            Assert.Equal(expectedOwners[roleId], owner["personName"]!.GetValue<string>());
            Assert.Equal("assigned", owner["assignmentStatus"]!.GetValue<string>());
        }

        var doctrine = artifact.Node["productDoctrine"]!;
        Assert.Equal("product-owner-confirmed", doctrine["status"]!.GetValue<string>());
        Assert.Equal("Clint Morgan", doctrine["confirmedBy"]!.GetValue<string>());
        Assert.Equal(
            "observational-educational-evidence-aware-non-prescriptive",
            doctrine["stance"]!.GetValue<string>());
        Assert.Contains(
            "does not automatically require less useful information",
            doctrine["governingPrinciple"]!.GetValue<string>(),
            StringComparison.Ordinal);

        var stageGates = artifact.Node["stageGates"]!;
        Assert.True(stageGates["blanketSuppressionProhibited"]!.GetValue<bool>());
        Assert.Equal(
            ["legalRights"],
            stageGates["sourceActivationRequiredApprovals"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray());
        Assert.Equal(
            ["securityData"],
            stageGates["sourceActivationConditionalApprovals"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray());
        Assert.Contains(
            "untrusted-bulk-archive-or-parser",
            stageGates["securityDataTriggers"]!.AsArray()
                .Select(value => value!.GetValue<string>()));
        Assert.Equal(
            ["evidence"],
            stageGates["canonicalClaimPromotionRequiredApprovals"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray());
        Assert.Equal(
            ["product"],
            stageGates["productCapabilityReviewRequiredApprovals"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray());

        var sources = artifact.Node["sources"]!.AsArray();
        Assert.Equal(7, sources.Count);
        var actualSources = sources
            .Select(source => source!["sourceId"]!.GetValue<string>())
            .ToHashSet(StringComparer.Ordinal);
        Assert.Equal(expectedSources, actualSources);
        Assert.Equal(7, actualSources.Count);

        foreach (var source in sources)
        {
            Assert.Equal("approved", source!["decisionStatus"]!.GetValue<string>());
            Assert.True(source["activationReady"]!.GetValue<bool>());
            Assert.Equal("reviewed", source["rights"]!["reviewStatus"]!.GetValue<string>());
            Assert.NotEmpty(source["rights"]!["allowedUses"]!.AsArray());
            Assert.NotEmpty(source["rights"]!["proposedUses"]!.AsArray());
            Assert.False(string.IsNullOrWhiteSpace(
                source["rights"]!["legalBasisOrLicense"]!.GetValue<string>()));
            Assert.Equal("Johnathan Harper", source["rights"]!["reviewedBy"]!.GetValue<string>());
            Assert.NotNull(source["rights"]!["verifiedAtUtc"]);
            Assert.Equal("approved", source["operations"]!["status"]!.GetValue<string>());
            Assert.NotNull(source["operations"]!["lastReviewedAtUtc"]);
            Assert.True(source["acquisition"]!["enabled"]!.GetValue<bool>());
            Assert.Equal(
                source["acquisition"]!["reviewCandidateMethod"]!.GetValue<string>(),
                source["acquisition"]!["method"]!.GetValue<string>());
            Assert.Contains(
                source["acquisition"]!["apiTermsStatus"]!.GetValue<string>(),
                new[] { "reviewed", "not-applicable" });
            Assert.Equal("manual", source["refresh"]!["mode"]!.GetValue<string>());
            Assert.Equal(
                "mark-stale-and-restrict-current-status-claims",
                source["refresh"]!["stalenessAction"]!.GetValue<string>());
            Assert.False(source["dataBoundary"]!["trainingUseAllowed"]!.GetValue<bool>());
            Assert.Equal(
                "claim-level-before-canonical-promotion",
                source["evidenceBoundary"]!["humanReviewRequirement"]!.GetValue<string>());
            var permittedContent = source["dataBoundary"]!["proposedPermittedContent"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray();
            Assert.Contains(
                permittedContent,
                value => value.Contains("dose ranges", StringComparison.Ordinal)
                    && value.Contains("uncertainty", StringComparison.Ordinal));
            Assert.Contains(
                permittedContent,
                value => value.Contains("Comparisons", StringComparison.Ordinal)
                    && value.Contains("without diagnosis, prescribing, or individualized directives", StringComparison.Ordinal));

            var approvals = source["approvals"]!.AsObject();
            Assert.Equal(4, approvals.Count);
            Assert.Equal("Clint Morgan", approvals["product"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("product-capability", approvals["product"]!["decisionScope"]!.GetValue<string>());
            Assert.Equal("product-capability-review", approvals["product"]!["blockingStage"]!.GetValue<string>());
            Assert.Equal("reviewed", approvals["product"]!["reviewStatus"]!.GetValue<string>());
            Assert.Equal("approved", approvals["product"]!["decision"]!.GetValue<string>());
            Assert.NotNull(approvals["product"]!["decidedAtUtc"]);
            Assert.NotEmpty(approvals["product"]!["decisionNotes"]!.AsArray());
            Assert.Equal("Johnathan Harper", approvals["legalRights"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("legal-rights", approvals["legalRights"]!["decisionScope"]!.GetValue<string>());
            Assert.Equal("source-activation", approvals["legalRights"]!["blockingStage"]!.GetValue<string>());
            Assert.Equal("reviewed", approvals["legalRights"]!["reviewStatus"]!.GetValue<string>());
            Assert.Equal("approved", approvals["legalRights"]!["decision"]!.GetValue<string>());
            Assert.NotNull(approvals["legalRights"]!["decidedAtUtc"]);
            Assert.NotEmpty(approvals["legalRights"]!["decisionNotes"]!.AsArray());
            Assert.Equal("Ellison Nemoy", approvals["evidence"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("evidence-promotion", approvals["evidence"]!["decisionScope"]!.GetValue<string>());
            Assert.Equal("canonical-claim-promotion", approvals["evidence"]!["blockingStage"]!.GetValue<string>());
            Assert.Equal("Pradic Patel", approvals["securityData"]!["assigneeName"]!.GetValue<string>());
            Assert.Equal("security-data", approvals["securityData"]!["decisionScope"]!.GetValue<string>());
            Assert.Equal("source-activation", approvals["securityData"]!["blockingStage"]!.GetValue<string>());
            Assert.Equal("review-required", approvals["evidence"]!["reviewStatus"]!.GetValue<string>());
            Assert.Null(approvals["evidence"]!["decision"]);
            Assert.Null(approvals["evidence"]!["decidedAtUtc"]);
            var sourceId = source["sourceId"]!.GetValue<string>();
            var securityTriggers = source["securityDataTriggersDetected"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray();
            Assert.Contains("new-egress-or-storage-boundary", securityTriggers);
            if (sourceId == "nih-nccih")
            {
                Assert.Equal(["new-egress-or-storage-boundary"], securityTriggers);
            }
            else
            {
                Assert.Equal(
                    [
                        "new-egress-or-storage-boundary",
                        "untrusted-bulk-archive-or-parser",
                    ],
                    securityTriggers);
            }

            Assert.Equal(
                "reviewed",
                approvals["securityData"]!["reviewStatus"]!.GetValue<string>());
            Assert.Equal(
                "approved-with-controls",
                approvals["securityData"]!["decision"]!.GetValue<string>());
            Assert.Equal(
                "2026-07-26T10:40:42Z",
                approvals["securityData"]!["decidedAtUtc"]!.GetValue<string>());
            var securityNotes = approvals["securityData"]!["decisionNotes"]!.AsArray()
                .Select(value => value!.GetValue<string>())
                .ToArray();
            Assert.Contains(
                securityNotes,
                note => note.Contains(
                    "not the original decision time",
                    StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains(
                    "ResearchOutput/source-acquisition/v1",
                    StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains(
                    "Raw response bodies",
                    StringComparison.Ordinal)
                    && note.Contains("database writes", StringComparison.Ordinal)
                    && note.Contains("promotion", StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains("disabled redirects", StringComparison.Ordinal)
                    && note.Contains("no automatic retry", StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains("worker service identity", StringComparison.Ordinal)
                    && note.Contains("evidence reviewer", StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains(
                    "explicit positive runtime configuration value with no default",
                    StringComparison.Ordinal)
                    && note.Contains("content-free tombstone", StringComparison.Ordinal)
                    && note.Contains("quarantined", StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains("BioStackKnowledgeWorker", StringComparison.Ordinal)
                    && note.Contains(
                        "must not be committed",
                        StringComparison.Ordinal));
            Assert.Contains(
                securityNotes,
                note => note.Contains("operator Clint Morgan", StringComparison.Ordinal)
                    && note.Contains(
                        "independent reviewer Ellison Nemoy",
                        StringComparison.Ordinal));
        }
    }

    [Fact]
    public void Validator_Can_Represent_A_Reviewed_Source_Activation_Without_Approving_Claim_Promotion()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        var artifact = new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
        var source = artifact.Node["sources"]![0]!;
        source["decisionStatus"] = "approved";
        source["activationReady"] = true;
        source["rights"]!["reviewStatus"] = "reviewed";
        source["rights"]!["legalBasisOrLicense"] = "CC0 for the selected openFDA data class";
        source["rights"]!["allowedUses"] = new JsonArray("candidate retrieval", "factual-field storage");
        source["rights"]!["reviewedBy"] = "Johnathan Harper";
        source["rights"]!["verifiedAtUtc"] = "2026-07-25T13:15:00Z";
        source["operations"]!["status"] = "approved";
        source["operations"]!["lastReviewedAtUtc"] = "2026-07-25T13:15:00Z";
        source["acquisition"]!["enabled"] = true;
        source["acquisition"]!["method"] = "api";
        source["acquisition"]!["apiTermsStatus"] = "reviewed";
        source["refresh"]!["mode"] = "manual";
        var legalApproval = source["approvals"]!["legalRights"]!;
        legalApproval["reviewStatus"] = "reviewed";
        legalApproval["decision"] = "approved-with-controls";
        legalApproval["decidedAtUtc"] = "2026-07-25T13:15:00Z";
        legalApproval["decisionNotes"] = new JsonArray("Limited to the selected openFDA data class.");
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
        Assert.Equal(
            "review-required",
            source["approvals"]!["evidence"]!["reviewStatus"]!.GetValue<string>());
        Assert.Null(source["approvals"]!["evidence"]!["decision"]);
    }

    [Fact]
    public void Validator_Rejects_Activation_With_Unresolved_Rights()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        var source = artifact.Node["sources"]![0]!;
        source["rights"]!["reviewStatus"] = "review-required";
        source["rights"]!["legalBasisOrLicense"] = null;
        source["rights"]!["allowedUses"] = new JsonArray();
        source["rights"]!["reviewedBy"] = null;
        source["rights"]!["verifiedAtUtc"] = null;
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Rejects_Reviewed_Approval_Without_Decision_And_Date()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        var legalApproval = artifact.Node["sources"]![0]!["approvals"]!["legalRights"]!;
        legalApproval["reviewStatus"] = "reviewed";
        legalApproval["decision"] = null;
        legalApproval["decidedAtUtc"] = null;
        legalApproval["decisionNotes"] = new JsonArray();
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Accepts_Detected_Security_Trigger_With_Review_Pending_While_Inactive()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        var source = artifact.Node["sources"]![0]!;
        source["securityDataTriggersDetected"] =
            new JsonArray(
                "new-egress-or-storage-boundary",
                "untrusted-bulk-archive-or-parser");
        var securityApproval = source["approvals"]!["securityData"]!;
        securityApproval["reviewStatus"] = "review-required";
        securityApproval["decision"] = null;
        securityApproval["decidedAtUtc"] = null;
        source["decisionStatus"] = "selected-pending-source-activation-review";
        source["activationReady"] = false;
        source["operations"]!["status"] = "disabled";
        source["acquisition"]!["enabled"] = false;
        source["acquisition"]!["method"] = "none";
        source["refresh"]!["mode"] = "disabled-until-approved";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.True(result.IsValid, result.Summary());
    }

    [Fact]
    public void Validator_Rejects_Activation_With_Security_Trigger_And_Review_Pending()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        var source = artifact.Node["sources"]![0]!;
        source["securityDataTriggersDetected"] =
            new JsonArray(
                "new-egress-or-storage-boundary",
                "untrusted-bulk-archive-or-parser");
        var securityApproval = source["approvals"]!["securityData"]!;
        securityApproval["reviewStatus"] = "review-required";
        securityApproval["decision"] = null;
        securityApproval["decidedAtUtc"] = null;
        source["decisionStatus"] = "approved";
        source["activationReady"] = true;
        source["operations"]!["status"] = "approved";
        source["acquisition"]!["enabled"] = true;
        source["acquisition"]!["method"] = "api";
        var legalApproval = source["approvals"]!["legalRights"]!;
        legalApproval["reviewStatus"] = "reviewed";
        legalApproval["decision"] = "approved-with-controls";
        legalApproval["decidedAtUtc"] = "2026-07-25T13:15:00Z";
        legalApproval["decisionNotes"] = new JsonArray("Limited to the reviewed data class.");
        source["rights"]!["reviewStatus"] = "reviewed";
        source["rights"]!["legalBasisOrLicense"] = "Reviewed public-data terms";
        source["rights"]!["allowedUses"] = new JsonArray("candidate retrieval");
        source["rights"]!["reviewedBy"] = "Johnathan Harper";
        source["rights"]!["verifiedAtUtc"] = "2026-07-25T13:15:00Z";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Rejects_Cross_Wired_Approval_Scope()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        artifact.Node["sources"]![0]!["approvals"]!["product"]!["decisionScope"] = "legal-rights";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validator_Rejects_Enabled_Acquisition_After_Rights_Rejection()
    {
        var artifact = LoadSevenSourceDecisionArtifact();
        var source = artifact.Node["sources"]![0]!;
        var legalApproval = source["approvals"]!["legalRights"]!;
        legalApproval["reviewStatus"] = "reviewed";
        legalApproval["decision"] = "rejected";
        legalApproval["decidedAtUtc"] = "2026-07-25T13:15:00Z";
        legalApproval["decisionNotes"] = new JsonArray("The proposed content class and use were rejected.");
        source["acquisition"]!["enabled"] = true;
        source["acquisition"]!["method"] = "api";
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceAuthorizationDecisionBatch, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("operations")]
    [InlineData("acquisition")]
    [InlineData("evidencePolicy")]
    [InlineData("provenanceRequirements")]
    [InlineData("refreshPolicy")]
    [InlineData("remediation")]
    [InlineData("dataBoundary")]
    public void Validator_Rejects_Source_Registry_Missing_Governance_Section(string section)
    {
        var artifact = LoadArtifact(ResearchArtifactKind.SourceRegistry, "source-registry.sample.json");
        artifact.Node["sources"]![0]!.AsObject().Remove(section);
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Keyword.Equals("required", StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("rights")]
    [InlineData("operations")]
    [InlineData("acquisition")]
    [InlineData("evidence")]
    [InlineData("provenance")]
    [InlineData("refresh")]
    [InlineData("remediation")]
    [InlineData("data-boundary")]
    public void Validator_Rejects_Enabled_Source_With_Incomplete_Activation_Evidence(string prerequisite)
    {
        var artifact = LoadArtifact(ResearchArtifactKind.SourceRegistry, "source-registry.sample.json");
        var source = artifact.Node["sources"]![0]!;
        switch (prerequisite)
        {
            case "rights": source["rights"]!["legalBasisOrLicense"] = null; break;
            case "operations": source["operations"]!["ownerRole"] = null; break;
            case "acquisition": source["acquisition"]!["method"] = "none"; break;
            case "evidence": source["evidencePolicy"]!["authorizedFieldUse"] = new JsonArray(); break;
            case "provenance": source["provenanceRequirements"]!["requiredFields"] = new JsonArray(); break;
            case "refresh": source["refreshPolicy"]!["cadence"] = null; break;
            case "remediation": source["remediation"]!["contactRole"] = null; break;
            case "data-boundary": source["dataBoundary"]!["permittedContent"] = new JsonArray(); break;
        }
        var validator = ResearchArtifactValidator.LoadFromDirectory(TestPaths.WorkerSchemaDirectory());

        var result = validator.Validate(ResearchArtifactKind.SourceRegistry, artifact.Node);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Loader_Rejects_Non_Object_Root()
    {
        var tempPath = Path.Combine(Path.GetTempPath(), $"biostack-research-{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempPath, "[]");
            var loader = new ResearchArtifactLoader();

            var ex = Assert.Throws<InvalidOperationException>(() =>
                loader.Load(ResearchArtifactKind.SourceRegistry, tempPath));

            Assert.Contains("must be a JSON object", ex.Message);
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    private static LoadedResearchArtifact LoadArtifact(ResearchArtifactKind kind, string fixtureName)
    {
        var loader = new ResearchArtifactLoader();
        return loader.Load(kind, TestPaths.FixturePath(fixtureName));
    }

    private static LoadedResearchArtifact LoadSevenSourceDecisionArtifact()
    {
        var repositoryRoot = Directory.GetParent(TestPaths.BackendRoot())!.FullName;
        var decisionPath = Path.Combine(
            repositoryRoot,
            "research",
            "source-authorization",
            "recommended-seven-source-decisions.v1.json");
        return new ResearchArtifactLoader().Load(
            ResearchArtifactKind.SourceAuthorizationDecisionBatch,
            decisionPath);
    }
}
