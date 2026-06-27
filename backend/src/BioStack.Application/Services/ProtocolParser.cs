namespace BioStack.Application.Services;

using System.Text.RegularExpressions;
using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;
using BioStack.Infrastructure.Knowledge;
using Microsoft.Extensions.Caching.Memory;

public sealed class ProtocolParser : IProtocolParser
{
    private static readonly Regex DosePattern = new(
        @"(?<dose>\d+(?:\.\d+)?)\s*(?<unit>mcg|micrograms?|ug|μg|mg|milligrams?)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex DurationPattern = new(
        @"(?<duration>\b\d+(?:\.\d+)?\s*(?:day|days|week|weeks|month|months|cycle|cycles)\b)",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private static readonly Regex FrequencyPattern = new(
        @"\b(?<frequency>daily|twice daily|once daily|weekly|twice weekly|three times weekly|3x weekly|2x weekly|every other day|eod|morning|nightly|evening|pre[-\s]?workout|post[-\s]?workout)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Matches cycle/duration phrases that are not compounds, e.g.:
    //   "8 weeks on, 8 weeks off", "8w on / 8w off", "cycle 8 weeks on 8 off"
    // These must never be emitted as a compound row.
    private static readonly Regex CyclePattern = new(
        @"\b\d+\s*(?:w|wk|wks|week|weeks|d|day|days|month|months)\b\s*(?:on|off)\b.*?\b(?:on|off)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    // Dosing-table schedule codes ("W1-2", "W7-15", "D3") used to label phase
    // windows. They carry doses but are never compound names.
    private static readonly Regex ScheduleCodePattern = new(
        @"^[wd]\d",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    private readonly IKnowledgeSource _knowledgeSource;
    private readonly IBlendDecomposerService _blendDecomposerService;
    private readonly IMemoryCache _memoryCache;

    public ProtocolParser(
        IKnowledgeSource knowledgeSource,
        IBlendDecomposerService blendDecomposerService,
        IMemoryCache memoryCache)
    {
        _knowledgeSource = knowledgeSource;
        _blendDecomposerService = blendDecomposerService;
        _memoryCache = memoryCache;
    }

    public async Task<ProtocolParseResult> ParseAsync(string inputText, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(inputText))
        {
            return new ProtocolParseResult(
                new List<ProtocolEntryResponse>(),
                new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase),
                new List<ProtocolBlendExpansionResponse>());
        }

        var knowledge = await _memoryCache.GetOrCreateAsync("analyzer:knowledge:aliases", async entry =>
        {
            entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(24);
            return await _knowledgeSource.GetAllCompoundsAsync(cancellationToken);
        }) ?? new List<KnowledgeEntry>();
        var aliases = BuildAliasMap(knowledge);
        var blendExpansions = new List<ProtocolBlendExpansionResponse>();
        var entries = SplitIntoSegments(inputText)
            .SelectMany(segment => ParseSegment(segment, aliases, blendExpansions))
            .Where(entry => !string.IsNullOrWhiteSpace(entry.CompoundName))
            .GroupBy(entry => entry.CompoundName, StringComparer.OrdinalIgnoreCase)
            .Select(MergeEntries)
            .ToList();

        var matchedKnowledge = entries
            .Select(entry => aliases.TryGetValue(NormalizeLookupKey(entry.CompoundName), out var match) ? match : null)
            .Where(entry => entry is not null)
            .Cast<KnowledgeEntry>()
            .GroupBy(entry => entry.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First(), StringComparer.OrdinalIgnoreCase);

        return new ProtocolParseResult(entries, matchedKnowledge, blendExpansions);
    }

    private IEnumerable<ProtocolEntryResponse> ParseSegment(
        string segment,
        IReadOnlyDictionary<string, KnowledgeEntry> aliases,
        ICollection<ProtocolBlendExpansionResponse> blendExpansions)
    {
        // Cycle phrases like "8 weeks on, 8 weeks off" describe duration/cadence,
        // not a compound. Strip the cycle phrase from the segment rather than
        // discarding the whole thing, so that a compound written on the same line
        // is still parsed (e.g. "BPC-157 500mcg daily — 8 weeks on, 8 weeks off").
        segment = CyclePattern.Replace(segment, string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(segment))
        {
            return Array.Empty<ProtocolEntryResponse>();
        }

        var cleaned = Regex.Replace(segment.Trim(), @"^[\-\*\u2022\d\.\)\s]+", string.Empty).Trim();
        var frequency = ExtractMatch(FrequencyPattern, cleaned);
        var duration = ExtractMatch(DurationPattern, cleaned);
        var blend = _blendDecomposerService.Decompose(cleaned);
        if (blend.Components.Count > 0)
        {
            blendExpansions.Add(new ProtocolBlendExpansionResponse(blend.BlendName, blend.Components));
            var doseMatches = DosePattern.Matches(cleaned)
                .Select(match => match)
                .ToList();
            var recommendedDoses = doseMatches.Count >= blend.Components.Count
                ? doseMatches.TakeLast(blend.Components.Count).ToList()
                : new List<Match>();

            return blend.Components.Select((component, index) =>
            {
                var dose = 0d;
                var unit = string.Empty;
                if (recommendedDoses.Count == blend.Components.Count)
                {
                    var doseMatch = recommendedDoses[index];
                    double.TryParse(doseMatch.Groups["dose"].Value, out dose);
                    unit = NormalizeUnit(doseMatch.Groups["unit"].Value);
                }

                var resolved = ResolveCanonicalName(component, aliases);
                var recognized = aliases.ContainsKey(NormalizeLookupKey(component));
                return new ProtocolEntryResponse(
                    resolved,
                    dose,
                    unit,
                    NormalizeFrequency(frequency),
                    duration,
                    Recognized: recognized);
            }).ToList();
        }

        var singleDoseMatch = DosePattern.Match(cleaned);
        var parsedDose = 0d;
        var parsedUnit = string.Empty;

        if (singleDoseMatch.Success)
        {
            double.TryParse(singleDoseMatch.Groups["dose"].Value, out parsedDose);
            parsedUnit = NormalizeUnit(singleDoseMatch.Groups["unit"].Value);
        }

        // Recognition gate: a segment only becomes a compound entry when there is
        // real evidence it names one. Without this gate, every prose paragraph,
        // section header, table-structure row, note, and citation in an uploaded
        // document is emitted as a fake "Unknown" compound (the "102 found, 4
        // normalized" failure — see ProtocolAnalyzerDocxPacketGoldenTests).
        //
        //   (a) the segment contains a known compound alias, OR
        //   (b) it carries a dose or explicit frequency AND the text before that
        //       token is shaped like a compound name (a short, compound-like token
        //       — not prose, a header, a schedule code, or a "label: value" fragment).
        var aliasName = ResolveAliasName(cleaned, aliases);
        if (aliasName is not null)
        {
            return new[]
            {
                new ProtocolEntryResponse(
                    aliasName,
                    parsedDose,
                    parsedUnit,
                    NormalizeFrequency(frequency),
                    duration,
                    Recognized: true)
            };
        }

        // A dose or an explicit frequency ("Semaglutide weekly") marks a dosing line.
        // Duration alone is deliberately excluded: "Weeks 1-15" style phrases pair
        // with planning prose far more often than with a real compound.
        var frequencyMatch = FrequencyPattern.Match(cleaned);
        if (!singleDoseMatch.Success && !frequencyMatch.Success)
        {
            return Array.Empty<ProtocolEntryResponse>();
        }

        // The compound name is whatever precedes the first dose/frequency token.
        // Slicing (rather than stripping frequency words out of the whole segment)
        // keeps headings like "Daily and Weekly Tracking Templates" from collapsing
        // to a fake "Tracking Templates" compound: the frequency leads, so the
        // name-slice is empty and the segment is rejected.
        var nameCutIndex = singleDoseMatch.Success ? singleDoseMatch.Index : frequencyMatch.Index;
        var nameSlice = BuildNameSlice(cleaned, nameCutIndex);
        if (!IsLikelyCompoundName(nameSlice))
        {
            return Array.Empty<ProtocolEntryResponse>();
        }

        return new[]
        {
            new ProtocolEntryResponse(
                nameSlice,
                parsedDose,
                parsedUnit,
                NormalizeFrequency(frequency),
                duration)
        };
    }

    // When the same compound is emitted by both a blend header (no dose) and a
    // dedicated detail line ("BPC-157 500mcg daily"), merge into one entry that
    // preserves the richest data: highest dose, and any populated unit/freq/duration.
    private static ProtocolEntryResponse MergeEntries(IGrouping<string, ProtocolEntryResponse> group)
    {
        var members = group.ToList();
        if (members.Count == 1)
        {
            return members[0];
        }

        var canonicalName = members
            .Select(entry => entry.CompoundName)
            .FirstOrDefault(name => !string.IsNullOrWhiteSpace(name)) ?? group.Key;

        var bestDoseEntry = members
            .OrderByDescending(entry => entry.Dose)
            .ThenByDescending(entry => string.IsNullOrWhiteSpace(entry.Unit) ? 0 : 1)
            .First();

        var unit = !string.IsNullOrWhiteSpace(bestDoseEntry.Unit)
            ? bestDoseEntry.Unit
            : members.Select(entry => entry.Unit).FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        var frequency = members
            .Select(entry => entry.Frequency)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        var duration = members
            .Select(entry => entry.Duration)
            .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;

        // Recognized is true if ANY of the merged entries is recognized
        var recognized = members.Any(entry => entry.Recognized);

        return new ProtocolEntryResponse(canonicalName, bestDoseEntry.Dose, unit, frequency, duration, recognized);
    }

    // Returns the canonical name when the segment contains a known compound alias,
    // otherwise null. The longest alias wins so "BPC-157" beats a bare "BPC".
    private static string? ResolveAliasName(string segment, IReadOnlyDictionary<string, KnowledgeEntry> aliases)
    {
        var directMatch = aliases
            .OrderByDescending(alias => alias.Key.Length)
            .FirstOrDefault(alias => ContainsAlias(segment, alias.Key));

        return directMatch.Value?.CanonicalName;
    }

    // Extracts the candidate compound-name text that precedes the dose/frequency
    // token (at cutIndex), stripping any residual frequency/duration phrases,
    // dosing verbs, and table/label punctuation.
    private static string BuildNameSlice(string segment, int cutIndex)
    {
        var nameSlice = cutIndex > 0 && cutIndex <= segment.Length ? segment[..cutIndex] : string.Empty;
        nameSlice = FrequencyPattern.Replace(nameSlice, string.Empty);
        nameSlice = DurationPattern.Replace(nameSlice, string.Empty);
        nameSlice = Regex.Replace(nameSlice, @"\b(take|inject|dose|with|for|at|on|and)\b", " ", RegexOptions.IgnoreCase);
        nameSlice = Regex.Replace(nameSlice, @"[,:;|]+", " ");
        nameSlice = Regex.Replace(nameSlice, @"\s+", " ").Trim();

        return nameSlice;
    }

    // A dose alone is not enough to call a segment a compound. Real compound names
    // are short and "name-shaped" — a capitalized word (Semaglutide), an all-caps
    // token (NAD, KPV), or an alphanumeric/hyphenated token (BPC-157, GHK-Cu,
    // TB-500). Prose words ("planning", "titration", "up"), short Title-case
    // function words ("At", "To"), section headers, schedule codes ("W7-15"), and
    // bare numbers are rejected. Lowercase compound names are still caught upstream
    // by the alias path.
    private static bool IsLikelyCompoundName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length is < 2 or > 40)
        {
            return false;
        }

        var tokens = name.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (tokens.Length is 0 or > 3)
        {
            return false;
        }

        foreach (var token in tokens)
        {
            // Must contain a letter — pure numbers/ranges ("100", "2-3x") are doses.
            if (!token.Any(char.IsLetter))
            {
                return false;
            }

            // Dosing-table schedule codes ("W1-2", "W7-15", "D3") are not compounds.
            if (ScheduleCodePattern.IsMatch(token))
            {
                return false;
            }

            var isAlphabetic = token.All(char.IsLetter);

            // All-lowercase words ("planning", "titration", "up") and 1-2 letter
            // Title-case words ("At", "To", "In") are prose, not compound names.
            // Tokens carrying a digit or hyphen (BPC-157, B12) bypass this — they
            // are unambiguously compound-shaped.
            if (isAlphabetic &&
                (token.Equals(token.ToLowerInvariant(), StringComparison.Ordinal) || token.Length <= 2))
            {
                return false;
            }
        }

        return true;
    }

