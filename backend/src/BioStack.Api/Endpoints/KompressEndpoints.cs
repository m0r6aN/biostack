namespace BioStack.Api.Endpoints;

using Keon.Kompress.Core.Abstractions;
using Keon.Kompress.Core.Model;
using Keon.Kompress.Core.Pipeline;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

/// <summary>
/// Admin-only context compression for agent and operator workflows. These endpoints
/// deliberately do not alter BioStack's canonical evidence, exports, or service-to-service
/// contracts; callers explicitly opt in when preparing text for an LLM context window.
/// </summary>
public static class KompressEndpoints
{
    public static void MapKompressEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/v1/admin/kompress")
            .WithTags("Admin", "Kompress")
            .RequireAuthorization("AdminOnly");

        group.MapPost("/compress", CompressAsync)
            .WithName("KompressContent")
            .WithSummary("Compress content for an LLM context window with reversible storage.");

        group.MapPost("/retrieve", RetrieveAsync)
            .WithName("RetrieveKompressedContent")
            .WithSummary("Retrieve original content referenced by a Kompress marker hash.");
    }

    private static async Task<IResult> CompressAsync(
        [FromBody] KompressContentRequest request,
        [FromServices] CompressionPipeline pipeline,
        [FromServices] IOptions<KompressOptions> options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Content))
        {
            return Results.BadRequest(new { message = "content is required." });
        }

        if (!TryParseContentType(request.ContentType, out var contentType))
        {
            return Results.BadRequest(new
            {
                message = "contentType must be auto, text, json, log, diff, search-results, or conversation."
            });
        }

        var result = await pipeline.CompressAsync(
            request.Content,
            options.Value.ToBaseCompressionConfig(),
            contentType,
            toolName: "biostack-admin-api",
            sessionId: request.SessionId,
            cancellationToken);

        return Results.Ok(new KompressContentResponse(
            result.Content,
            result.TokensBefore,
            result.TokensAfter,
            result.TokensSaved,
            result.Ratio,
            result.TransformsApplied,
            result.Retrievable,
            result.PendingRefs.Select(reference => reference.Hash.Value).ToArray()));
    }

    private static async Task<IResult> RetrieveAsync(
        [FromBody] RetrieveKompressedContentRequest request,
        [FromServices] ICompressionStore store,
        [FromServices] ITenantContext tenantContext,
        CancellationToken cancellationToken)
    {
        if (!ContentHash.TryParse(request.Hash, out var hash))
        {
            return Results.BadRequest(new { message = "hash must be a non-empty hexadecimal Kompress content hash." });
        }

        var tenant = tenantContext.Current;
        var entry = await store.RetrieveAsync(tenant, hash, cancellationToken);
        return entry is null
            ? Results.NotFound()
            : Results.Ok(new RetrieveKompressedContentResponse(
                entry.OriginalContent,
                entry.OriginalTokens,
                entry.ToolName,
                entry.CreatedAtUtc));
    }

    private static bool TryParseContentType(string? value, out ContentType contentType)
    {
        var normalized = value?.Trim().ToLowerInvariant();
        contentType = normalized switch
        {
            null or "" or "auto" => ContentType.Unknown,
            "text" or "plain-text" => ContentType.PlainText,
            "json" => ContentType.Json,
            "log" => ContentType.Log,
            "diff" => ContentType.Diff,
            "search-results" => ContentType.SearchResults,
            "conversation" => ContentType.Conversation,
            _ => (ContentType)(-1),
        };

        return (int)contentType >= 0;
    }
}

public sealed record KompressContentRequest(
    string Content,
    string? ContentType = "auto",
    string? SessionId = null);

public sealed record KompressContentResponse(
    string Content,
    int TokensBefore,
    int TokensAfter,
    int TokensSaved,
    double Ratio,
    IReadOnlyList<string> TransformsApplied,
    bool Retrievable,
    IReadOnlyList<string> RetrievalHashes);

public sealed record RetrieveKompressedContentRequest(string Hash);

public sealed record RetrieveKompressedContentResponse(
    string Content,
    int OriginalTokens,
    string? ToolName,
    DateTimeOffset CreatedAtUtc);
