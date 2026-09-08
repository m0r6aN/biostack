namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json.Nodes;
using Json.Schema;

public interface IResearchArtifactValidator
{
    ValidationResult Validate(ResearchArtifactKind kind, JsonNode artifactNode);
}

public sealed class ResearchArtifactValidator : IResearchArtifactValidator
{
    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private readonly IReadOnlyDictionary<ResearchArtifactKind, JsonSchema> _schemas;
    private readonly IReadOnlyDictionary<string, JsonSchema> _sourceAuthorizationSchemas;

    public ResearchArtifactValidator(
        IReadOnlyDictionary<ResearchArtifactKind, JsonSchema> schemas,
        IReadOnlyDictionary<string, JsonSchema>? sourceAuthorizationSchemas = null)
    {
        _schemas = schemas ?? throw new ArgumentNullException(nameof(schemas));
        _sourceAuthorizationSchemas = sourceAuthorizationSchemas
            ?? (schemas.TryGetValue(ResearchArtifactKind.SourceAuthorizationDecisionBatch, out var historical)
                ? new Dictionary<string, JsonSchema>(StringComparer.Ordinal) { ["1.2.0"] = historical }
                : new Dictionary<string, JsonSchema>(StringComparer.Ordinal));
    }

    public static ResearchArtifactValidator LoadFromDirectory(string schemaDirectory)
    {
        if (string.IsNullOrWhiteSpace(schemaDirectory))
        {
            throw new ArgumentException("Schema directory is required.", nameof(schemaDirectory));
        }

        var resolved = Path.IsPathRooted(schemaDirectory)
            ? schemaDirectory
            : Path.Combine(AppContext.BaseDirectory, schemaDirectory);

        var schemas = new Dictionary<ResearchArtifactKind, JsonSchema>();
        foreach (var descriptor in ResearchArtifactSchemas.All)
        {
            var path = Path.Combine(resolved, descriptor.SchemaFileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Research schema file not found at '{path}'.", path);
            }

            schemas[descriptor.Kind] = JsonSchema.FromFile(path);
        }

        var sourceAuthorizationSchemas = new Dictionary<string, JsonSchema>(StringComparer.Ordinal);
        foreach (var (version, fileName) in ResearchArtifactSchemas.SourceAuthorizationVersions)
        {
            var path = Path.Combine(resolved, fileName);
            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Research schema file not found at '{path}'.", path);
            }
            sourceAuthorizationSchemas[version] = JsonSchema.FromFile(path);
        }

        return new ResearchArtifactValidator(schemas, sourceAuthorizationSchemas);
    }

    public ValidationResult Validate(ResearchArtifactKind kind, JsonNode artifactNode)
    {
        if (artifactNode is null) throw new ArgumentNullException(nameof(artifactNode));
        if (!_schemas.TryGetValue(kind, out var schema))
        {
            throw new ArgumentOutOfRangeException(nameof(kind), kind, "No loaded schema for research artifact kind.");
        }

        if (kind == ResearchArtifactKind.SourceAuthorizationDecisionBatch)
        {
            if (artifactNode is not JsonObject artifact
                || artifact["schemaVersion"] is not JsonValue versionNode
                || !versionNode.TryGetValue<string>(out var version)
                || !_sourceAuthorizationSchemas.TryGetValue(version, out schema))
            {
                return ValidationResult.Invalid(new[]
                {
                    new ValidationError("/schemaVersion", "enum", "Unsupported source authorization schema version."),
                });
            }
        }

        var results = schema.Evaluate(artifactNode, Options);
        if (results.IsValid) return ValidationResult.Valid();

        var errors = new List<ValidationError>();
        FlattenErrors(results, errors);
        if (errors.Count == 0)
        {
            errors.Add(new ValidationError("/", "unknown", "Schema evaluation reported failure with no details."));
        }

        return ValidationResult.Invalid(errors);
    }

    private static void FlattenErrors(EvaluationResults node, List<ValidationError> sink)
    {
        if (node.HasErrors && node.Errors is { } errs)
        {
            foreach (var (keyword, message) in errs)
            {
                sink.Add(new ValidationError(node.InstanceLocation.ToString(), keyword, message));
            }
        }

        if (node.Details is { Count: > 0 } children)
        {
            foreach (var child in children)
            {
                FlattenErrors(child, sink);
            }
        }
    }
}
