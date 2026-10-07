namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class FdaOpenFdaDrugLabelAcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T16:00:00Z");

    [Fact]
    public async Task Acquire_Uses_Fixed_FirstParty_Endpoint_And_Escaped_Exact_Terms()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            """{"meta":{"results":{"total":0}},"results":[]}"""));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(["Semaglutide", "Ozempic"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Empty(batch.Candidates);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("api.fda.gov", request.RequestUri!.Host);
        Assert.Equal("/drug/label.json", request.RequestUri.AbsolutePath);
        Assert.Contains("openfda.generic_name", request.RequestUri.Query);
        Assert.Contains("Semaglutide", Uri.UnescapeDataString(request.RequestUri.Query));
        Assert.Contains("Ozempic", Uri.UnescapeDataString(request.RequestUri.Query));
        Assert.EndsWith("&limit=100", request.RequestUri.Query);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("other-source")]
    [InlineData("wrong-planner")]
    [InlineData("wrong-method")]
    [InlineData("stale-registry")]
    [InlineData("missing-provenance")]
    public async Task Acquire_Rejects_Unauthorized_Intent_Before_Http(string mutation)
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent(["Semaglutide"]);
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["fda:legal-rights-not-approved"],
            },
            "other-source" => intent with { SourceId = "pubmed" },
            "wrong-planner" => intent with { AdapterId = "fda-planning-v2" },
            "wrong-method" => intent with { CandidateMethod = "bulk-download" },
            "stale-registry" => intent with { RegistryBindingSha256 = new string('0', 64) },
            "missing-provenance" => intent with { RequiredProvenanceFields = ["sourceItemId"] },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Deduplicated_Allowlisted_ReviewRequired_Candidates()
    {
        var body = await File.ReadAllTextAsync(
            TestPaths.FixturePath("openfda-drug-label.synthetic.json"));
        var handler = new RecordingHandler(_ => JsonResponse(body));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.True(batch.Truncated);
        var candidate = Assert.Single(batch.Candidates);
        Assert.Equal("market-semaglutide-001", candidate.RequestId);
        Assert.Equal("Semaglutide", candidate.CompoundName);
        Assert.Equal("fda", candidate.SourceRegistryId);
        Assert.Equal("label-semaglutide-001", candidate.SourceItemId);
        Assert.Contains("search=id", candidate.SourceUrl);
        Assert.EndsWith("&limit=1", candidate.SourceUrl);
        Assert.Contains("Semaglutide", Uri.UnescapeDataString(candidate.QueryUrl));
        Assert.Equal("20260701", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(RetrievedAt, candidate.RetrievedAtUtc);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal(RegistrySha256, candidate.RegistryBindingSha256);
        Assert.Equal(
            FdaOpenFdaDrugLabelAcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(2, candidate.EvidenceLimitations.Count);
        Assert.Equal(["SEMAGLUTIDE"], candidate.Fields["openfda.generic_name"]);
        Assert.Equal(
            ["HUMAN PRESCRIPTION DRUG"],
            candidate.Fields["openfda.product_type"]);
        Assert.Equal(
            ["Synthetic interaction fixture text."],
            candidate.Fields["drug_interactions"]);
        Assert.DoesNotContain("dosage_and_administration", candidate.Fields.Keys);
        Assert.DoesNotContain("gmdn_terms", candidate.Fields.Keys);
        Assert.DoesNotContain("unexpected_field", candidate.Fields.Keys);
    }

    [Fact]
    public async Task Acquire_Emits_RuntimeCompatible_Provenance_And_Rights_Envelope()
    {
        var intent = ReadyIntent(["Semaglutide"]);
        var candidate = await AcquireFixtureCandidateAsync(intent);

        Assert.Equal(
            [
                "approved-indications",
                "contraindications-warnings",
                "identity",
                "interactions",
                "regulatory",
            ],
            candidate.AuthorizedFieldUses);
        Assert.Equal(
            ["label-semaglutide-001"],
            candidate.SourceSpecificProvenance["labelId"].Values);
        Assert.Equal(
            ["20260701"],
            candidate.SourceSpecificProvenance["effectiveTime"].Values);
        Assert.Equal(
            ["set-semaglutide-001"],
            candidate.SourceSpecificProvenance["labelSetId"].Values);
        Assert.Equal(
            ["7"],
            candidate.SourceSpecificProvenance["labelVersion"].Values);
        Assert.All(
            candidate.SourceSpecificProvenance.Values,
            value =>
            {
                Assert.Equal("present", value.Availability);
                Assert.NotEmpty(value.Values);
                Assert.Empty(value.UnavailableReason);
            });

        Assert.Equal(2, candidate.RightsAttributions.Count);
        Assert.All(
            candidate.RightsAttributions,
            attribution =>
            {
                Assert.Equal("reviewed", attribution.RightsStatus);
                Assert.Contains("FDA", attribution.Provider);
                Assert.StartsWith("https://", attribution.SourceUrl);
                Assert.StartsWith("https://", attribution.TermsUrl);
                Assert.NotEmpty(attribution.CoveredFields);
            });
        Assert.Equal(
            candidate.Fields.Keys.Order(StringComparer.OrdinalIgnoreCase),
            candidate.RightsAttributions
                .SelectMany(attribution => attribution.CoveredFields)
                .Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            candidate.Fields.Count,
            candidate.RightsAttributions
                .SelectMany(attribution => attribution.CoveredFields)
                .Count());

        var document = Assert.Single(candidate.DocumentProvenance);
        Assert.Contains(candidate.SourceItemId, document.Title);
        Assert.Empty(document.PublishedDate);
        Assert.Equal(candidate.SourcePublicationOrUpdateDate, document.UpdatedDate);
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        Assert.Contains("does not imply", candidate.ReuseBoundary.Acknowledgement);
        Assert.Contains(
            candidate.ReuseBoundary.ExcludedContentClasses,
            exclusion => exclusion.Contains("GMDN", StringComparison.Ordinal));
        Assert.Contains(
            candidate.ReuseBoundary.ExcludedContentClasses,
            exclusion => exclusion.Contains("third-party", StringComparison.Ordinal));
        Assert.Contains(
            candidate.ReuseBoundary.ExcludedContentClasses,
            exclusion => exclusion.Contains(
                "individualized",
                StringComparison.Ordinal));
        Assert.Null(candidate.ManualCaptureAudit);

        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            intent.RequiredProvenanceFields,
            expectedSourceRegistryId: "fda",
            expectedRegistrySha256: RegistrySha256);
        FdaOpenFdaDrugLabelAcquisitionAdapter.ValidateCandidateEnvelope(candidate);
    }

    [Theory]
    [InlineData("missing", "source-publication-date-missing")]
    [InlineData("blank", "source-publication-date-missing")]
    [InlineData("wrong-format", "source-publication-date-invalid")]
    [InlineData("invalid-calendar-date", "source-publication-date-invalid")]
    public async Task Acquire_Rejects_Missing_Or_Invalid_EffectiveTime(
        string scenario,
        string expectedCode)
    {
        var effectiveTime = scenario switch
        {
            "missing" => string.Empty,
            "blank" => ",\"effective_time\":\" \"",
            "wrong-format" => ",\"effective_time\":\"2026-07-01\"",
            "invalid-calendar-date" => ",\"effective_time\":\"20260230\"",
            _ => throw new ArgumentOutOfRangeException(nameof(scenario)),
        };
        var body =
            "{\"meta\":{\"results\":{\"total\":1}},\"results\":[{\"id\":\"label-1\""
            + effectiveTime
            + "}]}";
        var adapter = CreateAdapter(new RecordingHandler(_ => JsonResponse(body)));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Theory]
    [InlineData("missing-rights", "candidate-rights-attribution-invalid")]
    [InlineData("unreviewed-rights", "candidate-rights-attribution-invalid")]
    [InlineData("missing-provenance", "candidate-source-provenance-invalid")]
    [InlineData("covered-field-gap", "candidate-rights-covered-fields-mismatch")]
    [InlineData("unrepresented-use", "candidate-authorized-field-use-invalid")]
    [InlineData("missing-transformation", "candidate-date-or-transformation-invalid")]
    [InlineData("fabricated-published-date", "candidate-document-provenance-invalid")]
    public async Task CandidateEnvelope_Rejects_Incomplete_Or_Mismatched_Metadata(
        string mutation,
        string expectedCode)
    {
        var candidate = await AcquireFixtureCandidateAsync(
            ReadyIntent(["Semaglutide"]));
        var mutated = MutateCandidate(candidate, mutation);

        var exception = Assert.Throws<SourceAcquisitionException>(
            () => FdaOpenFdaDrugLabelAcquisitionAdapter
                .ValidateCandidateEnvelope(mutated));

        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public async Task Acquire_Emits_Only_Field_Groups_Authorized_By_Intent()
    {
        var body = await File.ReadAllTextAsync(
            TestPaths.FixturePath("openfda-drug-label.synthetic.json"));
        var handler = new RecordingHandler(_ => JsonResponse(body));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent(["Semaglutide"]) with
        {
            AuthorizedFieldUses = ["identity"],
        };

        var candidate = Assert.Single(
            (await adapter.AcquireAsync(intent, RetrievedAt)).Candidates);

        Assert.Contains("openfda.generic_name", candidate.Fields.Keys);
        Assert.DoesNotContain("openfda.application_number", candidate.Fields.Keys);
        Assert.DoesNotContain("openfda.product_type", candidate.Fields.Keys);
        Assert.DoesNotContain("indications_and_usage", candidate.Fields.Keys);
        Assert.DoesNotContain("warnings", candidate.Fields.Keys);
        Assert.DoesNotContain("drug_interactions", candidate.Fields.Keys);
    }

    [Fact]
    public async Task Acquire_Treats_404_As_NoMatch()
    {
        var handler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.NotFound));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(["No Match"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Surfaces_429_And_RetryAfter_Without_Retrying()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter = new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, batch.Status);
        Assert.Equal("30", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_Redirect_Response()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://example.invalid/");
            return response;
        });
        var adapter = CreateAdapter(handler);

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));

        Assert.Equal("redirect-response", exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_NonJson_And_Malformed_Responses()
    {
        var nonJsonHandler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain"),
            });
        var malformedHandler = new RecordingHandler(_ => JsonResponse("{"));

        var nonJson = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(nonJsonHandler)
                .AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));
        var malformed = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(malformedHandler)
                .AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));

        Assert.Equal("unexpected-content-type", nonJson.Code);
        Assert.Equal("malformed-json", malformed.Code);
    }

    [Fact]
    public async Task Acquire_Rejects_Missing_SourceItemId()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            """{"meta":{"results":{"total":1}},"results":[{"effective_time":"20260701"}]}"""));
        var adapter = CreateAdapter(handler);

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));

        Assert.Equal("source-item-id-missing", exception.Code);
    }

    [Fact]
    public async Task Acquire_Rejects_Response_Over_Configured_Size()
    {
        var handler = new RecordingHandler(_ => JsonResponse(
            """{"meta":{"results":{"total":0}},"results":[]}"""));
        var adapter = CreateAdapter(handler, maximumResponseBytes: 10);

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt));

        Assert.Equal("response-too-large", exception.Code);
    }

    [Theory]
    [InlineData("Semaglutide\nInjected")]
    [InlineData("")]
    public async Task Acquire_Rejects_Invalid_Search_Terms_Before_Http(string term)
    {
        var handler = new RecordingHandler(_ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(ReadyIntent([term]), RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task RequestGate_Serializes_Concurrent_Acquisitions()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new FdaOpenFdaRequestGate(time);
        var firstLease = await gate.AcquireAsync(CancellationToken.None);

        var second = gate.AcquireAsync(CancellationToken.None).AsTask();

        Assert.False(second.IsCompleted);
        firstLease.Dispose();
        using var secondLease = await second;
    }

    [Fact]
    public async Task RequestGate_Enforces_Minute_Budget()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new FdaOpenFdaRequestGate(time);
        for (var index = 0; index < FdaOpenFdaRequestGate.MaximumRequestsPerMinute; index++)
        {
            using var lease = await gate.AcquireAsync(CancellationToken.None);
        }

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());

        Assert.Equal("local-minute-budget-exhausted", exception.Code);
    }

    [Fact]
    public async Task RequestGate_Enforces_Daily_Budget()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new FdaOpenFdaRequestGate(time);
        for (var index = 0; index < FdaOpenFdaRequestGate.MaximumRequestsPerDay; index++)
        {
            if (index > 0 && index % FdaOpenFdaRequestGate.MaximumRequestsPerMinute == 0)
            {
                time.Advance(TimeSpan.FromMinutes(1));
            }
            using var lease = await gate.AcquireAsync(CancellationToken.None);
        }

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());

        Assert.Equal("local-daily-budget-exhausted", exception.Code);
    }

    [Fact]
    public async Task RequestGate_Preserves_Rolling_Minute_Budget_Across_Utc_Midnight()
    {
        var time = new ManualTimeProvider(
            DateTimeOffset.Parse("2026-07-25T23:59:50Z"));
        var gate = new FdaOpenFdaRequestGate(time);
        for (var index = 0; index < FdaOpenFdaRequestGate.MaximumRequestsPerMinute; index++)
        {
            using var lease = await gate.AcquireAsync(CancellationToken.None);
        }
        time.Advance(TimeSpan.FromSeconds(20));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => gate.AcquireAsync(CancellationToken.None).AsTask());

        Assert.Equal("local-minute-budget-exhausted", exception.Code);
        time.Advance(TimeSpan.FromSeconds(40));
        using var afterMinute = await gate.AcquireAsync(CancellationToken.None);
    }

    private static FdaOpenFdaDrugLabelAcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int maximumResponseBytes =
            FdaOpenFdaDrugLabelAcquisitionAdapter.DefaultMaximumResponseBytes)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            maximumResponseBytes,
            new FdaOpenFdaRequestGate(TimeProvider.System));

    private static async Task<SourceAcquisitionCandidate>
        AcquireFixtureCandidateAsync(SourceAcquisitionIntent intent)
    {
        var body = await File.ReadAllTextAsync(
            TestPaths.FixturePath("openfda-drug-label.synthetic.json"));
        var adapter = CreateAdapter(
            new RecordingHandler(_ => JsonResponse(body)));
        return Assert.Single(
            (await adapter.AcquireAsync(intent, RetrievedAt)).Candidates);
    }

    private static SourceAcquisitionCandidate MutateCandidate(
        SourceAcquisitionCandidate candidate,
        string mutation)
        => mutation switch
        {
            "missing-rights" => candidate with { RightsAttributions = [] },
            "unreviewed-rights" => candidate with
            {
                RightsAttributions =
                [
                    candidate.RightsAttributions[0] with
                    {
                        RightsStatus = "review-required",
                    },
                    candidate.RightsAttributions[1],
                ],
            },
            "missing-provenance" => candidate with
            {
                SourceSpecificProvenance =
                    new Dictionary<string, SourceProvenanceValue>(),
            },
            "covered-field-gap" => candidate with
            {
                RightsAttributions =
                [
                    candidate.RightsAttributions[0] with
                    {
                        CoveredFields = candidate.RightsAttributions[0]
                            .CoveredFields
                            .Skip(1)
                            .ToList(),
                    },
                    candidate.RightsAttributions[1],
                ],
            },
            "unrepresented-use" => candidate with
            {
                AuthorizedFieldUses =
                    candidate.AuthorizedFieldUses.Concat(["monitoring"]).ToList(),
            },
            "missing-transformation" => candidate with
            {
                TransformationPipelineVersion = string.Empty,
            },
            "fabricated-published-date" => candidate with
            {
                DocumentProvenance =
                [
                    candidate.DocumentProvenance[0] with
                    {
                        PublishedDate =
                            candidate.SourcePublicationOrUpdateDate,
                    },
                ],
            },
            _ => throw new ArgumentOutOfRangeException(
                nameof(mutation),
                mutation,
                "Unknown candidate mutation."),
        };

    private static SourceAcquisitionIntent ReadyIntent(IReadOnlyList<string> searchTerms)
        => new(
            SourceId: "fda",
            AdapterId: FdaOpenFdaDrugLabelAcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-semaglutide-001",
            CompoundName: "Semaglutide",
            SearchTerms: searchTerms,
            CandidateMethod: "api",
            AuthorizedFieldUses:
            [
                "identity",
                "regulatory",
                "approved-indications",
                "contraindications-warnings",
                "monitoring",
                "interactions",
                "misinformation-monitoring",
            ],
            RequiredProvenanceFields:
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "sourcePublicationOrUpdateDate",
                "retrievedAtUtc",
                "rightsReviewStatusAtRetrieval",
                "transformationPipelineVersion",
                "humanReviewStatus",
            ],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: Array.Empty<string>());

    private static HttpResponseMessage JsonResponse(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, HttpResponseMessage> responseFactory)
        : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(request);
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }
}
