namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Text.Json.Nodes;

public enum SourceAcquisitionDisposition
{
    Blocked = 0,
    Ready = 1,
}

public sealed record SourceAcquisitionTarget(
    string RequestId,
    string CompoundName,
    IReadOnlyList<string> Aliases,
    string Classification,
    string Priority);

public sealed record SourcePlanningAdapterResult(
    string AdapterId,
    IReadOnlyList<string> SearchTerms);

public sealed record SourceAcquisitionIntent(
    string SourceId,
    string AdapterId,
    string RequestId,
    string CompoundName,
    IReadOnlyList<string> SearchTerms,
    string CandidateMethod,
    IReadOnlyList<string> AuthorizedFieldUses,
    IReadOnlyList<string> RequiredProvenanceFields,
    string RegistrySchemaVersion,
    string RegistryBindingSha256,
    SourceAcquisitionDisposition Disposition,
    IReadOnlyList<string> BlockingReasons);

public sealed record SourceAcquisitionPlan(
    IReadOnlyList<SourceAcquisitionIntent> Intents,
    int ReadyCount,
    int BlockedCount);

public interface ISourcePlanningAdapter
{
    string SourceId { get; }
    SourcePlanningAdapterResult Plan(SourceAcquisitionTarget target);
}

public interface ISourceAcquisitionPlanBuilder
{
    SourceAcquisitionPlan Build(
        JsonNode researchRequestBatch,
        JsonNode sourceAuthorizationDecisionBatch,
        JsonNode sourceRegistry,
        string actualSourceRegistrySha256,
        IEnumerable<ISourcePlanningAdapter> adapters);
}

public sealed class SourceAcquisitionPlanBuilder : ISourceAcquisitionPlanBuilder
{
    private readonly ISourceRegistryActivationPolicy _activationPolicy;

    public SourceAcquisitionPlanBuilder()
        : this(new SourceRegistryActivationPolicy())
    {
    }

    internal SourceAcquisitionPlanBuilder(ISourceRegistryActivationPolicy activationPolicy)
    {
        _activationPolicy = activationPolicy ?? throw new ArgumentNullException(nameof(activationPolicy));
    }

