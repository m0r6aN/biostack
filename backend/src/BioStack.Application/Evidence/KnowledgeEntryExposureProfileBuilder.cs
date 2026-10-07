namespace BioStack.Application.Evidence;

using System.Globalization;
using System.Text.RegularExpressions;
using BioStack.Domain.Entities;
using BioStack.Domain.Evidence;

/// <summary>
/// Builds a reviewed-evidence profile for Class B comparison from KnowledgeEntry fields.
/// Best-effort parsing of free-text dosing strings; fails closed to "no regimen" when unparsable.
/// </summary>
public static class KnowledgeEntryExposureProfileBuilder
{
    private static readonly Regex AmountUnitRegex = new(
        @"(?<min>\d+(?:\.\d+)?)\s*(?:-|–|to)\s*(?<max>\d+(?:\.\d+)?)\s*(?<unit>mcg|µg|ug|mg|g)\b|(?<amount>\d+(?:\.\d+)?)\s*(?<unit2>mcg|µg|ug|mg|g)\b",
        RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public static ReviewedEvidenceProfile? TryBuild(KnowledgeEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var regimens = new List<PublishedExposureRegimen>();

        // Prefer structured tiered dosing (beginner start as initiation context).
        if (entry.TieredDosing?.Beginner is { } beginner)
        {
            if (TryParseAmount(beginner.StartDose, out var startMin, out var startMax, out var startUnit))
            {
                decimal? maxStudied = null;
                if (TryParseAmount(beginner.MaxDose, out var maxMin, out var maxMax, out _)
                    || TryParseAmount(entry.MaxReportedDose, out maxMin, out maxMax, out _))
                {
                    maxStudied = maxMax ?? maxMin;
                }

                regimens.Add(new PublishedExposureRegimen(
                    StudyArm: "knowledge_tiered_beginner",
                    Substance: entry.CanonicalName,
                    InitiationAmountMin: startMin,
                    InitiationAmountMax: startMax ?? startMin,
                    MaintenanceAmountMin: null,
                    MaintenanceAmountMax: null,
                    MaximumStudiedAmount: maxStudied,
                    Unit: startUnit,
                    Route: null,
                    Frequency: string.IsNullOrWhiteSpace(entry.Frequency) ? null : entry.Frequency,
                    SourceCitation: FirstSource(entry) ?? $"knowledge:{entry.CanonicalName}",
                    SourceLocation: "tieredDosing.beginner.startDose",
                    EvidenceClass: "reviewed_knowledge_entry",
                    PopulationSummary: null));
            }
        }

        if (regimens.Count == 0
            && TryParseAmount(entry.StandardDosageRange, out var stdMin, out var stdMax, out var stdUnit))
        {
            decimal? maxStudied = null;
            if (TryParseAmount(entry.MaxReportedDose, out var maxMin, out var maxMax, out _))
            {
                maxStudied = maxMax ?? maxMin;
            }

            regimens.Add(new PublishedExposureRegimen(
                StudyArm: "knowledge_standard_range",
                Substance: entry.CanonicalName,
                InitiationAmountMin: stdMin,
                InitiationAmountMax: stdMax ?? stdMin,
                MaintenanceAmountMin: stdMin,
                MaintenanceAmountMax: stdMax ?? stdMin,
                MaximumStudiedAmount: maxStudied,
                Unit: stdUnit,
                Route: null,
                Frequency: string.IsNullOrWhiteSpace(entry.Frequency) ? null : entry.Frequency,
                SourceCitation: FirstSource(entry) ?? $"knowledge:{entry.CanonicalName}",
                SourceLocation: "standardDosageRange",
                EvidenceClass: "reviewed_knowledge_entry",
                PopulationSummary: null));
        }

        if (regimens.Count == 0
            && TryParseAmount(entry.RecommendedDosage, out var recMin, out var recMax, out var recUnit))
        {
            regimens.Add(new PublishedExposureRegimen(
                StudyArm: "knowledge_recommended",
                Substance: entry.CanonicalName,
                InitiationAmountMin: recMin,
                InitiationAmountMax: recMax ?? recMin,
                MaintenanceAmountMin: null,
                MaintenanceAmountMax: null,
                MaximumStudiedAmount: null,
                Unit: recUnit,
                Route: null,
                Frequency: string.IsNullOrWhiteSpace(entry.Frequency) ? null : entry.Frequency,
                SourceCitation: FirstSource(entry) ?? $"knowledge:{entry.CanonicalName}",
                SourceLocation: "recommendedDosage",
                EvidenceClass: "reviewed_knowledge_entry",
                PopulationSummary: null));
        }

        if (regimens.Count == 0)
        {
            return null;
        }

        return new ReviewedEvidenceProfile(
            SubjectName: entry.CanonicalName,
            Regimens: regimens,
            HasHumanEvidence: true,
            EvidenceLimitedToAnimals: false,
            EvidenceLimitedToCaseReports: false,
            ConflictingHumanEvidence: false);
    }

    public static bool TryParseAmount(
        string? text,
        out decimal min,
        out decimal? max,
        out string unit)
    {
        min = 0;
        max = null;
        unit = "mg";
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var match = AmountUnitRegex.Match(text);
        if (!match.Success)
        {
            return false;
        }

        if (match.Groups["min"].Success && match.Groups["max"].Success)
        {
            if (!decimal.TryParse(match.Groups["min"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out min))
            {
                return false;
            }

            if (!decimal.TryParse(match.Groups["max"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out var maxValue))
            {
                return false;
            }

            max = maxValue;
            unit = NormalizeUnit(match.Groups["unit"].Value);
            return true;
        }

        if (match.Groups["amount"].Success)
        {
            if (!decimal.TryParse(match.Groups["amount"].Value, NumberStyles.Number, CultureInfo.InvariantCulture, out min))
            {
                return false;
            }

            max = min;
            unit = NormalizeUnit(match.Groups["unit2"].Value);
            return true;
        }

        return false;
    }

    private static string NormalizeUnit(string unit)
    {
        var u = unit.Trim().ToLowerInvariant();
        return u is "µg" or "ug" ? "mcg" : u;
    }

    private static string? FirstSource(KnowledgeEntry entry)
        => entry.SourceReferences.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
}
