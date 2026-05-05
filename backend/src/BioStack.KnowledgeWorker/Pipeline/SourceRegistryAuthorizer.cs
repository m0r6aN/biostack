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
        var packetSources = ReadPacketSources(root);
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
                packetSources.TryGetValue(sourceRef, out var packetSourceType);
                var entry = registry.Resolve(sourceRef, packetSourceType);
                if (entry is null)
                {
                    reviewReasons.Add($"Claim '{claimId}' source '{sourceRef}' is not mapped to the source registry.");
                    qualityFlags.Add("source-registry-unmapped-source");
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

    private static Dictionary<string, string> ReadPacketSources(JsonObject root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceNode in root["sources"]?.AsArray() ?? new JsonArray())
        {
            if (sourceNode is null) continue;
            var source = sourceNode.AsObject();
            var id = ReadString(source["sourceId"]);
            if (id.Length == 0) continue;
            result[id] = ReadString(source["sourceType"]);
        }
        return result;
    }

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

    private sealed record SourceRegistryEntry(string SourceId, string SourceType, IReadOnlyList<string> AuthorizedFieldUse);

    private sealed class SourceRegistryIndex
    {
        private readonly IReadOnlyDictionary<string, SourceRegistryEntry> _byId;
        private readonly IReadOnlyDictionary<string, SourceRegistryEntry> _byType;

        private SourceRegistryIndex(
            IReadOnlyDictionary<string, SourceRegistryEntry> byId,
            IReadOnlyDictionary<string, SourceRegistryEntry> byType)
        {
            _byId = byId;
            _byType = byType;
        }

        public static SourceRegistryIndex Build(JsonNode sourceRegistry)
        {
            var byId = new Dictionary<string, SourceRegistryEntry>(StringComparer.OrdinalIgnoreCase);
            var byType = new Dictionary<string, SourceRegistryEntry>(StringComparer.OrdinalIgnoreCase);
            foreach (var sourceNode in sourceRegistry["sources"]?.AsArray() ?? new JsonArray())
            {
                if (sourceNode is null) continue;
                var source = sourceNode.AsObject();
                var entry = new SourceRegistryEntry(
                    ReadString(source["sourceId"]),
                    ReadString(source["sourceType"]),
                    ReadStringArray(source["authorizedFieldUse"]));
                if (entry.SourceId.Length == 0) continue;
                byId[entry.SourceId] = entry;
                if (!byType.ContainsKey(entry.SourceType)) byType[entry.SourceType] = entry;
            }
            return new SourceRegistryIndex(byId, byType);
        }

        public SourceRegistryEntry? Resolve(string sourceRef, string? packetSourceType)
        {
            if (_byId.TryGetValue(sourceRef, out var exact)) return exact;
            if (TryPrefix(sourceRef, out var mapped) && _byId.TryGetValue(mapped, out var mappedEntry)) return mappedEntry;
            foreach (var (id, entry) in _byId)
            {
                if (sourceRef.StartsWith(id + "-", StringComparison.OrdinalIgnoreCase)) return entry;
            }
            if (!string.IsNullOrWhiteSpace(packetSourceType) && _byType.TryGetValue(packetSourceType, out var typeEntry)) return typeEntry;
            return null;
        }

        private static bool TryPrefix(string sourceRef, out string sourceId)
        {
            var mappings = new (string Prefix, string SourceId)[]
            {
                ("dailymed-", "dailymed"),
                ("fda-", "fda"),
                ("pubchem-", "pubchem"),
                ("clinicaltrials-", "clinicaltrials"),
                ("nih-ods-", "nih-ods"),
                ("nih-nccih-", "nih-nccih"),
                ("nccih-", "nih-nccih"),
                ("issn-", "issn-position-stands"),
                ("drugbank-", "drugbank"),
                ("wada-", "wada"),
                ("nejm-", "peer-reviewed-paper"),
                ("jama-", "peer-reviewed-paper"),
                ("pubmed-", "peer-reviewed-paper"),
                ("pmc-", "peer-reviewed-review"),
                ("frontiers-", "peer-reviewed-review"),
            };

            foreach (var (prefix, id) in mappings)
            {
                if (!sourceRef.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) continue;
                sourceId = id;
                return true;
            }

            sourceId = string.Empty;
            return false;
        }
    }
}