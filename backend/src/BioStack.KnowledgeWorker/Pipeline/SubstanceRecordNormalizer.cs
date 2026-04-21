namespace BioStack.KnowledgeWorker.Pipeline;

using System.Globalization;
using System.Text.RegularExpressions;
using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Normalizes a schema-valid record for persistence:
///   * canonicalId / slug → kebab-case of the canonical name when missing.
///   * aliases / brandNames / synonyms → trimmed, de-duplicated (case-insensitive),
///     and validated for intra-record conflicts (same alias claimed twice).
///   * enum-like strings → canonical casing from the schema.
///   * null string collections → empty lists (never null).
/// Normalization is idempotent.
/// </summary>
public interface ISubstanceRecordNormalizer
{
    SubstanceRecord Normalize(SubstanceRecord record);
}

public sealed class SubstanceRecordNormalizer : ISubstanceRecordNormalizer
{
    private static readonly Regex KebabSanitize = new("[^a-z0-9]+", RegexOptions.Compiled);
    private static readonly Regex KebabTrim     = new("^-+|-+$",    RegexOptions.Compiled);

    public SubstanceRecord Normalize(SubstanceRecord record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        NormalizeIdentity(record.Identity);
        NormalizeRegulatory(record.Regulatory);
        NormalizeMechanism(record.Mechanism);
        NormalizeOps(record.Ops);

        return record;
    }

    private static void NormalizeIdentity(Identity id)
    {
        id.CanonicalName = (id.CanonicalName ?? string.Empty).Trim();

        if (string.IsNullOrWhiteSpace(id.CanonicalId))
        {
            id.CanonicalId = Slugify(id.CanonicalName);
        }
        else
        {
            id.CanonicalId = Slugify(id.CanonicalId);
        }

        if (string.IsNullOrWhiteSpace(id.Slug))
        {
            id.Slug = id.CanonicalId;
        }
        else
        {
            id.Slug = Slugify(id.Slug);
        }

        id.Aliases    = DedupeStrings(id.Aliases,    excludeAgainst: new[] { id.CanonicalName });
        id.BrandNames = DedupeStrings(id.BrandNames, excludeAgainst: new[] { id.CanonicalName });
        id.Synonyms   = DedupeStrings(id.Synonyms,   excludeAgainst: new[] { id.CanonicalName });

        // Cross-bucket conflict: an alias that is also a brand name is kept in brandNames,
        // and removed from aliases (brand carries additional commercial semantics).
        var brandSet = new HashSet<string>(id.BrandNames, StringComparer.OrdinalIgnoreCase);
        id.Aliases = id.Aliases.Where(a => !brandSet.Contains(a)).ToList();

        id.ActiveMoieties = DedupeStrings(id.ActiveMoieties);
    }

    private static void NormalizeRegulatory(Regulatory reg)
    {
        reg.RegulatoryStatus    = (reg.RegulatoryStatus ?? string.Empty).Trim();
        reg.Jurisdiction        = (reg.Jurisdiction     ?? string.Empty).Trim();
        reg.ApprovedIndications = DedupeStrings(reg.ApprovedIndications);
        reg.OffLabelNotes       = (reg.OffLabelNotes ?? new()).Select(s => (s ?? string.Empty).Trim())
                                                               .Where(s => s.Length > 0)
                                                               .ToList();

        foreach (var item in reg.LabelStatusByUseCase)
        {
            item.UseCase     = (item.UseCase     ?? string.Empty).Trim();
            item.LabelStatus = LowerEnumLike(item.LabelStatus);
        }
    }

    private static void NormalizeMechanism(Mechanism mech)
    {
        mech.MechanismSummary  = (mech.MechanismSummary ?? string.Empty).Trim();
        mech.PrimaryMechanisms = DedupeStrings(mech.PrimaryMechanisms);
        mech.Pathways          = DedupeStrings(mech.Pathways);
        mech.Targets           = DedupeStrings(mech.Targets);
        mech.EffectTags        = DedupeStrings(mech.EffectTags);
        mech.GoalTags          = DedupeStrings(mech.GoalTags);
    }

    private static void NormalizeOps(Ops ops)
    {
        ops.LastChangeType = LowerEnumLike(ops.LastChangeType);
        ops.Completeness   = LowerEnumLike(ops.Completeness);
        ops.ReviewReasons  = (ops.ReviewReasons ?? new()).Select(s => (s ?? string.Empty).Trim())
                                                         .Where(s => s.Length > 0).ToList();
        ops.QualityFlags   = (ops.QualityFlags  ?? new()).Select(s => (s ?? string.Empty).Trim())
                                                         .Where(s => s.Length > 0).ToList();
    }

    /// <summary>Deterministic lower-kebab-case. Accepts arbitrary input, never throws.</summary>
    public static string Slugify(string input)
    {
        if (string.IsNullOrWhiteSpace(input)) return string.Empty;
        var lower = input.Trim().ToLower(CultureInfo.InvariantCulture);
        var replaced = KebabSanitize.Replace(lower, "-");
        return KebabTrim.Replace(replaced, string.Empty);
    }

    private static string LowerEnumLike(string? value)
        => string.IsNullOrWhiteSpace(value) ? string.Empty : value.Trim().ToLower(CultureInfo.InvariantCulture);

    private static List<string> DedupeStrings(List<string>? source, IEnumerable<string>? excludeAgainst = null)
    {
        if (source is null || source.Count == 0) return new List<string>();

        var exclude = new HashSet<string>(
            (excludeAgainst ?? Array.Empty<string>()).Select(s => (s ?? string.Empty).Trim()),
            StringComparer.OrdinalIgnoreCase);

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var result = new List<string>(source.Count);
        foreach (var raw in source)
        {
            var trimmed = (raw ?? string.Empty).Trim();
            if (trimmed.Length == 0) continue;
            if (exclude.Contains(trimmed)) continue;
            if (!seen.Add(trimmed)) continue;
            result.Add(trimmed);
        }
        return result;
    }
}
