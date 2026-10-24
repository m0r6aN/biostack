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
///     Absence of ClassA support → the field is KEPT and labeled "unofficial source"
///     (owner ruling 2026-10-24: remove do-not-display gates). Nothing is blanked.
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
    IReadOnlyList<string> ReviewReasons)
{
    /// <summary>Fields kept from non-official sources; the page labels them, never hides them.</summary>
    public IReadOnlyList<string> UnofficialFields { get; init; } = Array.Empty<string>();
}

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

        // ClassB: nothing is blanked. Owner ruling 2026-10-24 (round 2, ruling 9): BioStack
        // does not hide gathered data. Regulatory, safety, dosing and blend statements that
        // come only from non-official sources are kept and recorded as "unofficial" so the
        // page labels them instead of dropping them. ClassA still wins on conflict upstream
        // (FieldAuthorityPolicy); this gate only marks authority.
        var unofficial = new List<string>();
        if (!string.IsNullOrWhiteSpace(record.Regulatory.RegulatoryStatus)) unofficial.Add("regulatoryStatus");
        if (record.Regulatory.ApprovedIndications.Count > 0) unofficial.Add("approvedIndications");
        if (record.Regulatory.LabelStatusByUseCase.Count > 0) unofficial.Add("labelStatusByUseCase");
        if (record.Safety.Contraindications.Count > 0) unofficial.Add("contraindications");
        if (record.Safety.Warnings.Count > 0) unofficial.Add("warnings");
        if (record.Safety.Monitoring.Count > 0) unofficial.Add("monitoring");
        if (record.DosingGuidance.Any(d => d.ProductSpecific)) unofficial.Add("labelDosing");
        if (record.Compatibility.CompatibleBlends.Count > 0
            || record.Compatibility.BlendingRules.Any(br => br.Status.Equals("allowed", StringComparison.OrdinalIgnoreCase)))
            unofficial.Add("compatibility");

        var reviews = new List<string>
        {
            "ClassB-only record: regulatory and safety statements come from non-official sources and are shown with that label.",
        };

        record.Ops.NeedsReview   = true;
        record.Ops.ReviewReasons = record.Ops.ReviewReasons.Concat(reviews).Distinct().ToList();
        record.Ops.QualityFlags  = record.Ops.QualityFlags.Concat(new[] { "classB-only" }).Distinct().ToList();

        return new TrustGateResult(
            Record:         record,
            RecordClass:    cls,
            StrippedFields: Array.Empty<string>(),
            ReviewReasons:  reviews)
        {
            UnofficialFields = unofficial,
        };
    }
}
