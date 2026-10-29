namespace BioStack.Application.Services;

using System.Globalization;
using System.Text;
using BioStack.Domain.Entities;

/// <summary>One catalog record the matcher offers for a typed name. Catalog facts only: nothing here comes from other users.</summary>
public sealed record CatalogMatch(string Slug, string Name, string Kind, string Label, float Score, string Reason);

/// <summary>
/// Finds the catalog records a typed name may refer to. Pure and deterministic: no I/O, no clock, no network.
/// "Same as" is for the same name written differently (exact name, alternate name or brand, slug).
/// "Similar" is for near misses (the words are part of a name, an abbreviation, or a small spelling difference).
/// BioStack only ever offers these: the person decides, and nothing is linked automatically.
/// </summary>
public static class CatalogMatcher
{
    public const int MaxCandidates = 3;

    /// <summary>Words that describe the form of an item, not what it is. Dropped from the typed name before matching.</summary>
    private static readonly HashSet<string> FormWords = new(StringComparer.Ordinal)
    {
        "capsule", "capsules", "tablet", "tablets", "softgel", "softgels", "powder", "extract", "supplement", "supplements",
        "gummy", "gummies", "liquid",
    };

    /// <summary>Words too common to justify a "Similar" offer on their own ("vitamin", "acid", "oil").</summary>
    private static readonly HashSet<string> GenericWords = new(StringComparer.Ordinal)
    {
        "vitamin", "vitamins", "acid", "acids", "oil", "oils", "complex", "mineral", "minerals", "amino", "protein", "root",
        "leaf", "bark", "seed", "extract", "powder", "the", "and", "of", "with", "plus", "forte",
    };

    /// <summary>The name reduced to lowercase letters and digits ("ER-100" == "er 100" == "ER100"). Used as the lookup key.</summary>
    public static string Key(string? value) => string.Concat(Tokens(value));

    public static IReadOnlyList<CatalogMatch> Match(string? query, IReadOnlyList<CatalogItem> catalog, int max = MaxCandidates)
    {
        var tokens = Tokens(query).ToList();
        var identity = tokens.Where(t => !FormWords.Contains(t)).ToList();
        if (identity.Count > 0) tokens = identity;
        var key = string.Concat(tokens);
        if (key.Length == 0 || catalog.Count == 0) return [];

        var matches = new List<CatalogMatch>();
        foreach (var item in catalog)
        {
            var best = BestFor(item, tokens, key);
            if (best is not null) matches.Add(best);
        }

        return matches
            .OrderBy(m => m.Label == CatalogMatchLabel.SameAs ? 0 : 1)
            .ThenByDescending(m => m.Score)
            .ThenBy(m => m.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(m => m.Slug, StringComparer.Ordinal)
            .Take(max)
            .ToList();
    }

    private static CatalogMatch? BestFor(CatalogItem item, List<string> queryTokens, string queryKey)
    {
        CatalogMatch? best = null;
        void Offer(CatalogMatch? candidate)
        {
            if (candidate is not null && (best is null || candidate.Score > best.Score)) best = candidate;
        }

        var nameTokens = Tokens(item.Name).ToList();
        if (Matches(item, queryKey, nameTokens, queryTokens, "name", item.Name) is { } byName) Offer(byName);

        var slugKey = Key(item.Slug);
        if (slugKey.Length > 0 && slugKey == queryKey)
            Offer(new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Slug, CatalogMatchLabel.SameAs, 0.97f, "has the same address in BioStack's library"));

        foreach (var alias in item.Aliases)
        {
            if (string.IsNullOrWhiteSpace(alias)) continue;
            var aliasTokens = Tokens(alias).ToList();
            Offer(Matches(item, queryKey, aliasTokens, queryTokens, "alias", alias));
        }

        return best;
    }

