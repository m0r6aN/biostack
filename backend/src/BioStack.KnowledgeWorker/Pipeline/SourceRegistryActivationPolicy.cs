namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record SourceRegistryActivationSnapshot(
    string SourceId,
    IReadOnlyList<string> Aliases,
    string AcquisitionMethod,
    IReadOnlyList<string> AuthorizedFieldUses,
    IReadOnlyList<string> RequiredProvenanceFields,
    bool CanAcquire,
    IReadOnlyList<string> BlockingReasons);

public interface ISourceRegistryActivationPolicy
{
    SourceRegistryActivationIndex Build(JsonNode sourceRegistry);
}

public sealed class SourceRegistryActivationPolicy : ISourceRegistryActivationPolicy
{
    public SourceRegistryActivationIndex Build(JsonNode sourceRegistry)
    {
        if (sourceRegistry is null) throw new ArgumentNullException(nameof(sourceRegistry));

        var byReference = new Dictionary<string, SourceRegistryActivationSnapshot?>(
            StringComparer.OrdinalIgnoreCase);
        var bySourceId = new Dictionary<string, SourceRegistryActivationSnapshot?>(
            StringComparer.OrdinalIgnoreCase);
        var referencesBySourceId = new Dictionary<string, HashSet<string>>(
            StringComparer.OrdinalIgnoreCase);

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

            var sourceId = ReadString(identity["sourceId"]);
            if (sourceId.Length == 0) continue;

            var aliases = ReadStringArray(identity["aliases"]);
            var reasons = ActivationBlockingReasons(
                rights,
                operations,
                acquisition,
                evidencePolicy,
                provenance,
                refresh,
                remediation,
                dataBoundary);
            var snapshot = new SourceRegistryActivationSnapshot(
                SourceId: sourceId,
                Aliases: aliases,
                AcquisitionMethod: ReadString(acquisition["method"]),
                AuthorizedFieldUses: ReadStringArray(evidencePolicy["authorizedFieldUse"]),
                RequiredProvenanceFields: ReadStringArray(provenance["requiredFields"]),
                CanAcquire: reasons.Count == 0,
                BlockingReasons: reasons);

            if (!referencesBySourceId.TryGetValue(sourceId, out var identityReferences))
            {
                identityReferences = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                referencesBySourceId[sourceId] = identityReferences;
            }
            identityReferences.Add(sourceId);
            identityReferences.UnionWith(aliases);

            if (!bySourceId.TryAdd(sourceId, snapshot))
            {
                bySourceId[sourceId] = null;
                foreach (var reference in identityReferences)
                {
                    byReference[reference] = null;
                }
                continue;
            }
            AddReference(byReference, sourceId, snapshot);
            foreach (var alias in aliases)
            {
                AddReference(byReference, alias, snapshot);
            }
        }

