namespace BioStack.KnowledgeWorker.Tests;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json.Nodes;
using BioStack.KnowledgeWorker.Pipeline;
using Xunit;

public class ClinicalTrialsGovV2AcquisitionAdapterTests
{
    private const string RegistrySha256 =
        "3c8425e090f31ea17eb4d6a10f8ea8a5e2f352f753f3c5312fc7fcce80d03e28";
    private static readonly DateTimeOffset RetrievedAt =
        DateTimeOffset.Parse("2026-07-25T16:00:00Z");

    [Fact]
    public async Task Acquire_Uses_Fixed_InterventionName_Query_And_Explicit_Fields()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));
        var adapter = CreateAdapter(handler);

        await adapter.AcquireAsync(
            ReadyIntent(["Retatrutide", "LY3437943"]),
            RetrievedAt);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Get, request.Method);
        Assert.Equal("clinicaltrials.gov", request.Uri.Host);
        Assert.Equal("/api/v2/studies", request.Uri.AbsolutePath);
        var query = ParseQuery(request.Uri);
        Assert.Equal(
            ["countTotal", "fields", "format", "pageSize", "query.intr"],
            query.Keys.Order(StringComparer.Ordinal));
        Assert.Equal("json", query["format"]);
        Assert.Equal(
            "(AREA[InterventionName]\"Retatrutide\" OR AREA[InterventionName]\"LY3437943\")",
            query["query.intr"]);
        Assert.Equal(
            "NCTId,BriefTitle,OfficialTitle,OrgFullName,OverallStatus,"
            + "LastUpdateSubmitDate,StudyFirstPostDate,LastUpdatePostDate,"
            + "StudyType,Phase,Condition,LeadSponsorName,LeadSponsorClass,"
            + "InterventionName,InterventionType,InterventionOtherName,"
            + "PrimaryOutcomeMeasure,PrimaryOutcomeTimeFrame,"
            + "SecondaryOutcomeMeasure,SecondaryOutcomeTimeFrame",
            query["fields"]);
        Assert.Equal("50", query["pageSize"]);
        Assert.Equal("true", query["countTotal"]);
        Assert.DoesNotContain("pageToken", query.Keys);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("blocked")]
    [InlineData("wrong-source")]
    [InlineData("wrong-planner")]
    [InlineData("wrong-method")]
    [InlineData("stale-registry")]
    [InlineData("missing-provenance")]
    [InlineData("unsupported-use")]
    [InlineData("query-injection")]
    [InlineData("control-character")]
    [InlineData("too-many-terms")]
    public async Task Acquire_Rejects_Invalid_Intent_Or_Query_Before_Http(
        string mutation)
    {
        var handler = new RecordingHandler(
            (_, _) => throw new InvalidOperationException("HTTP must not run."));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent(["Retatrutide"]);
        intent = mutation switch
        {
            "blocked" => intent with
            {
                Disposition = SourceAcquisitionDisposition.Blocked,
                BlockingReasons = ["clinicaltrials:legal-rights-not-approved"],
            },
            "wrong-source" => intent with { SourceId = "pubmed" },
            "wrong-planner" => intent with
            {
                AdapterId = "clinicaltrials-planning-v2",
            },
            "wrong-method" => intent with { CandidateMethod = "bulk-download" },
            "stale-registry" => intent with
            {
                RegistryBindingSha256 = new string('0', 64),
            },
            "missing-provenance" => intent with
            {
                RequiredProvenanceFields = ["sourceItemId"],
            },
            "unsupported-use" => intent with
            {
                AuthorizedFieldUses = ["regulatory"],
            },
            "query-injection" => intent with
            {
                SearchTerms =
                [
                    "Retatrutide\") OR AREA[NCTId]\"NCT00000001",
                ],
            },
            "control-character" => intent with
            {
                SearchTerms = ["Retatrutide\nInjected"],
            },
            "too-many-terms" => intent with
            {
                SearchTerms = Enumerable.Range(1, 21)
                    .Select(index => $"Synthetic-{index}")
                    .ToList(),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(mutation)),
        };

        await Assert.ThrowsAnyAsync<Exception>(
            () => adapter.AcquireAsync(intent, RetrievedAt));

        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Emits_Allowlisted_ReviewRequired_Registry_Context()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));
        var adapter = CreateAdapter(handler);

        var batch = await adapter.AcquireAsync(
            ReadyIntent(["Retatrutide"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.False(batch.Truncated);
        Assert.Null(batch.RetryAfter);
        var candidate = Assert.Single(batch.Candidates);
        Assert.Equal("clinicaltrials", candidate.SourceRegistryId);
        Assert.Equal("NCT00000001", candidate.SourceItemId);
        Assert.Equal(
            "https://clinicaltrials.gov/study/NCT00000001",
            candidate.SourceUrl);
        Assert.Contains("query.intr=", candidate.QueryUrl);
        Assert.Equal("2026-07-20", candidate.SourcePublicationOrUpdateDate);
        Assert.Equal(RetrievedAt, candidate.RetrievedAtUtc);
        Assert.Equal("reviewed", candidate.RightsReviewStatusAtRetrieval);
        Assert.Equal("review-required", candidate.HumanReviewStatus);
        Assert.Equal(
            ClinicalTrialsGovV2AcquisitionAdapter.TransformationVersion,
            candidate.TransformationPipelineVersion);

        Assert.Equal(["NCT00000001"], candidate.Fields["nct_id"]);
        Assert.Equal(
            ["Synthetic Trial Registry Organization"],
            candidate.Fields["organization_full_name"]);
        Assert.Equal(
            ["Synthetic Sponsor"],
            candidate.Fields["lead_sponsor_name"]);
        Assert.Equal(["PHASE2"], candidate.Fields["registered_phase"]);
        Assert.Equal(
            ["Synthetic primary outcome name | Time frame: Synthetic week 24"],
            candidate.Fields["registered_primary_outcomes"]);
        Assert.Equal(
            [
                "Co-listed intervention in registered study design: Synthetic comparator (DRUG)",
            ],
            candidate.Fields["co_listed_interventions_design_context"]);
        Assert.Equal(
            new[]
            {
                "brief_title",
                "co_listed_interventions_design_context",
                "conditions",
                "intervention_names",
                "intervention_other_names",
                "intervention_types",
                "lead_sponsor_class",
                "lead_sponsor_name",
                "nct_id",
                "official_title",
                "organization_full_name",
                "registered_phase",
                "registered_primary_outcomes",
                "registered_secondary_outcomes",
                "study_type",
            }.Order(StringComparer.OrdinalIgnoreCase),
            candidate.Fields.Keys.Order(StringComparer.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            candidate.Fields.SelectMany(pair => pair.Value),
            value => value.Contains(
                "must not be emitted",
                StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            candidate.Fields.Keys,
            key => key.Contains("result", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(
            candidate.Fields.Keys,
            key => key.Contains("interaction", StringComparison.OrdinalIgnoreCase));

        Assert.Equal(
            "present",
            candidate.SourceSpecificProvenance["nctId"].Availability);
        Assert.Equal(
            ["RECRUITING"],
            candidate.SourceSpecificProvenance["overallStatus"].Values);
        Assert.Equal(
            ["PHASE2"],
            candidate.SourceSpecificProvenance["phase"].Values);
        Assert.DoesNotContain(
            "dataTimestamp",
            candidate.SourceSpecificProvenance.Keys);
        Assert.Equal(
            ["v2"],
            candidate.SourceSpecificProvenance["apiVersion"].Values);
        Assert.Equal(
            ["Synthetic Sponsor"],
            candidate.SourceSpecificProvenance["leadSponsorName"].Values);
        Assert.Equal(
            ["2026-07-22"],
            candidate.SourceSpecificProvenance["lastUpdatePostDate"].Values);

        var attribution = Assert.Single(candidate.RightsAttributions);
        Assert.Contains("sponsor", attribution.Provider);
        Assert.Equal(candidate.SourceUrl, attribution.SourceUrl);
        Assert.Equal(
            candidate.Fields.Keys.OrderBy(key => key, StringComparer.Ordinal),
            attribution.CoveredFields);
        var document = Assert.Single(candidate.DocumentProvenance);
        Assert.Equal(
            "Synthetic Protocol Metadata for Adapter Testing",
            document.Title);
        Assert.Equal("2026-01-10", document.PublishedDate);
        Assert.Equal("2026-07-22", document.UpdatedDate);
        Assert.True(candidate.ReuseBoundary.NonEndorsementRequired);
        Assert.Contains(
            "submitted",
            candidate.ReuseBoundary.Acknowledgement,
            StringComparison.OrdinalIgnoreCase);
        Assert.Contains(
            "results measurements and statistical analyses",
            candidate.ReuseBoundary.ExcludedContentClasses);
        Assert.Equal(3, candidate.EvidenceLimitations.Count);
    }

    [Fact]
    public async Task Acquire_Intersects_Intent_Field_Uses()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));
        var adapter = CreateAdapter(handler);
        var intent = ReadyIntent(["Retatrutide"]) with
        {
            AuthorizedFieldUses = ["identity", "regulatory"],
        };

        var candidate = Assert.Single(
            (await adapter.AcquireAsync(intent, RetrievedAt)).Candidates);

        Assert.Equal(["identity"], candidate.AuthorizedFieldUses);
        Assert.Contains("nct_id", candidate.Fields.Keys);
        Assert.DoesNotContain("registered_phase", candidate.Fields.Keys);
        Assert.DoesNotContain(
            "co_listed_interventions_design_context",
            candidate.Fields.Keys);
    }

    [Fact]
    public async Task Acquire_Uses_Typed_NotApplicable_Only_For_Official_Na_Phase()
    {
        var root = ValidNode();
        PhaseArray(root).Clear();
        PhaseArray(root).Add("NA");
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));
        var adapter = CreateAdapter(handler);

        var candidate = Assert.Single(
            (await adapter.AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt)).Candidates);

        var phase = candidate.SourceSpecificProvenance["phase"];
        Assert.Equal("not-applicable", phase.Availability);
        Assert.Empty(phase.Values);
        Assert.Contains("official phase value NA", phase.UnavailableReason);
        Assert.Equal(
            ["Official registry value: NA (not applicable)"],
            candidate.Fields["registered_phase"]);
    }

    [Theory]
    [InlineData("missing")]
    [InlineData("empty")]
    [InlineData("na-combined")]
    [InlineData("unknown")]
    public async Task Acquire_Rejects_Missing_Or_Invalid_Phase(string mutation)
    {
        var root = ValidNode();
        var design = DesignModule(root);
        if (mutation == "missing")
        {
            design.Remove("phases");
        }
        else
        {
            PhaseArray(root).Clear();
            if (mutation == "na-combined")
            {
                PhaseArray(root).Add("NA");
                PhaseArray(root).Add("PHASE2");
            }
            else if (mutation == "unknown")
            {
                PhaseArray(root).Add("SYNTHETIC_UNKNOWN_PHASE");
            }
        }
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));
        var adapter = CreateAdapter(handler);

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => adapter.AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_Rejects_Unmapped_Returned_Study_From_Candidate_Output()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Unmapped-Synthetic-Alias"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.Completed, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.False(batch.Truncated);
    }

    [Fact]
    public async Task Acquire_Accepts_Exact_Intervention_OtherName_Match()
    {
        var root = ValidNode();
        var intervention = Protocol(root)["armsInterventionsModule"]!
            ["interventions"]![0]!.AsObject();
        intervention["name"] = "Synthetic canonical intervention";
        intervention["otherNames"] = new JsonArray("Retatrutide");
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var candidate = Assert.Single(
            (await CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt)).Candidates);

        Assert.Equal("NCT00000001", candidate.SourceItemId);
    }

    [Fact]
    public async Task Acquire_Empty_Studies_Is_NoMatch()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(
                JsonResponse("""{"studies":[],"totalCount":0}""")));
        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Synthetic-No-Match"]),
            RetrievedAt);

        Assert.Equal(SourceAcquisitionBatchStatus.NoMatch, batch.Status);
        Assert.Empty(batch.Candidates);
        Assert.False(batch.Truncated);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData("identical")]
    [InlineData("conflicting")]
    public async Task Acquire_Rejects_Duplicate_Nct_Ids(string mutation)
    {
        var root = ValidNode();
        root.AsObject().Remove("nextPageToken");
        root["totalCount"] = 2;
        var duplicate = Study(root).DeepClone();
        if (mutation == "conflicting")
        {
            duplicate["protocolSection"]!["identificationModule"]![
                "briefTitle"] = "Conflicting duplicate title";
        }
        root["studies"]!.AsArray().Add(duplicate);
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));

        Assert.Equal("duplicate-nct-id", exception.Code);
    }

    [Theory]
    [InlineData("missing-total")]
    [InlineData("total-smaller")]
    [InlineData("empty-nonzero")]
    [InlineData("empty-token")]
    [InlineData("token-without-more")]
    [InlineData("missing-token-with-more")]
    [InlineData("underfilled-first-page")]
    public async Task Acquire_Rejects_Contradictory_First_Page_Metadata(
        string mutation)
    {
        var root = ValidNode();
        switch (mutation)
        {
            case "missing-total":
                root.AsObject().Remove("totalCount");
                break;
            case "total-smaller":
                root["totalCount"] = 0;
                break;
            case "empty-nonzero":
                root["studies"] = new JsonArray();
                root["totalCount"] = 1;
                root.AsObject().Remove("nextPageToken");
                break;
            case "empty-token":
                root["studies"] = new JsonArray();
                root["totalCount"] = 0;
                root["nextPageToken"] = "SYNTHETIC-UNEXPECTED-TOKEN";
                break;
            case "token-without-more":
                root["totalCount"] = 1;
                root["nextPageToken"] = "SYNTHETIC-UNEXPECTED-TOKEN";
                break;
            case "missing-token-with-more":
                root.AsObject().Remove("nextPageToken");
                root["totalCount"] = 2;
                break;
            case "underfilled-first-page":
                root["totalCount"] = 2;
                root["nextPageToken"] = "SYNTHETIC-NEXT-PAGE-TOKEN";
                break;
        }
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
    }

    [Fact]
    public async Task Acquire_First_Page_Without_More_Results_Is_Not_Truncated()
    {
        var root = ValidNode();
        root.AsObject().Remove("nextPageToken");
        root["totalCount"] = 1;
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Retatrutide"]),
            RetrievedAt);

        Assert.False(batch.Truncated);
        Assert.Single(batch.Candidates);
    }

    [Theory]
    [InlineData("2026")]
    [InlineData("2026-07")]
    [InlineData("2026-07-20T00:00")]
    [InlineData("2026-7-2")]
    public async Task Acquire_Requires_Exact_LastUpdateSubmitDate(string value)
    {
        var root = ValidNode();
        StatusModule(root)["lastUpdateSubmitDate"] = value;
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
    }

    [Theory]
    [InlineData("ACTIVE_NOT_RECRUITING")]
    [InlineData("COMPLETED")]
    [InlineData("ENROLLING_BY_INVITATION")]
    [InlineData("NOT_YET_RECRUITING")]
    [InlineData("RECRUITING")]
    [InlineData("SUSPENDED")]
    [InlineData("TERMINATED")]
    [InlineData("WITHDRAWN")]
    [InlineData("AVAILABLE")]
    [InlineData("NO_LONGER_AVAILABLE")]
    [InlineData("TEMPORARILY_NOT_AVAILABLE")]
    [InlineData("APPROVED_FOR_MARKETING")]
    [InlineData("WITHHELD")]
    [InlineData("UNKNOWN")]
    public async Task Acquire_Accepts_Current_Official_Overall_Status(
        string overallStatus)
    {
        var root = ValidNode();
        StatusModule(root)["overallStatus"] = overallStatus;
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var candidate = Assert.Single(
            (await CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt)).Candidates);

        Assert.Equal(
            [
                overallStatus == "UNKNOWN"
                    ? "ClinicalTrials.gov official OverallStatus enum value: UNKNOWN"
                    : overallStatus,
            ],
            candidate.SourceSpecificProvenance["overallStatus"].Values);
    }

    [Fact]
    public async Task Acquire_Rejects_Unknown_Overall_Status()
    {
        var root = ValidNode();
        StatusModule(root)["overallStatus"] = "UNKNOWN_STATUS";
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));

        Assert.Equal("overall-status-invalid", exception.Code);
    }

    [Theory]
    [InlineData("studyFirstPostDateStruct", "2026")]
    [InlineData("studyFirstPostDateStruct", "2026-07")]
    [InlineData("studyFirstPostDateStruct", "2026-07-20T00:00")]
    [InlineData("studyFirstPostDateStruct", "2026-02-30")]
    [InlineData("lastUpdatePostDateStruct", "2026")]
    [InlineData("lastUpdatePostDateStruct", "2026-07")]
    [InlineData("lastUpdatePostDateStruct", "2026-07-20T00:00")]
    [InlineData("lastUpdatePostDateStruct", "2026-02-30")]
    public async Task Acquire_Requires_Exact_Posted_Dates(
        string propertyName,
        string value)
    {
        var root = ValidNode();
        StatusModule(root)[propertyName]!["date"] = value;
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
    }

    [Theory]
    [InlineData(429, SourceAcquisitionBatchStatus.RateLimited)]
    [InlineData(503, SourceAcquisitionBatchStatus.BackPressure)]
    public async Task Acquire_Surfaces_Backoff_Without_Retry(
        int statusCode,
        SourceAcquisitionBatchStatus expectedStatus)
    {
        var handler = new RecordingHandler((_, _) =>
        {
            var response = new HttpResponseMessage((HttpStatusCode)statusCode);
            response.Headers.RetryAfter =
                new RetryConditionHeaderValue(TimeSpan.FromSeconds(30));
            return Task.FromResult(response);
        });

        var batch = await CreateAdapter(handler).AcquireAsync(
            ReadyIntent(["Retatrutide"]),
            RetrievedAt);

        Assert.Equal(expectedStatus, batch.Status);
        Assert.Equal("30", batch.RetryAfter);
        Assert.Empty(batch.Candidates);
        Assert.Single(handler.Requests);
    }

    [Theory]
    [InlineData(202, "http-202")]
    [InlineData(400, "http-400")]
    [InlineData(404, "http-404")]
    public async Task Acquire_Rejects_Explicit_Nonterminal_Or_Error_Status(
        int statusCode,
        string expectedCode)
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage((HttpStatusCode)statusCode)));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));

        Assert.Equal(expectedCode, exception.Code);
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task Acquire_Rejects_Redirect_NonJson_And_Malformed_Json()
    {
        var redirectHandler = new RecordingHandler((_, _) =>
        {
            var response = new HttpResponseMessage(HttpStatusCode.Redirect);
            response.Headers.Location = new Uri("https://example.invalid/");
            return Task.FromResult(response);
        });
        var nonJsonHandler = new RecordingHandler(
            (_, _) => Task.FromResult(
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(
                        "not json",
                        Encoding.UTF8,
                        "text/plain"),
                }));
        var malformedHandler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse("{")));

        var redirect = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(redirectHandler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
        var nonJson = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(nonJsonHandler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
        var malformed = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(malformedHandler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));

        Assert.Equal("redirect-response", redirect.Code);
        Assert.Equal("unexpected-content-type", nonJson.Code);
        Assert.Equal("malformed-json", malformed.Code);
    }

    [Theory]
    [InlineData("nctId")]
    [InlineData("overallStatus")]
    [InlineData("lastUpdateSubmitDate")]
    public async Task Acquire_Rejects_Missing_Hard_Provenance(string field)
    {
        var root = ValidNode();
        var module = field switch
        {
            "nctId" => IdentificationModule(root),
            _ => StatusModule(root),
        };
        module.Remove(field);
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));

        var exception = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(handler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));

        Assert.Equal("required-field-missing", exception.Code);
    }

    [Fact]
    public async Task Acquire_Rejects_Too_Many_Studies_And_Oversized_Response()
    {
        var root = ValidNode();
        var study = root["studies"]![0]!.DeepClone();
        var studies = root["studies"]!.AsArray();
        for (var index = studies.Count; index < 51; index++)
        {
            studies.Add(study.DeepClone());
        }
        var tooManyHandler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(root.ToJsonString())));
        var oversizeHandler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));

        var tooMany = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(tooManyHandler).AcquireAsync(
                ReadyIntent(["Retatrutide"]),
                RetrievedAt));
        var oversized = await Assert.ThrowsAsync<SourceAcquisitionException>(
            () => CreateAdapter(
                    oversizeHandler,
                    maximumResponseBytes: 10)
                .AcquireAsync(
                    ReadyIntent(["Retatrutide"]),
                    RetrievedAt));

        Assert.Equal("study-count-exceeded", tooMany.Code);
        Assert.Equal("response-too-large", oversized.Code);
    }

    [Fact]
    public async Task Acquire_Waits_For_Serialized_Request_Gate()
    {
        var handler = new RecordingHandler(
            (_, _) => Task.FromResult(JsonResponse(ValidBody())));
        var gate = new ControlledRequestGate();
        var adapter = new ClinicalTrialsGovV2AcquisitionAdapter(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            ClinicalTrialsGovV2AcquisitionAdapter.DefaultMaximumResponseBytes,
            gate);

        var acquisition = adapter.AcquireAsync(
            ReadyIntent(["Retatrutide"]),
            RetrievedAt);
        await gate.WaitUntilEnteredAsync();

        Assert.Empty(handler.Requests);
        Assert.False(acquisition.IsCompleted);
        gate.Release();
        await acquisition;

        Assert.Single(handler.Requests);
        Assert.Equal(1, gate.AcquisitionCount);
    }

    private static ClinicalTrialsGovV2AcquisitionAdapter CreateAdapter(
        HttpMessageHandler handler,
        int maximumResponseBytes =
            ClinicalTrialsGovV2AcquisitionAdapter.DefaultMaximumResponseBytes)
        => new(
            new HttpClient(handler, disposeHandler: true),
            RegistrySha256,
            maximumResponseBytes,
            new SerializedSourceRequestGate(
                TimeProvider.System,
                [],
                dailyBudget: null));

    private static SourceAcquisitionIntent ReadyIntent(
        IReadOnlyList<string> searchTerms)
        => new(
            SourceId: "clinicaltrials",
            AdapterId:
                ClinicalTrialsGovV2AcquisitionAdapter.PlanningAdapterId,
            RequestId: "market-retatrutide-001",
            CompoundName: "Retatrutide",
            SearchTerms: searchTerms,
            CandidateMethod: "api",
            AuthorizedFieldUses:
            [
                "identity",
                "efficacy-claims",
                "interactions",
            ],
            RequiredProvenanceFields:
            [
                "sourceRegistryId",
                "sourceItemId",
                "sourceUrl",
                "nctId",
                "overallStatus",
                "phase",
                "lastUpdateSubmitDate",
                "retrievedAtUtc",
                "rightsReviewStatusAtRetrieval",
                "transformationPipelineVersion",
                "humanReviewStatus",
            ],
            RegistrySchemaVersion: "2.0.0",
            RegistryBindingSha256: RegistrySha256,
            Disposition: SourceAcquisitionDisposition.Ready,
            BlockingReasons: []);

    private static string ValidBody()
        => File.ReadAllText(
            TestPaths.FixturePath(
                "clinicaltrials-v2-studies.synthetic.json"));

    private static IReadOnlyDictionary<string, string> ParseQuery(Uri uri)
    {
        var values = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var segment in uri.Query.TrimStart('?').Split('&'))
        {
            var parts = segment.Split('=', 2);
            Assert.Equal(2, parts.Length);
            var key = Uri.UnescapeDataString(parts[0]);
            var value = Uri.UnescapeDataString(parts[1]);
            Assert.True(values.TryAdd(key, value), $"Duplicate query key: {key}");
        }
        return values;
    }

    private static JsonNode ValidNode() => JsonNode.Parse(ValidBody())!;

    private static JsonObject Study(JsonNode root)
        => root["studies"]![0]!.AsObject();

    private static JsonObject Protocol(JsonNode root)
        => Study(root)["protocolSection"]!.AsObject();

    private static JsonObject IdentificationModule(JsonNode root)
        => Protocol(root)["identificationModule"]!.AsObject();

    private static JsonObject StatusModule(JsonNode root)
        => Protocol(root)["statusModule"]!.AsObject();

    private static JsonObject DesignModule(JsonNode root)
        => Protocol(root)["designModule"]!.AsObject();

    private static JsonArray PhaseArray(JsonNode root)
        => DesignModule(root)["phases"]!.AsArray();

    private static HttpResponseMessage JsonResponse(string body)
        => new(HttpStatusCode.OK)
        {
            Content = new StringContent(
                body,
                Encoding.UTF8,
                "application/json"),
        };

    private sealed record RecordedRequest(HttpMethod Method, Uri Uri);

    private sealed class RecordingHandler(
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>>
            responseFactory)
        : HttpMessageHandler
    {
        public List<RecordedRequest> Requests { get; } = [];

        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            Requests.Add(
                new RecordedRequest(
                    request.Method,
                    request.RequestUri
                    ?? throw new InvalidOperationException(
                        "Request URI is required.")));
            return responseFactory(request, cancellationToken);
        }
    }

    private sealed class ControlledRequestGate : ISourceRequestGate
    {
        private readonly TaskCompletionSource _entered =
            new(TaskCreationOptions.RunContinuationsAsynchronously);
        private readonly TaskCompletionSource _release =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int AcquisitionCount { get; private set; }

        public async ValueTask<IDisposable> AcquireAsync(
            CancellationToken cancellationToken)
        {
            AcquisitionCount++;
            _entered.TrySetResult();
            await _release.Task.WaitAsync(cancellationToken);
            return new NoopLease();
        }

        public Task WaitUntilEnteredAsync() => _entered.Task;

        public void Release() => _release.TrySetResult();

        private sealed class NoopLease : IDisposable
        {
            public void Dispose()
            {
            }
        }
    }
}
