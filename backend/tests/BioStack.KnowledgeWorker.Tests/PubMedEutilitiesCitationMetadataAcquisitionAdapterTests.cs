namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class PubMedEutilitiesCitationMetadataAcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private const string Tool = "biostack-research";
    private const string Email = "evidence@example.com";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T20:00:00Z");

    [Theory]
    [InlineData("", Email)]
    [InlineData("bad tool", Email)]
    [InlineData("x", Email)]
    [InlineData(Tool, "")]
    [InlineData(Tool, "not-an-email")]
    [InlineData(Tool, "Display Name <evidence@example.com>")]
    public void Constructor_Requires_Validated_Ncbi_Client_Identity(
        string tool,
        string email)
    {
        Assert.ThrowsAny<ArgumentException>(
            () => new PubMedEutilitiesCitationMetadataAcquisitionAdapter(
                RegistrySha256,
                tool,
                email));
    }

    [Fact]
    public async Task Acquire_Uses_Fixed_Bounded_ESearch_And_ESummary_Requests()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(
            ReadyIntent(["Semaglutide", "Ozempic"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Equal(2, handler.Requests.Count);
        var search = handler.Requests[0].RequestUri!;
        Assert.Equal("https", search.Scheme);
        Assert.Equal("eutils.ncbi.nlm.nih.gov", search.Host);
        Assert.Equal("/entrez/eutils/esearch.fcgi", search.AbsolutePath);
        var searchQuery = Uri.UnescapeDataString(search.Query);
        Assert.Contains("db=pubmed", searchQuery);
        Assert.Contains(
            "term=\"Semaglutide\"[Title/Abstract] OR \"Ozempic\"[Title/Abstract]",
            searchQuery);
        Assert.Contains("retmode=json", searchQuery);
        Assert.Contains("retstart=0", searchQuery);
        Assert.Contains("retmax=50", searchQuery);
        Assert.Contains("sort=pub_date", searchQuery);
        Assert.Contains("usehistory=n", searchQuery);
        Assert.Contains($"tool={Tool}", searchQuery);
        Assert.Contains($"email={Email}", searchQuery);
        Assert.DoesNotContain("api_key", searchQuery, StringComparison.OrdinalIgnoreCase);

        var summary = handler.Requests[1].RequestUri!;
        Assert.Equal("/entrez/eutils/esummary.fcgi", summary.AbsolutePath);
        var summaryQuery = Uri.UnescapeDataString(summary.Query);
        Assert.Contains("db=pubmed", summaryQuery);
        Assert.Contains("id=111,222", summaryQuery);
        Assert.Contains("retmode=json", summaryQuery);
        Assert.DoesNotContain("version=", summaryQuery);
        Assert.Contains($"tool={Tool}", summaryQuery);
        Assert.Contains($"email={Email}", summaryQuery);
        Assert.DoesNotContain("api_key", summaryQuery, StringComparison.OrdinalIgnoreCase);
        Assert.All(
            handler.Requests,
            request => Assert.Equal(
                "application/json",
                Assert.Single(request.Headers.Accept).MediaType));
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("other-source")]
    [InlineData("wrong-planner")]
    [InlineData("wrong-method")]
    [InlineData("stale-registry")]
    [InlineData("missing-provenance")]
    [InlineData("identity-only")]
    public async Task Acquire_Rejects_Unauthorized_Intent_Before_Http(
        string mutation)
    {
        var handler = NoNetworkHandler();
        var intent = ReadyIntent(["Semaglutide"]);
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["pubmed:legal-rights-not-approved"],
            },
            "other-source" => intent with { SourceId = "pubchem" },
            "wrong-planner" => intent with { AdapterId = "pubmed-planning-v2" },
            "wrong-method" => intent with { CandidateMethod = "bulk-download" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId", "pmid"],
            },
            "identity-only" => intent with
            {
                AuthorizedFieldUses = ["identity"],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Citation_Metadata_Only_With_Redacted_QueryUrl()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(
            ReadyIntent(["Semaglutide"]),
            RetrievedAt);

        Assert.False(batch.Truncated);
        Assert.Equal(2, batch.Candidates.Count);
        var candidate = batch.Candidates[0];
        Assert.Equal("pubmed", candidate.SourceRegistryId);
        Assert.Equal("111", candidate.SourceItemId);
        Assert.Equal("https://pubmed.ncbi.nlm.nih.gov/111/", candidate.SourceUrl);
        Assert.NotNull(candidate.QueryUrl);
        Assert.DoesNotContain("tool=", candidate.QueryUrl);
        Assert.DoesNotContain("email=", candidate.QueryUrl);
        Assert.DoesNotContain(Tool, candidate.QueryUrl);
        Assert.DoesNotContain(Email, candidate.QueryUrl);
        Assert.Equal("2025 Jul 01", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(
            PubMedEutilitiesCitationMetadataAcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal(
            ["mechanism", "efficacy-claims", "interactions"],
            candidate.AuthorizedFieldUses);
        Assert.Equal(
            [
                "article_title",
                "e_location",
                "issue",
                "journal_source",
                "languages",
                "pages",
                "publication_date",
                "publication_types",
                "volume",
            ],
            candidate.Fields.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain("abstract", candidate.Fields.Keys);
        Assert.DoesNotContain("authors", candidate.Fields.Keys);
        Assert.Equal(
            ["111"],
            candidate.SourceSpecificProvenance["pmid"].Values);
        Assert.Equal(
            ["10.0000/synthetic.111"],
            candidate.SourceSpecificProvenance["doi"].Values);
        Assert.Equal(
            ["PMC111"],
            candidate.SourceSpecificProvenance["pmcid"].Values);
        Assert.Equal(
            ["\"Semaglutide\"[Title/Abstract]"],
            candidate.SourceSpecificProvenance["query"].Values);
        var attribution = Assert.Single(candidate.RightsAttributions);
        Assert.Equal("PubMed citation metadata only.", attribution.Scope);
        Assert.Equal(
            "PubMed, National Library of Medicine",
            attribution.Provider);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            "https://www.nlm.nih.gov/databases/download.html",
            attribution.TermsUrl);
        Assert.Equal("reviewed", attribution.RightsStatus);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            new[]
            {
                "article_title",
                "doi",
                "e_location",
                "issue",
                "journal_source",
                "languages",
                "pages",
                "pmcid",
                "pmid",
                "publication_date",
                "publication_types",
                "publicationDate",
                "query",
                "volume",
            }.Order(StringComparer.OrdinalIgnoreCase),
            attribution.CoveredFields.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            "PubMed and the National Library of Medicine are the source of the citation metadata.",
            candidate.ReuseBoundary.Acknowledgement);
        Assert.Equal(
            new[]
            {
                "abstracts",
                "EFetch content",
                "excerpts",
                "LinkOut content",
                "PMC full text",
                "publisher full text",
            },
            candidate.ReuseBoundary.ExcludedContentClasses.Order(
                StringComparer.OrdinalIgnoreCase));
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        var serializedCandidate = JsonSerializer.Serialize(candidate);
        Assert.DoesNotContain(Tool, serializedCandidate);
        Assert.DoesNotContain(Email, serializedCandidate);
        Assert.DoesNotContain(
            "api_key",
            serializedCandidate,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Acquire_Encodes_Allowed_Reserved_Query_Characters_Without_Parameter_Injection()
    {
        var handler = await SuccessfulHandler();
        const string term = "A&B?C#D%=E OR F";

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent([term]),
            RetrievedAt);

        var actual = handler.Requests[0].RequestUri!;
        Assert.Equal("https", actual.Scheme);
        Assert.Equal("eutils.ncbi.nlm.nih.gov", actual.Host);
        Assert.Equal(string.Empty, actual.Fragment);
        Assert.Contains(
            "A%26B%3FC%23D%25%3DE%20OR%20F",
            actual.Query);
        Assert.Equal(1, Count(actual.Query, "tool="));
        Assert.Equal(1, Count(actual.Query, "email="));
        Assert.DoesNotContain(
            "api_key=",
            actual.Query,
            StringComparison.OrdinalIgnoreCase);
        var candidateQuery = batch.Candidates[0].QueryUrl!;
        Assert.Contains(
            "A%26B%3FC%23D%25%3DE%20OR%20F",
            candidateQuery);
        Assert.DoesNotContain("tool=", candidateQuery);
        Assert.DoesNotContain("email=", candidateQuery);
        Assert.DoesNotContain(
            "api_key=",
            candidateQuery,
            StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Acquire_Represents_Missing_Doi_And_Pmcid_As_Typed_Absence()
    {
        var handler = await SuccessfulHandler();

        var candidate = (await CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt))
            .Candidates[1];

        Assert.Equal("222", candidate.SourceItemId);
        Assert.Equal("2024 Dec", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(
            ["2024 Dec"],
            candidate.Fields["publication_date"]);
        Assert.Equal(
            ["2024 Dec"],
            candidate.SourceSpecificProvenance["publicationDate"].Values);
        Assert.DoesNotContain("sortpubdate", candidate.Fields.Keys);
        var doi = candidate.SourceSpecificProvenance["doi"];
        Assert.Equal("not-provided", doi.Availability);
        Assert.Empty(doi.Values);
        Assert.Contains("did not provide a DOI", doi.UnavailableReason);
        var pmcid = candidate.SourceSpecificProvenance["pmcid"];
        Assert.Equal("not-provided", pmcid.Availability);
        Assert.Empty(pmcid.Values);
        Assert.Contains("did not provide a PMCID", pmcid.UnavailableReason);
    }

    [Fact]
    public async Task Acquire_Uses_Exact_Nonempty_Authorized_Field_Intersection()
    {
        var handler = await SuccessfulHandler();
        var intent = ReadyIntent(["Semaglutide"]) with
        {
            AuthorizedFieldUses =
            [
                "identity",
                "interactions",
                "mechanism",
                "regulatory",
            ],
        };

        var candidates = (await CreateAdapter(handler).AcquireAsync(
            intent,
            RetrievedAt)).Candidates;

        Assert.All(
            candidates,
            candidate => Assert.Equal(
                ["mechanism", "interactions"],
                candidate.AuthorizedFieldUses));
    }

    [Fact]
    public async Task Acquire_Returns_NoMatch_For_Zero_Count_And_Empty_IdList()
    {
        var handler = new RecordingHandler(request =>
            request.RequestUri!.AbsolutePath.EndsWith(
                "esearch.fcgi",
                StringComparison.Ordinal)
                ? JsonResponse(
                    """{"header":{"type":"esearch"},"esearchresult":{"count":"0","retmax":"0","retstart":"0","idlist":[]}}""")
                : throw new InvalidOperationException("ESummary must not run."));

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["No match"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Fifty_In_ESearch_Order_And_Marks_Truncation()
    {
        var fixture = BuildFiftyResultFixture(totalCount: 51);
        var handler = RoutedHandler(fixture.Search, fixture.Summary);

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Semaglutide"]),
            RetrievedAt);

        Assert.True(batch.Truncated);
        Assert.Equal(50, batch.Candidates.Count);
        Assert.Equal(
            fixture.Pmids,
            batch.Candidates.Select(candidate => candidate.SourceItemId));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Bad\nTerm")]
    [InlineData("Bad\"Term")]
    [InlineData("Bad'Term")]
    [InlineData("Bad\\Term")]
    [InlineData("Bad[Title]")]
    public async Task Acquire_Rejects_Invalid_Terms_Before_Http(string term)
    {
        var handler = NoNetworkHandler();

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent([term]),
                RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Deduplicates_Terms_And_Rejects_Over_Twenty()
    {
        var handler = await SuccessfulHandler();
        await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Semaglutide", "semaglutide", "Ozempic"]),
            RetrievedAt);
        var query = Uri.UnescapeDataString(handler.Requests[0].RequestUri!.Query);
        Assert.Equal(1, Count(query, "\"Semaglutide\"[Title/Abstract]"));

        var tooMany = Enumerable.Range(1, 21)
            .Select(index => $"term-{index}")
            .ToList();
        var noNetwork = NoNetworkHandler();
        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(noNetwork).AcquireAsync(
                ReadyIntent(tooMany),
                RetrievedAt));
        Assert.Empty(noNetwork.Requests);
    }

    [Theory]
    [InlineData("duplicate-search-id")]
    [InlineData("count-less-than-ids")]
    [InlineData("positive-count-empty-ids")]
    [InlineData("wrong-retmax")]
    [InlineData("negative-count")]
    [InlineData("negative-retstart")]
    [InlineData("invalid-search-id")]
    [InlineData("wrong-search-header")]
    [InlineData("search-status-error")]
    [InlineData("search-errorlist")]
    [InlineData("summary-uids-reordered")]
    [InlineData("summary-uid-duplicate")]
    [InlineData("summary-extra-record")]
    [InlineData("summary-record-uid-mismatch")]
    [InlineData("duplicate-doi")]
    [InlineData("missing-pmid")]
    [InlineData("invalid-pmcid")]
    [InlineData("wrong-summary-header")]
    [InlineData("nonobject-articleid")]
    public async Task Acquire_Fails_Closed_On_Id_And_Result_Correlation_Mutations(
        string mutation)
    {
        var search = await Fixture("pubmed-esearch.synthetic.json");
        var summary = await Fixture("pubmed-esummary.synthetic.json");
        search = mutation switch
        {
            "duplicate-search-id" => search.Replace(
                "\"222\"",
                "\"111\"",
                StringComparison.Ordinal),
            "count-less-than-ids" => search.Replace(
                "\"count\": \"2\"",
                "\"count\": \"1\"",
                StringComparison.Ordinal),
            "positive-count-empty-ids" => search.Replace(
                """
                    "idlist": [
                      "111",
                      "222"
                    ],
                """,
                """
                    "idlist": [],
                """,
                StringComparison.Ordinal),
            "wrong-retmax" => search.Replace(
                "\"retmax\": \"2\"",
                "\"retmax\": \"1\"",
                StringComparison.Ordinal),
            "negative-count" => search.Replace(
                "\"count\": \"2\"",
                "\"count\": \"-1\"",
                StringComparison.Ordinal),
            "negative-retstart" => search.Replace(
                "\"retstart\": \"0\"",
                "\"retstart\": \"-1\"",
                StringComparison.Ordinal),
            "invalid-search-id" => search.Replace(
                "\"111\"",
                "\"0\"",
                StringComparison.Ordinal),
            "wrong-search-header" => search.Replace(
                "\"type\": \"esearch\"",
                "\"type\": \"esummary\"",
                StringComparison.Ordinal),
            "search-status-error" => search.Replace(
                "\"count\": \"2\"",
                "\"status\": \"error\", \"count\": \"2\"",
                StringComparison.Ordinal),
            "search-errorlist" => search.Replace(
                "\"count\": \"2\"",
                "\"errorlist\": {\"phrasesnotfound\": [\"bad\"]}, \"count\": \"2\"",
                StringComparison.Ordinal),
            _ => search,
        };
        summary = mutation switch
        {
            "summary-uids-reordered" => summary.Replace(
                """
                    "uids": [
                      "111",
                      "222"
                    ],
                """,
                """
                    "uids": [
                      "222",
                      "111"
                    ],
                """,
                StringComparison.Ordinal),
            "summary-uid-duplicate" => summary.Replace(
                """
                    "uids": [
                      "111",
                      "222"
                    ],
                """,
                """
                    "uids": [
                      "111",
                      "111"
                    ],
                """,
                StringComparison.Ordinal),
            "summary-extra-record" => summary.Replace(
                """
                    "222": {
                """,
                """
                    "333": {},
                    "222": {
                """,
                StringComparison.Ordinal),
            "summary-record-uid-mismatch" => summary.Replace(
                "\"uid\": \"111\"",
                "\"uid\": \"999\"",
                StringComparison.Ordinal),
            "duplicate-doi" => summary.Replace(
                """
                        {
                          "idtype": "doi",
                          "value": "10.0000/synthetic.111"
                        },
                """,
                """
                        {
                          "idtype": "doi",
                          "value": "10.0000/synthetic.111"
                        },
                        {
                          "idtype": "doi",
                          "value": "10.0000/conflict"
                        },
                """,
                StringComparison.Ordinal),
            "missing-pmid" => summary.Replace(
                "\"idtype\": \"pubmed\"",
                "\"idtype\": \"other\"",
                StringComparison.Ordinal),
            "invalid-pmcid" => summary.Replace(
                "\"value\": \"PMC111\"",
                "\"value\": \"PMCbad\"",
                StringComparison.Ordinal),
            "wrong-summary-header" => summary.Replace(
                "\"type\": \"esummary\"",
                "\"type\": \"esearch\"",
                StringComparison.Ordinal),
            "nonobject-articleid" => summary.Replace(
                "\"articleids\": [",
                "\"articleids\": [\"malformed\",",
                StringComparison.Ordinal),
            _ => summary,
        };
        var handler = RoutedHandler(search, summary);

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_Deduplicates_Identical_Article_Identifiers()
    {
        var search = await Fixture("pubmed-esearch.synthetic.json");
        var summary = (await Fixture("pubmed-esummary.synthetic.json")).Replace(
            """
                    {
                      "idtype": "doi",
                      "value": "10.0000/synthetic.111"
                    },
            """,
            """
                    {
                      "idtype": "doi",
                      "value": "10.0000/synthetic.111"
                    },
                    {
                      "idtype": "doi",
                      "value": "10.0000/synthetic.111"
                    },
            """,
            StringComparison.Ordinal);
        Assert.Equal(2, Count(summary, "10.0000/synthetic.111"));

        var batch = await CreateAdapter(RoutedHandler(search, summary))
            .AcquireAsync(ReadyIntent(["Semaglutide"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Equal(
            ["10.0000/synthetic.111"],
            batch.Candidates[0].SourceSpecificProvenance["doi"].Values);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, SourceAcquisitionBatchStatus.BackPressure)]
    public async Task Acquire_Surfaces_Http_Control_Status_Without_Retry(
        HttpStatusCode statusCode,
        SourceAcquisitionBatchStatus expected)
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(statusCode)
            {
                Content = new StringContent("<html>busy</html>", Encoding.UTF8, "text/html"),
            };
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return response;
        });

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Semaglutide"]),
            RetrievedAt);

        Assert.Equal(expected, batch.Status);
        Assert.Equal("30", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests, SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(HttpStatusCode.ServiceUnavailable, SourceAcquisitionBatchStatus.BackPressure)]
    public async Task Acquire_Discards_All_Output_On_ESummary_Http_Control_Status(
        HttpStatusCode statusCode,
        SourceAcquisitionBatchStatus expected)
    {
        var search = await Fixture("pubmed-esearch.synthetic.json");
        var calls = 0;
        var handler = new RecordingHandler(_ =>
        {
            calls++;
            if (calls == 1) return JsonResponse(search);
            var response = new HttpResponseMessage(statusCode);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(45));
            return response;
        });

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Semaglutide"]),
            RetrievedAt);

        Assert.Equal(expected, batch.Status);
        Assert.Equal("45", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("summary")]
    public async Task Acquire_Surfaces_Json_RateLimit_And_Discards_Partial_Work(
        string stage)
    {
        var search = await Fixture("pubmed-esearch.synthetic.json");
        var calls = 0;
        var handler = new RecordingHandler(_ =>
        {
            calls++;
            if (stage == "search" || calls == 2)
            {
                var response = JsonResponse(
                    """{"error":"API rate limit exceeded"}""");
                response.Headers.RetryAfter =
                    new RetryConditionHeaderValue(TimeSpan.FromSeconds(15));
                return response;
            }
            return JsonResponse(search);
        });

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Semaglutide"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, batch.Status);
        Assert.Equal("15", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Equal(stage == "search" ? 1 : 2, handler.Requests.Count);
    }

    [Fact]
    public async Task Acquire_Rejects_NonRate_Json_Error_Object()
    {
        var handler = new RecordingHandler(_ =>
            JsonResponse("""{"error":{"message":"invalid database"}}"""));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));

        Assert.Equal("source-error-response", exception.Code);
    }

    [Theory]
    [InlineData(HttpStatusCode.Accepted, "asynchronous-response-not-supported")]
    [InlineData(HttpStatusCode.Redirect, "redirect-response")]
    [InlineData(HttpStatusCode.InternalServerError, "http-500")]
    public async Task Acquire_Fails_Closed_On_Unsupported_Http_Status(
        HttpStatusCode statusCode,
        string expectedCode)
    {
        var handler = new RecordingHandler(
            _ => new HttpResponseMessage(statusCode));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_NonJson_And_Malformed_Responses()
    {
        var nonJson = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain"),
            });
        var malformed = new RecordingHandler(_ => JsonResponse("{"));

        var nonJsonException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(nonJson).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));
        var malformedException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(malformed).AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));

        Assert.Equal("unexpected-content-type", nonJsonException.Code);
        Assert.Equal("malformed-json", malformedException.Code);
    }

    [Theory]
    [InlineData("search")]
    [InlineData("summary")]
    public async Task Acquire_Enforces_Operation_Specific_Response_Size_Caps(
        string stage)
    {
        var search = await Fixture("pubmed-esearch.synthetic.json");
        var summary = await Fixture("pubmed-esummary.synthetic.json");
        var handler = RoutedHandler(search, summary);
        var adapter = stage == "search"
            ? CreateAdapter(handler, searchMaximumResponseBytes: 10)
            : CreateAdapter(handler, summaryMaximumResponseBytes: 10);

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(
                ReadyIntent(["Semaglutide"]),
                RetrievedAt));

        Assert.Equal("response-too-large", exception.Code);
        Assert.Equal(stage == "search" ? 1 : 2, handler.Requests.Count);
    }

    [Fact]
    public async Task Pacer_Waits_334ms_And_Honors_Exact_Boundary()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var delays = new List<TimeSpan>();
        var gate = new NcbiEutilitiesPacedRequestGate(
            time,
            (delay, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                delays.Add(delay);
                time.Advance(delay);
                return ValueTask.CompletedTask;
            });

        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }
        time.Advance(NcbiEutilitiesPacedRequestGate.MinimumStartInterval);
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Equal(
            [NcbiEutilitiesPacedRequestGate.MinimumStartInterval],
            delays);
    }

    [Fact]
    public async Task Pacer_Serializes_And_Releases_After_Cancelled_Delay()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        using var cancellation = new CancellationTokenSource();
        var gate = new NcbiEutilitiesPacedRequestGate(
            time,
            (delay, cancellationToken) =>
            {
                if (cancellationToken.CanBeCanceled)
                {
                    cancellation.Cancel();
                    return new ValueTask(Task.FromCanceled(cancellationToken));
                }
                time.Advance(delay);
                return ValueTask.CompletedTask;
            });
        var first = await gate.AcquireAsync(CancellationToken.None);
        var blocked = gate.AcquireAsync(CancellationToken.None).AsTask();
        Assert.False(blocked.IsCompleted);
        first.Dispose();
        using (await blocked)
        {
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => gate.AcquireAsync(cancellation.Token).AsTask());
        using var recovered =
            await gate.AcquireAsync(CancellationToken.None);
    }

    private static PubMedEutilitiesCitationMetadataAcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int searchMaximumResponseBytes =
            PubMedEutilitiesCitationMetadataAcquisitionAdapter
                .DefaultSearchMaximumResponseBytes,
        int summaryMaximumResponseBytes =
            PubMedEutilitiesCitationMetadataAcquisitionAdapter
                .DefaultSummaryMaximumResponseBytes)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            Tool,
            Email,
            searchMaximumResponseBytes,
            summaryMaximumResponseBytes,
            new PassThroughRequestGate());

    private static async Task<RecordingHandler> SuccessfulHandler()
        => RoutedHandler(
            await Fixture("pubmed-esearch.synthetic.json"),
            await Fixture("pubmed-esummary.synthetic.json"));

    private static RecordingHandler RoutedHandler(
        string search,
        string summary)
        => new(request =>
            request.RequestUri!.AbsolutePath.EndsWith(
                "esearch.fcgi",
                StringComparison.Ordinal)
                ? JsonResponse(search)
                : JsonResponse(summary));

    private static RecordingHandler NoNetworkHandler()
        => new(_ => throw new InvalidOperationException("HTTP must not run."));

    private static SourceAcquisitionIntent ReadyIntent(
        IReadOnlyList<string> searchTerms)
        => new(
            SourceId: "pubmed",
            AdapterId:
                PubMedEutilitiesCitationMetadataAcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-semaglutide-001",
            CompoundName: "Semaglutide",
            SearchTerms: searchTerms,
            CandidateMethod: "api",
            AuthorizedFieldUses:
            [
                "mechanism",
                "efficacy-claims",
                "interactions",
            ],
            RequiredProvenanceFields:
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "pmid",
                "doi",
                "pmcid",
                "publicationDate",
                "retrievedAtUtc",
                "query",
                "rightsReviewStatusAtRetrieval",
                "transformationPipelineVersion",
                "humanReviewStatus",
            ],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);

    private static async Task<string> Fixture(string name)
        => await File.ReadAllTextAsync(TestPaths.FixturePath(name));

    private static FiftyResultFixture BuildFiftyResultFixture(long totalCount)
    {
        var pmids = Enumerable.Range(1001, 50)
            .Select(value => value.ToString())
            .ToList();
        var idList = new JsonArray();
        var uids = new JsonArray();
        foreach (var pmid in pmids)
        {
            idList.Add(pmid);
            uids.Add(pmid);
        }
        var search = new JsonObject
        {
            ["header"] = new JsonObject { ["type"] = "esearch" },
            ["esearchresult"] = new JsonObject
            {
                ["count"] = totalCount.ToString(),
                ["retmax"] = "50",
                ["retstart"] = "0",
                ["idlist"] = idList,
            },
        };
        var result = new JsonObject { ["uids"] = uids };
        foreach (var pmid in pmids)
        {
            result[pmid] = new JsonObject
            {
                ["uid"] = pmid,
                ["pubdate"] = "2025",
                ["title"] = $"Synthetic citation {pmid}.",
                ["articleids"] = new JsonArray
                {
                    new JsonObject
                    {
                        ["idtype"] = "pubmed",
                        ["value"] = pmid,
                    },
                },
            };
        }
        var summary = new JsonObject
        {
            ["header"] = new JsonObject { ["type"] = "esummary" },
            ["result"] = result,
        };
        return new FiftyResultFixture(
            pmids,
            search.ToJsonString(),
            summary.ToJsonString());
    }

    private static HttpResponseMessage JsonResponse(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(body, Encoding.UTF8, "application/json"),
        };

    private static int Count(string source, string value)
        => (source.Length - source.Replace(
                value,
                string.Empty,
                StringComparison.Ordinal).Length)
           / value.Length;

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

    private sealed class PassThroughRequestGate : ISourceRequestGate
    {
        public ValueTask<IDisposable> AcquireAsync(
            CancellationToken cancellationToken)
            => ValueTask.FromResult<IDisposable>(new NoOpLease());
    }

    private sealed class NoOpLease : IDisposable
    {
        public void Dispose()
        {
        }
    }

    private sealed class ManualTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        private DateTimeOffset _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan amount) => _utcNow = _utcNow.Add(amount);
    }

    private sealed record FiftyResultFixture(
        IReadOnlyList<string> Pmids,
        string Search,
        string Summary);
}
