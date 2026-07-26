namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public sealed class ClinicalTrialsGovV2AcquisitionAdapter : ISourceAcquisitionAdapter
{
    public const string TransformationVersion =
        "clinicaltrials-gov-v2-study-metadata-v1";
    public const string PlanningAdapterId = "clinicaltrials-planning-v1";
    public const string FixedEndpoint =
        "https://clinicaltrials.gov/api/v2/studies";
    public const int ResultLimit = 50;
    public const int DefaultMaximumResponseBytes = 2 * 1024 * 1024;

    private const string ExplicitFields =
        "NCTId,BriefTitle,OfficialTitle,OrgFullName,OverallStatus,"
        + "LastUpdateSubmitDate,StudyFirstPostDate,LastUpdatePostDate,"
        + "StudyType,Phase,Condition,LeadSponsorName,LeadSponsorClass,"
        + "InterventionName,InterventionType,InterventionOtherName,"
        + "PrimaryOutcomeMeasure,PrimaryOutcomeTimeFrame,"
        + "SecondaryOutcomeMeasure,SecondaryOutcomeTimeFrame";

    private static readonly string[] RequiredProvenanceFields =
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
    ];

    private static readonly HashSet<string> SupportedFieldUses =
        new(
            ["identity", "efficacy-claims", "interactions"],
            StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> PhaseNotApplicable =
        new(["phase"], StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> OfficialOverallStatuses =
        new(
            [
                "ACTIVE_NOT_RECRUITING",
                "COMPLETED",
                "ENROLLING_BY_INVITATION",
                "NOT_YET_RECRUITING",
                "RECRUITING",
                "SUSPENDED",
                "TERMINATED",
                "WITHDRAWN",
                "AVAILABLE",
                "NO_LONGER_AVAILABLE",
                "TEMPORARILY_NOT_AVAILABLE",
                "APPROVED_FOR_MARKETING",
                "WITHHELD",
                "UNKNOWN",
            ],
            StringComparer.Ordinal);

    private static readonly string[] EvidenceLimitations =
    [
        "ClinicalTrials.gov records are submitted by study sponsors or responsible parties; registration, recruitment status, and posting do not establish efficacy, safety, completion, or government endorsement.",
        "Registered outcome measures and time frames describe planned study design, not measured results, statistical findings, or clinical conclusions.",
        "Interventions co-listed in one registered study are design context only and do not establish an interaction, combination safety, or effectiveness.",
    ];

    private static readonly SerializedSourceRequestGate SharedRequestGate =
        new(TimeProvider.System, [], dailyBudget: null);

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly int _maximumResponseBytes;
    private readonly ISourceRequestGate _requestGate;
    private readonly SourceAcquisitionIntentRequirements _intentRequirements;

    public ClinicalTrialsGovV2AcquisitionAdapter(
        string expectedRegistrySha256,
        int maximumResponseBytes = DefaultMaximumResponseBytes)
        : this(
            SourceAcquisitionHttpTransport.CreateRedirectDisabledAnonymousClient(
                TimeSpan.FromSeconds(20)),
            expectedRegistrySha256,
            maximumResponseBytes,
            SharedRequestGate)
    {
    }

    internal ClinicalTrialsGovV2AcquisitionAdapter(
        HttpClient httpClient,
        string expectedRegistrySha256,
        int maximumResponseBytes,
        ISourceRequestGate requestGate)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _expectedRegistrySha256 =
            SourceAcquisitionIntentGuard.RequireLowercaseSha256(
                expectedRegistrySha256,
                nameof(expectedRegistrySha256));
        _maximumResponseBytes = maximumResponseBytes > 0
            ? maximumResponseBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        _requestGate = requestGate ?? throw new ArgumentNullException(nameof(requestGate));
        _intentRequirements = new SourceAcquisitionIntentRequirements(
            SourceId: "clinicaltrials",
            SourceDisplayName: "ClinicalTrials.gov",
            PlanningAdapterId,
            CandidateMethod: "api",
            ExpectedRegistrySha256: _expectedRegistrySha256,
            RequiredProvenanceFields);
    }

    public string SourceId => "clinicaltrials";
    public string AdapterId => TransformationVersion;

    public async Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            _intentRequirements);
        var authorizedUses = intent.AuthorizedFieldUses
            .Where(SupportedFieldUses.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (authorizedUses.Count == 0)
        {
            throw new SourceAcquisitionException(
                "authorized-field-use-empty",
                "ClinicalTrials.gov acquisition requires an authorized identity, efficacy-claims, or interactions field use.");
        }

        var searchTerms = ValidateSearchTerms(intent.SearchTerms);
        var requestUri = BuildRequestUri(searchTerms);
        using var requestLease = await _requestGate.AcquireAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return EmptyBatch(
                SourceAcquisitionBatchStatus.RateLimited,
                response.Headers.RetryAfter?.ToString());
        }
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return EmptyBatch(
                SourceAcquisitionBatchStatus.BackPressure,
                response.Headers.RetryAfter?.ToString());
        }
        if ((int)response.StatusCode is >= 300 and <= 399)
        {
            throw new SourceAcquisitionException(
                "redirect-response",
                "ClinicalTrials.gov redirects are not accepted by this adapter.");
        }
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            throw new SourceAcquisitionException(
                "http-202",
                "ClinicalTrials.gov asynchronous responses are not accepted by this adapter.");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"ClinicalTrials.gov returned HTTP {(int)response.StatusCode}.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(
                mediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                "ClinicalTrials.gov response content type must be application/json.");
        }

        var body = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            response.Content,
            _maximumResponseBytes,
            "ClinicalTrials.gov",
            cancellationToken);
        return ParseResponse(
            body,
            intent,
            authorizedUses,
            searchTerms,
            requestUri,
            retrievedAtUtc);
    }

    private static SourceAcquisitionBatch EmptyBatch(
        SourceAcquisitionBatchStatus status,
        string? retryAfter)
        => new(
            status,
            Array.Empty<SourceAcquisitionCandidate>(),
            Truncated: false,
            RetryAfter: retryAfter);

    private static IReadOnlyList<string> ValidateSearchTerms(
        IReadOnlyList<string> rawTerms)
    {
        if (rawTerms is null)
        {
            throw new SourceAcquisitionException(
                "invalid-search-term-count",
                "ClinicalTrials.gov acquisition requires search terms.");
        }

        var terms = rawTerms
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (terms.Count is < 1 or > 20)
        {
            throw new SourceAcquisitionException(
                "invalid-search-term-count",
                "ClinicalTrials.gov acquisition requires between 1 and 20 distinct search terms.");
        }
        if (terms.Any(term =>
                term.Length > 128
                || term.Any(character => !IsSafeSearchCharacter(character))))
        {
            throw new SourceAcquisitionException(
                "invalid-search-term",
                "ClinicalTrials.gov search terms contain unsupported query syntax or characters.");
        }
        return terms;
    }

    private static bool IsSafeSearchCharacter(char character)
        => char.IsLetterOrDigit(character)
           || character == ' '
           || character is '-' or '_' or '.' or '+' or '/' or '\'';

    private static Uri BuildRequestUri(IReadOnlyList<string> terms)
    {
        var clauses = terms.Select(
            term => $"AREA[InterventionName]\"{term}\"");
        var interventionQuery = $"({string.Join(" OR ", clauses)})";
        var url =
            $"{FixedEndpoint}?format=json"
            + $"&query.intr={Uri.EscapeDataString(interventionQuery)}"
            + $"&fields={Uri.EscapeDataString(ExplicitFields)}"
            + $"&pageSize={ResultLimit}"
            + "&countTotal=true";
        if (url.Length > 4096)
        {
            throw new SourceAcquisitionException(
                "request-uri-too-long",
                "ClinicalTrials.gov request URI exceeds the adapter limit.");
        }
        return new Uri(url, UriKind.Absolute);
    }

    private static SourceAcquisitionBatch ParseResponse(
        byte[] body,
        SourceAcquisitionIntent intent,
        IReadOnlyList<string> authorizedUses,
        IReadOnlyList<string> searchTerms,
        Uri requestUri,
        DateTimeOffset retrievedAtUtc)
    {
        JsonDocument document;
        try
        {
            document = JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new SourceAcquisitionException(
                "malformed-json",
                $"ClinicalTrials.gov returned malformed JSON: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("studies", out var studies)
                || studies.ValueKind != JsonValueKind.Array)
            {
                throw new SourceAcquisitionException(
                    "studies-missing",
                    "ClinicalTrials.gov response did not contain a studies array.");
            }
            var returnedCount = studies.GetArrayLength();
            if (returnedCount > ResultLimit)
            {
                throw new SourceAcquisitionException(
                    "study-count-exceeded",
                    "ClinicalTrials.gov response exceeded the requested study limit.");
            }

            var totalCount = ReadRequiredTotalCount(root);
            var nextPageToken = ReadOptionalRootString(root, "nextPageToken");
            var hasToken = nextPageToken.Length > 0;
            if (totalCount < returnedCount)
            {
                throw new SourceAcquisitionException(
                    "total-count-invalid",
                    "ClinicalTrials.gov totalCount was smaller than the returned study count.");
            }
            if (returnedCount == 0)
            {
                if (totalCount != 0 || hasToken)
                {
                    throw new SourceAcquisitionException(
                        "first-page-correlation-invalid",
                        "An empty first page requires totalCount zero and no nextPageToken.");
                }
                return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch, null);
            }

            var hasMoreByCount = totalCount > returnedCount;
            if (hasToken != hasMoreByCount)
            {
                throw new SourceAcquisitionException(
                    "first-page-correlation-invalid",
                    "ClinicalTrials.gov nextPageToken must be present exactly when totalCount exceeds the returned first page.");
            }
            if (returnedCount != Math.Min(totalCount, ResultLimit))
            {
                throw new SourceAcquisitionException(
                    "first-page-correlation-invalid",
                    "ClinicalTrials.gov first-page study count must equal min(totalCount, pageSize).");
            }

            var candidates = new List<SourceAcquisitionCandidate>();
            var seenNctIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var study in studies.EnumerateArray())
            {
                if (study.ValueKind != JsonValueKind.Object)
                {
                    throw new SourceAcquisitionException(
                        "study-invalid",
                        "ClinicalTrials.gov returned a non-object study.");
                }
                var protocol = ReadRequiredObject(study, "protocolSection");
                var identification =
                    ReadRequiredObject(protocol, "identificationModule");
                var nctId = ReadRequiredString(identification, "nctId");
                if (!IsNctId(nctId))
                {
                    throw new SourceAcquisitionException(
                        "nct-id-invalid",
                        "ClinicalTrials.gov returned an invalid NCT identifier.");
                }
                if (!seenNctIds.Add(nctId))
                {
                    throw new SourceAcquisitionException(
                        "duplicate-nct-id",
                        $"ClinicalTrials.gov returned duplicate NCT identifier {nctId}.");
                }

                var interventions = ReadInterventions(protocol);
                if (!HasExactInterventionMatch(interventions, searchTerms))
                {
                    continue;
                }
                var candidate = ParseStudy(
                    protocol,
                    interventions,
                    intent,
                    authorizedUses,
                    searchTerms,
                    requestUri,
                    retrievedAtUtc);
                candidates.Add(candidate);
            }

            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.Completed,
                candidates,
                Truncated: hasMoreByCount || hasToken,
                RetryAfter: null);
        }
    }

    private static SourceAcquisitionCandidate ParseStudy(
        JsonElement protocol,
        IReadOnlyList<RegisteredIntervention> interventions,
        SourceAcquisitionIntent intent,
        IReadOnlyList<string> authorizedUses,
        IReadOnlyList<string> searchTerms,
        Uri requestUri,
        DateTimeOffset retrievedAtUtc)
    {
        var identification = ReadRequiredObject(protocol, "identificationModule");
        var status = ReadRequiredObject(protocol, "statusModule");
        var design = ReadRequiredObject(protocol, "designModule");

        var nctId = ReadRequiredString(identification, "nctId");
        if (!IsNctId(nctId))
        {
            throw new SourceAcquisitionException(
                "nct-id-invalid",
                "ClinicalTrials.gov returned an invalid NCT identifier.");
        }
        var overallStatus = ReadRequiredString(status, "overallStatus");
        if (!OfficialOverallStatuses.Contains(overallStatus))
        {
            throw new SourceAcquisitionException(
                "overall-status-invalid",
                "ClinicalTrials.gov returned an unsupported OverallStatus.");
        }
        var lastUpdateSubmitDate =
            ReadRequiredIsoDate(status, "lastUpdateSubmitDate");
        var phases = ReadRequiredStringArray(design, "phases");
        var phaseProvenance = BuildPhaseProvenance(phases);
        var sourceUrl = $"https://clinicaltrials.gov/study/{nctId}";

        var fields = BuildFields(
            protocol,
            identification,
            design,
            authorizedUses,
            searchTerms,
            nctId,
            phases,
            interventions);
        if (fields.Count == 0)
        {
            throw new SourceAcquisitionException(
                "candidate-fields-empty",
                "ClinicalTrials.gov study did not contain an authorized candidate field.");
        }

        var sponsorModule = ReadOptionalObject(
            protocol,
            "sponsorCollaboratorsModule");
        var leadSponsor = sponsorModule is null
            ? null
            : ReadOptionalObject(sponsorModule.Value, "leadSponsor");
        var leadSponsorName = leadSponsor is null
            ? string.Empty
            : ReadOptionalString(leadSponsor.Value, "name");
        var leadSponsorClass = leadSponsor is null
            ? string.Empty
            : ReadOptionalString(leadSponsor.Value, "class");
        var firstPostDate = ReadOptionalDateStruct(
            status,
            "studyFirstPostDateStruct");
        var lastUpdatePostDate = ReadOptionalDateStruct(
            status,
            "lastUpdatePostDateStruct");

        var provenance =
            new Dictionary<string, SourceProvenanceValue>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["nctId"] = SourceProvenanceValue.Present(nctId),
                ["overallStatus"] =
                    SourceProvenanceValue.Present(
                        overallStatus == "UNKNOWN"
                            ? "ClinicalTrials.gov official OverallStatus enum value: UNKNOWN"
                            : overallStatus),
                ["phase"] = phaseProvenance,
                ["lastUpdateSubmitDate"] =
                    SourceProvenanceValue.Present(lastUpdateSubmitDate),
                ["apiVersion"] = SourceProvenanceValue.Present("v2"),
            };
        AddOptionalProvenance(
            provenance,
            "studyFirstPostDate",
            firstPostDate);
        AddOptionalProvenance(
            provenance,
            "lastUpdatePostDate",
            lastUpdatePostDate);
        AddOptionalProvenance(
            provenance,
            "leadSponsorName",
            leadSponsorName);
        AddOptionalProvenance(
            provenance,
            "leadSponsorClass",
            leadSponsorClass);

        var title = ReadOptionalString(identification, "officialTitle");
        if (title.Length == 0)
        {
            title = ReadOptionalString(identification, "briefTitle");
        }
        if (title.Length == 0) title = $"ClinicalTrials.gov study {nctId}";

        var candidate = new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            SourceRegistryId: "clinicaltrials",
            SourceItemId: nctId,
            SourceUrl: sourceUrl,
            QueryUrl: requestUri.AbsoluteUri,
            SourcePublicationOrUpdateDate: lastUpdateSubmitDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: fields)
        {
            AuthorizedFieldUses = authorizedUses,
            SourceSpecificProvenance = provenance,
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope:
                        "Public study registration metadata submitted to ClinicalTrials.gov.",
                    Provider:
                        "ClinicalTrials.gov / study sponsor or responsible party",
                    SourceUrl: sourceUrl,
                    TermsUrl:
                        "https://clinicaltrials.gov/about-site/terms-conditions",
                    RightsStatus: "reviewed",
                    CoveredFields: fields.Keys
                        .OrderBy(field => field, StringComparer.Ordinal)
                        .ToList()),
            ],
            DocumentProvenance =
            [
                new SourceDocumentProvenance(
                    Title: title,
                    Section: "Protocol registration metadata",
                    PublishedDate: firstPostDate,
                    UpdatedDate: lastUpdatePostDate.Length > 0
                        ? lastUpdatePostDate
                        : lastUpdateSubmitDate),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    $"Source: ClinicalTrials.gov study {nctId}; record content was submitted by the study sponsor or responsible party, and listing does not imply NIH or NLM endorsement.",
                ExcludedContentClasses:
                [
                    "results measurements and statistical analyses",
                    "outcome conclusions",
                    "linked publications and uploaded documents",
                    "personal contact details",
                ],
                NonEndorsementRequired: true),
        };

        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            RequiredProvenanceFields,
            expectedSourceRegistryId: "clinicaltrials",
            expectedRegistrySha256: intent.RegistryBindingSha256,
            allowedNotApplicableFields: PhaseNotApplicable);
        return candidate;
    }

    private static Dictionary<string, IReadOnlyList<string>> BuildFields(
        JsonElement protocol,
        JsonElement identification,
        JsonElement design,
        IReadOnlyList<string> authorizedUses,
        IReadOnlyList<string> searchTerms,
        string nctId,
        IReadOnlyList<string> phases,
        IReadOnlyList<RegisteredIntervention> interventions)
    {
        var fields = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase);
        if (authorizedUses.Contains("identity", StringComparer.OrdinalIgnoreCase))
        {
            fields["nct_id"] = [nctId];
            AddOptionalScalar(fields, identification, "briefTitle", "brief_title");
            AddOptionalScalar(
                fields,
                identification,
                "officialTitle",
                "official_title");
            AddOptionalScalar(fields, design, "studyType", "study_type");

            var organization = ReadOptionalObject(
                identification,
                "organization");
            if (organization is not null)
            {
                AddOptionalScalar(
                    fields,
                    organization.Value,
                    "fullName",
                    "organization_full_name");
            }
            var conditionsModule = ReadOptionalObject(
                protocol,
                "conditionsModule");
            if (conditionsModule is not null)
            {
                AddOptionalArray(
                    fields,
                    conditionsModule.Value,
                    "conditions",
                    "conditions");
            }

            var sponsorModule = ReadOptionalObject(
                protocol,
                "sponsorCollaboratorsModule");
            var leadSponsor = sponsorModule is null
                ? null
                : ReadOptionalObject(sponsorModule.Value, "leadSponsor");
            if (leadSponsor is not null)
            {
                AddOptionalScalar(
                    fields,
                    leadSponsor.Value,
                    "name",
                    "lead_sponsor_name");
                AddOptionalScalar(
                    fields,
                    leadSponsor.Value,
                    "class",
                    "lead_sponsor_class");
            }
        }

        if (authorizedUses.Contains("identity", StringComparer.OrdinalIgnoreCase))
        {
            AddValues(
                fields,
                "intervention_names",
                interventions.Select(intervention => intervention.Name));
            AddValues(
                fields,
                "intervention_types",
                interventions.Select(intervention => intervention.Type));
            AddValues(
                fields,
                "intervention_other_names",
                interventions.SelectMany(intervention => intervention.OtherNames));
        }

        if (authorizedUses.Contains(
                "efficacy-claims",
                StringComparer.OrdinalIgnoreCase))
        {
            fields["registered_phase"] = phases
                .Select(phase => string.Equals(
                    phase,
                    "NA",
                    StringComparison.OrdinalIgnoreCase)
                    ? "Official registry value: NA (not applicable)"
                    : phase)
                .ToList();
            var outcomesModule = ReadOptionalObject(protocol, "outcomesModule");
            if (outcomesModule is not null)
            {
                AddOutcomeDesignContext(
                    fields,
                    outcomesModule.Value,
                    "primaryOutcomes",
                    "registered_primary_outcomes");
                AddOutcomeDesignContext(
                    fields,
                    outcomesModule.Value,
                    "secondaryOutcomes",
                    "registered_secondary_outcomes");
            }
        }

        if (authorizedUses.Contains(
                "interactions",
                StringComparer.OrdinalIgnoreCase))
        {
            var searchTermSet = searchTerms.ToHashSet(
                StringComparer.OrdinalIgnoreCase);
            var matchedInterventions = interventions
                .Where(intervention =>
                    searchTermSet.Contains(intervention.Name)
                    || intervention.OtherNames.Any(searchTermSet.Contains))
                .ToHashSet();
            if (matchedInterventions.Count > 0)
            {
                var coListed = interventions
                    .Where(intervention =>
                        !matchedInterventions.Contains(intervention))
                    .Select(intervention =>
                        $"Co-listed intervention in registered study design: {intervention.Name} ({intervention.Type})");
                AddValues(
                    fields,
                    "co_listed_interventions_design_context",
                    coListed);
            }
        }

        return fields;
    }

    private static bool HasExactInterventionMatch(
        IReadOnlyList<RegisteredIntervention> interventions,
        IReadOnlyList<string> searchTerms)
    {
        var searchTermSet = searchTerms.ToHashSet(
            StringComparer.OrdinalIgnoreCase);
        return interventions.Any(intervention =>
            searchTermSet.Contains(intervention.Name)
            || intervention.OtherNames.Any(searchTermSet.Contains));
    }

    private static IReadOnlyList<RegisteredIntervention> ReadInterventions(
        JsonElement protocol)
    {
        var module = ReadOptionalObject(protocol, "armsInterventionsModule");
        if (module is null
            || !module.Value.TryGetProperty("interventions", out var array))
        {
            return Array.Empty<RegisteredIntervention>();
        }
        if (array.ValueKind != JsonValueKind.Array)
        {
            throw InvalidField("interventions");
        }

        var values = new List<RegisteredIntervention>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw InvalidField("interventions");
            }
            values.Add(new RegisteredIntervention(
                Name: ReadRequiredString(item, "name"),
                Type: ReadRequiredString(item, "type"),
                OtherNames: ReadOptionalStringArray(item, "otherNames")));
        }
        return values;
    }

    private static void AddOutcomeDesignContext(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement outcomesModule,
        string propertyName,
        string outputName)
    {
        if (!outcomesModule.TryGetProperty(propertyName, out var array))
        {
            return;
        }
        if (array.ValueKind != JsonValueKind.Array)
        {
            throw InvalidField(propertyName);
        }

        var values = new List<string>();
        foreach (var item in array.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw InvalidField(propertyName);
            }
            var measure = ReadRequiredString(item, "measure");
            var timeFrame = ReadRequiredString(item, "timeFrame");
            values.Add($"{measure} | Time frame: {timeFrame}");
        }
        AddValues(fields, outputName, values);
    }

    private static SourceProvenanceValue BuildPhaseProvenance(
        IReadOnlyList<string> phases)
    {
        if (phases.Count == 1
            && string.Equals(phases[0], "NA", StringComparison.Ordinal))
        {
            return SourceProvenanceValue.NotApplicable(
                "ClinicalTrials.gov explicitly reported the official phase value NA.");
        }
        if (phases.Any(phase =>
                string.Equals(phase, "NA", StringComparison.OrdinalIgnoreCase)))
        {
            throw new SourceAcquisitionException(
                "phase-invalid",
                "ClinicalTrials.gov phase NA cannot be combined with another phase.");
        }
        var supportedPhases = new HashSet<string>(
            [
                "EARLY_PHASE1",
                "PHASE1",
                "PHASE2",
                "PHASE3",
                "PHASE4",
            ],
            StringComparer.Ordinal);
        if (phases.Any(phase => !supportedPhases.Contains(phase)))
        {
            throw new SourceAcquisitionException(
                "phase-invalid",
                "ClinicalTrials.gov returned an unsupported phase value.");
        }
        return SourceProvenanceValue.Present(phases.ToArray());
    }

    private static void AddOptionalProvenance(
        IDictionary<string, SourceProvenanceValue> provenance,
        string name,
        string value)
    {
        if (value.Length > 0)
        {
            provenance[name] = SourceProvenanceValue.Present(value);
        }
    }

    private static void AddOptionalScalar(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement parent,
        string propertyName,
        string outputName)
    {
        var value = ReadOptionalString(parent, propertyName);
        if (value.Length > 0) fields[outputName] = [value];
    }

    private static void AddOptionalArray(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement parent,
        string propertyName,
        string outputName)
        => AddValues(
            fields,
            outputName,
            ReadOptionalStringArray(parent, propertyName));

    private static void AddValues(
        IDictionary<string, IReadOnlyList<string>> fields,
        string outputName,
        IEnumerable<string> values)
    {
        var normalized = values
            .Select(value => value.Trim())
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (normalized.Count > 0) fields[outputName] = normalized;
    }

    private static JsonElement ReadRequiredObject(
        JsonElement parent,
        string propertyName)
        => ReadOptionalObject(parent, propertyName)
           ?? throw new SourceAcquisitionException(
               "required-field-missing",
               $"ClinicalTrials.gov study is missing required object '{propertyName}'.");

    private static JsonElement? ReadOptionalObject(
        JsonElement parent,
        string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return null;
        }
        if (property.ValueKind != JsonValueKind.Object)
        {
            throw InvalidField(propertyName);
        }
        return property;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName)
    {
        var value = ReadOptionalString(parent, propertyName);
        if (value.Length == 0)
        {
            throw new SourceAcquisitionException(
                "required-field-missing",
                $"ClinicalTrials.gov study is missing required field '{propertyName}'.");
        }
        return value;
    }

    private static string ReadOptionalString(
        JsonElement parent,
        string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return string.Empty;
        }
        if (property.ValueKind != JsonValueKind.String)
        {
            throw InvalidField(propertyName);
        }
        return property.GetString()?.Trim() ?? string.Empty;
    }

    private static IReadOnlyList<string> ReadRequiredStringArray(
        JsonElement parent,
        string propertyName)
    {
        var values = ReadOptionalStringArray(parent, propertyName);
        if (values.Count == 0)
        {
            throw new SourceAcquisitionException(
                "required-field-missing",
                $"ClinicalTrials.gov study is missing required field '{propertyName}'.");
        }
        return values;
    }

    private static IReadOnlyList<string> ReadOptionalStringArray(
        JsonElement parent,
        string propertyName)
    {
        if (!parent.TryGetProperty(propertyName, out var property))
        {
            return Array.Empty<string>();
        }
        if (property.ValueKind != JsonValueKind.Array)
        {
            throw InvalidField(propertyName);
        }
        var values = new List<string>();
        foreach (var item in property.EnumerateArray())
        {
            if (item.ValueKind != JsonValueKind.String)
            {
                throw InvalidField(propertyName);
            }
            var value = item.GetString()?.Trim() ?? string.Empty;
            if (value.Length > 0) values.Add(value);
        }
        return values.Distinct(StringComparer.Ordinal).ToList();
    }

    private static string ReadRequiredIsoDate(
        JsonElement parent,
        string propertyName)
    {
        var value = ReadRequiredString(parent, propertyName);
        if (!DateOnly.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw InvalidField(propertyName);
        }
        return value;
    }

    private static string ReadOptionalDateStruct(
        JsonElement parent,
        string propertyName)
    {
        var dateStruct = ReadOptionalObject(parent, propertyName);
        return dateStruct is null
            ? string.Empty
            : ReadRequiredIsoDate(dateStruct.Value, "date");
    }

    private static string ReadOptionalRootString(
        JsonElement root,
        string propertyName)
        => ReadOptionalString(root, propertyName);

    private static long ReadRequiredTotalCount(JsonElement root)
    {
        if (!root.TryGetProperty("totalCount", out var property))
        {
            throw new SourceAcquisitionException(
                "total-count-missing",
                "ClinicalTrials.gov response must include totalCount when countTotal=true.");
        }
        if (!property.TryGetInt64(out var value) || value < 0)
        {
            throw InvalidField("totalCount");
        }
        return value;
    }

    private static bool IsNctId(string value)
        => value.Length == 11
           && value.StartsWith("NCT", StringComparison.Ordinal)
           && value.AsSpan(3).ToArray().All(
               character => character is >= '0' and <= '9');

    private static SourceAcquisitionException InvalidField(string propertyName)
        => new(
            "field-invalid",
            $"ClinicalTrials.gov returned invalid field '{propertyName}'.");

    private sealed record RegisteredIntervention(
        string Name,
        string Type,
        IReadOnlyList<string> OtherNames);
}
