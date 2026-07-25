namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Text.RegularExpressions;

public sealed record NccihManualReviewCapture(
    string SourceUrl,
    string PageTitle,
    string SourceItemSlug,
    string PageUpdatedDate,
    string Section,
    IReadOnlyDictionary<string, IReadOnlyList<string>> Fields,
    SourceManualCaptureAudit Audit);

public sealed class NccihManualReviewCandidateWorkflow
{
    public const string SourceId = "nih-nccih";
    public const string PlanningAdapterId = "nih-nccih-planning-v1";
    public const string TransformationVersion = "nccih-manual-review-v1";
    public const string CanonicalSourceUrl =
        "https://www.nccih.nih.gov/health/melatonin-what-you-need-to-know";
    public const string CanonicalPageTitle =
        "Melatonin: What You Need To Know";
    public const string CanonicalSourceItemSlug =
        "melatonin-what-you-need-to-know";

    private const int MaximumSectionLength = 200;
    private const int MaximumCapturedValueLength = 1_000;
    private const int MaximumValuesPerField = 8;
    private const int MaximumAuditNotes = 8;
    private const int MaximumAuditNoteLength = 500;
    private const int MaximumActorIdLength = 128;

    private static readonly Regex LinkLikeText = new(
        @"(?:\b[a-z][a-z0-9+.-]*:(?=\S)|\bwww\.|(?<![\p{L}\p{N}_-])/(?:[a-z0-9._~!$&'()+,;=:@%-]+/)*[a-z0-9._~!$&'()+,;=:@%-]+(?:[?#]\S*)?|(?<![\p{L}\p{N}_-])(?:[a-z0-9](?:[a-z0-9-]{0,61}[a-z0-9])?\.)+(?:[a-z]{2,63}|xn--[a-z0-9-]{2,59})(?=$|[^\p{L}\p{N}_-]))",
        RegexOptions.CultureInvariant | RegexOptions.IgnoreCase,
        TimeSpan.FromMilliseconds(100));

    private static readonly Regex MarkdownListPrefix = new(
        @"^(?:[-+*]\s+|\d{1,9}[.)]\s+)",
        RegexOptions.CultureInvariant,
        TimeSpan.FromMilliseconds(100));

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

