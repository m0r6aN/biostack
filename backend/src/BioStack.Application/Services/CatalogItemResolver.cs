namespace BioStack.Application.Services;

using BioStack.Domain.Enums;

/// <summary>A published catalog record, reduced to what linking and the cap need.</summary>
public sealed record CatalogItem(string Slug, string Name, CompoundCategory Category, IReadOnlyList<string> Aliases);

/// <summary>Reads the live public catalog. Read-only; nothing here leaves the server.</summary>
public interface ICatalogItemResolver
{
    Task<CatalogItem?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default);
    Task<IReadOnlyDictionary<string, CatalogItem>> FindBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<CatalogItem>> ListAsync(CancellationToken cancellationToken = default);
}

public sealed class KnowledgeCatalogItemResolver : ICatalogItemResolver
{
    private readonly IKnowledgeService _knowledge;

    public KnowledgeCatalogItemResolver(IKnowledgeService knowledge)
    {
        _knowledge = knowledge;
    }

    public async Task<CatalogItem?> FindBySlugAsync(string slug, CancellationToken cancellationToken = default)
    {
        var found = await FindBySlugsAsync([slug], cancellationToken);
        return found.TryGetValue(Normalize(slug), out var item) ? item : null;
    }

    public async Task<IReadOnlyDictionary<string, CatalogItem>> FindBySlugsAsync(IEnumerable<string> slugs, CancellationToken cancellationToken = default)
    {
        var wanted = slugs.Select(Normalize).Where(s => s.Length > 0).ToHashSet(StringComparer.Ordinal);
        var result = new Dictionary<string, CatalogItem>(StringComparer.Ordinal);
        if (wanted.Count == 0)
            return result;

        foreach (var item in await ListAsync(cancellationToken))
        {
            if (wanted.Contains(item.Slug) && !result.ContainsKey(item.Slug))
                result[item.Slug] = item;
        }

        return result;
    }

    public async Task<IReadOnlyList<CatalogItem>> ListAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<CatalogItem>();
        foreach (var entry in await _knowledge.GetAllCompoundsAsync(cancellationToken))
        {
            var key = Normalize(entry.Slug);
            if (key.Length > 0)
                items.Add(new CatalogItem(key, entry.CanonicalName, entry.Classification, entry.Aliases));
        }

        return items;
    }

    public static string Normalize(string? slug) => (slug ?? string.Empty).Trim().ToLowerInvariant();
}
