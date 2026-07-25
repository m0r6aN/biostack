namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class PubChemPugRestCompoundAcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T16:00:00Z");

    [Fact]
    public async Task Acquire_Uses_Fixed_ExactName_And_ModifyDate_Endpoints()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(
            ReadyIntent(["Acetylsalicylic acid"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.False(batch.Truncated);
        Assert.Equal(2, handler.Requests.Count);
        var propertyRequest = handler.Requests[0];
        Assert.Equal(HttpMethod.Get, propertyRequest.Method);
        Assert.Equal("https", propertyRequest.RequestUri!.Scheme);
        Assert.Equal("pubchem.ncbi.nlm.nih.gov", propertyRequest.RequestUri.Host);
        Assert.Contains(
            "/rest/pug/compound/name/Acetylsalicylic%20acid/property/",
            propertyRequest.RequestUri.AbsoluteUri);
        Assert.EndsWith(
            "/MolecularFormula,MolecularWeight,SMILES,InChI,InChIKey,ExactMass/JSON",
            propertyRequest.RequestUri.AbsolutePath);
        var dateRequest = handler.Requests[1];
        Assert.Equal(
            "/rest/pug_view/data/compound/2244/JSON",
            dateRequest.RequestUri!.AbsolutePath);
        Assert.Equal("?heading=Modify%20Date", dateRequest.RequestUri.Query);
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
    [InlineData("mechanism-only")]
    public async Task Acquire_Rejects_Unauthorized_Intent_Before_Http(
        string mutation)
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent(["Aspirin"]);
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["pubchem:legal-rights-not-approved"],
            },
            "other-source" => intent with { SourceId = "pubmed" },
            "wrong-planner" => intent with { AdapterId = "pubchem-planning-v2" },
            "wrong-method" => intent with { CandidateMethod = "bulk-download" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId", "pubchemCid"],
            },
            "mechanism-only" => intent with
            {
                AuthorizedFieldUses = ["mechanism"],
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_One_Allowlisted_ReviewRequired_Candidate()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);

        var candidate = Assert.Single(
            (await adapter.AcquireAsync(
                ReadyIntent(["Aspirin"]),
                RetrievedAt)).Candidates);

        Assert.Equal("market-aspirin-001", candidate.RequestId);
        Assert.Equal("Aspirin", candidate.CompoundName);
        Assert.Equal("pubchem", candidate.SourceRegistryId);
        Assert.Equal("2244", candidate.SourceItemId);
        Assert.Equal(
            "https://pubchem.ncbi.nlm.nih.gov/compound/2244",
            candidate.SourceUrl);
        Assert.Contains("/name/Aspirin/property/", candidate.QueryUrl);
        Assert.Equal("2026-07-01", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(RetrievedAt, candidate.RetrievedAtUtc);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal(RegistrySha256, candidate.RegistryBindingSha256);
        Assert.Equal(
            PubChemPugRestCompoundAcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(["identity"], candidate.AuthorizedFieldUses);
        Assert.Equal(
            ["2244"],
            candidate.SourceSpecificProvenance["pubchemCid"].Values);
        Assert.Equal(
            ["2026-07-01"],
            candidate.SourceSpecificProvenance["recordUpdateDate"].Values);
        Assert.Equal(["C9H8O4"], candidate.Fields["molecular_formula"]);
        Assert.Equal(["180.16"], candidate.Fields["molecular_weight"]);
        Assert.Equal(["180.04225873"], candidate.Fields["exact_mass"]);
        Assert.Equal(
            new[]
            {
                "exact_mass",
                "inchi",
                "inchikey",
                "molecular_formula",
                "molecular_weight",
                "smiles",
            },
            candidate.Fields.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain("IUPACName", candidate.Fields.Keys);
        Assert.DoesNotContain("ContributorDescription", candidate.Fields.Keys);
        var attribution = Assert.Single(candidate.RightsAttributions);
        Assert.Equal(
            "PubChem-computed compound properties and PubChem record metadata only.",
            attribution.Scope);
        Assert.Equal(
            "PubChem, National Center for Biotechnology Information",
            attribution.Provider);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            "https://www.ncbi.nlm.nih.gov/home/about/policies/",
            attribution.TermsUrl);
        Assert.Equal("reviewed", attribution.RightsStatus);
        Assert.Equal(
            new[]
            {
                "exact_mass",
                "inchi",
                "inchikey",
                "molecular_formula",
                "molecular_weight",
                "pubchemCid",
                "recordUpdateDate",
                "smiles",
            },
            attribution.CoveredFields.Order(StringComparer.OrdinalIgnoreCase));
        Assert.Equal(
            "PubChem is the source of the computed compound properties and record metadata.",
            candidate.ReuseBoundary.Acknowledgement);
        Assert.False(string.IsNullOrWhiteSpace(candidate.ReuseBoundary.Acknowledgement));
        Assert.Equal(
            new[]
            {
                "bioassays",
                "contributor annotations",
                "descriptions",
                "mechanism claims",
                "synonyms",
            },
            candidate.ReuseBoundary.ExcludedContentClasses.Order(
                StringComparer.OrdinalIgnoreCase));
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
    }

    [Fact]
    public async Task Acquire_Encodes_Reserved_Characters_As_One_Name_Path_Segment()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);
        const string term = "A/B?C#D%E&F=G";

        await adapter.AcquireAsync(ReadyIntent([term]), RetrievedAt);

        var requestUri = handler.Requests[0].RequestUri!;
        Assert.Equal("https", requestUri.Scheme);
        Assert.Equal("pubchem.ncbi.nlm.nih.gov", requestUri.Host);
        Assert.Equal(string.Empty, requestUri.Query);
        Assert.Equal(string.Empty, requestUri.Fragment);
        Assert.Contains(
            "/name/A%2FB%3FC%23D%25E%26F%3DG/property/",
            requestUri.AbsoluteUri);
        Assert.EndsWith(
            "/property/MolecularFormula,MolecularWeight,SMILES,InChI,InChIKey,ExactMass/JSON",
            requestUri.AbsolutePath);
        Assert.Equal(
            "A%2FB%3FC%23D%25E%26F%3DG/",
            requestUri.Segments[5]);
    }

    [Fact]
    public async Task Acquire_Deduplicates_Aliases_Only_When_They_Resolve_To_Same_Cid()
    {
        var handler = await SuccessfulHandler();
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(
            ReadyIntent(["Aspirin", "Acetylsalicylic acid", "aspirin"]),
            RetrievedAt);

        Assert.Single(batch.Candidates);
        Assert.Equal(3, handler.Requests.Count);
        Assert.Equal(
            2,
            handler.Requests.Count(request =>
                request.RequestUri!.AbsolutePath.Contains(
                    "/rest/pug/compound/name/",
                    StringComparison.Ordinal)));
        Assert.Single(
            handler.Requests,
            request => request.RequestUri!.AbsolutePath.Contains(
                "/rest/pug_view/",
                StringComparison.Ordinal));
    }

    [Fact]
    public async Task Acquire_Rejects_Multiple_Results_And_Alias_Cid_Disagreement()
    {
        var propertyBody = await Fixture("pubchem-properties.synthetic.json");
        var multiple = propertyBody.Replace(
            """
                  }
                ]
            """,
            """
                  },
                  {
                    "CID": 3672,
                    "MolecularFormula": "C13H18O2"
                  }
                ]
            """,
            StringComparison.Ordinal);
        var multipleHandler = new RecordingHandler(_ => JsonResponse(multiple));

        var multipleException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(multipleHandler)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));

        var call = 0;
        var disagreementHandler = new RecordingHandler(_ =>
        {
            call++;
            var body = call == 1
                ? propertyBody
                : propertyBody.Replace("2244", "3672", StringComparison.Ordinal);
            return JsonResponse(body);
        });
        var disagreementException =
            await Assert.ThrowsAsync<SourceAcquisitionException>(
                () => CreateAdapter(disagreementHandler)
                    .AcquireAsync(
                        ReadyIntent(["Aspirin", "Other aspirin"]),
                        RetrievedAt));

        Assert.Equal("ambiguous-compound-resolution", multipleException.Code);
        Assert.Equal("ambiguous-compound-resolution", disagreementException.Code);
        Assert.Equal(2, disagreementHandler.Requests.Count);
    }

    [Fact]
    public async Task Acquire_Treats_PerTerm_404s_As_NoMatch()
    {
        var allMissing = new RecordingHandler(
            _ => new HttpResponseMessage(HttpStatusCode.NotFound));
        var allMissingBatch = await CreateAdapter(allMissing)
            .AcquireAsync(
                ReadyIntent(["Not a compound", "Also absent"]),
                RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, allMissingBatch.Status);
        Assert.Empty(allMissingBatch.Candidates);
        Assert.Equal(2, allMissing.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]
    [InlineData(HttpStatusCode.ServiceUnavailable)]
    public async Task Acquire_Surfaces_RateLimit_And_RetryAfter_Without_Retry(
        HttpStatusCode statusCode)
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

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, batch.Status);
        Assert.Equal("30", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(
        "Request Count status: Red (80%), Request Time status: Green (0%), Service status: Green (10%)",
        SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(
        "Request Count status: Green (0%), Request Time status: Black (100%), Service status: Green (10%)",
        SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(
        "Request Count status: Green (0%), Request Time status: Green (0%), Service status: Red (80%)",
        SourceAcquisitionBatchStatus.BackPressure)]
    public async Task Acquire_Stops_On_Dynamic_Throttling_Header(
        string header,
        SourceAcquisitionBatchStatus expectedStatus)
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = JsonResponse("""{"ignored":true}""");
            response.Headers.TryAddWithoutValidation("X-Throttling-Control", header);
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt);

        Assert.Equal(expectedStatus, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Allows_Yellow_Dynamic_Throttling_Status()
    {
        var propertyBody = await Fixture("pubchem-properties.synthetic.json");
        var modifyDateBody = await Fixture("pubchem-modify-date.synthetic.json");
        var handler = new RecordingHandler(request =>
        {
            var response = JsonResponse(
                request.RequestUri!.AbsolutePath.Contains(
                    "/rest/pug_view/",
                    StringComparison.Ordinal)
                    ? modifyDateBody
                    : propertyBody);
            response.Headers.TryAddWithoutValidation(
                "X-Throttling-Control",
                "Request Count status: Yellow (60%), Request Time status: Yellow (55%), Service status: Yellow (65%)");
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Single(batch.Candidates);
        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Acquire_Uses_Service_BackPressure_Before_503_Fallback()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = new HttpResponseMessage(
                HttpStatusCode.ServiceUnavailable)
            {
                Content = new StringContent(
                    "<html>busy</html>",
                    Encoding.UTF8,
                    "text/html"),
            };
            response.Headers.TryAddWithoutValidation(
                "X-Throttling-Control",
                "Request Count status: Green (0%), Request Time status: Green (0%), Service status: Black (100%)");
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.BackPressure, batch.Status);
        Assert.Empty(batch.Candidates);
    }

    [Fact]
    public async Task Acquire_Rejects_Duplicate_Or_Conflicting_Throttle_Indicators()
    {
        var handler = new RecordingHandler(_ =>
        {
            var response = JsonResponse("""{"ignored":true}""");
            response.Headers.TryAddWithoutValidation(
                "X-Throttling-Control",
                "Request Count status: Green (0%), Request Count status: Red (80%), Request Time status: Green (0%), Service status: Green (0%)");
            return response;
        });

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));

        Assert.Equal("throttling-header-invalid", exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Discards_Partial_Work_On_Later_Throttle()
    {
        var propertyBody = await Fixture("pubchem-properties.synthetic.json");
        var calls = 0;
        var handler = new RecordingHandler(_ =>
        {
            calls++;
            if (calls <= 2) return JsonResponse(propertyBody);
            var response = new HttpResponseMessage(HttpStatusCode.ServiceUnavailable);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(10));
            return response;
        });

        var batch = await CreateAdapter(handler)
            .AcquireAsync(
                ReadyIntent(["Aspirin", "Acetylsalicylic acid"]),
                RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.RateLimited, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.Equal(3, handler.Requests.Count);
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
            () => CreateAdapter(handler)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_NonJson_Malformed_And_Oversized_Responses()
    {
        var nonJson = new RecordingHandler(_ =>
            new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("not json", Encoding.UTF8, "text/plain"),
            });
        var malformed = new RecordingHandler(_ => JsonResponse("{"));
        var oversized = new RecordingHandler(_ => JsonResponse(
            """{"PropertyTable":{"Properties":[]}}"""));

        var nonJsonException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(nonJson)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));
        var malformedException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(malformed)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));
        var oversizedException = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(oversized, maximumResponseBytes: 10)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));

        Assert.Equal("unexpected-content-type", nonJsonException.Code);
        Assert.Equal("malformed-json", malformedException.Code);
        Assert.Equal("response-too-large", oversizedException.Code);
    }

    [Theory]
    [InlineData("missing-cid")]
    [InlineData("missing-fields")]
    [InlineData("record-type")]
    [InlineData("cid-mismatch")]
    [InlineData("duplicate-date")]
    [InlineData("mixed-date-array")]
    [InlineData("cross-item-date-reference")]
    [InlineData("wrong-reference-source")]
    [InlineData("wrong-reference-id")]
    [InlineData("wrong-reference-origin")]
    public async Task Acquire_Rejects_Malformed_Or_Uncorrelated_Records(
        string mutation)
    {
        var propertyBody = await Fixture("pubchem-properties.synthetic.json");
        var modifyDateBody = await Fixture("pubchem-modify-date.synthetic.json");
        propertyBody = mutation switch
        {
            "missing-cid" => propertyBody.Replace(
                "\"CID\": 2244,",
                "\"NotCID\": 2244,",
                StringComparison.Ordinal),
            "missing-fields" =>
                """{"PropertyTable":{"Properties":[{"CID":2244}]}}""",
            _ => propertyBody,
        };
        modifyDateBody = mutation switch
        {
            "record-type" => modifyDateBody.Replace(
                "\"RecordType\": \"CID\"",
                "\"RecordType\": \"SID\"",
                StringComparison.Ordinal),
            "cid-mismatch" => modifyDateBody.Replace(
                "\"RecordNumber\": 2244",
                "\"RecordNumber\": 3672",
                StringComparison.Ordinal),
            "duplicate-date" => modifyDateBody.Replace(
                "\"2026-07-01\"",
                "\"2026-07-01\", \"2026-07-02\"",
                StringComparison.Ordinal),
            "mixed-date-array" => modifyDateBody.Replace(
                "\"2026-07-01\"",
                "\"2026-07-01\", 7",
                StringComparison.Ordinal),
            "cross-item-date-reference" =>
                """
                {
                  "Record": {
                    "RecordType": "CID",
                    "RecordNumber": 2244,
                    "Section": [
                      {
                        "TOCHeading": "Modify Date",
                        "Information": [
                          {
                            "ReferenceNumber": 1,
                            "Value": {}
                          },
                          {
                            "Value": {
                              "DateISO8601": ["2026-07-01"]
                            }
                          }
                        ]
                      }
                    ],
                    "Reference": [
                      {
                        "ReferenceNumber": 1,
                        "SourceName": "PubChem",
                        "SourceID": "PubChem",
                        "URL": "https://pubchem.ncbi.nlm.nih.gov/"
                      }
                    ]
                  }
                }
                """,
            "wrong-reference-source" => modifyDateBody.Replace(
                "\"SourceName\": \"PubChem\"",
                "\"SourceName\": \"Contributor\"",
                StringComparison.Ordinal),
            "wrong-reference-id" => modifyDateBody.Replace(
                "\"SourceID\": \"PubChem\"",
                "\"SourceID\": \"Other\"",
                StringComparison.Ordinal),
            "wrong-reference-origin" => modifyDateBody.Replace(
                "https://pubchem.ncbi.nlm.nih.gov/",
                "https://example.invalid/",
                StringComparison.Ordinal),
            _ => modifyDateBody,
        };
        var handler = RoutedHandler(propertyBody, modifyDateBody);

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler)
                .AcquireAsync(ReadyIntent(["Aspirin"]), RetrievedAt));
    }

    [Theory]
    [InlineData("")]
    [InlineData("Aspirin\nInjected")]
    [InlineData("Aspirin\" OR true")]
    [InlineData("Aspirin's")]
    [InlineData("..")]
    public async Task Acquire_Rejects_Invalid_Search_Terms_Before_Http(string term)
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler)
                .AcquireAsync(ReadyIntent([term]), RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_Too_Many_Distinct_Terms_Before_Http()
    {
        var handler = new RecordingHandler(
            _ => throw new InvalidOperationException("HTTP must not run."));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler)
                .AcquireAsync(
                    ReadyIntent(["one", "two", "three", "four", "five", "six"]),
                    RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Paced_RequestGate_Waits_For_The_Minimum_Start_Interval()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var delays = new List<TimeSpan>();
        var gate = new PubChemPacedRequestGate(
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

        Assert.Equal([PubChemPacedRequestGate.MinimumStartInterval], delays);
    }

    [Fact]
    public async Task Paced_RequestGate_Does_Not_Wait_At_The_Exact_Boundary()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var delays = new List<TimeSpan>();
        var gate = new PubChemPacedRequestGate(
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
        time.Advance(PubChemPacedRequestGate.MinimumStartInterval);
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }

        Assert.Empty(delays);
    }

    [Fact]
    public async Task Paced_RequestGate_Serializes_Across_Shared_Callers()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        var gate = new PubChemPacedRequestGate(
            time,
            (delay, cancellationToken) =>
            {
                cancellationToken.ThrowIfCancellationRequested();
                time.Advance(delay);
                return ValueTask.CompletedTask;
            });
        var firstLease = await gate.AcquireAsync(CancellationToken.None);

        var second = gate.AcquireAsync(CancellationToken.None).AsTask();

        Assert.False(second.IsCompleted);
        firstLease.Dispose();
        using var secondLease = await second;
    }

    [Fact]
    public async Task Paced_RequestGate_Releases_Serialization_When_Delay_Is_Cancelled()
    {
        var time = new ManualTimeProvider(RetrievedAt);
        using var cancellation = new CancellationTokenSource();
        var gate = new PubChemPacedRequestGate(
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
        using (await gate.AcquireAsync(CancellationToken.None))
        {
        }

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => gate.AcquireAsync(cancellation.Token).AsTask());

        using var recoveredLease =
            await gate.AcquireAsync(CancellationToken.None);
    }

    private static PubChemPugRestCompoundAcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int maximumResponseBytes =
            PubChemPugRestCompoundAcquisitionAdapter.DefaultMaximumResponseBytes)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            maximumResponseBytes,
            new PassThroughRequestGate());

    private static async Task<RecordingHandler> SuccessfulHandler()
        => RoutedHandler(
            await Fixture("pubchem-properties.synthetic.json"),
            await Fixture("pubchem-modify-date.synthetic.json"));

    private static RecordingHandler RoutedHandler(
        string propertyBody,
        string modifyDateBody)
        => new(request =>
            request.RequestUri!.AbsolutePath.Contains(
                "/rest/pug_view/",
                StringComparison.Ordinal)
                ? JsonResponse(modifyDateBody)
                : JsonResponse(propertyBody));

    private static SourceAcquisitionIntent ReadyIntent(
        IReadOnlyList<string> searchTerms)
        => new(
            SourceId: "pubchem",
            AdapterId:
                PubChemPugRestCompoundAcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-aspirin-001",
            CompoundName: "Aspirin",
            SearchTerms: searchTerms,
            CandidateMethod: "api",
            AuthorizedFieldUses: ["identity", "mechanism"],
            RequiredProvenanceFields:
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "pubchemCid",
                "recordUpdateDate",
                "retrievedAtUtc",
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
}