        return new SourceRegistryActivationIndex(
            ReadString(sourceRegistry["schemaVersion"]),
            bySourceId,
            byReference);
    }

    private static IReadOnlyList<string> ActivationBlockingReasons(
        JsonObject rights,
        JsonObject operations,
        JsonObject acquisition,
        JsonObject evidencePolicy,
        JsonObject provenance,
        JsonObject refresh,
        JsonObject remediation,
        JsonObject dataBoundary)
    {
        var reasons = new List<string>();
        AddUnless(reasons, Is(rights["reviewStatus"], "approved"), "rights-review-not-approved");
        AddUnless(reasons, HasText(rights["legalBasisOrLicense"]), "rights-legal-basis-missing");
        AddUnless(reasons, HasAbsoluteUri(rights["termsUrl"]), "rights-terms-url-missing");
        AddUnless(reasons, HasTimestamp(rights["verifiedAtUtc"]), "rights-verification-missing");
        AddUnless(reasons, ReadStringArray(rights["allowedUses"]).Count > 0, "rights-allowed-uses-missing");
        AddUnless(reasons, HasText(rights["reviewedByRole"]), "rights-reviewer-missing");

        AddUnless(reasons, Is(operations["status"], "active"), "operations-not-active");
        AddUnless(reasons, HasText(operations["ownerRole"]), "operations-owner-missing");
        AddUnless(reasons, HasText(operations["securityOwnerRole"]), "operations-security-owner-missing");
        AddUnless(reasons, HasTimestamp(operations["lastReviewedAtUtc"]), "operations-review-missing");

        AddUnless(reasons, ReadBool(acquisition["enabled"]), "acquisition-disabled");
        AddUnless(reasons, !Is(acquisition["method"], "none"), "acquisition-method-missing");
        AddUnless(
            reasons,
            IsReviewedOrNotApplicable(acquisition["robotsPolicyStatus"]),
            "acquisition-robots-review-incomplete");
        AddUnless(
            reasons,
            IsReviewedOrNotApplicable(acquisition["apiTermsStatus"]),
            "acquisition-api-terms-review-incomplete");
        AddUnless(reasons, HasText(acquisition["rateLimitPolicy"]), "acquisition-rate-limit-missing");
        AddUnless(reasons, HasText(acquisition["accessNotes"]), "acquisition-access-notes-missing");

        AddUnless(
            reasons,
            ReadStringArray(evidencePolicy["authorizedFieldUse"]).Count > 0,
            "evidence-authorized-use-missing");
        AddUnless(
            reasons,
            ReadStringArray(provenance["requiredFields"]).Count > 0,
            "provenance-required-fields-missing");
        AddUnless(reasons, IsActiveRefreshMode(refresh["mode"]), "refresh-mode-inactive");
        AddUnless(reasons, HasText(refresh["cadence"]), "refresh-cadence-missing");
        AddUnless(reasons, HasText(remediation["correctionProcedure"]), "remediation-correction-missing");
        AddUnless(reasons, HasText(remediation["retractionProcedure"]), "remediation-retraction-missing");
        AddUnless(reasons, HasText(remediation["removalProcedure"]), "remediation-removal-missing");
        AddUnless(reasons, HasText(remediation["contactRole"]), "remediation-contact-missing");
        AddUnless(
            reasons,
            ReadStringArray(dataBoundary["permittedContent"]).Count > 0,
            "data-boundary-permitted-content-missing");

        return reasons;
    }

    private static void AddUnless(ICollection<string> reasons, bool condition, string reason)
    {
        if (!condition) reasons.Add(reason);
    }

    private static void AddReference(
        IDictionary<string, SourceRegistryActivationSnapshot?> references,
        string reference,
        SourceRegistryActivationSnapshot snapshot)
    {
        if (string.IsNullOrWhiteSpace(reference)) return;
        if (references.TryGetValue(reference, out var existing))
        {
            if (existing is null || !ReferenceEquals(existing, snapshot))
            {
                references[reference] = null;
            }
            return;
        }

        references[reference] = snapshot;
    }

    private static bool Is(JsonNode? node, string value)
        => string.Equals(ReadString(node), value, StringComparison.OrdinalIgnoreCase);

    private static bool HasText(JsonNode? node)
        => !string.IsNullOrWhiteSpace(ReadString(node));

    private static bool HasAbsoluteUri(JsonNode? node)
        => Uri.TryCreate(ReadString(node), UriKind.Absolute, out _);

    private static bool HasTimestamp(JsonNode? node)
        => DateTimeOffset.TryParse(ReadString(node), out _);

    private static bool IsReviewedOrNotApplicable(JsonNode? node)
        => Is(node, "approved") || Is(node, "not-applicable");

    private static bool IsActiveRefreshMode(JsonNode? node)
        => Is(node, "scheduled") || Is(node, "manual");

    private static bool ReadBool(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var result) && result;

    private static string ReadString(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<string>(out var result)
            ? result.Trim()
            : string.Empty;

    private static IReadOnlyList<string> ReadStringArray(JsonNode? node)
        => node is JsonArray array
            ? array.Select(ReadString)
                .Where(value => value.Length > 0)
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToList()
            : Array.Empty<string>();
}

public sealed class SourceRegistryActivationIndex
{
    private readonly IReadOnlyDictionary<string, SourceRegistryActivationSnapshot?> _bySourceId;
    private readonly IReadOnlyDictionary<string, SourceRegistryActivationSnapshot?> _byReference;

    internal SourceRegistryActivationIndex(
        string schemaVersion,
        IReadOnlyDictionary<string, SourceRegistryActivationSnapshot?> bySourceId,
        IReadOnlyDictionary<string, SourceRegistryActivationSnapshot?> byReference)
    {
        SchemaVersion = schemaVersion;
        _bySourceId = bySourceId;
        _byReference = byReference;
    }

    public string SchemaVersion { get; }

    public IReadOnlyCollection<SourceRegistryActivationSnapshot> Sources
        => _bySourceId.Values.Where(snapshot => snapshot is not null).Cast<SourceRegistryActivationSnapshot>().ToList();

    public SourceRegistryActivationSnapshot? Resolve(string sourceReference)
        => _byReference.TryGetValue(sourceReference, out var snapshot) ? snapshot : null;

    public SourceRegistryActivationSnapshot? BySourceId(string sourceId)
        => _bySourceId.TryGetValue(sourceId, out var snapshot) ? snapshot : null;
}
