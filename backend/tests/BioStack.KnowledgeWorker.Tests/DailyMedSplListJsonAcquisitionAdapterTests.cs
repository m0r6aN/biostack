namespace BioStack.KnowledgeWorker.Tests;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public sealed class DailyMedSplListJsonAcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private const string ExpectedRequestUrl =
        "https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=Semaglutide&name_type=both&pagesize=50&page=1";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse(
            "2026-07-25T16:00:00Z",
            CultureInfo.InvariantCulture);

    [Fact]
    public async Task Acquire_Uses_Exact_Fixed_First_Page_Query()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));
        var adapter = CreateAdapter(handler);

        await adapter.AcquireAsync(ReadyIntent(), RetrievedAt);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal(ExpectedRequestUrl, request.RequestUri!.AbsoluteUri);
        var accept = Assert.Single(request.Headers.Accept);
        Assert.Equal("application/json", accept.MediaType);
        Assert.Null(request.Headers.Authorization);
        Assert.Empty(request.Headers.UserAgent);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("other-source")]
    [InlineData("wrong-planner")]
    [InlineData("wrong-method")]
    [InlineData("stale-registry")]
    [InlineData("missing-provenance")]
    [InlineData("identity-not-authorized")]
    public async Task Acquire_Rejects_Unauthorized_Intent_Before_Http(
        string mutation)
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var intent = ReadyIntent();
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["dailymed:legal-rights-not-approved"],
            },
            "other-source" => intent with { SourceId = "pubmed" },
            "wrong-planner" => intent with { AdapterId = "dailymed-planning-v2" },
            "wrong-method" => intent with { CandidateMethod = "bulk-download" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId"],
            },
            "identity-not-authorized" => intent with
            {
                AuthorizedFieldUses = ["regulatory"],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(" Semaglutide")]
    [InlineData("Semaglutide ")]
    [InlineData("Semaglutide\nInjected")]
    [InlineData("Semaglutide&page=2")]
    [InlineData("Semaglutide?name_type=exact")]
    [InlineData("Semaglutide#fragment")]
    [InlineData("Semaglutide=other")]
    [InlineData("Semaglutide\"")]
    [InlineData("Semaglutide\\")]
    [InlineData("Semaglutide[0]")]
    public async Task Acquire_Rejects_Noncanonical_Or_Injected_Compound_Before_Http(
        string compoundName)
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var intent = ReadyIntent() with { CompoundName = compoundName };

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(intent, RetrievedAt));

        Assert.Equal("compound-name-invalid", exception.Code);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_Overlong_Compound_Before_Http()
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var intent = ReadyIntent() with { CompoundName = new string('a', 129) };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Only_Identity_Fields_And_Review_Required()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.False(batch.Truncated);
        Assert.Null(batch.RetryAfter);
        Assert.Equal(2, batch.Candidates.Count);
        var candidate = batch.Candidates[0];
        Assert.Equal("market-semaglutide-001", candidate.RequestId);
        Assert.Equal("Semaglutide", candidate.CompoundName);
        Assert.Equal("dailymed", candidate.SourceRegistryId);
        Assert.Equal(
            "11111111-1111-4111-8111-111111111111",
            candidate.SourceItemId);
        Assert.Equal(
            "https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid=11111111-1111-4111-8111-111111111111",
            candidate.SourceUrl);
        Assert.Equal(ExpectedRequestUrl, candidate.QueryUrl);
        Assert.Equal("2026-07-20", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(RetrievedAt, candidate.RetrievedAtUtc);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal(RegistrySha256, candidate.RegistryBindingSha256);
        Assert.Equal(
            DailyMedSplListJsonAcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(["identity"], candidate.AuthorizedFieldUses);
        Assert.Equal(
            ["label_title", "spl_set_id", "label_version", "published_date"],
            candidate.Fields.Keys);
        Assert.Equal(
            ["SEMAGLUTIDE INJECTION [SYNTHETIC LABELER]"],
            candidate.Fields["label_title"]);
        Assert.Equal(
            ["11111111-1111-4111-8111-111111111111"],
            candidate.Fields["spl_set_id"]);
        Assert.Equal(["4"], candidate.Fields["label_version"]);
        Assert.Equal(["2026-07-20"], candidate.Fields["published_date"]);
        Assert.DoesNotContain(
            candidate.Fields.Keys,
            key => key.Contains("section", StringComparison.OrdinalIgnoreCase)
                || key.Contains("ndc", StringComparison.OrdinalIgnoreCase)
                || key.Contains("xml", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(3, candidate.EvidenceLimitations.Count);
    }

    [Fact]
    public async Task Acquire_Intersects_Authorized_Uses_To_Identity()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));
        var intent = ReadyIntent() with
        {
            AuthorizedFieldUses =
            [
                "identity",
                "regulatory",
                "approved-indications",
                "monitoring",
            ],
        };

        var candidates = (await CreateAdapter(handler)
            .AcquireAsync(intent, RetrievedAt)).Candidates;

        Assert.All(
            candidates,
            candidate => Assert.Equal(
                ["identity"],
                candidate.AuthorizedFieldUses));
    }

    [Fact]
    public async Task Acquire_Emits_Present_And_Typed_Absent_Provenance()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));

        var candidate = Assert.Single(
            (await CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt)).Candidates,
            value => value.SourceItemId.StartsWith(
                "11111111",
                StringComparison.Ordinal));

        Assert.Equal(
            ["splSetId", "labelVersion", "ndc", "effectiveDate", "sectionName", "sectionCode"],
            candidate.SourceSpecificProvenance.Keys);
        AssertProvenance(
            candidate,
            "splSetId",
            "present",
            "11111111-1111-4111-8111-111111111111");
        AssertProvenance(candidate, "labelVersion", "present", "4");
        AssertTypedAbsence(candidate, "ndc", "not-provided");
        AssertTypedAbsence(candidate, "effectiveDate", "not-provided");
        AssertTypedAbsence(candidate, "sectionName", "not-applicable");
        AssertTypedAbsence(candidate, "sectionCode", "not-applicable");
    }

    [Fact]
    public async Task Acquire_Emits_Narrow_Rights_And_Reuse_Boundary()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));

        var candidate = (await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt)).Candidates[0];

        var attribution = Assert.Single(candidate.RightsAttributions);
        Assert.Equal(
            "DailyMed SPL list-record identity metadata only.",
            attribution.Scope);
        Assert.Equal(
            "DailyMed, National Library of Medicine",
            attribution.Provider);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            "https://www.nlm.nih.gov/web_policies.html",
            attribution.TermsUrl);
        Assert.Equal("reviewed", attribution.RightsStatus);
        Assert.Equal(
            ["label_title", "spl_set_id", "label_version", "published_date", "splSetId", "labelVersion"],
            attribution.CoveredFields);
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        Assert.Contains(
            "SPL section text",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Contains(
            "SPL XML",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Contains(
            "media and linked documents",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Contains(
            "third-party material",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Contains(
            "product-specific claims",
            candidate.ReuseBoundary.ExcludedContentClasses);
    }

    [Fact]
    public async Task Acquire_Returns_NoMatch_Only_For_Coherent_Zero_Result_Page()
    {
        var root = FixtureNode();
        root["metadata"]!["total_elements"] = 0;
        root["metadata"]!["total_pages"] = 0;
        root["data"] = new JsonArray();
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.False(batch.Truncated);
    }

    [Theory]
    [InlineData("elements_per_page", "49")]
    [InlineData("elements_per_page", "50.0")]
    [InlineData("elements_per_page", "5e1")]
    [InlineData("elements_per_page", "\"50\"")]
    [InlineData("elements_per_page", "050")]
    [InlineData("elements_per_page", "+50")]
    [InlineData(
        "elements_per_page",
        "18446744073709551616")]
    [InlineData("current_page", "2")]
    [InlineData("current_page", "1.0")]
    [InlineData("current_page", "\"1\"")]
    [InlineData("total_elements", "-1")]
    [InlineData("total_elements", "2.0")]
    [InlineData("total_elements", "\"2\"")]
    [InlineData(
        "total_elements",
        "18446744073709551616")]
    [InlineData("total_pages", "2")]
    [InlineData("total_pages", "1e0")]
    [InlineData("total_pages", "\"1\"")]
    [InlineData("next_page", "2")]
    [InlineData(
        "next_page_url",
        "\"https://example.invalid/page=2\"")]
    [InlineData("previous_page", "0")]
    [InlineData(
        "previous_page_url",
        "\"https://example.invalid/page=0\"")]
    public async Task Acquire_Rejects_Incoherent_Page_Metadata(
        string field,
        string rawJson)
    {
        var root = FixtureNode();
        string body;
        try
        {
            root["metadata"]![field] = JsonNode.Parse(rawJson);
            body = root.ToJsonString();
        }
        catch (System.Text.Json.JsonException)
        {
            body = Fixture().Replace(
                $"\"{field}\": 50",
                $"\"{field}\": {rawJson}",
                StringComparison.Ordinal);
        }
        var handler = new RecordingHandler(_ => JsonResponse(body));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));
    }

    [Theory]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=Semaglutide&name_type=both&pagesize=50&page=2")]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?name_type=both&drug_name=Semaglutide&pagesize=50&page=1")]
    [InlineData("https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=semaglutide&name_type=both&pagesize=50&page=1")]
    public async Task Acquire_Rejects_Current_Url_That_Is_Not_Exact_Request(
        string currentUrl)
    {
        var root = FixtureNode();
        root["metadata"]!["current_url"] = currentUrl;
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal("current-url-mismatch", exception.Code);
    }

    [Fact]
    public async Task Acquire_Rejects_Data_Count_Not_Equal_To_Bounded_Total()
    {
        var root = FixtureNode();
        root["metadata"]!["total_elements"] = 3;
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal("result-count-correlation-invalid", exception.Code);
    }

    [Fact]
    public async Task Acquire_Accepts_Exactly_Fifty_Correlated_Records_And_Marks_Truncated()
    {
        var root = FixtureNode();
        var data = new JsonArray();
        for (var index = 1; index <= 50; index++)
        {
            data.Add(new JsonObject
            {
                ["spl_version"] = index,
                ["published_date"] = "Jul 20, 2026",
                ["setid"] = $"00000000-0000-4000-8000-{index:000000000000}",
                ["title"] = $"SEMAGLUTIDE SYNTHETIC LABEL {index}",
            });
        }
        root["data"] = data;
        root["metadata"]!["total_elements"] = 51;
        root["metadata"]!["total_pages"] = 2;
        root["metadata"]!["next_page"] = 2;
        root["metadata"]!["next_page_url"] =
            "https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=Semaglutide&name_type=both&pagesize=50&page=2";
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Equal(50, batch.Candidates.Count);
        Assert.True(batch.Truncated);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_String_Representation_For_Populated_Next_Page()
    {
        var root = FixtureNode();
        var data = new JsonArray();
        for (var index = 1; index <= 50; index++)
        {
            data.Add(new JsonObject
            {
                ["spl_version"] = index,
                ["published_date"] = "Jul 20, 2026",
                ["setid"] = $"00000000-0000-4000-8000-{index:000000000000}",
                ["title"] = $"SEMAGLUTIDE SYNTHETIC LABEL {index}",
            });
        }
        root["data"] = data;
        root["metadata"]!["total_elements"] = 51;
        root["metadata"]!["total_pages"] = 2;
        root["metadata"]!["next_page"] = "2";
        root["metadata"]!["next_page_url"] =
            "https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=Semaglutide&name_type=both&pagesize=50&page=2";
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal("field-invalid", exception.Code);
    }

    [Fact]
    public async Task Acquire_Rejects_More_Than_Fifty_Records()
    {
        var root = FixtureNode();
        var data = root["data"]!.AsArray();
        for (var index = 3; index <= 51; index++)
        {
            data.Add(new JsonObject
            {
                ["spl_version"] = 1,
                ["published_date"] = "Jul 20, 2026",
                ["setid"] = $"00000000-0000-4000-8000-{index:000000000000}",
                ["title"] = $"SEMAGLUTIDE SYNTHETIC LABEL {index}",
            });
        }
        root["metadata"]!["total_elements"] = 51;
        root["metadata"]!["total_pages"] = 2;
        root["metadata"]!["next_page"] = 2;
        root["metadata"]!["next_page_url"] =
            "https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json?drug_name=Semaglutide&name_type=both&pagesize=50&page=2";
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal("result-count-exceeded", exception.Code);
    }

    [Theory]
    [InlineData("setid", "11111111-1111-4111-8111-11111111111A")]
    [InlineData("setid", "not-a-guid")]
    [InlineData("published_date", "2026-07-20")]
    [InlineData("published_date", "Juil 20, 2026")]
    [InlineData("title", "TIRZEPATIDE INJECTION")]
    [InlineData("title", "SEMAGLUTIDE\nINJECTION")]
    public async Task Acquire_Rejects_Invalid_Or_Uncorrelated_Record(
        string field,
        string value)
    {
        var root = FixtureNode();
        root["data"]![0]![field] = value;
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));
    }

    [Theory]
    [InlineData("0")]
    [InlineData("-1")]
    [InlineData("+4")]
    [InlineData("04")]
    [InlineData("4.0")]
    [InlineData("4e0")]
    [InlineData("\"4\"")]
    [InlineData("18446744073709551616")]
    public async Task Acquire_Rejects_Noncanonical_Numeric_Spl_Version(
        string rawJson)
    {
        var body = Fixture().Replace(
            "\"spl_version\": 4",
            $"\"spl_version\": {rawJson}",
            StringComparison.Ordinal);
        var handler = new RecordingHandler(_ => JsonResponse(body));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_Rejects_Overlong_Title()
    {
        var root = FixtureNode();
        root["data"]![0]!["title"] =
            "SEMAGLUTIDE " + new string('a', 1013);
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_Rejects_Duplicate_Set_Id()
    {
        var root = FixtureNode();
        root["data"]![1]!["setid"] =
            "11111111-1111-4111-8111-111111111111";
        var handler = new RecordingHandler(
            _ => JsonResponse(root.ToJsonString()));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal("duplicate-spl-set-id", exception.Code);
    }

    [Fact]
    public async Task Acquire_Parses_Published_Date_With_Invariant_Culture()
    {
        var originalCulture = CultureInfo.CurrentCulture;
        var originalUiCulture = CultureInfo.CurrentUICulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("fr-FR");
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo("fr-FR");
            var handler = new RecordingHandler(_ => JsonResponse(Fixture()));

            var candidates = (await CreateAdapter(handler)
                .AcquireAsync(ReadyIntent(), RetrievedAt)).Candidates;

            Assert.Equal("2026-07-20", candidates[0].SourcePublicationOrUpdateDate);
            Assert.Equal("2026-06-15", candidates[1].SourcePublicationOrUpdateDate);
        }
        finally
        {
            CultureInfo.CurrentCulture = originalCulture;
            CultureInfo.CurrentUICulture = originalUiCulture;
        }
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, "http-202")]
    [InlineData(HttpStatusCode.MovedPermanently, "redirect-response")]
    [InlineData(HttpStatusCode.BadRequest, "http-400")]
    [InlineData(HttpStatusCode.NotFound, "http-404")]
    [InlineData(HttpStatusCode.InternalServerError, "http-500")]
    public async Task Acquire_Fails_Closed_For_Unexpected_Http_Status(
        HttpStatusCode status,
        string code)
    {
        var handler = new RecordingHandler(
            _ => new HttpResponseMessage(status));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));

        Assert.Equal(code, exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Surfaces_429_And_RetryAfter_Without_Retry()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(
                HttpStatusCode.TooManyRequests);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, batch.Status);
        Assert.Equal("30", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Surfaces_503_And_RetryAfter_Without_Retry()
    {
        var retryAt = RetrievedAt.AddMinutes(5);
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(retryAt);
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.BackPressure, batch.Status);
        Assert.Equal(
            retryAt.ToString("r", CultureInfo.InvariantCulture),
            batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_NonJson_Malformed_And_Oversized_Bodies()
    {
        var nonJson = new RecordingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(
                    "{}",
                    Encoding.UTF8,
                    "text/plain"),
            });
        var malformed = new RecordingHandler(_ => JsonResponse("{"));
        var oversized = new RecordingHandler(_ => JsonResponse(Fixture()));

        var nonJsonException =
            await Assert.ThrowsAsync<SourceAcquisitionException>(
                () => CreateAdapter(nonJson).AcquireAsync(
                    ReadyIntent(),
                    RetrievedAt));
        var malformedException =
            await Assert.ThrowsAsync<SourceAcquisitionException>(
                () => CreateAdapter(malformed).AcquireAsync(
                    ReadyIntent(),
                    RetrievedAt));
        var oversizedException =
            await Assert.ThrowsAsync<SourceAcquisitionException>(
                () => CreateAdapter(oversized, maximumResponseBytes: 32)
                    .AcquireAsync(ReadyIntent(), RetrievedAt));

        Assert.Equal("unexpected-content-type", nonJsonException.Code);
        Assert.Equal("malformed-json", malformedException.Code);
        Assert.Equal("response-too-large", oversizedException.Code);
    }

    [Fact]
    public void Constructor_Rejects_Response_Cap_Above_Fixed_One_Mebibyte()
    {
        var handler = new RecordingHandler(_ => JsonResponse(Fixture()));

        Assert.Throws<ArgumentOutOfRangeException>(
            () => CreateAdapter(
                handler,
                DailyMedSplListJsonAcquisitionAdapter
                    .DefaultMaximumResponseBytes + 1));
    }

    [Theory]
    [InlineData("{}")]
    [InlineData("""{"metadata":{},"data":{}}""")]
    [InlineData("""{"metadata":[],"data":[]}""")]
    [InlineData("""{"metadata":{},"data":[]}""")]
    public async Task Acquire_Rejects_Malformed_Response_Shape(string body)
    {
        var handler = new RecordingHandler(_ => JsonResponse(body));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_Cancellation_While_Waiting_For_Serial_Gate_Does_Not_Call_Http()
    {
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            [],
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

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => acquire);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Serializes_Concurrent_Requests()
    {
        var handler = new CoordinatedHandler();
        var gate = new SerializedSourceRequestGate(
            TimeProvider.System,
            [],
            dailyBudget: null);
        var adapter = CreateAdapter(handler, requestGate: gate);

        var first = adapter.AcquireAsync(ReadyIntent(), RetrievedAt);
        await handler.FirstRequestEntered.Task.WaitAsync(
            TimeSpan.FromSeconds(5));
        var second = adapter.AcquireAsync(ReadyIntent(), RetrievedAt);
        await Task.Delay(50);

        Assert.Equal(1, handler.RequestCount);
        handler.ReleaseFirstRequest.SetResult();
        await first;
        await second;
        Assert.Equal(2, handler.RequestCount);
    }

    private static DailyMedSplListJsonAcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int maximumResponseBytes =
            DailyMedSplListJsonAcquisitionAdapter.DefaultMaximumResponseBytes,
        ISourceRequestGate? requestGate = null)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            maximumResponseBytes,
            requestGate ?? new SerializedSourceRequestGate(
                TimeProvider.System,
                [],
                dailyBudget: null));

    private static SourceAcquisitionIntent ReadyIntent()
        => new(
            SourceId: "dailymed",
            AdapterId:
                DailyMedSplListJsonAcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-semaglutide-001",
            CompoundName: "Semaglutide",
            SearchTerms: ["Semaglutide"],
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
                "splSetId",
                "ndc",
                "labelVersion",
                "effectiveDate",
                "sectionName",
                "sectionCode",
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
            TestPaths.FixturePath(
                "dailymed-spls-list.synthetic.json"));

    private static JsonObject FixtureNode()
        => JsonNode.Parse(Fixture())!.AsObject();

    private static HttpResponseMessage JsonResponse(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                body,
                Encoding.UTF8,
                "application/json"),
        };

    private static void AssertProvenance(
        SourceAcquisitionCandidate candidate,
        string key,
        string availability,
        string expectedValue)
    {
        var value = candidate.SourceSpecificProvenance[key];
        Assert.Equal(availability, value.Availability);
        Assert.Equal([expectedValue], value.Values);
        Assert.Equal(string.Empty, value.UnavailableReason);
    }

    private static void AssertTypedAbsence(
        SourceAcquisitionCandidate candidate,
        string key,
        string availability)
    {
        var value = candidate.SourceSpecificProvenance[key];
        Assert.Equal(availability, value.Availability);
        Assert.Empty(value.Values);
        Assert.False(string.IsNullOrWhiteSpace(value.UnavailableReason));
    }

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

    private sealed class CoordinatedHandler : HttpMessageHandler
    {
        private int _requestCount;

        public int RequestCount => Volatile.Read(ref _requestCount);

        public TaskCompletionSource FirstRequestEntered { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TaskCompletionSource ReleaseFirstRequest { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var requestNumber = Interlocked.Increment(ref _requestCount);
            if (requestNumber == 1)
            {
                FirstRequestEntered.SetResult();
                await ReleaseFirstRequest.Task.WaitAsync(cancellationToken);
            }
            return JsonResponse(Fixture());
        }
    }
}