    public SourceAcquisitionPlan Build(
        JsonNode researchRequestBatch,
        JsonNode sourceAuthorizationDecisionBatch,
        JsonNode sourceRegistry,
        string actualSourceRegistrySha256,
        IEnumerable<ISourcePlanningAdapter> adapters)
    {
        if (researchRequestBatch is null) throw new ArgumentNullException(nameof(researchRequestBatch));
        if (sourceAuthorizationDecisionBatch is null)
        {
            throw new ArgumentNullException(nameof(sourceAuthorizationDecisionBatch));
        }
        if (sourceRegistry is null) throw new ArgumentNullException(nameof(sourceRegistry));
        if (actualSourceRegistrySha256 is null)
        {
            throw new ArgumentNullException(nameof(actualSourceRegistrySha256));
        }
        if (adapters is null) throw new ArgumentNullException(nameof(adapters));

        var requests = ResearchRequestIndex.FromBatches(new[] { researchRequestBatch })
            .All()
            .Select(request => new SourceAcquisitionTarget(
                request.RequestId,
                request.CompoundName,
                request.Aliases,
                request.Classification,
                request.Priority))
            .OrderBy(request => request.CompoundName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(request => request.RequestId, StringComparer.OrdinalIgnoreCase)
            .ToList();

        var sourceDecisions = ReadSourceDecisions(sourceAuthorizationDecisionBatch);
        var adapterIndex = BuildAdapterIndex(adapters);
        RequireExactSelectedSources(sourceDecisions.Keys, adapterIndex.Keys);

        var activationIndex = _activationPolicy.Build(sourceRegistry);
        var registryBinding = sourceAuthorizationDecisionBatch["registryBinding"]?.AsObject()
                              ?? new JsonObject();
        var registryBindingVersion = ReadString(registryBinding["schemaVersion"]);
        var registryBindingSha256 = ReadString(registryBinding["sha256"]);
        var intents = new List<SourceAcquisitionIntent>();

        foreach (var sourceId in RecommendedOfficialSourcePlanningAdapters.SourceIds)
        {
            var decision = sourceDecisions[sourceId];
            var adapter = adapterIndex[sourceId];
            var activation = activationIndex.BySourceId(sourceId);

            foreach (var request in requests)
            {
                var adapterResult = adapter.Plan(request);
                var blockers = BuildBlockingReasons(
                    sourceId,
                    decision,
                    activation,
                    activationIndex.SchemaVersion,
                    registryBindingVersion,
                    actualSourceRegistrySha256,
                    registryBindingSha256);
                var disposition = blockers.Count == 0
                    ? SourceAcquisitionDisposition.Ready
                    : SourceAcquisitionDisposition.Blocked;
                var decisionAuthorizedFieldUses =
                    ReadStringArray(decision["evidenceBoundary"]?["authorizedFieldUse"]);
                var decisionRequiredProvenanceFields =
                    ReadStringArray(decision["provenance"]?["requiredFields"]);
                var authorizedFieldUses = disposition == SourceAcquisitionDisposition.Ready
                    ? Intersection(
                        activation!.AuthorizedFieldUses,
                        decisionAuthorizedFieldUses)
                    : decisionAuthorizedFieldUses;
                var requiredProvenanceFields = disposition == SourceAcquisitionDisposition.Ready
                    ? Union(
                        activation!.RequiredProvenanceFields,
                        decisionRequiredProvenanceFields)
                    : decisionRequiredProvenanceFields;
                intents.Add(new SourceAcquisitionIntent(
                    SourceId: sourceId,
                    AdapterId: adapterResult.AdapterId,
                    RequestId: request.RequestId,
                    CompoundName: request.CompoundName,
                    SearchTerms: adapterResult.SearchTerms,
                    CandidateMethod: SelectAcquisitionMethod(decision, activation, disposition),
                    AuthorizedFieldUses: authorizedFieldUses,
                    RequiredProvenanceFields: requiredProvenanceFields,
                    RegistrySchemaVersion: activationIndex.SchemaVersion,
                    RegistryBindingSha256: registryBindingSha256,
                    Disposition: disposition,
                    BlockingReasons: blockers));
            }
        }

        return new SourceAcquisitionPlan(
            Intents: intents,
            ReadyCount: intents.Count(intent => intent.Disposition == SourceAcquisitionDisposition.Ready),
            BlockedCount: intents.Count(intent => intent.Disposition == SourceAcquisitionDisposition.Blocked));
    }

    private static IReadOnlyDictionary<string, JsonObject> ReadSourceDecisions(JsonNode batch)
    {
        var decisions = new Dictionary<string, JsonObject>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in batch["sources"]?.AsArray() ?? new JsonArray())
        {
            if (node is null) continue;
            var decision = node.AsObject();
            var sourceId = ReadString(decision["sourceId"]);
            if (sourceId.Length == 0)
            {
                throw new InvalidOperationException("Every source decision must declare sourceId.");
            }
            if (!decisions.TryAdd(sourceId, decision))
            {
                throw new InvalidOperationException($"Duplicate source decision '{sourceId}'.");
            }
        }
        return decisions;
    }

    private static IReadOnlyDictionary<string, ISourcePlanningAdapter> BuildAdapterIndex(
        IEnumerable<ISourcePlanningAdapter> adapters)
    {
        var index = new Dictionary<string, ISourcePlanningAdapter>(StringComparer.OrdinalIgnoreCase);
        foreach (var adapter in adapters)
        {
            if (adapter is null) throw new InvalidOperationException("Source planning adapters cannot contain null.");
            if (string.IsNullOrWhiteSpace(adapter.SourceId))
            {
                throw new InvalidOperationException("Every source planning adapter must declare SourceId.");
            }
            if (!index.TryAdd(adapter.SourceId, adapter))
            {
                throw new InvalidOperationException($"Duplicate source planning adapter '{adapter.SourceId}'.");
            }
        }
        return index;
    }

    private static void RequireExactSelectedSources(
        IEnumerable<string> decisionSourceIds,
        IEnumerable<string> adapterSourceIds)
    {
        var expected = RecommendedOfficialSourcePlanningAdapters.SourceIds
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var decisions = decisionSourceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var adapters = adapterSourceIds.ToHashSet(StringComparer.OrdinalIgnoreCase);

        if (!expected.SetEquals(decisions))
        {
            throw new InvalidOperationException(
                "Source authorization decisions must contain exactly the recommended seven sources.");
        }
        if (!expected.SetEquals(adapters))
        {
            throw new InvalidOperationException(
                "Source planning adapters must contain exactly the recommended seven sources.");
        }
    }

