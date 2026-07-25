namespace BioStack.KnowledgeWorker.Pipeline;

using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;

public sealed class FdaOpenFdaDrugLabelAcquisitionAdapter : ISourceAcquisitionAdapter
{
    public const string TransformationVersion = "fda-openfda-drug-label-v1";
    public const string PlanningAdapterId = "fda-planning-v1";
    public const string FixedEndpoint = "https://api.fda.gov/drug/label.json";
    public const int ResultLimit = 100;
    public const int DefaultMaximumResponseBytes = 2 * 1024 * 1024;

    private static readonly FdaOpenFdaRequestGate SharedRequestGate =
        new(TimeProvider.System);

    private static readonly string[] EvidenceLimitations =
    [
        "openFDA drug-label content is submitted by manufacturers and distributors; FDA states that reformatted label content is not independently verified and may not represent a currently distributed product.",
        "A label record, especially an OTC or otherwise unapproved product record, does not by itself establish that FDA approved the product or indication.",
    ];

    private static readonly string[] RequiredProvenanceFields =
    [
        "sourceRegistryId",
        "sourceItemId",
        "sourceUrl",
        "sourcePublicationOrUpdateDate",
        "retrievedAtUtc",
        "rightsReviewStatusAtRetrieval",
        "transformationPipelineVersion",
        "humanReviewStatus",
    ];

    private static readonly string[] OpenFdaArrayFields =
    [
        "generic_name",
        "brand_name",
        "substance_name",
        "manufacturer_name",
        "route",
        "dosage_form",
        "application_number",
        "product_ndc",
        "product_type",
    ];

    private static readonly string[] LabelArrayFields =
    [
        "indications_and_usage",
        "contraindications",
        "warnings",
        "boxed_warning",
        "warnings_and_cautions",
        "adverse_reactions",
        "drug_interactions",
    ];

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly int _maximumResponseBytes;
    private readonly IFdaOpenFdaRequestGate _requestGate;

    public FdaOpenFdaDrugLabelAcquisitionAdapter(
        string expectedRegistrySha256,
        int maximumResponseBytes = DefaultMaximumResponseBytes)
        : this(
            CreateSecureAnonymousClient(),
            expectedRegistrySha256,
            maximumResponseBytes,
            SharedRequestGate)
    {
    }

