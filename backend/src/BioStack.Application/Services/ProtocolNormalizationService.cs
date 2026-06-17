namespace BioStack.Application.Services;

using BioStack.Contracts.Responses;
using BioStack.Domain.Entities;

public sealed class ProtocolNormalizationService : IProtocolNormalizationService
{
    public string NormalizeExtractedText(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return string.Empty;
        }

        var normalized = input
            .Replace("\u00A0", " ", StringComparison.Ordinal)
            .Replace('\t', ' ')
            .Replace('•', '-')
            .Replace('–', '-')
            .Replace('—', '-')
            .Replace("μg", "mcg", StringComparison.OrdinalIgnoreCase);

        // Normalize the "ug" microgram unit to "mcg" ONLY when it follows a number
        // (e.g. "500ug" -> "500mcg"). A blind substring replace corrupts every word
        // containing "ug" — drug -> "drmcg", through -> "thromcgh" — which then
        // poisons compound matching, the extracted-text preview, and citations.
        normalized = System.Text.RegularExpressions.Regex.Replace(
            normalized, @"(?<=\d)\s*ug\b", "mcg", System.Text.RegularExpressions.RegexOptions.IgnoreCase);

        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"[ ]{2,}", " ");
        normalized = System.Text.RegularExpressions.Regex.Replace(normalized, @"(\r?\n){3,}", Environment.NewLine + Environment.NewLine);

        var lines = normalized
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(line => line.Trim())
            .Where(line => !string.IsNullOrWhiteSpace(line))
            .ToList();

        return string.Join(Environment.NewLine, lines);
    }

    public NormalizedProtocol Normalize(ProtocolParseResult parseResult)
    {
        var compounds = parseResult.Entries
            .Select(entry =>
            {
                var isKnown = parseResult.KnowledgeByCompound.TryGetValue(entry.CompoundName, out var knowledge);
                return new NormalizedProtocolCompound(
                    isKnown ? knowledge!.CanonicalName : entry.CompoundName,
                    NormalizeDose(entry.Dose, entry.Unit),
                    entry.Unit,
                    NormalizeFrequency(entry.Frequency),
                    NormalizeDuration(entry.Duration),
                    isKnown ? string.Join(" ", knowledge!.Benefits.Concat(knowledge.Pathways)) : string.Empty,
                    isKnown);
            })
            .OrderBy(compound => compound.CanonicalName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(compound => compound.DoseMcg)
            .ToList();

        return new NormalizedProtocol(compounds, parseResult.BlendExpansions);
    }

    public AnalysisContext BuildAnalysisContext(
        string? goal,
        IEnumerable<string>? secondaryGoals,
        string? sex,
        int? age,
        double? weight,
        IEnumerable<string>? existingStackContext)
    {
        var normalizedSecondaryGoals = secondaryGoals?
            .Select(item => item?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(item => !string.IsNullOrEmpty(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        return new AnalysisContext(
            goal?.Trim() ?? string.Empty,
            normalizedSecondaryGoals,
            sex?.Trim() ?? string.Empty,
            ToAgeBand(age),
            ToWeightBand(weight),
            existingStackContext?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).OrderBy(item => item, StringComparer.OrdinalIgnoreCase).ToList()
                ?? new List<string>(),
            ProtocolFingerprintService.ParserVersion,
            ProtocolFingerprintService.KnowledgeVersion,
            ProtocolFingerprintService.ScoringVersion);
    }

    public OptimizationContext BuildOptimizationContext(
        string? goal,
        IEnumerable<string>? secondaryGoals,
        int? maxCompounds,
        IEnumerable<string>? requiredCompoundIds,
        IEnumerable<string>? excludedCompoundIds,
        IEnumerable<string>? existingProfileContext,
        string optimizationMode = "all")
    {
        var normalizedSecondaryGoals = secondaryGoals?
            .Select(item => item?.Trim().ToLowerInvariant() ?? string.Empty)
            .Where(item => !string.IsNullOrEmpty(item))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(item => item, StringComparer.OrdinalIgnoreCase)
            .ToList() ?? new List<string>();

        return new OptimizationContext(
            goal?.Trim() ?? string.Empty,
            normalizedSecondaryGoals,
            maxCompounds ?? 5,
            requiredCompoundIds?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>(),
            excludedCompoundIds?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>(),
            existingProfileContext?.Where(item => !string.IsNullOrWhiteSpace(item)).Distinct(StringComparer.OrdinalIgnoreCase).ToList() ?? new List<string>(),
            optimizationMode,
            ProtocolFingerprintService.ScoringVersion,
            ProtocolFingerprintService.KnowledgeVersion,
            ProtocolFingerprintService.CounterfactualVersion);
    }

    private static double NormalizeDose(double dose, string unit)
    {
        return unit.ToLowerInvariant() switch
        {
            "mg" => dose * 1000d,
            "mcg" => dose,
            _ => dose
        };
    }

    private static string NormalizeFrequency(string frequency)
    {
        var normalized = (frequency ?? string.Empty).Trim().ToLowerInvariant();
        return normalized switch
        {
            "every day" => "daily",
            "once daily" => "daily",
            "eod" => "every-other-day",
            "3x weekly" => "three-times-weekly",
            "2x weekly" => "twice-weekly",
            _ => normalized.Replace(' ', '-')
        };
    }

    private static string NormalizeDuration(string duration)
    {
        return (duration ?? string.Empty).Trim().ToLowerInvariant().Replace(' ', '-');
    }

    private static string ToAgeBand(int? age)
    {
        if (age is null)
        {
            return "unknown";
        }

        return age.Value switch
        {
            < 30 => "18-29",
            < 40 => "30-39",
            < 50 => "40-49",
            < 60 => "50-59",
            _ => "60-plus"
        };
    }

    private static string ToWeightBand(double? weight)
    {
        if (weight is null)
        {
            return "unknown";
        }

        return weight.Value switch
        {
            < 140 => "under-140",
            < 180 => "140-179",
            < 220 => "180-219",
            _ => "220-plus"
        };
    }
}

public interface IProtocolNormalizationService
{
    string NormalizeExtractedText(string input);
    NormalizedProtocol Normalize(ProtocolParseResult parseResult);
    AnalysisContext BuildAnalysisContext(string? goal, IEnumerable<string>? secondaryGoals, string? sex, int? age, double? weight, IEnumerable<string>? existingStackContext);
    OptimizationContext BuildOptimizationContext(string? goal, IEnumerable<string>? secondaryGoals, int? maxCompounds, IEnumerable<string>? requiredCompoundIds, IEnumerable<string>? excludedCompoundIds, IEnumerable<string>? existingProfileContext, string optimizationMode = "all");
}
