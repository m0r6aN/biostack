namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Raw substance record payload and its position in the seed file. The pipeline
/// keeps the parsed <see cref="JsonNode"/> alongside its typed projection so
/// schema validation runs against the original document (draft-2020-12 aware).
/// </summary>
public sealed record LoadedRecord(int Index, JsonNode Node);

/// <summary>
/// Loads and parses the seed dataset from a JSON file on disk. Does not validate
/// the schema; it only ensures the file exists, is a JSON array, and each element
/// is an object. Validation is delegated to <see cref="ISubstanceRecordValidator"/>.
/// </summary>
public interface ISubstanceRecordLoader
{
    IReadOnlyList<LoadedRecord> Load(string absoluteOrRelativePath);
}

public sealed class SubstanceRecordLoader : ISubstanceRecordLoader
{
    private static readonly JsonNodeOptions NodeOpts = new() { PropertyNameCaseInsensitive = false };
    private static readonly JsonDocumentOptions DocOpts = new()
    {
        AllowTrailingCommas = false,
        CommentHandling     = JsonCommentHandling.Disallow,
    };

    public IReadOnlyList<LoadedRecord> Load(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Seed file path is required.", nameof(path));
        }

        var resolved = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException(
                $"Seed dataset not found at '{resolved}'. " +
                "Check Worker:SeedFilePath and container volume mounts.",
                resolved);
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
                $"Seed dataset at '{resolved}' is not valid JSON: {ex.Message}", ex);
        }

        if (root is not JsonArray arr)
        {
            throw new InvalidOperationException(
                $"Seed dataset at '{resolved}' must be a JSON array of substance records.");
        }

        var records = new List<LoadedRecord>(arr.Count);
        for (var i = 0; i < arr.Count; i++)
        {
            var node = arr[i];
            if (node is not JsonObject)
            {
                throw new InvalidOperationException(
                    $"Seed dataset entry at index {i} is not a JSON object.");
            }
            records.Add(new LoadedRecord(i, node!));
        }

        return records;
    }
}
