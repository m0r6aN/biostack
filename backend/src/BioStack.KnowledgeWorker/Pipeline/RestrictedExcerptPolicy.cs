namespace BioStack.KnowledgeWorker.Pipeline;

using System.Reflection;
using System.Text.Json.Nodes;

/// <summary>
/// Withholds stored excerpt text for the DrugBank scope contained by owner decision C1, at the point
/// the evidence-packet artifact is written.
///
/// Scope. This implements C1 and only C1: the DrugBank class, its registered per-item ids, and records
/// actually served from a DrugBank host. It is deliberately NOT a general restriction engine. An earlier
/// revision derived its scope from the registry's <c>dataBoundary</c> marker, which silently reached six
/// classes including two retired authorization placeholders and an ISSN paper. C3 retires authorization
/// classes; it is not a decision to withhold their literature, and conflating the two expanded an
/// approved decision without review.
///
/// Where. <c>ResearchJob.WriteEvidencePacketArtifact</c> is the only place raw
/// <c>extractedEvidence[].quote</c> text is persisted to a generated artifact. Downstream consumers
/// already carry no quotes: the substance-record compiler reads statement, tier, confidence and
/// sourceRefs; the promotion exporter and manifest builder operate on that quote-free draft; the
/// compound-graph builder never reads extractedEvidence; and the trust ledger hardcodes its quote field
/// to null.
///
/// Write-time containment is necessary but NOT sufficient on its own. It does not reach artifacts
/// already written, and it does not reach a client-supplied packet posted to a consumer such as the
/// AI-suggestion route. Read-time and provider-boundary containment are required alongside it and are
/// implemented separately; a passing test here proves this path only.
///
/// Fail-closed. The scope comes from an embedded copy of
/// <c>shared/source-rights/drugbank-containment.v1.json</c>, the same manifest the frontend consumes, so
/// containment cannot depend on an optional registry file being supplied. If that manifest cannot be
/// read the policy throws rather than emitting: a rights control that quietly disables itself when its
/// configuration is missing is not a control. The source registry, when present, may only ADD matches
/// through the DrugBank class's aliases; it can never remove one. Neither a missing registry nor a
/// partial <c>permittedContent</c> declaration releases these excerpts. Release requires a reviewed
/// superseding C1 decision.
///
/// This authorizes nothing. It only declines to copy excerpt text out of the record the operator already
/// holds, and it cannot make any source usable.
/// </summary>
public static class RestrictedExcerptPolicy
{
    /// <summary>Quality flag appended to a packet whose artifact had excerpt text withheld.</summary>
    public const string WithheldFlag = "restricted-source-excerpt-withheld";

    private const string ManifestResourceName =
        "BioStack.KnowledgeWorker.SourceRights.drugbank-containment.v1.json";

    private static readonly Lazy<ContainmentManifest> Manifest = new(LoadManifest);

    /// <summary>The policy id recorded by the embedded containment manifest.</summary>
    public static string PolicyId => Manifest.Value.PolicyId;

    /// <summary>
    /// Returns a copy of <paramref name="packet"/> with excerpt text removed for every contained source,
    /// or the same node when nothing is contained. The input node is never mutated.
    /// </summary>
    /// <param name="packet">The evidence packet about to be written as an artifact.</param>
    /// <param name="sourceRegistry">
    /// Optional. Used only to widen matching via the DrugBank class's registered aliases. Containment is
    /// identical without it for every id and host named by the manifest.
    /// </param>
    /// <remarks>
    /// The citation survives what it withholds: <c>sourceRef</c> and <c>pageOrSection</c> are preserved
    /// and only <c>quote</c> becomes null, which the evidence-packet schema already permits. A reviewer
    /// still sees that a claim rests on a contained excerpt and where that excerpt sits in the work, so
    /// the claim can be re-sourced rather than silently dropped. Because an evidence item is
    /// <c>additionalProperties: false</c>, the fact of withholding is recorded once at packet level in
    /// <c>ops.qualityFlags</c>, appended to whatever review flags are already there.
    /// </remarks>
    public static JsonNode WithholdRestrictedExcerpts(JsonNode packet, JsonNode? sourceRegistry)
    {
        if (packet is null) throw new ArgumentNullException(nameof(packet));

        var manifest = Manifest.Value;
        var containedIds = new HashSet<string>(manifest.SourceIds, StringComparer.OrdinalIgnoreCase);
        AddRegistryAliases(containedIds, manifest.SourceClassId, sourceRegistry);

        var clone = JsonNode.Parse(packet.ToJsonString());
        if (clone is not JsonObject root) return packet;

        var containedRefs = ResolveContainedRefs(root, containedIds, manifest.HostSuffixes);
        if (containedRefs.Count == 0) return packet;

        var withheld = 0;
        if (root["claims"] is JsonArray claims)
        {
            foreach (var claimNode in claims)
            {
                if (claimNode is not JsonObject claim) continue;
                if (claim["extractedEvidence"] is not JsonArray evidence) continue;

                foreach (var evidenceNode in evidence)
                {
                    if (evidenceNode is not JsonObject item) continue;
                    var sourceRef = TryReadString(item["sourceRef"])?.Trim();
                    if (sourceRef is null || !containedRefs.Contains(sourceRef)) continue;
                    if (item["quote"] is null) continue;

                    item["quote"] = null;
                    withheld++;
                }
            }
        }

        if (withheld == 0) return packet;

        var ops = root["ops"] as JsonObject;
        if (ops is null)
        {
            ops = new JsonObject();
            root["ops"] = ops;
        }
        if (ops["qualityFlags"] is not JsonArray flags)
        {
            flags = new JsonArray();
            ops["qualityFlags"] = flags;
        }
        if (!ContainsValue(flags, WithheldFlag))
        {
            flags.Add(WithheldFlag);
        }

        return root;
    }

