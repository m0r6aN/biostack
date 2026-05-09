namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;

public sealed record PromotionImportPreview(
    string PreviewVersion,
    DateTimeOffset GeneratedAtUtc,
    PromotionImportPreviewCounts Counts,
    IReadOnlyList<PromotionImportPreviewItem> Items);

public sealed record PromotionImportPreviewCounts(
    int TotalExported,
    int WouldCreate,
    int WouldUpdate,
    int WouldSkip,
    int SchemaValid,
    int SchemaInvalid,
    int DuplicateSlugs,
    int DuplicateCanonicalIds,
    int ActiveRecords,
    int InactiveRecords);

public sealed record PromotionImportPreviewItem(
    string Name,
    string Slug,
    string CanonicalId,
    string Action,
    bool SchemaValid,
    bool IsActive,
    bool ExistingSeedMatch,
    IReadOnlyList<string> Reasons,
    IReadOnlyList<string> ReviewDecisionIds);

public interface IPromotionImportPreviewBuilder
{
    PromotionImportPreview Build(
        JsonArray exportedSubstances,
        PromotionExportManifest exportManifest,
        JsonArray existingSeedSubstances,
        ISubstanceRecordValidator validator);
}

public sealed class PromotionImportPreviewBuilder : IPromotionImportPreviewBuilder
{
    public PromotionImportPreview Build(
        JsonArray exportedSubstances,
        PromotionExportManifest exportManifest,
        JsonArray existingSeedSubstances,
        ISubstanceRecordValidator validator)
    {
        if (exportedSubstances is null) throw new ArgumentNullException(nameof(exportedSubstances));
        if (exportManifest is null) throw new ArgumentNullException(nameof(exportManifest));
        if (existingSeedSubstances is null) throw new ArgumentNullException(nameof(existingSeedSubstances));
        if (validator is null) throw new ArgumentNullException(nameof(validator));

        var existingKeys = ExistingKeys(existingSeedSubstances);
        var exportSlugCounts = exportedSubstances
            .Where(n => n is not null)
            .Select(n => ReadString(n!["identity"]?["slug"]))
            .Where(s => s.Length > 0)
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var exportIdCounts = exportedSubstances
            .Where(n => n is not null)
            .Select(n => ReadString(n!["identity"]?["canonicalId"]))
            .Where(s => s.Length > 0)
            .GroupBy(s => s, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);
        var candidatesByName = exportManifest.Candidates
            .GroupBy(c => c.Name, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var items = exportedSubstances
            .Where(n => n is not null)
            .Select(node => ToItem(node!, validator, existingKeys, exportSlugCounts, exportIdCounts, candidatesByName))
            .OrderBy(i => i.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new PromotionImportPreview(
            PreviewVersion: "1.0.0",
            GeneratedAtUtc: DateTimeOffset.UtcNow,
            Counts: new PromotionImportPreviewCounts(
                TotalExported: items.Count,
                WouldCreate: items.Count(i => i.Action == "create"),
                WouldUpdate: items.Count(i => i.Action == "update"),
                WouldSkip: items.Count(i => i.Action == "skip"),
                SchemaValid: items.Count(i => i.SchemaValid),
                SchemaInvalid: items.Count(i => !i.SchemaValid),
                DuplicateSlugs: exportSlugCounts.Count(kv => kv.Value > 1),
                DuplicateCanonicalIds: exportIdCounts.Count(kv => kv.Value > 1),
                ActiveRecords: items.Count(i => i.IsActive),
                InactiveRecords: items.Count(i => !i.IsActive)),
            Items: items);
    }

    private static PromotionImportPreviewItem ToItem(
        JsonNode node,
        ISubstanceRecordValidator validator,
        HashSet<string> existingKeys,
        IReadOnlyDictionary<string, int> exportSlugCounts,
        IReadOnlyDictionary<string, int> exportIdCounts,
        IReadOnlyDictionary<string, PromotionExportCandidate> candidatesByName)
    {
        var name = ReadString(node["identity"]?["canonicalName"]);
        var slug = ReadString(node["identity"]?["slug"]);
        var canonicalId = ReadString(node["identity"]?["canonicalId"]);
        var isActive = ReadBool(node["ops"]?["isActive"]);
        var schemaResult = validator.Validate(node);
        var reasons = new List<string>();

        if (!schemaResult.IsValid) reasons.Add($"schema invalid: {schemaResult.Summary()}");
        if (name.Length == 0) reasons.Add("missing canonical name");
        if (slug.Length == 0) reasons.Add("missing slug");
        if (canonicalId.Length == 0) reasons.Add("missing canonicalId");
        if (slug.Length > 0 && exportSlugCounts.TryGetValue(slug, out var slugCount) && slugCount > 1) reasons.Add("duplicate slug in export");
        if (canonicalId.Length > 0 && exportIdCounts.TryGetValue(canonicalId, out var idCount) && idCount > 1) reasons.Add("duplicate canonicalId in export");
        if (isActive) reasons.Add("exported record is active; expected inactive dry-run artifact");

        var existingMatch = existingKeys.Contains(slug) || existingKeys.Contains(canonicalId);
        var action = reasons.Count > 0 ? "skip" : existingMatch ? "update" : "create";
        var reviewDecisionIds = candidatesByName.TryGetValue(name, out var candidate)
            ? candidate.ReviewDecisionIds
            : Array.Empty<string>();

        return new PromotionImportPreviewItem(
            Name: name,
            Slug: slug,
            CanonicalId: canonicalId,
            Action: action,
            SchemaValid: schemaResult.IsValid,
            IsActive: isActive,
            ExistingSeedMatch: existingMatch,
            Reasons: reasons,
            ReviewDecisionIds: reviewDecisionIds);
    }

    private static HashSet<string> ExistingKeys(JsonArray existingSeedSubstances)
    {
        var keys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var node in existingSeedSubstances.Where(n => n is not null))
        {
            var slug = ReadString(node!["identity"]?["slug"]);
            var id = ReadString(node!["identity"]?["canonicalId"]);
            if (slug.Length > 0) keys.Add(slug);
            if (id.Length > 0) keys.Add(id);
        }
        return keys;
    }

    private static string ReadString(JsonNode? node)
        => node?.GetValue<string>()?.Trim() ?? string.Empty;

    private static bool ReadBool(JsonNode? node)
        => node is JsonValue value && value.TryGetValue<bool>(out var result) && result;
}