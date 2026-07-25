namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Xml;
using System.Xml.Linq;

public sealed class NihOdsFactSheetAcquisitionAdapter : ISourceAcquisitionAdapter
{
    public const string TransformationVersion =
        "nih-ods-fact-sheet-strict-inline-v1";
    public const string PlanningAdapterId = "nih-ods-planning-v1";
    public const string FixedEndpoint =
        "https://ods.od.nih.gov/api/?outputformat=XML&readinglevel=Health%20Professional&resourcename=ImmuneFunction";
    public const int DefaultMaximumResponseBytes = 1024 * 1024;
    public const string TargetSection = "N-acetylcysteine and Glutathione";

    private const string FactsheetNamespace = "http://tempuri.org/factsheet.xsd";
    private const string ExpectedPagePath =
        "/factsheets/ImmuneFunction-HealthProfessional/";
    private const string ExpectedPageTitle =
        "Dietary Supplements for Immune Function and Infectious Diseases";
    private const int MaximumExcerptCharacters = 4_000;
    private const int MaximumTotalExcerptCharacters = 12_000;

    private static readonly Uri FixedEndpointUri = new(FixedEndpoint, UriKind.Absolute);
    private static readonly ISourceRequestGate SharedRequestGate =
        new SerializedSourceRequestGate(
            TimeProvider.System,
            Array.Empty<SourceSlidingWindowBudget>(),
            dailyBudget: null);

    private static readonly string[] RequiredProvenanceFields =
    [
        "sourceRegistryId",
        "sourceItemId",
        "sourceUrl",
        "pageTitle",
        "section",
        "pageUpdatedDate",
        "retrievedAtUtc",
        "rightsReviewStatusAtRetrieval",
        "transformationPipelineVersion",
        "humanReviewStatus",
    ];

    private static readonly HashSet<string> ApprovedFieldUses =
        new(
            ["identity", "mechanism", "efficacy-claims", "interactions"],
            StringComparer.OrdinalIgnoreCase);

    private static readonly string[] EvidenceLimitations =
    [
        "NIH ODS fact sheets are educational medical summaries, not product labels, and do not independently support product-specific dosing or safety-critical conclusions.",
        "The retained text consists only of bounded, complete source-authored paragraphs from one reviewed section; paragraphs containing nonallowlisted inline markup are omitted whole, and the result must not be presented as diagnosis, prescription, individualized treatment direction, or a guarantee of safety or efficacy.",
        "Claim-level evidence review is required before canonical promotion, including review against underlying studies or labels where the claim risk warrants it.",
    ];

    private static readonly HashSet<string> AllowedInlineElementNames =
        new(
            [
                "em",
                "strong",
                "sup",
            ],
            StringComparer.Ordinal);

    private readonly HttpClient _httpClient;
    private readonly string _expectedRegistrySha256;
    private readonly int _maximumResponseBytes;
    private readonly ISourceRequestGate _requestGate;

    public NihOdsFactSheetAcquisitionAdapter(
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

    internal NihOdsFactSheetAcquisitionAdapter(
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
    }

    public string SourceId => "nih-ods";
    public string AdapterId => TransformationVersion;

    public async Task<SourceAcquisitionBatch> AcquireAsync(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        CancellationToken cancellationToken = default)
    {
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            new SourceAcquisitionIntentRequirements(
                SourceId,
                "NIH ODS",
                PlanningAdapterId,
                "api",
                _expectedRegistrySha256,
                RequiredProvenanceFields));

        if (!MatchesGovernedCatalog(intent))
        {
            return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch);
        }

