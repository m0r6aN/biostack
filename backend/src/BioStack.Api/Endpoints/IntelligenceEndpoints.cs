namespace BioStack.Api.Endpoints;

using BioStack.Application.Governance;
using BioStack.Application.Services;
using BioStack.Application.Services.Intelligence;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Knowledge;

/// <summary>
/// Graph-backed intelligence read surfaces (Lane C). Serves reviewed compound relationships and
/// compatibility from the materialized graph, and emits an <c>intelligence.graph-artifact.used</c>
/// receipt whenever a graph-backed artifact informs a user-facing response.
///
/// Lane H: every response is routed through <see cref="IUserFacingIntelligenceGate"/> before it is
/// returned — constraining doctrine-violating text, applying warning-first framing for high-risk
/// categories, disclosing fallback as evidence-limited, and recording a safety receipt when a
/// warning/constraint/refusal occurs (the safety receipt preserves the graph/compound/source refs).
/// </summary>
public static class IntelligenceEndpoints
{
    public static void MapIntelligenceEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/v1/intelligence")
            .WithTags("Intelligence")
            .RequireAuthorization();

        group.MapGet("/compounds/{compound}/relationships", GetCompoundRelationships)
            .WithName("GetCompoundRelationships");

        group.MapGet("/compatibility", GetCompatibility)
            .WithName("GetCompoundCompatibility");
    }

    private static async Task<IResult> GetCompoundRelationships(
        string compound,
        IGraphIntelligenceService graphIntelligence,
        IUserFacingIntelligenceGate gate,
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
        CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(compound))
        {
            return Results.BadRequest("A compound name is required.");
        }

        var response = await graphIntelligence.GetRelationshipsForCompoundAsync(compound, ct);

        var graphBacked = response.Source == IntelligenceSource.Graph && response.GraphArtifactHash is not null;
        var subject = $"intelligence:compound:{CompoundSlug.From(compound)}/relationships";
        var compounds = new[] { compound }
            .Concat(response.Relationships.SelectMany(r => new[] { r.SubjectCompound, r.ObjectCompound }));
        var evidenceRefs = BuildEvidenceRefs(
            graphBacked ? response.GraphArtifactHash : null,
            compounds,
            response.Relationships.SelectMany(r => r.SourceRefs));

        if (graphBacked)
        {
            await IssueGraphArtifactReceiptAsync(
                receipts, currentUser, response.GraphArtifactHash!, subject, evidenceRefs, ct);
        }

        var (safeRelationships, decision) = await GateRelationshipsAsync(
            gate, currentUser, "intelligence.compound-relationships",
            subject, response.Source, response.GraphArtifactHash,
            compounds, response.Relationships, evidenceRefs, ct);

        return Results.Ok(response with
        {
            Relationships = safeRelationships,
            SafetyStatus = decision.SafetyStatus,
            Warnings = decision.Warnings,
            PolicyRefs = decision.PolicyRefs,
            SafetyReceiptId = decision.SafetyReceiptUri,
        });
    }

    private static async Task<IResult> GetCompatibility(
        HttpContext httpContext,
        IGraphIntelligenceService graphIntelligence,
        IUserFacingIntelligenceGate gate,
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
        CancellationToken ct)
    {
        var compounds = httpContext.Request.Query["compounds"]
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => c!.Trim())
            .ToList();

        if (compounds.Count < 2)
        {
            return Results.BadRequest("Provide at least two 'compounds' query values.");
        }

        var response = await graphIntelligence.GetCompatibilityAsync(compounds, ct);

        var graphBacked = response.Source == IntelligenceSource.Graph && response.GraphArtifactHash is not null;
        var subject = $"intelligence:compatibility:{string.Join("+", response.Compounds.Select(CompoundSlug.From).OrderBy(s => s, StringComparer.Ordinal))}";
        var evidenceRefs = BuildEvidenceRefs(
            graphBacked ? response.GraphArtifactHash : null,
            response.Compounds,
            response.Relationships.SelectMany(r => r.SourceRefs));

        if (graphBacked)
        {
            await IssueGraphArtifactReceiptAsync(
                receipts, currentUser, response.GraphArtifactHash!, subject, evidenceRefs, ct);
        }

        var (safeRelationships, decision) = await GateRelationshipsAsync(
            gate, currentUser, "intelligence.compatibility",
            subject, response.Source, response.GraphArtifactHash,
            response.Compounds, response.Relationships, evidenceRefs, ct);

        return Results.Ok(response with
        {
            Relationships = safeRelationships,
            SafetyStatus = decision.SafetyStatus,
            Warnings = decision.Warnings,
            PolicyRefs = decision.PolicyRefs,
            SafetyReceiptId = decision.SafetyReceiptUri,
        });
    }

    /// <summary>
    /// Run the relationship list through the safety gate: constrain each relationship's reason text,
    /// apply high-risk framing, and disclose fallback. Returns the rewritten relationships (reasons
    /// replaced in place where constrained) and the gate decision carrying the response-level metadata.
    /// </summary>
    private static async Task<(IReadOnlyList<GraphRelationshipResponse> Relationships, IntelligenceOutputDecision Decision)>
        GateRelationshipsAsync(
            IUserFacingIntelligenceGate gate,
            ICurrentUserAccessor currentUser,
            string outputType,
            string subject,
            string source,
            string? graphHash,
            IEnumerable<string> compounds,
            IReadOnlyList<GraphRelationshipResponse> relationships,
            IReadOnlyList<string> evidenceRefs,
            CancellationToken ct)
    {
        // Gate only the reason fields that carry text; track their positions so safe text maps back.
        var indexed = relationships
            .Select((r, i) => (Relationship: r, Index: i))
            .Where(x => !string.IsNullOrWhiteSpace(x.Relationship.Reason))
            .ToList();

        var substances = compounds
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var decision = await gate.EvaluateAsync(new IntelligenceOutputRequest(
            OutputType: outputType,
            ActorUserId: currentUser.GetCurrentUserId(),
            SubjectUri: subject,
            TextFields: indexed.Select(x => x.Relationship.Reason!).ToList(),
            EvidenceRefs: evidenceRefs,
            SourceType: source,
            Substances: substances,
            GraphArtifactHash: graphHash), ct);

        var safe = relationships.ToList();
        for (var k = 0; k < indexed.Count; k++)
        {
            safe[indexed[k].Index] = safe[indexed[k].Index] with { Reason = decision.SafeText[k] };
        }

        return (safe, decision);
    }

    private static List<string> BuildEvidenceRefs(
        string? graphHash,
        IEnumerable<string> compounds,
        IEnumerable<string> sourceRefs)
    {
        var refs = new List<string>();
        if (!string.IsNullOrWhiteSpace(graphHash))
            refs.Add(ReceiptRefs.CompoundGraph(graphHash));
        refs.AddRange(compounds
            .Where(c => !string.IsNullOrWhiteSpace(c))
            .Select(c => ReceiptRefs.Compound(CompoundSlug.From(c))));
        refs.AddRange(sourceRefs.Where(s => !string.IsNullOrWhiteSpace(s)));
        return refs.Distinct(StringComparer.Ordinal).ToList();
    }

    private static Task IssueGraphArtifactReceiptAsync(
        IRuntimeReceiptFactory receipts,
        ICurrentUserAccessor currentUser,
        string graphHash,
        string subject,
        IReadOnlyList<string> evidenceRefs,
        CancellationToken ct)
        => receipts.IssueAndAppendAsync(new ReceiptContext(
            ReceiptClass: ReceiptClass.IntelligenceGraphArtifactUsed,
            SubjectUri: subject,
            Actor: ReceiptActor.User(currentUser.GetCurrentUserId()),
            EvidenceRefs: evidenceRefs,
            Decision: "graph-backed",
            EffectStatus: "non-effecting",
            InputHashSeed: $"{graphHash}|{subject}"), ct);
}