    internal FdaOpenFdaDrugLabelAcquisitionAdapter(
        HttpClient httpClient,
        string expectedRegistrySha256,
        int maximumResponseBytes,
        IFdaOpenFdaRequestGate requestGate)
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _expectedRegistrySha256 = RequireSha256(expectedRegistrySha256);
        _maximumResponseBytes = maximumResponseBytes > 0
            ? maximumResponseBytes
            : throw new ArgumentOutOfRangeException(nameof(maximumResponseBytes));
        _requestGate = requestGate ?? throw new ArgumentNullException(nameof(requestGate));
    }

    public string SourceId => "fda";
    public string AdapterId => TransformationVersion;

    private static HttpClient CreateSecureAnonymousClient()
    {
        var handler = new SocketsHttpHandler
        {
            AllowAutoRedirect = false,
            AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
        };
        return new HttpClient(handler, disposeHandler: true)
        {
            Timeout = TimeSpan.FromSeconds(20),
        };
    }

    public async Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        ValidateIntent(intent, retrievedAtUtc);
        var requestUri = BuildRequestUri(intent.SearchTerms);
        using var requestLease = await _requestGate.AcquireAsync(cancellationToken);

        using var request = new HttpRequestMessage(HttpMethod.Get, requestUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        using var response = await _httpClient.SendAsync(
            request,
            HttpCompletionOption.ResponseHeadersRead,
            cancellationToken);

        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.NoMatch,
                Array.Empty<SourceAcquisitionCandidate>(),
                Truncated: false,
                RetryAfter: null);
        }

        if (response.StatusCode == HttpStatusCode.TooManyRequests)
        {
            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.RateLimited,
                Array.Empty<SourceAcquisitionCandidate>(),
                Truncated: false,
                RetryAfter: response.Headers.RetryAfter?.ToString());
        }

        if ((int)response.StatusCode is >= 300 and <= 399)
        {
            throw new SourceAcquisitionException(
                "redirect-response",
                "openFDA redirects are not accepted by this adapter.");
        }

        if (!response.IsSuccessStatusCode)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"openFDA returned HTTP {(int)response.StatusCode}.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/json", StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                "openFDA response content type must be application/json.");
        }

        var body = await ReadBoundedBodyAsync(response.Content, cancellationToken);
        return ParseResponse(body, intent, requestUri, retrievedAtUtc);
    }

    private void ValidateIntent(SourceAcquisitionIntent intent, DateTimeOffset retrievedAtUtc)
    {
        if (intent is null) throw new ArgumentNullException(nameof(intent));
        if (intent.Disposition != SourceAcquisitionDisposition.Ready
            || intent.BlockingReasons.Count > 0)
        {
            throw new SourceAcquisitionException(
                "intent-not-ready",
                "Only blocker-free Ready intents may be acquired.");
        }
        if (!string.Equals(intent.SourceId, SourceId, StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "source-not-supported",
                "This adapter accepts FDA intents only.");
        }
        if (!string.Equals(intent.AdapterId, PlanningAdapterId, StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "planning-adapter-mismatch",
                $"This adapter requires planning adapter '{PlanningAdapterId}'.");
        }
        if (!string.Equals(intent.CandidateMethod, "api", StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "acquisition-method-not-supported",
                "This adapter accepts API acquisition intents only.");
        }
        if (!string.Equals(
                intent.RegistryBindingSha256,
                _expectedRegistrySha256,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "source-registry-sha256-mismatch",
                "The acquisition intent is not bound to the expected source registry.");
        }
        if (retrievedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new SourceAcquisitionException(
                "retrieval-timestamp-not-utc",
                "The retrieval timestamp must use a UTC offset.");
        }

        var providedProvenance = intent.RequiredProvenanceFields
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (RequiredProvenanceFields.Any(field => !providedProvenance.Contains(field)))
        {
            throw new SourceAcquisitionException(
                "required-provenance-missing",
                "The acquisition intent is missing FDA-required provenance fields.");
        }
    }

    private static Uri BuildRequestUri(IReadOnlyList<string> rawTerms)
    {
        var terms = rawTerms
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (terms.Count is < 1 or > 20)
        {
            throw new SourceAcquisitionException(
                "invalid-search-term-count",
                "FDA acquisition requires between 1 and 20 distinct search terms.");
        }
        if (terms.Any(term => term.Length > 128 || term.Any(char.IsControl)))
        {
            throw new SourceAcquisitionException(
                "invalid-search-term",
                "FDA search terms must be at most 128 characters and contain no control characters.");
        }

        var clauses = terms.SelectMany(term =>
        {
            var escaped = term
                .Replace(@"\", @"\\", StringComparison.Ordinal)
                .Replace("\"", "\\\"", StringComparison.Ordinal);
            return new[]
            {
                $"openfda.generic_name:\"{escaped}\"",
                $"openfda.brand_name:\"{escaped}\"",
                $"openfda.substance_name:\"{escaped}\"",
            };
        });
        // openFDA documents adjacent search clauses as an OR query.
        var search = string.Join(" ", clauses);
        var url = $"{FixedEndpoint}?search={Uri.EscapeDataString(search)}&limit={ResultLimit}";
        if (url.Length > 4096)
        {
            throw new SourceAcquisitionException(
                "request-uri-too-long",
                "FDA acquisition request URI exceeds the adapter limit.");
        }

        return new Uri(url, UriKind.Absolute);
    }

    private async Task<byte[]> ReadBoundedBodyAsync(
        HttpContent content,
        CancellationToken cancellationToken)
    {
        if (content.Headers.ContentLength > _maximumResponseBytes)
        {
            throw new SourceAcquisitionException(
                "response-too-large",
                "openFDA response exceeded the configured size limit.");
        }

        await using var stream = await content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[16 * 1024];
        while (true)
        {
            var read = await stream.ReadAsync(chunk, cancellationToken);
            if (read == 0) break;
            if (buffer.Length + read > _maximumResponseBytes)
            {
                throw new SourceAcquisitionException(
                    "response-too-large",
                    "openFDA response exceeded the configured size limit.");
            }
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        return buffer.ToArray();
    }

    private static SourceAcquisitionBatch ParseResponse(
        byte[] body,
        SourceAcquisitionIntent intent,
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
                $"openFDA returned malformed JSON: {exception.Message}");
        }

        using (document)
        {
            if (!document.RootElement.TryGetProperty("results", out var results)
                || results.ValueKind != JsonValueKind.Array)
            {
                throw new SourceAcquisitionException(
                    "results-missing",
                    "openFDA response did not contain a results array.");
            }

            var candidates = new List<SourceAcquisitionCandidate>();
            var seenIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in results.EnumerateArray())
            {
                var sourceItemId = ReadRequiredString(item, "id");
                if (!seenIds.Add(sourceItemId)) continue;

                var fields = new Dictionary<string, IReadOnlyList<string>>(
                    StringComparer.OrdinalIgnoreCase);
                var authorizedUses = intent.AuthorizedFieldUses
                    .ToHashSet(StringComparer.OrdinalIgnoreCase);
                if (authorizedUses.Contains("identity"))
                {
                    AddScalar(fields, item, "set_id");
                    AddScalar(fields, item, "version");
                }
                if (item.TryGetProperty("openfda", out var openFda)
                    && openFda.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in OpenFdaArrayFields)
                    {
                        var requiredUse = field is "application_number" or "product_ndc" or "product_type"
                            ? "regulatory"
                            : "identity";
                        if (authorizedUses.Contains(requiredUse))
                        {
                            AddArray(fields, openFda, field, $"openfda.{field}");
                        }
                    }
                }
                if (authorizedUses.Contains("approved-indications"))
                {
                    AddArray(fields, item, "indications_and_usage", "indications_and_usage");
                }
                if (authorizedUses.Contains("contraindications-warnings"))
                {
                    foreach (var field in LabelArrayFields.Where(field =>
                                 !string.Equals(field, "indications_and_usage", StringComparison.Ordinal)
                                 && !string.Equals(field, "drug_interactions", StringComparison.Ordinal)))
                    {
                        AddArray(fields, item, field, field);
                    }
                }
                if (authorizedUses.Contains("interactions"))
                {
                    AddArray(fields, item, "drug_interactions", "drug_interactions");
                }

                candidates.Add(new SourceAcquisitionCandidate(
                    RequestId: intent.RequestId,
                    CompoundName: intent.CompoundName,
                    SourceRegistryId: "fda",
                    SourceItemId: sourceItemId,
                    SourceUrl: BuildRecordUri(sourceItemId).AbsoluteUri,
                    QueryUrl: requestUri.AbsoluteUri,
                    SourcePublicationOrUpdateDate: ReadOptionalString(item, "effective_time"),
                    RetrievedAtUtc: retrievedAtUtc,
                    RightsReviewStatusAtRetrieval: "reviewed",
                    RegistryBindingSha256: intent.RegistryBindingSha256,
                    TransformationPipelineVersion: TransformationVersion,
                    HumanReviewStatus: "review-required",
                    EvidenceLimitations: EvidenceLimitations,
                    Fields: fields));
            }

            var total = ReadTotal(document.RootElement);
            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.Completed,
                candidates,
                Truncated: total > results.GetArrayLength(),
                RetryAfter: null);
        }
    }

    private static Uri BuildRecordUri(string sourceItemId)
    {
        var exactIdSearch = $"id:\"{sourceItemId.Replace("\"", "\\\"", StringComparison.Ordinal)}\"";
        return new Uri(
            $"{FixedEndpoint}?search={Uri.EscapeDataString(exactIdSearch)}&limit=1",
            UriKind.Absolute);
    }

    private static int ReadTotal(JsonElement root)
    {
        if (root.TryGetProperty("meta", out var meta)
            && meta.TryGetProperty("results", out var results)
            && results.TryGetProperty("total", out var total)
            && total.TryGetInt32(out var value))
        {
            return value;
        }
        return 0;
    }

    private static string ReadRequiredString(JsonElement item, string propertyName)
    {
        var value = ReadOptionalString(item, propertyName);
        if (value.Length == 0)
        {
            throw new SourceAcquisitionException(
                "source-item-id-missing",
                $"openFDA result is missing required field '{propertyName}'.");
        }
        return value;
    }

    private static string ReadOptionalString(JsonElement item, string propertyName)
        => item.TryGetProperty(propertyName, out var property)
           && property.ValueKind == JsonValueKind.String
            ? property.GetString()?.Trim() ?? string.Empty
            : string.Empty;

    private static void AddScalar(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement item,
        string propertyName)
    {
        var value = ReadOptionalString(item, propertyName);
        if (value.Length > 0) fields[propertyName] = new[] { value };
    }

    private static void AddArray(
        IDictionary<string, IReadOnlyList<string>> fields,
        JsonElement item,
        string propertyName,
        string outputName)
    {
        if (!item.TryGetProperty(propertyName, out var property)
            || property.ValueKind != JsonValueKind.Array)
        {
            return;
        }

        var values = property.EnumerateArray()
            .Where(value => value.ValueKind == JsonValueKind.String)
            .Select(value => value.GetString()?.Trim() ?? string.Empty)
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.Ordinal)
            .ToList();
        if (values.Count > 0) fields[outputName] = values;
    }

    private static string RequireSha256(string value)
    {
        if (value is null) throw new ArgumentNullException(nameof(value));
        var normalized = value.Trim();
        if (normalized.Length != 64
            || normalized.Any(character =>
                character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Expected source registry SHA-256 must be lowercase hexadecimal.",
                nameof(value));
        }
        return normalized;
    }
}

