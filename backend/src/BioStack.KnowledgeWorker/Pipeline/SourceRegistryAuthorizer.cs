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
    private readonly ISourceRegistryActivationPolicy _activationPolicy;

    public SourceRegistryAuthorizer()
        : this(new SourceRegistryActivationPolicy())
    {
    }

    internal SourceRegistryAuthorizer(ISourceRegistryActivationPolicy activationPolicy)
    {
        _activationPolicy = activationPolicy ?? throw new ArgumentNullException(nameof(activationPolicy));
    }

    public SourceRegistryAuthorizationResult Authorize(JsonNode evidencePacket, JsonNode sourceRegistry)
    {
        if (evidencePacket is null) throw new ArgumentNullException(nameof(evidencePacket));
        if (sourceRegistry is null) throw new ArgumentNullException(nameof(sourceRegistry));

        var packet = JsonNode.Parse(evidencePacket.ToJsonString())!;
        var root = packet.AsObject();
        var registry = _activationPolicy.Build(sourceRegistry);
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

                if (!entry.CanAcquire)
                {
                    reviewReasons.Add(
                        $"Claim '{claimId}' source '{sourceRef}' is disabled pending approved rights, active operations, and enabled acquisition.");
                    qualityFlags.Add("source-registry-source-disabled");
                    continue;
                }

                if (entry.AuthorizedFieldUses.Contains(requiredUse, StringComparer.OrdinalIgnoreCase))
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

}
