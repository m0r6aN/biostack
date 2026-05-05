namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;

public sealed record PromotionExportResult(
    string ExportDirectory,
    string SubstancesDirectory,
    string AggregatePath,
    string ManifestPath,
    int ExportedCount,
    IReadOnlyList<string> SkippedCompounds);

public sealed record PromotionExportManifest(
    string ManifestVersion,
    DateTimeOffset GeneratedAtUtc,
    int ExportedCount,
    IReadOnlyList<PromotionExportCandidate> Candidates,
    IReadOnlyList<string> SkippedCompounds);

public sealed record PromotionExportCandidate(
    string Name,
    string Slug,
    string Readiness,
    string SubstanceFile,
    int AggregateIndex,
    IReadOnlyList<string> ReviewDecisionIds,
    IReadOnlyList<string> QualityFlags);

public interface IPromotionExporter
{
    PromotionExportResult Export(JsonArray draftSubstances, PromotionManifest promotionManifest, string outputDirectory);
}

public sealed class PromotionExporter : IPromotionExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public PromotionExportResult Export(JsonArray draftSubstances, PromotionManifest promotionManifest, string outputDirectory)
    {
        if (draftSubstances is null) throw new ArgumentNullException(nameof(draftSubstances));
        if (promotionManifest is null) throw new ArgumentNullException(nameof(promotionManifest));
        if (string.IsNullOrWhiteSpace(outputDirectory)) throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

        var exportDir = Path.Combine(outputDirectory, "promotion-export");
        var substancesDir = Path.Combine(exportDir, "substances");
        Directory.CreateDirectory(substancesDir);

        var aggregate = new JsonArray();
        var candidates = new List<PromotionExportCandidate>();
        var skipped = new List<string>();
        var draftsByName = draftSubstances
            .Where(node => node is not null)
            .Select(node => node!)
            .GroupBy(node => ReadString(node["identity"]?["canonicalName"]), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        foreach (var candidate in promotionManifest.CandidatesForPromotion.OrderBy(c => c.Name, StringComparer.OrdinalIgnoreCase))
        {
            if (!draftsByName.TryGetValue(candidate.Name, out var draft))
            {
                skipped.Add($"{candidate.Name}: draft not found");
                continue;
            }

            var clone = JsonNode.Parse(draft.ToJsonString())!;
            ForceInactive(clone);
            var slug = ReadString(clone["identity"]?["slug"]);
            if (slug.Length == 0) slug = SubstanceRecordNormalizer.Slugify(candidate.Name);
            var substancePath = Path.Combine(substancesDir, $"{slug}.json");
            File.WriteAllText(substancePath, clone.ToJsonString(JsonOptions));

            var aggregateIndex = aggregate.Count;
            aggregate.Add(JsonNode.Parse(clone.ToJsonString())!);
            candidates.Add(new PromotionExportCandidate(
                Name: candidate.Name,
                Slug: slug,
                Readiness: candidate.Readiness,
                SubstanceFile: substancePath,
                AggregateIndex: aggregateIndex,
                ReviewDecisionIds: candidate.ReviewDecisionIds,
                QualityFlags: candidate.QualityFlags));
        }

        var aggregatePath = Path.Combine(exportDir, "substances.promotable.json");
        var manifestPath = Path.Combine(exportDir, "promotion-export-manifest.json");
        var manifest = new PromotionExportManifest(
            ManifestVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            ExportedCount: candidates.Count,
            Candidates: candidates,
            SkippedCompounds: skipped);

        File.WriteAllText(aggregatePath, aggregate.ToJsonString(JsonOptions));
        File.WriteAllText(manifestPath, JsonSerializer.Serialize(manifest, JsonOptions));

        return new PromotionExportResult(
            ExportDirectory: exportDir,
            SubstancesDirectory: substancesDir,
            AggregatePath: aggregatePath,
            ManifestPath: manifestPath,
            ExportedCount: candidates.Count,
            SkippedCompounds: skipped);
    }

    private static void ForceInactive(JsonNode draft)
    {
        var ops = draft["ops"]?.AsObject();
        if (ops is not null) ops["isActive"] = false;
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;
}