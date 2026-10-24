namespace BioStack.KnowledgeWorker.Discovery;

using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

/// <summary>
/// Name rules for discovery: normalization for matching, development code names
/// (ER-100, LY3437943, CT-388, SLU-PP-332, UBT251) and INN-stem names (orforglipron,
/// petrelintide, bimagrumab, nusinersen, ...).
/// </summary>
public static partial class CandidateNames
{
    // Uppercase prefix (optionally hyphen-joined segments), optional hyphen, 2-7 digits, optional
    // trailing letter. Lowercase words never match, so "week 52" or "phase 12" are not codes.
    [GeneratedRegex(@"(?<![A-Za-z0-9-])(?<prefix>[A-Z]{1,6}(?:-[A-Z]{1,6})*)-?(?<num>\d{2,7}[A-Za-z]?)(?![A-Za-z0-9])")]
    private static partial Regex CodeNameRegex();

    [GeneratedRegex(@"\b[A-Za-z][a-z]{4,}\b")]
    private static partial Regex WordRegex();

    // English and biochemistry words that happen to end in an INN stem.
    private static readonly HashSet<string> EnglishStemWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "peptide", "peptides", "polypeptide", "dipeptide", "tripeptide", "oligopeptide", "neuropeptide",
        "nucleotide", "oligonucleotide", "polynucleotide", "dinucleotide", "mononucleotide", "riptide",
        "concept", "except", "accept", "intercept", "percept", "precept", "excel", "cancel", "parcel",
        "worsen", "coarsen", "hoarsen", "lipopeptide", "glycopeptide", "aptide",
    };

    /// <summary>Lowercase letters and digits only: "ER-100" == "er 100" == "ER100".</summary>
    public static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var sb = new StringBuilder(value.Length);
        foreach (var c in value.Normalize(NormalizationForm.FormKD))
        {
            if (char.IsLetterOrDigit(c) && c < 128) sb.Append(char.ToLowerInvariant(c));
        }
        return sb.ToString();
    }

    /// <summary>Development code names in <paramref name="text"/>, hyphenated canonically ("ER100" stays as written).</summary>
    public static IReadOnlyList<string> ExtractCodeNames(string? text, IReadOnlyCollection<string> prefixDenylist)
    {
        if (string.IsNullOrWhiteSpace(text)) return Array.Empty<string>();
        var deny = prefixDenylist as ISet<string> ?? new HashSet<string>(prefixDenylist, StringComparer.OrdinalIgnoreCase);
        var found = new List<string>();
        foreach (Match m in CodeNameRegex().Matches(text))
        {
            var prefix = m.Groups["prefix"].Value;
            var first = prefix.Split('-')[0];
            if (deny.Contains(first) || deny.Contains(prefix)) continue;
            // Years and plain counts with a one-letter prefix (e.g. "Q2024") are not codes.
            if (first.Length == 1 && m.Groups["num"].Value.Length < 3) continue;
            if (!found.Contains(m.Value, StringComparer.OrdinalIgnoreCase)) found.Add(m.Value);
        }
        return found;
    }

    /// <summary>Words that end in a configured INN stem, title-cased ("orforglipron" → "Orforglipron").</summary>
    public static IReadOnlyList<(string Name, string Classification)> ExtractInnNames(string? text, IReadOnlyList<InnStem> stems)
    {
        if (string.IsNullOrWhiteSpace(text) || stems.Count == 0) return Array.Empty<(string, string)>();
        var found = new List<(string, string)>();
        foreach (Match m in WordRegex().Matches(text))
        {
            var word = m.Value.ToLowerInvariant();
            if (EnglishStemWords.Contains(word)) continue;
            var stem = stems.FirstOrDefault(s => word.EndsWith(s.Suffix, StringComparison.Ordinal) && word.Length >= s.Suffix.Length + 4);
            if (stem is null) continue;
            var name = CultureInfo.InvariantCulture.TextInfo.ToTitleCase(word);
            if (!found.Any(f => f.Item1 == name)) found.Add((name, stem.Classification));
        }
        return found;
    }

    /// <summary>Classification implied by an INN stem in <paramref name="name"/>, or null.</summary>
    public static string? StemClassification(string name, IReadOnlyList<InnStem> stems)
    {
        var lower = name.Trim().ToLowerInvariant();
        if (lower.Contains(' ') || lower.Any(char.IsDigit)) return null;
        return stems.FirstOrDefault(s => lower.EndsWith(s.Suffix, StringComparison.Ordinal) && lower.Length >= s.Suffix.Length + 4)?.Classification;
    }

    public static bool IsCodeName(string name, IReadOnlyCollection<string> prefixDenylist)
        => ExtractCodeNames(name, prefixDenylist).Any(c => Normalize(c) == Normalize(name));

    /// <summary>kebab-case id that satisfies the substance schema's canonicalId/slug pattern.</summary>
    public static string Slug(string name)
    {
        var sb = new StringBuilder();
        var dash = false;
        foreach (var c in name.Normalize(NormalizationForm.FormKD))
        {
            if (char.IsLetterOrDigit(c) && c < 128) { sb.Append(char.ToLowerInvariant(c)); dash = false; }
            else if (!dash && sb.Length > 0) { sb.Append('-'); dash = true; }
        }
        return sb.ToString().Trim('-');
    }
}
