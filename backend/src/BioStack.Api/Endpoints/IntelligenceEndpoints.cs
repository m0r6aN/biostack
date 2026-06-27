namespace BioStack.Api.Endpoints;

using BioStack.Application.Services;
using BioStack.Application.Services.Intelligence;
using BioStack.Contracts.Responses;
using BioStack.Infrastructure.Keon;
using BioStack.Infrastructure.Knowledge;

/// <summary>
/// Graph-backed intelligence read surfaces (Lane C). Serves reviewed compound relationships and
/// compatibility from the materialized graph, and emits an <c>intelligence.graph-artifact.used</c>
/// receipt whenever a graph-backed artifact informs a user-facing response.
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
        if (graphBacked)
        {
            var subject = $"intelligence:compound:{CompoundSlug.From(compound)}/relationships";
            var compounds = new[] { compound }
                .Concat(response.Relationships.SelectMany(r => new[] { r.SubjectCompound, r.ObjectCompound }));
            var evidenceRefs = BuildEvidenceRefs(
                response.GraphArtifactHash,
                compounds,
                response.Relationships.SelectMany(r => r.SourceRefs));

            await IssueGraphArtifactReceiptAsync(
                receipts, currentUser, response.GraphArtifactHash!, subject, evidenceRefs, ct);
        }

        return Results.Ok(response);
    }

    private static async Task<IResult> GetCompatibility(
        HttpContext httpContext,
        IGraphIntelligenceService graphIntelligence,
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
        if (graphBacked)
        {
            var subject = $"intelligence:compatibility:{string.Join("+", response.Compounds.Select(CompoundSlug.From).OrderBy(s => s, StringComparer.Ordinal))}";
            var evidenceRefs = BuildEvidenceRefs(
                response.GraphArtifactHash,
                response.Compounds,
                response.Relationships.SelectMany(r => r.SourceRefs));

            await IssueGraphArtifactReceiptAsync(
                receipts, currentUser, response.GraphArtifactHash!, subject, evidenceRefs, ct);
        }

        return Results.Ok(response);
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
