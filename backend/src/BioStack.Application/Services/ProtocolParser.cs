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
        // not a compound. Skip the segment so it never surfaces as an entry.
        if (CyclePattern.IsMatch(segment))
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
                return new ProtocolEntryResponse(
                    resolved,
                    dose,
                    unit,
                    NormalizeFrequency(frequency),
                    duration);
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

        var compoundName = ResolveCompoundName(cleaned, singleDoseMatch, aliases);

        return new[]
        {
            new ProtocolEntryResponse(
                compoundName,
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

        return new ProtocolEntryResponse(canonicalName, bestDoseEntry.Dose, unit, frequency, duration);
    }

    private static string ResolveCompoundName(string segment, Match doseMatch, IReadOnlyDictionary<string, KnowledgeEntry> aliases)
    {
        var directMatch = aliases
            .OrderByDescending(alias => alias.Key.Length)
            .FirstOrDefault(alias => ContainsAlias(segment, alias.Key));

        if (directMatch.Value is not null)
        {
            return directMatch.Value.CanonicalName;
        }

        var nameSlice = doseMatch.Success ? segment[..doseMatch.Index] : segment;
        nameSlice = FrequencyPattern.Replace(nameSlice, string.Empty);
        nameSlice = DurationPattern.Replace(nameSlice, string.Empty);
        nameSlice = Regex.Replace(nameSlice, @"\b(take|inject|dose|with|for|at|on|and)\b", " ", RegexOptions.IgnoreCase);
        nameSlice = Regex.Replace(nameSlice, @"[,:;]+", " ");
        nameSlice = Regex.Replace(nameSlice, @"\s+", " ").Trim();

        return nameSlice;
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
        return Regex.Split(inputText, @"(?:\r?\n|;|\s\+\s)")
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
