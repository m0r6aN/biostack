namespace BioStack.KnowledgeWorker.Pipeline;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Maps a <see cref="SubstanceRecord"/> (post-normalize, post-trust-gate) to the
/// persisted <see cref="KnowledgeEntry"/> domain entity. Lossy by design: the
/// <c>KnowledgeEntry</c> model predates the full substance-record schema, so we
/// project the richest safe subset and leave deeper fields for a future Phase 2
/// <c>IngestionDbContext</c> schema.
/// </summary>
public interface ISubstanceCanonicalizer
{
    KnowledgeEntry ToKnowledgeEntry(SubstanceRecord record);
}

public sealed class SubstanceCanonicalizer : ISubstanceCanonicalizer
{
    public KnowledgeEntry ToKnowledgeEntry(SubstanceRecord record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var id = record.Identity;

        var aliases = id.Aliases
            .Concat(id.BrandNames)
            .Concat(id.Synonyms)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var entry = new KnowledgeEntry
        {
            CanonicalName     = id.CanonicalName,
            Aliases           = aliases,
            Classification    = MapClassification(id.Classification),
            RegulatoryStatus  = record.Regulatory.RegulatoryStatus ?? string.Empty,
            MechanismSummary  = record.Mechanism.MechanismSummary ?? string.Empty,
            EvidenceTier      = MapEvidenceTier(record.Evidence.OverallTier),
            Pathways          = record.Mechanism.Pathways
                                      .Concat(record.Mechanism.PrimaryMechanisms)
                                      .Concat(record.Mechanism.Targets)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            Benefits          = record.Indications
                                      .Select(i => i.BenefitSummary.Trim())
                                      .Where(s => s.Length > 0)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            PairsWellWith     = record.StackIntelligence.PairsWellWith
                                      .Select(p => p.Target.Trim())
                                      .Where(s => s.Length > 0)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            AvoidWith         = record.StackIntelligence.AvoidWith
                                      .Select(a => a.Target.Trim())
                                      .Where(s => s.Length > 0)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            CompatibleBlends  = record.Compatibility.CompatibleBlends.ToList(),
            VialCompatibility = record.Compatibility.VialCompatibilitySummary ?? string.Empty,
            SourceReferences  = record.Provenance.SourceRecords
                                      .Select(s => s.Url ?? s.Title)
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList()!,
            DrugInteractions  = record.Interactions
                                      .Where(i => string.Equals(i.Type, "drug-drug", StringComparison.OrdinalIgnoreCase))
                                      .Select(FormatInteraction)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            Notes             = string.Join(" | ", record.Ops.ReviewReasons),
        };

        // Primary dosing projection — first productSpecific guidance wins, otherwise first entry.
        var primary = record.DosingGuidance.FirstOrDefault(d => d.ProductSpecific)
                   ?? record.DosingGuidance.FirstOrDefault();
        if (primary is not null)
        {
            entry.RecommendedDosage   = primary.Dose.ScheduleText ?? string.Empty;
            entry.Frequency           = primary.Dose.Frequency    ?? string.Empty;
            entry.PreferredTimeOfDay  = primary.Dose.PreferredTimeOfDay ?? string.Empty;
            entry.StandardDosageRange = primary.Dose.Amount.HasValue
                ? $"{primary.Dose.Amount.Value} {primary.Dose.Unit}"
                : string.Empty;
        }

        return entry;
    }

    private static CompoundCategory MapClassification(string schemaValue)
    {
        return (schemaValue ?? string.Empty).Trim() switch
        {
            "Peptide"          => CompoundCategory.Peptide,
            "Pharmaceutical"   => CompoundCategory.Pharmaceutical,
            "Supplement"       => CompoundCategory.Supplement,
            "Hormone"          => CompoundCategory.Hormone,
            "SARM"             => CompoundCategory.Sarm,
            "SERM"             => CompoundCategory.Serm,
            // The schema carries several classifications we do not yet model
            // (Small Molecule, Biologic, Vitamin, Mineral, Amino Acid, Research Compound,
            // Other). They collapse to Unknown until the domain enum is extended.
            _                  => CompoundCategory.Unknown,
        };
    }

    private static EvidenceTier MapEvidenceTier(string schemaValue)
    {
        return (schemaValue ?? string.Empty).Trim() switch
        {
            "Strong"       => EvidenceTier.Strong,
            "Moderate"     => EvidenceTier.Moderate,
            "Limited"      => EvidenceTier.Limited,
            "Insufficient" => EvidenceTier.Unknown,
            _              => EvidenceTier.Unknown,
        };
    }

    private static string FormatInteraction(Interaction i)
    {
        var effect = string.IsNullOrWhiteSpace(i.Effect) ? "" : $" — {i.Effect}";
        return $"{i.Target}{effect} ({i.Severity})";
    }
}