    private static string ResolveCanonicalName(string value, IReadOnlyDictionary<string, KnowledgeEntry> aliases)
    {
        var key = NormalizeLookupKey(value);
        return aliases.TryGetValue(key, out var entry) ? entry.CanonicalName : value.Trim();
    }

    private static IReadOnlyDictionary<string, KnowledgeEntry> BuildAliasMap(IEnumerable<KnowledgeEntry> knowledge)
    {
        var map = new Dictionary<string, KnowledgeEntry>(StringComparer.OrdinalIgnoreCase);

        foreach (var entry in knowledge)
        {
            AddAlias(map, entry.CanonicalName, entry);
            foreach (var alias in entry.Aliases)
            {
                AddAlias(map, alias, entry);
            }
        }

        AddSyntheticAlias(map, "BPC157", "BPC-157");
        AddSyntheticAlias(map, "BPC 157", "BPC-157");
        AddSyntheticAlias(map, "CJC-1295 no dac", "CJC-1295 no DAC");
        AddSyntheticAlias(map, "CJC 1295 no dac", "CJC-1295 no DAC");

        return map;

        void AddSyntheticAlias(Dictionary<string, KnowledgeEntry> aliases, string alias, string canonical)
        {
            var existing = aliases.Values.FirstOrDefault(entry =>
                string.Equals(entry.CanonicalName, canonical, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                AddAlias(aliases, alias, existing);
            }
        }
    }

    private static void AddAlias(IDictionary<string, KnowledgeEntry> map, string alias, KnowledgeEntry entry)
    {
        var key = NormalizeLookupKey(alias);
        if (!string.IsNullOrWhiteSpace(key))
        {
            map[key] = entry;
        }
    }

    private static bool ContainsAlias(string segment, string normalizedAlias)
    {
        var normalizedSegment = NormalizeLookupKey(segment);
        return normalizedSegment.Contains(normalizedAlias, StringComparison.OrdinalIgnoreCase);
    }

    private static IEnumerable<string> SplitIntoSegments(string inputText)
    {
        // Also split on the pipe used to join table cells (DocxProtocolExtractor /
        // SpreadsheetProtocolExtractor emit rows as "cell | cell | cell"). Splitting
        // per cell keeps a compound name from being fused with a dose that belongs
        // to a different column, and isolates prose/structure cells so the
        // recognition gate can drop them.
        return Regex.Split(inputText, @"(?:\r?\n|;|\s\+\s|\s*\|\s*)")
            .Select(segment => segment.Trim())
            .Where(segment => segment.Length > 0);
    }

    private static string NormalizeLookupKey(string value)
    {
        var normalized = value.Replace("μ", "u", StringComparison.OrdinalIgnoreCase);
        normalized = Regex.Replace(normalized, @"[^a-zA-Z0-9\+]+", " ");
        return Regex.Replace(normalized, @"\s+", " ").Trim().ToLowerInvariant();
    }

    private static string ExtractMatch(Regex pattern, string value)
    {
        var match = pattern.Match(value);
        return match.Success ? match.Groups[1].Value.Trim() : string.Empty;
    }

    private static string NormalizeUnit(string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "microgram" or "micrograms" or "ug" or "μg" or "mcg" => "mcg",
            "milligram" or "milligrams" or "mg" => "mg",
            _ => unit.ToLowerInvariant()
        };
    }

    private static string NormalizeFrequency(string frequency)
    {
        if (string.IsNullOrWhiteSpace(frequency))
        {
            return string.Empty;
        }

        return frequency.ToLowerInvariant() switch
        {
            "eod" => "every other day",
            "2x weekly" => "twice weekly",
            "3x weekly" => "three times weekly",
            _ => frequency
        };
    }
}

public sealed record ProtocolParseResult(
    List<ProtocolEntryResponse> Entries,
    IReadOnlyDictionary<string, KnowledgeEntry> KnowledgeByCompound,
    List<ProtocolBlendExpansionResponse> BlendExpansions);

public interface IProtocolParser
{
    Task<ProtocolParseResult> ParseAsync(string inputText, CancellationToken cancellationToken = default);
}
