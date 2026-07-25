namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

public sealed class PubChemPugRestCompoundAcquisitionAdapter
    : ISourceAcquisitionAdapter
{
    public const string PlanningAdapterId = "pubchem-planning-v1";
    public const string TransformationVersion =
        "pubchem-pug-rest-compound-identity-v1";
    public const int DefaultMaximumResponseBytes = 1024 * 1024;

    private const string PropertyEndpointPrefix =
        "https://pubchem.ncbi.nlm.nih.gov/rest/pug/compound/name/";
    private const string PropertyEndpointSuffix =
        "/property/MolecularFormula,MolecularWeight,SMILES,InChI,InChIKey,ExactMass/JSON";
    private const string PugViewEndpointPrefix =
        "https://pubchem.ncbi.nlm.nih.gov/rest/pug_view/data/compound/";
    private const string PugViewEndpointSuffix =
        "/JSON?heading=Modify%20Date";
    private const string CompoundPagePrefix =
        "https://pubchem.ncbi.nlm.nih.gov/compound/";
    private const string TermsUrl =
        "https://www.ncbi.nlm.nih.gov/home/about/policies/";
    private const int MaximumSearchTerms = 5;

    private static readonly string[] RequiredProvenanceFields =
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
    ];

    private static readonly IReadOnlyList<string> EvidenceLimitations =
    [
        "PubChem compound identity properties do not establish clinical efficacy, safety, dosing, contraindications, or suitability.",
        "This adapter includes only PubChem-computed identity properties and PubChem record metadata; depositor annotations, descriptions, synonyms, bioassays, and mechanism claims are excluded.",
        "Exact-name lookup can resolve more than one CID; every candidate requires human review before canonical promotion.",
    ];

    private static readonly IReadOnlyDictionary<string, string> PropertyFields =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["MolecularFormula"] = "molecular_formula",
            ["MolecularWeight"] = "molecular_weight",
            ["SMILES"] = "smiles",
            ["InChI"] = "inchi",
            ["InChIKey"] = "inchikey",
            ["ExactMass"] = "exact_mass",
        };

    private static readonly ISourceRequestGate SharedRequestGate =
        new PubChemPacedRequestGate(TimeProvider.System);

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly int _maximumResponseBytes;
    private readonly ISourceRequestGate _requestGate;

    public PubChemPugRestCompoundAcquisitionAdapter(
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

    internal PubChemPugRestCompoundAcquisitionAdapter(
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
    }

    public string SourceId => "pubchem";
    public string AdapterId => TransformationVersion;

    public async Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIntent(intent, retrievedAtUtc);
        var terms = NormalizeTerms(intent.SearchTerms);
        PubChemPropertyRecord? resolvedRecord = null;
        Uri? resolvedQueryUri = null;

        foreach (var term in terms)
        {
            var propertyUri = BuildPropertyUri(term);
            var propertyResponse = await GetJsonAsync(propertyUri, cancellationToken);
            if (propertyResponse.BatchStatus is not null)
            {
                return EmptyBatch(
                    propertyResponse.BatchStatus.Value,
                    propertyResponse.RetryAfter);
            }
            if (propertyResponse.NotFound) continue;

            var propertyRecords = ParsePropertyResponse(propertyResponse.Body!);
            if (propertyRecords.Count != 1)
            {
                throw new SourceAcquisitionException(
                    "ambiguous-compound-resolution",
                    "Each PubChem exact-name response must resolve to exactly one compound.");
            }

            var propertyRecord = propertyRecords[0];
            if (resolvedRecord is not null
                && resolvedRecord.Cid != propertyRecord.Cid)
            {
                throw new SourceAcquisitionException(
                    "ambiguous-compound-resolution",
                    "PubChem aliases resolved to different compound CIDs.");
            }
            resolvedRecord ??= propertyRecord;
            resolvedQueryUri ??= propertyUri;
        }

        if (resolvedRecord is null)
        {
            return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch, null);
        }

        var modifyDateUri = BuildModifyDateUri(resolvedRecord.Cid);
        var modifyDateResponse =
            await GetJsonAsync(modifyDateUri, cancellationToken);
        if (modifyDateResponse.BatchStatus is not null)
        {
            return EmptyBatch(
                modifyDateResponse.BatchStatus.Value,
                modifyDateResponse.RetryAfter);
        }
        if (modifyDateResponse.NotFound)
        {
            throw new SourceAcquisitionException(
                "modify-date-not-found",
                $"PubChem did not return Modify Date metadata for CID {resolvedRecord.Cid}.");
        }

        var recordUpdateDate = ParseModifyDateResponse(
            modifyDateResponse.Body!,
            resolvedRecord.Cid);
        var candidate = BuildCandidate(
            intent,
            resolvedRecord,
            resolvedQueryUri!,
            recordUpdateDate,
            retrievedAtUtc);
        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            RequiredProvenanceFields,
            SourceId,
            _expectedRegistrySha256);

        return new SourceAcquisitionBatch(
            SourceAcquisitionBatchStatus.Completed,
            [candidate],
            Truncated: false,
            RetryAfter: null);
    }

    private void ValidateIntent(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc)
    {
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            new SourceAcquisitionIntentRequirements(
                SourceId,
                "PubChem",
                PlanningAdapterId,
                "api",
                _expectedRegistrySha256,
                RequiredProvenanceFields));

        if (intent.AuthorizedFieldUses is null
            || !intent.AuthorizedFieldUses.Contains(
                "identity",
                StringComparer.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "identity-use-not-authorized",
                "The PubChem identity adapter requires an authorized identity field use.");
        }
    }

    private static IReadOnlyList<string> NormalizeTerms(IReadOnlyList<string> rawTerms)
    {
        if (rawTerms is null)
        {
            throw new SourceAcquisitionException(
                "invalid-search-term-count",
                "PubChem acquisition requires one to five distinct search terms.");
        }

        var terms = rawTerms
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (terms.Count is < 1 or > MaximumSearchTerms)
        {
            throw new SourceAcquisitionException(
                "invalid-search-term-count",
                "PubChem acquisition requires one to five distinct search terms.");
        }
        if (terms.Any(term =>
                term.Length > 128
                || term.Any(char.IsControl)
                || term.Contains("\"", StringComparison.Ordinal)
                || term.Contains("'", StringComparison.Ordinal)
                || term.Contains("..", StringComparison.Ordinal)))
        {
            throw new SourceAcquisitionException(
                "invalid-search-term",
                "PubChem search terms must be at most 128 characters and contain no controls, quotes, or dot-segments.");
        }
        return terms;
    }

    private async Task<PubChemHttpResult> GetJsonAsync(
        Uri requestUri,
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
        var throttlingStatus = ReadThrottlingStatus(response);
        if (throttlingStatus is not null)
        {
            return PubChemHttpResult.Status(throttlingStatus.Value, retryAfter);
        }
        if (response.StatusCode is HttpStatusCode.TooManyRequests
            or HttpStatusCode.ServiceUnavailable)
        {
            return PubChemHttpResult.Status(
                SourceAcquisitionBatchStatus.RateLimited,
                retryAfter);
        }

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return PubChemHttpResult.Missing();
        }
        if ((int)response.StatusCode is >= 300 and <= 399)
        {
            throw new SourceAcquisitionException(
                "redirect-response",
                "PubChem redirects are not accepted by this adapter.");
        }
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            throw new SourceAcquisitionException(
                "asynchronous-response-not-supported",
                "PubChem asynchronous responses are not accepted by this adapter.");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"PubChem returned HTTP {(int)response.StatusCode}.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(
                mediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                "PubChem response content type must be application/json.");
        }

        var body = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            response.Content,
            _maximumResponseBytes,
            "PubChem",
            cancellationToken);
        return PubChemHttpResult.Json(body);
    }

    private static SourceAcquisitionBatchStatus? ReadThrottlingStatus(
        HttpResponseMessage response)
    {
        if (!response.Headers.TryGetValues(
                "X-Throttling-Control",
                out var values))
        {
            return null;
        }

        var segments = string.Join(",", values)
            .Split(
                ',',
                StringSplitOptions.RemoveEmptyEntries
                | StringSplitOptions.TrimEntries);
        var requestCount = ParseThrottlingIndicator(
            segments,
            "Request Count");
        var requestTime = ParseThrottlingIndicator(
            segments,
            "Request Time");
        var service = ParseThrottlingIndicator(segments, "Service");
        if (IsRedOrBlack(service))
        {
            return SourceAcquisitionBatchStatus.BackPressure;
        }
        if (IsRedOrBlack(requestCount)
            || IsRedOrBlack(requestTime))
        {
            return SourceAcquisitionBatchStatus.RateLimited;
        }
        return null;
    }

    private static string ParseThrottlingIndicator(
        IReadOnlyList<string> segments,
        string indicator)
    {
        var prefix = $"{indicator} status:";
        var matching = segments
            .Where(segment => segment.StartsWith(
                prefix,
                StringComparison.OrdinalIgnoreCase))
            .ToList();
        if (matching.Count != 1)
        {
            throw new SourceAcquisitionException(
                "throttling-header-invalid",
                $"PubChem X-Throttling-Control must contain exactly one {indicator} indicator.");
        }

        var value = matching[0][prefix.Length..]
            .TrimStart()
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault();
        if (value is null
            || !new[] { "Green", "Yellow", "Red", "Black" }.Contains(
                value,
                StringComparer.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "throttling-header-invalid",
                $"PubChem X-Throttling-Control contained an invalid {indicator} status.");
        }
        return value;
    }

    private static bool IsRedOrBlack(string value)
        => string.Equals(value, "Red", StringComparison.OrdinalIgnoreCase)
           || string.Equals(value, "Black", StringComparison.OrdinalIgnoreCase);

    private static IReadOnlyList<PubChemPropertyRecord> ParsePropertyResponse(
        byte[] body)
    {
        using var document = ParseJson(body);
        if (!document.RootElement.TryGetProperty(
                "PropertyTable",
                out var table)
            || table.ValueKind != JsonValueKind.Object
            || !table.TryGetProperty("Properties", out var properties)
            || properties.ValueKind != JsonValueKind.Array)
        {
            throw new SourceAcquisitionException(
                "properties-missing",
                "PubChem response did not contain PropertyTable.Properties.");
        }

        var records = new List<PubChemPropertyRecord>();
        foreach (var property in properties.EnumerateArray())
        {
            if (property.ValueKind != JsonValueKind.Object
                || !property.TryGetProperty("CID", out var cidElement)
                || !cidElement.TryGetInt64(out var cid)
                || cid <= 0)
            {
                throw new SourceAcquisitionException(
                    "pubchem-cid-invalid",
                    "PubChem property result is missing a positive integer CID.");
            }

            var fields = new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.OrdinalIgnoreCase);
            foreach (var mapping in PropertyFields)
            {
                if (!property.TryGetProperty(mapping.Key, out var value)) continue;
                var normalized = ReadScalar(value);
                if (normalized.Length > 0)
                {
                    fields[mapping.Value] = [normalized];
                }
            }
            if (fields.Count == 0)
            {
                throw new SourceAcquisitionException(
                    "identity-fields-missing",
                    $"PubChem CID {cid} did not contain an approved identity property.");
            }
            records.Add(new PubChemPropertyRecord(cid, fields));
        }
        return records;
    }

    private static string ParseModifyDateResponse(byte[] body, long expectedCid)
    {
        using var document = ParseJson(body);
        if (!document.RootElement.TryGetProperty("Record", out var record)
            || record.ValueKind != JsonValueKind.Object
            || !record.TryGetProperty("RecordType", out var recordType)
            || recordType.ValueKind != JsonValueKind.String
            || !string.Equals(
                recordType.GetString()?.Trim(),
                "CID",
                StringComparison.Ordinal)
            || !record.TryGetProperty("RecordNumber", out var recordNumber)
            || !recordNumber.TryGetInt64(out var actualCid)
            || actualCid != expectedCid)
        {
            throw new SourceAcquisitionException(
                "pug-view-cid-mismatch",
                "PubChem PUG View RecordNumber did not match the requested CID.");
        }

        var modifySections = new List<JsonElement>();
        CollectModifyDateSections(record, modifySections);
        if (modifySections.Count != 1)
        {
            throw new SourceAcquisitionException(
                "modify-date-section-invalid",
                "PubChem PUG View must contain exactly one Modify Date section.");
        }

        var dates = new List<string>();
        var referenceNumbers = new HashSet<long>();
        ReadModifyDateInformation(
            modifySections[0],
            dates,
            referenceNumbers);
        if (dates.Count != 1
            || !DateOnly.TryParseExact(
                dates[0],
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new SourceAcquisitionException(
                "record-update-date-invalid",
                "PubChem Modify Date metadata must contain exactly one ISO date.");
        }
        if (referenceNumbers.Count != 1
            || !ReferencesArePubChem(record, referenceNumbers))
        {
            throw new SourceAcquisitionException(
                "modify-date-reference-invalid",
                "PubChem Modify Date metadata must link only to a PubChem reference.");
        }
        return dates[0];
    }

    private static void CollectModifyDateSections(
        JsonElement node,
        ICollection<JsonElement> destination)
    {
        if (node.ValueKind == JsonValueKind.Object)
        {
            if (node.TryGetProperty("TOCHeading", out var heading)
                && heading.ValueKind == JsonValueKind.String
                && string.Equals(
                    heading.GetString()?.Trim(),
                    "Modify Date",
                    StringComparison.Ordinal))
            {
                destination.Add(node);
            }
            foreach (var property in node.EnumerateObject())
            {
                CollectModifyDateSections(property.Value, destination);
            }
        }
        else if (node.ValueKind == JsonValueKind.Array)
        {
            foreach (var item in node.EnumerateArray())
            {
                CollectModifyDateSections(item, destination);
            }
        }
    }

    private static void ReadModifyDateInformation(
        JsonElement section,
        ICollection<string> dates,
        ISet<long> referenceNumbers)
    {
        if (!section.TryGetProperty("Information", out var information)
            || information.ValueKind != JsonValueKind.Array)
        {
            throw new SourceAcquisitionException(
                "modify-date-information-missing",
                "PubChem Modify Date section did not contain Information.");
        }

        if (information.GetArrayLength() != 1)
        {
            throw new SourceAcquisitionException(
                "modify-date-information-invalid",
                "PubChem Modify Date must contain exactly one Information item.");
        }

        var item = information[0];
        if (!item.TryGetProperty("ReferenceNumber", out var referenceNumber)
            || !referenceNumber.TryGetInt64(out var number)
            || number <= 0)
        {
            throw new SourceAcquisitionException(
                "modify-date-reference-invalid",
                "PubChem Modify Date must contain one positive ReferenceNumber.");
        }
        referenceNumbers.Add(number);
        if (!item.TryGetProperty("Value", out var value)
            || value.ValueKind != JsonValueKind.Object
            || !value.TryGetProperty("DateISO8601", out var dateValues)
            || dateValues.ValueKind != JsonValueKind.Array
            || dateValues.GetArrayLength() != 1
            || dateValues[0].ValueKind != JsonValueKind.String)
        {
            throw new SourceAcquisitionException(
                "record-update-date-invalid",
                "PubChem Modify Date must contain one ISO date.");
        }
        dates.Add(dateValues[0].GetString()?.Trim() ?? string.Empty);
    }

    private static bool ReferencesArePubChem(
        JsonElement record,
        IReadOnlySet<long> requiredNumbers)
    {
        if (!record.TryGetProperty("Reference", out var references)
            || references.ValueKind != JsonValueKind.Array)
        {
            return false;
        }

        var matched = 0;
        foreach (var reference in references.EnumerateArray())
        {
            if (!reference.TryGetProperty(
                    "ReferenceNumber",
                    out var numberElement)
                || !numberElement.TryGetInt64(out var number)
                || !requiredNumbers.Contains(number))
            {
                continue;
            }
            if (!reference.TryGetProperty("SourceName", out var sourceName)
                || sourceName.ValueKind != JsonValueKind.String
                || !string.Equals(
                    sourceName.GetString()?.Trim(),
                    "PubChem",
                    StringComparison.OrdinalIgnoreCase)
                || !reference.TryGetProperty("SourceID", out var sourceId)
                || sourceId.ValueKind != JsonValueKind.String
                || !string.Equals(
                    sourceId.GetString()?.Trim(),
                    "PubChem",
                    StringComparison.OrdinalIgnoreCase)
                || !reference.TryGetProperty("URL", out var url)
                || url.ValueKind != JsonValueKind.String
                || !IsOfficialPubChemOrigin(url.GetString()))
            {
                return false;
            }
            matched++;
        }
        return matched == 1;
    }

    private static bool IsOfficialPubChemOrigin(string? value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && string.Equals(
               uri.Scheme,
               Uri.UriSchemeHttps,
               StringComparison.OrdinalIgnoreCase)
           && string.Equals(
               uri.Host,
               "pubchem.ncbi.nlm.nih.gov",
               StringComparison.OrdinalIgnoreCase)
           && uri.UserInfo.Length == 0
           && uri.Port == 443;

    private static JsonDocument ParseJson(byte[] body)
    {
        try
        {
            return JsonDocument.Parse(body);
        }
        catch (JsonException exception)
        {
            throw new SourceAcquisitionException(
                "malformed-json",
                $"PubChem returned malformed JSON: {exception.Message}");
        }
    }

    private static SourceAcquisitionCandidate BuildCandidate(
        SourceAcquisitionIntent intent,
        PubChemPropertyRecord propertyRecord,
        Uri propertyUri,
        string recordUpdateDate,
        DateTimeOffset retrievedAtUtc)
    {
        var cid = propertyRecord.Cid.ToString(CultureInfo.InvariantCulture);
        var sourceUrl = new Uri(
            CompoundPagePrefix + cid,
            UriKind.Absolute).AbsoluteUri;
        var coveredFields = propertyRecord.Fields.Keys
            .Concat(["pubchemCid", "recordUpdateDate"])
            .ToList();

        return new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            SourceRegistryId: "pubchem",
            SourceItemId: cid,
            SourceUrl: sourceUrl,
            QueryUrl: propertyUri.AbsoluteUri,
            SourcePublicationOrUpdateDate: recordUpdateDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: propertyRecord.Fields)
        {
            AuthorizedFieldUses = ["identity"],
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["pubchemCid"] = SourceProvenanceValue.Present(cid),
                    ["recordUpdateDate"] =
                        SourceProvenanceValue.Present(recordUpdateDate),
                },
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope:
                        "PubChem-computed compound properties and PubChem record metadata only.",
                    Provider:
                        "PubChem, National Center for Biotechnology Information",
                    SourceUrl: sourceUrl,
                    TermsUrl: TermsUrl,
                    RightsStatus: "reviewed",
                    CoveredFields: coveredFields),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    "PubChem is the source of the computed compound properties and record metadata.",
                ExcludedContentClasses:
                [
                    "contributor annotations",
                    "descriptions",
                    "synonyms",
                    "bioassays",
                    "mechanism claims",
                ],
                NonEndorsementRequired: true),
        };
    }

    private static Uri BuildPropertyUri(string term)
        => new(
            PropertyEndpointPrefix
            + Uri.EscapeDataString(term)
            + PropertyEndpointSuffix,
            UriKind.Absolute);

    private static Uri BuildModifyDateUri(long cid)
        => new(
            PugViewEndpointPrefix
            + cid.ToString(CultureInfo.InvariantCulture)
            + PugViewEndpointSuffix,
            UriKind.Absolute);

    private static void RequireFixedFirstPartyUri(Uri uri)
    {
        var valid = uri.IsAbsoluteUri
                    && string.Equals(
                        uri.Scheme,
                        Uri.UriSchemeHttps,
                        StringComparison.Ordinal)
                    && string.Equals(
                        uri.Host,
                        "pubchem.ncbi.nlm.nih.gov",
                        StringComparison.Ordinal)
                    && uri.UserInfo.Length == 0
                    && uri.Port == 443
                    && (uri.AbsolutePath.StartsWith(
                            "/rest/pug/compound/name/",
                            StringComparison.Ordinal)
                        || uri.AbsolutePath.StartsWith(
                            "/rest/pug_view/data/compound/",
                            StringComparison.Ordinal));
        if (!valid)
        {
            throw new SourceAcquisitionException(
                "request-uri-not-allowlisted",
                "PubChem acquisition permits only fixed first-party HTTPS endpoints.");
        }
    }

    private static string ReadScalar(JsonElement value)
        => value.ValueKind switch
        {
            JsonValueKind.String => value.GetString()?.Trim() ?? string.Empty,
            JsonValueKind.Number => value.GetRawText(),
            _ => string.Empty,
        };

    private static SourceAcquisitionBatch EmptyBatch(
        SourceAcquisitionBatchStatus status,
        string? retryAfter)
        => new(
            status,
            Array.Empty<SourceAcquisitionCandidate>(),
            Truncated: false,
            RetryAfter: retryAfter);

    private sealed record PubChemPropertyRecord(
        long Cid,
        IReadOnlyDictionary<string, IReadOnlyList<string>> Fields);

    private sealed record PubChemHttpResult(
        byte[]? Body,
        bool NotFound,
        SourceAcquisitionBatchStatus? BatchStatus,
        string? RetryAfter)
    {
        public static PubChemHttpResult Json(byte[] body)
            => new(body, NotFound: false, BatchStatus: null, RetryAfter: null);

        public static PubChemHttpResult Missing()
            => new(null, NotFound: true, BatchStatus: null, RetryAfter: null);

        public static PubChemHttpResult Status(
            SourceAcquisitionBatchStatus status,
            string? retryAfter)
            => new(null, NotFound: false, status, retryAfter);
    }
}

internal sealed class PubChemPacedRequestGate : ISourceRequestGate
{
    internal static readonly TimeSpan MinimumStartInterval =
        TimeSpan.FromMilliseconds(200);

    private readonly TimeProvider _timeProvider;
    private readonly Func<TimeSpan, CancellationToken, ValueTask> _delayAsync;
    private readonly SemaphoreSlim _serialGate = new(1, 1);
    private DateTimeOffset? _lastRequestStartUtc;

    public PubChemPacedRequestGate(TimeProvider timeProvider)
        : this(
            timeProvider,
            (delay, cancellationToken) => new ValueTask(
                Task.Delay(delay, timeProvider, cancellationToken)))
    {
    }

    internal PubChemPacedRequestGate(
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
                var elapsed = now - _lastRequestStartUtc.Value;
                var remaining = MinimumStartInterval - elapsed;
                if (remaining > TimeSpan.Zero)
                {
                    await _delayAsync(remaining, cancellationToken);
                    now = _timeProvider.GetUtcNow();
                    if (now - _lastRequestStartUtc.Value
                        < MinimumStartInterval)
                    {
                        throw new SourceAcquisitionException(
                            "request-pacing-clock-invalid",
                            "PubChem request pacing did not advance to the required interval.");
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
