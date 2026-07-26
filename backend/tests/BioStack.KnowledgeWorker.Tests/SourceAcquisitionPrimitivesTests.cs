namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Text;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class SourceAcquisitionPrimitivesTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T16:00:00Z");

    [Fact]
    public void Candidate_Extensions_Are_AppendOnly_And_Default_Empty()
    {
        var candidate = CandidateWithDefaults() with { QueryUrl = null };

        Assert.Null(candidate.QueryUrl);
        Assert.Empty(candidate.AuthorizedFieldUses);
        Assert.Empty(candidate.SourceSpecificProvenance);
        Assert.Empty(candidate.RightsAttributions);
        Assert.Empty(candidate.DocumentProvenance);
        Assert.Equal(SourceReuseBoundary.Unspecified, candidate.ReuseBoundary);
        Assert.Null(candidate.ManualCaptureAudit);
        Assert.Equal(2, (int)SourceAcquisitionBatchStatus.RateLimited);
        Assert.Equal(3, (int)SourceAcquisitionBatchStatus.BackPressure);
    }

    [Fact]
    public void CandidateGuard_Accepts_Common_And_Present_Source_Provenance()
    {
        var candidate = Candidate() with
        {
            AuthorizedFieldUses = ["identity"],
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["pubchemCid"] = SourceProvenanceValue.Present("2244"),
                },
        };

        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "queryUrl",
                "sourcePublicationOrUpdateDate",
                "retrievedAtUtc",
                "rightsReviewStatusAtRetrieval",
                "registryBindingSha256",
                "transformationPipelineVersion",
                "humanReviewStatus",
                "authorizedFieldUses",
                "pubchemCid",
            ],
            expectedSourceRegistryId: "pubchem",
            expectedRegistrySha256: RegistrySha256);
    }

    [Theory]
    [InlineData("doi")]
    [InlineData("pmcid")]
    [InlineData("productNdc")]
    public void CandidateGuard_Requires_PerKey_NotProvided_Allowlist(string key)
    {
        var candidate = Candidate() with
        {
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    [key] = SourceProvenanceValue.NotProvided(
                        $"The source record did not provide {key}."),
                },
        };

        var rejected = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                [key],
                expectedSourceRegistryId: "pubchem",
                expectedRegistrySha256: RegistrySha256));
        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            [key],
            expectedSourceRegistryId: "pubchem",
            expectedRegistrySha256: RegistrySha256,
            allowedNotProvidedFields:
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { key });

        Assert.Equal("candidate-required-provenance-missing", rejected.Code);
    }

    [Fact]
    public void CandidateGuard_Requires_Separate_NotApplicable_Allowlist()
    {
        var candidate = Candidate() with
        {
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["phase"] = SourceProvenanceValue.NotApplicable(
                        "The study type does not use a trial phase."),
                },
        };

        var rejected = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                ["phase"],
                expectedSourceRegistryId: "pubchem",
                expectedRegistrySha256: RegistrySha256));
        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            ["phase"],
            expectedSourceRegistryId: "pubchem",
            expectedRegistrySha256: RegistrySha256,
            allowedNotApplicableFields:
                new HashSet<string>(StringComparer.OrdinalIgnoreCase) { "phase" });

        Assert.Equal("candidate-required-provenance-missing", rejected.Code);
    }

    [Theory]
    [InlineData("")]
    [InlineData("N/A")]
    [InlineData("unknown")]
    [InlineData("not provided")]
    [InlineData("not-provided")]
    [InlineData("not applicable")]
    [InlineData("not-applicable")]
    public void CandidateGuard_Rejects_Blank_And_Sentinel_Provenance(string value)
    {
        var candidate = Candidate() with
        {
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["pubchemCid"] = SourceProvenanceValue.Present(value),
                },
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                ["pubchemCid"],
                expectedSourceRegistryId: "pubchem",
                expectedRegistrySha256: RegistrySha256));

        Assert.Equal("candidate-required-provenance-missing", exception.Code);
    }

    [Fact]
    public void CandidateGuard_Requires_Exact_Governed_Common_Values()
    {
        var candidate = Candidate() with
        {
            RetrievedAtUtc = new DateTimeOffset(
                2026,
                7,
                25,
                12,
                0,
                0,
                TimeSpan.FromHours(-4)),
            RightsReviewStatusAtRetrieval = "pending",
            HumanReviewStatus = "approved",
            RegistryBindingSha256 = new string('A', 64),
        };

        foreach (var field in new[]
                 {
                     "retrievedAtUtc",
                     "rightsReviewStatusAtRetrieval",
                     "humanReviewStatus",
                     "registryBindingSha256",
                 })
        {
            var exception = Assert.Throws<SourceAcquisitionException>(
                () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                    candidate,
                    [field],
                    expectedSourceRegistryId: "pubchem",
                    expectedRegistrySha256: RegistrySha256));
            Assert.Equal("candidate-core-invariant-invalid", exception.Code);
        }
    }

    [Theory]
    [InlineData("http-source")]
    [InlineData("userinfo-source")]
    [InlineData("http-query")]
    [InlineData("userinfo-query")]
    [InlineData("wrong-source")]
    [InlineData("wrong-hash")]
    [InlineData("empty-authorized")]
    [InlineData("empty-evidence")]
    [InlineData("empty-fields")]
    public void CandidateGuard_Rejects_Unsafe_Or_Empty_Core_Candidate(string mutation)
    {
        var candidate = Candidate();
        candidate = mutation switch
        {
            "http-source" => candidate with
            {
                SourceUrl = "http://pubchem.ncbi.nlm.nih.gov/compound/2244",
            },
            "userinfo-source" => candidate with
            {
                SourceUrl =
                    "https://user:secret@pubchem.ncbi.nlm.nih.gov/compound/2244",
            },
            "http-query" => candidate with
            {
                QueryUrl =
                    "http://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/aspirin",
            },
            "userinfo-query" => candidate with
            {
                QueryUrl =
                    "https://user:secret@pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/aspirin",
            },
            "wrong-source" => candidate with { SourceRegistryId = "pubmed" },
            "wrong-hash" => candidate with { RegistryBindingSha256 = new string('0', 64) },
            "empty-authorized" => candidate with { AuthorizedFieldUses = [] },
            "empty-evidence" => candidate with { EvidenceLimitations = [] },
            "empty-fields" => candidate with
            {
                Fields = new Dictionary<string, IReadOnlyList<string>>(),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                [],
                expectedSourceRegistryId: "pubchem",
                expectedRegistrySha256: RegistrySha256));

        Assert.Equal("candidate-core-invariant-invalid", exception.Code);
    }

    [Fact]
    public void CandidateGuard_Rejects_Unavailable_HardRequired_PubChem_Update_Date()
    {
        var candidate = Candidate() with
        {
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["recordUpdateDate"] = SourceProvenanceValue.NotProvided(
                        "The response did not provide a record update date."),
                },
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                ["recordUpdateDate"],
                expectedSourceRegistryId: "pubchem",
                expectedRegistrySha256: RegistrySha256));

        Assert.Equal("candidate-required-provenance-missing", exception.Code);
    }

    [Fact]
    public void ManualCaptureGuard_Requires_Independent_Approved_Attested_Review()
    {
        var audit = ValidManualAudit();
        var candidate = Candidate() with
        {
            QueryUrl = null,
            ManualCaptureAudit = audit,
        };

        SourceAcquisitionCandidateGuard.ValidateApprovedManualCaptureAudit(candidate);

        var sameReviewer = candidate with
        {
            ManualCaptureAudit = audit with { ReviewerId = "OPERATOR-1" },
        };
        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard
                .ValidateApprovedManualCaptureAudit(sameReviewer));
        Assert.Equal("manual-capture-audit-invalid", exception.Code);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(4)]
    [InlineData(5)]
    [InlineData(6)]
    public void ManualCaptureGuard_Requires_Every_Attestation(int falseAttestation)
    {
        var attestations = AllManualAttestations();
        attestations = falseAttestation switch
        {
            0 => attestations with { SourceAuthoredTextOnly = false },
            1 => attestations with { ExcludedRestrictedThirdPartyContent = false },
            2 => attestations with { AcknowledgementRetained = false },
            3 => attestations with { NoEndorsementImplication = false },
            4 => attestations with { NoIndividualizedAdviceOrDosingDirection = false },
            5 => attestations with { NoRegulatoryClaim = false },
            6 => attestations with { NoSafetyCriticalConclusion = false },
            _ => throw new ArgumentOutOfRangeException(nameof(falseAttestation)),
        };
        var candidate = Candidate() with
        {
            QueryUrl = null,
            ManualCaptureAudit = ValidManualAudit() with
            {
                Attestations = attestations,
            },
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard
                .ValidateApprovedManualCaptureAudit(candidate));

        Assert.Equal("manual-capture-audit-invalid", exception.Code);
    }

    [Theory]
    [InlineData("timestamp-inversion")]
    [InlineData("capture-non-utc")]
    [InlineData("review-non-utc")]
    [InlineData("not-approved")]
    [InlineData("query-present")]
    public void ManualCaptureGuard_Rejects_Invalid_Review_State(string mutation)
    {
        var audit = ValidManualAudit();
        var candidate = Candidate() with
        {
            QueryUrl = null,
            ManualCaptureAudit = audit,
        };
        candidate = mutation switch
        {
            "timestamp-inversion" => candidate with
            {
                ManualCaptureAudit = audit with
                {
                    ReviewedAtUtc = audit.CapturedAtUtc.AddMinutes(-1),
                },
            },
            "capture-non-utc" => candidate with
            {
                ManualCaptureAudit = audit with
                {
                    CapturedAtUtc = new DateTimeOffset(
                        2026,
                        7,
                        25,
                        11,
                        50,
                        0,
                        TimeSpan.FromHours(-4)),
                },
            },
            "review-non-utc" => candidate with
            {
                ManualCaptureAudit = audit with
                {
                    ReviewedAtUtc = new DateTimeOffset(
                        2026,
                        7,
                        25,
                        11,
                        55,
                        0,
                        TimeSpan.FromHours(-4)),
                },
            },
            "not-approved" => candidate with
            {
                ManualCaptureAudit = audit with { Decision = "rejected" },
            },
            "query-present" => candidate with
            {
                QueryUrl = "https://www.nccih.nih.gov/",
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionCandidateGuard
                .ValidateApprovedManualCaptureAudit(candidate));

        Assert.Equal("manual-capture-audit-invalid", exception.Code);
    }

    [Fact]
    public void IntentGuard_Validates_Ready_RegistryBound_Declared_Provenance()
    {
        var requirements = new SourceAcquisitionIntentRequirements(
            SourceId: "pubchem",
            SourceDisplayName: "PubChem",
            PlanningAdapterId: "pubchem-planning-v1",
            CandidateMethod: "api",
            ExpectedRegistrySha256: RegistrySha256,
            RequiredProvenanceFields: ["sourceItemId", "pubchemCid"]);
        var intent = new SourceAcquisitionIntent(
            SourceId: "pubchem",
            AdapterId: "pubchem-planning-v1",
            RequestId: "market-aspirin-001",
            CompoundName: "Aspirin",
            SearchTerms: ["Aspirin"],
            CandidateMethod: "api",
            AuthorizedFieldUses: ["identity"],
            RequiredProvenanceFields: ["sourceItemId", "pubchemCid"],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);

        SourceAcquisitionIntentGuard.Validate(intent, RetrievedAt, requirements);

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionIntentGuard.Validate(
                intent with { RequiredProvenanceFields = ["sourceItemId"] },
                RetrievedAt,
                requirements));
        Assert.Equal("required-provenance-missing", exception.Code);

        var defaultTime = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionIntentGuard.Validate(
                intent,
                default,
                requirements));
        Assert.Equal("retrieval-timestamp-not-utc", defaultTime.Code);
    }

    [Theory]
    [InlineData("disposition", "intent-not-ready")]
    [InlineData("blocker", "intent-not-ready")]
    [InlineData("source", "source-not-supported")]
    [InlineData("planner", "planning-adapter-mismatch")]
    [InlineData("method", "acquisition-method-not-supported")]
    [InlineData("hash", "source-registry-sha256-mismatch")]
    [InlineData("provenance", "required-provenance-missing")]
    [InlineData("non-utc", "retrieval-timestamp-not-utc")]
    [InlineData("default-time", "retrieval-timestamp-not-utc")]
    public void IntentGuard_Rejects_Every_Unauthorized_Mutation(
        string mutation,
        string expectedCode)
    {
        var intent = ReadyPubChemIntent();
        var retrievedAt = RetrievedAt;
        (intent, retrievedAt) = mutation switch
        {
            "disposition" => (
                intent with { Disposition = SourceAcquisitionDisposition.Blocked },
                retrievedAt),
            "blocker" => (
                intent with { BlockingReasons = ["pubchem:blocked"] },
                retrievedAt),
            "source" => (intent with { SourceId = "pubmed" }, retrievedAt),
            "planner" => (
                intent with { AdapterId = "pubchem-planning-v2" },
                retrievedAt),
            "method" => (
                intent with { CandidateMethod = "bulk-download" },
                retrievedAt),
            "hash" => (
                intent with { RegistryBindingSha256 = new string('0', 64) },
                retrievedAt),
            "provenance" => (
                intent with { RequiredProvenanceFields = ["sourceItemId"] },
                retrievedAt),
            "non-utc" => (
                intent,
                new DateTimeOffset(
                    2026,
                    7,
                    25,
                    12,
                    0,
                    0,
                    TimeSpan.FromHours(-4))),
            "default-time" => (intent, default),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => SourceAcquisitionIntentGuard.Validate(
                intent,
                retrievedAt,
                PubChemIntentRequirements()));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public async Task HttpTransport_Is_RedirectDisabled_And_Bounds_Response_Bodies()
    {
        using var handler =
            SourceAcquisitionHttpTransport.CreateRedirectDisabledHandler();
        using var body = new StringContent(
            "bounded source body",
            Encoding.UTF8,
            "application/json");

        var bytes = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            body,
            maximumResponseBytes: 1024,
            sourceDisplayName: "synthetic",
            CancellationToken.None);
        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
                body,
                maximumResponseBytes: 2,
                sourceDisplayName: "synthetic",
                CancellationToken.None));

        Assert.False(handler.AllowAutoRedirect);
        Assert.Equal("bounded source body", Encoding.UTF8.GetString(bytes));
        Assert.Equal("response-too-large", exception.Code);
    }

    [Fact]
    public async Task HttpTransport_Enforces_Streamed_Cap_Without_ContentLength()
    {
        using var content = new UnknownLengthContent(
            Encoding.UTF8.GetBytes("streamed body beyond cap"));
        Assert.Null(content.Headers.ContentLength);

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
                content,
                maximumResponseBytes: 4,
                sourceDisplayName: "synthetic",
                CancellationToken.None));

        Assert.Equal("response-too-large", exception.Code);
    }

    [Fact]
    public async Task HttpTransport_Honors_Cancellation()
    {
        using var content = new UnknownLengthContent(
            Encoding.UTF8.GetBytes("cancelled body"));
        using var cancellation = new CancellationTokenSource();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
                content,
                maximumResponseBytes: 1024,
                sourceDisplayName: "synthetic",
                cancellation.Token));
    }

    [Fact]
    public async Task RequestGate_Allows_Serialization_Without_Invented_Quota()
    {
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            [],
            dailyBudget: null);
        var firstLease = await gate.AcquireAsync(CancellationToken.None);

        var second = gate.AcquireAsync(CancellationToken.None).AsTask();

        Assert.False(second.IsCompleted);
        firstLease.Dispose();
        using var secondLease = await second;
    }

    [Fact]
    public async Task RequestGate_Enforces_Multiple_Sliding_Windows_And_Daily_Budget()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new SerializedSourceRequestGate(
            time,
            [
                new SourceSlidingWindowBudget(
                    2,
                    TimeSpan.FromSeconds(1),
                    "second-budget",
                    "Second budget exhausted."),
                new SourceSlidingWindowBudget(
                    3,
                    TimeSpan.FromMinutes(1),
                    "minute-budget",
                    "Minute budget exhausted."),
            ],
            new SourceDailyRequestBudget(
                4,
                "daily-budget",
                "Daily budget exhausted."));
        for (var index = 0; index < 2; index++)
        {
            using var lease = await gate.AcquireAsync(CancellationToken.None);
        }

        var secondBudget = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());
        time.Advance(TimeSpan.FromSeconds(1));
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }
        var minuteBudget = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());
        time.Advance(TimeSpan.FromMinutes(1));
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }
        var dailyBudget = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());

        Assert.Equal("second-budget", secondBudget.Code);
        Assert.Equal("minute-budget", minuteBudget.Code);
        Assert.Equal("daily-budget", dailyBudget.Code);
    }

    [Fact]
    public async Task RequestGate_Honors_Wait_Cancellation_And_Double_Dispose()
    {
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            [],
            dailyBudget: null);
        var first = await gate.AcquireAsync(CancellationToken.None);
        using var cancellation = new CancellationTokenSource();
        var cancelledWait = gate.AcquireAsync(cancellation.Token).AsTask();
        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => cancelledWait);
        first.Dispose();
        first.Dispose();

        var second = await gate.AcquireAsync(CancellationToken.None);
        var third = gate.AcquireAsync(CancellationToken.None).AsTask();
        Assert.False(third.IsCompleted);
        second.Dispose();
        using var thirdLease = await third;
    }

    [Fact]
    public async Task RequestGate_Reopens_At_Exact_Sliding_Window_Boundary()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new SerializedSourceRequestGate(
            time,
            [
                new SourceSlidingWindowBudget(
                    1,
                    TimeSpan.FromMinutes(1),
                    "minute-budget",
                    "Minute budget exhausted."),
            ],
            dailyBudget: null);
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }

        var blocked = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());
        time.Advance(TimeSpan.FromMinutes(1));
        using var boundaryLease = await gate.AcquireAsync(CancellationToken.None);

        Assert.Equal("minute-budget", blocked.Code);
    }

    [Fact]
    public async Task RequestGate_Preserves_Rolling_Window_Across_Utc_Midnight()
    {
        var time = new ManualTimeProvider(
            DateTimeOffset.Parse("2026-07-25T23:59:50Z"));
        var gate = new SerializedSourceRequestGate(
            time,
            [
                new SourceSlidingWindowBudget(
                    1,
                    TimeSpan.FromMinutes(1),
                    "minute-budget",
                    "Minute budget exhausted."),
            ],
            new SourceDailyRequestBudget(
                10,
                "daily-budget",
                "Daily budget exhausted."));
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }
        time.Advance(TimeSpan.FromSeconds(20));

        var blocked = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());
        time.Advance(TimeSpan.FromSeconds(40));
        using var boundaryLease = await gate.AcquireAsync(CancellationToken.None);

        Assert.Equal("minute-budget", blocked.Code);
    }

    private static SourceAcquisitionCandidate Candidate()
        => CandidateWithDefaults() with
        {
            AuthorizedFieldUses = ["identity"],
            Fields = new Dictionary<string, IReadOnlyList<string>>
            {
                ["identity"] = ["Aspirin"],
            },
        };

    private static SourceAcquisitionCandidate CandidateWithDefaults()
        => new(
            RequestId: "market-aspirin-001",
            CompoundName: "Aspirin",
            SourceRegistryId: "pubchem",
            SourceItemId: "2244",
            SourceUrl: "https://pubchem.ncbi.nlm.nih.gov/compound/2244",
            QueryUrl:
                "https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/aspirin/property/JSON",
            SourcePublicationOrUpdateDate: "2026-07-01",
            RetrievedAtUtc: RetrievedAt,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: RegistrySha256,
            TransformationPipelineVersion: "pubchem-pug-rest-v1",
            HumanReviewStatus: "review-required",
            EvidenceLimitations: ["Synthetic test candidate."],
            Fields: new Dictionary<string, IReadOnlyList<string>>());

    private static SourceAcquisitionIntent ReadyPubChemIntent()
        => new(
            SourceId: "pubchem",
            AdapterId: "pubchem-planning-v1",
            RequestId: "market-aspirin-001",
            CompoundName: "Aspirin",
            SearchTerms: ["Aspirin"],
            CandidateMethod: "api",
            AuthorizedFieldUses: ["identity"],
            RequiredProvenanceFields: ["sourceItemId", "pubchemCid"],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);

    private static SourceAcquisitionIntentRequirements PubChemIntentRequirements()
        => new(
            SourceId: "pubchem",
            SourceDisplayName: "PubChem",
            PlanningAdapterId: "pubchem-planning-v1",
            CandidateMethod: "api",
            ExpectedRegistrySha256: RegistrySha256,
            RequiredProvenanceFields: ["sourceItemId", "pubchemCid"]);

    private static SourceManualCaptureAudit ValidManualAudit()
        => new(
            OperatorId: "operator-1",
            CapturedAtUtc: RetrievedAt.AddMinutes(-10),
            ReviewerId: "reviewer-1",
            ReviewedAtUtc: RetrievedAt.AddMinutes(-5),
            Decision: "approved",
            Notes: ["First-party source-authored text verified."],
            Attestations: AllManualAttestations());

    private static SourceManualCaptureAttestations AllManualAttestations()
        => new(
            SourceAuthoredTextOnly: true,
            ExcludedRestrictedThirdPartyContent: true,
            AcknowledgementRetained: true,
            NoEndorsementImplication: true,
            NoIndividualizedAdviceOrDosingDirection: true,
            NoRegulatoryClaim: true,
            NoSafetyCriticalConclusion: true);

    private sealed class UnknownLengthContent(byte[] bytes) : HttpContent
    {
        protected override Task SerializeToStreamAsync(
            Stream stream,
            TransportContext? context)
            => stream.WriteAsync(bytes).AsTask();

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }
}
