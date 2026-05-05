namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;

public sealed record LoadedResearchArtifact(ResearchArtifactKind Kind, string Path, JsonNode Node);

public interface IResearchArtifactLoader
{
    LoadedResearchArtifact Load(ResearchArtifactKind kind, string absoluteOrRelativePath);
}

public sealed class ResearchArtifactLoader : IResearchArtifactLoader
{
    private static readonly JsonNodeOptions NodeOpts = new() { PropertyNameCaseInsensitive = false };
    private static readonly JsonDocumentOptions DocOpts = new()
    {
        AllowTrailingCommas = false,
        CommentHandling = JsonCommentHandling.Disallow,
    };

    public LoadedResearchArtifact Load(ResearchArtifactKind kind, string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Research artifact path is required.", nameof(path));
        }

        var resolved = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"Research artifact not found at '{resolved}'.", resolved);
        }

        using var stream = File.OpenRead(resolved);
        JsonNode? root;
        try
        {
            root = JsonNode.Parse(stream, NodeOpts, DocOpts);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Research artifact at '{resolved}' is not valid JSON: {ex.Message}", ex);
        }

        if (root is not JsonObject)
        {
            throw new InvalidOperationException(
                $"Research artifact at '{resolved}' must be a JSON object.");
        }

        return new LoadedResearchArtifact(kind, resolved, root);
    }
}