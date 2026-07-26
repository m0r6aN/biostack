namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class DailyMedSplListJsonAcquisitionAdapter
    : ISourceAcquisitionAdapter
{
    public const string PlanningAdapterId = "dailymed-planning-v1";
    public const string TransformationVersion =
        "dailymed-spl-list-json-identity-v1";
    public const string FixedEndpoint =
        "https://dailymed.nlm.nih.gov/dailymed/services/v2/spls.json";
    public const int ResultLimit = 50;
    public const int DefaultMaximumResponseBytes = 1024 * 1024;

    private const string TermsUrl =
        "https://www.nlm.nih.gov/web_policies.html";
    private const int MaximumTitleLength = 1024;

    private static readonly string[] RequiredProvenanceFields =
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
    ];

    private static readonly IReadOnlySet<string> AllowedNotProvidedFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "ndc",
            "effectiveDate",
        };

    private static readonly IReadOnlySet<string> AllowedNotApplicableFields =
        new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "sectionName",
            "sectionCode",
        };

    private static readonly IReadOnlyList<string> EvidenceLimitations =
    [
        "DailyMed list-record identity metadata does not establish product equivalence, labeled indications, efficacy, safety, dosing, contraindications, interactions, or suitability.",
        "This adapter does not retrieve or parse SPL XML, section text, NDC data, effective dates, media, linked documents, bulk archives, or third-party material.",
        "Every identity candidate is product-, label-set-, and version-specific and requires human review before canonical promotion.",
    ];

    private static readonly ISourceRequestGate SharedRequestGate =
        new SerializedSourceRequestGate(
            TimeProvider.System,
            [],
            dailyBudget: null);

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly int _maximumResponseBytes;
    private readonly ISourceRequestGate _requestGate;
    private readonly SourceAcquisitionIntentRequirements _intentRequirements;

    public DailyMedSplListJsonAcquisitionAdapter(
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

    internal DailyMedSplListJsonAcquisitionAdapter(
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
        _maximumResponseBytes =
            maximumResponseBytes is > 0 and <= DefaultMaximumResponseBytes
            ? maximumResponseBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        _requestGate = requestGate ?? throw new ArgumentNullException(nameof(requestGate));
        _intentRequirements = new SourceAcquisitionIntentRequirements(
            SourceId: "dailymed",
            SourceDisplayName: "DailyMed",
            PlanningAdapterId,
            CandidateMethod: "api",
            ExpectedRegistrySha256: _expectedRegistrySha256,
            RequiredProvenanceFields);
    }

    public string SourceId => "dailymed";
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
        if (intent.AuthorizedFieldUses?.Contains(
                "identity",
                StringComparer.OrdinalIgnoreCase) != true)
        {
            throw new SourceAcquisitionException(
                "identity-use-not-authorized",
                "The DailyMed JSON list adapter requires authorized identity use.");
        }

        var compoundName = ValidateCanonicalCompoundName(intent.CompoundName);
        var requestUri = BuildRequestUri(compoundName, page: 1);
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
                "DailyMed redirects are not accepted by this adapter.");
        }
        if (response.StatusCode == HttpStatusCode.Accepted)
        {
            throw new SourceAcquisitionException(
                "http-202",
                "DailyMed asynchronous responses are not accepted by this adapter.");
        }
        if (!response.IsSuccessStatusCode)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"DailyMed returned HTTP {(int)response.StatusCode}.");
        }
        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(
                mediaType,
                "application/json",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                "DailyMed response content type must be application/json.");
        }

        var body = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            response.Content,
            _maximumResponseBytes,
            "DailyMed",
            cancellationToken);
        return ParseResponse(
            body,
            intent,
            compoundName,
            requestUri,
            retrievedAtUtc);
    }

    private SourceAcquisitionBatch ParseResponse(
        byte[] body,
        SourceAcquisitionIntent intent,
        string compoundName,
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
                $"DailyMed returned malformed JSON: {exception.Message}");
        }

        using (document)
        {
            var root = document.RootElement;
            if (root.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("metadata", out var metadata)
                || metadata.ValueKind != JsonValueKind.Object
                || !root.TryGetProperty("data", out var data)
                || data.ValueKind != JsonValueKind.Array)
            {
                throw new SourceAcquisitionException(
                    "response-shape-invalid",
                    "DailyMed response must contain metadata and a data array.");
            }

            var page = ValidateMetadata(
                metadata,
                requestUri,
                compoundName,
                data.GetArrayLength());
            if (page.TotalElements == 0)
            {
                return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch, null);
            }

            var candidates = new List<SourceAcquisitionCandidate>(
                data.GetArrayLength());
            var seenSetIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var item in data.EnumerateArray())
            {
                if (item.ValueKind != JsonValueKind.Object)
                {
                    throw InvalidField("data");
                }
                var setId = ReadCanonicalSetId(item);
                if (!seenSetIds.Add(setId))
                {
                    throw new SourceAcquisitionException(
                        "duplicate-spl-set-id",
                        $"DailyMed returned duplicate SPL set ID {setId}.");
                }
                var version = ReadPositiveJsonIntegerAsString(
                    item,
                    "spl_version");
                var title = ReadBoundedTitle(item);
                RequireTitleCorrelation(title, compoundName);
                var publishedDate = ReadPublishedDate(item);
                var candidate = BuildCandidate(
                    intent,
                    setId,
                    version,
                    title,
                    publishedDate,
                    requestUri,
                    retrievedAtUtc);
                SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                    candidate,
                    RequiredProvenanceFields,
                    SourceId,
                    _expectedRegistrySha256,
                    allowedNotProvidedFields: AllowedNotProvidedFields,
                    allowedNotApplicableFields: AllowedNotApplicableFields);
                candidates.Add(candidate);
            }

            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.Completed,
                candidates,
                Truncated: page.TotalElements > (ulong)candidates.Count,
                RetryAfter: null);
        }
    }

    private static DailyMedPageMetadata ValidateMetadata(
        JsonElement metadata,
        Uri requestUri,
        string compoundName,
        int returnedCount)
    {
        if (returnedCount > ResultLimit)
        {
            throw new SourceAcquisitionException(
                "result-count-exceeded",
                "DailyMed returned more than 50 list records.");
        }
        var elementsPerPage =
            ReadNonNegativeJsonInteger(metadata, "elements_per_page");
        var totalPages =
            ReadNonNegativeJsonInteger(metadata, "total_pages");
        var totalElements =
            ReadNonNegativeJsonInteger(metadata, "total_elements");
        var currentPage =
            ReadNonNegativeJsonInteger(metadata, "current_page");
        if (elementsPerPage != ResultLimit || currentPage != 1)
        {
            throw new SourceAcquisitionException(
                "page-metadata-invalid",
                "DailyMed metadata must describe page 1 with 50 elements per page.");
        }
        if ((ulong)returnedCount
            != Math.Min(totalElements, (ulong)ResultLimit))
        {
            throw new SourceAcquisitionException(
                "result-count-correlation-invalid",
                "DailyMed data count must equal min(total_elements, 50).");
        }
        var expectedPages = totalElements == 0
            ? 0
            : 1 + ((totalElements - 1) / (ulong)ResultLimit);
        if (totalPages != expectedPages)
        {
            throw new SourceAcquisitionException(
                "total-pages-invalid",
                "DailyMed total_pages did not match total_elements and pagesize.");
        }

        var currentUrl =
            ReadRequiredString(metadata, "current_url", "metadata");
        if (!Uri.TryCreate(currentUrl, UriKind.Absolute, out var currentUri)
            || !string.Equals(
                currentUri.AbsoluteUri,
                requestUri.AbsoluteUri,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "current-url-mismatch",
                "DailyMed metadata current_url did not match the exact request.");
        }

        var hasNextPage = totalElements > (ulong)ResultLimit;
        var expectedNextUrl = hasNextPage
            ? BuildRequestUri(compoundName, page: 2).AbsoluteUri
            : "null";
        RequireMetadataPageValue(
            metadata,
            "next_page",
            hasNextPage ? 2UL : null);
        RequireMetadataValue(metadata, "next_page_url", expectedNextUrl);
        RequireMetadataValue(metadata, "previous_page", "null");
        RequireMetadataValue(metadata, "previous_page_url", "null");
        return new DailyMedPageMetadata(totalElements);
    }

    private static SourceAcquisitionCandidate BuildCandidate(
        SourceAcquisitionIntent intent,
        string setId,
        string version,
        string title,
        string publishedDate,
        Uri requestUri,
        DateTimeOffset retrievedAtUtc)
    {
        var sourceUrl =
            $"https://dailymed.nlm.nih.gov/dailymed/drugInfo.cfm?setid={setId}";
        var fields = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase)
        {
            ["label_title"] = [title],
            ["spl_set_id"] = [setId],
            ["label_version"] = [version],
            ["published_date"] = [publishedDate],
        };
        return new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            SourceRegistryId: "dailymed",
            SourceItemId: setId,
            SourceUrl: sourceUrl,
            QueryUrl: requestUri.AbsoluteUri,
            SourcePublicationOrUpdateDate: publishedDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: fields)
        {
            AuthorizedFieldUses = ["identity"],
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["splSetId"] = SourceProvenanceValue.Present(setId),
                    ["labelVersion"] = SourceProvenanceValue.Present(version),
                    ["ndc"] = SourceProvenanceValue.NotProvided(
                        "DailyMed /spls list JSON does not provide NDC values."),
                    ["effectiveDate"] = SourceProvenanceValue.NotProvided(
                        "DailyMed /spls list JSON does not provide an SPL effective date."),
                    ["sectionName"] = SourceProvenanceValue.NotApplicable(
                        "This JSON list adapter does not retrieve or parse SPL sections."),
                    ["sectionCode"] = SourceProvenanceValue.NotApplicable(
                        "This JSON list adapter does not retrieve or parse SPL section codes."),
                },
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope: "DailyMed SPL list-record identity metadata only.",
                    Provider: "DailyMed, National Library of Medicine",
                    SourceUrl: sourceUrl,
                    TermsUrl: TermsUrl,
                    RightsStatus: "reviewed",
                    CoveredFields:
                    [
                        "label_title",
                        "spl_set_id",
                        "label_version",
                        "published_date",
                        "splSetId",
                        "labelVersion",
                    ]),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    "DailyMed and the National Library of Medicine are the source of this SPL list-record identity metadata.",
                ExcludedContentClasses:
                [
                    "SPL section text",
                    "SPL XML",
                    "media and linked documents",
                    "third-party material",
                    "product-specific claims",
                ],
                NonEndorsementRequired: true),
        };
    }

    private static string ValidateCanonicalCompoundName(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var normalized = value.Trim();
        if (!string.Equals(value, normalized, StringComparison.Ordinal)
            || normalized.Length is < 1 or > 128
            || normalized.Any(character =>
                !char.IsLetterOrDigit(character)
                && character != ' '
                && character is not ('-' or '_' or '.' or '+' or '/'
                    or '\'' or '(' or ')')))
        {
            throw new SourceAcquisitionException(
                "compound-name-invalid",
                "DailyMed compound name contains unsupported query syntax or characters.");
        }
        return normalized;
    }

    private static Uri BuildRequestUri(string compoundName, int page)
        => new(
            $"{FixedEndpoint}?drug_name={Uri.EscapeDataString(compoundName)}"
            + "&name_type=both"
            + $"&pagesize={ResultLimit}"
            + $"&page={page}",
            UriKind.Absolute);

    private static string ReadCanonicalSetId(JsonElement item)
    {
        var value = ReadRequiredString(item, "setid", "data");
        if (!Guid.TryParseExact(value, "D", out var guid)
            || !string.Equals(
                value,
                guid.ToString("D", CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            throw InvalidField("setid");
        }
        return value;
    }

    private static string ReadPositiveJsonIntegerAsString(
        JsonElement item,
        string propertyName)
    {
        var numeric = ReadCanonicalJsonInteger(
            item,
            propertyName,
            "data");
        if (numeric == 0)
        {
            throw InvalidField(propertyName);
        }
        return numeric.ToString(CultureInfo.InvariantCulture);
    }

    private static string ReadBoundedTitle(JsonElement item)
    {
        var title = ReadRequiredString(item, "title", "data");
        if (title.Length > MaximumTitleLength || title.Any(char.IsControl))
        {
            throw InvalidField("title");
        }
        return title;
    }

    private static string ReadPublishedDate(JsonElement item)
    {
        var value = ReadRequiredString(item, "published_date", "data");
        if (!DateOnly.TryParseExact(
                value,
                "MMM dd, yyyy",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var date))
        {
            throw InvalidField("published_date");
        }
        return date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
    }

    private static void RequireTitleCorrelation(
        string title,
        string compoundName)
    {
        var normalizedTitle = NormalizeWords(title);
        var normalizedTerm = NormalizeWords(compoundName);
        if (normalizedTerm.Length == 0
            || !($" {normalizedTitle} ").Contains(
                $" {normalizedTerm} ",
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "title-query-correlation-invalid",
                "DailyMed label title did not contain the normalized compound term.");
        }
    }

    private static string NormalizeWords(string value)
    {
        var builder = new StringBuilder(value.Length);
        var previousWasSpace = true;
        foreach (var character in value.Normalize(NormalizationForm.FormKC))
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(char.ToLowerInvariant(character));
                previousWasSpace = false;
            }
            else if (!previousWasSpace)
            {
                builder.Append(' ');
                previousWasSpace = true;
            }
        }
        return builder.ToString().Trim();
    }

    private static ulong ReadNonNegativeJsonInteger(
        JsonElement metadata,
        string propertyName)
        => ReadCanonicalJsonInteger(
            metadata,
            propertyName,
            "metadata");

    private static ulong ReadCanonicalJsonInteger(
        JsonElement parent,
        string propertyName,
        string section)
    {
        if (!parent.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Number)
        {
            throw InvalidField($"{section}.{propertyName}");
        }
        var raw = property.GetRawText();
        if (raw.Length == 0
            || raw.Any(character => character is < '0' or > '9')
            || raw.Length > 1 && raw[0] == '0'
            || !ulong.TryParse(
                raw,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numeric))
        {
            throw InvalidField($"{section}.{propertyName}");
        }
        return numeric;
    }

    private static string ReadRequiredString(
        JsonElement parent,
        string propertyName,
        string section)
    {
        if (!parent.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.String)
        {
            throw InvalidField($"{section}.{propertyName}");
        }
        var value = property.GetString() ?? string.Empty;
        if (value.Length == 0
            || !string.Equals(value, value.Trim(), StringComparison.Ordinal)
            || value.Any(char.IsControl))
        {
            throw InvalidField($"{section}.{propertyName}");
        }
        return value;
    }

    private static void RequireMetadataValue(
        JsonElement metadata,
        string propertyName,
        string expected)
    {
        var actual = ReadRequiredString(metadata, propertyName, "metadata");
        if (!string.Equals(actual, expected, StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "pagination-metadata-invalid",
                $"DailyMed metadata {propertyName} did not match the first-page request.");
        }
    }

    private static void RequireMetadataPageValue(
        JsonElement metadata,
        string propertyName,
        ulong? expected)
    {
        if (expected is not null)
        {
            var actual = ReadNonNegativeJsonInteger(metadata, propertyName);
            if (actual == expected.Value) return;
        }
        else if (metadata.TryGetProperty(propertyName, out var property)
                 && property.ValueKind == JsonValueKind.String
                 && string.Equals(
                     property.GetString(),
                     "null",
                     StringComparison.Ordinal))
        {
            return;
        }

        throw new SourceAcquisitionException(
            "pagination-metadata-invalid",
            $"DailyMed metadata {propertyName} did not match the first-page request.");
    }

    private static SourceAcquisitionBatch EmptyBatch(
        SourceAcquisitionBatchStatus status,
        string? retryAfter)
        => new(
            status,
            Array.Empty<SourceAcquisitionCandidate>(),
            Truncated: false,
            RetryAfter: retryAfter);

    private static SourceAcquisitionException InvalidField(string field)
        => new(
            "field-invalid",
            $"DailyMed returned invalid field '{field}'.");

    private sealed record DailyMedPageMetadata(ulong TotalElements);
}
