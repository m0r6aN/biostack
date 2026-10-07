namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

/// <summary>
/// Resolves a packet source reference to the authority tier declared by the source registry.
///
/// Registry schema v2 (<c>source-registry.schema.json</c>) nests these values: the registry id is at
/// <c>identity.sourceId</c>, per-item ids are listed in <c>identity.aliases</c>, and the tier is at
/// <c>evidencePolicy.authorityTier</c>. The schema requires the <c>identity</c> and <c>evidencePolicy</c>
/// objects, so a registry entry never exposes <c>sourceId</c> or <c>authorityTier</c> at its top level.
///
/// Call sites previously read those two fields from the top level of a registry entry, so the registry
/// fallback could not match any entry and silently returned null. This type is the single correct
/// implementation; callers should not re-derive it.
/// </summary>
public static class SourceRegistryTierLookup
{
    /// <summary>
    /// Returns the registry-declared authority tier for <paramref name="sourceRef"/>, matching either the
    /// registry id or one of its registered aliases, or null when the reference is not registered.
    /// </summary>
    public static string? LookupAuthorityTier(string? sourceRef, JsonNode? sourceRegistry)
    {
        if (string.IsNullOrWhiteSpace(sourceRef)) return null;

        JsonArray? sources;
        try
        {
            sources = sourceRegistry?["sources"] as JsonArray;
        }
        catch
        {
            return null;
        }
        if (sources is null) return null;

        foreach (var node in sources)
        {
            if (node is not JsonObject entry) continue;
            if (!MatchesIdentity(entry, sourceRef!)) continue;
            return TryReadString(entry["evidencePolicy"]?["authorityTier"]);
        }

        return null;
    }

    private static bool MatchesIdentity(JsonObject entry, string sourceRef)
    {
        if (entry["identity"] is not JsonObject identity) return false;

        if (string.Equals(TryReadString(identity["sourceId"]), sourceRef, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (identity["aliases"] is JsonArray aliases)
        {
            foreach (var alias in aliases)
            {
                if (string.Equals(TryReadString(alias), sourceRef, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
        }

        return false;
    }

    private static string? TryReadString(JsonNode? node)
    {
        if (node is null) return null;
        try
        {
            return node.GetValue<string>();
        }
        catch
        {
            return null;
        }
    }
}
