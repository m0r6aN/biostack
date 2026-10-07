namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Mail;
using System.Text.Json;

public sealed class PubMedEutilitiesCitationMetadataAcquisitionAdapter
    : ISourceAcquisitionAdapter
{
    public const string PlanningAdapterId = "pubmed-planning-v1";
    public const string TransformationVersion =
        "pubmed-eutilities-citation-metadata-v1";
    public const int DefaultSearchMaximumResponseBytes = 256 * 1024;
    public const int DefaultSummaryMaximumResponseBytes = 1024 * 1024;

    private const string ESearchEndpoint =
        "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esearch.fcgi";
    private const string ESummaryEndpoint =
        "https://eutils.ncbi.nlm.nih.gov/entrez/eutils/esummary.fcgi";
    private const string PubMedRecordPrefix =
        "https://pubmed.ncbi.nlm.nih.gov/";
    private const string TermsUrl =
        "https://www.nlm.nih.gov/databases/download.html";
    private const int MaximumSearchTerms = 20;
    private const int MaximumResults = 50;
    private const int MaximumRequestUriLength = 4096;

    private static readonly string[] AllowedFieldUses =
    [
        "mechanism",
        "efficacy-claims",
        "interactions",
    ];

    private static readonly string[] RequiredProvenanceFields =
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
    ];

    private static readonly IReadOnlySet<string> OptionalIdentifierFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "doi",
            "pmcid",
        };

    private static readonly IReadOnlyList<string> EvidenceLimitations =
    [
        "PubMed citation metadata and indexing do not establish study quality, applicability, clinical certainty, efficacy, safety, or causality.",
        "This adapter does not retrieve abstracts, excerpts, PMC or publisher full text, EFetch content, or LinkOut content.",
        "Every candidate requires claim-level linkage, evidence grading, and human review before canonical promotion.",
    ];

    private static readonly ISourceRequestGate SharedRequestGate =
        new NcbiEutilitiesPacedRequestGate(TimeProvider.System);

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly string _ncbiTool;
    private readonly string _contactEmail;
    private readonly int _searchMaximumResponseBytes;
    private readonly int _summaryMaximumResponseBytes;
    private readonly ISourceRequestGate _requestGate;

    public PubMedEutilitiesCitationMetadataAcquisitionAdapter(
        string expectedRegistrySha256,
        string ncbiTool,
        string contactEmail,
        int searchMaximumResponseBytes = DefaultSearchMaximumResponseBytes,
        int summaryMaximumResponseBytes = DefaultSummaryMaximumResponseBytes)
        : this(
            SourceAcquisitionHttpTransport.CreateRedirectDisabledAnonymousClient(
                TimeSpan.FromSeconds(20)),
            expectedRegistrySha256,
            ncbiTool,
            contactEmail,
            searchMaximumResponseBytes,
            summaryMaximumResponseBytes,
            SharedRequestGate)
    {
    }

    internal PubMedEutilitiesCitationMetadataAcquisitionAdapter(
        HttpClient httpClient,
        string expectedRegistrySha256,
        string ncbiTool,
        string contactEmail,
        int searchMaximumResponseBytes,
        int summaryMaximumResponseBytes,
        ISourceRequestGate requestGate)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _expectedRegistrySha256 =
            SourceAcquisitionIntentGuard.RequireLowercaseSha256(
                expectedRegistrySha256,
                nameof(expectedRegistrySha256));
        _ncbiTool = RequireNcbiTool(ncbiTool);
        _contactEmail = RequireContactEmail(contactEmail);
        _searchMaximumResponseBytes = searchMaximumResponseBytes > 0
            ? searchMaximumResponseBytes
            : throw new ArgumentOutOfRangeException(
                nameof(searchMaximumResponseBytes));
        _summaryMaximumResponseBytes = summaryMaximumResponseBytes > 0
            ? summaryMaximumResponseBytes
            : throw new ArgumentOutOfRangeException(
                nameof(summaryMaximumResponseBytes));
        _requestGate = requestGate ?? throw new ArgumentNullException(nameof(requestGate));
    }

    public string SourceId => "pubmed";
    public string AdapterId => TransformationVersion;

    public async Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var authorizedUses = ValidateIntent(intent, retrievedAtUtc);
        var query = BuildQuery(intent.SearchTerms);
        var searchUri = BuildSearchUri(query, includeClientIdentity: true);
        var redactedSearchUri = BuildSearchUri(
            query,
            includeClientIdentity: false);
        RequireBoundedUri(searchUri);

        var searchResponse = await GetJsonAsync(
            searchUri,
            _searchMaximumResponseBytes,
            "PubMed ESearch",
            cancellationToken);
        if (searchResponse.Status is not null)
        {
            return EmptyBatch(
                searchResponse.Status.Value,
                searchResponse.RetryAfter);
        }

        using var searchDocument = ParseJson(searchResponse.Body!, "PubMed ESearch");
        var searchJsonStatus = ClassifyJsonError(searchDocument.RootElement);
        if (searchJsonStatus is not null)
        {
            return EmptyBatch(searchJsonStatus.Value, searchResponse.RetryAfter);
        }
        ValidateOperationEnvelope(searchDocument.RootElement, "esearch");
        var searchResult = ParseSearchResult(searchDocument.RootElement);
        if (searchResult.Pmids.Count == 0)
        {
            return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch, null);
        }

        var summaryUri = BuildSummaryUri(searchResult.Pmids);
        RequireBoundedUri(summaryUri);
        var summaryResponse = await GetJsonAsync(
            summaryUri,
            _summaryMaximumResponseBytes,
            "PubMed ESummary",
            cancellationToken);
        if (summaryResponse.Status is not null)
        {
            return EmptyBatch(
                summaryResponse.Status.Value,
                summaryResponse.RetryAfter);
        }

        using var summaryDocument = ParseJson(
            summaryResponse.Body!,
            "PubMed ESummary");
        var summaryJsonStatus = ClassifyJsonError(summaryDocument.RootElement);
        if (summaryJsonStatus is not null)
        {
            return EmptyBatch(summaryJsonStatus.Value, summaryResponse.RetryAfter);
        }
        ValidateOperationEnvelope(summaryDocument.RootElement, "esummary");

        var summaries = ParseSummaryResult(
            summaryDocument.RootElement,
            searchResult.Pmids);
        var candidates = new List<SourceAcquisitionCandidate>(summaries.Count);
        foreach (var summary in summaries)
        {
            var candidate = BuildCandidate(
                intent,
                authorizedUses,
                query,
                redactedSearchUri,
                summary,
                retrievedAtUtc);
            SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                candidate,
                RequiredProvenanceFields,
                SourceId,
                _expectedRegistrySha256,
                allowedNotProvidedFields: OptionalIdentifierFields);
            candidates.Add(candidate);
        }

        return new SourceAcquisitionBatch(
            SourceAcquisitionBatchStatus.Completed,
            candidates,
            Truncated: searchResult.TotalCount > searchResult.Pmids.Count,
            RetryAfter: null);
    }

    private IReadOnlyList<string> ValidateIntent(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc)
    {
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            new SourceAcquisitionIntentRequirements(
                SourceId,
                "PubMed",
                PlanningAdapterId,
                "api",
                _expectedRegistrySha256,
                RequiredProvenanceFields));

        var authorized = AllowedFieldUses
            .Where(allowed => intent.AuthorizedFieldUses?.Contains(
                allowed,
                StringComparer.OrdinalIgnoreCase) == true)
            .ToList();
        if (authorized.Count == 0)
        {
            throw new SourceAcquisitionException(
                "citation-use-not-authorized",
                "The PubMed citation adapter requires an authorized mechanism, efficacy-claims, or interactions field use.");
        }
        return authorized;
    }

    private static string BuildQuery(IReadOnlyList<string> rawTerms)
    {
        if (rawTerms is null)
        {
            throw InvalidTermCount();
        }
        var terms = rawTerms
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (terms.Count is < 1 or > MaximumSearchTerms)
        {
            throw InvalidTermCount();
        }
        if (terms.Any(term =>
                term.Length > 128
                || term.Any(char.IsControl)
                || term.Contains("\"", StringComparison.Ordinal)
                || term.Contains("'", StringComparison.Ordinal)
                || term.Contains("\\", StringComparison.Ordinal)
                || term.Contains("[", StringComparison.Ordinal)
                || term.Contains("]", StringComparison.Ordinal)))
        {
            throw new SourceAcquisitionException(
                "invalid-search-term",
                "PubMed search terms must be at most 128 characters and contain no controls, quotes, backslashes, or brackets.");
        }

        return string.Join(
            " OR ",
            terms.Select(term => $"\"{term}\"[Title/Abstract]"));
    }

    private Uri BuildSearchUri(string query, bool includeClientIdentity)
    {
        var parameters = new List<KeyValuePair<string, string>>
        {
            new("db", "pubmed"),
            new("term", query),
            new("retmode", "json"),
            new("retstart", "0"),
            new("retmax", MaximumResults.ToString(CultureInfo.InvariantCulture)),
            new("sort", "pub_date"),
            new("usehistory", "n"),
        };
        if (includeClientIdentity)
        {
            parameters.Add(new("tool", _ncbiTool));
            parameters.Add(new("email", _contactEmail));
        }
        return BuildUri(ESearchEndpoint, parameters);
    }

    private Uri BuildSummaryUri(IReadOnlyList<string> pmids)
        => BuildUri(
            ESummaryEndpoint,
            [
                new("db", "pubmed"),
                new("id", string.Join(",", pmids)),
                new("retmode", "json"),
                new("tool", _ncbiTool),
                new("email", _contactEmail),
            ]);

    private static Uri BuildUri(
        string endpoint,
        IEnumerable<KeyValuePair<string, string>> parameters)
    {
        var query = string.Join(
            "&",
            parameters.Select(pair =>
                $"{Uri.EscapeDataString(pair.Key)}={Uri.EscapeDataString(pair.Value)}"));
        return new Uri($"{endpoint}?{query}", UriKind.Absolute);
    }

    private static void RequireBoundedUri(Uri uri)
    {
        if (uri.AbsoluteUri.Length > MaximumRequestUriLength)
        {
            throw new SourceAcquisitionException(
                "request-uri-too-long",
                "PubMed request URI exceeds the adapter limit.");
        }
    }

    private async Task<PubMedHttpResult> GetJsonAsync(
        Uri requestUri,
        int maximumResponseBytes,
        string sourceDisplayName,
        CancellationToken cancellationToken)
    {
        RequireFixedFirstPartyUri(requestUri);
        using var requestLease = await _requestGate.AcquireAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(
            new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);
        var retryAfter = response.Headers.RetryAfter?.ToString();
        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return PubMedHttpResult.StatusResult(
                SourceAcquisitionBatchStatus.RateLimited,
                retryAfter);
        }
        if (response.StatusCode == HttpStatusCode.ServiceUnavailable)
        {
            return PubMedHttpResult.StatusResult(
                SourceAcquisitionBatchStatus.BackPressure,
                retryAfter);
        }
        if ((int)response.StatusCode is >= 300 and <= 399)
        {
            throw new SourceAcquisitionException(
                "redirect-response",
                "PubMed redirects are not accepted by this adapter.");
        }
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            throw new SourceAcquisitionException(
                "asynchronous-response-not-supported",
                "PubMed asynchronous responses are not accepted by this adapter.");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"PubMed returned HTTP {(int)response.StatusCode}.");
        }
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(
                mediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                $"{sourceDisplayName} response content type must be application/json.");
        }
        var body = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            response.Content,
            maximumResponseBytes,
            sourceDisplayName,
            cancellationToken);
        return PubMedHttpResult.Json(body, retryAfter);
    }

    private static PubMedSearchResult ParseSearchResult(JsonElement root)
    {
        if (!root.TryGetProperty("esearchresult", out var result)
            || result.ValueKind != JsonValueKind.Object)
        {
            throw new SourceAcquisitionException(
                "search-result-missing",
                "PubMed ESearch did not contain esearchresult.");
        }
        var count = ReadNonNegativeIntegerString(result, "count");
        var retstart = ReadNonNegativeIntegerString(result, "retstart");
        var retmax = ReadNonNegativeIntegerString(result, "retmax");
        if (retstart != 0)
        {
            throw new SourceAcquisitionException(
                "search-retstart-invalid",
                "PubMed ESearch retstart must be zero.");
        }
        if (!result.TryGetProperty("idlist", out var idList)
            || idList.ValueKind != JsonValueKind.Array
            || idList.GetArrayLength() > MaximumResults)
        {
            throw new SourceAcquisitionException(
                "search-id-list-invalid",
                "PubMed ESearch idlist is missing or exceeds 50 records.");
        }

        var pmids = new List<string>();
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var item in idList.EnumerateArray())
        {
            var pmid = ReadPmid(item, "PubMed ESearch");
            if (!seen.Add(pmid))
            {
                throw new SourceAcquisitionException(
                    "duplicate-pmid",
                    "PubMed ESearch returned a duplicate PMID.");
            }
            pmids.Add(pmid);
        }
        if (pmids.Count != Math.Min(count, MaximumResults))
        {
            throw new SourceAcquisitionException(
                "search-count-correlation-invalid",
                "PubMed ESearch idlist count must equal min(count, 50).");
        }
        if (retmax != pmids.Count)
        {
            throw new SourceAcquisitionException(
                "search-retmax-invalid",
                "PubMed ESearch retmax must equal the returned idlist count.");
        }
        return new PubMedSearchResult(count, pmids);
    }

    private static IReadOnlyList<PubMedSummaryRecord> ParseSummaryResult(
        JsonElement root,
        IReadOnlyList<string> requestedPmids)
    {
        if (!root.TryGetProperty("result", out var result)
            || result.ValueKind != JsonValueKind.Object
            || !result.TryGetProperty("uids", out var uids)
            || uids.ValueKind != JsonValueKind.Array)
        {
            throw new SourceAcquisitionException(
                "summary-result-missing",
                "PubMed ESummary did not contain result.uids.");
        }

        var returnedUids = new List<string>();
        var seenUids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var uid in uids.EnumerateArray())
        {
            var value = ReadPmid(uid, "PubMed ESummary uids");
            if (!seenUids.Add(value))
            {
                throw new SourceAcquisitionException(
                    "duplicate-pmid",
                    "PubMed ESummary returned a duplicate UID.");
            }
            returnedUids.Add(value);
        }
        if (!returnedUids.SequenceEqual(requestedPmids, StringComparer.Ordinal))
        {
            throw new SourceAcquisitionException(
                "summary-uid-correlation-invalid",
                "PubMed ESummary UIDs did not exactly match the requested PMIDs.");
        }

        var recordKeys = result.EnumerateObject()
            .Where(property =>
                !string.Equals(property.Name, "uids", StringComparison.Ordinal))
            .Select(property => property.Name)
            .ToList();
        if (recordKeys.Count != requestedPmids.Count
            || !recordKeys.ToHashSet(StringComparer.Ordinal)
                .SetEquals(requestedPmids))
        {
            throw new SourceAcquisitionException(
                "summary-record-correlation-invalid",
                "PubMed ESummary record keys did not exactly match the requested PMIDs.");
        }

        var records = new List<PubMedSummaryRecord>(requestedPmids.Count);
        foreach (var requestedPmid in requestedPmids)
        {
            var item = result.GetProperty(requestedPmid);
            if (item.ValueKind != JsonValueKind.Object)
            {
                throw new SourceAcquisitionException(
                    "summary-record-invalid",
                    $"PubMed ESummary record {requestedPmid} was not an object.");
            }
            var uid = ReadRequiredString(item, "uid", "PubMed ESummary");
            if (!string.Equals(uid, requestedPmid, StringComparison.Ordinal))
            {
                throw new SourceAcquisitionException(
                    "summary-record-correlation-invalid",
                    "PubMed ESummary record UID did not match its requested PMID.");
            }

            var identifiers = ReadIdentifiers(item, requestedPmid);
            var publicationDate =
                ReadRequiredString(item, "pubdate", "PubMed ESummary");
            var fields = new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["article_title"] =
                [
                    ReadRequiredString(item, "title", "PubMed ESummary"),
                ],
                ["publication_date"] = [publicationDate],
            };
            AddOptionalScalar(fields, item, "source", "journal_source");
            AddOptionalScalar(fields, item, "volume", "volume");
            AddOptionalScalar(fields, item, "issue", "issue");
            AddOptionalScalar(fields, item, "pages", "pages");
            AddOptionalScalar(fields, item, "elocationid", "e_location");
            AddOptionalStringArray(
                fields,
                item,
                "pubtype",
                "publication_types");
            AddOptionalStringArray(fields, item, "lang", "languages");
            records.Add(new PubMedSummaryRecord(
                requestedPmid,
                identifiers.Doi,
                identifiers.Pmcid,
                publicationDate,
                fields));
        }
        return records;
    }

    private static PubMedIdentifiers ReadIdentifiers(
        JsonElement item,
        string requestedPmid)
    {
        if (!item.TryGetProperty("articleids", out var articleIds)
            || articleIds.ValueKind != JsonValueKind.Array)
        {
            throw new SourceAcquisitionException(
                "article-identifiers-missing",
                "PubMed ESummary did not contain articleids.");
        }
        var identifiers = new Dictionary<string, string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var articleId in articleIds.EnumerateArray())
        {
            if (articleId.ValueKind != JsonValueKind.Object)
            {
                throw new SourceAcquisitionException(
                    "article-identifiers-invalid",
                    "Every PubMed ESummary articleids entry must be an object.");
            }
            var type = ReadRequiredString(
                articleId,
                "idtype",
                "PubMed ESummary articleids");
            if (type is not ("pubmed" or "doi" or "pmc")
                && !string.Equals(type, "pubmed", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "doi", StringComparison.OrdinalIgnoreCase)
                && !string.Equals(type, "pmc", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }
            var value = ReadRequiredString(
                articleId,
                "value",
                "PubMed ESummary articleids");
            if (identifiers.TryGetValue(type, out var existing))
            {
                if (!string.Equals(
                        existing,
                        value,
                        StringComparison.OrdinalIgnoreCase))
                {
                    throw new SourceAcquisitionException(
                        "conflicting-article-identifier",
                        $"PubMed ESummary returned conflicting {type} identifiers.");
                }
                continue;
            }
            identifiers.Add(type, value);
        }
        if (!identifiers.TryGetValue("pubmed", out var pmid)
            || !string.Equals(pmid, requestedPmid, StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "pmid-correlation-invalid",
                "PubMed ESummary requires one articleids PMID matching the record UID.");
        }
        if (identifiers.TryGetValue("pmc", out var pmcid)
            && (!pmcid.StartsWith("PMC", StringComparison.OrdinalIgnoreCase)
                || !ulong.TryParse(pmcid[3..], out var pmcNumber)
                || pmcNumber == 0))
        {
            throw new SourceAcquisitionException(
                "pmcid-invalid",
                "PubMed ESummary returned an invalid PMCID.");
        }
        return new PubMedIdentifiers(
            identifiers.GetValueOrDefault("doi"),
            identifiers.GetValueOrDefault("pmc"));
    }

    private static SourceAcquisitionCandidate BuildCandidate(
        SourceAcquisitionIntent intent,
        IReadOnlyList<string> authorizedUses,
        string query,
        Uri redactedSearchUri,
        PubMedSummaryRecord summary,
        DateTimeOffset retrievedAtUtc)
    {
        var sourceUrl =
            $"{PubMedRecordPrefix}{summary.Pmid}/";
        var doiProvenance = summary.Doi is null
            ? SourceProvenanceValue.NotProvided(
                "PubMed ESummary did not provide a DOI for this citation.")
            : SourceProvenanceValue.Present(summary.Doi);
        var pmcidProvenance = summary.Pmcid is null
            ? SourceProvenanceValue.NotProvided(
                "PubMed ESummary did not provide a PMCID for this citation.")
            : SourceProvenanceValue.Present(summary.Pmcid);
        var coveredFields = summary.Fields.Keys
            .Concat(["pmid", "doi", "pmcid", "publicationDate", "query"])
            .ToList();

        return new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            SourceRegistryId: "pubmed",
            SourceItemId: summary.Pmid,
            SourceUrl: sourceUrl,
            QueryUrl: redactedSearchUri.AbsoluteUri,
            SourcePublicationOrUpdateDate: summary.PublicationDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: summary.Fields)
        {
            AuthorizedFieldUses = authorizedUses,
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["pmid"] = SourceProvenanceValue.Present(summary.Pmid),
                    ["doi"] = doiProvenance,
                    ["pmcid"] = pmcidProvenance,
                    ["publicationDate"] =
                        SourceProvenanceValue.Present(summary.PublicationDate),
                    ["query"] = SourceProvenanceValue.Present(query),
                },
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope: "PubMed citation metadata only.",
                    Provider: "PubMed, National Library of Medicine",
                    SourceUrl: sourceUrl,
                    TermsUrl: TermsUrl,
                    RightsStatus: "reviewed",
                    CoveredFields: coveredFields),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    "PubMed and the National Library of Medicine are the source of the citation metadata.",
                ExcludedContentClasses:
                [
                    "abstracts",
                    "excerpts",
                    "PMC full text",
                    "publisher full text",
                    "EFetch content",
                    "LinkOut content",
                ],
                NonEndorsementRequired: true),
        };
    }

    private static SourceAcquisitionBatchStatus? ClassifyJsonError(
        JsonElement root)
    {
        var messages = new List<string>();
        CollectJsonErrors(root, messages);
        var hasExplicitErrorStatus = HasExplicitErrorStatus(root);
        if (messages.Count == 0 && !hasExplicitErrorStatus) return null;
        if (messages.Any(message =>
                message.Contains("rate limit", StringComparison.OrdinalIgnoreCase)
                || message.Contains(
                    "too many requests",
                    StringComparison.OrdinalIgnoreCase)
                || message.Contains("429", StringComparison.OrdinalIgnoreCase)))
        {
            return SourceAcquisitionBatchStatus.RateLimited;
        }
        throw new SourceAcquisitionException(
            "source-error-response",
            "PubMed returned a JSON error object.");
    }

    private static void ValidateOperationEnvelope(
        JsonElement root,
        string expectedType)
    {
        if (root.ValueKind != JsonValueKind.Object
            || !root.TryGetProperty("header", out var header)
            || header.ValueKind != JsonValueKind.Object
            || !header.TryGetProperty("type", out var type)
            || type.ValueKind != JsonValueKind.String
            || !string.Equals(
                type.GetString()?.Trim(),
                expectedType,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "operation-header-invalid",
                $"PubMed response header type must be '{expectedType}'.");
        }
    }

    private static bool HasExplicitErrorStatus(JsonElement element)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (string.Equals(
                        property.Name,
                        "status",
                        StringComparison.OrdinalIgnoreCase)
                    && property.Value.ValueKind == JsonValueKind.String
                    && string.Equals(
                        property.Value.GetString()?.Trim(),
                        "error",
                        StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
                if (HasExplicitErrorStatus(property.Value)) return true;
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                if (HasExplicitErrorStatus(item)) return true;
            }
        }
        return false;
    }

    private static void CollectJsonErrors(
        JsonElement element,
        ICollection<string> messages)
    {
        if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                if (property.Name.StartsWith(
                        "error",
                        StringComparison.OrdinalIgnoreCase))
                {
                    CollectStrings(property.Value, messages);
                    if (messages.Count == 0)
                    {
                        messages.Add("unspecified PubMed error");
                    }
                }
                else
                {
                    CollectJsonErrors(property.Value, messages);
                }
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectJsonErrors(item, messages);
            }
        }
    }

    private static void CollectStrings(
        JsonElement element,
        ICollection<string> destination)
    {
        if (element.ValueKind == JsonValueKind.String)
        {
            destination.Add(element.GetString() ?? string.Empty);
        }
        else if (element.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in element.EnumerateObject())
            {
                CollectStrings(property.Value, destination);
            }
        }
        else if (element.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in element.EnumerateArray())
            {
                CollectStrings(item, destination);
            }
        }
    }

    private static JsonDocument ParseJson(byte[] body, string operation)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new SourceAcquisitionException(
                "malformed-json",
                $"{operation} returned malformed JSON: {exception.Message}");
        }
    }

    private static long ReadNonNegativeIntegerString(
        JsonElement item,
        string propertyName)
    {
        if (!item.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String
            || !long.TryParse(
                property.GetString(),
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var value)
            || value < 0)
        {
            throw new SourceAcquisitionException(
                "search-count-invalid",
                "PubMed ESearch count must be a non-negative integer string.");
        }
        return value;
    }

    private static string ReadPmid(JsonElement element, string operation)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            throw new SourceAcquisitionException(
                "pmid-invalid",
                $"{operation} PMID must be a string.");
        }
        var value = element.GetString()?.Trim() ?? string.Empty;
        if (!ulong.TryParse(
                value,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numeric)
            || numeric == 0
            || !string.Equals(
                numeric.ToString(CultureInfo.InvariantCulture),
                value,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "pmid-invalid",
                $"{operation} returned an invalid PMID.");
        }
        return value;
    }

    private static string ReadRequiredString(
        JsonElement item,
        string propertyName,
        string operation)
    {
        if (!item.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"{operation} field '{propertyName}' must be a string.");
        }
        var value = property.GetString()?.Trim() ?? string.Empty;
        if (value.Length == 0 || value.Any(char.IsControl))
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"{operation} field '{propertyName}' must be substantive.");
        }
        return value;
    }

    private static void AddOptionalScalar(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement item,
        string propertyName,
        string outputName)
    {
        if (!item.TryGetProperty(propertyName, out var property)) return;
        if (property.ValueKind != JsonValueKind.String)
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"PubMed ESummary field '{propertyName}' must be a string.");
        }
        var value = property.GetString()?.Trim() ?? string.Empty;
        if (value.Any(char.IsControl))
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"PubMed ESummary field '{propertyName}' cannot contain controls.");
        }
        if (value.Length > 0) fields[outputName] = [value];
    }

    private static void AddOptionalStringArray(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement item,
        string propertyName,
        string outputName)
    {
        if (!item.TryGetProperty(propertyName, out var property)) return;
        if (property.ValueKind != JsonValueKind.Array
            || property.EnumerateArray().Any(value =>
                value.ValueKind != JsonValueKind.String))
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"PubMed ESummary field '{propertyName}' must be a string array.");
        }
        var values = property.EnumerateArray()
            .Select(value => value.GetString()?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (values.Any(value => value.Any(char.IsControl)))
        {
            throw new SourceAcquisitionException(
                "citation-field-invalid",
                $"PubMed ESummary field '{propertyName}' cannot contain controls.");
        }
        if (values.Count > 0) fields[outputName] = values;
    }

    private static void RequireFixedFirstPartyUri(Uri uri)
    {
        var valid = uri.IsAbsoluteUri
                    && string.Equals(
                        uri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.Ordinal)
                    && string.Equals(
                        uri.Host,
                        "eutils.ncbi.nlm.nih.gov",
                        StringComparison.Ordinal)
                    && uri.UserInfo.Length == 0
                    && uri.Port == 443
                    && uri.Fragment.Length == 0
                    && (string.Equals(
                            uri.AbsolutePath,
                            "/entrez/eutils/esearch.fcgi",
                            StringComparison.Ordinal)
                        || string.Equals(
                            uri.AbsolutePath,
                            "/entrez/eutils/esummary.fcgi",
                            StringComparison.Ordinal));
        if (!valid)
        {
            throw new SourceAcquisitionException(
                "request-uri-not-allowlisted",
                "PubMed acquisition permits only fixed first-party ESearch and ESummary HTTPS endpoints.");
        }
    }

    private static string RequireNcbiTool(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var normalized = value.Trim();
        if (normalized.Length is < 2 or > 64
            || normalized.Any(character =>
                !char.IsAsciiLetterOrDigit(character)
                && character is not ('.' or '_' or '-')))
        {
            throw new ArgumentException(
                "NCBI tool must be 2-64 ASCII letters, digits, dots, underscores, or hyphens.",
                nameof(value));
        }
        return normalized;
    }

    private static string RequireContactEmail(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var normalized = value.Trim();
        try
        {
            var address = new MailAddress(normalized);
            if (normalized.Length > 254
                || !string.Equals(
                    address.Address,
                    normalized,
                    StringComparison.OrdinalIgnoreCase)
                || !address.Host.Contains('.', StringComparison.Ordinal)
                || normalized.Any(char.IsControl))
            {
                throw new FormatException();
            }
        }
        catch (FormatException)
        {
            throw new ArgumentException(
                "NCBI contact email must be a valid bare email address.",
                nameof(value));
        }
        return normalized;
    }

    private static SourceAcquisitionException InvalidTermCount()
        => new(
            "invalid-search-term-count",
            "PubMed acquisition requires one to twenty distinct search terms.");

    private static SourceAcquisitionBatch EmptyBatch(
        SourceAcquisitionBatchStatus status,
        string? retryAfter)
        => new(
            status,
            Array.Empty<SourceAcquisitionCandidate>(),
            Truncated: false,
            RetryAfter: retryAfter);

    private sealed record PubMedSearchResult(
        long TotalCount,
        IReadOnlyList<string> Pmids);

    private sealed record PubMedIdentifiers(string? Doi, string? Pmcid);

    private sealed record PubMedSummaryRecord(
        string Pmid,
        string? Doi,
        string? Pmcid,
        string PublicationDate,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Fields);

    private sealed record PubMedHttpResult(
        byte[]? Body,
        SourceAcquisitionBatchStatus? Status,
        string? RetryAfter)
    {
        public static PubMedHttpResult Json(byte[] body, string? retryAfter)
            => new(body, Status: null, retryAfter);

        public static PubMedHttpResult StatusResult(
            SourceAcquisitionBatchStatus status,
            string? retryAfter)
            => new(null, status, retryAfter);
    }
}

