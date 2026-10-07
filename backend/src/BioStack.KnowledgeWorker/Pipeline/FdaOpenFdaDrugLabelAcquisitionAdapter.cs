namespace BioStack.KnowledgeWorker.Pipeline;

using System.Net;
using System.Net.Http.Headers;
using System.Globalization;
using System.Text;
using System.Text.Json;

public sealed class FdaOpenFdaDrugLabelAcquisitionAdapter : ISourceAcquisitionAdapter
{
    public const string TransformationVersion = "fda-openfda-drug-label-v1";
    public const string PlanningAdapterId = "fda-planning-v1";
    public const string FixedEndpoint = "https://api.fda.gov/drug/label.json";
    public const string OpenFdaTermsUrl = "https://open.fda.gov/terms/";
    public const string FdaWebsitePoliciesUrl =
        "https://www.fda.gov/about-fda/about-website/website-policies";
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

    private static readonly IReadOnlyDictionary<string, string> FieldUseByField =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            ["set_id"] = "identity",
            ["version"] = "identity",
            ["openfda.generic_name"] = "identity",
            ["openfda.brand_name"] = "identity",
            ["openfda.substance_name"] = "identity",
            ["openfda.manufacturer_name"] = "identity",
            ["openfda.route"] = "identity",
            ["openfda.dosage_form"] = "identity",
            ["openfda.application_number"] = "regulatory",
            ["openfda.product_ndc"] = "regulatory",
            ["openfda.product_type"] = "regulatory",
            ["indications_and_usage"] = "approved-indications",
            ["contraindications"] = "contraindications-warnings",
            ["warnings"] = "contraindications-warnings",
            ["boxed_warning"] = "contraindications-warnings",
            ["warnings_and_cautions"] = "contraindications-warnings",
            ["adverse_reactions"] = "contraindications-warnings",
            ["drug_interactions"] = "interactions",
        };

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
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            new SourceAcquisitionIntentRequirements(
                SourceId,
                "FDA",
                PlanningAdapterId,
                "api",
                _expectedRegistrySha256,
                RequiredProvenanceFields));

        if (intent.AuthorizedFieldUses is null
            || !intent.AuthorizedFieldUses.Any(use =>
                FieldUseByField.Values.Contains(
                    use,
                    StringComparer.OrdinalIgnoreCase)))
        {
            throw new SourceAcquisitionException(
                "authorized-field-use-not-supported",
                "The FDA adapter requires at least one supported authorized field use.");
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
                var effectiveTime = ReadRequiredEffectiveTime(item);

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

                var sourceUrl = BuildRecordUri(sourceItemId).AbsoluteUri;
                var representedUses = fields.Keys
                    .Select(field => FieldUseByField[field])
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .OrderBy(use => use, StringComparer.Ordinal)
                    .ToList();
                var provenance = new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["labelId"] = SourceProvenanceValue.Present(sourceItemId),
                    ["effectiveTime"] =
                        SourceProvenanceValue.Present(effectiveTime),
                };
                AddOptionalPresentProvenance(
                    provenance,
                    item,
                    "set_id",
                    "labelSetId");
                AddOptionalPresentProvenance(
                    provenance,
                    item,
                    "version",
                    "labelVersion");

                var candidate = new SourceAcquisitionCandidate(
                    RequestId: intent.RequestId,
                    CompoundName: intent.CompoundName,
                    SourceRegistryId: "fda",
                    SourceItemId: sourceItemId,
                    SourceUrl: sourceUrl,
                    QueryUrl: requestUri.AbsoluteUri,
                    SourcePublicationOrUpdateDate: effectiveTime,
                    RetrievedAtUtc: retrievedAtUtc,
                    RightsReviewStatusAtRetrieval: "reviewed",
                    RegistryBindingSha256: intent.RegistryBindingSha256,
                    TransformationPipelineVersion: TransformationVersion,
                    HumanReviewStatus: "review-required",
                    EvidenceLimitations: EvidenceLimitations,
                    Fields: fields)
                {
                    AuthorizedFieldUses = representedUses,
                    SourceSpecificProvenance = provenance,
                    RightsAttributions = BuildRightsAttributions(
                        fields.Keys,
                        sourceUrl),
                    DocumentProvenance =
                    [
                        new SourceDocumentProvenance(
                            Title: $"openFDA drug label record {sourceItemId}",
                            Section: "Allowlisted openFDA drug-label fields",
                            PublishedDate: string.Empty,
                            UpdatedDate: effectiveTime),
                    ],
                    ReuseBoundary = new SourceReuseBoundary(
                        Acknowledgement:
                            "Source: U.S. FDA openFDA drug-label record. Label content is submitted by manufacturers or distributors; inclusion does not imply FDA verification, approval, or endorsement.",
                        ExcludedContentClasses:
                        [
                            "GMDN and other separately restricted data",
                            "photographs, media, and third-party content",
                            "copyrighted full text beyond the reviewed openFDA boundary",
                            "unallowlisted label sections and raw response bodies",
                            "individualized diagnosis, prescribing, dosing, or treatment guidance",
                        ],
                        NonEndorsementRequired: true),
                    ManualCaptureAudit = null,
                };

                SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
                    candidate,
                    intent.RequiredProvenanceFields,
                    "fda",
                    intent.RegistryBindingSha256);
                ValidateCandidateEnvelope(candidate);
                candidates.Add(candidate);
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

    private static IReadOnlyList<SourceRightsAttribution> BuildRightsAttributions(
        IEnumerable<string> emittedFields,
        string sourceUrl)
    {
        var fields = emittedFields
            .OrderBy(field => field, StringComparer.Ordinal)
            .ToList();
        var openFdaMetadataFields = fields
            .Where(field =>
                field.StartsWith("openfda.", StringComparison.OrdinalIgnoreCase)
                || field is "set_id" or "version")
            .ToList();
        var labelSectionFields = fields
            .Except(openFdaMetadataFields, StringComparer.OrdinalIgnoreCase)
            .ToList();
        var attributions = new List<SourceRightsAttribution>();

        if (openFdaMetadataFields.Count > 0)
        {
            attributions.Add(new SourceRightsAttribution(
                Scope:
                    "Allowlisted openFDA drug-label record metadata and identity or regulatory fields only.",
                Provider: "U.S. Food and Drug Administration (FDA) / openFDA",
                SourceUrl: sourceUrl,
                TermsUrl: OpenFdaTermsUrl,
                RightsStatus: "reviewed",
                CoveredFields: openFdaMetadataFields));
        }
        if (labelSectionFields.Count > 0)
        {
            attributions.Add(new SourceRightsAttribution(
                Scope:
                    "Allowlisted FDA drug-label sections submitted by the manufacturer or distributor.",
                Provider:
                    "U.S. Food and Drug Administration (FDA) / label submitter",
                SourceUrl: sourceUrl,
                TermsUrl: FdaWebsitePoliciesUrl,
                RightsStatus: "reviewed",
                CoveredFields: labelSectionFields));
        }

        return attributions;
    }

    internal static void ValidateCandidateEnvelope(
        SourceAcquisitionCandidate candidate)
    {
        ArgumentNullException.ThrowIfNull(candidate);

        if (!IsValidEffectiveTime(candidate.SourcePublicationOrUpdateDate)
            || !string.Equals(
                candidate.TransformationPipelineVersion,
                TransformationVersion,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "candidate-date-or-transformation-invalid",
                "FDA candidates require a valid effective_time and the exact transformation version.");
        }

        if (candidate.Fields is null || candidate.Fields.Count == 0)
        {
            throw new SourceAcquisitionException(
                "candidate-authorized-field-use-invalid",
                "FDA candidates require at least one allowlisted emitted field.");
        }
        var representedUses = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase);
        foreach (var field in candidate.Fields.Keys)
        {
            if (!FieldUseByField.TryGetValue(field, out var use))
            {
                throw new SourceAcquisitionException(
                    "candidate-authorized-field-use-invalid",
                    $"FDA candidate field '{field}' has no reviewed field-use mapping.");
            }
            representedUses.Add(use);
        }
        if (candidate.AuthorizedFieldUses is null
            || candidate.AuthorizedFieldUses.Count == 0
            || candidate.AuthorizedFieldUses.Count != representedUses.Count
            || candidate.AuthorizedFieldUses.Any(
                use => string.IsNullOrWhiteSpace(use))
            || !representedUses.SetEquals(candidate.AuthorizedFieldUses))
        {
            throw new SourceAcquisitionException(
                "candidate-authorized-field-use-invalid",
                "FDA candidate authorized field uses must exactly match emitted fields.");
        }

        var provenance = candidate.SourceSpecificProvenance;
        var provenanceValid = provenance is not null
                              && provenance.Count >= 2
                              && provenance.All(pair =>
                                  IsSubstantive(pair.Key)
                                  && pair.Value is not null
                                  && string.Equals(
                                      pair.Value.Availability,
                                      "present",
                                      StringComparison.Ordinal)
                                  && pair.Value.Values is not null
                                  && pair.Value.Values.Count > 0
                                  && pair.Value.Values.All(IsSubstantive)
                                  && string.IsNullOrWhiteSpace(
                                      pair.Value.UnavailableReason))
                              && HasExactProvenance(
                                  provenance,
                                  "labelId",
                                  candidate.SourceItemId)
                              && HasExactProvenance(
                                  provenance,
                                  "effectiveTime",
                                  candidate.SourcePublicationOrUpdateDate);
        if (!provenanceValid)
        {
            throw new SourceAcquisitionException(
                "candidate-source-provenance-invalid",
                "FDA candidate source-specific provenance is incomplete or non-substantive.");
        }

        var rights = candidate.RightsAttributions;
        if (rights is null
            || rights.Count == 0
            || rights.Any(attribution =>
                !IsSubstantive(attribution.Scope)
                || !IsSubstantive(attribution.Provider)
                || !IsSafeHttpsUri(attribution.SourceUrl)
                || !IsSafeHttpsUri(attribution.TermsUrl)
                || !string.Equals(
                    attribution.RightsStatus,
                    "reviewed",
                    StringComparison.Ordinal)
                || attribution.CoveredFields is null
                || attribution.CoveredFields.Count == 0
                || attribution.CoveredFields.Any(
                    field => !IsSubstantive(field))))
        {
            throw new SourceAcquisitionException(
                "candidate-rights-attribution-invalid",
                "FDA candidate rights attributions must be reviewed and substantive.");
        }

        var coveredFields = rights
            .SelectMany(attribution => attribution.CoveredFields)
            .ToList();
        var emittedFieldSet = candidate.Fields.Keys
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var coveredFieldSet = coveredFields
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (coveredFields.Count != emittedFieldSet.Count
            || coveredFieldSet.Count != emittedFieldSet.Count
            || !coveredFieldSet.SetEquals(emittedFieldSet))
        {
            throw new SourceAcquisitionException(
                "candidate-rights-covered-fields-mismatch",
                "FDA rights attributions must cover each emitted field exactly once.");
        }

        var boundary = candidate.ReuseBoundary;
        var exclusions = boundary?.ExcludedContentClasses;
        var boundaryValid = boundary is not null
                            && IsSubstantive(boundary.Acknowledgement)
                            && boundary.NonEndorsementRequired
                            && exclusions is not null
                            && exclusions.Count >= 5
                            && exclusions.All(IsSubstantive)
                            && ContainsExclusion(exclusions, "GMDN")
                            && ContainsExclusion(exclusions, "media")
                            && ContainsExclusion(exclusions, "third-party")
                            && ContainsExclusion(exclusions, "copyrighted full text")
                            && ContainsExclusion(exclusions, "individualized");
        if (!boundaryValid)
        {
            throw new SourceAcquisitionException(
                "candidate-reuse-boundary-invalid",
                "FDA candidates require the reviewed non-endorsement and exclusion boundary.");
        }

        if (candidate.ManualCaptureAudit is not null)
        {
            throw new SourceAcquisitionException(
                "candidate-manual-capture-audit-unexpected",
                "Automated FDA candidates cannot carry a manual-capture audit.");
        }

        if (candidate.DocumentProvenance is null
            || candidate.DocumentProvenance.Count != 1
            || candidate.DocumentProvenance.Any(document =>
                !IsSubstantive(document.Title)
                || !IsSubstantive(document.Section)
                || !string.IsNullOrWhiteSpace(document.PublishedDate)
                || !string.Equals(
                    document.UpdatedDate,
                    candidate.SourcePublicationOrUpdateDate,
                    StringComparison.Ordinal)))
        {
            throw new SourceAcquisitionException(
                "candidate-document-provenance-invalid",
                "FDA candidates require substantive label-document provenance.");
        }
    }

    private static bool HasExactProvenance(
        IReadOnlyDictionary<string, SourceProvenanceValue> provenance,
        string key,
        string expectedValue)
        => provenance.TryGetValue(key, out var value)
           && value.Values.Count == 1
           && string.Equals(
               value.Values[0],
               expectedValue,
               StringComparison.Ordinal);

    private static bool ContainsExclusion(
        IReadOnlyList<string> exclusions,
        string fragment)
        => exclusions.Any(exclusion =>
            exclusion.Contains(fragment, StringComparison.OrdinalIgnoreCase));

    private static bool IsSubstantive(string? value)
        => !string.IsNullOrWhiteSpace(value);

    private static bool IsSafeHttpsUri(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && string.Equals(
               uri.Scheme,
               Uri.UriSchemeHttps,
               StringComparison.OrdinalIgnoreCase)
           && uri.UserInfo.Length == 0;

    private static bool IsValidEffectiveTime(string? value)
        => value is not null
           && DateTime.TryParseExact(
               value,
               "yyyyMMdd",
               CultureInfo.InvariantCulture,
               DateTimeStyles.None,
               out _);

    private static string ReadRequiredEffectiveTime(JsonElement item)
    {
        if (!item.TryGetProperty("effective_time", out var property)
            || property.ValueKind != JsonValueKind.String
            || string.IsNullOrWhiteSpace(property.GetString()))
        {
            throw new SourceAcquisitionException(
                "source-publication-date-missing",
                "openFDA result is missing required field 'effective_time'.");
        }

        var value = property.GetString()!.Trim();
        if (!IsValidEffectiveTime(value))
        {
            throw new SourceAcquisitionException(
                "source-publication-date-invalid",
                "openFDA effective_time must be a valid calendar date in yyyyMMdd format.");
        }
        return value;
    }

    private static void AddOptionalPresentProvenance(
        IDictionary<string, SourceProvenanceValue> provenance,
        JsonElement item,
        string propertyName,
        string outputName)
    {
        var value = ReadOptionalString(item, propertyName);
        if (value.Length > 0)
        {
            provenance[outputName] = SourceProvenanceValue.Present(value);
        }
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
