namespace BioStack.KnowledgeWorker.Pipeline;

using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Enforces the ClassA / ClassB field-authority policy on a normalized record
/// before canonicalization. This is the non-reinterpretable enforcement point
/// of BioStack's trust policy:
///
///   * ClassA may create / update canonical regulatory and safety-critical fields:
///     regulatory.*, safety.contraindications, safety.warnings, safety.monitoring,
///     dosingGuidance items marked productSpecific, and formulations' storage /
///     reconstitution blocks.
///
///   * ClassB may enrich: mechanism summaries, aliases, pathway / context tags,
///     supportive guidance, stack heuristics (pairsWellWith / avoidWith narrative,
///     overlap / timing rules).
///
///   * ClassB alone MUST NOT establish: hard-safe compatibility ("allowed"),
///     canonical dosing truth, contraindications, warnings, monitoring, regulatory
///     truth, or "compatible"/"recommended" verdicts on safety-relevant mixing.
///
///   * Conflict: if ClassB conflicts with ClassA, ClassA wins.
///     Absence of ClassA support → the regulated / safety-critical field is
///     kept null / unknown / review-required, never filled optimistically.
///
/// The gate runs AFTER schema validation & normalization and BEFORE canonicalization,
/// because the fields it strips would otherwise be written to <c>KnowledgeEntry</c>
/// by <see cref="SubstanceCanonicalizer"/>.
/// </summary>
public interface ITrustGate
{
    TrustGateResult Apply(SubstanceRecord record);
}

public sealed record TrustGateResult(
    SubstanceRecord Record,
    TrustClass      RecordClass,
    IReadOnlyList<string> StrippedFields,
    IReadOnlyList<string> ReviewReasons);

public sealed class TrustGate : ITrustGate
{
    public TrustGateResult Apply(SubstanceRecord record)
    {
        if (record is null) throw new ArgumentNullException(nameof(record));

        var cls = TrustClassification.ResolveRecordClass(record.Provenance);

        if (cls == TrustClass.A)
        {
            return new TrustGateResult(
                Record:         record,
                RecordClass:    cls,
                StrippedFields: Array.Empty<string>(),
                ReviewReasons:  Array.Empty<string>());
        }

        // ClassB: strip ClassA-only canonical truth. Mark for human review.
        var stripped  = new List<string>();
        var reviews   = new List<string>
        {
            "ClassB source cannot establish ClassA canonical truth; regulated fields blanked.",
        };

        // Regulatory canon — blank.
        if (!string.IsNullOrWhiteSpace(record.Regulatory.RegulatoryStatus))
        {
            record.Regulatory.RegulatoryStatus = string.Empty;
            stripped.Add("regulatory.regulatoryStatus");
        }
        if (record.Regulatory.ApprovedIndications.Count > 0)
        {
            record.Regulatory.ApprovedIndications = new List<string>();
            stripped.Add("regulatory.approvedIndications");
        }
        if (record.Regulatory.LabelStatusByUseCase.Count > 0)
        {
            record.Regulatory.LabelStatusByUseCase = new List<LabelStatusByUseCaseItem>();
            stripped.Add("regulatory.labelStatusByUseCase");
        }

        // Safety canon — blank.
        if (record.Safety.Contraindications.Count > 0)
        {
            record.Safety.Contraindications = new List<SafetyItem>();
            stripped.Add("safety.contraindications");
        }
        if (record.Safety.Warnings.Count > 0)
        {
            record.Safety.Warnings = new List<SafetyItem>();
            stripped.Add("safety.warnings");
        }
        if (record.Safety.Monitoring.Count > 0)
        {
            record.Safety.Monitoring = new List<MonitoringItem>();
            stripped.Add("safety.monitoring");
        }

        // Product-specific dosing truth — blank (enrichment-only dosing cannot establish canon).
        var removedDosing = record.DosingGuidance.RemoveAll(d => d.ProductSpecific);
        if (removedDosing > 0)
        {
            stripped.Add($"dosingGuidance.productSpecific[x{removedDosing}]");
        }

        // Blending "allowed" verdicts — a ClassB source cannot declare a blend hard-safe.
        var removedAllow = record.Compatibility.BlendingRules.RemoveAll(
            br => br.Status.Equals("allowed", StringComparison.OrdinalIgnoreCase));
        if (removedAllow > 0)
        {
            stripped.Add($"compatibility.blendingRules.allowed[x{removedAllow}]");
        }
        if (record.Compatibility.CompatibleBlends.Count > 0)
        {
            record.Compatibility.CompatibleBlends = new List<string>();
            stripped.Add("compatibility.compatibleBlends");
        }

        // Flag for review. Operators must escalate to a ClassA source before republication.
        record.Ops.NeedsReview   = true;
        record.Ops.ReviewReasons = record.Ops.ReviewReasons.Concat(reviews).ToList();
        record.Ops.QualityFlags  = record.Ops.QualityFlags.Concat(new[] { "classB-only" }).Distinct().ToList();

        return new TrustGateResult(
            Record:         record,
            RecordClass:    cls,
            StrippedFields: stripped,
            ReviewReasons:  reviews);
    }
}
