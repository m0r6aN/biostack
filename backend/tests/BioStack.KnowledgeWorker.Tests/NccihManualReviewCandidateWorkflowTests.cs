namespace BioStack.KnowledgeWorker.Tests;

using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class NccihManualReviewCandidateWorkflowTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset CapturedAt =
        DateTimeOffset.Parse("2026-07-25T16:00:00Z");
    private static readonly DateTimeOffset ReviewedAt =
        DateTimeOffset.Parse("2026-07-25T16:05:00Z");
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T16:10:00Z");

    [Fact]
    public void CreateCandidate_Emits_Exact_Audited_ReviewRequired_Candidate()
    {
        var batch = CreateWorkflow().CreateCandidate(
            ReadyIntent(),
            ValidCapture(),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.False(batch.Truncated);
        Assert.Null(batch.RetryAfter);
        var candidate = Assert.Single(batch.Candidates);
        Assert.Equal("nih-nccih", candidate.SourceRegistryId);
        Assert.Equal(
            NccihManualReviewCandidateWorkflow.CanonicalSourceItemSlug,
            candidate.SourceItemId);
        Assert.Equal(
            NccihManualReviewCandidateWorkflow.CanonicalSourceUrl,
            candidate.SourceUrl);
        Assert.Null(candidate.QueryUrl);
        Assert.Equal("Melatonin", candidate.CompoundName);
        Assert.Equal("2025-02-15", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(RetrievedAt, candidate.RetrievedAtUtc);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(
            NccihManualReviewCandidateWorkflow.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal(
            ["efficacy-claims", "identity", "interactions", "mechanism"],
            candidate.AuthorizedFieldUses);
        Assert.Equal(
            ["Synthetic source-authored identity context."],
            candidate.Fields["identity_context"]);
        Assert.Equal(
            ["Synthetic source-authored mechanism context."],
            candidate.Fields["mechanism_context"]);
        Assert.Equal(
            ["Synthetic source-authored efficacy context."],
            candidate.Fields["efficacy_context"]);
        Assert.Equal(
            ["Synthetic source-authored interaction context."],
            candidate.Fields["interaction_context"]);
        Assert.All(
            candidate.Fields.SelectMany(pair => pair.Value),
            value => Assert.DoesNotContain("http", value));

        Assert.Equal(
            [NccihManualReviewCandidateWorkflow.CanonicalPageTitle],
            candidate.SourceSpecificProvenance["pageTitle"].Values);
        Assert.Equal(
            ["Synthetic NCCIH section"],
            candidate.SourceSpecificProvenance["section"].Values);
        Assert.Equal(
            ["2025-02-15"],
            candidate.SourceSpecificProvenance["pageUpdatedDate"].Values);
        Assert.Equal(
            ["manual-review"],
            candidate.SourceSpecificProvenance["captureMethod"].Values);

        var attribution = Assert.Single(candidate.RightsAttributions);
        Assert.Contains("public-domain", attribution.Scope);
        Assert.Contains("NCCIH", attribution.Provider);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            "https://www.nccih.nih.gov/tools/privacy",
            attribution.TermsUrl);
        Assert.Equal(
            candidate.Fields.Keys.OrderBy(key => key, StringComparer.Ordinal),
            attribution.CoveredFields);
        var document = Assert.Single(candidate.DocumentProvenance);
        Assert.Equal(
            NccihManualReviewCandidateWorkflow.CanonicalPageTitle,
            document.Title);
        Assert.Equal("Synthetic NCCIH section", document.Section);
        Assert.Equal("2025-02-15", document.UpdatedDate);
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        Assert.Contains("NCCIH", candidate.ReuseBoundary.Acknowledgement);
        Assert.Contains(
            "does not endorse",
            candidate.ReuseBoundary.Acknowledgement);
        Assert.Contains(
            "separately copyrighted third-party material",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Equal(3, candidate.EvidenceLimitations.Count);

        var audit = Assert.IsType<SourceManualCaptureAudit>(
            candidate.ManualCaptureAudit);
        Assert.Equal("operator.synthetic", audit.OperatorId);
        Assert.Equal("reviewer.synthetic", audit.ReviewerId);
        Assert.Equal("approved", audit.Decision);
        Assert.True(audit.Attestations.AllSatisfied);
    }

    [Theory]
    [InlineData("different-compound")]
    [InlineData("missing-canonical-term")]
    [InlineData("unmapped-extra-term")]
    public void CreateCandidate_Returns_NoMatch_Before_Accepting_Capture(
        string mutation)
    {
        var intent = ReadyIntent();
        intent = mutation switch
        {
            "different-compound" => intent with
            {
                CompoundName = "Synthetic Unmapped Compound",
                SearchTerms = ["Synthetic Unmapped Compound"],
            },
            "missing-canonical-term" => intent with
            {
                SearchTerms = ["N-acetyl-5-methoxytryptamine"],
            },
            "unmapped-extra-term" => intent with
            {
                SearchTerms = ["Melatonin", "Synthetic Unmapped Alias"],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var batch = CreateWorkflow().CreateCandidate(
            intent,
            capture: null,
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.False(batch.Truncated);
        Assert.Null(batch.RetryAfter);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("wrong-source")]
    [InlineData("wrong-planner")]
    [InlineData("wrong-method")]
    [InlineData("stale-registry")]
    [InlineData("missing-provenance")]
    [InlineData("non-utc-retrieval")]
    public void CreateCandidate_Rejects_Invalid_Intent_Before_Capture(
        string mutation)
    {
        var intent = ReadyIntent();
        var retrievedAt = RetrievedAt;
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["nih-nccih:legal-rights-not-approved"],
            },
            "wrong-source" => intent with { SourceId = "nih-ods" },
            "wrong-planner" => intent with
            {
                AdapterId = "nih-nccih-planning-v2",
            },
            "wrong-method" => intent with { CandidateMethod = "api" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId"],
            },
            "non-utc-retrieval" => intent,
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        if (mutation == "non-utc-retrieval")
        {
            retrievedAt = new DateTimeOffset(
                2026,
                7,
                25,
                12,
                10,
                0,
                TimeSpan.FromHours(-4));
        }

        Assert.ThrowsAny<Exception>(
            () => CreateWorkflow().CreateCandidate(
                intent,
                capture: null,
                retrievedAt));
    }

    [Fact]
    public void CreateCandidate_Requires_Capture_For_Mapped_Target()
    {
        var exception = Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                ReadyIntent(),
                capture: null,
                RetrievedAt));

        Assert.Equal("manual-capture-required", exception.Code);
    }

    [Theory]
    [InlineData("url")]
    [InlineData("title")]
    [InlineData("slug")]
    public void CreateCandidate_Requires_Exact_Allowlisted_Page_Identity(
        string mutation)
    {
        var capture = ValidCapture();
        capture = mutation switch
        {
            "url" => capture with
            {
                SourceUrl =
                    "https://www.nccih.nih.gov/health/synthetic-other-page",
            },
            "title" => capture with
            {
                PageTitle = "Synthetic Other Page",
            },
            "slug" => capture with
            {
                SourceItemSlug = "synthetic-other-page",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                ReadyIntent(),
                capture,
                RetrievedAt));

        Assert.Equal("manual-capture-page-not-allowlisted", exception.Code);
    }

    [Theory]
    [InlineData("partial-date")]
    [InlineData("impossible-date")]
    [InlineData("blank-section")]
    [InlineData("markup-section")]
    [InlineData("url-section")]
    [InlineData("ftp-section")]
    [InlineData("bare-domain-section")]
    [InlineData("markdown-section")]
    [InlineData("relative-url-section")]
    [InlineData("urn-section")]
    [InlineData("markdown-list-section")]
    public void CreateCandidate_Rejects_Invalid_Required_Page_Provenance(
        string mutation)
    {
        var capture = ValidCapture();
        capture = mutation switch
        {
            "partial-date" => capture with { PageUpdatedDate = "2025-02" },
            "impossible-date" => capture with
            {
                PageUpdatedDate = "2025-02-30",
            },
            "blank-section" => capture with { Section = " " },
            "markup-section" => capture with
            {
                Section = "<strong>Synthetic</strong>",
            },
            "url-section" => capture with
            {
                Section = "See https://example.invalid/",
            },
            "ftp-section" => capture with
            {
                Section = "See ftp://example.invalid/resource",
            },
            "bare-domain-section" => capture with
            {
                Section = "See example.invalid for context.",
            },
            "markdown-section" => capture with
            {
                Section = "**Synthetic section**",
            },
            "relative-url-section" => capture with
            {
                Section = "See /health/melatonin for context.",
            },
            "urn-section" => capture with
            {
                Section = "See urn:isbn:9780140328721.",
            },
            "markdown-list-section" => capture with
            {
                Section = "- Synthetic section",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                ReadyIntent(),
                capture,
                RetrievedAt));
    }

    [Fact]
    public void CreateCandidate_Intersects_Fields_With_Authorized_Uses()
    {
        var intent = ReadyIntent() with
        {
            AuthorizedFieldUses = ["identity", "regulatory"],
        };
        var capture = ValidCapture() with
        {
            Fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] =
                    ["Synthetic source-authored identity context."],
            },
        };

        var candidate = Assert.Single(
            CreateWorkflow().CreateCandidate(
                intent,
                capture,
                RetrievedAt).Candidates);

        Assert.Equal(["identity"], candidate.AuthorizedFieldUses);
        Assert.Equal(["identity_context"], candidate.Fields.Keys);
    }

    [Theory]
    [InlineData("unsupported-key")]
    [InlineData("wrong-case-key")]
    [InlineData("unauthorized-key")]
    [InlineData("empty-fields")]
    [InlineData("empty-values")]
    [InlineData("too-many-values")]
    [InlineData("duplicate-values")]
    [InlineData("blank-value")]
    [InlineData("markup-value")]
    [InlineData("markdown-link")]
    [InlineData("url-value")]
    [InlineData("ftp-url-value")]
    [InlineData("bare-domain-value")]
    [InlineData("markdown-emphasis")]
    [InlineData("markdown-heading")]
    [InlineData("markdown-strike")]
    [InlineData("relative-url-value")]
    [InlineData("magnet-uri-value")]
    [InlineData("markdown-plus-list")]
    [InlineData("markdown-number-list")]
    [InlineData("single-character-scheme")]
    [InlineData("long-markdown-number-list")]
    [InlineData("control-value")]
    [InlineData("oversized-value")]
    public void CreateCandidate_Rejects_Invalid_Or_Unauthorized_Capture_Fields(
        string mutation)
    {
        var intent = ReadyIntent();
        IReadOnlyDictionary<string, IReadOnlyList<string>> fields =
            ValidCapture().Fields;
        if (mutation == "unsupported-key")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["dose_recommendation"] = ["Synthetic captured value."],
            };
        }
        else if (mutation == "wrong-case-key")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["Identity_Context"] = ["Synthetic captured value."],
            };
        }
        else if (mutation == "unauthorized-key")
        {
            intent = intent with { AuthorizedFieldUses = ["identity"] };
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["mechanism_context"] = ["Synthetic captured value."],
            };
        }
        else if (mutation == "empty-fields")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>();
        }
        else if (mutation == "empty-values")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] = [],
            };
        }
        else if (mutation == "too-many-values")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] = Enumerable.Range(1, 9)
                    .Select(index => $"Synthetic captured value {index}.")
                    .ToList(),
            };
        }
        else if (mutation == "duplicate-values")
        {
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] =
                [
                    "Synthetic captured value.",
                    "Synthetic captured value.",
                ],
            };
        }
        else
        {
            var invalidValue = mutation switch
            {
                "blank-value" => " ",
                "markup-value" => "<em>Synthetic captured value.</em>",
                "markdown-link" => "[Synthetic](relative-link)",
                "url-value" => "See https://example.invalid/",
                "ftp-url-value" => "See ftp://example.invalid/resource",
                "bare-domain-value" => "See example.invalid for context.",
                "markdown-emphasis" => "**Synthetic captured value.**",
                "markdown-heading" => "# Synthetic captured value.",
                "markdown-strike" => "~~Synthetic captured value.~~",
                "relative-url-value" => "See /health/melatonin for context.",
                "magnet-uri-value" => "See magnet:?xt=urn:btih:synthetic.",
                "markdown-plus-list" => "+ Synthetic captured value.",
                "markdown-number-list" => "1. Synthetic captured value.",
                "single-character-scheme" => "See x:synthetic.",
                "long-markdown-number-list" =>
                    "123456789. Synthetic captured value.",
                "control-value" => "Synthetic\ncaptured value.",
                "oversized-value" => new string('s', 1_001),
                _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
            };
            fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] = [invalidValue],
            };
        }

        var capture = ValidCapture() with { Fields = fields };
        Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                intent,
                capture,
                RetrievedAt));
    }

    [Theory]
    [InlineData("same-reviewer")]
    [InlineData("review-before-capture")]
    [InlineData("review-not-approved")]
    [InlineData("non-utc-capture")]
    [InlineData("non-utc-review")]
    [InlineData("review-after-retrieval")]
    [InlineData("false-attestation")]
    [InlineData("blank-notes")]
    [InlineData("url-note")]
    [InlineData("mailto-note")]
    [InlineData("bare-domain-note")]
    [InlineData("oversized-operator")]
    public void CreateCandidate_Requires_Strict_Independent_Approved_Audit(
        string mutation)
    {
        var audit = ValidAudit();
        audit = mutation switch
        {
            "same-reviewer" => audit with
            {
                ReviewerId = audit.OperatorId,
            },
            "review-before-capture" => audit with
            {
                ReviewedAtUtc = CapturedAt.AddMinutes(-1),
            },
            "review-not-approved" => audit with
            {
                Decision = "pending",
            },
            "non-utc-capture" => audit with
            {
                CapturedAtUtc = new DateTimeOffset(
                    2026,
                    7,
                    25,
                    12,
                    0,
                    0,
                    TimeSpan.FromHours(-4)),
            },
            "non-utc-review" => audit with
            {
                ReviewedAtUtc = new DateTimeOffset(
                    2026,
                    7,
                    25,
                    12,
                    5,
                    0,
                    TimeSpan.FromHours(-4)),
            },
            "review-after-retrieval" => audit with
            {
                ReviewedAtUtc = RetrievedAt.AddMinutes(1),
            },
            "false-attestation" => audit with
            {
                Attestations = audit.Attestations with
                {
                    ExcludedRestrictedThirdPartyContent = false,
                },
            },
            "blank-notes" => audit with { Notes = [" "] },
            "url-note" => audit with
            {
                Notes = ["Reviewed https://example.invalid/"],
            },
            "mailto-note" => audit with
            {
                Notes = ["Reviewed mailto:reviewer@example.invalid"],
            },
            "bare-domain-note" => audit with
            {
                Notes = ["Reviewed example.invalid independently."],
            },
            "oversized-operator" => audit with
            {
                OperatorId = new string('o', 129),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        var capture = ValidCapture() with { Audit = audit };

        Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                ReadyIntent(),
                capture,
                RetrievedAt));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void CreateCandidate_Requires_Every_Manual_Attestation(
        int falseAttestation)
    {
        var attestations = AllAttestations();
        attestations = falseAttestation switch
        {
            0 => attestations with { SourceAuthoredTextOnly = false },
            1 => attestations with
            {
                ExcludedRestrictedThirdPartyContent = false,
            },
            2 => attestations with { AcknowledgementRetained = false },
            3 => attestations with { NoEndorsementImplication = false },
            4 => attestations with
            {
                NoIndividualizedAdviceOrDosingDirection = false,
            },
            5 => attestations with { NoRegulatoryClaim = false },
            6 => attestations with { NoSafetyCriticalConclusion = false },
            _ => throw new ArgumentOutOfRangeException(
                nameof(falseAttestation)),
        };
        var capture = ValidCapture() with
        {
            Audit = ValidAudit() with { Attestations = attestations },
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => CreateWorkflow().CreateCandidate(
                ReadyIntent(),
                capture,
                RetrievedAt));

        Assert.Equal("manual-capture-audit-invalid", exception.Code);
    }

    [Fact]
    public void Constructor_Requires_Exact_Lowercase_Registry_Hash()
    {
        Assert.Throws<ArgumentException>(
            () => new NccihManualReviewCandidateWorkflow(
                RegistrySha256.ToUpperInvariant()));
        Assert.Throws<ArgumentException>(
            () => new NccihManualReviewCandidateWorkflow("not-a-hash"));
    }

    private static NccihManualReviewCandidateWorkflow CreateWorkflow()
        => new(RegistrySha256);

    private static SourceAcquisitionIntent ReadyIntent()
        => new(
            SourceId: "nih-nccih",
            AdapterId:
                NccihManualReviewCandidateWorkflow.PlanningAdapterId,
            RequestId: "market-melatonin-001",
            CompoundName: "Melatonin",
            SearchTerms:
            [
                "Melatonin",
                "N-acetyl-5-methoxytryptamine",
            ],
            CandidateMethod: "manual-review",
            AuthorizedFieldUses:
            [
                "identity",
                "mechanism",
                "efficacy-claims",
                "interactions",
            ],
            RequiredProvenanceFields:
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "pageTitle",
                "section",
                "pageUpdatedDate",
                "retrievedAtUtc",
                "rightsReviewStatusAtRetrieval",
                "transformationPipelineVersion",
                "humanReviewStatus",
            ],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);

    private static NccihManualReviewCapture ValidCapture()
        => new(
            SourceUrl:
                NccihManualReviewCandidateWorkflow.CanonicalSourceUrl,
            PageTitle:
                NccihManualReviewCandidateWorkflow.CanonicalPageTitle,
            SourceItemSlug:
                NccihManualReviewCandidateWorkflow.CanonicalSourceItemSlug,
            PageUpdatedDate: "2025-02-15",
            Section: "Synthetic NCCIH section",
            Fields: new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity_context"] =
                    ["Synthetic source-authored identity context."],
                ["mechanism_context"] =
                    ["Synthetic source-authored mechanism context."],
                ["efficacy_context"] =
                    ["Synthetic source-authored efficacy context."],
                ["interaction_context"] =
                    ["Synthetic source-authored interaction context."],
            },
            Audit: ValidAudit());

    private static SourceManualCaptureAudit ValidAudit()
        => new(
            OperatorId: "operator.synthetic",
            CapturedAtUtc: CapturedAt,
            ReviewerId: "reviewer.synthetic",
            ReviewedAtUtc: ReviewedAt,
            Decision: "approved",
            Notes:
            [
                "Synthetic independent review confirmed source-authored text and exclusions.",
            ],
            Attestations: AllAttestations());

    private static SourceManualCaptureAttestations AllAttestations()
        => new(
            SourceAuthoredTextOnly: true,
            ExcludedRestrictedThirdPartyContent: true,
            AcknowledgementRetained: true,
            NoEndorsementImplication: true,
            NoIndividualizedAdviceOrDosingDirection: true,
            NoRegulatoryClaim: true,
            NoSafetyCriticalConclusion: true);
}
