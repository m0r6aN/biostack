namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record SourceRegistryAuthorizationResult(
    JsonNode Packet,
    IReadOnlyList<string> ReviewReasons,
    IReadOnlyList<string> QualityFlags);

public interface ISourceRegistryAuthorizer
{
    SourceRegistryAuthorizationResult Authorize(JsonNode evidencePacket, JsonNode sourceRegistry);
}

public sealed class SourceRegistryAuthorizer : ISourceRegistryAuthorizer
{
    public SourceRegistryAuthorizationResult Authorize(JsonNode evidencePacket, JsonNode sourceRegistry)
    {
        if (evidencePacket is null) throw new ArgumentNullException(nameof(evidencePacket));
        if (sourceRegistry is null) throw new ArgumentNullException(nameof(sourceRegistry));

        var packet = JsonNode.Parse(evidencePacket.ToJsonString())!;
        var root = packet.AsObject();
        var registry = SourceRegistryIndex.Build(sourceRegistry);
        var reviewReasons = new List<string>();
        var qualityFlags = new List<string>();

        foreach (var claimNode in root["claims"]?.AsArray() ?? new JsonArray())
        {
            if (claimNode is null) continue;
            var claim = claimNode.AsObject();
            var claimId = ReadString(claim["claimId"]);
            var claimType = ReadString(claim["claimType"]);
            var requiredUse = ClaimTypeToAuthorizedUse(claimType);
            if (requiredUse is null) continue;

            var sourceRefs = ReadStringArray(claim["sourceRefs"]);
            var authorized = false;
            foreach (var sourceRef in sourceRefs)
            {
                var entry = registry.Resolve(sourceRef);
                if (entry is null)
                {
                    reviewReasons.Add($"Claim '{claimId}' source '{sourceRef}' is not mapped to the source registry.");
                    qualityFlags.Add("source-registry-unmapped-source");
                    continue;
                }

                if (!entry.IsEnabled)
                {
                    reviewReasons.Add(
                        $"Claim '{claimId}' source '{sourceRef}' is disabled pending approved rights, active operations, and enabled acquisition.");
                    qualityFlags.Add("source-registry-source-disabled");
                    continue;
                }

                if (entry.AuthorizedFieldUse.Contains(requiredUse, StringComparer.OrdinalIgnoreCase))
                {
                    authorized = true;
                }
            }

            if (!authorized)
            {
                reviewReasons.Add($"Claim '{claimId}' of type '{claimType}' lacks source-registry authorization for '{requiredUse}'.");
                qualityFlags.Add("source-registry-field-mismatch");
            }
        }

        ApplyOpsFlags(root, reviewReasons, qualityFlags);
        return new SourceRegistryAuthorizationResult(
            packet,
            reviewReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            qualityFlags.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static string? ClaimTypeToAuthorizedUse(string claimType) => claimType switch
    {
        "identity" => "identity",
        "regulatory" => "regulatory",
        "approved-indication" => "approved-indications",
        "studied-use" or "common-off-label-use" or "efficacy" => "efficacy-claims",
        "mechanism" or "target-pathway" => "mechanism",
        "dose-context" => "product-specific-dosing",
        "formulation" or "storage-reconstitution" => "storage-reconstitution",
        "contraindication" or "warning" or "adverse-effect" => "contraindications-warnings",
        "monitoring" => "monitoring",
        "interaction" => "interactions",
        "stack-heuristic" => "stack-heuristics",
        "misinformation-claim" => "misinformation-monitoring",
        _ => null,
    };

    private static void ApplyOpsFlags(JsonObject root, List<string> reviewReasons, List<string> qualityFlags)
    {
        var ops = root["ops"]?.AsObject() ?? new JsonObject();
        root["ops"] = ops;
        var allReviewReasons = ReadStringArray(ops["reviewReasons"]).Concat(reviewReasons)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var allQualityFlags = ReadStringArray(ops["qualityFlags"]).Concat(qualityFlags)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        ops["needsReview"] = ReadBool(ops["needsReview"]) || allReviewReasons.Count > 0;
        ops["reviewReasons"] = ToJsonArray(allReviewReasons);
        ops["qualityFlags"] = ToJsonArray(allQualityFlags);
    }

    private static string ReadString(JsonNode? node) => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static bool ReadBool(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var result) && result;

    private static List<string> ReadStringArray(JsonNode? node)
    {
        if (node is not JsonArray arr) return new List<string>();
        return arr.Select(item => item?.GetValue<string>()?.Trim() ?? string.Empty)
            .Where(s => s.Length > 0)
            .ToList();
    }

    private static JsonArray ToJsonArray(IEnumerable<string> values)
    {
        var arr = new JsonArray();
        foreach (var value in values) arr.Add(value);
        return arr;
    }

    private sealed record SourceRegistryEntry(
        string SourceId,
        IReadOnlyList<string> Aliases,
        string RightsReviewStatus,
        string LegalBasisOrLicense,
        string TermsUrl,
        string RightsVerifiedAtUtc,
        IReadOnlyList<string> AllowedUses,
        string RightsReviewedByRole,
        string OperationalStatus,
        string OwnerRole,
        string SecurityOwnerRole,
        string OperationsLastReviewedAtUtc,
        bool AcquisitionEnabled,
        string AcquisitionMethod,
        string RobotsPolicyStatus,
        string ApiTermsStatus,
        string RateLimitPolicy,
        string AccessNotes,
        IReadOnlyList<string> AuthorizedFieldUse)
    {
        public IReadOnlyList<string> ProvenanceRequiredFields { get; init; } = Array.Empty<string>();
        public string RefreshMode { get; init; } = string.Empty;
        public string RefreshCadence { get; init; } = string.Empty;
        public string CorrectionProcedure { get; init; } = string.Empty;
        public string RetractionProcedure { get; init; } = string.Empty;
        public string RemovalProcedure { get; init; } = string.Empty;
        public string RemediationContactRole { get; init; } = string.Empty;
        public IReadOnlyList<string> PermittedContent { get; init; } = Array.Empty<string>();

        public bool IsEnabled =>
            string.Equals(RightsReviewStatus, "approved", StringComparison.OrdinalIgnoreCase)
            && HasText(LegalBasisOrLicense)
            && HasAbsoluteUri(TermsUrl)
            && HasTimestamp(RightsVerifiedAtUtc)
            && AllowedUses.Count > 0
            && HasText(RightsReviewedByRole)
            && string.Equals(OperationalStatus, "active", StringComparison.OrdinalIgnoreCase)
            && HasText(OwnerRole)
            && HasText(SecurityOwnerRole)
            && HasTimestamp(OperationsLastReviewedAtUtc)
            && AcquisitionEnabled
            && !string.Equals(AcquisitionMethod, "none", StringComparison.OrdinalIgnoreCase)
            && IsReviewedOrNotApplicable(RobotsPolicyStatus)
            && IsReviewedOrNotApplicable(ApiTermsStatus)
            && HasText(RateLimitPolicy)
            && HasText(AccessNotes)
            && AuthorizedFieldUse.Count > 0
            && ProvenanceRequiredFields.Count > 0
            && IsActiveRefreshMode(RefreshMode)
            && HasText(RefreshCadence)
            && HasText(CorrectionProcedure)
            && HasText(RetractionProcedure)
            && HasText(RemovalProcedure)
            && HasText(RemediationContactRole)
            && PermittedContent.Count > 0;

        private static bool HasText(string value) => !string.IsNullOrWhiteSpace(value);

        private static bool HasAbsoluteUri(string value)
            => Uri.TryCreate(value, UriKind.Absolute, out _);

        private static bool HasTimestamp(string value)
            => DateTimeOffset.TryParse(value, out _);

        private static bool IsReviewedOrNotApplicable(string value)
            => string.Equals(value, "approved", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "not-applicable", StringComparison.OrdinalIgnoreCase);

        private static bool IsActiveRefreshMode(string value)
            => string.Equals(value, "scheduled", StringComparison.OrdinalIgnoreCase)
               || string.Equals(value, "manual", StringComparison.OrdinalIgnoreCase);
    }

    private sealed class SourceRegistryIndex
    {
        private readonly IReadOnlyDictionary<string, SourceRegistryEntry?> _byReference;

        private SourceRegistryIndex(IReadOnlyDictionary<string, SourceRegistryEntry?> byReference)
        {
            _byReference = byReference;
        }

        public static SourceRegistryIndex Build(JsonNode sourceRegistry)
        {
            var byReference = new Dictionary<string, SourceRegistryEntry?>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceNode in sourceRegistry["sources"]?.AsArray() ?? new JsonArray())
            {
                if (sourceNode is null) continue;
                var source = sourceNode.AsObject();
                var identity = source["identity"]?.AsObject() ?? new JsonObject();
                var rights = source["rights"]?.AsObject() ?? new JsonObject();
                var operations = source["operations"]?.AsObject() ?? new JsonObject();
                var acquisition = source["acquisition"]?.AsObject() ?? new JsonObject();
                var evidencePolicy = source["evidencePolicy"]?.AsObject() ?? new JsonObject();
                var provenance = source["provenanceRequirements"]?.AsObject() ?? new JsonObject();
                var refresh = source["refreshPolicy"]?.AsObject() ?? new JsonObject();
                var remediation = source["remediation"]?.AsObject() ?? new JsonObject();
                var dataBoundary = source["dataBoundary"]?.AsObject() ?? new JsonObject();
                var entry = new SourceRegistryEntry(
                    ReadString(identity["sourceId"]),
                    ReadStringArray(identity["aliases"]),
                    ReadString(rights["reviewStatus"]),
                    ReadString(rights["legalBasisOrLicense"]),
                    ReadString(rights["termsUrl"]),
                    ReadString(rights["verifiedAtUtc"]),
                    ReadStringArray(rights["allowedUses"]),
                    ReadString(rights["reviewedByRole"]),
                    ReadString(operations["status"]),
                    ReadString(operations["ownerRole"]),
                    ReadString(operations["securityOwnerRole"]),
                    ReadString(operations["lastReviewedAtUtc"]),
                    ReadBool(acquisition["enabled"]),
                    ReadString(acquisition["method"]),
                    ReadString(acquisition["robotsPolicyStatus"]),
                    ReadString(acquisition["apiTermsStatus"]),
                    ReadString(acquisition["rateLimitPolicy"]),
                    ReadString(acquisition["accessNotes"]),
                    ReadStringArray(evidencePolicy["authorizedFieldUse"]))
                {
                    ProvenanceRequiredFields = ReadStringArray(provenance["requiredFields"]),
                    RefreshMode = ReadString(refresh["mode"]),
                    RefreshCadence = ReadString(refresh["cadence"]),
                    CorrectionProcedure = ReadString(remediation["correctionProcedure"]),
                    RetractionProcedure = ReadString(remediation["retractionProcedure"]),
                    RemovalProcedure = ReadString(remediation["removalProcedure"]),
                    RemediationContactRole = ReadString(remediation["contactRole"]),
                    PermittedContent = ReadStringArray(dataBoundary["permittedContent"]),
                };
                if (entry.SourceId.Length == 0) continue;
                AddReference(byReference, entry.SourceId, entry);
                foreach (var alias in entry.Aliases)
                {
                    AddReference(byReference, alias, entry);
                }
            }
            return new SourceRegistryIndex(byReference);
        }

        public SourceRegistryEntry? Resolve(string sourceRef)
        {
            return _byReference.TryGetValue(sourceRef, out var entry) ? entry : null;
        }

        private static void AddReference(
            IDictionary<string, SourceRegistryEntry?> references,
            string reference,
            SourceRegistryEntry entry)
        {
            if (string.IsNullOrWhiteSpace(reference)) return;
            if (references.TryGetValue(reference, out var existing))
            {
                if (existing is null || !ReferenceEquals(existing, entry))
                {
                    references[reference] = null;
                }
                return;
            }

            references[reference] = entry;
        }
    }
}
