namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class NihOdsFactSheetAcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T20:00:00Z");

    [Fact]
    public async Task Acquire_Uses_Exact_Final_FirstParty_Endpoint_Once()
    {
        var handler = new RecordingHandler(_ => XmlResponse(Fixture()));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(
            NihOdsFactSheetAcquisitionAdapter.FixedEndpoint,
            request.RequestUri!.AbsoluteUri);
        Assert.Equal("ods.od.nih.gov", request.RequestUri.Host);
        Assert.Equal("/api/", request.RequestUri.AbsolutePath);
        Assert.Contains(
            request.Headers.Accept,
            value => value.MediaType == "application/xml");
        Assert.Contains(
            request.Headers.Accept,
            value => value.MediaType == "text/xml");
    }

    [Theory]
    [InlineData("Semaglutide", "Semaglutide")]
    [InlineData("Glutathione", "GSH")]
    [InlineData("Glutathione", "Glutathione hydrochloride")]
    [InlineData("glutathione", "Glutathione")]
    [InlineData("Glutathione", "glutathione")]
    [InlineData(" Glutathione", "Glutathione")]
    [InlineData("Glutathione", "Glutathione ")]
    public async Task Acquire_Returns_NoMatch_For_Unmapped_Catalog_Term_Before_Http(
        string compoundName,
        string term)
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent() with
        {
            CompoundName = compoundName,
            SearchTerms = [term],
        };

        var batch = await adapter.AcquireAsync(intent, RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Empty(handler.Requests);
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
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent();
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["nih-ods:not-ready"],
            },
            "other-source" => intent with { SourceId = "nih-nccih" },
            "wrong-planner" => intent with { AdapterId = "nih-ods-planning-v2" },
            "wrong-method" => intent with { CandidateMethod = "manual" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId"],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Bounded_SourceAuthored_ReviewRequired_Candidate()
    {
        var handler = new RecordingHandler(_ => XmlResponse(Fixture()));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(ReadyIntent(), RetrievedAt);
        var candidate = Assert.Single(batch.Candidates);

        Assert.True(batch.Truncated);
        Assert.Equal("market-glutathione-001", candidate.RequestId);
        Assert.Equal("Glutathione", candidate.CompoundName);
        Assert.Equal("nih-ods", candidate.SourceRegistryId);
        Assert.Equal(
            "ods-factsheet-99005156-n-acetylcysteine-and-glutathione",
            candidate.SourceItemId);
        Assert.Equal(
            "https://ods.od.nih.gov/factsheets/ImmuneFunction-HealthProfessional/",
            candidate.SourceUrl);
        Assert.Equal(
            NihOdsFactSheetAcquisitionAdapter.FixedEndpoint,
            candidate.QueryUrl);
        Assert.Equal("2026-07-01", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(
            NihOdsFactSheetAcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal(
            ["identity", "mechanism", "efficacy-claims"],
            candidate.AuthorizedFieldUses);
        Assert.Equal(
            [
                "Synthetic identity fixture text for glutathione.",
                "Synthetic allowlisted mechanism fixture paragraph.",
            ],
            candidate.Fields["identity_mechanism_source_excerpt"]);
        Assert.Equal(
            ["Synthetic efficacy fixture text reports an uncertain observed outcome."],
            candidate.Fields["efficacy_claim_source_excerpt"]);
        Assert.DoesNotContain(
            candidate.Fields.SelectMany(pair => pair.Value),
            value => value.Contains("Safety", StringComparison.OrdinalIgnoreCase)
                     || value.Contains("table", StringComparison.OrdinalIgnoreCase)
                     || value.Contains("reference", StringComparison.OrdinalIgnoreCase)
                     || value.Contains("neighboring", StringComparison.OrdinalIgnoreCase)
                     || value.Contains("linked words", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task Acquire_Emits_Ods_Provenance_Rights_And_Reuse_Boundary()
    {
        var candidate = Assert.Single(
            (await CreateAdapter(
                    new RecordingHandler(_ => XmlResponse(Fixture())))
                .AcquireAsync(ReadyIntent(), RetrievedAt))
            .Candidates);

        Assert.Equal(
            "Dietary Supplements for Immune Function and Infectious Diseases",
            Assert.Single(
                candidate.SourceSpecificProvenance["pageTitle"].Values));
        Assert.Equal(
            NihOdsFactSheetAcquisitionAdapter.TargetSection,
            Assert.Single(candidate.SourceSpecificProvenance["section"].Values));
        Assert.Equal(
            "2026-07-01",
            Assert.Single(
                candidate.SourceSpecificProvenance["pageUpdatedDate"].Values));
        var rights = Assert.Single(candidate.RightsAttributions);
        Assert.Equal("NIH Office of Dietary Supplements", rights.Provider);
        Assert.Contains("public-domain", rights.RightsStatus);
        Assert.Contains("identity_mechanism_source_excerpt", rights.CoveredFields);
        Assert.Contains("Acknowledgement", candidate.ReuseBoundary.Acknowledgement);
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        Assert.Contains(
            candidate.ReuseBoundary.ExcludedContentClasses,
            value => value.Contains("third-party", StringComparison.Ordinal));
        Assert.Equal("review-required", candidate.HumanReviewStatus);
    }

    [Fact]
    public async Task Acquire_Emits_Only_Requested_Approved_Field_Groups()
    {
        var handler = new RecordingHandler(_ => XmlResponse(Fixture()));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent() with { AuthorizedFieldUses = ["efficacy-claims"] };

        var candidate = Assert.Single(
            (await adapter.AcquireAsync(intent, RetrievedAt)).Candidates);

        Assert.DoesNotContain(
            "identity_mechanism_source_excerpt",
            candidate.Fields.Keys);
        Assert.Contains("efficacy_claim_source_excerpt", candidate.Fields.Keys);
        Assert.Equal(["efficacy-claims"], candidate.AuthorizedFieldUses);
    }

    [Fact]
    public async Task Acquire_Returns_NoMatch_When_Approved_Use_Has_No_Target_Output()
    {
        var handler = new RecordingHandler(_ => XmlResponse(Fixture()));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent() with { AuthorizedFieldUses = ["interactions"] };

        var batch = await adapter.AcquireAsync(intent, RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Surfaces_429_And_503_Without_Retry()
    {
        var rateHandler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });
        var pressureHandler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(60));
            return response;
        });

        var rate = await CreateAdapter(rateHandler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);
        var pressure = await CreateAdapter(pressureHandler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, rate.Status);
        Assert.Equal("30", rate.RetryAfter);
        Assert.Equal(SourceAcquisitionBatchStatus.BackPressure, pressure.Status);
        Assert.Equal("60", pressure.RetryAfter);
        Assert.Single(rateHandler.Requests);
        Assert.Single(pressureHandler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.Redirect, "redirect-response")]
    [InlineData(HttpStatusCode.Forbidden, "access-denied")]
    [InlineData(HttpStatusCode.Accepted, "http-202")]
    [InlineData(HttpStatusCode.PartialContent, "http-206")]
    [InlineData(HttpStatusCode.NotFound, "http-404")]
    [InlineData(HttpStatusCode.InternalServerError, "http-500")]
    public async Task Acquire_Requires_Exact_200_After_Explicit_429_And_503(
        HttpStatusCode status,
        string expectedCode)
    {
        var handler = new RecordingHandler(_ => new HttpResponseMessage(status));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(ReadyIntent(), RetrievedAt));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_Unexpected_ContentType_And_Response_Size()
    {
        var htmlHandler = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("<html />", Encoding.UTF8, "text/html"),
            });
        var sizeHandler = new RecordingHandler(_ => XmlResponse(Fixture()));

        var contentType = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(htmlHandler).AcquireAsync(ReadyIntent(), RetrievedAt));
        var size = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(sizeHandler, maximumResponseBytes: 100)
                .AcquireAsync(ReadyIntent(), RetrievedAt));

        Assert.Equal("unexpected-content-type", contentType.Code);
        Assert.Equal("response-too-large", size.Code);
    }

    [Theory]
    [InlineData("outer-dtd")]
    [InlineData("content-dtd")]
    [InlineData("wrong-root")]
    [InlineData("wrong-namespace")]
    [InlineData("reordered-top-level")]
    [InlineData("duplicate-top-level")]
    [InlineData("unknown-top-level")]
    [InlineData("fsid-leading-zero")]
    [InlineData("fsid-zero")]
    [InlineData("fsid-negative")]
    [InlineData("fsid-alpha")]
    [InlineData("wrong-language")]
    [InlineData("wrong-url-host")]
    [InlineData("wrong-url-path")]
    [InlineData("wrong-title")]
    [InlineData("missing-date")]
    [InlineData("noncanonical-date")]
    [InlineData("invalid-date")]
    [InlineData("ambiguous-section")]
    public async Task Acquire_Rejects_Unsafe_Or_OffContract_Xml(string mutation)
    {
        var body = Fixture();
        body = mutation switch
        {
            "outer-dtd" => body.Replace(
                "<Factsheet",
                "<!DOCTYPE Factsheet [<!ENTITY xxe SYSTEM \"file:///restricted\">]><Factsheet",
                StringComparison.Ordinal),
            "content-dtd" => body.Replace(
                "&lt;h2&gt;Other Ingredients&lt;/h2&gt;",
                "&lt;!DOCTYPE ods-content [ &lt;!ENTITY xxe SYSTEM \"file:///restricted\"&gt; ]&gt;&lt;h2&gt;Other Ingredients&lt;/h2&gt;",
                StringComparison.Ordinal),
            "wrong-root" => body.Replace(
                "<Factsheet xmlns=",
                "<Other xmlns=",
                StringComparison.Ordinal).Replace(
                "</Factsheet>",
                "</Other>",
                StringComparison.Ordinal),
            "wrong-namespace" => body.Replace(
                "http://tempuri.org/factsheet.xsd",
                "urn:synthetic:wrong-factsheet",
                StringComparison.Ordinal),
            "reordered-top-level" => MoveFsidAfterLanguage(body),
            "duplicate-top-level" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>99005156</FSID><FSID>99005156</FSID>",
                StringComparison.Ordinal),
            "unknown-top-level" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>99005156</FSID><SyntheticUnknown>value</SyntheticUnknown>",
                StringComparison.Ordinal),
            "fsid-leading-zero" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>099005156</FSID>",
                StringComparison.Ordinal),
            "fsid-zero" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>0</FSID>",
                StringComparison.Ordinal),
            "fsid-negative" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>-99005156</FSID>",
                StringComparison.Ordinal),
            "fsid-alpha" => body.Replace(
                "<FSID>99005156</FSID>",
                "<FSID>synthetic-id</FSID>",
                StringComparison.Ordinal),
            "wrong-language" => body.Replace(
                "<LanguageCode>en</LanguageCode>",
                "<LanguageCode>es</LanguageCode>",
                StringComparison.Ordinal),
            "wrong-url-host" => body.Replace(
                "https://ods.od.nih.gov:443/factsheets/ImmuneFunction-HealthProfessional/",
                "https://example.invalid/factsheets/ImmuneFunction-HealthProfessional/",
                StringComparison.Ordinal),
            "wrong-url-path" => body.Replace(
                "/factsheets/ImmuneFunction-HealthProfessional/",
                "/factsheets/SyntheticOther-HealthProfessional/",
                StringComparison.Ordinal),
            "wrong-title" => body.Replace(
                "Dietary Supplements for Immune Function and Infectious Diseases",
                "Synthetic unrelated title",
                StringComparison.Ordinal),
            "missing-date" => body.Replace(
                "<Reviewed>2026-07-01</Reviewed>",
                "<Reviewed />",
                StringComparison.Ordinal),
            "noncanonical-date" => body.Replace(
                "<Reviewed>2026-07-01</Reviewed>",
                "<Reviewed>2026-7-1</Reviewed>",
                StringComparison.Ordinal),
            "invalid-date" => body.Replace(
                "<Reviewed>2026-07-01</Reviewed>",
                "<Reviewed>2026-02-30</Reviewed>",
                StringComparison.Ordinal),
            "ambiguous-section" => body.Replace(
                "&lt;h3&gt;Next synthetic ingredient&lt;/h3&gt;",
                "&lt;h3&gt;N-acetylcysteine and Glutathione&lt;/h3&gt;",
                StringComparison.Ordinal),
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        var handler = new RecordingHandler(_ => XmlResponse(body));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(ReadyIntent(), RetrievedAt));
    }

    [Theory]
    [InlineData("unknown-inline")]
    [InlineData("namespaced-inline")]
    [InlineData("attributed-inline")]
    [InlineData("uppercase-inline")]
    public async Task Acquire_Drops_Entire_Paragraph_With_Nonallowlisted_Inline_Markup(
        string mutation)
    {
        const string allowed =
            "&lt;p&gt;Synthetic allowlisted &lt;em&gt;mechanism&lt;/em&gt; fixture paragraph.&lt;/p&gt;";
        var replacement = mutation switch
        {
            "unknown-inline" =>
                "&lt;p&gt;Synthetic before &lt;mark&gt;unknown inline&lt;/mark&gt; synthetic after.&lt;/p&gt;",
            "namespaced-inline" =>
                "&lt;p&gt;Synthetic before &lt;x:em xmlns:x=\"urn:synthetic:inline\"&gt;namespaced inline&lt;/x:em&gt; synthetic after.&lt;/p&gt;",
            "attributed-inline" =>
                "&lt;p&gt;Synthetic before &lt;em class=\"synthetic\"&gt;attributed inline&lt;/em&gt; synthetic after.&lt;/p&gt;",
            "uppercase-inline" =>
                "&lt;p&gt;Synthetic before &lt;EM&gt;uppercase inline&lt;/EM&gt; synthetic after.&lt;/p&gt;",
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };
        var handler = new RecordingHandler(
            _ => XmlResponse(
                Fixture().Replace(allowed, replacement, StringComparison.Ordinal)));

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        var candidate = Assert.Single(batch.Candidates);
        Assert.True(batch.Truncated);
        Assert.Equal(
            ["Synthetic identity fixture text for glutathione."],
            candidate.Fields["identity_mechanism_source_excerpt"]);
        Assert.DoesNotContain(
            candidate.Fields.SelectMany(field => field.Value),
            value => value.Contains("synthetic before", StringComparison.OrdinalIgnoreCase)
                     || value.Contains("synthetic after", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Constructor_Rejects_Response_Ceiling_Above_One_MiB()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateAdapter(
                new RecordingHandler(_ => XmlResponse(Fixture())),
                NihOdsFactSheetAcquisitionAdapter.DefaultMaximumResponseBytes + 1));
    }

    [Fact]
    public async Task Acquire_Cancellation_While_Waiting_For_Gate_Does_Not_Call_Http()
    {
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            Array.Empty<SourceSlidingWindowBudget>(),
            dailyBudget: null);
        using var lease = await gate.AcquireAsync(CancellationToken.None);
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler, requestGate: gate);
        using var cancellation = new CancellationTokenSource();
        var acquire = adapter.AcquireAsync(
            ReadyIntent(),
            RetrievedAt,
            cancellation.Token);

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquire);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Propagates_Cancellation_From_Http()
    {
        var handler = new CancellationHandler();
        var adapter = CreateAdapter(handler);
        using var cancellation = new CancellationTokenSource();
        var acquire = adapter.AcquireAsync(
            ReadyIntent(),
            RetrievedAt,
            cancellation.Token);
        await handler.RequestEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        cancellation.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => acquire);
        Assert.Equal(1, handler.RequestCount);
    }

    [Fact]
    public async Task RequestGate_Serializes_Concurrent_Acquisitions()
    {
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            Array.Empty<SourceSlidingWindowBudget>(),
            dailyBudget: null);
        var firstLease = await gate.AcquireAsync(CancellationToken.None);

        var second = gate.AcquireAsync(CancellationToken.None).AsTask();

        Assert.False(second.IsCompleted);
        firstLease.Dispose();
        using var secondLease = await second;
    }

    private static NihOdsFactSheetAcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int maximumResponseBytes =
            NihOdsFactSheetAcquisitionAdapter.DefaultMaximumResponseBytes,
        ISourceRequestGate? requestGate = null)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            maximumResponseBytes,
            requestGate ?? new SerializedSourceRequestGate(
                TimeProvider.System,
                Array.Empty<SourceSlidingWindowBudget>(),
                dailyBudget: null));

    private static SourceAcquisitionIntent ReadyIntent()
        => new(
            SourceId: "nih-ods",
            AdapterId: NihOdsFactSheetAcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-glutathione-001",
            CompoundName: "Glutathione",
            SearchTerms: ["Glutathione"],
            CandidateMethod: "api",
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
            BlockingReasons: Array.Empty<string>());

    private static string Fixture()
        => File.ReadAllText(
            TestPaths.FixturePath("nih-ods-immune-function.synthetic.xml"));

    private static string MoveFsidAfterLanguage(string body)
    {
        const string fsid = "<FSID>99005156</FSID>";
        const string language = "<LanguageCode>en</LanguageCode>";
        return body
            .Replace(fsid, string.Empty, StringComparison.Ordinal)
            .Replace(language, language + fsid, StringComparison.Ordinal);
    }

    private static HttpResponseMessage XmlResponse(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/xml"),
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

    private sealed class CancellationHandler : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public TaskCompletionSource RequestEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref _requestCount);
            RequestEntered.SetResult();
            await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            throw new InvalidOperationException("Cancellation was not observed.");
        }
    }
}
