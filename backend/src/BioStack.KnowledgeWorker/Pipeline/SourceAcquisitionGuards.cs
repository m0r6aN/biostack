namespace BioStack.KnowledgeWorker.Pipeline;

internal sealed record SourceAcquisitionIntentRequirements(
    string SourceId,
    string SourceDisplayName,
    string PlanningAdapterId,
    string CandidateMethod,
    string ExpectedRegistrySha256,
    IReadOnlyList<string> RequiredProvenanceFields);

internal static class SourceAcquisitionIntentGuard
{
    public static void Validate(
        SourceAcquisitionIntent intent,
        DateTimeOffset retrievedAtUtc,
        SourceAcquisitionIntentRequirements requirements)
    {
        if (intent is null) throw new ArgumentNullException(nameof(intent));
        if (requirements is null) throw new ArgumentNullException(nameof(requirements));

        if (intent.Disposition != SourceAcquisitionDisposition.Ready
            || intent.BlockingReasons is null
            || intent.BlockingReasons.Count > 0)
        {
            throw new SourceAcquisitionException(
                "intent-not-ready",
                "Only blocker-free Ready intents may be acquired.");
        }
        if (!string.Equals(
                intent.SourceId,
                requirements.SourceId,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "source-not-supported",
                $"This adapter accepts {requirements.SourceDisplayName} intents only.");
        }
        if (!string.Equals(
                intent.AdapterId,
                requirements.PlanningAdapterId,
                StringComparison.Ordinal))
        {
            throw new SourceAcquisitionException(
                "planning-adapter-mismatch",
                $"This adapter requires planning adapter '{requirements.PlanningAdapterId}'.");
        }
        if (!string.Equals(
                intent.CandidateMethod,
                requirements.CandidateMethod,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new SourceAcquisitionException(
                "acquisition-method-not-supported",
                $"This adapter accepts {requirements.CandidateMethod.ToUpperInvariant()} acquisition intents only.");
        }
        if (!string.Equals(
                intent.RegistryBindingSha256,
                requirements.ExpectedRegistrySha256,
                StringComparison.Ordinal)
            || !IsLowercaseSha256(intent.RegistryBindingSha256)
            || !IsLowercaseSha256(requirements.ExpectedRegistrySha256))
        {
            throw new SourceAcquisitionException(
                "source-registry-sha256-mismatch",
                "The acquisition intent is not bound to the expected source registry.");
        }
        if (retrievedAtUtc == default
            || retrievedAtUtc.Offset != TimeSpan.Zero)
        {
            throw new SourceAcquisitionException(
                "retrieval-timestamp-not-utc",
                "The retrieval timestamp must use a UTC offset.");
        }

        var providedProvenance = (intent.RequiredProvenanceFields
                                  ?? Array.Empty<string>())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (requirements.RequiredProvenanceFields is null
            || requirements.RequiredProvenanceFields.Any(
                field => !providedProvenance.Contains(field)))
        {
            throw new SourceAcquisitionException(
                "required-provenance-missing",
                $"The acquisition intent is missing {requirements.SourceDisplayName}-required provenance fields.");
        }
    }

    public static string RequireLowercaseSha256(string value, string parameterName)
    {
        if (value is null) throw new ArgumentNullException(parameterName);
        var normalized = value.Trim();
        if (normalized.Length != 64
            || normalized.Any(character =>
                character is not (>= '0' and <= '9')
                and not (>= 'a' and <= 'f')))
        {
            throw new ArgumentException(
                "Expected source registry SHA-256 must be lowercase hexadecimal.",
                parameterName);
        }
        return normalized;
    }

    private static bool IsLowercaseSha256(string value)
        => !string.IsNullOrEmpty(value)
           && value.Length == 64
           && value.All(character =>
               character is >= '0' and <= '9'
               || character is >= 'a' and <= 'f');
}

internal static class SourceAcquisitionCandidateGuard
{
    public static void ValidateRequiredProvenance(
        SourceAcquisitionCandidate candidate,
        IReadOnlyList<string> requiredProvenanceFields,
        string expectedSourceRegistryId,
        string expectedRegistrySha256,
        IReadOnlySet<string>? allowedNotProvidedFields = null,
        IReadOnlySet<string>? allowedNotApplicableFields = null)
    {
        if (candidate is null) throw new ArgumentNullException(nameof(candidate));
        if (requiredProvenanceFields is null)
        {
            throw new ArgumentNullException(nameof(requiredProvenanceFields));
        }

        ValidateCoreCandidateInvariants(
            candidate,
            expectedSourceRegistryId,
            expectedRegistrySha256);
        var missing = requiredProvenanceFields
            .Where(field => !HasValidValue(
                candidate,
                field,
                allowedNotProvidedFields,
                allowedNotApplicableFields))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        if (missing.Count > 0)
        {
            throw new SourceAcquisitionException(
                "candidate-required-provenance-missing",
                $"The acquisition candidate is missing required provenance: {string.Join(", ", missing)}.");
        }
    }

