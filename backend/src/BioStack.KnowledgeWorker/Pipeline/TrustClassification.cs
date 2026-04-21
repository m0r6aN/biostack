namespace BioStack.KnowledgeWorker.Pipeline;

using BioStack.KnowledgeWorker.Models;

/// <summary>
/// Source-trust classification (signed policy — see engineering note in <see cref="TrustGate"/>):
///
///   * <see cref="TrustClass.A"/> — authoritative: may create or update regulatory,
///     safety-critical, product-specific-dosing, contraindication, warning,
///     monitoring, and formulation/reconstitution fields.
///
///   * <see cref="TrustClass.B"/> — enrichment-only: may populate mechanism summaries,
///     aliases, pathway/context tags, supportive guidance, and stack heuristics.
///     MUST NOT establish canonical regulatory truth, hard-safe compatibility,
///     canonical dosing truth, contraindications, warnings, or monitoring rules.
/// </summary>
public enum TrustClass
{
    A = 1,
    B = 2,
}

/// <summary>
/// Maps raw source-type strings (enum values of <c>provenance.sourceRecords[].sourceType</c>)
/// to a trust class. The record-level class is <see cref="TrustClass.A"/> when any attached
/// source is ClassA; otherwise <see cref="TrustClass.B"/>. An absence of any source is
/// treated as ClassB — enrichment-only — because no authoritative backing exists.
/// </summary>
public static class TrustClassification
{
    // ClassA sources: regulator labels, regulator bulletins, published clinical guidelines,
    // manufacturer prescribing documents. Authoritative on product-specific canonical truth.
    private static readonly HashSet<string> ClassASourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "label",
        "regulator",
        "guideline",
        "manufacturer",
    };

    // ClassB sources: primary research papers, review articles, third-party databases,
    // internal curation notes, or unclassified "other". Enrichment-only by policy.
    private static readonly HashSet<string> ClassBSourceTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "paper",
        "review",
        "database",
        "internal-curation",
        "other",
    };

    public static TrustClass Classify(string sourceType)
    {
        if (string.IsNullOrWhiteSpace(sourceType)) return TrustClass.B;
        if (ClassASourceTypes.Contains(sourceType)) return TrustClass.A;
        if (ClassBSourceTypes.Contains(sourceType)) return TrustClass.B;
        // Unknown / future source types default to ClassB — fail-closed.
        return TrustClass.B;
    }

    /// <summary>
    /// Record-level class. A record with at least one ClassA source is ClassA;
    /// a record with only ClassB sources (or no sources) is ClassB.
    /// </summary>
    public static TrustClass ResolveRecordClass(Provenance provenance)
    {
        if (provenance?.SourceRecords is null || provenance.SourceRecords.Count == 0)
        {
            return TrustClass.B;
        }

        foreach (var src in provenance.SourceRecords)
        {
            if (Classify(src.SourceType) == TrustClass.A)
            {
                return TrustClass.A;
            }
        }
        return TrustClass.B;
    }
}