    private static IReadOnlyList<string> BuildBlockingReasons(
        string sourceId,
        JsonObject decision,
        SourceRegistryActivationSnapshot? activation,
        string actualRegistrySchemaVersion,
        string boundRegistrySchemaVersion,
        string actualRegistrySha256,
        string boundRegistrySha256)
    {
        var blockers = new List<string>();
        if (activation is null)
        {
            blockers.Add("source-registry-entry-missing");
        }
        else
        {
            blockers.AddRange(activation.BlockingReasons);
        }

        if (!string.Equals(actualRegistrySchemaVersion, boundRegistrySchemaVersion, StringComparison.Ordinal))
        {
            blockers.Add("source-registry-version-mismatch");
        }
        if (!IsLowercaseSha256(actualRegistrySha256)
            || !IsLowercaseSha256(boundRegistrySha256))
        {
            blockers.Add("source-registry-sha256-invalid");
        }
        else if (!string.Equals(actualRegistrySha256, boundRegistrySha256, StringComparison.Ordinal))
        {
            blockers.Add("source-registry-sha256-mismatch");
        }
        if (!Is(decision["decisionStatus"], "approved"))
        {
            blockers.Add("source-decision-not-approved");
        }
        if (!ReadBool(decision["activationReady"]))
        {
            blockers.Add("source-decision-not-activation-ready");
        }
        if (!ApprovalIsApproved(
                decision,
                "product",
                "product-capability",
                "product-capability-review"))
        {
            blockers.Add("product-selection-not-approved");
        }
        if (!ApprovalIsApproved(
                decision,
                "legalRights",
                "legal-rights",
                "source-activation"))
        {
            blockers.Add("legal-rights-not-approved");
        }

        var rights = decision["rights"];
        if (!Is(rights?["reviewStatus"], "reviewed"))
        {
            blockers.Add("source-rights-not-reviewed");
        }
        if (ReadString(rights?["legalBasisOrLicense"]).Length == 0)
        {
            blockers.Add("source-rights-legal-basis-missing");
        }
        if (ReadStringArray(rights?["allowedUses"]).Count == 0)
        {
            blockers.Add("source-rights-allowed-uses-missing");
        }
        if (ReadString(rights?["reviewedBy"]).Length == 0)
        {
            blockers.Add("source-rights-reviewer-missing");
        }
        else if (!string.Equals(
                     ReadString(rights?["reviewedBy"]),
                     ReadString(decision["approvals"]?["legalRights"]?["assigneeName"]),
                     StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add("source-rights-reviewer-approval-assignee-mismatch");
        }
        if (!HasDateTime(rights?["verifiedAtUtc"]))
        {
            blockers.Add("source-rights-review-timestamp-missing");
        }

        var operations = decision["operations"];
        if (!Is(operations?["status"], "approved"))
        {
            blockers.Add("source-operations-not-approved");
        }
        if (!HasDateTime(operations?["lastReviewedAtUtc"]))
        {
            blockers.Add("source-operations-review-timestamp-missing");
        }

        var securityTriggers = ReadStringArray(decision["securityDataTriggersDetected"]);
        if (securityTriggers.Count > 0
            && !ApprovalIsApproved(
                decision,
                "securityData",
                "security-data",
                "source-activation"))
        {
            blockers.Add("security-data-review-required");
        }
        if (securityTriggers.Count == 0
            && !ApprovalIsNotApplicable(decision, "securityData"))
        {
            blockers.Add("security-data-applicability-unresolved");
        }

        var acquisition = decision["acquisition"];
        if (!ReadBool(acquisition?["enabled"]))
        {
            blockers.Add("source-acquisition-disabled");
        }
        if (!IsApprovedAcquisitionMethod(acquisition?["method"]))
        {
            blockers.Add("source-acquisition-method-not-approved");
        }
        if (!IsReviewedOrNotApplicable(acquisition?["apiTermsStatus"]))
        {
            blockers.Add("source-api-terms-not-approved");
        }
        if (!IsReviewedOrNotApplicable(acquisition?["robotsPolicyStatus"]))
        {
            blockers.Add("source-robots-policy-not-approved");
        }
        if (ReadString(acquisition?["reviewCandidateMethod"]).Length == 0)
        {
            blockers.Add("source-candidate-method-missing");
        }

        var refresh = decision["refresh"];
        if (!Is(refresh?["mode"], "manual") && !Is(refresh?["mode"], "scheduled"))
        {
            blockers.Add("source-refresh-not-active");
        }
        if (ReadString(refresh?["proposedCadence"]).Length == 0)
        {
            blockers.Add("source-refresh-cadence-missing");
        }

        if (ReadString(decision["sourceName"]).Length == 0)
        {
            blockers.Add("source-name-missing");
        }
        if (ReadStringArray(decision["evidenceBoundary"]?["authorizedFieldUse"]).Count == 0)
        {
            blockers.Add("source-authorized-field-use-missing");
        }
        if (ReadStringArray(decision["provenance"]?["requiredFields"]).Count == 0)
        {
            blockers.Add("source-provenance-fields-missing");
        }
        if (activation is not null
            && !string.Equals(
                ReadString(acquisition?["method"]),
                activation.AcquisitionMethod,
                StringComparison.OrdinalIgnoreCase))
        {
            blockers.Add("source-acquisition-method-registry-mismatch");
        }
        if (activation is not null
            && Intersection(
                    activation.AuthorizedFieldUses,
                    ReadStringArray(decision["evidenceBoundary"]?["authorizedFieldUse"]))
                .Count == 0)
        {
            blockers.Add("source-authorized-field-use-no-registry-overlap");
        }
        return blockers
            .Select(reason => $"{sourceId}:{reason}")
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static bool ApprovalIsApproved(
        JsonObject decision,
        string approvalName,
        string expectedDecisionScope,
        string expectedBlockingStage)
    {
        var approval = decision["approvals"]?[approvalName];
        return ReadString(approval?["assigneeName"]).Length > 0
               && Is(approval?["decisionScope"], expectedDecisionScope)
               && Is(approval?["blockingStage"], expectedBlockingStage)
               && ApprovalHasReviewStatus(decision, approvalName, "reviewed")
               && (Is(approval?["decision"], "approved")
                   || Is(approval?["decision"], "approved-with-controls"))
               && HasDateTime(approval?["decidedAtUtc"])
               && ReadStringArray(approval?["decisionNotes"]).Count > 0;
    }

    private static bool ApprovalHasReviewStatus(
        JsonObject decision,
        string approvalName,
        string reviewStatus)
        => Is(decision["approvals"]?[approvalName]?["reviewStatus"], reviewStatus);

    private static bool ApprovalIsNotApplicable(JsonObject decision, string approvalName)
    {
        if (decision["approvals"]?[approvalName] is not JsonObject approval)
        {
            return false;
        }

        return Is(approval["reviewStatus"], "not-applicable")
               && approval.ContainsKey("decision")
               && approval["decision"] is null
               && approval.ContainsKey("decidedAtUtc")
               && approval["decidedAtUtc"] is null;
    }

    private static string SelectAcquisitionMethod(
        JsonObject decision,
        SourceRegistryActivationSnapshot? activation,
        SourceAcquisitionDisposition disposition)
    {
        var acquisition = decision["acquisition"];
        if (disposition == SourceAcquisitionDisposition.Ready)
        {
            return activation!.AcquisitionMethod;
        }

        var reviewCandidateMethod = ReadString(acquisition?["reviewCandidateMethod"]);
        return reviewCandidateMethod.Length > 0
            ? reviewCandidateMethod
            : ReadString(acquisition?["method"]);
    }

    private static bool IsLowercaseSha256(string value)
        => value.Length == 64
           && value.All(character =>
               character is >= '0' and <= '9'
               || character is >= 'a' and <= 'f');

    private static IReadOnlyList<string> Intersection(
        IReadOnlyList<string> registryValues,
        IReadOnlyList<string> decisionValues)
    {
        var approved = decisionValues.ToHashSet(StringComparer.OrdinalIgnoreCase);
        return registryValues
            .Where(approved.Contains)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private static IReadOnlyList<string> Union(
        IReadOnlyList<string> registryValues,
        IReadOnlyList<string> decisionValues)
        => registryValues
            .Concat(decisionValues)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static bool IsApprovedAcquisitionMethod(JsonNode? node)
        => Is(node, "api") || Is(node, "bulk-download") || Is(node, "manual-review");

    private static bool IsReviewedOrNotApplicable(JsonNode? node)
        => Is(node, "reviewed") || Is(node, "not-applicable");

    private static bool HasDateTime(JsonNode? node)
        => DateTimeOffset.TryParse(
            ReadString(node),
            CultureInfo.InvariantCulture,
            DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal,
            out _);

    private static bool Is(JsonNode? node, string value)
        => string.Equals(ReadString(node), value, StringComparison.OrdinalIgnoreCase);

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