    /// <summary>
    /// Source references in this packet that fall inside the contained scope, by declared id or by the
    /// host actually recorded for the source.
    /// </summary>
    private static HashSet<string> ResolveContainedRefs(
        JsonObject packet,
        HashSet<string> containedIds,
        IReadOnlyList<string> hostSuffixes)
    {
        var refs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (packet["sources"] is JsonArray sources)
        {
            foreach (var node in sources)
            {
                if (node is not JsonObject source) continue;
                var sourceId = TryReadString(source["sourceId"])?.Trim();
                if (string.IsNullOrEmpty(sourceId)) continue;

                if (containedIds.Contains(sourceId) || HostIsContained(TryReadString(source["url"]), hostSuffixes))
                {
                    refs.Add(sourceId);
                }
            }
        }

        // An id named by the manifest is contained even if the packet declares no source record for it.
        foreach (var id in containedIds)
        {
            refs.Add(id);
        }

        return refs;
    }

    /// <summary>
    /// True when the URL's host equals a contained suffix or is a subdomain of it.
    /// </summary>
    /// <remarks>
    /// Matching is on host labels, never substrings. "drugbank.com" must not match
    /// "notdrugbank.com" or "drugbank.com.example.org"; "go.drugbank.com" must match. A substring test
    /// would both over-match unrelated hosts and invite a lookalike domain to slip past.
    /// </remarks>
    private static bool HostIsContained(string? url, IReadOnlyList<string> hostSuffixes)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        if (!Uri.TryCreate(url.Trim(), UriKind.Absolute, out var parsed)) return false;

        var host = parsed.Host.TrimEnd('.');
        if (host.Length == 0) return false;

        foreach (var suffix in hostSuffixes)
        {
            if (string.Equals(host, suffix, StringComparison.OrdinalIgnoreCase)) return true;
            if (host.EndsWith("." + suffix, StringComparison.OrdinalIgnoreCase)) return true;
        }

        return false;
    }

    /// <summary>
    /// Adds the contained class's registered aliases. Additive only: the registry can widen containment
    /// but can never narrow what the manifest names.
    /// </summary>
    private static void AddRegistryAliases(HashSet<string> containedIds, string classId, JsonNode? sourceRegistry)
    {
        if (sourceRegistry?["sources"] is not JsonArray sources) return;

        foreach (var node in sources)
        {
            if (node is not JsonObject entry) continue;
            if (entry["identity"] is not JsonObject identity) continue;
            if (!string.Equals(TryReadString(identity["sourceId"])?.Trim(), classId, StringComparison.OrdinalIgnoreCase)) continue;
            if (identity["aliases"] is not JsonArray aliases) continue;

            foreach (var alias in aliases)
            {
                var value = TryReadString(alias)?.Trim();
                if (value is { Length: > 0 }) containedIds.Add(value);
            }
        }
    }

    private static ContainmentManifest LoadManifest()
    {
        var assembly = typeof(RestrictedExcerptPolicy).Assembly;
        using var stream = assembly.GetManifestResourceStream(ManifestResourceName)
            ?? throw new InvalidOperationException(
                $"Containment manifest '{ManifestResourceName}' is missing from {assembly.GetName().Name}. " +
                "Excerpt containment cannot be enforced without it and the worker must not emit artifacts.");

        using var reader = new StreamReader(stream);
        var parsed = JsonNode.Parse(reader.ReadToEnd()) as JsonObject
            ?? throw new InvalidOperationException("Containment manifest is not a JSON object.");

        var sourceIds = ReadStringArray(parsed["sourceIds"]);
        var hostSuffixes = ReadStringArray(parsed["hostSuffixes"]);
        if (sourceIds.Count == 0 && hostSuffixes.Count == 0)
        {
            throw new InvalidOperationException(
                "Containment manifest names no source ids or host suffixes; refusing to run an empty rights control.");
        }

        return new ContainmentManifest(
            TryReadString(parsed["policyId"]) ?? "unknown",
            TryReadString(parsed["sourceClassId"]) ?? string.Empty,
            sourceIds,
            hostSuffixes);
    }

    private static List<string> ReadStringArray(JsonNode? node)
    {
        var values = new List<string>();
        if (node is not JsonArray array) return values;
        foreach (var item in array)
        {
            var value = TryReadString(item);
            if (value is { Length: > 0 }) values.Add(value);
        }
        return values;
    }

    private static bool ContainsValue(JsonArray array, string value)
    {
        foreach (var node in array)
        {
            if (string.Equals(TryReadString(node), value, StringComparison.OrdinalIgnoreCase)) return true;
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

    private sealed record ContainmentManifest(
        string PolicyId,
        string SourceClassId,
        IReadOnlyList<string> SourceIds,
        IReadOnlyList<string> HostSuffixes);
}