    private static void ValidateCoreCandidateInvariants(
        SourceAcquisitionCandidate candidate,
        string expectedSourceRegistryId,
        string expectedRegistrySha256)
    {
        var sourceUrlIsHttps = IsSafeHttpsUri(candidate.SourceUrl);
        var queryUrlIsSafe = candidate.QueryUrl is null
                             || IsSafeHttpsUri(candidate.QueryUrl);
        var collectionsExist = candidate.EvidenceLimitations is not null
                               && candidate.EvidenceLimitations.Count > 0
                               && candidate.EvidenceLimitations.All(IsSubstantive)
                               && candidate.Fields is not null
                               && candidate.Fields.Count > 0
                               && candidate.Fields.All(pair =>
                                   IsSubstantive(pair.Key)
                                   && pair.Value is not null
                                   && pair.Value.Count > 0
                                   && pair.Value.All(IsSubstantive))
                               && candidate.AuthorizedFieldUses is not null
                               && candidate.AuthorizedFieldUses.Count > 0
                               && candidate.AuthorizedFieldUses.All(IsSubstantive)
                               && candidate.SourceSpecificProvenance is not null
                               && candidate.RightsAttributions is not null
                               && candidate.DocumentProvenance is not null
                               && candidate.ReuseBoundary is not null;
        var valid = IsSubstantive(expectedSourceRegistryId)
                    && string.Equals(
                        candidate.SourceRegistryId,
                        expectedSourceRegistryId,
                        StringComparison.OrdinalIgnoreCase)
                    && IsLowercaseSha256(expectedRegistrySha256)
                    && IsLowercaseSha256(candidate.RegistryBindingSha256)
                    && string.Equals(
                        candidate.RegistryBindingSha256,
                        expectedRegistrySha256,
                        StringComparison.Ordinal)
                    && candidate.RetrievedAtUtc != default
                    && candidate.RetrievedAtUtc.Offset == TimeSpan.Zero
                    && string.Equals(
                        candidate.RightsReviewStatusAtRetrieval,
                        "reviewed",
                        StringComparison.OrdinalIgnoreCase)
                    && string.Equals(
                        candidate.HumanReviewStatus,
                        "review-required",
                        StringComparison.OrdinalIgnoreCase)
                    && sourceUrlIsHttps
                    && queryUrlIsSafe
                    && collectionsExist;
        if (!valid)
        {
            throw new SourceAcquisitionException(
                "candidate-core-invariant-invalid",
                "The acquisition candidate violates its source, registry, UTC, rights, review, HTTPS, or collection invariant.");
        }
    }

    public static void ValidateApprovedManualCaptureAudit(
        SourceAcquisitionCandidate candidate)
    {
        if (candidate is null) throw new ArgumentNullException(nameof(candidate));
        var audit = candidate.ManualCaptureAudit;
        var valid = audit is not null
                    && IsSubstantive(audit.OperatorId)
                    && IsSubstantive(audit.ReviewerId)
                    && !string.Equals(
                        audit.OperatorId.Trim(),
                        audit.ReviewerId!.Trim(),
                        StringComparison.OrdinalIgnoreCase)
                    && audit.CapturedAtUtc != default
                    && audit.CapturedAtUtc.Offset == TimeSpan.Zero
                    && audit.ReviewedAtUtc is not null
                    && audit.ReviewedAtUtc.Value != default
                    && audit.ReviewedAtUtc.Value.Offset == TimeSpan.Zero
                    && audit.ReviewedAtUtc.Value > audit.CapturedAtUtc
                    && string.Equals(
                        audit.Decision,
                        "approved",
                        StringComparison.OrdinalIgnoreCase)
                    && audit.Notes is not null
                    && audit.Notes.Count > 0
                    && audit.Notes.All(IsSubstantive)
                    && audit.Attestations is not null
                    && audit.Attestations.AllSatisfied
                    && candidate.QueryUrl is null
                    && candidate.RetrievedAtUtc != default
                    && candidate.RetrievedAtUtc.Offset == TimeSpan.Zero
                    && candidate.RetrievedAtUtc >= audit.ReviewedAtUtc.Value;
        if (!valid)
        {
            throw new SourceAcquisitionException(
                "manual-capture-audit-invalid",
                "A ready manual-capture candidate requires an independently reviewed, approved UTC audit with all safety and rights attestations.");
        }
    }

