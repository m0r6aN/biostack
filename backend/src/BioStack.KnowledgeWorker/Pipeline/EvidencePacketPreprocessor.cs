namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record EvidencePacketPreprocessingResult(
    JsonNode Packet,
    IReadOnlyList<string> ReviewReasons,
    IReadOnlyList<string> QualityFlags);

public interface IEvidencePacketPreprocessor
{
    EvidencePacketPreprocessingResult Preprocess(JsonNode evidencePacket);
}

public sealed class EvidencePacketPreprocessor : IEvidencePacketPreprocessor
{
    public EvidencePacketPreprocessingResult Preprocess(JsonNode evidencePacket)
    {
        if (evidencePacket is null) throw new ArgumentNullException(nameof(evidencePacket));

        var packet = JsonNode.Parse(evidencePacket.ToJsonString())!;
        var root = packet.AsObject();
        var reviewReasons = new List<string>();
        var qualityFlags = new List<string>();

        var sourceTiers = ReadSourceTiers(root);
        foreach (var claimNode in root["claims"]?.AsArray() ?? new JsonArray())
        {
            if (claimNode is null) continue;
            var claim = claimNode.AsObject();
            var claimId = ReadString(claim["claimId"]);
            var claimType = ReadString(claim["claimType"]);
            var fieldAuthorityRequired = ReadBool(claim["fieldAuthorityRequired"]);
            var sourceRefs = ReadStringArray(claim["sourceRefs"]);

            foreach (var sourceRef in sourceRefs.Where(s => !sourceTiers.ContainsKey(s)))
            {
                reviewReasons.Add($"Claim '{claimId}' references unknown source '{sourceRef}'.");
                qualityFlags.Add("unknown-source-ref");
            }

            var hasAuthoritativeSupport = sourceRefs.Any(s =>
                sourceTiers.TryGetValue(s, out var tier) && FieldAuthorityPolicy.IsAuthoritativeTier(tier));

            if (FieldAuthorityPolicy.RequiresAuthoritativeSupport(claimType, fieldAuthorityRequired)
                && !hasAuthoritativeSupport)
            {
                reviewReasons.Add($"Claim '{claimId}' of type '{claimType}' requires authoritative A1/A2 support.");
                qualityFlags.Add("missing-authoritative-support");
            }
        }

        ApplyOpsFlags(root, reviewReasons, qualityFlags);

        return new EvidencePacketPreprocessingResult(
            packet,
            reviewReasons.Distinct(StringComparer.OrdinalIgnoreCase).ToList(),
            qualityFlags.Distinct(StringComparer.OrdinalIgnoreCase).ToList());
    }

    private static Dictionary<string, string> ReadSourceTiers(JsonObject root)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var sourceNode in root["sources"]?.AsArray() ?? new JsonArray())
        {
            if (sourceNode is null) continue;
            var source = sourceNode.AsObject();
            var id = ReadString(source["sourceId"]);
            if (id.Length == 0) continue;
            result[id] = ReadString(source["authorityTier"]);
        }
        return result;
    }

    private static void ApplyOpsFlags(JsonObject root, List<string> reviewReasons, List<string> qualityFlags)
    {
        var ops = root["ops"]?.AsObject() ?? new JsonObject();
        root["ops"] = ops;

        var allReviewReasons = ReadStringArray(ops["reviewReasons"])
            .Concat(reviewReasons)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        var allQualityFlags = ReadStringArray(ops["qualityFlags"])
            .Concat(qualityFlags)
            .Where(s => !string.IsNullOrWhiteSpace(s))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        ops["needsReview"] = ReadBool(ops["needsReview"]) || allReviewReasons.Count > 0;
        ops["reviewReasons"] = ToJsonArray(allReviewReasons);
        ops["qualityFlags"] = ToJsonArray(allQualityFlags);
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

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