using System.Text.Json.Nodes;

namespace BioStack.KnowledgeWorker.Pipeline;

public static class ResearchCategoryCatalog
{
    private static readonly Lazy<IReadOnlyDictionary<string, string>> Aliases = new(LoadAliases);

    public static IReadOnlyList<string> NormalizeMany(IEnumerable<string> categories)
        => categories
            .Select(Normalize)
            .Where(category => category.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

    private static string Normalize(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.Length == 0) return string.Empty;
        var key = trimmed
            .ToLowerInvariant()
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        while (key.Contains("  ", StringComparison.Ordinal))
        {
            key = key.Replace("  ", " ", StringComparison.Ordinal);
        }

        return Aliases.Value.TryGetValue(key.Trim(), out var canonical)
            ? canonical
            : trimmed;
    }

    private static IReadOnlyDictionary<string, string> LoadAliases()
    {
        var taxonomyPath = ResolveTaxonomyPath();
        if (taxonomyPath is null) return new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        var taxonomy = JsonNode.Parse(File.ReadAllText(taxonomyPath))?.AsObject();
        var aliases = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        foreach (var category in taxonomy?["categories"]?.AsArray() ?? new JsonArray())
        {
            var canonical = category?["name"]?.GetValue<string>()?.Trim() ?? string.Empty;
            if (canonical.Length == 0) continue;
            var deprecated = category?["deprecated"]?.GetValue<bool>() ?? false;
            var replacedBy = category?["replacedBy"]?.GetValue<string>()?.Trim() ?? string.Empty;
            var target = deprecated && replacedBy.Length > 0 ? replacedBy : canonical;

            aliases[NormalizeKey(canonical)] = target;

            foreach (var alias in category?["aliases"]?.AsArray() ?? new JsonArray())
            {
                var value = alias?.GetValue<string>()?.Trim() ?? string.Empty;
                if (value.Length == 0) continue;
                aliases[NormalizeKey(value)] = target;
            }
        }

        return aliases;
    }

    private static string NormalizeKey(string value)
    {
        var key = value
            .Trim()
            .ToLowerInvariant()
            .Replace("/", " ", StringComparison.Ordinal)
            .Replace("_", " ", StringComparison.Ordinal)
            .Replace("-", " ", StringComparison.Ordinal);

        while (key.Contains("  ", StringComparison.Ordinal))
        {
            key = key.Replace("  ", " ", StringComparison.Ordinal);
        }

        return key.Trim();
    }

    private static string? ResolveTaxonomyPath()
    {
        var current = new DirectoryInfo(AppContext.BaseDirectory);
        while (current is not null)
        {
            var candidate = Path.Combine(current.FullName, "research", "category-taxonomy.json");
            if (File.Exists(candidate)) return candidate;
            current = current.Parent;
        }

        var cwdCandidate = Path.Combine(Directory.GetCurrentDirectory(), "research", "category-taxonomy.json");
        return File.Exists(cwdCandidate) ? cwdCandidate : null;
    }
}