        ValidateFixedEndpoint(FixedEndpointUri);
        using var requestLease = await _requestGate.AcquireAsync(cancellationToken);
        using var request = new HttpRequestMessage(HttpMethod.Get, FixedEndpointUri);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/xml"));
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/xml"));
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
                "NIH ODS redirects are not accepted by this adapter.");
        }
        if (response.StatusCode == HttpStatusCode.Forbidden)
        {
            throw new SourceAcquisitionException(
                "access-denied",
                "NIH ODS denied the anonymous request; browser, cookie, and challenge bypasses are not attempted.");
        }
        if (response.StatusCode != HttpStatusCode.OK)
        {
            throw new SourceAcquisitionException(
                $"http-{(int)response.StatusCode}",
                $"NIH ODS returned HTTP {(int)response.StatusCode}.");
        }

        var mediaType = response.Content.Headers.ContentType?.MediaType;
        if (!string.Equals(mediaType, "application/xml", StringComparison.OrdinalIgnoreCase)
            && !string.Equals(mediaType, "text/xml", StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "unexpected-content-type",
                "NIH ODS response content type must be application/xml or text/xml.");
        }

        var body = await SourceAcquisitionHttpTransport.ReadBoundedBodyAsync(
            response.Content,
            _maximumResponseBytes,
            "NIH ODS",
            cancellationToken);
        return ParseResponse(body, intent, retrievedAtUtc);
    }

    private SourceAcquisitionBatch ParseResponse(
        byte[] body,
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc)
    {
        var document = LoadHardenedXml(body, "malformed-xml");
        XNamespace ns = FactsheetNamespace;
        if (document.Root?.Name != ns + "Factsheet")
        {
            throw new SourceAcquisitionException(
                "unexpected-xml-root",
                "NIH ODS response must use the reviewed Factsheet root and namespace.");
        }

        RejectUnexpectedTopLevelShape(document.Root, ns);
        var fsid = ReadRequiredElement(document.Root, ns + "FSID", "factsheet-id-missing");
        if (!ulong.TryParse(
                fsid,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var numericFsid)
            || numericFsid == 0
            || !string.Equals(
                fsid,
                numericFsid.ToString(CultureInfo.InvariantCulture),
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "factsheet-id-invalid",
                "NIH ODS FSID must be a canonical positive integer.");
        }
        var languageCode = ReadRequiredElement(
            document.Root,
            ns + "LanguageCode",
            "language-code-missing");
        if (!string.Equals(languageCode, "en", StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "language-not-supported",
                "The governed NIH ODS mapping accepts the English fact sheet only.");
        }

        var pageUpdatedDate = ReadRequiredElement(
            document.Root,
            ns + "Reviewed",
            "page-updated-date-missing");
        if (!DateOnly.TryParseExact(
                pageUpdatedDate,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new SourceAcquisitionException(
                "page-updated-date-invalid",
                "NIH ODS Reviewed must be an ISO calendar date.");
        }

        var sourceUrl = ReadAndValidateSourceUrl(
            ReadRequiredElement(document.Root, ns + "URL", "source-url-missing"));
        var pageTitle = ReadRequiredElement(
            document.Root,
            ns + "Title",
            "page-title-missing");
        if (!string.Equals(pageTitle, ExpectedPageTitle, StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "page-title-mismatch",
                "NIH ODS returned a page outside the fixed governed mapping.");
        }

        var content = ReadRequiredElement(
            document.Root,
            ns + "Content",
            "content-missing",
            trim: false);
        var section = ExtractTargetSection(content, intent.AuthorizedFieldUses);
        if (section.Fields.Count == 0)
        {
            return EmptyBatch(SourceAcquisitionBatchStatus.NoMatch);
        }

        var authorizedUses = intent.AuthorizedFieldUses
            .Where(use =>
                ApprovedFieldUses.Contains(use)
                && (use.Equals("efficacy-claims", StringComparison.OrdinalIgnoreCase)
                    && section.Fields.ContainsKey("efficacy_claim_source_excerpt")
                    || (use.Equals("identity", StringComparison.OrdinalIgnoreCase)
                        || use.Equals("mechanism", StringComparison.OrdinalIgnoreCase))
                    && section.Fields.ContainsKey(
                        "identity_mechanism_source_excerpt")))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var coveredFields = section.Fields.Keys.ToList();
        var candidate = new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: intent.CompoundName,
            SourceRegistryId: SourceId,
            SourceItemId:
                $"ods-factsheet-{fsid}-n-acetylcysteine-and-glutathione",
            SourceUrl: sourceUrl,
            QueryUrl: FixedEndpoint,
            SourcePublicationOrUpdateDate: pageUpdatedDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: section.Fields)
        {
            AuthorizedFieldUses = authorizedUses,
            SourceSpecificProvenance =
                new Dictionary<string, SourceProvenanceValue>(
                    StringComparer.OrdinalIgnoreCase)
                {
                    ["pageTitle"] = SourceProvenanceValue.Present(pageTitle),
                    ["section"] = SourceProvenanceValue.Present(TargetSection),
                    ["pageUpdatedDate"] =
                        SourceProvenanceValue.Present(pageUpdatedDate),
                },
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope: "ods-authored-public-domain-text",
                    Provider: "NIH Office of Dietary Supplements",
                    SourceUrl: sourceUrl,
                    TermsUrl:
                        "https://ods.od.nih.gov/HealthInformation/ODS_Frequently_Asked_Questions/",
                    RightsStatus: "reviewed-public-domain-with-acknowledgement",
                    CoveredFields: coveredFields),
            ],
            DocumentProvenance =
            [
                new SourceDocumentProvenance(
                    Title: pageTitle,
                    Section: TargetSection,
                    PublishedDate: string.Empty,
                    UpdatedDate: pageUpdatedDate),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    "Acknowledgement: Source is the NIH Office of Dietary Supplements. Each retained paragraph is kept whole under the strict inline-markup allowlist; affected paragraphs are omitted rather than selectively redacted, and only surrounding whitespace is normalized.",
                ExcludedContentClasses:
                [
                    "images and logos",
                    "tables and figures",
                    "references and bibliography entries",
                    "links and linked text",
                    "third-party or separately copyrighted material",
                    "safety subsection outside the approved field-use intersection",
                    "entire paragraphs containing links, images, tables, unknown markup, namespaced markup, or attributed inline elements",
                ],
                NonEndorsementRequired: true),
        };

        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            RequiredProvenanceFields,
            SourceId,
            _expectedRegistrySha256);
        return new SourceAcquisitionBatch(
            SourceAcquisitionBatchStatus.Completed,
            [candidate],
            Truncated: section.Truncated,
            RetryAfter: null);
    }

    private static XDocument LoadHardenedXml(byte[] body, string malformedCode)
    {
        try
        {
            using var stream = new MemoryStream(body, writable: false);
            using var reader = XmlReader.Create(
                stream,
                new XmlReaderSettings
                {
                    DtdProcessing = DtdProcessing.Prohibit,
                    XmlResolver = null,
                    MaxCharactersFromEntities = 0,
                    MaxCharactersInDocument = DefaultMaximumResponseBytes,
                    IgnoreComments = false,
                    IgnoreProcessingInstructions = false,
                });
            return XDocument.Load(reader, LoadOptions.PreserveWhitespace);
        }
        catch (XmlException exception)
        {
            throw new SourceAcquisitionException(
                malformedCode,
                $"NIH ODS returned unsafe or malformed XML: {exception.Message}");
        }
    }

    private static void RejectUnexpectedTopLevelShape(XElement root, XNamespace ns)
    {
        XName[] orderedNames =
        [
            ns + "FSID",
            ns + "LanguageCode",
            ns + "Reviewed",
            ns + "URL",
            ns + "Title",
            ns + "ShortTitle",
            ns + "Content",
            ns + "References",
            ns + "Glossary",
            ns + "Disclaimer",
            ns + "PDF",
            ns + "ODSLogo",
            ns + "Analytics",
        ];
        var elements = root.Elements().ToList();
        var lastIndex = -1;
        var ordered = true;
        foreach (var element in elements)
        {
            var index = Array.IndexOf(orderedNames, element.Name);
            if (index < 0 || index <= lastIndex)
            {
                ordered = false;
                break;
            }
            lastIndex = index;
        }

        var required = new[]
        {
            ns + "FSID",
            ns + "LanguageCode",
            ns + "Reviewed",
            ns + "URL",
            ns + "Title",
            ns + "Content",
        };
        if (!ordered
            || root.Attributes().Any(attribute => !attribute.IsNamespaceDeclaration)
            || required.Any(name => elements.Count(element => element.Name == name) != 1))
        {
            throw new SourceAcquisitionException(
                "unexpected-xml-shape",
                "NIH ODS response did not match the reviewed fact-sheet schema shape.");
        }
    }

    private static ExtractedSection ExtractTargetSection(
        string content,
        IReadOnlyList<string> requestedUses)
    {
        var wrapped = Encoding.UTF8.GetBytes($"<ods-content>{content}</ods-content>");
        var fragment = LoadHardenedXml(wrapped, "unsafe-or-malformed-content");
        var elements = fragment.Root?.Elements().ToList()
                       ?? throw new SourceAcquisitionException(
                           "content-missing",
                           "NIH ODS Content was empty.");

        var targetIndex = elements.FindIndex(
            element => IsHeading(element, "h3", TargetSection));
        if (targetIndex < 0
            || elements.Skip(targetIndex + 1)
                .Any(element => IsHeading(element, "h3", TargetSection)))
        {
            throw new SourceAcquisitionException(
                "target-section-missing-or-ambiguous",
                "NIH ODS Content must contain exactly one governed target section.");
        }

        var requested = (requestedUses ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allowIntro = requested.Contains("identity") || requested.Contains("mechanism");
        var allowEfficacy = requested.Contains("efficacy-claims");
        var intro = new List<string>();
        var efficacy = new List<string>();
        var mode = SectionMode.Introduction;
        var truncated = false;

        for (var index = targetIndex + 1; index < elements.Count; index++)
        {
            var element = elements[index];
            if (IsHeadingLevel(element, "h3")) break;
            if (IsHeading(element, "h4", "Efficacy"))
            {
                mode = SectionMode.Efficacy;
                continue;
            }
            if (IsHeading(element, "h4", "Safety"))
            {
                mode = SectionMode.Excluded;
                continue;
            }
            if (IsHeadingLevel(element, "h4"))
            {
                mode = SectionMode.Excluded;
                continue;
            }
            if (!string.Equals(
                    element.Name.LocalName,
                    "p",
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (!TryReadAllowedParagraphText(element, out var text))
            {
                truncated = true;
                continue;
            }
            if (text.Length == 0) continue;
            if (text.Length > MaximumExcerptCharacters)
            {
                throw new SourceAcquisitionException(
                    "section-excerpt-too-large",
                    "NIH ODS target section contained a paragraph beyond the excerpt limit.");
            }

            if (mode == SectionMode.Introduction && allowIntro)
            {
                if (intro.Count >= 4) truncated = true;
                else intro.Add(text);
            }
            else if (mode == SectionMode.Efficacy && allowEfficacy)
            {
                if (efficacy.Count >= 3) truncated = true;
                else efficacy.Add(text);
            }
        }

        if (intro.Sum(value => value.Length) + efficacy.Sum(value => value.Length)
            > MaximumTotalExcerptCharacters)
        {
            throw new SourceAcquisitionException(
                "section-excerpt-too-large",
                "NIH ODS target section exceeded the total excerpt limit.");
        }

        var fields = new Dictionary<string, IReadOnlyList<string>>(
            StringComparer.OrdinalIgnoreCase);
        if (intro.Count > 0)
        {
            fields["identity_mechanism_source_excerpt"] = intro;
        }
        if (efficacy.Count > 0)
        {
            fields["efficacy_claim_source_excerpt"] = efficacy;
        }
        return new ExtractedSection(fields, truncated);
    }

    private static bool TryReadAllowedParagraphText(
        XElement paragraph,
        out string text)
    {
        text = string.Empty;
        if (paragraph.Name.NamespaceName.Length > 0
            || !string.Equals(
                paragraph.Name.LocalName,
                "p",
                StringComparison.Ordinal))
        {
            return false;
        }

        var builder = new StringBuilder();
        if (!TryAppendAllowedNodes(paragraph.Nodes(), builder))
        {
            return false;
        }
        text = NormalizeWhitespace(builder.ToString());
        return true;
    }

    private static bool TryAppendAllowedNodes(
        IEnumerable<XNode> nodes,
        StringBuilder builder)
    {
        foreach (var node in nodes)
        {
            if (node is XText text && node is not XCData)
            {
                builder.Append(text.Value);
                continue;
            }
            if (node is XElement element)
            {
                if (element.Name.NamespaceName.Length > 0
                    || !AllowedInlineElementNames.Contains(element.Name.LocalName)
                    || element.HasAttributes
                    || !TryAppendAllowedNodes(element.Nodes(), builder))
                {
                    return false;
                }
                continue;
            }
            return false;
        }
        return true;
    }

    private static string NormalizeWhitespace(string value)
    {
        var builder = new StringBuilder(value.Length);
        var pendingSpace = false;
        foreach (var character in value)
        {
            if (char.IsWhiteSpace(character))
            {
                pendingSpace = builder.Length > 0;
                continue;
            }
            if (pendingSpace)
            {
                builder.Append(' ');
                pendingSpace = false;
            }
            builder.Append(character);
        }
        return builder.ToString().Trim();
    }

    private static bool IsHeading(XElement element, string level, string text)
        => IsHeadingLevel(element, level)
           && string.Equals(
               NormalizeWhitespace(element.Value),
               text,
               StringComparison.Ordinal);

    private static bool IsHeadingLevel(XElement element, string level)
        => element.Name.NamespaceName.Length == 0
           && string.Equals(
               element.Name.LocalName,
               level,
               StringComparison.Ordinal);

    private static bool MatchesGovernedCatalog(SourceAcquisitionIntent intent)
    {
        if (!string.Equals(
                intent.CompoundName,
                "Glutathione",
                StringComparison.Ordinal))
        {
            return false;
        }
        return intent.SearchTerms is { Count: 1 }
               && string.Equals(
                   intent.SearchTerms[0],
                   "Glutathione",
                   StringComparison.Ordinal);
    }

    private static void ValidateFixedEndpoint(Uri uri)
    {
        var expectedQuery =
            "?outputformat=XML&readinglevel=Health%20Professional&resourcename=ImmuneFunction";
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.Ordinal)
            || !string.Equals(uri.Host, "ods.od.nih.gov", StringComparison.Ordinal)
            || !string.Equals(uri.AbsolutePath, "/api/", StringComparison.Ordinal)
            || !string.Equals(uri.Query, expectedQuery, StringComparison.Ordinal)
            || uri.UserInfo.Length > 0
            || !uri.IsDefaultPort)
        {
            throw new SourceAcquisitionException(
                "fixed-endpoint-invalid",
                "NIH ODS fixed endpoint failed its first-party boundary check.");
        }
    }

    private static string ReadAndValidateSourceUrl(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri)
            || !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.Host, "ods.od.nih.gov", StringComparison.OrdinalIgnoreCase)
            || !string.Equals(uri.AbsolutePath, ExpectedPagePath, StringComparison.Ordinal)
            || uri.UserInfo.Length > 0
            || uri.Port != 443
            || uri.Query.Length > 0
            || uri.Fragment.Length > 0)
        {
            throw new SourceAcquisitionException(
                "source-url-invalid",
                "NIH ODS returned a source URL outside the governed fact-sheet page.");
        }
        return $"https://ods.od.nih.gov{ExpectedPagePath}";
    }

    private static string ReadRequiredElement(
        XElement root,
        XName name,
        string code,
        bool trim = true)
    {
        var element = root.Element(name);
        var value = element?.Value ?? string.Empty;
        if (trim) value = value.Trim();
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new SourceAcquisitionException(
                code,
                $"NIH ODS response is missing required field '{name.LocalName}'.");
        }
        return value;
    }

    private static SourceAcquisitionBatch EmptyBatch(
        SourceAcquisitionBatchStatus status,
        string? retryAfter = null)
        => new(status, Array.Empty<SourceAcquisitionCandidate>(), false, retryAfter);

    private enum SectionMode
    {
        Introduction,
        Efficacy,
        Excluded,
    }

    private sealed record ExtractedSection(
        IReadOnlyDictionary<string, IReadOnlyList<string>> Fields,
        bool Truncated);
}