internal sealed class NcbiEutilitiesPacedRequestGate : ISourceRequestGate
{
    internal static readonly TimeSpan MinimumStartInterval =
        TimeSpan.FromMilliseconds(334);

    private readonly TimeProvider _timeProvider;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> _delayAsync;
    private readonly SemaphoreSlim _serialGate = new(1, 1);
    private DateTimeOffset? _lastRequestStartUtc;

    public NcbiEutilitiesPacedRequestGate(TimeProvider timeProvider)
        : this(
            timeProvider,
            (delay, cancellationToken) => new ValueTask(
                Task.Delay(delay, timeProvider, cancellationToken)))
    {
    }

    internal NcbiEutilitiesPacedRequestGate(
        TimeProvider timeProvider,
        Func<TimeSpan, CancellationToken, ValueTask> delayAsync)
    {
        _timeProvider =
            timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _delayAsync = delayAsync ?? throw new ArgumentNullException(nameof(delayAsync));
    }

    public async ValueTask<IDisposable> AcquireAsync(
        CancellationToken cancellationToken)
    {
        await _serialGate.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            if (_lastRequestStartUtc is not null)
            {
                var remaining = MinimumStartInterval
                                - (now - _lastRequestStartUtc.Value);
                if (remaining > TimeSpan.Zero)
                {
                    await _delayAsync(remaining, cancellationToken);
                    now = _timeProvider.GetUtcNow();
                    if (now - _lastRequestStartUtc.Value
                        < MinimumStartInterval)
                    {
                        throw new SourceAcquisitionException(
                            "request-pacing-clock-invalid",
                            "NCBI request pacing did not advance to the required interval.");
                    }
                }
            }
            _lastRequestStartUtc = now;
            return new RequestLease(_serialGate);
        }
        catch
        {
            _serialGate.Release();
            throw;
        }
    }

    private sealed class RequestLease(SemaphoreSlim serialGate) : IDisposable
    {
        private SemaphoreSlim? _serialGate = serialGate;

        public void Dispose()
            => Interlocked.Exchange(ref _serialGate, null)?.Release();
    }
}
