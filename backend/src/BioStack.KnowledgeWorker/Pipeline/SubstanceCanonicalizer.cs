namespace BioStack.KnowledgeWorker.Pipeline;

using BioStack.Domain.Entities;
using BioStack.Domain.Enums;
using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Maps a <see cref="SubstanceRecord"/> (post-normalize, post-trust-gate) to the
/// persisted <see cref="KnowledgeEntry"/> domain entity. Not lossy: the flat columns
/// keep their existing meaning for protocol/interaction services, and everything else
/// the record carries (safety, regulatory detail, studied uses including not-indicated
/// ones, claims, gaps, controversies, interactions, dose reports, titled sources) is
/// carried in <see cref="KnowledgeEntry.Details"/> so the public page can show it.
/// </summary>
public interface ISubstanceCanonicalizer
{
    KnowledgeEntry ToKnowledgeEntry(SubstanceRecord record);
}

public sealed class SubstanceCanonicalizer : ISubstanceCanonicalizer
{
    private readonly IClaimSignOffSource _signOffs;

    public SubstanceCanonicalizer() : this(EmptyClaimSignOffSource.Instance) { }

    public SubstanceCanonicalizer(IClaimSignOffSource signOffs) => _signOffs = signOffs;

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
            // Pathways are short tags used by the overlap checker. Prose sentences (often a copy
            // of the mechanism summary in primaryMechanisms) are not tags and are left out
            // (growth spec O7: Ibutamoren's pathways held its mechanism sentence).
            Pathways          = record.Mechanism.Pathways
                                      .Concat(record.Mechanism.PrimaryMechanisms)
                                      .Concat(record.Mechanism.Targets)
                                      .Select(s => s.Trim())
                                      .Where(IsPathwayTag)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            // An indication explicitly marked "not-indicated" records no claimed
            // benefit — its evidence-scoped summary must not surface as a benefit.
            Benefits          = record.Indications
                                      .Where(i => !string.Equals(i.LabelStatus, "not-indicated", StringComparison.OrdinalIgnoreCase))
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
                                      .Where(s => PublicSourcePolicy.IsPublicCitable(s.Url, s.Publisher))
                                      .Select(s => s.Url ?? s.Title)
                                      .Where(s => !string.IsNullOrWhiteSpace(s))
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList()!,
            DrugInteractions  = record.Interactions
                                      .Where(i => string.Equals(i.Type, "drug-drug", StringComparison.OrdinalIgnoreCase))
                                      .Select(FormatInteraction)
                                      .Distinct(StringComparer.OrdinalIgnoreCase)
                                      .ToList(),
            // Notes is display text. Pipeline review reasons are internal and live in
            // Details.ReviewReasons; they are never printed raw on the page.
            Notes             = string.Join(" ", record.Provenance.CurationNotes
                                      .Select(n => n.Trim())
                                      .Where(n => n.Length > 0)),
            Details           = BuildDetails(record, _signOffs.Current),
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
            "SARM"               => CompoundCategory.Sarm,
            "SERM"               => CompoundCategory.Serm,
            "Amino Acid"         => CompoundCategory.AminoAcid,
            "Research Compound"  => CompoundCategory.ResearchCompound,
            "Small Molecule"     => CompoundCategory.SmallMolecule,
            "Biologic"           => CompoundCategory.Biologic,
            "Vitamin"            => CompoundCategory.Vitamin,
            "Mineral"            => CompoundCategory.Mineral,
            "Gene Therapy"       => CompoundCategory.GeneTherapy,
            "Epigenetic Therapy" => CompoundCategory.EpigeneticTherapy,
            "Cell Therapy"       => CompoundCategory.CellTherapy,
            "Oligonucleotide"    => CompoundCategory.Oligonucleotide,
            "Other"              => CompoundCategory.Other,
            _                    => CompoundCategory.Unknown,
        };
    }

    // 7-tier methodology labels ("Early Evidence"). "Not graded" and anything else map to Unknown.
    private static EvidenceTier MapEvidenceTier(string schemaValue) => EvidenceTiers.Parse(schemaValue);

    private static KnowledgeEntryDetails BuildDetails(SubstanceRecord r, ClaimSignOffIndex signOffs)
    {
        static List<string> Clean(IEnumerable<string?> values) => values
            .Select(v => (v ?? string.Empty).Trim())
            .Where(v => v.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        static KnowledgeSafetyItem Item(SafetyItem i) => new()
        {
            Item = i.Item, Severity = i.Severity, Notes = i.Notes ?? string.Empty, Sources = i.Sources.ToList(),
        };

        return new KnowledgeEntryDetails
        {
            CompoundFamily       = r.Identity.CompoundFamily ?? string.Empty,
            RequiresPrescription = r.Regulatory.RequiresPrescription,
            Jurisdiction         = r.Regulatory.Jurisdiction ?? string.Empty,
            ApprovedIndications  = Clean(r.Regulatory.ApprovedIndications),
            LabelStatusByUseCase = r.Regulatory.LabelStatusByUseCase
                .Select(l => new KnowledgeLabelStatus { UseCase = l.UseCase, LabelStatus = l.LabelStatus })
                .ToList(),
            OffLabelNotes        = Clean(r.Regulatory.OffLabelNotes),
            StudiedUses          = r.Indications.Select(i => new KnowledgeStudiedUse
            {
                UseCase = i.UseCase, LabelStatus = i.LabelStatus, Population = i.Population,
                Summary = i.BenefitSummary, EvidenceTier = i.EvidenceTier, Sources = i.Sources.ToList(),
            }).ToList(),
            Safety = new KnowledgeSafety
            {
                Contraindications     = r.Safety.Contraindications.Select(Item).ToList(),
                Warnings              = r.Safety.Warnings.Select(Item).ToList(),
                Precautions           = r.Safety.Precautions.Select(p => new KnowledgeSafetyItem
                {
                    Item = p.Item, Notes = p.Notes ?? string.Empty, Sources = p.Sources.ToList(),
                }).ToList(),
                AdverseEffectsCommon  = Clean(r.Safety.AdverseEffects.Common),
                AdverseEffectsSerious = Clean(r.Safety.AdverseEffects.Serious),
                Monitoring            = r.Safety.Monitoring.Select(m => new KnowledgeMonitoringItem
                {
                    Parameter = m.Parameter, Reason = m.Reason, Frequency = m.Frequency ?? string.Empty,
                }).ToList(),
            },
            DoseReports = r.DoseReports.Select(d => new KnowledgeDoseReport
            {
                Side = string.Equals(d.Side, "reported", StringComparison.OrdinalIgnoreCase) ? "reported" : "study",
                Context = d.Context ?? string.Empty, Amount = d.Amount, Unit = d.Unit, Frequency = d.Frequency,
                Duration = d.Duration ?? string.Empty, Route = d.Route ?? string.Empty,
                Population = d.Population ?? string.Empty, Outcome = d.Outcome ?? string.Empty,
                SourceType = d.SourceType, SourceTitle = d.SourceTitle, SourceUrl = d.SourceUrl,
                RetrievedAt = d.RetrievedAt, Grade = d.Grade, Notes = d.Notes ?? string.Empty,
            }).ToList(),
            LabelDosing = r.DosingGuidance.Select(g => new KnowledgeLabelDose
            {
                Context = g.Context.UseCase, Population = g.Context.Population, Route = g.Context.Route,
                Amount = g.Dose.Amount, Unit = g.Dose.Unit, Frequency = g.Dose.Frequency,
                ScheduleText = g.Dose.ScheduleText, ProductSpecific = g.ProductSpecific,
                Notes = Clean(g.Notes), Sources = g.Sources.ToList(),
            }).ToList(),
            Claims = r.Evidence.ClaimSpecificEvidence.Select(c => new KnowledgeClaim
            {
                Claim = c.Claim, Tier = c.Tier, Confidence = c.Confidence, Sources = c.Sources.ToList(),
            }).ToList(),
            PublicClaims  = PublicClaimProjector.Project(r, signOffs),
            EvidenceGaps  = Clean(r.Evidence.EvidenceGaps),
            Controversies = Clean(r.Evidence.Controversies),
            Interactions  = r.Interactions.Select(i => new KnowledgeInteraction
            {
                Type = i.Type, Target = i.Target, Severity = i.Severity, Mechanism = i.Mechanism ?? string.Empty,
                Effect = i.Effect ?? string.Empty, Note = i.RecommendedAction, Sources = i.Sources.ToList(),
            }).ToList(),
            Formulations = r.Formulations.Select(f => new KnowledgeFormulation
            {
                ProductName = f.ProductName, Route = f.Route, DosageForm = f.DosageForm, Strength = f.Strength ?? string.Empty,
            }).ToList(),
            Sources = r.Provenance.SourceRecords
                .Where(s => PublicSourcePolicy.IsPublicCitable(s.Url, s.Publisher))
                .Select(s => new KnowledgeSourceRecord
            {
                Title = s.Title, Url = s.Url ?? string.Empty, Publisher = s.Publisher ?? string.Empty,
                SourceType = s.SourceType, PublishedAt = s.PublishedAt, LastCheckedAt = s.LastCheckedAt,
            }).ToList(),
            ReviewStatus   = r.Provenance.ReviewStatus ?? string.Empty,
            LastReviewedAt = r.Provenance.LastReviewedAt,
            ReviewReasons  = Clean(r.Ops.ReviewReasons),
        };
    }

    private static string FormatInteraction(Interaction i)
    {
        var effect = string.IsNullOrWhiteSpace(i.Effect) ? "" : $" — {i.Effect}";
        return $"{i.Target}{effect} ({i.Severity})";
    }
}