internal interface IFdaOpenFdaRequestGate
{
    ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken);
}

internal sealed class FdaOpenFdaRequestGate(TimeProvider timeProvider) : IFdaOpenFdaRequestGate
{
    internal const int MaximumRequestsPerMinute = 120;
    internal const int MaximumRequestsPerDay = 900;

    private readonly TimeProvider _timeProvider =
        timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    private readonly SemaphoreSlim _serialGate = new(1, 1);
    private readonly Queue<DateTimeOffset> _minuteRequests = new();
    private DateOnly _budgetDateUtc;
    private int _dailyRequests;

    public async ValueTask<IDisposable> AcquireAsync(CancellationToken cancellationToken)
    {
        await _serialGate.WaitAsync(cancellationToken);
        try
        {
            var now = _timeProvider.GetUtcNow();
            var date = DateOnly.FromDateTime(now.UtcDateTime);
            if (date != _budgetDateUtc)
            {
                _budgetDateUtc = date;
                _dailyRequests = 0;
            }

            while (_minuteRequests.TryPeek(out var requestAt)
                   && now - requestAt >= TimeSpan.FromMinutes(1))
            {
                _minuteRequests.Dequeue();
            }

            if (_minuteRequests.Count >= MaximumRequestsPerMinute)
            {
                throw new SourceAcquisitionException(
                    "local-minute-budget-exhausted",
                    "The anonymous FDA request budget of 120 requests per minute is exhausted.");
            }
            if (_dailyRequests >= MaximumRequestsPerDay)
            {
                throw new SourceAcquisitionException(
                    "local-daily-budget-exhausted",
                    "The anonymous FDA request budget of 900 requests per UTC day is exhausted.");
            }

            _minuteRequests.Enqueue(now);
            _dailyRequests++;
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
