namespace BioStack.Infrastructure.Keon;

/// <summary>
/// Canonical formatters for receipt subject and evidence references (Lane G).
///
/// Centralizing the <c>kind:{id}</c> formats keeps <see cref="ReceiptRequest.EvidenceRefs"/>
/// stable and testable across receipt families. Where a structured entity does not yet exist,
/// callers fall back to the most stable available identifier (e.g. a compound slug) using the
/// matching helper here, and that fallback is documented at the call site.
/// </summary>
public static class ReceiptRefs
{
    // ── Knowledge graph / sources ──────────────────────────────────────────────
    public static string KnowledgeEntry(string id) => $"knowledge-entry:{id}";
    public static string Source(string id) => $"source:{id}";
    public static string SourceIntake(string id) => $"source-intake:{id}";
    public static string StagedArtifact(string id) => $"staged-artifact:{id}";
    public static string TranscriptCandidate(string artifactId) => $"transcript-candidate:{artifactId}";
    public static string EvidenceClaim(string id) => $"evidence-claim:{id}";
    public static string RelationshipEdge(string id) => $"relationship-edge:{id}";
    public static string CompoundGraph(string hash) => $"compound-graph:{hash}";

    /// <summary>
    /// Reference to a compound by slug. Used where a structured knowledge-entry id is not
    /// available on the surface (e.g. a client-supplied stack-review payload), the slug being
    /// the most stable identifier present.
    /// </summary>
    public static string Compound(string slug) => $"compound:{slug}";

    // ── Protocol / profile ─────────────────────────────────────────────────────
    public static string Protocol(Guid id) => $"protocol:{id}";
    public static string ProtocolRun(Guid id) => $"protocol-run:{id}";
    public static string Profile(Guid id) => $"profile:{id}";
    public static string CheckIn(Guid id) => $"check-in:{id}";

    // ── Policy / safety / deliberation ─────────────────────────────────────────
    public static string Policy(string policyIdOrHash) => $"policy:{policyIdOrHash}";
    public static string SafetyGate(string decisionId) => $"safety-gate:{decisionId}";
    public static string CollectiveDeliberation(string id) => $"collective-deliberation:{id}";
}