    private static readonly IReadOnlyDictionary<string, string> FieldUseByKey =
        new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["identity_context"] = "identity",
            ["mechanism_context"] = "mechanism",
            ["efficacy_context"] = "efficacy-claims",
            ["interaction_context"] = "interactions",
        };

    private static readonly HashSet<string> SupportedFieldUses =
        new(
            FieldUseByKey.Values,
            StringComparer.OrdinalIgnoreCase);

    private static readonly HashSet<string> ExactMelatoninSearchTerms =
        new(
            [
                "Melatonin",
                "N-acetyl-5-methoxytryptamine",
            ],
            StringComparer.OrdinalIgnoreCase);

    private static readonly string[] EvidenceLimitations =
    [
        "NCCIH consumer-health information is educational context, not a product label, individualized medical direction, or proof that a product is safe or effective.",
        "Manually captured statements remain source-authored page context and require claim-level evidence review before canonical promotion.",
        "NCCIH-authored text must remain distinguishable from photographs, illustrations, linked resources, and separately copyrighted third-party material.",
    ];

    private readonly string _expectedRegistrySha256;
    private readonly SourceAcquisitionIntentRequirements _intentRequirements;

    public NccihManualReviewCandidateWorkflow(string expectedRegistrySha256)
    {
        _expectedRegistrySha256 =
            SourceAcquisitionIntentGuard.RequireLowercaseSha256(
                expectedRegistrySha256,
                nameof(expectedRegistrySha256));
        _intentRequirements = new SourceAcquisitionIntentRequirements(
            SourceId,
            "NIH NCCIH",
            PlanningAdapterId,
            CandidateMethod: "manual-review",
            ExpectedRegistrySha256: _expectedRegistrySha256,
            RequiredProvenanceFields);
    }

    public SourceAcquisitionBatch CreateCandidate(
        SourceAcquisitionIntent intent,
        NccihManualReviewCapture? capture,
        DateTimeOffset retrievedAtUtc)
    {
        SourceAcquisitionIntentGuard.Validate(
            intent,
            retrievedAtUtc,
            _intentRequirements);

        if (!IsExactMelatoninTarget(intent))
        {
            return new SourceAcquisitionBatch(
                SourceAcquisitionBatchStatus.NoMatch,
                Array.Empty<SourceAcquisitionCandidate>(),
                Truncated: false,
                RetryAfter: null);
        }

        if (capture is null)
        {
            throw new SourceAcquisitionException(
                "manual-capture-required",
                "The mapped NCCIH target requires an approved manual capture.");
        }

        ValidatePageIdentity(capture);
        ValidateAuditInput(capture.Audit);
        var pageUpdatedDate = RequireNormalizedDate(
            capture.PageUpdatedDate,
            "pageUpdatedDate");
        var section = RequirePlainText(
            capture.Section,
            "section",
            MaximumSectionLength);
        var authorizedUses = (intent.AuthorizedFieldUses
                              ?? Array.Empty<string>())
            .Where(SupportedFieldUses.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (authorizedUses.Count == 0)
        {
            throw new SourceAcquisitionException(
                "authorized-field-use-empty",
                "NCCIH manual capture requires an authorized identity, mechanism, efficacy-claims, or interactions field use.");
        }

        var fields = ValidateCapturedFields(capture.Fields, authorizedUses);
        var candidateUses = fields.Keys
            .Select(key => FieldUseByKey[key])
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.Ordinal)
            .ToList();
        var provenance =
            new Dictionary<string, SourceProvenanceValue>(
                StringComparer.OrdinalIgnoreCase)
            {
                ["pageTitle"] =
                    SourceProvenanceValue.Present(CanonicalPageTitle),
                ["section"] = SourceProvenanceValue.Present(section),
                ["pageUpdatedDate"] =
                    SourceProvenanceValue.Present(pageUpdatedDate),
                ["sourceItemSlug"] =
                    SourceProvenanceValue.Present(CanonicalSourceItemSlug),
                ["captureMethod"] =
                    SourceProvenanceValue.Present("manual-review"),
            };

        var candidate = new SourceAcquisitionCandidate(
            RequestId: intent.RequestId,
            CompoundName: "Melatonin",
            SourceRegistryId: SourceId,
            SourceItemId: CanonicalSourceItemSlug,
            SourceUrl: CanonicalSourceUrl,
            QueryUrl: null,
            SourcePublicationOrUpdateDate: pageUpdatedDate,
            RetrievedAtUtc: retrievedAtUtc,
            RightsReviewStatusAtRetrieval: "reviewed",
            RegistryBindingSha256: intent.RegistryBindingSha256,
            TransformationPipelineVersion: TransformationVersion,
            HumanReviewStatus: "review-required",
            EvidenceLimitations: EvidenceLimitations,
            Fields: fields)
        {
            AuthorizedFieldUses = candidateUses,
            SourceSpecificProvenance = provenance,
            RightsAttributions =
            [
                new SourceRightsAttribution(
                    Scope:
                        "Manually captured NCCIH-authored public-domain text from the allowlisted page; photographs, illustrations, and separately copyrighted third-party material are excluded.",
                    Provider:
                        "NIH National Center for Complementary and Integrative Health (NCCIH)",
                    SourceUrl: CanonicalSourceUrl,
                    TermsUrl:
                        "https://www.nccih.nih.gov/tools/privacy",
                    RightsStatus: "reviewed",
                    CoveredFields: fields.Keys
                        .OrderBy(key => key, StringComparer.Ordinal)
                        .ToList()),
            ],
            DocumentProvenance =
            [
                new SourceDocumentProvenance(
                    Title: CanonicalPageTitle,
                    Section: section,
                    PublishedDate: string.Empty,
                    UpdatedDate: pageUpdatedDate),
            ],
            ReuseBoundary = new SourceReuseBoundary(
                Acknowledgement:
                    "Source: NIH National Center for Complementary and Integrative Health (NCCIH). BioStack grouped independently reviewed, source-authored text into labeled evidence-context fields; NCCIH does not endorse BioStack or its use of the material.",
                ExcludedContentClasses:
                [
                    "photographs and illustrations",
                    "videos, logos, and trademarks",
                    "linked external resources",
                    "separately copyrighted third-party material",
                    "individualized advice, dosing direction, regulatory claims, and safety-critical conclusions",
                ],
                NonEndorsementRequired: true),
            ManualCaptureAudit = capture.Audit,
        };

        SourceAcquisitionCandidateGuard.ValidateRequiredProvenance(
            candidate,
            RequiredProvenanceFields,
            expectedSourceRegistryId: SourceId,
            expectedRegistrySha256: _expectedRegistrySha256);
        SourceAcquisitionCandidateGuard.ValidateApprovedManualCaptureAudit(
            candidate);

        return new SourceAcquisitionBatch(
            SourceAcquisitionBatchStatus.Completed,
            [candidate],
            Truncated: false,
            RetryAfter: null);
    }

    private static bool IsExactMelatoninTarget(SourceAcquisitionIntent intent)
    {
        if (!string.Equals(
                intent.CompoundName?.Trim(),
                "Melatonin",
                StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        var terms = (intent.SearchTerms ?? Array.Empty<string>())
            .Select(term => term?.Trim() ?? string.Empty)
            .Where(term => term.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        return terms.Count > 0
               && terms.Contains(
                   "Melatonin",
                   StringComparer.OrdinalIgnoreCase)
               && terms.All(ExactMelatoninSearchTerms.Contains);
    }

    private static void ValidatePageIdentity(NccihManualReviewCapture capture)
    {
        if (!string.Equals(
                capture.SourceUrl,
                CanonicalSourceUrl,
                StringComparison.Ordinal)
            || !string.Equals(
                capture.PageTitle,
                CanonicalPageTitle,
                StringComparison.Ordinal)
            || !string.Equals(
                capture.SourceItemSlug,
                CanonicalSourceItemSlug,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "manual-capture-page-not-allowlisted",
                "The NCCIH manual capture does not match the exact allowlisted page identity.");
        }
    }

    private static Dictionary<string, IReadOnlyList<string>>
        ValidateCapturedFields(
            IReadOnlyDictionary<string, IReadOnlyList<string>> fields,
            IReadOnlySet<string> authorizedUses)
    {
        if (fields is null || fields.Count is < 1 or > 4)
        {
            throw new SourceAcquisitionException(
                "manual-capture-fields-invalid",
                "NCCIH manual capture requires between one and four allowlisted fields.");
        }

        var normalized =
            new Dictionary<string, IReadOnlyList<string>>(
                StringComparer.Ordinal);
        foreach (var pair in fields)
        {
            if (pair.Key is null
                || !FieldUseByKey.TryGetValue(pair.Key, out var fieldUse)
                || !authorizedUses.Contains(fieldUse)
                || pair.Value is null
                || pair.Value.Count is < 1 or > MaximumValuesPerField)
            {
                throw new SourceAcquisitionException(
                    "manual-capture-field-not-authorized",
                    "NCCIH manual capture contains an unknown, unauthorized, empty, or oversized field.");
            }

            var values = pair.Value
                .Select(value => RequirePlainText(
                    value,
                    pair.Key,
                    MaximumCapturedValueLength))
                .ToList();
            if (values.Distinct(StringComparer.Ordinal).Count() != values.Count)
            {
                throw new SourceAcquisitionException(
                    "manual-capture-field-duplicate",
                    "NCCIH manual capture contains a duplicate source-authored value.");
            }
            normalized.Add(pair.Key, values);
        }
        return normalized;
    }

    private static void ValidateAuditInput(SourceManualCaptureAudit audit)
    {
        if (audit is null
            || !IsBoundedActorId(audit.OperatorId)
            || !IsBoundedActorId(audit.ReviewerId)
            || audit.Notes is null
            || audit.Notes.Count is < 1 or > MaximumAuditNotes
            || audit.Notes.Any(note =>
                !IsPlainText(note, MaximumAuditNoteLength)))
        {
            throw new SourceAcquisitionException(
                "manual-capture-audit-input-invalid",
                "NCCIH manual capture audit identifiers and notes must be bounded, substantive plain text.");
        }
    }

    private static bool IsBoundedActorId(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && value.Length <= MaximumActorIdLength
           && value == value.Trim()
           && value.All(character =>
               char.IsLetterOrDigit(character)
               || character is '-' or '_' or '.' or '@');

    private static string RequireNormalizedDate(
        string value,
        string fieldName)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value != value.Trim()
            || !DateTime.TryParseExact(
                value,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out _))
        {
            throw new SourceAcquisitionException(
                "manual-capture-date-invalid",
                $"NCCIH manual capture field '{fieldName}' must be an exact normalized date.");
        }
        return value;
    }

    private static string RequirePlainText(
        string value,
        string fieldName,
        int maximumLength)
    {
        if (!IsPlainText(value, maximumLength))
        {
            throw new SourceAcquisitionException(
                "manual-capture-text-invalid",
                $"NCCIH manual capture field '{fieldName}' must be bounded, substantive plain text without markup or URLs.");
        }
        return value;
    }

    private static bool IsPlainText(string? value, int maximumLength)
    {
        if (string.IsNullOrWhiteSpace(value)
            || value.Length > maximumLength
            || value != value.Trim()
            || value.Any(char.IsControl)
            || value.IndexOf(
                "http://",
                StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf(
                "https://",
                StringComparison.OrdinalIgnoreCase) >= 0
            || value.IndexOf(
                "www.",
                StringComparison.OrdinalIgnoreCase) >= 0
            || LinkLikeText.IsMatch(value)
            || MarkdownListPrefix.IsMatch(value))
        {
            return false;
        }

        return value.All(character =>
            character is not '<'
            and not '>'
            and not '['
            and not ']'
            and not '{'
            and not '}'
            and not '`'
            and not '*'
            and not '_'
            and not '#'
            and not '~'
            and not '|');
    }
}