    private static bool HasValidValue(
        SourceAcquisitionCandidate candidate,
        string fieldName,
        IReadOnlySet<string>? allowedNotProvidedFields,
        IReadOnlySet<string>? allowedNotApplicableFields)
    {
        if (string.IsNullOrWhiteSpace(fieldName)) return false;

        var normalizedField = fieldName.ToLowerInvariant();
        if (normalizedField == "retrievedatutc")
        {
            return candidate.RetrievedAtUtc != default
                   && candidate.RetrievedAtUtc.Offset == TimeSpan.Zero;
        }
        if (normalizedField == "registrybindingsha256")
        {
            return IsLowercaseSha256(candidate.RegistryBindingSha256);
        }
        if (normalizedField == "rightsreviewstatusatretrieval")
        {
            return string.Equals(
                candidate.RightsReviewStatusAtRetrieval,
                "reviewed",
                StringComparison.OrdinalIgnoreCase);
        }
        if (normalizedField == "humanreviewstatus")
        {
            return string.Equals(
                candidate.HumanReviewStatus,
                "review-required",
                StringComparison.OrdinalIgnoreCase);
        }

        var isCommonField = normalizedField is
            "sourceregistryid"
            or "sourceitemid"
            or "sourceurl"
            or "queryurl"
            or "sourcepublicationorupdatedate"
            or "transformationpipelineversion"
            or "authorizedfielduses";
        var commonValue = normalizedField switch
        {
            "sourceregistryid" => candidate.SourceRegistryId,
            "sourceitemid" => candidate.SourceItemId,
            "sourceurl" => candidate.SourceUrl,
            "queryurl" => candidate.QueryUrl,
            "sourcepublicationorupdatedate" => candidate.SourcePublicationOrUpdateDate,
            "transformationpipelineversion" => candidate.TransformationPipelineVersion,
            "authorizedfielduses" => candidate.AuthorizedFieldUses
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)),
            _ => null,
        };
        if (isCommonField) return IsSubstantive(commonValue);

        if (candidate.SourceSpecificProvenance is null) return false;
        var sourceSpecific = candidate.SourceSpecificProvenance
            .FirstOrDefault(pair =>
                string.Equals(pair.Key, fieldName, StringComparison.OrdinalIgnoreCase));
        if (sourceSpecific.Value is null) return false;

        return sourceSpecific.Value.Availability switch
        {
            "present" => sourceSpecific.Value.Values is not null
                         && sourceSpecific.Value.Values.Count > 0
                         && sourceSpecific.Value.Values.All(IsSubstantive)
                         && string.IsNullOrWhiteSpace(
                             sourceSpecific.Value.UnavailableReason),
            "not-provided" => sourceSpecific.Value.Values is not null
                              && allowedNotProvidedFields?.Any(allowed =>
                                  string.Equals(
                                      allowed,
                                      fieldName,
                                      StringComparison.OrdinalIgnoreCase)) == true
                              && sourceSpecific.Value.Values.Count == 0
                              && IsSubstantive(
                                  sourceSpecific.Value.UnavailableReason),
            "not-applicable" => sourceSpecific.Value.Values is not null
                                && allowedNotApplicableFields?.Any(allowed =>
                                     string.Equals(
                                         allowed,
                                         fieldName,
                                         StringComparison.OrdinalIgnoreCase)) == true
                                && sourceSpecific.Value.Values.Count == 0
                                && IsSubstantive(
                                    sourceSpecific.Value.UnavailableReason),
            _ => false,
        };
    }

    private static bool IsSubstantive(string? value)
        => !string.IsNullOrWhiteSpace(value)
           && !string.Equals(value.Trim(), "N/A", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value.Trim(), "unknown", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value.Trim(), "not provided", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value.Trim(), "not-provided", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value.Trim(), "not applicable", StringComparison.OrdinalIgnoreCase)
           && !string.Equals(value.Trim(), "not-applicable", StringComparison.OrdinalIgnoreCase);

    private static bool IsLowercaseSha256(string value)
        => !string.IsNullOrEmpty(value)
           && value.Length == 64
           && value.All(character =>
               character is >= '0' and <= '9'
               || character is >= 'a' and <= 'f');

    private static bool IsSafeHttpsUri(string value)
        => Uri.TryCreate(value, UriKind.Absolute, out var uri)
           && string.Equals(
               uri.Scheme,
               Uri.UriSchemeHttps,
               StringComparison.OrdinalIgnoreCase)
           && uri.UserInfo.Length == 0;
}
