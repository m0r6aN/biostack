namespace BioStack.KnowledgeWorker.Pipeline;

using System.Text.Json;
using System.Text.Json.Nodes;
using Json.Schema;

/// <summary>
/// Validates raw substance records against the canonical draft-2020-12 schema
/// at <c>Schemas/substance-record.schema.json</c>. Returns a list of all
/// violations (not first-fail) so operators can see every problem per record.
/// </summary>
public interface ISubstanceRecordValidator
{
    ValidationResult Validate(JsonNode recordNode);
}

public sealed class SubstanceRecordValidator : ISubstanceRecordValidator
{
    private static readonly EvaluationOptions Options = new()
    {
        OutputFormat    = OutputFormat.List,
        RequireFormatValidation = true,
    };

    private readonly JsonSchema _schema;

    public SubstanceRecordValidator(JsonSchema schema)
    {
        _schema = schema ?? throw new ArgumentNullException(nameof(schema));
    }

    /// <summary>Load the schema from disk (bundled next to the worker DLL).</summary>
    public static SubstanceRecordValidator LoadFromFile(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException("Schema path is required.", nameof(path));
        }

        var resolved = Path.IsPathRooted(path)
            ? path
            : Path.Combine(AppContext.BaseDirectory, path);

        if (!File.Exists(resolved))
        {
            throw new FileNotFoundException($"Schema file not found at '{resolved}'.", resolved);
        }

        var schema = JsonSchema.FromFile(resolved);
        return new SubstanceRecordValidator(schema);
    }

    public ValidationResult Validate(JsonNode recordNode)
    {
        if (recordNode is null)
        {
            throw new ArgumentNullException(nameof(recordNode));
        }

        var results = _schema.Evaluate(recordNode, Options);

        if (results.IsValid)
        {
            return ValidationResult.Valid();
        }

        var errors = new List<ValidationError>();
        FlattenErrors(results, errors);

        if (errors.Count == 0)
        {
            // Defensive: IsValid=false with no attached details shouldn't happen,
            // but never let a record slip through silently.
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
                sink.Add(new ValidationError(
                    Location: node.InstanceLocation.ToString(),
                    Keyword:  keyword,
                    Message:  message));
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