    private static CatalogMatch? Matches(
        CatalogItem item, string queryKey, List<string> candidateTokens, List<string> queryTokens, string source, string shownName)
    {
        var candidateKey = string.Concat(candidateTokens);
        if (candidateKey.Length == 0) return null;

        if (candidateKey == queryKey)
        {
            return source == "name"
                ? new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Exact, CatalogMatchLabel.SameAs, 1.0f, "has the same name")
                : new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Alias, CatalogMatchLabel.SameAs, 0.98f,
                    $"matches its alternate name \"{shownName}\"");
        }

        var forWhat = source == "name" ? "its name" : $"its alternate name \"{shownName}\"";

        // Abbreviation: same number of words, each typed word is the start of the matching word ("vit d3" -> "vitamin d3").
        if (queryTokens.Count == candidateTokens.Count && queryTokens.Count > 0 && queryKey != candidateKey
            && queryTokens.Zip(candidateTokens).All(pair => pair.Second.StartsWith(pair.First, StringComparison.Ordinal)
                && (pair.First.Length >= 3 || pair.First == pair.Second))
            && queryTokens.Zip(candidateTokens).Any(pair => pair.First != pair.Second))
        {
            return new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Partial, CatalogMatchLabel.Similar, 0.85f,
                $"may be a short form of {forWhat}");
        }

        // Words that are part of the other name ("magnesium glycinate" vs "magnesium").
        var shared = queryTokens.Intersect(candidateTokens).ToList();
        if (shared.Count > 0 && shared.Any(t => !GenericWords.Contains(t) && t.Length >= 3))
        {
            var queryInside = queryTokens.All(candidateTokens.Contains);
            var candidateInside = candidateTokens.All(queryTokens.Contains);
            if (queryInside || candidateInside)
            {
                var score = 0.8f * shared.Count / Math.Max(queryTokens.Count, candidateTokens.Count);
                return new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Partial, CatalogMatchLabel.Similar, Math.Max(score, 0.4f),
                    queryInside ? $"{forWhat} includes the words you typed" : $"you typed more than {forWhat}");
            }
        }

        // Spelling: one word is a small edit away, or shares most of its character triplets ("cretine", "ashwaganda").
        // Only a long, purely alphabetic word may differ, and every other word must match exactly, so "vitamin d" never
        // becomes "vitamin c" and "bpc 157" never becomes "bpc 158".
        if (queryTokens.Count == candidateTokens.Count && queryKey.Length >= 5)
        {
            var differing = queryTokens.Zip(candidateTokens).Where(pair => pair.First != pair.Second).ToList();
            if (differing.Count == 1)
            {
                var (typed, known) = differing[0];
                if (typed.Length >= 5 && known.Length >= 5 && typed.All(char.IsLetter) && known.All(char.IsLetter)
                    && Math.Abs(typed.Length - known.Length) <= 2)
                {
                    var allowed = Math.Max(typed.Length, known.Length) >= 9 ? 2 : 1;
                    var distance = EditDistance(typed, known, allowed);
                    if (distance <= allowed || TrigramJaccard(typed, known) >= 0.8)
                    {
                        var score = distance <= allowed ? 0.7f - 0.1f * (distance - 1) : 0.6f;
                        return new CatalogMatch(item.Slug, item.Name, CatalogMatchKind.Fuzzy, CatalogMatchLabel.Similar, Math.Max(score, 0.5f),
                            $"is spelled almost the same as {forWhat}");
                    }
                }
            }
        }

        return null;
    }

    private static IEnumerable<string> Tokens(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) yield break;
        var token = new StringBuilder();
        foreach (var c in value.Normalize(NormalizationForm.FormKD))
        {
            var category = CharUnicodeInfo.GetUnicodeCategory(c);
            if (category == UnicodeCategory.NonSpacingMark) continue; // accents
            if (char.IsLetterOrDigit(c))
            {
                token.Append(char.ToLowerInvariant(c));
            }
            else if (token.Length > 0)
            {
                yield return token.ToString();
                token.Clear();
            }
        }

        if (token.Length > 0) yield return token.ToString();
    }

    /// <summary>Optimal string alignment distance (insert, delete, replace, swap adjacent). Stops early above <paramref name="limit"/>.</summary>
    internal static int EditDistance(string a, string b, int limit)
    {
        if (Math.Abs(a.Length - b.Length) > limit) return limit + 1;
        var d = new int[a.Length + 1, b.Length + 1];
        for (var i = 0; i <= a.Length; i++) d[i, 0] = i;
        for (var j = 0; j <= b.Length; j++) d[0, j] = j;
        for (var i = 1; i <= a.Length; i++)
        {
            for (var j = 1; j <= b.Length; j++)
            {
                var cost = a[i - 1] == b[j - 1] ? 0 : 1;
                d[i, j] = Math.Min(Math.Min(d[i - 1, j] + 1, d[i, j - 1] + 1), d[i - 1, j - 1] + cost);
                if (i > 1 && j > 1 && a[i - 1] == b[j - 2] && a[i - 2] == b[j - 1])
                    d[i, j] = Math.Min(d[i, j], d[i - 2, j - 2] + 1);
            }
        }

        return d[a.Length, b.Length];
    }

    internal static double TrigramJaccard(string a, string b)
    {
        static HashSet<string> Grams(string s)
        {
            var set = new HashSet<string>(StringComparer.Ordinal);
            for (var i = 0; i + 3 <= s.Length; i++) set.Add(s.Substring(i, 3));
            return set;
        }

        var left = Grams(a);
        var right = Grams(b);
        if (left.Count == 0 || right.Count == 0) return 0;
        var intersection = left.Count(right.Contains);
        return (double)intersection / (left.Count + right.Count - intersection);
    }
}
